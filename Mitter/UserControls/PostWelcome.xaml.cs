using System;
using System.Windows;
using System.Windows.Controls;
using FieldOfTweets.Common;
using FieldOfTweets.Common.UI.ImageHost;
using FieldOfTweets.Common.UI.Interfaces;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;
using Telerik.Windows.Controls;

namespace Mitter.UserControls
{
    public partial class PostWelcome : UserControl
    {

        public PostWelcome()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(PostWelcome_Loaded);
        }

        void PostWelcome_Loaded(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Height = Application.Current.Host.Content.ActualHeight;
        }

        public event EventHandler LinkClickEvent;

        private void lnkContinue_Click(object sender, RoutedEventArgs e)
        {
            LinkClickEvent(this, null);
        }

        private void lstImageHost_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

            var list = sender as RadListPicker;

            ApplicationConstants.ImageHostEnum newVal;

            switch (list.SelectedIndex)
            {
                case 0:
                    newVal = ApplicationConstants.ImageHostEnum.Twitter;
                    break;
                case 1:
                    newVal = ApplicationConstants.ImageHostEnum.SkyDrive;
                    break;
                case 2:
                    newVal = ApplicationConstants.ImageHostEnum.yFrog;
                    break;
                case 3:
                    newVal = ApplicationConstants.ImageHostEnum.TwitPic;
                    break;
                default:
                    newVal = ApplicationConstants.ImageHostEnum.Twitter;
                    break;

            }

            var sh = new SettingsHelper();
            sh.SetImageHost(newVal);

            ImageHostFactory.ClearHost();
        }


        private void switchBackground_Checked(object sender, RoutedEventArgs e)
        {

            var tog = sender as ToggleSwitch;
            tog.Content = "Yes";

            StartPeriodicAgent();

        }

        private void switchBackground_Unchecked(object sender, RoutedEventArgs e)
        {
            var tog = sender as ToggleSwitch;
            tog.Content = "No";

            RemoveAgent(PeriodicTaskName);
        }

        private void RemoveAgent(string name)
        {
            try
            {
                ScheduledActionService.Remove(name);
            }
            catch (Exception)
            {
            }
        }

        PeriodicTask _periodicTask;
        private const string PeriodicTaskName = "FieldOfTweetsAgent";

        private void StartPeriodicAgent()
        {

            try
            {
                _periodicTask = ScheduledActionService.Find(PeriodicTaskName) as PeriodicTask;

                // If the task already exists and the IsEnabled property is false, background
                // agents have been disabled by the user
                if (_periodicTask != null && !_periodicTask.IsEnabled)
                {
                    MessageBox.Show("Background agents for this application are currently disabled in the WP7 settings.", "Background agents", MessageBoxButton.OK);
                    return;
                }

                // If the task already exists and background agents are enabled for the
                // application, you must remove the task and then add it again to update 
                // the schedule
                if (_periodicTask != null && _periodicTask.IsEnabled)
                {
                    RemoveAgent(PeriodicTaskName);
                }

                // The description is required for periodic agents. This is the string that the user
                // will see in the background services Settings page on the device.
                _periodicTask = new PeriodicTask(PeriodicTaskName)
                {
                    Description = "Mehdoh update task. Used to update mentions and direct messages, send notifications and update the live tile in the background."
                };

                ScheduledActionService.Add(_periodicTask);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


        }



    }
}
