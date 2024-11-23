# TobiiHTCViveDevkit VRCFT
VRCFT v5 Module that adds Eye Tracking from TobiiXR HTC Vive Devkits

![TobiiDevkit](TobiiDevkit.png)

## Setup

~~Go to ~~:~~ https://developer.tobii.com/xr/develop/xr-sdk/getting-started/tobii-htc-dev-kit/~~

~~Follow Steps 1 and 2 to install the drivers and calibrate.~~

(Old Link is dead, Updated Instructions are below)

1) Go to https://gaming.tobii.com/getstarted/
   
      Under "Select your hardware", choose "Tobii"
  
      Under "Select Tobii device", choose "Tobii EyeX" then install the "Tobii Eye Tracking Core Software"

2) Go to https://developer.tobii.com/xr/explore/mirrors/#setup

      Dowload the "Tobii HTC VIVE Devkit" version of The Mirrors Demo

      Perform the Setup step, then exit The Mirrors Demo

   

3) Install the module Into VRCFT.

## Notes

Due to an issue with Tobii Stream Engine this module will always fail to shut down its own thread during teardown (closing VRCFT/ Uninstalling Modules). This will cause VRCFT to freeze until it is shut down forcefully (Such as with Task Manager). 

If you plan on uninstalling this or other modules. You will either need to Run VRCFT with the Devkit Disconnected, or delete the modules manually (They are typically stored within "C:\Users\<user>\AppData\Roaming\VRCFaceTracking\CustomLibs"
