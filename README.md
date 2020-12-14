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

*Serilog* is used for the logging. Please check the homepage for details https://serilog.net/. Please check out this page https://github.com/serilog/serilog-settings-configuration to figure out what can be done in the *appsettings.json*.

If you don't want to play with the settings, but still want to have the logs in a file, you can simply redirect the standard out:
![image](https://user-images.githubusercontent.com/12346829/102124550-baf33300-3e48-11eb-93e1-04eb4292ad65.png)

The current default 
