using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Settings;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.Interfaces;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Scheduler;

namespace Mitter.UI.Settings
{
    public partial class Settings_Background : AnimatedBasePage
    {

        private const string PeriodicTaskName = "FieldOfTweetsAgent";

        private bool _ignoreCheckBoxEvents;
        private bool _ignoreDisplayModeEvents;
        private bool _allowedToSaveToggles;

        private PeriodicTask _periodicTask;

        public Settings_Background()
        {
            _allowedToSaveToggles = false;
            _ignoreCheckBoxEvents = true;
            _ignoreDisplayModeEvents = true;

            Loaded += new RoutedEventHandler(Settings_Background_Loaded);
            InitializeComponent();

            AnimationContext = LayoutRoot;         
        }

        void Settings_Background_Loaded(object sender, RoutedEventArgs e)
        {
            _ignoreDisplayModeEvents = false;
            _ignoreCheckBoxEvents = false;
            _allowedToSaveToggles = true;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {

            if (e.Uri == ExternalUri)
            {
                DontAnimatedOut = false;
            }
            else
            {
                DontAnimatedOut = true;
            }

            base.OnNavigatingFrom(e);

        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            if (e.NavigationMode == NavigationMode.New)
            {
                LoadSettings();
            }

            base.OnNavigatedTo(e);

        }

        private void LoadSettings()
        {

            scrollOptions.Visibility = Visibility.Visible;

            var settingsHelper = new SettingsHelper(true);
            var allSettings = settingsHelper.GetSettingsBackground();

            _periodicTask = ScheduledActionService.Find(PeriodicTaskName) as PeriodicTask;

            switchBackground.IsChecked = _periodicTask != null && _periodicTask.IsEnabled;

            ShowHideBackgroundOptions(switchBackground);

            switchWifi.IsChecked = allSettings.OnlyUpdateOnWifi; // settingsHelper.GetOnlyUpdateOnWifi();
            switchToast.IsChecked = allSettings.EnableToast; // settingsHelper.GetToastEnabled();

            GetSleepSettings(allSettings);

            GetBackgroundLastStarted();
            GetBackgroundLastRun();

        }

        private void sleepToggle_Checked(object sender, RoutedEventArgs e)
        {

            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var toggle = sender as ToggleSwitch;
            if (toggle.IsChecked.HasValue && toggle.IsChecked.Value)
            {
                stackSleep.Visibility = Visibility.Visible;
            }
            else
            {
                stackSleep.Visibility = Visibility.Collapsed;
            }

            var sh = new SettingsHelper();
            sh.SetSleepEnabled(toggle.IsChecked.HasValue && toggle.IsChecked.Value);

        }

        private void sleepFrom_ValueChanged(object sender, DateTimeValueChangedEventArgs e)
        {

            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var timePicker = sender as TimePicker;
            var sh = new SettingsHelper();
            var value = timePicker.Value.Value;
            sh.SetSleepFrom(value);

        }

        private void sleepTo_ValueChanged(object sender, DateTimeValueChangedEventArgs e)
        {

            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var timePicker = sender as TimePicker;
            var sh = new SettingsHelper();
            var value = timePicker.Value.Value;
            sh.SetSleepTo(value);

        }

        private void switchWifi_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetOnlyUpdateOnWifi(switchWifi.IsChecked.HasValue && switchWifi.IsChecked.Value);

        }

