using System;
using System.Windows;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.UI.Animations.Page;

namespace Mitter.UI.Settings
{

    public partial class Settings_Location : AnimatedBasePage
    {
        
        private bool _ignoreCheckBoxEvents;
        private bool _allowedToSaveToggles;

        public Settings_Location()
        {
            _allowedToSaveToggles = false;
            _ignoreCheckBoxEvents = true;

            Loaded += new RoutedEventHandler(Settings_Location_Loaded);
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        void Settings_Location_Loaded(object sender, RoutedEventArgs e)
        {
            _ignoreCheckBoxEvents = false;
            _allowedToSaveToggles = true;
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
            var settingsHelper = new SettingsHelper();
            var settings = settingsHelper.GetSettingsLocation();

            toggleLocation.IsChecked = settings.LocationServicesEnabled; // settingsHelper.GetLocationServicesEnabled();
            toggleGeoTag.IsChecked = settings.AlwaysGeoTag;
        }

        private void toggleLocation_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetLocationServicesEnabled(toggleLocation.IsChecked.HasValue && toggleLocation.IsChecked.Value);
        }

        private void buttonPrivacy_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/PrivacyPolicy.xaml", UriKind.Relative));
        }

        private void toggleGeoTag_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetAlwaysGeoTag(toggleGeoTag.IsChecked.HasValue && toggleGeoTag.IsChecked.Value);

        }


    }
}