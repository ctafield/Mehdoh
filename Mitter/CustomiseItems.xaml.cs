// <copyright file="CustomiseItems.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>
// Mehdoh for Windows Phone
// </summary>

#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Clarity.Phone.Extensions;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.Responses;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.Resources;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;
using Mitter.UserControls;

#endregion

namespace Mitter
{
    public partial class CustomiseItems : AnimatedBasePage
    {
        protected ObservableCollection<CustomiseItemCoreViewModel> UsersLists { get; set; }
        protected ObservableCollection<CustomiseItemCoreViewModel> UsersSearches { get; set; }

        public CustomiseItems()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            switch (animationType)
            {
                case AnimationType.NavigateForwardIn:
                    return new SlideUpAnimator { RootElement = LayoutRoot };

                case AnimationType.NavigateBackwardOut:
                    return new SlideDownAnimator { RootElement = LayoutRoot };

                case AnimationType.NavigateBackwardIn:
                    return new SlideUpAnimator { RootElement = LayoutRoot };

                case AnimationType.NavigateForwardOut:
                    return new SlideDownAnimator { RootElement = LayoutRoot };
            }

            return base.GetAnimation(animationType, toOrFrom);
        }


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            BindValues(e);
        }

        private void BindValues(NavigationEventArgs e)
        {
            BindCoreTwitterValues();

            BindListValues();
            BindSpecialTwitterValues();
            BindTwitterSearchValues();
        }

        private async void BindTwitterSearchValues()
        {
            // lstSearch
            UsersSearches = new ObservableCollection<CustomiseItemCoreViewModel>();
            lstSearch.DataContext = UsersSearches;

            using (var dh = new MainDataContext())
            {
                foreach (var account in dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                {
                    var api = new TwitterApi(account.Id);
                    var result = await api.GetSavedSearches();
                    api_GetSavedSearchesCompletedEvent(account.Id, result);
                }
            }
        }

        private void api_GetSavedSearchesCompletedEvent(long accountId, List<ResponseGetSavedSearch> savedSearches )
        {

            if (savedSearches == null || !savedSearches.Any() || UsersSearches == null)
                return;

            UiHelper.SafeDispatch(() =>
                                      {
                                          using (var dh = new MainDataContext())
                                          {
                                              var currentAccount = dh.Profiles.Single(x => x.Id == accountId);

                                              foreach (var search in savedSearches)
                                              {
                                                  string profileImageUrl = currentAccount.CachedImageUri;

                                                  var newItem = new CustomiseItemCoreViewModel
                                                                    {
                                                                        Description = search.name,
                                                                        Title = search.name,
                                                                        Value = search.query,
                                                                        Type = ApplicationConstants.ColumnTypeTwitterSearch,
                                                                        AccountId = currentAccount.Id,
                                                                        ProfileImageUrl = profileImageUrl
                                                                    };

                                                  if (UsersSearches != null)
                                                      UsersSearches.Add(newItem);
                                                  else
                                                      return;
                                              }
                                          }

                                      });
        }

        private void BindSpecialTwitterValues()
        {
            var specialItems = new List<CustomiseItemCoreViewModel>();

            using (var dh = new MainDataContext())
            {
                foreach (var item in dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                {

                    specialItems.Add(new CustomiseItemCoreViewModel
                    {
                        Title = "retweets of you",
                        Description = "tweets of yours that have been retweeted by other users",
                        Value = ApplicationConstants.ColumnTwitterRetweetsOfMe,
                        Type = ApplicationConstants.ColumnTypeTwitter,
                        AccountId = item.Id,
                        ProfileImageUrl = item.CachedImageUri
                    });

                    specialItems.Add(new CustomiseItemCoreViewModel
                                         {
                                             Title = "new followers",
                                             Description = "people who have recently followed you",
                                             Value = ApplicationConstants.ColumnTwitterNewFollowers,
                                             Type = ApplicationConstants.ColumnTypeTwitter,
                                             AccountId = item.Id,
                                             ProfileImageUrl = item.CachedImageUri
                                         });

                    specialItems.Add(new CustomiseItemCoreViewModel
                                         {
                                             Title = "photos",
                                             Description = "just pictures from your timeline",
                                             Value = ApplicationConstants.ColumnTwitterPhotoView,
                                             Type = ApplicationConstants.ColumnTypeTwitter,
                                             AccountId = item.Id,
                                             ProfileImageUrl = item.CachedImageUri
                                         });
                }
            }

            lstSpecial.DataContext = specialItems;
        }

        private async void BindListValues()
        {
            UsersLists = new ObservableCollection<CustomiseItemCoreViewModel>();
            lstLists.DataContext = UsersLists;

            using (var dh = new MainDataContext())
            {
                foreach (var account in dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                {
                    var api = new TwitterApi(account.Id);
                    var result = await api.GetUsersLists();
                    api_GetUsersListsCompletedEvent(account.Id, result);
                }
            }
        }

        private void api_GetUsersListsCompletedEvent(long accountId, List<ResponseGetUsersList> getUsersList)
        {

            if (getUsersList == null || !getUsersList.Any() || UsersLists == null)
                return;

            Dispatcher.BeginInvoke(() =>
                                       {

                                           using (var dh = new MainDataContext())
                                           {
                                               var currentAccount =dh.Profiles.SingleOrDefault(x => x.Id == accountId);
                                               if (currentAccount == default(ProfileTable))
                                               {
                                                   return;
                                               }

                                               foreach (var listItem in getUsersList)
                                               {
                                                   string profileImageUrl;

                                                   if (listItem.user.id == currentAccount.Id)
                                                       profileImageUrl = currentAccount.CachedImageUri;
                                                   else
                                                       profileImageUrl = listItem.user.profile_image_url;

                                                   var newItem = new CustomiseItemCoreViewModel
                                                                     {
                                                                         Description = listItem.description,
                                                                         SubTitle = listItem.full_name,
                                                                         Title = listItem.name,
                                                                         Value = listItem.id_str,
                                                                         Type = ApplicationConstants.ColumnTypeTwitterList,
                                                                         AccountId = currentAccount.Id,
                                                                         ProfileImageUrl = profileImageUrl
                                                                     };

                                                   if (UsersLists != null)
                                                       UsersLists.Add(newItem);                                                                                                          
                                                   else
                                                       return;
                                               }

                                           }
                                       });
        }

        private void BindCoreTwitterValues()
        {
            var coreItems = new List<CustomiseItemCoreViewModel>();

            using (var dh = new MainDataContext())
            {
                foreach (
                    var account in dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter)
                    )
                {
                    coreItems.Add(new CustomiseItemCoreViewModel
                                      {
                                          Title = "timeline",
                                          Description = "the main timeline",
                                          Value = ApplicationConstants.ColumnTwitterTimeline,
                                          Type = ApplicationConstants.ColumnTypeTwitter,
                                          AccountId = account.Id,
                                          ProfileImageUrl = account.CachedImageUri
                                      });
                    coreItems.Add(new CustomiseItemCoreViewModel
                                      {
                                          Title = "mentions",
                                          Description = "your @ mentions",
                                          Value = ApplicationConstants.ColumnTwitterMentions,
                                          Type = ApplicationConstants.ColumnTypeTwitter,
                                          AccountId = account.Id,
                                          ProfileImageUrl = account.CachedImageUri
                                      });
                    coreItems.Add(new CustomiseItemCoreViewModel
                                      {
                                          Title = "messages",
                                          Description = "your direct messages",
                                          Value = ApplicationConstants.ColumnTwitterMessages,
                                          Type = ApplicationConstants.ColumnTypeTwitter,
                                          AccountId = account.Id,
                                          ProfileImageUrl = account.CachedImageUri
                                      });
                    coreItems.Add(new CustomiseItemCoreViewModel
                                      {
                                          Title = ApplicationResources.favourites,
                                          Description = ApplicationResources.yourfavourites,
                                          Value = ApplicationConstants.ColumnTwitterFavourites,
                                          Type = ApplicationConstants.ColumnTypeTwitter,
                                          AccountId = account.Id,
                                          ProfileImageUrl = account.CachedImageUri
                                      });
                }
            }

            lstCore.ItemsSource = coreItems;
        }

        private void lst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;

            if (listBox.SelectedIndex == -1)
                return;

            var newItem = listBox.SelectedItem as CustomiseItemCoreViewModel;

            if (LicenceInfo.IsTrial())
            {
                if (newItem.Type == ApplicationConstants.ColumnTypeTwitterList)
                {
                    // only 1 list
                    if (
                        ColumnHelper.ColumnConfig.Count(x => x.ColumnType == ApplicationConstants.ColumnTypeTwitterList) >=
                        1)
                    {
                        listBox.SelectedIndex = -1;
                        MessageBox.Show(
                            "sorry, but the trial of mehdoh only allows you to have one twitter list. buy the full version to be able to add more!",
                            "trial mode", MessageBoxButton.OK);
                        return;
                    }
                }

                if (ColumnHelper.ColumnConfig.Count >= 4)
                {
                    listBox.SelectedIndex = -1;
                    MessageBox.Show(
                        "sorry, but the trial of mehdoh only a maximum of four items. buy the full version to be able to add more!",
                        "trial mode", MessageBoxButton.OK);
                    return;
                }
            }

            ColumnHelper.AddNewColumn(newItem.AsDomainModel());

            ((IMehdohApp)(Application.Current)).RebindColumns();

            NavigationService.GoBack();

            listBox.SelectedIndex = -1;
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

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
        
            base.OnBackKeyPress(e);

            UsersLists = null;
            UsersSearches = null;
        }

        #endregion
    }
}