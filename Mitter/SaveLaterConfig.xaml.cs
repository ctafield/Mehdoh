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
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ThirdPartyApi;
using Microsoft.Phone.Controls;
using Mitter.Helpers;

namespace Mitter
{

    public partial class SaveLaterConfig : AnimatedBasePage
    {

        public SaveLaterConfig()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;
        }


        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn)
                return new SlideUpAnimator() { RootElement = LayoutRoot };
            else
                return new SlideDownAnimator() { RootElement = LayoutRoot };
        }

        private string Service { get; set; }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string temp;
            if (NavigationContext.QueryString.ContainsKey("service"))
            {
                if (NavigationContext.QueryString.TryGetValue("service", out temp))
                {
                    Service = HttpUtility.UrlDecode(temp);
                }
            }

            if (string.IsNullOrEmpty(Service))
            {
                NavigationService.GoBack();
            }
            else
            {

                if (Service.ToLower() == "instapaper")
                {
                    using (var dh = new MainDataContext())
                    {
                        var res = dh.ReadLaterSettings.First();
                        txtUsername.Text = res.InstapaperUsername ?? "";
                        txtPassword.Password = res.InstapaperPassword ?? "";
                    }
                }
                else
                {
                    using (var dh = new MainDataContext())
                    {
                        var res = dh.ReadLaterSettings.First();
                        txtUsername.Text = res.ReadItLaterUsername ?? "";
                        txtPassword.Password = res.ReadItLaterPassword ?? "";
                    }
                }

            }

            serviceTitle.Text = Service;

        }


        private void mnuSave_Click(object sender, EventArgs e)
        {
            SaveAndReturn();
        }

        private void mnuRemove_Click(object sender, EventArgs e)
        {

            if (MessageBox.Show("are you sure you want to remove the settings for this account?", "remove " + Service, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {

                txtPassword.Password = string.Empty;
                txtUsername.Text = string.Empty;

                SaveAndReturn();
            }
        }

        private void SaveAndReturn()
        {

            if (Service.ToLower() == "instapaper")
            {
                using (var dh = new MainDataContext())
                {
                    var res = dh.ReadLaterSettings.First();
                    res.InstapaperUsername = txtUsername.Text;
                    res.InstapaperPassword = txtPassword.Password;
                    dh.SubmitChanges();
                }
            }
            else
            {
                using (var dh = new MainDataContext())
                {
                    var res = dh.ReadLaterSettings.First();
                    res.ReadItLaterUsername = txtUsername.Text;
                    res.ReadItLaterPassword = txtPassword.Password;
                    dh.SubmitChanges();
                }
            }

            UiHelper.SafeDispatch(() => NavigationService.GoBack());

        }

        private void buttonVerify_Click(object sender, RoutedEventArgs e)
        {

            if (Service.ToLower() == "instapaper")
            {
                var api = new InstapaperApi();
                api.AuthenticateCompleted += new EventHandler<EventArgs>(api_AuthenticateCompleted);
                api.Authenticate(txtUsername.Text, txtPassword.Password);
            }
            else
            {
                var api = new ReadItLaterApi();
                api.AuthenticateCompleted += new EventHandler<EventArgs>(api_AuthenticateCompleted);
                api.Authenticate(txtUsername.Text, txtPassword.Password);
            }

        }

        void api_AuthenticateCompleted(object sender, EventArgs e)
        {

            bool success = false;

            if (sender is InstapaperApi)
            {
                var api = sender as InstapaperApi;
                success = api.AuthenticateSuccess;
            }

            if (sender is ReadItLaterApi)
            {
                var api = sender as ReadItLaterApi;
                success = api.AuthenticateSuccess;
            }

            UiHelper.SafeDispatch(() =>
            {
                if (success)
                {
                    txtCorrect.Visibility = Visibility.Visible;
                    txtWrong.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtCorrect.Visibility = Visibility.Collapsed;
                    txtWrong.Visibility = Visibility.Visible;
                }
            });

        }

    }

}