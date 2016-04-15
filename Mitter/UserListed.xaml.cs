using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ViewModels;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Mitter
{
    public partial class UserListed : AnimatedBasePage
    {

        private ObservableCollection<CustomiseItemCoreViewModel> ListedInData { get; set; }
        private ObservableCollection<CustomiseItemCoreViewModel> UserLists { get; set; }

        private bool DataLoaded { get; set; }
        private string ScreenName { get; set; }
        private long NextCursor { get; set; }
        private long AccountId { get; set; }

        public UserListed()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;
            DataLoaded = false;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            // Clear up

            if (ListedInData != null)
                ListedInData.Clear();
            ListedInData = null;

            if (UserLists != null)
                UserLists.Clear();
            UserLists = null;

            base.OnBackKeyPress(e);
        }

        protected override void AnimationsComplete(AnimationType animationType)
        {

            switch (animationType)
            {
                case AnimationType.NavigateBackwardIn:
                    //reset list so you can select the same element again
                    lstLists.SelectedIndex = -1;
                    lstListed.SelectedIndex = -1;
                    break;
            }

        }


        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {

            switch (animationType)
            {
                case AnimationType.NavigateBackwardOut:
                    if (pivotMain.SelectedIndex == 0)
                        return new SlideDownAnimator() { RootElement = LayoutRoot };
                    if (pivotMain.SelectedIndex == 1)
                        return new SlideDownAnimator() { RootElement = LayoutRoot };
                    if (pivotMain.SelectedIndex == 2)
                        return new SlideDownAnimator() { RootElement = LayoutRoot };
                    break;

                case AnimationType.NavigateBackwardIn:
                    //if (pivotMain.SelectedIndex == 0)
                    //return GetContinuumAnimation(lstListed.ItemContainerGenerator.ContainerFromIndex(lstListed.SelectedIndex) as FrameworkElement, animationType);                        
                    //if (pivotMain.SelectedIndex == 1)
                    //return GetContinuumAnimation(lstLists.ItemContainerGenerator.ContainerFromIndex(lstLists.SelectedIndex) as FrameworkElement, animationType);                        
                    break;

                case AnimationType.NavigateForwardOut:
                    //if (pivotMain.SelectedIndex == 0)
                    //return GetContinuumAnimation(lstListed.ItemContainerGenerator.ContainerFromIndex(lstListed.SelectedIndex) as FrameworkElement, animationType);
                    //if (pivotMain.SelectedIndex == 1)
                    //return GetContinuumAnimation(lstLists.ItemContainerGenerator.ContainerFromIndex(lstLists.SelectedIndex) as FrameworkElement, animationType);
                    break;
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (DataLoaded)
                return;

            AccountId = UiHelper.GetAccountId(NavigationContext);

            string temp;
            if (!NavigationContext.QueryString.TryGetValue("screen", out temp))
                NavigationService.GoBack();

            ScreenName = temp;

            string page = string.Empty;
            if (NavigationContext.QueryString.TryGetValue("page", out temp))
                page = temp;

            if (!string.IsNullOrWhiteSpace(page))
            {
                int pageIndex = int.Parse(page);
                pivotMain.SelectedIndex = pageIndex;
            }

            NextCursor = -1;

            ListedInData = new ObservableCollection<CustomiseItemCoreViewModel>();
            UserLists = new ObservableCollection<CustomiseItemCoreViewModel>();

            lstListed.DataContext = ListedInData;
            lstLists.DataContext = UserLists;

            UiHelper.ShowProgressBar("fetching lists");

            LoadListedIn();
            LoadUserLists();

            CheckEventsOnListBox();

            DataLoaded = true;
        }


        private void CheckEventsOnListBox()
        {

            if (!ListedEventsSet)
            {
                var timelineSv = (ScrollViewer)FindElementRecursive(lstListed, typeof(ScrollViewer));
                if (timelineSv != null)
                {
                    // Visual States are always on the first child of the control template 
                    var element = VisualTreeHelper.GetChild(timelineSv, 0) as FrameworkElement;
                    if (element != null)
                    {
                        var vgroup = FindVisualState(element, "VerticalCompression");
                        if (vgroup != null)
                        {
                            vgroup.CurrentStateChanging += new EventHandler<VisualStateChangedEventArgs>(lstTimelineVertical_CurrentStateChanging);
                        }
                    }
                    ListedEventsSet = true;
                }
            }

        }

        protected bool ListedEventsSet { get; set; }

        private void lstTimelineVertical_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState.Name == "CompressionBottom")
            {
                UiHelper.ShowProgressBar("fetching more lists");
                ThreadPool.QueueUserWorkItem(LoadListedIn);
            }
        }

        private async void LoadUserLists()
        {
            var api = new TwitterApi(AccountId);
            var results = await api.GetUsersLists(ScreenName);
            api_GetUsersListsCompletedEvent(AccountId, results, api.HasError, api.ErrorMessage);
        }

        void api_GetUsersListsCompletedEvent(long accountId, List<ResponseGetUsersList> getUsersList, bool hasError, string errorMessage)
        {

            if (hasError)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                    UiHelper.ShowToast(errorMessage);
                else
                    UiHelper.ShowToast("There was a problem connecting to Twitter.");
            }

            if (UserLists == null) // cancelled out before retrieving values?
                return;


            if (getUsersList == null || !getUsersList.Any())
            {
                UiHelper.SafeDispatch(() =>
                                          {
                                              txtUserLists.Visibility = Visibility.Visible;
                                              lstLists.Visibility = Visibility.Collapsed;
                                              UiHelper.HideProgressBar();
                                          });
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {

                try
                {

                    foreach (var listItem in getUsersList)
                    {
                        var newItem = new CustomiseItemCoreViewModel()
                        {
                            Description = listItem.description,
                            SubTitle = listItem.full_name,
                            Title = listItem.name,
                            Value = listItem.id_str,
                            AccountId = listItem.user.id,
                            ProfileImageUrl = listItem.user.profile_image_url
                        };

                        if (UserLists == null)
                            return;

                        UserLists.Add(newItem);
                    }

                }
                catch
                {
                }

                txtUserLists.Visibility = Visibility.Collapsed;
                lstLists.Visibility = Visibility.Visible;

                CheckEventsOnListBox();

                UiHelper.HideProgressBar();

            });

        }

        private void LoadListedIn(object state)
        {
            LoadListedIn();
        }

        private void LoadListedIn()
        {
            var api = new TwitterApi(AccountId);
            api.GetListsMembershipsCompletedEvent += new EventHandler(api_GetListsMembershipsCompletedEvent);
            api.GetListsMemberships(ScreenName, NextCursor);
        }

        void api_GetListsMembershipsCompletedEvent(object sender, EventArgs e)
        {

            var api = sender as TwitterApi;

            if (ListedInData == null) // cancelled out before retrieving values?
                return;

            if (api != null && api.HasError)
            {
                if (!string.IsNullOrEmpty(api.ErrorMessage))
                    UiHelper.ShowToast(api.ErrorMessage);
                else
                    UiHelper.ShowToast("There was a problem connecting to Twitter.");
                return;
            }

            if (api == null || api.ListMembership == null || api.ListMembership.lists == null || !api.ListMembership.lists.Any())
            {
                UiHelper.SafeDispatch(() =>
                                          {
                                              if (ListedInData == null || !ListedInData.Any())
                                              {
                                                  txtUserListed.Visibility = Visibility.Visible;
                                                  lstListed.Visibility = Visibility.Collapsed;
                                              }
                                              UiHelper.HideProgressBar();
                                          });
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {
                NextCursor = api.ListMembership.next_cursor;

                try
                {

                    foreach (var listItem in api.ListMembership.lists)
                    {
                        var newItem = new CustomiseItemCoreViewModel()
                                          {
                                              Description = listItem.description,
                                              SubTitle = listItem.full_name,
                                              Title = listItem.name,
                                              Value = listItem.id_str,
                                              AccountId = listItem.user.id,
                                              ProfileImageUrl = listItem.user.profile_image_url
                                          };

                        if (ListedInData == null) // cancelled out before retrieving values?
                            return;

                        ListedInData.Add(newItem);
                    }

                    CheckEventsOnListBox();

                    txtUserListed.Visibility = Visibility.Collapsed;
                    lstListed.Visibility = Visibility.Visible;

                }
                catch (Exception)
                {
                }
                finally
                {
                    if (api != null)
                    {
                        api.ListMembership = null;
                        api = null;
                    }
                    UiHelper.HideProgressBar();
                    
                }

            });

        }

        private void lst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var listBox = sender as ListBox;

            if (listBox.SelectedIndex == -1)
                return;

            var item = listBox.SelectedValue as CustomiseItemCoreViewModel;

            // Show the list
            NavigationService.Navigate(new Uri("/ShowList.xaml?accountId=" + AccountId + "&slug=" + item.Value + "&title=" + item.Title, UriKind.Relative));

        }

        #region Find UI Helper

        private UIElement FindElementRecursive(FrameworkElement parent, Type targetType)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            UIElement returnElement = null;
            if (childCount > 0)
            {
                for (int i = 0; i < childCount; i++)
                {
                    Object element = VisualTreeHelper.GetChild(parent, i);
                    if (element.GetType() == targetType)
                    {
                        return element as UIElement;
                    }
                    else
                    {
                        returnElement = FindElementRecursive(VisualTreeHelper.GetChild(parent, i) as FrameworkElement, targetType);
                    }
                }
            }
            return returnElement;
        }
        private VisualStateGroup FindVisualState(FrameworkElement element, string name)
        {
            if (element == null)
                return null;

            IList groups = VisualStateManager.GetVisualStateGroups(element);
            foreach (VisualStateGroup group in groups)
                if (group.Name == name)
                    return group;

            return null;
        }

        #endregion

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckEventsOnListBox();
        }


        private void pivotMain_DoubleTap(object sender, GestureEventArgs e)
        {

        }

    }
}