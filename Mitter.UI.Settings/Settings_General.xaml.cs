// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ImageHost;
using FieldOfTweets.Common.UI.Interfaces;
using Telerik.Windows.Controls;

namespace Mitter.UI.Settings
{
    public partial class Settings_General : AnimatedBasePage
    {
        #region Fields

        private readonly List<string> _availableImageHosts = new List<string>
                                                                {
                                                                    "yfrog",
                                                                    "twitpic",
                                                                    "imgly",
                                                                    //"twitvid",
                                                                    "twitter",
                                                                    "mobypicture",
                                                                    "onedrive"
                                                                };

        private bool _allowedToSaveToggles;

        private bool _ignoreCheckBoxEvents;
        private bool _ignoreDisplayModeEvents;

        #endregion

        #region Constructor

        public Settings_General()
        {
            _allowedToSaveToggles = false;
            _ignoreCheckBoxEvents = true;
            _ignoreDisplayModeEvents = true;

            Loaded += Settings_General_Loaded;

            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        #endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            
#if WP7
            // hide some things for WP7
            lstVideoPlayer.Visibility = Visibility.Collapsed;
#endif

            if (e.NavigationMode == NavigationMode.New)
            {
                LoadSettings();
            }

            base.OnNavigatedTo(e);
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

        private void Settings_General_Loaded(object sender, RoutedEventArgs e)
        {
            _ignoreDisplayModeEvents = false;
            _ignoreCheckBoxEvents = false;
            _allowedToSaveToggles = true;
        }


        private void LoadSettings()
        {
            var settingsHelper = new SettingsHelper(true);
            var generalSettings = settingsHelper.GetSettingsGeneral();

            lstRefreshMode.SelectedIndex = generalSettings.RefreshAllColumns ? 0 : 1; // settingsHelper.GetRefreshAllColumns() ? 0 : 1;

            // Bind the display mode
            lstNameDisplayMode.DataContext = new List<string>
                                                 {
                                                     "screen name",
                                                     "display name"
                                                 };

            var res = generalSettings.NameDisplayMode; // settingsHelper.GetNameDisplayMode();
            lstNameDisplayMode.SelectedItem = res.ToString().ToLower() == "screenname" ? "screen name" : "display name";

            // image host
            var host = generalSettings.ImageHost; // settingsHelper.GetImageHost();
            lstImageHost.ItemsSource = _availableImageHosts;
            try
            {
                switch (host)
                {
                    case ApplicationConstants.ImageHostEnum.yFrog:
                        lstImageHost.SelectedIndex = 0;
                        break;
                    case ApplicationConstants.ImageHostEnum.TwitPic:
                        lstImageHost.SelectedIndex = 1;
                        break;
                    case ApplicationConstants.ImageHostEnum.Imgly:
                        lstImageHost.SelectedIndex = 2;
                        break;
                    case ApplicationConstants.ImageHostEnum.TwitVid:
                        lstImageHost.SelectedIndex = 3; // fall back to twitter
                        break;
                    case ApplicationConstants.ImageHostEnum.Twitter:
                        lstImageHost.SelectedIndex = 3; 
                        break;
                    case ApplicationConstants.ImageHostEnum.MobyPicture:
                        lstImageHost.SelectedIndex = 4;
                        break;
                    case ApplicationConstants.ImageHostEnum.SkyDrive:
                        lstImageHost.SelectedIndex = 5;
                        break;
                    default:
                        lstImageHost.SelectedIndex = 3; // default back to twitter
                        break;
                }
            }
            catch (Exception)
            {
                lstImageHost.SelectedIndex = 3; // default back to twitter
            }

            var refreshCount = generalSettings.RefreshCount.ToString(CultureInfo.InvariantCulture); // settingsHelper.GetRefreshCount().ToString();

            foreach (var item in from item in lstRefreshCount.Items
                                 let thisItem = item as RadListPickerItem
                                 where thisItem != null && thisItem.Content.ToString() == refreshCount
                                 select item)
            {
                lstRefreshCount.SelectedItem = item;
                break;
            }

            var themeHelper = new ThemeHelper();
            var theme = (int)themeHelper.GetCurrentTheme();
            lstTheme.SelectedIndex = theme;

            // font size
            var fontSize = generalSettings.FontSize;

            foreach (var item in from item in lstFontSize.Items
                                 let thisItem = item as RadListPickerItem
                                 where thisItem != null && thisItem.Content.ToString() == fontSize
                                 select item)
            {
                lstFontSize.SelectedItem = item;
                break;
            }

            // orientation lock
            lstOrientationLock.SelectedIndex = (int)generalSettings.OrientationLock;

            // retweet style
            lstRetweetStyle.SelectedIndex = (int)generalSettings.RetweetStyle;

            toggleHeaderAvatar.IsChecked = generalSettings.ShowPivotHeaderAvatars;
            toggleHeaderCount.IsChecked = generalSettings.ShowPivotHeaderCounts;

            //toggleImageEdit.IsChecked = generalSettings.UseImageEditingTools;

            //toggleMaps.IsChecked = allSettings.DisplayMaps; // settingsHelper.GetDisplayMaps();
            toggleLinks.IsChecked = generalSettings.DisplayLinks; // settingsHelper.GetDisplayLinks();
            toggleTimelineImages.IsChecked = generalSettings.ShowTimelineImages;
            toggleReturnTimeline.IsChecked = generalSettings.ReturnToTimeline;

            // use metrotube
            lstVideoPlayer.SelectedIndex = (short)generalSettings.VideoPlayerApp;

        }

        private void lstNameDisplayMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            ApplicationConstants.NameDisplayModeEnum newVal = lstNameDisplayMode.SelectedItem.ToString() == "screen name"
                                                                  ? ApplicationConstants.NameDisplayModeEnum.ScreenName
                                                                  : ApplicationConstants.NameDisplayModeEnum.FullName;

            var sh = new SettingsHelper();
            sh.SetNameDisplayMode(newVal);
        }

