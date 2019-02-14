# RegistryUWP Desktop Bridge Sample
## Windows - Developer Incubation and Learning - Paula Scholz

<figure>
  <img src="/images/RegistryUWPIcon.png" alt="Registry UWP Icon"/>
</figure>

Many operations in Windows require access to the Registry, a hierarchical database for storing many kinds of system and applications settings, either to read key values or to write them.  However, in a Universal Windows Platform application, access to the Registry is forbidden.

In this sample, we launch a Win32 fullTrust application (RegistryReadAppService) using Desktop Bridge and command it to read the Startup programs key values in the HKEY_LOCAL_MACHINE root element, SOFTWARE\Microsoft\Windows\CurrentVersion\Run.  

This key's values are the names and locations of the operating system's Startup programs.  We return a string array of these values to the UWP program over an AppServiceConnection where they are converted into an ObservableCollection and bound to a ListView in the user interface.

We can then press a button to add "notepad.exe" to the list of Startup programs.  This sends a command to RegistryReadAppService to launch an elevated process to write to this subkey of the HKEY_LOCAL_MACHINE root element, which requires Administrative privilege.  This will only persist to the real machine registry when written by a 64-bit process.  32-bit processes will only change a virtualized registry entry for that user and application, and not the actual HKEY_LOCAL_MACHINE element.

If the user presses "Yes" on the elevation dialog raised by Windows, the RegistryReadAppService launches ElevatedRegistryWrite.exe with admin privilege, which adds "notepad" to the list of Startup programs.  Then, when the computer is rebooted, Notepad will launch along with the other startup programs.

If "notepad" is already on the list, then pushing the button will remove it and Notepad will no longer be started on reboot.

<figure>
  <img src="/images/Fig1_RegistryUWPSample.PNG" alt="Registry UWP Icon"/>
  <figcaption>Figure 1 - RegistryUWP Application</figcaption>
</figure>

In the application we have a ListView, where the Startup program list is displayed, a button which says "Add Notepad" or "Remove Notepad", depending on whether or not Notepad.exe is already on the list, and a Status area where messages are displayed.  The application detects if it is running in S-Mode and also the processor mode of the computer it runs on.

## Visual Studio Solution

The Visual Studio solution is shown below.  There are four projects.

<figure>
  <img src="/images/Fig2_RegistryUWPSolution.PNG" alt="Visual Studio Solution"/>
  <figcaption>Figure 2 - RegistryUWP Visual Studio Solution</figcaption>
</figure>

From the top down, we first see the ElevatedRegistryWrite project. This is the application launched by the RegistryReadAppService as elevated with Administrator privilege to write to the registry.  While the RegistryReadAppService and RegistryUWP programs communicate with each other over an [AppServiceConnection](https://docs.microsoft.com/en-us/uwp/api/Windows.ApplicationModel.AppService.AppServiceConnection), ElevatedRegistryWrite.exe can only communicate with the RegistryReadAppService by its exit code, which is actually sufficient for our purpose.

Next, the solution contains **RegistryPackaging**, the start-up project, which is the [Windows Application Packaging Project](https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-packaging-dot-net) for our solution.  It is within the context of this project that our solution will be packaged for sideloading and deployment to the Windows Store.  This is the project where package capabilities, store logos, and configuration are set.

Then, the RegistryReadAppService project.  This is the "headless" Win32 App Service layer in the middle of our solution which shows no user interface and has no window.  This application receives commands over the AppServiceConnection from RegistryUWP and reads the registry or launches ElevatedRegistryWrite to write to the registry.  Data is sent back to the RegistryUWP program in an [AppServiceResponse](https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.appservice.appserviceresponse) to the request.

Finally, we have the RegistryUWP project at the bottom of the Visual Studio solution, the Universal Windows Platform program that provides the user interface, launches RegistryReadAppService as a "full-trust" Win32 app, receives Startup program names from RegistryReadAppService over an AppServiceConnection, and sends an "elevatedRegistryWrite" command to RegistryReadAppService which then launches ElevatedRegistryWrite as an elevated process.

