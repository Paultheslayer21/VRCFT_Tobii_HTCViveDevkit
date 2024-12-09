using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VRCFaceTracking;
using Tobii.StreamEngine;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.IO;
using System.Text.Json;
using System.Reflection;

public class TobiiHTCVive : ExtTrackingModule
{
    // The interface that your module can send as tracking data.
    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, false);

    // Initialize the variables needed for Tobii Stream Engine
    private IntPtr apiContext = Marshal.AllocHGlobal(1024);
    private IntPtr deviceContext = Marshal.AllocHGlobal(1024);
    private List<string> urls;
    private tobii_error_t result;

    // A class to represent your config file
    private class TobiiConfig
    {
        public float XOffset
        {
            get; set;
        }
        public float YOffset
        {
            get; set;
        }
        public float XNormalization
        {
            get; set;
        }
        public float YNormalization
        {
            get; set;
        }
    }

    private TobiiConfig config;

    // This function is called when the module is initialized.
    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
    {
        // Get the directory of the currently running assembly (DLL or EXE)
        string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // Determine the path to the configuration file (next to the DLL)
        string configPath = Path.Combine(assemblyDirectory, "TobiiConfig.json");

        // Load configuration
        try
        {
            var configContent = File.ReadAllText(configPath);
            config = JsonSerializer.Deserialize<TobiiConfig>(configContent);
            Logger.LogInformation("Configuration loaded successfully from: " + configPath);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to load configuration from {configPath}: {ex.Message}");
            return (false, false);
        }

        ModuleInformation.Name = "Tobii HTC Vive Devkit";

        // Example of an embedded image stream being referenced as a stream
        var stream = GetType().Assembly.GetManifestResourceStream("VRCFaceTracking.TobiiHTCVive.Resources.Icon.png");
        ModuleInformation.StaticImages = stream != null ? new List<Stream> { stream } : ModuleInformation.StaticImages;

        Logger.LogInformation("Initializing module...");

        // Create Tobii API
        result = Interop.tobii_api_create(out apiContext, null);
        if (result == tobii_error_t.TOBII_ERROR_NO_ERROR)
            Logger.LogInformation("Tobii API Create Success");
        else
        {
            Logger.LogCritical("Tobii API Create Failure!");
            return (false, false);
        }

        // Enumerate devices to find connected eye trackers
        result = Interop.tobii_enumerate_local_device_urls(apiContext, out urls);
        if (result == tobii_error_t.TOBII_ERROR_NO_ERROR)
            Logger.LogInformation("Tobii Enumerate Devices Success");
        else
        {
            Logger.LogCritical("Tobii Enumerate Devices Failure!");
            return (false, false);
        }
        if (urls.Count == 0)
        {
            Logger.LogCritical("No Tobii Device found");
            return (false, false);
        }
        Logger.LogInformation("Tobii Devices found:");
        foreach (var url in urls)
        {
            Logger.LogInformation(url);
        }

        // Connect to the first tracker found
        result = Interop.tobii_device_create(apiContext, urls[0], Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE, out deviceContext);
        if (result == tobii_error_t.TOBII_ERROR_NO_ERROR)
            Logger.LogInformation("Tobii Device Create Success");
        else
        {
            Logger.LogCritical("Tobii Device Create Failure!");
            return (false, false);
        }

        return (true, false);
    }

    // This function is called when the module is unloaded or when VRCFaceTracking tears down the module.
    public override void Teardown()
    {
        // Deinitialize the tracking interface and dispose of any data created with the module
        Marshal.FreeHGlobal(deviceContext);
        Marshal.FreeHGlobal(apiContext);
        Interop.tobii_wearable_consumer_data_unsubscribe(deviceContext);
        Interop.tobii_device_destroy(deviceContext);
        Interop.tobii_api_destroy(apiContext);
    }

    // This function is called to poll data from the tracking interface. It will run in a separate thread.
    public override void Update()
    {
        // Subscribe to consumer data which will be sent to the ProcessCallback method
        result = Interop.tobii_wearable_consumer_data_subscribe(deviceContext, ProcessCallback);
        while (true)
        {
            // Optionally block this thread until data is available
            Interop.tobii_wait_for_callbacks(new[] { deviceContext });
            Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR || result == tobii_error_t.TOBII_ERROR_TIMED_OUT);

            // Process callbacks if data is available
            Interop.tobii_device_process_callbacks(deviceContext);
            Debug.Assert(result == tobii_error_t.TOBII_ERROR_NO_ERROR);
        }

        // Add a delay or halt for the next update cycle for performance
        Thread.Sleep(10);
    }

    // This is the callback function that processes the consumer data from the eye tracker
    private void ProcessCallback(ref tobii_wearable_consumer_data_t consumerData, nint userData)
    {
        // Apply offsets and normalization based on the loaded configuration
        var XOffset = config.XOffset;
        var YOffset = config.YOffset;
        var XNormalization = config.XNormalization;
        var YNormalization = config.YNormalization;

        // Left Eye
        if (consumerData.left.pupil_position_in_sensor_area_validity == tobii_validity_t.TOBII_VALIDITY_VALID)
        {
            // Blink State
            UnifiedTracking.Data.Eye.Left.Openness = consumerData.left.blink == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE ? (float)0 : (float)1;

            // Gaze Data
            UnifiedTracking.Data.Eye.Left.Gaze.x = ((consumerData.left.pupil_position_in_sensor_area_xy.x * 2 - 1) - XOffset) / XNormalization;
            UnifiedTracking.Data.Eye.Left.Gaze.y = ((consumerData.left.pupil_position_in_sensor_area_xy.y * -2 + 1) + YOffset) / YNormalization;
        }

        // Right Eye
        if (consumerData.right.pupil_position_in_sensor_area_validity == tobii_validity_t.TOBII_VALIDITY_VALID)
        {
            // Blink State
            UnifiedTracking.Data.Eye.Right.Openness = consumerData.right.blink == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE ? (float)0 : (float)1;

            // Gaze Data
            UnifiedTracking.Data.Eye.Right.Gaze.x = ((consumerData.right.pupil_position_in_sensor_area_xy.x * 2 - 1) + XOffset) / XNormalization;
            UnifiedTracking.Data.Eye.Right.Gaze.y = ((consumerData.right.pupil_position_in_sensor_area_xy.y * -2 + 1) + YOffset) / YNormalization;
        }
    }
}
