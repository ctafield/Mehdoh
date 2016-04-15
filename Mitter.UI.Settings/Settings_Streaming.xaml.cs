using System.Windows;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.Interfaces;

namespace Mitter.UI.Settings
{
    public partial class Settings_Streaming : AnimatedBasePage
    {
        private bool _ignoreDisplayModeEvents;
        private bool _allowedToSaveToggles;


        public Settings_Streaming()
        {
            _allowedToSaveToggles = false;
            _ignoreDisplayModeEvents = true;

            Loaded += new RoutedEventHandler(Settings_Streaming_Loaded);
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        void Settings_Streaming_Loaded(object sender, RoutedEventArgs e)
        {
            _ignoreDisplayModeEvents = false;
            _allowedToSaveToggles = true;            
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            
            if (e.NavigationMode == NavigationMode.New)
            {
                LoadSettings();
                ((IMehdohApp)(Application.Current)).ViewModel.HasBeenToStreamingSettingsPage = true;
            }

            base.OnNavigatedTo(e);
        
        }

        private void LoadSettings()
        {

            var settingsHelper = new SettingsHelper();
            var settings = settingsHelper.GetSettingsStreaming();

            toggleStreamingEnabled.IsChecked = settings.StreamingEnabled;
            toggleAutoScrollEnabled.IsChecked = settings.AutoScrollEnabled;
            toggleKeepScreenOn.IsChecked = settings.StreamingKeepScreenOn;
            toggleVibrate.IsChecked = settings.StreamingVibrate;
            toggleSound.IsChecked = settings.StreamingSound;
            toggleEnabledOnMobile.IsChecked = settings.StreamingOnMobile;

            SetOptionsVisiblity();
        }

        private void toggleStreamingEnabled_Checked(object sender, RoutedEventArgs e)
        {

            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            SetOptionsVisiblity();

            var newValue = toggleStreamingEnabled.IsChecked.HasValue && toggleStreamingEnabled.IsChecked.Value;

            var sh = new SettingsHelper();
            sh.SetStreamingEnabled(newValue);

            // switch it off / on
            if (!newValue)
                ((IMehdohApp)(Application.Current)).SuspendStreaming();
            else
                ((IMehdohApp)(Application.Current)).RebindColumns();

        }

        private void SetOptionsVisiblity()
        {
            var newValue = toggleStreamingEnabled.IsChecked.HasValue && toggleStreamingEnabled.IsChecked.Value;
            stackOptions.Visibility = (newValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void toggleAutoScrollEnabled_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetAutoScrollEnabled(toggleAutoScrollEnabled.IsChecked.HasValue && toggleAutoScrollEnabled.IsChecked.Value);
        }

        private void toggleKeepScreenOn_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetStreamingKeepScreenOn(toggleKeepScreenOn.IsChecked.HasValue && toggleKeepScreenOn.IsChecked.Value);

        }

        private void toggleVibrate_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetStreamingVibrate(toggleVibrate.IsChecked.HasValue && toggleVibrate.IsChecked.Value);           
        }

        private void toggleSound_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetStreamingSound(toggleSound.IsChecked.HasValue && toggleSound.IsChecked.Value);                       
        }

        private void toggleEnabledOnMobile_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetStreamingOnMobile(toggleEnabledOnMobile.IsChecked.HasValue && toggleEnabledOnMobile.IsChecked.Value);                                         
        }
    }
}