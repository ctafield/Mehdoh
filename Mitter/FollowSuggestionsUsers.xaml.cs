// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

#region

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ViewModels;

#endregion

namespace Mitter
{
    public partial class FollowSuggestionsUsers : AnimatedBasePage
    {
        #region Properties

        private ObservableCollection<FriendViewModel> Users { get; set; }

        protected long AccountId { get; set; }
        private string Slug { get; set; }

        #endregion

        #region Constructor

        public FollowSuggestionsUsers()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;

            Users = new ObservableCollection<FriendViewModel>();
            lstUsers.DataContext = Users;
        }

        #endregion

        #region Overrides

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn)
                //return new TurnstileFeatherForwardInAnimator() { ListBox = lstSearchResults, RootElement = LayoutRoot };
                return GetContinuumAnimation(ApplicationTitle, animationType);
            else if (animationType == AnimationType.NavigateBackwardIn)
                return new TurnstileBackwardInAnimator {RootElement = LayoutRoot};
            else if (animationType == AnimationType.NavigateBackwardOut)
                return new TurnstileFeatherBackwardOutAnimator {ListBox = lstUsers, RootElement = LayoutRoot};

            return null;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            Users = null;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New)
            {
                AccountId = UiHelper.GetAccountId(NavigationContext);

                string temp;
                if (!NavigationContext.QueryString.TryGetValue("slug", out temp))
                    NavigationService.GoBack();

                Slug = HttpUtility.UrlDecode(temp);

                NavigationContext.QueryString.TryGetValue("name", out temp);

                ApplicationTitle.Text = HttpUtility.UrlDecode(temp);

                RefreshUsers();
            }
            
        }

        #endregion

        #region Members

        private void RefreshUsers()
        {
            lstUsers.Visibility = Visibility.Collapsed;
            txtWait.Visibility = Visibility.Visible;
            UiHelper.ShowProgressBar("finding suggestions");

            var api = new TwitterApi(AccountId);
            api.GetSuggestedUsersCompletedEvent += api_GetSuggestedUsersCompletedEvent;
            api.GetSuggestedUsers(Slug);
        }


        private void api_GetSuggestedUsersCompletedEvent(object sender, EventArgs e)
        {
            var api = sender as TwitterApi;

            if (api == null || api.SuggestedUsers == null || api.SuggestedUsers.users == null ||
                !api.SuggestedUsers.users.Any())
            {
                UiHelper.HideProgressBar();

                UiHelper.SafeDispatch(() =>
                                          {
                                              if (api.HasError)
                                              {
                                                  UiHelper.ShowToast("twitter",
                                                                     "there was a problem with getting suggestions.");
                                              }
                                          });
                return;
            }

            UiHelper.SafeDispatch(() =>
                                      {
                                          foreach (var item in api.SuggestedUsers.users)
                                          {
                                              var newItem = new FriendViewModel
                                                                {
                                                                    DisplayName = item.name,
                                                                    ScreenName = item.screen_name,
                                                                    Id = item.id,
                                                                    ProfileImageUrl = item.profile_image_url
                                                                };

                                              if (Users != null)
                                                Users.Add(newItem);
                                          }

                                          lstUsers.Visibility = Visibility.Visible;
                                          txtWait.Visibility = Visibility.Collapsed;
                                          UiHelper.HideProgressBar();

                                          api.SuggestedUsers = null;
                                          api = null;
                                      });
        }

        private void lstUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstUsers.SelectedIndex == -1)
                return;

            var item = lstUsers.SelectedItem as FriendViewModel;
            if (item == null)
                return;

            var screen = item.ScreenName;

            NavigationService.Navigate(
                new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + HttpUtility.UrlEncode(screen),
                        UriKind.Relative));

            lstUsers.SelectedIndex = -1;
        }

        #endregion
    }
}