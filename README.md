# RegistryUWP Desktop Bridge Sample
## Windows - Developer Incubation and Learning - Paula Scholz

<figure>
  <img src="/images/RegistryUWPicon.png" alt="Registry UWP Icon"/>
</figure>

Many operations in Windows require access to the Registry, either to read key values or to write them.  However, in a Universal Windows Platform application, access to the Registry is forbidden.

In this sample, from the UWP application `RegistryUWP`, we launch a Win32 fullTrust application `RegistryReadAppService` using Desktop Bridge and command it to read the Startup programs key values in the `HKEY_LOCAL_MACHINE` root element, `SOFTWARE\Microsoft\Windows\CurrentVersion\Run`.  

This key's values are the names and locations of the operating system's Startup programs.  We return a string array of these values to the UWP program over an `AppServiceConnection` where they are converted into an `ObservableCollection` and bound to a `ListView` in the user interface.

We can then press a button to add `Notepad.exe` to the list of Startup programs.  This sends a command to `RegistryReadAppService` to launch an elevated process to write to this subkey of the `HKEY_LOCAL_MACHINE` root element, which requires Administrative privilege.  This will only persist to the real machine registry when written by a 64-bit process.  32-bit processes will only change a [virtualized registry](https://docs.microsoft.com/en-us/windows/desktop/SysInfo/registry-virtualization) entry for that user and application, and not the actual `HKEY_LOCAL_MACHINE` element.

If the user presses "Yes" on the elevation dialog raised by Windows, the `RegistryReadAppService` launches `ElevatedRegistryWrite.exe` with admin privilege, which adds "notepad" to the list of Startup programs.  Then, when the computer is rebooted, `Notepad.exe` will launch along with the other startup programs.

If "notepad" is already on the list, then pushing the button will remove it and `Notepad.exe` will no longer be started on reboot.

<figure>
  <img src="/images/Fig1_RegistryUWPSample.PNG" alt="Registry UWP Icon"/>
  <figcaption>Figure 1 - RegistryUWP Application</figcaption>
</figure>

In the application we have a `ListView`, where the Startup program list is displayed, a `Button` which says **"Add Notepad"** or **"Remove Notepad"**, depending on whether or not `Notepad.exe` is already on the list, and a Status area where messages are displayed.  The application detects if it is running in S-Mode and also the processor mode of the computer it runs on.

## Visual Studio Solution

The Visual Studio solution is shown below.  There are four projects.

<figure>
  <img src="/images/Fig2_RegistryUWPSolution.PNG" alt="Visual Studio Solution"/>
  <figcaption>Figure 2 - RegistryUWP Visual Studio Solution</figcaption>
</figure>

From the top down, we first see the `ElevatedRegistryWrite` project. This is the application launched by the `RegistryReadAppService` as elevated with Administrator privilege to write to the registry.  While the `RegistryReadAppService` and `RegistryUWP` programs communicate with each other over an [AppServiceConnection](https://docs.microsoft.com/en-us/uwp/api/Windows.ApplicationModel.AppService.AppServiceConnection), `ElevatedRegistryWrite.exe` can only communicate with the `RegistryReadAppService` by its exit code, which is actually sufficient for our purpose.

Next, the solution contains `RegistryPackaging`, the start-up project, which is the [Windows Application Packaging Project](https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-packaging-dot-net) for our solution.  It is within the context of this project that our solution will be packaged for sideloading and deployment to the [Windows Store](https://www.microsoft.com/en-us/store/apps/windows).  This is the project where package capabilities, store logos, and configuration are set.

Then, the `RegistryReadAppService` project.  This is the "headless" Win32 App Service layer in the middle of our solution which shows no user interface and has no window.  This application receives commands over the `AppServiceConnection` from `RegistryUWP` and reads the registry or launches `ElevatedRegistryWrite` to write to the registry.  Data is sent back to the `RegistryUWP` program in an [AppServiceResponse](https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.appservice.appserviceresponse) to the request.

Finally, we have the `RegistryUWP` project at the bottom of the Visual Studio solution, the UWP program that provides the user interface, launches `RegistryReadAppService` as a "full-trust" Win32 app, receives Startup program names from `RegistryReadAppService` over an `AppServiceConnection`, and sends an "elevatedRegistryWrite" command to `RegistryReadAppService` which then launches `ElevatedRegistryWrite` as an elevated process.

Let's look at how these projects relate to each other:

<figure>
  <img src="/images/RegistryUWP_AppStackPack.png" alt="Visual Studio Solution"/>
  <figcaption>Figure 3 - RegistryUWP Package Applications</figcaption>
</figure>

## RegistryPackaging Project

The `RegistryPackaging` project is the solution's startup project, and is the [Windows Application Packaging Project](https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-packaging-dot-net) container for our application bundle.  This project contains the packaging manifest where we declare capabilities needed by included application projects.  Note that this project has the 64-bit architecture selected, which is required for the app to write to the `HKEY_LOCAL_MACHINE` hive.  It is also built for **Windows 10, Version 1809 (build 17763)**, and you need to run this on that version of Windows or better to use fullTrust/allowElevation capabilities.

<figure>
  <img src="/images/RegistryUWP_packProjProp.PNG" alt="RegistryUWP Packaging Properties"/>
  <figcaption>Figure 4 - RegistryUWP Packaging Properties</figcaption>
</figure>

Those capabilities are declared in the project's `Package.appxmanifest` file.  You will have to view this file as code to edit the required properties.  There are three XML blocks in this file that concern us.

The first of these is the enclosing `Package` node at the top of the file.  Here, three additional namespaces are declared to support Desktop Bridge.  These are `rescap`, `desktop`, and `uap3`.  `Rescap` lets us declare restricted app capabilities, `desktop` contains references to the [Windows Desktop Extensions](https://docs.microsoft.com/en-us/windows/uwp/porting/desktop-to-uwp-extensions) needed for Desktop Bridge, and `uap3` allows us to declare an `AppExecutionAlias` for elevation.

```xml
<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
         xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest" 
         xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" 
         xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" 
         xmlns:desktop="http://schemas.microsoft.com/appx/manifest/desktop/windows10" 
         xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3" 
         IgnorableNamespaces="uap mp rescap desktop">
```

The `Extensions` child node of the `Applications` node is where we declare our `windows.fullTrustProcess` for access to restricted Win32 APIs and a relative path within the application bundle to the executable.  The `windows.AppExecutionAlias` and relative path to the elevated registry write application is also declared here, as well as the "CommunicationService" `AppService` between `RegistryUWP.exe` and `RegistryReadAppService.exe`

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

The last node we need to modify is the `Capabilities` node.  Here, we add the restricted capabilities `runFullTrust` and `allowElevation`, like this:

```xml
  <Capabilities>
    <Capability Name="internetClient" />
    <rescap:Capability Name="runFullTrust" />
    <rescap:Capability Name="allowElevation" />
  </Capabilities>
```

## RegistryUWP - Universal Windows Platform Project

The `RegistryUWP` project contains our user interface layer.  It has three XAML files accompanied by code-behind pages.  The first, `App.xaml`, is standard in any UWP application and contains application initialization code, most generated automatically by a template when the project was created.  For a Desktop Bridge application though, we need to add some code.

We need to set up the [AppServiceConnection](https://docs.microsoft.com/en-us/uwp/api/Windows.ApplicationModel.AppService.AppServiceConnection), which will be established when the fullTrustProcess `RegistryReadAppService` is launched and connects to `RegistryUWP` for the first time.  This code, in App.xaml.cs, is:

```c#
        /// <summary>
        /// Our app service connection endpoint.
        /// </summary>
        public static AppServiceConnection Connection = null;

        /// <summary>
        /// Invoked when our application is activated in the background.
        /// </summary>
        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);

            // if we've been triggered by the app service
            if (args.TaskInstance.TriggerDetails is AppServiceTriggerDetails)
            {
                BackgroundTaskDeferral appServiceDeferral = args.TaskInstance.GetDeferral();
                AppServiceTriggerDetails details = args.TaskInstance.TriggerDetails as AppServiceTriggerDetails;
                Connection = details.AppServiceConnection;

                // Inform the RegistryPage instance so it can hook up the Connection event handlers
                // to its methods.  
                RegistryPage.Current?.RegisterConnection();

            }
        }
```

The `AppServiceConnection` has an event handler that must be connected before we can receive data from it and this is done in the `RegistryPage.xaml.cs` file, which is reached through a static `RegistryPage` instance variable called `Current`.  

The static instance reference `RegistryPage.Current` is set in the constructor of `RegistryPage` and because our `RegistryReadAppService` is launched by `RegistryPage`, this static variable is guaranteed to exist before `App.OnBackgroundActivated` is called. Because this is a simple app with no view models and only two pages, one contained within a `Frame` of the other, these static references give us an easy way for the pages to communicate without complex dependency injection.

This same static instance technique is also implemented in `MainPage.xaml`, which owns the `Frame` that contains `RegistryPage`.  

```c#
        // a static reference to this MainPage object that allows downstream pages to get a handle
        // to the MainPage instance to call NotifyUser methods in this class.
        static public MainPage Current;

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(800, 600);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            // This is a static public reference that allows downstream pages to get a handle to the MainPage instance
            // in order to call methods that are in this class, like NotifyUser.
            Current = this;

            this.Loaded += MainPage_Loaded;

            NotifyUser("Welcome to the Sample!", NotifyType.StatusMessage);
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            shellFrame.Navigate(typeof(RegistryPage));
        }
```

`MainPage` is started as the `rootFrame` in `App.xaml.cs` in its `Application.OnLaunched` override.  `MainPage` looks like this:

  <figure>
  <img src="/images/SampleShellMainPage.png" alt="Main Page"/>
  <figcaption>RegistryUWP MainPage</figcaption>
</figure>

`MainPage` has a method we will call a number of times through our globally-scoped static `MainPage.Current` reference, `NotifyUser` sends messages to the `MainPage` status area and looks like this:

```c#
        #region NotifyUser code
        /// <summary>
        /// Display a message to the user in the MainPage Status area.
        /// This method may be called from any thread.
        /// </summary>
        /// <param name="strMessage">The string message to display.</param>
        /// <param name="type">NotifyType.StatusMessage or NotifyType.ErrorMessage</param>
        public void NotifyUser(string strMessage, NotifyType type)
        {
            // If called from the UI thread, then update immediately.
            // Otherwise, schedule a task on the UI thread to perform the update.
            if (Dispatcher.HasThreadAccess)
            {
                UpdateStatus(strMessage, type);
            }
            else
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateStatus(strMessage, type));
            }
        }
```

The `NotifyType` is an enumeration for the type of notification we want:

```c#
    /// <summary>
    /// The XAML will display a green background for a Status message, and a red background for an ErrorMessage,
    /// blue/violet for a warning message.  ClearMessage type clears the Status box and sets it to green.
    /// </summary>
    public enum NotifyType
    {
        StatusMessage,
        WarningMessage,
        ErrorMessage,
        ClearMessage
    };
```

You will see this code used many times throughout the sample to communicate status, warning, and error messages to the user.

When `MainPage` is loaded, the `Frame` declared in its xaml is directed to navigate to the page that does most of the work in our sample, `RegistryPage`, like this:

```c#
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            shellFrame.Navigate(typeof(RegistryPage));
        }
```

The `RegistryPage` constructor has a similar pattern.  It looks like this:

```c#
        public RegistryPage()
        {
            // set our static 
            Current = this;

            this.InitializeComponent();

            Loaded += RegistryPage_Loaded;

            // Call NotifyUser through the static MainPage.Current and say hello.
            //
            // For those unfamiliar with the C# null conditional operator, see
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-conditional-operators
            if(IsSMode)
            {
                MainPage.Current?.NotifyUser(string.Format("S-Mode is enabled! Processor mode: {0}", OSBitness), NotifyType.WarningMessage);
            }
            else
            {
                MainPage.Current?.NotifyUser(string.Format("S-Mode is disabled. Processor mode: {0}.", OSBitness ), NotifyType.StatusMessage);
            }
        }
```

`RegistryPage` has a `bool` property called `IsSMode` which indicates if the machine we're running on is [Windows 10 S-Mode](https://www.microsoft.com/en-us/windows/s-mode).  It's backing store variable is instantiated like this:

```c#
private bool isSMode = Windows.System.Profile.WindowsIntegrityPolicy.IsEnabled;
```

The `RegistryPage` is where we launch the fullTrust Win32 process, `RegistryReadAppService`.  This is done in its `Loaded` event handler, like this:

```c#
        /// <summary>
        /// Launch the full trust RegistryReadAppService Win32 process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RegistryPage_Loaded(object sender, RoutedEventArgs e)
        {
            // get Startup Programs from the Win32 application.  When launched, it will query the Registry
            // for the key value and send it back to us through our Connection_RequestReceived event handler.
            await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

        }
```
## Sequence of Events

The sequence of events in RegistryUWP:

*  `RegistryUWP` application is started by the user.

*  `App.OnLaunched()` activates `MainPage` in its `rootFrame`.

*  `MainPage` instantiates, stores a reference to itself in static `MainPage.Current`

*  `MainPage_Loaded()` executes and navigates to `RegistryPage` in its `shellFrame`

* `RegistryPage` instantiates, stores a ref to itself in static `RegistryPage.Current`

* `RegistryPage_Loaded()` executes and launches FullTrustApp defined in `RegistryPackaging` Package.appxmanifest

*  The FullTrust Win32 `RegistryReadAppService` starts and opens an [AppServiceConnection](https://docs.microsoft.com/en-us/uwp/api/Windows.ApplicationModel.AppService.AppServiceConnection) to `RegistryUWP`

*  In `RegistryUWP`, the `OnBackgroundActivated()` event handler in `App.xaml.cs` calls `RegistryPage.RegisterConnection()` through the static `RegistryPage.Current` reference to hook up the `App.Connection.RequestReceived` event to its `Connection_RequestReceived()` handler.

* `RegistryPage.RegisterConnection()` then calls `RegistryPage.GetStartupProgramNames()`

* `RegistryPage.GetStartupProgramNames()` creates a `ValueSet`, adds a "verb" or command, calls [AppServiceConnection.SendMessageAsync](https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.appservice.appserviceconnection.sendmessageasync) to send command to the FullTrustProcess and waits for its response.

* `RegistryReadAppService.Connection_RequestReceived` handler fires, reads command, opens the Registry key for the Startup Program names, reads the name values, puts them into a ValueSet, and returns an array of name strings to `RegistryUWP.RegistryPage.GetStartupProgramNames()` through an [AppServiceResponse](https://docs.microsoft.com/en-us/uwp/api/windows.applicationmodel.appservice.appserviceresponse)

* `RegistryPage.GetStartupProgramNames()` receives the `AppServiceResponse`, reads the program names from the ValueSet, and updates the user interface.

* When the user presses the "Add Notepad" (or "Remove Notepad") button on the user interface, the `RegistryButton_Click` handler fires and sends a command verb to the `RegistryReadAppService` to write to the Registry to add or remove `Notepad.exe` from the Startup Programs list.

* The `RegistryReadAppService.Connection_RequestReceived` handler fires, reads the command, and calls `RegistryReadAppService.LaunchElevatedRegistryWrite()` to launch an elevated process to write to the Registry.  Writing to the HKEY_LOCAL_MACHINE hive requires Administrator privilege.

* `LaunchElevatedRegistryWrite()` creates a [ProcessStartInfo](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo?view=netframework-4.7.2) object and populates it with the proxy path to the `ElevatedRegistryWrite.exe` application inside `RegistryPackaging` APPX package.  It then tries to start the process through the [Process.Start](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start?view=netframework-4.7.2) method.

* Windows then launches an elevation dialog to ask the user for Administrator credentials.  If denied or canceled by the user, Windows returns E_FAIL to `RegistryReadAppService.LaunchElevatedRegistryWrite()` and an `exitCode` of 1 will be returned to `RegistryUWP`.  If approved, Windows then launches `ElevatedRegistryWrite.exe` with Administrator privilege to write to the Registry.

* Win32 `ElevatedRegistryWrite.exe` is very simple.  It opens the Startup Programs key at `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run` and reads the values, which are the Startup Program key names.  It looks for "notepad" and if it finds this, it writes to the key to delete the value, otherwise it adds `Notepad.exe` to the list of key values.  If everything went well, it returns an exit code of 0 to `RegistryReadAppService.LaunchElevatedRegistryWrite()`.  If there is any exception, it returns an exit code of 2.  Recall, the exit code of 1 was if the Windows elevation dialog was canceled by the user.  

* `RegistryReadAppService.LaunchElevatedRegistryWrite()` then returns the exit code to the `Connection_RequestReceived()` event handler. The `ElevatedRegistryWrite` process ends and the exit code is returned to `RegistryUWP` through the response.  

* `RegistryUWP.RegistryButton_Click()` checks the response for the exit code. If it is 0, it notifies the user of success, and then calls `RegistryUWP.GetStartupProgramNames()` to update the UI with the newly-modified list of Startup Program names.  Otherwise, it notifies the user of any error through `MainPage.NotifyUser`.

* When the user closes `RegistryUWP` and the UWP application ends, that end of the `AppServiceConnection` is closed.  The `RegistryReadAppService` watches for this event and when it occurs, its `Connection_ServiceClosed()` event handler is fired and the `RegistryReadAppService` exits.

## RegistryReadAppService Project - Win32 application

The `RegistryReadAppService` is our Desktop Bridge app service and instead of being a standard UWP [IBackgroundTask](https://docs.microsoft.com/en-us/windows/uwp/launch-resume/support-your-app-with-background-tasks), it is a simple "headless" Win32 "Windows" application with no window.

Both `RegistryReadAppService` and `ElevatedRegistryWrite` began development as console applications, but when they became ready for integration into the RegistryUWP solution, their Output Types were changed to "Windows Application" in their project properties, which them makes them "headless" console apps with no console window.  

<figure>
  <img src="/images/RegistryReadAppService_windowsApp.PNG" alt="RegistryReadAppService Output Type"/>
  <figcaption>RegistryReadAppService Output Type</figcaption>
</figure>

Because the RegistryReadAppService needs Desktop Bridge AppServiceConnection support, you need to reference `Windows.winmd` from `C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.17763.0\Windows.winmd`, or whatever version of Windows after 1809 (build 17763) you happen to be using.

Also, you'll need to add `System.Runtime.WindowsRuntime` from `C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Runtime.WindowsRuntime.dll`.  Both of these references are needed for Desktop Bridge to work in a Win32 .Net application.

## ElevatedRegistryWrite Project - Win32 application

This is a "headless" Win32 console application with no console window, similar to the `RegistryReadAppService`, but it only launches as an elevated process to write to the Registry, and does not communicate with the other processes except by its exit code. It has no special requirements other than to be launched as elevated.

## More information

There is an excellent three-part series of articles by Stefan Wick that show how to build UWP with Desktop extensions.  These may be found here:

*  https://stefanwick.com/2018/04/06/uwp-with-desktop-extension-part-1/
* https://stefanwick.com/2018/04/06/uwp-with-desktop-extension-part-2/
* https://stefanwick.com/2018/04/16/uwp-with-desktop-extension-part-3/

## RegistryUWP on Windows Store

You may install an x64 version of `RegistryUWP` here:

https://www.microsoft.com/en-us/p/registryuwp/9pkjl872p092?rtc=1&activetab=pivot:overviewtab


