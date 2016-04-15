#region

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ViewModels;

#endregion

namespace Mitter
{
    public partial class ListMembership : AnimatedBasePage
    {
        #region Properties

        private ObservableCollection<CustomiseItemCoreViewModel> UserLists { get; set; }
        private int StatusCount { get; set; }

        private string ScreenName { get; set; }
        private bool DataLoaded { get; set; }

        #endregion

        #region Constructor

        public ListMembership()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;

            DataLoaded = false;
            StatusCount = 0;
        }

        #endregion

        #region Overrides

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            switch (animationType)
            {
                case AnimationType.NavigateForwardIn:
                    return new SlideUpAnimator {RootElement = LayoutRoot};

                case AnimationType.NavigateBackwardOut:
                    return new SlideDownAnimator {RootElement = LayoutRoot};

                case AnimationType.NavigateBackwardIn:
                    return new SlideUpAnimator {RootElement = LayoutRoot};

                case AnimationType.NavigateForwardOut:
                    return new SlideDownAnimator {RootElement = LayoutRoot};
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string screenName;
            if (!NavigationContext.QueryString.TryGetValue("Screen", out screenName))
            {
                NavigationService.GoBack();
                return;
            }

            ScreenName = screenName;

            BindValues();
        }

        #endregion

        #region Members

        private void BindValues()
        {
            BindListValues();
        }

        private void BindListValues()
        {
            UiHelper.ShowProgressBar("retrieving lists and statuses");

            UserLists = new ObservableCollection<CustomiseItemCoreViewModel>();
            lstLists.DataContext = UserLists;

            using (var dh = new MainDataContext())
            {
                foreach (var account in dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                {
                    var api = new TwitterApi(account.Id);
                    api.GetUsersOwnListsCompletedEvent += api_GetUsersOwnListsCompletedEvent;
                    api.GetUsersOwnLists();
                }
            }
        }

        private void api_GetUsersOwnListsCompletedEvent(object sender, EventArgs e)
        {
            var api = sender as TwitterApi;

            if (api == null || api.GetUsersOwnList == null || api.GetUsersOwnList.lists == null || !api.GetUsersOwnList.lists.Any())
            {
                UiHelper.SafeDispatch(() =>
                                          {
                                              lstLists.Visibility = Visibility.Collapsed;
                                              txtNoLists.Visibility = Visibility.Visible;
                                          });
                UiHelper.HideProgressBar();
                return;
            }

            Dispatcher.BeginInvoke(() =>
                                       {
                                           lstLists.Visibility = Visibility.Visible;
                                           txtNoLists.Visibility = Visibility.Collapsed;

                                           UiHelper.HideProgressBar();

                                           using (var dh = new MainDataContext())
                                           {
                                               var currentAccount = dh.Profiles.Single(x => x.Id == api.AccountId);

                                               StatusCount = 0;

                                               foreach (var listItem in api.GetUsersOwnList.lists)
                                               {
                                                   string profileImageUrl;

                                                   if (listItem.user.id != currentAccount.Id)
                                                       continue;

                                                   StatusCount += 1;

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
                                                                         Type = 1,
                                                                         AccountId = currentAccount.Id,
                                                                         ProfileImageUrl = profileImageUrl,
                                                                         IsChecked = false
                                                                     };

                                                   UserLists.Add(newItem);

                                                   var accountId = api.AccountId;

                                                   var checkApi = new TwitterApi(accountId);
                                                   checkApi.State = newItem;
                                                   checkApi.CheckListMembershipCompletedEvent += checkApi_CheckListMembershipCompletedEvent;
                                                   checkApi.CheckListMembership(newItem.Value, ScreenName, accountId);
                                               }
                                           }
                                       });
        }

        private void model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!DataLoaded || StatusCount > 0)
                return;

            if (e.PropertyName != "IsChecked")
                return;

            // what is it?
            var model = sender as CustomiseItemCoreViewModel;
            if (model == null)
                return;

            if (model.IsChecked.HasValue && model.IsChecked.Value)
            {
                // add user to the list
                AddUserToList(model);
            }
            else
            {
                // remove the user from the list
                RemoveUserFromList(model);
            }
        }

        private void RemoveUserFromList(CustomiseItemCoreViewModel model)
        {
            UiHelper.ShowProgressBar("removing user from the list");

            var api = new TwitterApi(model.AccountId);
            api.RemoveUserFromListCompletedEvent += api_RemoveUserFromListCompletedEvent;
            api.RemoveUserFromList(model.Value, ScreenName);
        }

        private void api_RemoveUserFromListCompletedEvent(object sender, EventArgs e)
        {
            UiHelper.HideProgressBar();

            var api = sender as TwitterApi;
            UiHelper.ShowToast(api != null && api.HasError
                                   ? "twitter error while removing user from the list."
                                   : "user removed from the list.");
        }

        private void AddUserToList(CustomiseItemCoreViewModel model)
        {
            UiHelper.ShowProgressBar("adding user to the list");

            var api = new TwitterApi(model.AccountId);
            api.AddUserToListCompletedEvent += api_AddUserToListCompletedEvent;
            api.AddUserToList(model.Value, ScreenName);
        }

        private void api_AddUserToListCompletedEvent(object sender, EventArgs e)
        {
            UiHelper.HideProgressBar();

            var api = sender as TwitterApi;
            UiHelper.ShowToast(api != null && api.HasError
                                   ? "twitter error while adding user to list."
                                   : "user added to the list.");
        }


        private void checkApi_CheckListMembershipCompletedEvent(object sender, EventArgs e)
        {
            var api = sender as TwitterApi;

            UiHelper.SafeDispatch(() =>
                                      {
                                          var state = api.State as CustomiseItemCoreViewModel;
                                          if (state != null)
                                          {
                                              state.IsChecked = !api.HasError;
                                              // if it errors, then the user isn't in that list
                                              state.PropertyChanged += model_PropertyChanged;
                                          }

                                          StatusCount -= 1;

                                          if (StatusCount == 0)
                                          {
                                              DataLoaded = true;
                                              UiHelper.HideProgressBar();
                                          }
                                      });
        }

        #endregion
    }
}