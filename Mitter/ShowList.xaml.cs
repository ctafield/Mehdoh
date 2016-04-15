// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Clarity.Phone.Extensions;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.ViewModels;
using FieldOfTweets.Common.UI.UserControls;

#endregion

namespace Mitter
{
    public partial class ShowList : AnimatedBasePage
    {
        #region Fields

        private bool _fetchingMoreTweets;
        private bool _listEventsSet;

        #endregion

        #region Properties

        private bool DataLoaded { get; set; }
        private long AccountId { get; set; }
        private string ListId { get; set; }
        private string ListTitle { get; set; }

        public SortedObservableCollection<TimelineViewModel> ListStatuses { get; set; }
        protected List<AccountFriendViewModel> AccountIds { get; set; }

        protected DialogService SelectAccountPopup { get; set; }

        #endregion

        #region Constructor

        public ShowList()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;
        }

        #endregion

        #region Overrides

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn)
                //return new TurnstileFeatherForwardInAnimator() { ListBox = lstSearchResults, RootElement = LayoutRoot };
                return GetContinuumAnimation(PageTitle, animationType);

            if (animationType == AnimationType.NavigateBackwardOut)
                return new TurnstileFeatherBackwardOutAnimator {ListBox = lstStatuses, RootElement = LayoutRoot};

            return null;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (SelectAccountPopup != null)
            {
                HideSelectAccountPopup();
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            AccountId = UiHelper.GetAccountId(NavigationContext);

            AccountIds = new List<AccountFriendViewModel>();

            if (DataLoaded)
                return;

            string temp = string.Empty;
            if (!NavigationContext.QueryString.TryGetValue("slug", out temp))
                NavigationService.GoBack();

            ListId = temp;

            string temp2 = string.Empty;
            if (!NavigationContext.QueryString.TryGetValue("title", out temp2))
                NavigationService.GoBack();

            ListTitle = temp2;

            ShowData();

            DataLoaded = true;
        }

        #endregion

        #region Members

        private async void ShowData()
        {
            PageTitle.Text = ListTitle;

            ListStatuses = new SortedObservableCollection<TimelineViewModel>();
            lstStatuses.DataContext = ListStatuses;

            UiHelper.ShowProgressBar("getting tweets in this list");

            _fetchingMoreTweets = true;

            var api = new TwitterApi(AccountId);            
            var result = await api.GetListStatuses(ListId, 0, 0);
            api_GetListStatusesCompletedEvent(AccountId, result);

        }

        private void api_GetListStatusesCompletedEvent(long accountId, List<ResponseTweet> listStatuses)
        {

            _fetchingMoreTweets = false;

            if (listStatuses == null || !listStatuses.Any())
            {
                UiHelper.HideProgressBar();
                return;
            }

            UiHelper.SafeDispatch(() =>
                                       {
                                           foreach (var item in listStatuses)
                                           {
                                               if (!ListStatuses.Any(x => x.Id == item.id))
                                               {
                                                   var newItem = item.AsViewModel(accountId);
                                                   ListStatuses.Add(newItem);
                                               }
                                           }

                                           CheckListViewEventsAreSet();

                                           UiHelper.HideProgressBar();
                                       });
        }

        private void mnuSubscribe_Click(object sender, EventArgs e)
        {
            if (AccountIds.Count == 1)
            {
                UiHelper.ShowProgressBar("subscribing to list");
                var api = new TwitterApi(AccountId);
                api.SubscribeToListCompletedEvent += api_SubscribeToListCompletedevent;
                api.SubscribeToList(ListId);
            }
            else
            {
                ShowAccountSelect();
            }
        }

        // Contains a list of the current account Ids

        private void ShowAccountSelect()
        {
            //LayoutRoot.Visibility = Visibility.Collapsed;
            ApplicationBar.IsVisible = false;

            var post = new SelectTwitterAccount();
            post.ExistingValues = AccountIds;
            post.CheckPressed += post_CheckPressed;
            SelectAccountPopup = new DialogService
                                     {
                                         AnimationType = DialogService.AnimationTypes.Slide,
                                         Child = post
                                     };

            SelectAccountPopup.Show();
        }

        private void post_CheckPressed(object sender, EventArgs e)
        {
            var post = sender as SelectTwitterAccount;

            // any selected items?
            foreach (var account in post.Items)
            {
                if (account.IsSelected)
                {
                    UiHelper.ShowProgressBar("subscribing to list");
                    var api = new TwitterApi(account.Id);
                    api.SubscribeToListCompletedEvent += api_SubscribeToListCompletedevent;
                    api.SubscribeToList(ListId);
                }
            }

            HideSelectAccountPopup();
        }

        private void HideSelectAccountPopup()
        {
            UiHelper.SafeDispatch(() =>
                                      {
                                          EventHandler selectAccountPopupClosed = (sender, args) =>
                                                                                      {
                                                                                          SelectAccountPopup = null;
                                                                                          ApplicationBar.IsVisible =
                                                                                              true;
                                                                                      };
                                          SelectAccountPopup.Closed += selectAccountPopupClosed;
                                          SelectAccountPopup.Hide();
                                      });
        }

        private void api_SubscribeToListCompletedevent(object sender, EventArgs e)
        {
            var api = sender as TwitterApi;

            try
            {
                if (api.HasError)
                {
                    UiHelper.ShowToast("twitter", "there was a problem subscribing to the list");
                }
                else
                {
                    UiHelper.ShowToast("You have successfully subscribed to the list.");
                }
            }
            finally
            {
                UiHelper.HideProgressBar();
            }
        }

        private void CheckListViewEventsAreSet()
        {
            if (_listEventsSet)
                return;

            var currentListBox = (ScrollViewer) FindElementRecursive(lstStatuses, typeof (ScrollViewer));
            if (currentListBox == null)
                return;

            // Visual States are always on the first child of the control template 
            var element = VisualTreeHelper.GetChild(currentListBox, 0) as FrameworkElement;
            if (element == null)
                return;

            var verticalVisualStateGroup = FindVisualState(element, "VerticalCompression");
            if (verticalVisualStateGroup != null)
            {
                verticalVisualStateGroup.CurrentStateChanging += lstStatusesVertical_CurrentStateChanging;
                _listEventsSet = true;
            }
        }

        private async void lstStatusesVertical_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState.Name != "CompressionBottom")
                return;

            if (_fetchingMoreTweets)
                return;

            _fetchingMoreTweets = true;

            UiHelper.ShowProgressBar("getting more tweets");

            long maxId = 0;

            if (ListStatuses.Any())
                maxId = ListStatuses.Min(x => x.Id) - 1;

            var api = new TwitterApi(AccountId);            
            var result = await api.GetListStatuses(ListId, 0, maxId);
            api_GetListStatusesCompletedEvent(AccountId, result);

        }

        private void lstStatuses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lstBox = sender as ListBox;

            if (lstBox == null)
                return;

            // If selected index is -1 (no selection) do nothing
            if (lstBox.SelectedIndex == -1)
                return;

            var model = lstBox.SelectedValue as TimelineViewModel;
            if (model == null)
                return;

            string query = "accountId=" + model.AccountId + "&id=" + model.Id;

            if (model.MediaUrl != null)
                query += "&photo=true";

            // Navigate to the new page
            NavigateToDetailsPage(query);

            lstBox.SelectedIndex = -1;
        }

        private void NavigateToDetailsPage(string query)
        {
            UiHelper.SafeDispatch(() => NavigationService.Navigate(new Uri("/DetailsPage.xaml?" + query, UriKind.Relative)));
        }


        #endregion

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
                        returnElement = FindElementRecursive(VisualTreeHelper.GetChild(parent, i) as FrameworkElement,
                                                             targetType);
                    }
                }
            }
            return returnElement;
        }

        private VisualStateGroup FindVisualState(FrameworkElement element, string name)
        {
            if (element == null)
                return null;

            var groups = VisualStateManager.GetVisualStateGroups(element);
            foreach (VisualStateGroup group in groups)
                if (group.Name == name)
                    return group;

            return null;
        }

        #endregion
    }
}