        private void switchToast_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetToastEnabled(switchToast.IsChecked.HasValue && switchToast.IsChecked.Value);

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private void GetBackgroundLastRun()
        {

            try
            {

                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!myStore.FileExists(ApplicationConstants.BackgroundTaskLastRun))
                    {
                        txtBgLastRun.Text = string.Empty;
                        txtBgLastRun.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        using (var file = myStore.OpenFile(ApplicationConstants.BackgroundTaskLastRun, FileMode.Open))
                        {
                            using (var stream = new StreamReader(file))
                            {
                                var lastRun = stream.ReadLine();
                                txtBgLastRun.Text = "B/G task last completed:" + lastRun;
                                txtBgLastRun.Visibility = Visibility.Visible;
                            }
                        }
                    }

                }

            }
            catch (Exception)
            {
                txtBgLastRun.Text = string.Empty;
                txtBgLastRun.Visibility = Visibility.Collapsed;
            }

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private void GetBackgroundLastStarted()
        {

            try
            {

                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!myStore.FileExists(ApplicationConstants.BackgroundTaskLastStarted))
                    {
                        txtBgLastStarted.Text = string.Empty;
                        txtBgLastStarted.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        using (var file = myStore.OpenFile(ApplicationConstants.BackgroundTaskLastStarted, FileMode.Open))
                        {
                            using (var stream = new StreamReader(file))
                            {
                                var lastRun = stream.ReadLine();
                                txtBgLastStarted.Text = "B/G task last started: " + lastRun;
                                txtBgLastStarted.Visibility = Visibility.Visible;
                            }
                        }
                    }

                }

            }
            catch (Exception)
            {
                txtBgLastRun.Text = string.Empty;
                txtBgLastRun.Visibility = Visibility.Collapsed;
            }

        }


        private void GetSleepSettings(BackgroundSettings allSettings)
        {

            if (allSettings.SleepEnabled)
            {
                sleepToggle.IsChecked = true;
                stackSleep.Visibility = Visibility.Visible;
            }
            else
            {
                sleepToggle.IsChecked = false;
                stackSleep.Visibility = Visibility.Collapsed;
            }

            sleepFrom.Value = allSettings.SleepFrom;
            sleepTo.Value = allSettings.SleepTo;

        }


        private async void switchBackground_Checked(object sender, RoutedEventArgs e)
        {

            ShowHideBackgroundOptions(sender);

            if (!_ignoreCheckBoxEvents && _allowedToSaveToggles)
                if (!await StartPeriodicAgent())
                {
                    switchBackground.IsChecked = false;
                }
        }

        private void switchBackground_Unchecked(object sender, RoutedEventArgs e)
        {

            ShowHideBackgroundOptions(sender);

            if (!_ignoreCheckBoxEvents && _allowedToSaveToggles)
                StopPeriodicAgent();
        }

        private void ShowHideBackgroundOptions(object sender)
        {

            var toggle = sender as ToggleSwitch;

            if (toggle == null)
                return;

            if (toggle.IsChecked.HasValue && toggle.IsChecked.Value)
            {
                backgroundStack2.Visibility = Visibility.Visible;
                backgroundStack3.Visibility = Visibility.Visible;
                backgroundStack4.Visibility = Visibility.Visible;
            }
            else
            {
                backgroundStack2.Visibility = Visibility.Collapsed;
                backgroundStack3.Visibility = Visibility.Collapsed;
                backgroundStack4.Visibility = Visibility.Collapsed;
            }

        }

        private void StopPeriodicAgent()
        {
            var sh = new SettingsHelper();
            sh.SetBackgroundTaskEnabled(false);

            var bgHelper = new BackgroundHelper();
            bgHelper.RemoveTask();
        }

        private async Task<bool> StartPeriodicAgent()
        {
            var sh = new SettingsHelper();
            sh.SetBackgroundTaskEnabled(true);

            var bgHelper = new BackgroundHelper();

            // If the task already exists and the IsEnabled property is false, background
            // agents have been disabled by the user
            if (bgHelper.IsDisabled())
            {
                MessageBox.Show("Background agents for this application are currently disabled in the WP settings.", "Background agents", MessageBoxButton.OK);
                return false;
            }

            try
            {
                var startedTask = await bgHelper.StartTask();

                if (!startedTask)
                {
                    MessageBox.Show("Background agents for this application are currently disabled in the WP settings.", "Background agents", MessageBoxButton.OK);
                    return false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Background agents for this application are currently disabled in the WP settings.", "Background agents", MessageBoxButton.OK);
                return false;
            }


            // If debugging is enabled, use LaunchForTest to launch the agent in one minute.            

            //#if(DEBUG_AGENT)
            //ScheduledActionService.LaunchForTest(PeriodicTaskName, TimeSpan.FromSeconds(30));
            //#endif

            return true;
        }
    }
}