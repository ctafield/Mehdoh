using System;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;

namespace Mitter.UI.Settings
{
    public partial class Settings_SaveForLater : AnimatedBasePage
    {

        private bool _ignoreCheckBoxEvents;
        private bool _allowedToSaveToggles;

        public Settings_SaveForLater()
        {
            _allowedToSaveToggles = false;
            _ignoreCheckBoxEvents = true;

            Loaded += new RoutedEventHandler(Settings_SaveForLater_Loaded);
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        void Settings_SaveForLater_Loaded(object sender, RoutedEventArgs e)
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

            var settingsHelper = new SettingsHelper(true);
            
            toggleShowSave.IsChecked = settingsHelper.GetShowSaveLinksEnabled();

            var slh = new SaveLaterHelper();
            var saveSettings = slh.GetSettings();
            switchReadItLater.IsChecked = saveSettings.ReadItLaterEnabled;
            switchInstapaper.IsChecked = saveSettings.InstapaperEnabled;

        }

        private void toggleShowSave_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetShowSaveLinksEnabled(toggleShowSave.IsChecked.HasValue && toggleShowSave.IsChecked.Value);
        }

        private void switchInstapaper_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            using (var dh = new MainDataContext())
            {
                var res = dh.ReadLaterSettings.First();
                res.UseInstapaper = true;
                dh.SubmitChanges();
            }

        }

        private void switchInstapaper_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            using (var dh = new MainDataContext())
            {
                var res = dh.ReadLaterSettings.First();
                res.UseInstapaper = false;
                dh.SubmitChanges();
            }
        }

        private void switchReadItLater_Checked(object sender, RoutedEventArgs e)
        {

            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            using (var dh = new MainDataContext())
            {
                var res = dh.ReadLaterSettings.First();
                res.UseReadItLater = true;
                dh.SubmitChanges();
            }

        }

        private void switchReadItLater_Unchecked(object sender, RoutedEventArgs e)
        {

            if (_ignoreCheckBoxEvents) return;

            using (var dh = new MainDataContext())
            {
                var res = dh.ReadLaterSettings.First();
                res.UseReadItLater = false;
                dh.SubmitChanges();
            }

        }

        private void buttonInstapaper_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/SaveLaterConfig.xaml?service=instapaper", UriKind.Relative));
        }

        private void buttonReadItLater_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/SaveLaterConfig.xaml?service=pocket", UriKind.Relative));
        }

    }
}