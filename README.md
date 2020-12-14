# Introduction
The *WorkaroundUtilities* aim to enable workarounds for program issues. This can mean refreshing the browser on certain events, or killing and restarting a process.

# Get the program
Currently every new source version creates an executable. The recent history can be viewed in the actions: https://github.com/kirni/WorkaroundUtilities/actions
Going into the details of a activity will display the generated artifacts. 
![14-12-_2020_19-24-09](https://user-images.githubusercontent.com/12346829/102122296-921d6e80-3e45-11eb-957c-5ae465db5ce9.jpg)
![image](https://user-images.githubusercontent.com/12346829/102122584-f4766f00-3e45-11eb-90fd-2384f63caab9.png)

Currently this contains a debug build of the executable, the license, a sample of the [application settings](#Configure-the-WorkaroundUtilities) and this readme file.

# Run the program
At the moment it is a hard requirement to have the [appsettings.json](#Configure-the-WorkaroundUtilities) in the working directory.

# Configure the WorkaroundUtilities
The whole configuration is done in the file *appsettings.json* and consists of two main part - one for the logging and one for the actual workaround handlers.

The default settings aim to cover all options of the latter. Therefore it will be used as reference for further explanations:

````json
{
    "workarounds": [
      {
        "eventpollingSec": "10",
        "description": "Chrome refresh",
        "events": [ "USBconnectedEvent{F:}{G:}", "FileExistingEvent{F:/refresh.txt}{G:/refresh.txt}" ],
        "actions": [ "SendF5Action{Chrome}" ]
      },
      {
        "eventpollingSec": "20",
        "description": "Chrome kill",
        "events": ["RAMlimitEvent{Chrome}{2000}"],
        "actions": [ "TerminateProcessAction{Chrome}", "StartProcessAction{C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\Google Chrome.lnk}" ]
      }
    ],
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Overrride": {
        "Microsoft": "Information",
        "System": "Warning"
      }
    }
  }
}
````

## Configure the logging
*Serilog* is used for the logging. Please check the homepage for details https://serilog.net/. Please check out this page https://github.com/serilog/serilog-settings-configuration to figure out what can be done in the *appsettings.json*.

If you don't want to play with the settings, but still want to have the logs in a file, you can simply redirect the standard out:
![image](https://user-images.githubusercontent.com/12346829/102124550-baf33300-3e48-11eb-93e1-04eb4292ad65.png)

The current default minimum level is set to *Information*.

## Configure the warkaround workers
The main part of the configuration is a list of *workarounds*. Each of those will be executed in an independed thread. The basic execution is always the same:

In intervals of *eventpollingSec* all *events* are checked in the same order as they are configured. If all events are true, all *actions* are executed in the same order as they are configured.

### workaround options
| Option | datatype | description |
| --- | --- | --- |
| eventpollingSec | float | time in seconds in which the events are getting checked. |
| description | string | optional description of the workaround. it is used consistantly for the logging in order to connect the logs to the configured workarounds. therefoe it is recommended to use meaningful text. |
| events | array of strings | **all** valid events need to be true to cause any action. required options need to be specified between *{}*. if a event is not configured properly it will be skipped and a warning will be logged. if there is no valid event, the whole worker will be skipped and another warning will be logged. |
| actions | array of strings | When all events are true, all valid actions will be executed in the same order as configured. required options need to be specified betwenn *{}*. if a event is not configured properly it will be skipped and a warning will be logged. if there is no valud action, the whole worker will be skipped and another warning will be logged. |

#### implemented events
| event name | options | description | example |
| --- | --- | --- | --- |
| USBconnectedEvent | list of drives | is active when **any** of the defined drives is connected. this is checked with the [DriveInfo](https://docs.microsoft.com/en-us/dotnet/api/system.io.driveinfo?f1url=%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(System.IO.DriveInfo);k(DevLang-csharp)%26rd%3Dtrue&view=net-5.0) class via *IsReady* and *DriveType.Removable* | *USBconnectedEvent{F:}{G:}* is true when drive *F:* **or** drive *G:* are connected. |
| FileExistingEvent | list of path to files | is true when **any** of the defined files is existing | *FileExistingEvent{F:/refresh.txt}{G:/refresh.txt}* is true when the file *F:/refresh.txt* **or** the file *G:/refresh.txt* is existing. |
| RAMlimitEvent | **first** name of the process **second** *long* for RAM in *megabyte* | checks if the defined process together with all it's sub-taks are consuming more than the specified RAM. this is based on [Process.WorkingSet64](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.peakworkingset64?view=net-5.0#System_Diagnostics_Process_PeakWorkingSet64) and might be different to what you can see in the task manager. wrong types in the options or less than two options will currently cause exceptions. Additional options are ignored. | *RAMlimitEvent{Chrome}{2000}* checks if Chrome together with *all sub-tasks* uses more than 2000 MB |




