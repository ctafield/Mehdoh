using System;
using System.Linq;
using System.Threading;
using System.Windows;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ThirdPartyApi;
using Microsoft.Phone.Shell;
using Mitter.Helpers;

namespace Mitter
{

    public partial class SaveLater : AnimatedBasePage
    {
        public SaveLater()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(SaveLater_Loaded);

            AnimationContext = LayoutRoot;
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn)
                return new SlideUpAnimator() { RootElement = LayoutRoot };
            else if (animationType == AnimationType.NavigateBackwardOut)
                return new SlideDownAnimator() { RootElement = LayoutRoot };

            return null;
        }
        

        private string TweetId
        {
            get;
            set;
        }

        private string UrlToAdd
        {
            get;
            set;
        }

        private string Description
        {
            get;
            set;
        }

        void SaveLater_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public int RefreshingCount = 0;

        private bool IgnoreSwitches = true;

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            IgnoreSwitches = true;

            string temp;
            if (NavigationContext.QueryString.TryGetValue("url", out temp))
            {
                UrlToAdd = temp;
            }

            if (NavigationContext.QueryString.TryGetValue("desc", out temp))
            {
                Description = temp;
            }

            if (NavigationContext.QueryString.TryGetValue("id", out temp))
            {
                TweetId = temp;
            }

            using (var dh = new MainDataContext())
            {
                if (!dh.ReadLaterSettings.Any())
                {
                    var newSettings = new ReadLaterTable()
                    {
                        UseInstapaper = false,
                        UseReadItLater = false
                    };
                    dh.ReadLaterSettings.InsertOnSubmit(newSettings);
                    dh.SubmitChanges();

                    // Disable the toggles until sorted
                    switchInstapaper.IsChecked = false;
                    switchInstapaper.IsEnabled = false;
                    switchReadItLater.IsChecked = false;
                    switchReadItLater.IsEnabled = false;
                }
                else
                {

                    var settings = dh.ReadLaterSettings.First();
                    switchInstapaper.IsChecked = (!string.IsNullOrWhiteSpace(settings.InstapaperUsername) && settings.UseInstapaper);
                    switchReadItLater.IsChecked = (!string.IsNullOrWhiteSpace(settings.ReadItLaterUsername) && settings.UseReadItLater);

                    if (string.IsNullOrWhiteSpace(settings.InstapaperPassword) || string.IsNullOrWhiteSpace(settings.InstapaperUsername))
                        switchInstapaper.IsEnabled = false;
                    else
                        switchInstapaper.IsEnabled = true;

                    if (string.IsNullOrWhiteSpace(settings.ReadItLaterPassword) || string.IsNullOrWhiteSpace(settings.ReadItLaterUsername))
                        switchReadItLater.IsEnabled = false;
                    else
                        switchReadItLater.IsEnabled = true;

                }
            }

            IgnoreSwitches = false;

        }

        private void buttonInstapaper_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/SaveLaterConfig.xaml?service=instapaper", UriKind.Relative));
        }

        private void buttonReadItLater_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/SaveLaterConfig.xaml?service=pocket", UriKind.Relative));
        }

        private void switchInstapaper_Checked(object sender, RoutedEventArgs e)
        {
            if (IgnoreSwitches) return;

            using (var dh = new MainDataContext())
            {
                var res = dh.ReadLaterSettings.First();
                res.UseInstapaper = true;
                dh.SubmitChanges();
            }
        }

        private void switchInstapaper_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IgnoreSwitches) return;

            using (var dh = new MainDataContext())
            {
                var res = dh.ReadLaterSettings.First();
                res.UseInstapaper = false;
                dh.SubmitChanges();
            }
        }

        private void switchReadItLater_Checked(object sender, RoutedEventArgs e)
        {

            if (IgnoreSwitches) return;

            using (var dh = new MainDataContext())
            {
                var res = dh.ReadLaterSettings.First();
                res.UseReadItLater = true;
                dh.SubmitChanges();
            }

        }

        private void switchReadItLater_Unchecked(object sender, RoutedEventArgs e)
        {

            if (IgnoreSwitches) return;

            using (var dh = new MainDataContext())
            {
                var res = dh.ReadLaterSettings.First();
                res.UseReadItLater = false;
                dh.SubmitChanges();
            }

        }

        private bool UseInstapaper()
        {
            return (switchInstapaper.IsChecked.HasValue && switchInstapaper.IsChecked.Value);
        }

        private bool UseReadIt()
        {
            return (switchReadItLater.IsChecked.HasValue && switchReadItLater.IsChecked.Value);
        }

        private void mnuSave_Click(object sender, EventArgs e)
        {

            RefreshingCount = 0;

            UiHelper.SafeDispatch(() =>
            {
                SystemTray.ProgressIndicator = new ProgressIndicator
                {
                    IsVisible = true,
                    IsIndeterminate = true,
                    Text = "saving link for later"
                };
            });


            if (UseInstapaper())
                RefreshingCount++;

            if (UseReadIt())
                RefreshingCount++;

            if (RefreshingCount > 0)
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = false;

            using (var dh = new MainDataContext())
            {
                var res = dh.ReadLaterSettings.First();

                if (res.UseReadItLater)
                {
                    var readItLaterApi = new ReadItLaterApi();
                    readItLaterApi.AddUrlCompleted += new EventHandler<EventArgs>(readItLaterApi_AddUrlCompleted);
                    readItLaterApi.AddUrl(res.ReadItLaterUsername, res.ReadItLaterPassword, UrlToAdd, Description, TweetId);
                }

                if (res.UseInstapaper)
                {
                    var instaApi = new InstapaperApi();
                    instaApi.AddUrlCompleted += new EventHandler<EventArgs>(instaApi_AddUrlCompleted);
                    instaApi.AddUrl(res.InstapaperUsername, res.InstapaperPassword, UrlToAdd, Description, TweetId);
                }
            }

        }

        void readItLaterApi_AddUrlCompleted(object sender, EventArgs e)
        {
            RefreshingCount--;

            var api = sender as ReadItLaterApi;

            if (api.AddUrlSuccess)
            {
                CheckFinished();
            }
            else
            {
                UiHelper.HideProgressBar();
                UiHelper.ShowToast("failed to save to readitlater. check password.");
            }

        }

        void instaApi_AddUrlCompleted(object sender, EventArgs e)
        {
            RefreshingCount--;

            var api = sender as InstapaperApi;

            if (api.AddUrlSuccess)
                CheckFinished();
            else
            {
                UiHelper.HideProgressBar();
                UiHelper.ShowToast("failed to save to instapaper. check password.");
            }
        }

        private void CheckFinished()
        {
            if (RefreshingCount == 0)
            {
                UiHelper.SafeDispatch(() => ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IsEnabled = true);

                UiHelper.HideProgressBar();
                ThreadPool.QueueUserWorkItem(delegate
                {
                    Thread.Sleep(500);
                    UiHelper.ShowToast("link saved for later");
                });
                UiHelper.SafeDispatch(() => NavigationService.GoBack());
            }
        }
    }
}