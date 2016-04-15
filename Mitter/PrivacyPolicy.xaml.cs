using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using FieldOfTweets.Common.UI.Animations.Page;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Mitter
{
    public partial class PrivacyPolicy : AnimatedBasePage
    {
        public PrivacyPolicy()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            this.Loaded += new RoutedEventHandler(PrivacyPolicy_Loaded);
        }

        void PrivacyPolicy_Loaded(object sender, RoutedEventArgs e)
        {
            webBrowser.Navigate(new Uri("http://www.mehdoh.com/privacy.htm", UriKind.Absolute));
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn)
                return new SlideUpAnimator() { RootElement = LayoutRoot };
            else if (animationType == AnimationType.NavigateBackwardOut)
                return new SlideDownAnimator() { RootElement = LayoutRoot };

            return null;
        }

        private void webBrowser_Navigating(object sender, NavigatingEventArgs e)
        {
            SystemTray.ProgressIndicator = new ProgressIndicator
            {
                IsVisible = true,
                IsIndeterminate = true,
                Text = "retrieving privacy policy"
            };

        }

        private void webBrowser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            SystemTray.ProgressIndicator = new ProgressIndicator
            {
                IsVisible = false,
                IsIndeterminate = false
            };
        }

    }
}