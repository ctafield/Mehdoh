#region

using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Responses;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;

#endregion

namespace Mitter
{
    public partial class FollowSuggestions : AnimatedBasePage
    {
        public FollowSuggestions()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;
        }

        private long AccountId { get; set; }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!UiHelper.ValidateUser())
            {
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                return;
            }

            AccountId = UiHelper.GetAccountId(NavigationContext);

            if (e.NavigationMode == NavigationMode.New)
            {
                GetCategories();
            }
            
        }

        private void GetCategories()
        {
            txtWait.Visibility = Visibility.Visible;
            lstCategories.Visibility = Visibility.Collapsed;
            UiHelper.ShowProgressBar("getting categories");

            var api = new TwitterApi(AccountId);
            api.GetSuggestedUserCategoriesCompletedEvent += api_GetSuggestedUsersCompletedEvent;
            api.GetSuggestedUserCategories();
        }

        private void api_GetSuggestedUsersCompletedEvent(object sender, EventArgs e)
        {
            var api = sender as TwitterApi;

            if (api == null || api.SuggestedUserCategories == null || !api.SuggestedUserCategories.Any())
            {
                UiHelper.HideProgressBar();

                UiHelper.SafeDispatch(() =>
                                          {
                                              if (api.HasError)
                                              {
                                                  UiHelper.ShowToast("twitter",
                                                                     "there was a problem with getting categories.");
                                              }
                                          });
                return;
            }

            UiHelper.SafeDispatch(() =>
                                      {
                                          lstCategories.DataContext = api.SuggestedUserCategories;

                                          lstCategories.Visibility = Visibility.Visible;
                                          txtWait.Visibility = Visibility.Collapsed;
                                          UiHelper.HideProgressBar();
                                      });
        }

        private void lstCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var list = sender as ListBox;
            if (list.SelectedIndex == -1)
                return;

            var item = list.SelectedItem as ResponseGetSuggestedUserCategory;
            NavigationService.Navigate(new Uri("/FollowSuggestionsUsers.xaml?accountId=" + AccountId + "&slug=" + HttpUtility.UrlEncode(item.slug) +
                    "&name=" + HttpUtility.UrlEncode(item.name), UriKind.Relative));
        }

        protected override void AnimationsComplete(AnimationType animationType)
        {
            switch (animationType)
            {
                case AnimationType.NavigateForwardIn:
                    //if (!Refreshed)
                    //{
                    //    RefreshItems();
                    //    Refreshed = true;
                    //}
                    break;

                case AnimationType.NavigateBackwardIn:
                    //reset list so you can select the same element again
                    lstCategories.SelectedIndex = -1;
                    break;
            }
        }


        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            switch (animationType)
            {
                case AnimationType.NavigateBackwardOut:
                    return new SlideDownAnimator {RootElement = LayoutRoot};
                    //return new TurnstileFeatherBackwardOutAnimator() { ListBox = lstCategories, RootElement = LayoutRoot };

                case AnimationType.NavigateBackwardIn:
                    return
                        GetContinuumAnimation(
                            lstCategories.ItemContainerGenerator.ContainerFromIndex(lstCategories.SelectedIndex) as
                            FrameworkElement, animationType);

                case AnimationType.NavigateForwardOut:
                    return
                        GetContinuumAnimation(
                            lstCategories.ItemContainerGenerator.ContainerFromIndex(lstCategories.SelectedIndex) as
                            FrameworkElement, animationType);
            }

            return base.GetAnimation(animationType, toOrFrom);
        }
    }
}