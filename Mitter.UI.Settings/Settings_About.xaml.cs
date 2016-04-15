using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.UserControls;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Tasks;
using DialogService = Clarity.Phone.Extensions.DialogService;

namespace Mitter.UI.Settings
{
    public partial class Settings_About : AnimatedBasePage
    {

        public Settings_About()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;
        }


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            if (e.NavigationMode == NavigationMode.New || e.NavigationMode == NavigationMode.Back)
            {
                SetValues();
            }

            base.OnNavigatedTo(e);

        }

        private void SetValues()
        {

            imageWelcome.Source = UiHelper.GetCurrentTheme() == ThemeEnum.Light ?
                        new BitmapImage(new Uri("/WelcomeBackgroundLight.png", UriKind.Relative)) :
                        new BitmapImage(new Uri("/WelcomeBackgroundDark.png", UriKind.Relative));

            txtVersion.Text = VersionInfo.FullVersion().ToLower();

#if MEHDOH_FREE

            btnRateReview.Content = "Buy the full version";

#else

            if (LicenceInfo.IsTrial())
            {
                btnRateReview.Content = "Upgrade to full version";
            }
#endif

        }

        private List<AccountFriendViewModel> GetAllTwitterAccounts()
        {

            var list = new List<AccountFriendViewModel>();

            using (var dh = new MainDataContext())
            {
                if (dh.Profiles.Any(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                {
                    foreach (var account in dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                    {
                        list.Add(new AccountFriendViewModel()
                                     {
                                         Id = account.Id,
                                         ScreenName = account.ScreenName,
                                         StatusChecked = true,
                                         IsFriend = true
                                     });
                    }
                }
            }

            return list;

        }

        private void ShowAccountSelect()
        {

            var selectFollowAccount = new SelectTwitterAccount();


            selectFollowAccount.ExistingValues = GetAllTwitterAccounts();

            selectFollowAccount.CheckPressed += new EventHandler(selectFollowAccount_CheckPressed);

            SelectAccountPopup = new Clarity.Phone.Extensions.DialogService()
            {
                AnimationType = Clarity.Phone.Extensions.DialogService.AnimationTypes.Slide,
                Child = selectFollowAccount
            };

            try
            {
                SelectAccountPopup.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void HideSelectAccountPopup()
        {
            UiHelper.SafeDispatch(() =>
            {
                EventHandler selectAccountPopupClosed = (sender, args) =>
                {
                    SelectAccountPopup = null;
                };
                SelectAccountPopup.Closed += new EventHandler(selectAccountPopupClosed);
                SelectAccountPopup.Hide();
            });
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            if (SelectAccountPopup != null)
            {
                UiHelper.SafeDispatch(HideSelectAccountPopup);
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);

        }


        private void selectFollowAccount_CheckPressed(object sender, EventArgs e)
        {

            var post = sender as SelectTwitterAccount;

            bool following = false;

            // any selected items?
            foreach (var account in post.Items)
            {
                if (account.IsSelected)
                {
                    var api = new TwitterApi(account.Id);
                    api.FollowUser("Mehdoh");
                    following = true;
                }
            }

            if (following)
            {
                UiHelper.ShowToast("Thank you for following us!");
            }

            HideSelectAccountPopup();

        }

        protected DialogService SelectAccountPopup { get; set; }


        private void btnFollow_Click(object sender, RoutedEventArgs e)
        {
            ShowAccountSelect();
        }

        private void btnRateReview_Click(object sender, RoutedEventArgs e)
        {

#if MEHDOH_FREE
            UiHelper.ShowMehdohFullMarketplaceDetails();
#else
            var marketplaceTask = new MarketplaceReviewTask();
            marketplaceTask.Show();
#endif

        }

        private void aboutImage_DoubleTap(object sender, GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.MehdohSays;component/MehdohSays/MainPage.xaml", UriKind.Relative));
        }
    }
}