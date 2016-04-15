using System.Windows;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.UI.Animations.Page;

namespace Mitter.UI.Settings
{

    public partial class Settings_Synchronise : AnimatedBasePage
    {

        private bool _ignoreCheckBoxEvents;
        private bool _allowedToSaveToggles;


        public Settings_Synchronise()
        {
            _allowedToSaveToggles = false;
            _ignoreCheckBoxEvents = true;

            Loaded +=new RoutedEventHandler(Settings_Synchronise_Loaded);

            InitializeComponent();
            AnimationContext = LayoutRoot;
        }

        private void Settings_Synchronise_Loaded(object sender, RoutedEventArgs e)
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
            toggleTweetMarker.IsChecked = settingsHelper.GetUseTweetMarker();
        }

        private void toggleTweetMarker_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetUseTweetMarker(toggleTweetMarker.IsChecked.HasValue && toggleTweetMarker.IsChecked.Value);

        }
    }

}