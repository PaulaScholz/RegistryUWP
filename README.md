# RegistryUWP Desktop Bridge Sample
## Windows - Developer Incubation and Learning - Paula Scholz

<figure>
  <img src="/images/RegistryUWPicon.png" alt="Registry UWP Icon"/>
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
<br/>

In the application we have a ListView, where the Startup program list is displayed, a button which says "Add Notepad" or "Remove Notepad", depending on whether or not Notepad.exe is already on the list, and a Status area where messages are displayed.  The application detects if it is running in S-Mode and also the processor mode of the computer it runs on.

## Visual Studio Solution

The Visual Studio solution is shown below.  There are four projects.

<figure>
  <img src="/images/Fig2_RegistryUWPSolution.PNG" alt="Visual Studio Solution"/>
  <figcaption>Figure 2 - RegistryUWP Visual Studio Solution</figcaption>
</figure>
<br/>

From the top down, we first see the ElevatedRegistryWrite project. This is the application launched by the RegistryReadAppService as elevated with Administrator privilege to write to the registry.  While the RegistryReadAppService and RegistryUWP programs communicate with each other over an [AppServiceConnection](https://docs.microsoft.com/en-us/uwp/api/Windows.ApplicationModel.AppService.AppServiceConnection), ElevatedRegistryWrite.exe can only communicate with the RegistryReadAppService by its exit code, which is actually sufficient for our purpose.

Next, the solution contains **RegistryPackaging**, the start-up project, which is the [Windows Application Packaging Project](https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-packaging-dot-net) for our solution.  It is within the context of this project that our solution will be packaged for sideloading and deployment to the Windows Store.  This is the project where package capabilities, store logos, and configuration are set.

Then, the RegistryReadAppService project.  This is the "headless" Win32 App Service layer in the middle of our solution which shows no user interface and has no window.  This application receives commands over the AppServiceConnection from RegistryUWP and reads the registry or launches ElevatedRegistryWrite to write to the registry.  Data is sent back to the RegistryUWP program in an [AppServiceResponse](https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.appservice.appserviceresponse) to the request.

Finally, we have the RegistryUWP project at the bottom of the Visual Studio solution, the Universal Windows Platform program that provides the user interface, launches RegistryReadAppService as a "full-trust" Win32 app, receives Startup program names from RegistryReadAppService over an AppServiceConnection, and sends an "elevatedRegistryWrite" command to RegistryReadAppService which then launches ElevatedRegistryWrite as an elevated process.

Let's look at how these projects relate to each other:

<figure>
  <img src="/images/RegistryUWP_AppStackPack.png" alt="Visual Studio Solution"/>
  <figcaption>Figure 3 - RegistryUWP Package Applications</figcaption>
</figure>
<br/>

## RegistryPackaging Project

The **RegistryPackaging** project is the solution's startup project, and is the [Windows Application Packaging Project](https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-packaging-dot-net) container for our application bundle.  This project contains the packaging manifest where we declare capabilities needed by included application projects.  Note that this project has the 64-bit architecture selected, which is required for the app to write to the HKEY_LOCAL_MACHINE hive.  It is also built for Windows 10, Version 1809 (build 17763), and you need to run this on that version or better to use fullTrust/allowElevation capabilities.

<figure>
  <img src="/images/RegistryUWP_packProjProp.PNG" alt="RegistryUWP Packaging Properties"/>
  <figcaption>Figure 4 - RegistryUWP Packaging Properties</figcaption>
</figure>
<br/>

Those capabilities are declared in the project's **Package.appxmanifest** file.  You will have to view this file as code to edit the required properties.  There are three XML blocks in this file that concern us.

The first of these is the enclosing Package node at the top of the file.  Here, three additional namespaces are declared to support Desktop Bridge.  These are **rescap, desktop**, and **uap3**.  **Rescap** lets us declare restricted app capabilities, **desktop** contains references to the Windows Desktop Extensions needed for Desktop Bridge, and **uap3** allows us to declare an AppExecutionAlias for elevation.

```xml
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest" 
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" 
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" 
         xmlns:desktop="http://schemas.microsoft.com/appx/manifest/desktop/windows10" 
         xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3" 
         IgnorableNamespaces="uap mp rescap desktop">
```

The Extensions child node of the Applications node is where we declare our windows.fullTrustProcess for access to restricted Win32 APIs and a relative path within the application bundle to the executable.  The windows.AppExecutionAlias and relative path to the elevated registry write application is also declared here, as well as the "CommunicationService" app service between RegistryUWP.exe and RegistryReadAppService.exe

```xml
      <Extensions>
        <desktop:Extension Category="windows.fullTrustProcess" Executable="RegistryReadAppService\RegistryReadAppService.exe" />
        <uap3:Extension Category="windows.appExecutionAlias" Executable="ElevatedRegistryWrite\ElevatedRegistryWrite.exe" EntryPoint="Windows.FullTrustApplication">
          <uap3:AppExecutionAlias>
            <desktop:ExecutionAlias Alias="ElevatedRegistryWrite.exe" />
          </uap3:AppExecutionAlias>
        </uap3:Extension>
        <uap:Extension Category="windows.appService">
          <uap:AppService Name="CommunicationService" />
        </uap:Extension>
      </Extensions>
```

The last node we need to modify is the Capabilities node.  Here, we add the restricted capabilities *runFullTrust* and *allowElevation*, like this:

```xml
  <Capabilities>
    <Capability Name="internetClient" />
    <rescap:Capability Name="runFullTrust" />
    <rescap:Capability Name="allowElevation" />
  </Capabilities>
```
## RegistryUWP - Universal Windows Platform Project

