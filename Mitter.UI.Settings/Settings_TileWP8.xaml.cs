// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.Helpers;

namespace Mitter.UI.Settings
{
    public partial class Settings_TileWP8 : AnimatedBasePage
    {
        #region Fields

        private bool _allowedToSaveToggles;
        private bool _ignoreDisplayModeEvents;

        #endregion

        #region Constructor

        public Settings_TileWP8()
        {
            _allowedToSaveToggles = false;
            _ignoreDisplayModeEvents = true;

            Loaded += Settings_Tile_Loaded;
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        #endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                LoadSettings();
            }

            base.OnNavigatedTo(e);
        }

        private void Settings_Tile_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _ignoreDisplayModeEvents = false;
            _allowedToSaveToggles = true;
        }

        private void LoadSettings()
        {
            //var settingsHelper = new SettingsHelper();
            //var allSettings = settingsHelper.GetSettingsLiveTileSettings();

            //var backgroundColour = allSettings.TileBackgroundColour;
            //lstTileColour.SelectedIndex = (int) backgroundColour;
        }

        //private void lstTileColour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
        //        return;

        //    ShellHelper.TileBackgroundColourEnum style;

        //    switch (lstTileColour.SelectedIndex)
        //    {
        //        case 0:
        //            style = ShellHelper.TileBackgroundColourEnum.System;
        //            break;
        //        case 1:
        //            style = ShellHelper.TileBackgroundColourEnum.Mehdoh;
        //            break;
        //        default:
        //            style = ShellHelper.TileBackgroundColourEnum.System;
        //            break;
        //    }

        //    var sh = new SettingsHelper();
        //    sh.SetTileBackgroundColour(style);

        //    using (var dh = new MainDataContext())
        //    {
        //        // todo: check this 
        //        var firstProfileId = dh.Profiles.First(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter).Id;

        //        var shelly = new ShellHelper();
        //        shelly.ResetLiveTile(firstProfileId, false);
        //    }
        //}

        private async void btnLockSettings_Click(object sender, RoutedEventArgs e)
        {
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
        }

        private async void btnLockWallpaper_Click(object sender, RoutedEventArgs e)
        {
            var op = await Windows.Phone.System.UserProfile.LockScreenManager.RequestAccessAsync();

            // Only do further work if the access was granted.
            var isProvider = op == Windows.Phone.System.UserProfile.LockScreenRequestResult.Granted;

            if (isProvider)
            {
                btnLockWallpaper.IsEnabled = false;

                var lockScreenWallpaperHelper = new LockScreenWallpaperHelper();
                await lockScreenWallpaperHelper.CheckAndUpdateLockScreenWallpaper();

                UiHelper.ShowToast("lock wallpaper set to mehdoh! thank you!");

                btnLockWallpaper.IsEnabled = true;
            }
        }
    }
}