        private IMehdohApp App
        {
            get { return ((IMehdohApp) Application.Current); }
        }

        private void lstFontSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var listPickerItem = lstFontSize.SelectedItem as RadListPickerItem;
            if (listPickerItem != null)
            {
                var newVal = listPickerItem.Content as string;

                var sh = new SettingsHelper();
                sh.SetFontSize(newVal);

                App.FontSize = newVal;
            }
        }

        private void lstRefreshCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            stackWarningRefreshCount.Visibility = Visibility.Visible;

            var newValue = lstRefreshCount.SelectedItem as RadListPickerItem;
            int newVal = int.Parse(newValue.Content.ToString());

            var sh = new SettingsHelper();
            sh.SetRefreshCount(newVal);
        }

        private void lstImageHost_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            ApplicationConstants.ImageHostEnum newVal;

            try
            {
                switch (lstImageHost.SelectedIndex)
                {
                    case 0:
                        newVal = ApplicationConstants.ImageHostEnum.yFrog;
                        break;
                    case 1:
                        newVal = ApplicationConstants.ImageHostEnum.TwitPic;
                        break;
                    case 2:
                        newVal = ApplicationConstants.ImageHostEnum.Imgly;
                        break;
                    //case 3:
                    //    newVal = ApplicationConstants.ImageHostEnum.TwitVid;
                    //    break;
                    case 3:
                        newVal = ApplicationConstants.ImageHostEnum.Twitter;
                        break;
                    case 4:
                        newVal = ApplicationConstants.ImageHostEnum.MobyPicture;
                        break;
                    case 5:
                        newVal = ApplicationConstants.ImageHostEnum.SkyDrive;
                        break;
                    default:
                        newVal = ApplicationConstants.ImageHostEnum.Twitter;
                        break;
                }
            }
            catch (Exception)
            {
                newVal = ApplicationConstants.ImageHostEnum.Twitter;
            }

            var sh = new SettingsHelper();
            sh.SetImageHost(newVal);

            ImageHostFactory.ClearHost();
        }

        private void lstRetweetStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();

            switch (lstRetweetStyle.SelectedIndex)
            {
                case 0:
                    sh.SetRetweetStyle(ApplicationConstants.RetweetStyleEnum.RT);
                    break;

                case 1:
                    sh.SetRetweetStyle(ApplicationConstants.RetweetStyleEnum.MT);
                    break;

                case 2:
                    sh.SetRetweetStyle(ApplicationConstants.RetweetStyleEnum.QuotesVia);
                    break;

                case 3:
                    sh.SetRetweetStyle(ApplicationConstants.RetweetStyleEnum.Quotes);
                    break;
            }
        }


        //private void toggleMaps_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
        //        return;

        //    var sh = new SettingsHelper();

        //    bool newValue = toggleMaps.IsChecked.HasValue && toggleMaps.IsChecked.Value;
        //    sh.SetDisplayMaps(newValue);

        //    // update the global value           
        //    App.DisplayLinks = newValue;
        //}

        private void toggleLinks_UnChecked(object sender, RoutedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            var sh = new SettingsHelper();
            sh.SetDisplayLinks(toggleLinks.IsChecked.HasValue && toggleLinks.IsChecked.Value);
        }

        private void toggleLinks_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreCheckBoxEvents && !_allowedToSaveToggles)
                return;

            var newValue = toggleLinks.IsChecked.HasValue && toggleLinks.IsChecked.Value;
            var sh = new SettingsHelper();
            sh.SetDisplayLinks(newValue);

            // update the global value
            App.DisplayLinks = newValue;
        }

        private void lstRefreshMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var value = lstRefreshMode.SelectedIndex == 0;

            var sh = new SettingsHelper();
            sh.SetRefreshAllColumns(value);
        }

        //private void toggleImageEdit_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
        //        return;

        //    var sh = new SettingsHelper();
        //    sh.SettUseImageEditingTools(toggleImageEdit.IsChecked.HasValue && toggleImageEdit.IsChecked.Value);
        //}

        private void lstOrientationLock_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            ApplicationConstants.OrientationLockEnum value = ApplicationConstants.OrientationLockEnum.Off;

            switch (lstOrientationLock.SelectedIndex)
            {
                case 0:
                    value = ApplicationConstants.OrientationLockEnum.Off;
                    break;
                case 1:
                    value = ApplicationConstants.OrientationLockEnum.Portrait;
                    break;
                case 2:
                    value = ApplicationConstants.OrientationLockEnum.Landscape;
                    break;
            }

            var sh = new SettingsHelper();
            sh.SetOrientationLock(value);

            App.ResetSupportedOrientations();

            // show the warning
            stackWarningOrientation.Visibility = Visibility.Visible;
        }

        private void lstTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var value = ThemeHelper.Theme.System;

            try
            {
                switch (lstTheme.SelectedIndex)
                {
                    case 0:
                        value = ThemeHelper.Theme.System;
                        break;
                    case 1:
                        value = ThemeHelper.Theme.Dark;
                        break;
                    case 2:
                        value = ThemeHelper.Theme.Light;
                        break;
                    case 3:
                        value = ThemeHelper.Theme.MehdohDark;
                        break;
                    case 4:
                        value = ThemeHelper.Theme.MehdohLight;
                        break;
                    case 5:
                        value = ThemeHelper.Theme.GenericModernDark;
                        break;
                    case 6:
                        value = ThemeHelper.Theme.GenericModernLight;
                        break;
                }
            }
            catch (Exception)
            {
            }

            var th = new ThemeHelper();
            th.SetCurrentTheme(value);

            stackWarningTheme.Visibility = Visibility.Visible;
        }

        private void toggleTimelineImages_Checked(object sender, RoutedEventArgs e)
        {
            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            stackWarningTimelineImages.Visibility = Visibility.Visible;

            var sh = new SettingsHelper();
            sh.SetShowTimelineImages(toggleTimelineImages.IsChecked.HasValue && toggleTimelineImages.IsChecked.Value);
        }

        private void lstVideoPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var value = ApplicationConstants.VideoPlayerAppEnum.Default;

            switch (lstVideoPlayer.SelectedIndex)
            {
                case 0:
                    value = ApplicationConstants.VideoPlayerAppEnum.Default;
                    break;
                case 1:
                    value = ApplicationConstants.VideoPlayerAppEnum.OfficialYoutube; // this is now anything
                    break;
            }

            var sh = new SettingsHelper();
            sh.SetVideoPlayerApp(value);        
        }

        private void ToggleHeaderCount_OnChecked(object sender, RoutedEventArgs e)
        {

            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var value = toggleHeaderCount.IsChecked.GetValueOrDefault(true);

            var sh = new SettingsHelper();
            sh.SetShowPivotHeaderCounts(value);

            ((IMehdohApp)(Application.Current)).RebindColumns();

        }

        private void ToggleHeaderAvatar_OnChecked(object sender, RoutedEventArgs e)
        {

            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var value = toggleHeaderAvatar.IsChecked.GetValueOrDefault(true);

            var sh = new SettingsHelper();
            sh.SetShowPivotHeaderAvatars(value);

            ((IMehdohApp)(Application.Current)).RebindColumns();

        }

        private void ToggleReturnTimeline_OnChecked(object sender, RoutedEventArgs e)
        {

            if (_ignoreDisplayModeEvents && !_allowedToSaveToggles)
                return;

            var value = toggleReturnTimeline.IsChecked.GetValueOrDefault(false);

            var sh = new SettingsHelper();
            sh.SetReturnToTimeline(value);

        }
    }

}