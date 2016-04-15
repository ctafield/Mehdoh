// *********************************************************************************************************
// <copyright file="SettingsNew.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.UI.Animations.Page;

namespace Mitter.UI.Settings
{
    public partial class SettingsNew : AnimatedBasePage
    {
        #region Constructor

        public SettingsNew()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;

#if WP7
            lockTile.Text = "live tile";
#elif WP81
            lockTile.Text = "lock screen";
#elif WP8
            lockTile.Text = "lock & tile";
#endif
        }

        #endregion

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            switch (animationType)
            {
                case AnimationType.NavigateForwardIn:
                    return new TurnstileForwardInAnimator { RootElement = LayoutRoot };

                case AnimationType.NavigateForwardOut:
                    return new TurnstileForwardOutAnimator { RootElement = LayoutRoot };

                case AnimationType.NavigateBackwardOut:
                    return new TurnstileBackwardOutAnimator { RootElement = LayoutRoot };

                case AnimationType.NavigateBackwardIn:
                    return new TurnstileBackwardInAnimator { RootElement = LayoutRoot };
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            txtVersion.Text = VersionInfo.FullVersion();

            base.OnNavigatedTo(e);
        }


        private void general_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_General.xaml", UriKind.Relative));
        }

        private void startup_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Startup.xaml", UriKind.Relative));
        }

        private void streaming_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Streaming.xaml", UriKind.Relative));
        }

        private void location_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Location.xaml", UriKind.Relative));
        }

        private void saveForLater_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_SaveForLater.xaml", UriKind.Relative));
        }

        private void tile_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_TileWP8.xaml", UriKind.Relative));
        }

        private void about_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_About.xaml", UriKind.Relative));
        }

        private void background_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Background.xaml", UriKind.Relative));
        }

        private void synchronise_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Synchronise.xaml", UriKind.Relative));
        }

        private void tips_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Tips.xaml", UriKind.Relative));
        }

        private void rateLimit_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Rate.xaml", UriKind.Relative));
        }

        private void dataClear_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_ClearCache.xaml", UriKind.Relative));
        }

        private void mutes_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Mutes.xaml", UriKind.Relative));
        }
    }
}