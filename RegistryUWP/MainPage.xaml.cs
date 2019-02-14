//***********************************************************************
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//**********************************************************************​
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Windows.UI.Xaml.Media;
using Windows.UI.ViewManagement;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RegistryUWP
{
    /// <summary>
    /// An empty page that contains the sample code page in a frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
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

        /// <summary>
        /// Display a NotifyUserEventMessage to the user. Use this method from Publish/Subscribe
        /// messaging systems like Prism's EventAggregator where a NotifyUserEventMessage is the payload.
        /// </summary>
        /// <param name="message"></param>
        public void NotifyUser(NotifyUserEventMessage message)
        {
            NotifyUser(message.MessagePayload, message.MessageType);
        }

        /// <summary>
        /// Update the Status block. 
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        private void UpdateStatus(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                case NotifyType.ClearMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.WarningMessage:
                    // Yellow would wash out the text, but BlueViolet works fine here.
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.BlueViolet);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }

            if (type == NotifyType.ClearMessage)
            {
                // don't send String.Empty or it will make the status area disappear
                StatusBlock.Text = "  ";
            }
            else
            {
                StatusBlock.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
                StatusBlock.Text = strMessage;
            }

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

    }
}
