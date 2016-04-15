using System.Windows;
using System.Windows.Navigation;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.POCO;
using FieldOfTweets.Common.UI.Animations.Page;
using Microsoft.Phone.Controls;

namespace Mitter.UI.Settings
{
    public partial class Settings_Startup : AnimatedBasePage
    {
        private bool _ignoreDisplayModeEvents;
        private bool _allowedToSaveToggles;

        public Settings_Startup()
        {
            _allowedToSaveToggles = false;
            _ignoreDisplayModeEvents = true;

            Loaded += new RoutedEventHandler(Settings_Startup_Loaded);
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        void Settings_Startup_Loaded(object sender, RoutedEventArgs e)
        {
            _ignoreDisplayModeEvents = false;
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
            lstRefresh.DataContext = ColumnHelper.ColumnConfig;
        }

        private void lstRefresh_ToggleChecked(object sender, RoutedEventArgs e)
        {

            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                var dataItem = toggleSwitch.DataContext as ColumnModel;

                if (dataItem != null)
                {
                    if (toggleSwitch.IsChecked.HasValue)
                        dataItem.RefreshOnStartup = toggleSwitch.IsChecked.Value;
                    else
                        dataItem.RefreshOnStartup = false;

                    ColumnHelper.UpdateColumn(dataItem);
                }
            }

        }


    }
}