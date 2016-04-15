// *********************************************************************************************************
// <copyright file="AccountManagement.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.ViewModels;

#if WP8

#endif

#endregion

namespace Mitter
{
    public partial class AccountManagement : AnimatedBasePage
    {

        public AccountManagement()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;

            Loaded += AccountManagement_Loaded;
        }

        private void AccountManagement_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationBar.LocaliseMenu();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New || e.NavigationMode == NavigationMode.Back)
            {
                LoadAccountInfo();
            }
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn)
                return new SlideUpAnimator { RootElement = LayoutRoot };
            else if (animationType == AnimationType.NavigateBackwardOut)
                return new SlideDownAnimator { RootElement = LayoutRoot };

            return base.GetAnimation(animationType, toOrFrom);
        }


        private void LoadAccountInfo()
        {
            var accounts = new List<AccountViewModel>();

            using (var dh = new MainDataContext())
            {
                accounts.AddRange(dh.Profiles.Where(x => x.ProfileType != ApplicationConstants.AccountTypeEnum.Deleted).Select(item => item.AsViewModel()));
            }

            UiHelper.SafeDispatch(() =>
                                      {                                          
                                          if (!accounts.Any())
                                          {
                                              txtNoAccounts.Visibility = Visibility.Visible;
                                              lstAccounts.Visibility = Visibility.Collapsed;
                                          }
                                          else
                                          {
                                              lstAccounts.DataContext = accounts;
                                              txtNoAccounts.Visibility = Visibility.Collapsed;
                                              lstAccounts.Visibility = Visibility.Visible;
                                          }                                          
                                      });
        }


        private void lnkRefreshProfile_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var model = button.Tag as AccountViewModel;

            if (model != null && model.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter)
            {
                UiHelper.ShowProgressBar("refreshing profile");
                RefreshTwitterProfile(model);
            }
        }

        private async void RefreshTwitterProfile(AccountViewModel model)
        {
            var api = new TwitterApi(model.Id);            
            var result = await api.GetUserProfile(string.Empty, model.Id); // empty username in case they have renamed their profile
            api_GetUserProfileCompletedEvent(model.Id, result);

        }

        private void api_GetUserProfileCompletedEvent(long accountId, ResponseGetUserProfile userProfile)
        {

            if (userProfile == null)
            {
                UiHelper.HideProgressBar();
                return;
            }
            
            Dispatcher.BeginInvoke(delegate
                                       {
                                           var apiProfile = userProfile.AsViewModel();

                                           using (var dh = new MainDataContext())
                                           {
                                               var profile = dh.Profiles.FirstOrDefault(x => x.Id == accountId);

                                               if (profile == null)
                                               {
                                                   dh.Profiles.InsertOnSubmit(new ProfileTable
                                                                                  {
                                                                                      Id = apiProfile.Id,
                                                                                      Bio = apiProfile.Bio,
                                                                                      DisplayName = apiProfile.DisplayName,
                                                                                      ImageUri = apiProfile.ProfileImageUrl,
                                                                                      Location = apiProfile.Location,
                                                                                      ScreenName = apiProfile.ScreenName,
                                                                                      ProfileType = ApplicationConstants.AccountTypeEnum.Twitter
                                                                                  });
                                               }
                                               else
                                               {
                                                   profile.Id = apiProfile.Id;
                                                   profile.Bio = apiProfile.Bio;
                                                   profile.DisplayName = apiProfile.DisplayName;
                                                   profile.ImageUri = apiProfile.ProfileImageUrl;
                                                   profile.Location = apiProfile.Location;
                                                   profile.ScreenName = apiProfile.ScreenName;
                                                   profile.ProfileType = ApplicationConstants.AccountTypeEnum.Twitter;
                                               }

                                               dh.SubmitChanges();

                                               StorageHelperUI.SaveProfileImageCompletedEvent += RefreshAccountInformation;

                                               StorageHelperUI.SaveProfileImage(apiProfile.OriginalProfileImageUrl,
                                                                                apiProfile.ProfileImageUrl,
                                                                                apiProfile.Id,
                                                                                ApplicationConstants.AccountTypeEnum.Twitter);                                               
                                           }
                                       });
        }

        private void RefreshAccountInformation(object sender, EventArgs eventArgs)
        {
            UiHelper.HideProgressBar();
            LoadAccountInfo();
        }

        private void lstAccounts_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var listbox = sender as ListBox;
            if (listbox != null)
                listbox.SelectedIndex = -1;
        }

        private void lnkRemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null)
                return;

            var model = button.Tag as AccountViewModel;
            if (model == null)
                return;

            if (MessageBox.Show("Are you want to remove this account from mehdoh?", "remove account",MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                UiHelper.ShowProgressBar("removing account");

                // Remove any used columns
                ColumnHelper.RemoveColumnsForUser(model.Id);

                // Clear the app view model
                ((IMehdohApp)(Application.Current)).ViewModel.ClearDownUser(model.Id);

                // mark it as deleted so it doesnt appear in the refresh
                using (var dh = new MainDataContext())
                {
                    // This is the current Profile
                    var thisProfile = dh.Profiles.FirstOrDefault(x => x.Id == model.Id);
                    thisProfile.ProfileType = ApplicationConstants.AccountTypeEnum.Deleted;
                    dh.SubmitChanges();
                }

                ThreadPool.QueueUserWorkItem(RemoveUserAccount, model);

                StorageHelper.RemoveUser(model.Id);

                UiHelper.HideProgressBar();

                LoadAccountInfo();

                ((IMehdohApp)(Application.Current)).RebindColumns();

                // todo: if no accounts then what?
                //UiHelper.RemoveUser();
                //NavigationService.GoBack();
            }
        }

        private void RemoveUserAccount(object state)
        {
            var model = state as AccountViewModel;

            using (var dh = new MainDataContext())
            {
                dh.Log = new DataLogger();

                // This is the current Profile
                var thisProfile = dh.Profiles.FirstOrDefault(x => x.Id == model.Id);

                if (model.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter)
                {
                    var timelines = dh.Timeline.Where(x => x.ProfileId == thisProfile.Id);
                    var mentions = dh.Mentions.Where(x => x.ProfileId == thisProfile.Id);
                    var favourites = dh.Favourites.Where(x => x.ProfileId == thisProfile.Id);
                    var messages = dh.Messages.Where(x => x.ProfileId == thisProfile.Id);
                    var lists = dh.TwitterList.Where(x => x.ProfileId == thisProfile.Id);
                    var retweetsOfMe = dh.RetweetsOfMe.Where(x => x.ProfileId == thisProfile.Id);
                    var retweetsToMe = dh.RetweetsToMe.Where(x => x.ProfileId == thisProfile.Id);
                    var retweetsByMe = dh.RetweetedByMe.Where(x => x.ProfileId == thisProfile.Id);

                    dh.Timeline.DeleteAllOnSubmit(timelines);
                    dh.Mentions.DeleteAllOnSubmit(mentions);
                    dh.Favourites.DeleteAllOnSubmit(favourites);
                    dh.Messages.DeleteAllOnSubmit(messages);

                    dh.TwitterList.DeleteAllOnSubmit(lists);

                    dh.RetweetsOfMe.DeleteAllOnSubmit(retweetsOfMe);
                    dh.RetweetsToMe.DeleteAllOnSubmit(retweetsToMe);
                    dh.RetweetedByMe.DeleteAllOnSubmit(retweetsByMe);

                    dh.Profiles.DeleteOnSubmit(thisProfile);
                }
                else if (model.ProfileType == ApplicationConstants.AccountTypeEnum.Instagram)
                {
                    var news = dh.InstagramNews.Where(x => x.ProfileId == thisProfile.Id);

                    dh.InstagramNews.DeleteAllOnSubmit(news);

                    dh.Profiles.DeleteOnSubmit(thisProfile);
                }
                else if (model.ProfileType == ApplicationConstants.AccountTypeEnum.Soundcloud)
                {
                    dh.Profiles.DeleteOnSubmit(thisProfile);
                }

                dh.SubmitChanges();
            }
        }

        private void accountHeader_Tap(object sender, GestureEventArgs e)
        {
            var menuItem = sender as StackPanel;
            var model = menuItem.Tag as AccountViewModel;

            if (model.ProfileType != ApplicationConstants.AccountTypeEnum.Twitter)
                return;

            ShowAccountDetails(model);
        }

        private void ShowAccountDetails(AccountViewModel model)
        {
            if (model.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter)
                NavigationService.Navigate(new Uri("/UserProfile.xaml?accountId=" + model.Id + "&screen=" + model.ScreenName, UriKind.Relative));
            else if (model.ProfileType == ApplicationConstants.AccountTypeEnum.Instagram)
                NavigationService.Navigate(new Uri("/Mitter.UI.Instagram;component/InstagramProfile.xaml?accountId=" + model.Id + "&userId=" + model.Id, UriKind.Relative));
            else if (model.ProfileType == ApplicationConstants.AccountTypeEnum.Soundcloud)
                NavigationService.Navigate(new Uri("/Mitter.UI.Soundcloud;component/SoundcloudProfile.xaml?accountId=" + model.Id + "&userId=" + model.Id, UriKind.Relative));
        }

        private void lnkViewProfile_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as Button;
            var model = menuItem.Tag as AccountViewModel;

            ShowAccountDetails(model);
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Startup.xaml", UriKind.Relative));
        }

        private void mnuCustomise_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Customise.xaml", UriKind.Relative));
        }

        #region AddAccount

        private void AddTwitterAccount()
        {
            if (LicenceInfo.IsTrial())
            {
                using (var dh = new MainDataContext())
                {
                    if (dh.Profiles.Count(x => x.ProfileType == (int)ApplicationConstants.AccountTypeEnum.Twitter) >= 1)
                    {
                        MessageBox.Show(
                            "sorry, but the trial of mehdoh only allows one twitter account. buy the full version to be able to add more!",
                            "trial mode", MessageBoxButton.OK);
                        return;
                    }
                }
            }

            var twitterUri = new Uri("/TwitterOAuth.xaml", UriKind.Relative);
            NavigationService.Navigate(twitterUri);
        }

        private void mnu_AddAccount(object sender, EventArgs e)
        {
            AddTwitterAccount();
        }

        #endregion

    }
}