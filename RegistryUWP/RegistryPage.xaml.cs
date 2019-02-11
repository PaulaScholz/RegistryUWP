using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.AppService;
using Windows.UI.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RegistryUWP
{
    /// <summary>
    /// We don't have a ViewModel in this simple example, rather, the page itself contains 
    /// the properties and does change notification.
    /// </summary>
    public partial class RegistryPage : Page, INotifyPropertyChanged
    {
        // A static reference to this RegistryPage instance so we can hook up the Connection handler.
        // Eliminates the need for dependency injection.  The handler is hooked up in App.xaml.cs, in
        // the OnBackgroundActivated handler, fired when the fullTrustProcess opens a connection to us.
        public static RegistryPage Current;

        private bool isSMode = Windows.System.Profile.WindowsIntegrityPolicy.IsEnabled;

        private ObservableCollection<string> startupProgramNames = new ObservableCollection<string>();
        public ObservableCollection<string> StartupProgramNames
        {
            get { return startupProgramNames; }
            set { Set(ref startupProgramNames, value); }
        }
        public bool IsSMode
        {
            get { return isSMode; }
            set { Set(ref isSMode, value); }
        }

        public bool NotSMode
        {
            get { return !isSMode; }
        }

        public string OSBitness
        {
            get
            {
                if (System.Environment.Is64BitProcess)
                {
                    return "x64";
                }
                else
                {
                    return "x86  Registry changes virtualized and do not persist outside process";
                }
            }
        }

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

        /// <summary>
        /// Launch the full trust WinSatInfo Win32 process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RegistryPage_Loaded(object sender, RoutedEventArgs e)
        {
            // get Startup Programs from the Win32 application.  When launched, it will query the Registry
            // for the key value and send it back to us through our Connection_RequestReceived event handler.
            await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Property setter for UI-bound values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="storage"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        protected void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }

            storage = value;
            RaisePropertyChanged(propertyName);
        }

        // command the fullTrustApplication to launch the ElevatedRegistryWrite app
        private async void RegistryButton_Click(object sender, RoutedEventArgs e)
        {
            // no need to open a connection, if we got this far we have one
            ValueSet valueSet = new ValueSet();

            // program that receives this valueset will switch on the value of the verb
            valueSet.Add("verb", "elevatedRegistryWrite");

            AppServiceResponse response = null;

            try
            {
                // send the command and wait for a response
                response = await App.Connection.SendMessageAsync(valueSet);

                // if the command is a success, get the new results
                if (response?.Status == AppServiceResponseStatus.Success)
                {
                    int exitCode = (int)response.Message["exitcode"];

                    // we're done with the new assessmet, refresh the interface by telling
                    // our WinSatInfo program to get the assessment, which will refresh the UI
                    if (0 == exitCode)
                    {
                        MainPage.Current?.NotifyUser("ElevatedRegistrtyWrite action success.", NotifyType.StatusMessage);

                        GetStartupProgramNames();
                    }
                    else if (1 == exitCode)
                    {
                        MainPage.Current?.NotifyUser("ElevatedRegistrtyWrite elevation cancelled by user", NotifyType.WarningMessage);
                    }                    
                    else
                    {
                        MainPage.Current?.NotifyUser(string.Format("ElevatedRegistrtyWrite error. Return code={0}", exitCode), NotifyType.ErrorMessage);
                    }
                }
                else
                {
                    MainPage.Current?.NotifyUser(string.Format("RegistryButton_click error AppServiceResponse: {0}", response?.Status.ToString()), NotifyType.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                MainPage.Current?.NotifyUser(string.Format("RegistryButton_click Exception. Message {0}", ex.Message.ToString()), NotifyType.ErrorMessage);
            }
        }

        /// <summary>
        /// Called by App.xaml.cs OnBackgroundActivated through static Current ref
        /// </summary>
        public void RegisterConnection()
        {
            if (App.Connection != null)
            {
                App.Connection.RequestReceived += Connection_RequestReceived;

                GetStartupProgramNames();
            }
        }

        /// <summary>
        /// This isn't called in this demo, but is the pattern if needed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var deferral = args.GetDeferral();

            ValueSet message = args.Request.Message;
            ValueSet returnData = new ValueSet();
            returnData.Add("response", "success");

            // get the verb or "command" for this request
            string verb = message["verb"] as String;

            switch (verb)
            {

            }

            try
            {
                // Return the data to the caller.
                await args.Request.SendResponseAsync(returnData);
            }
            catch (Exception e)
            {
                // Your exception handling code here.
            }
            finally
            {
                // Complete the deferral so that the platform knows that we're done responding to the app service call.
                // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
                deferral.Complete();
            }
        }

        /// <summary>
        /// Get the Startup program names from the RegistryReadAppService and put them in ObservableCollection
        /// bound to the ListBox in the UI.
        /// </summary>
        private async void GetStartupProgramNames()
        {
            ValueSet valueSet = new ValueSet();

            valueSet.Clear();
            valueSet.Add("verb", "getStartupProgramNames");

            try
            {
                AppServiceResponse response = await App.Connection.SendMessageAsync(valueSet);

                if (response.Status == AppServiceResponseStatus.Success)
                {
                    ValueSet test = response.Message;

                    int x = response.Message.Count;
                    var a = response.Message.Keys;

                    // Get the data  that the service sent to us.
                    if (response.Message["verb"] as string == "RegistryReadResult")
                    {
                        // Update UI-bound collections and controls on the UI thread
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            StartupProgramNames.Clear();

                            string[] newNames = (string[])response.Message["StartupProgramNames"];

                            StartupProgramNames = new ObservableCollection<string>(newNames);

                            // scroll to bottom of the list
                            StartupProgramsListView.SelectedIndex = startupProgramNames.Count - 1;
                            StartupProgramsListView.ScrollIntoView(StartupProgramsListView.SelectedItem);

                            // Adjust the label of the RegistryButton.  The action of
                            // removing it actually takes place in ElevatedRegistryWrite.exe
                            // which decides on its own indepenently which action to take
                            // based on whether "notepad" is in the Registry's list.
                            if (StartupProgramNames.Contains("notepad"))
                                {
                                    RegistryButton.Content = "Remove Notepad";
                                }
                                else
                                {
                                    RegistryButton.Content = "Add Notepad";
                                }
                        });
                    }
                    else if (response.Message["verb"] as string == "RegistryReadError")
                    {
                        string exceptionMessage = response.Message["exceptionMessage"] as string;
                        MainPage.Current?.NotifyUser(string.Format("RegistryReadError, Exception: {0}", exceptionMessage), NotifyType.ErrorMessage);
                    }
                    else
                    {
                        MainPage.Current?.NotifyUser("RegistryReadError, unknown type.", NotifyType.ErrorMessage);
                    }
                }
                else
                {
                    MainPage.Current?.NotifyUser(string.Format("GetStartupProgramNames error AppServiceResponse: {0}", response?.Status.ToString()), NotifyType.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                MainPage.Current?.NotifyUser(string.Format("GetStartupProgramNames Exception. Message {0}", ex.Message.ToString()), NotifyType.ErrorMessage);
            }  
           
        }
    }
}
