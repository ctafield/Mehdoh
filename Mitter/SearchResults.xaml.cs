// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

#region

using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

#endregion

namespace Mitter
{
    public partial class SearchResults : AnimatedBasePage
    {
        #region Fields

        private string _query;

        #endregion

        #region Properties

        private bool LoadedData { get; set; }
        private bool InitialisedEvents { get; set; }
        private long AccountId { get; set; }

        private SearchResultsViewModel ViewModel { get; set; }

        #endregion

        #region Constructor

        public SearchResults()
        {
            InitializeComponent();
            Loaded += SearchResults_Loaded;
            AnimationContext = LayoutRoot;
        }

        #endregion

        #region Overrides

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn)
                //return new TurnstileFeatherForwardInAnimator() { ListBox = lstSearchResults, RootElement = LayoutRoot };
                return GetContinuumAnimation(ApplicationTitle, animationType);
            else if (animationType == AnimationType.NavigateBackwardOut)
                return new TurnstileFeatherBackwardOutAnimator { ListBox = lstSearchResults, RootElement = LayoutRoot };

            return null;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            if (ViewModel != null)
                ViewModel.Results = null;

            ViewModel = null;

            base.OnBackKeyPress(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!UiHelper.ValidateUser())
            {
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                return;
            }

            if (!NavigationContext.QueryString.TryGetValue("term", out _query))
                NavigationService.GoBack();

            _query = HttpUtility.UrlDecode(_query);

            ApplicationTitle.Text = _query;

            AccountId = UiHelper.GetAccountId(NavigationContext);

            if (!LoadedData)
            {
                ViewModel = new SearchResultsViewModel();
                DataContext = ViewModel;
                PerformSearch(true);
            }
        }

        #endregion

        #region Members

        private void SearchResults_Loaded(object sender, RoutedEventArgs e)
        {
            if (InitialisedEvents)
                return;

            CheckListViewEventsAreSet();
        }

        private void CheckListViewEventsAreSet()
        {
            var timelineSv = (ScrollViewer)FindElementRecursive(lstSearchResults, typeof(ScrollViewer));
            if (timelineSv != null)
            {
                // Visual States are always on the first child of the control template 
                var element = VisualTreeHelper.GetChild(timelineSv, 0) as FrameworkElement;
                if (element != null)
                {
                    var vgroup = FindVisualState(element, "VerticalCompression");
                    if (vgroup != null)
                    {
                        vgroup.CurrentStateChanging += lstSearchResultsVertical_CurrentStateChanging;
                        InitialisedEvents = true;
                    }
                }
            }
        }

        private void lstSearchResultsVertical_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState.Name != "CompressionBottom")
                return;

            UiHelper.ShowProgressBar("fetching more results");

            ThreadPool.QueueUserWorkItem(GetMoreSearchResults);
        }

        private void GetMoreSearchResults(object state)
        {
            PerformSearch(false);
        }

        private async void PerformSearch(bool setIndicator)
        {
            if (setIndicator)
            {
                UiHelper.ShowProgressBar("searching for " + _query);
            }

            var api = new TwitterApi(AccountId);

            long maxId = 0;

            if (ViewModel == null)
                return;

            if (ViewModel.Results.Any())
                maxId = ViewModel.Results.Min(x => x.Id);

            var results = await api.Search(_query, maxId, 0, false);
            api_SearchCompletedEvent(AccountId, results, api.HasError);


            LoadedData = true;
        }

        private void api_SearchCompletedEvent(long accountId, ResponseSearch searchResult, bool hasError)
        {

            if (searchResult == null || searchResult.statuses == null || !searchResult.statuses.Any())
            {
                UiHelper.HideProgressBar();

                UiHelper.SafeDispatch(() =>
                                          {
                                              if (ViewModel == null || ViewModel.Results == null || ViewModel.Results.Count == 0)
                                              {
                                                  noResults.Visibility = Visibility.Visible;
                                                  lstSearchResults.Visibility = Visibility.Collapsed;
                                              }

                                              if (hasError)
                                              {
                                                  UiHelper.ShowToast("twitter", "there was a problem with searching.");
                                              }
                                          });
                return;
            }

            Dispatcher.BeginInvoke(delegate
                                       {
                                           noResults.Visibility = Visibility.Collapsed;
                                           lstSearchResults.Visibility = Visibility.Visible;

                                           foreach (var o in searchResult.statuses)
                                           {
                                               var viewModel = o.AsViewModel(accountId);

                                               if (ViewModel != null && ViewModel.Results != null)
                                                   ViewModel.Results.Add(viewModel);
                                           }

                                           UiHelper.HideProgressBar();
                                       });
        }

        private void lstSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (lstSearchResults.SelectedIndex == -1)
                return;

            var item = lstSearchResults.SelectedValue as TimelineViewModel;
            var query = "accountId=" + AccountId + "&id=" + item.Id;

            if (item.MediaUrl != null)
                query += "&photo=true";

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/DetailsPage.xaml?" + query, UriKind.Relative));

            // Reset selected index to -1 (no selection)
            lstSearchResults.SelectedIndex = -1;
        }

        private void mnuRefresh_Click(object sender, EventArgs e)
        {

            if (ViewModel.Results == null)
                return;

            if (ViewModel.Results != null)
                ViewModel.Results.Clear();

            PerformSearch(true);
        }

        private void mnuSaveSearch_Click(object sender, EventArgs e)
        {
            UiHelper.ShowProgressBar("saving search");

            ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = false;

            var api = new TwitterApi(AccountId);
            api.SaveSearchCompletedEvent += api_SaveSearchCompletedEvent;
            api.SaveSearch(_query);
        }

        private void api_SaveSearchCompletedEvent(object sender, EventArgs e)
        {
            UiHelper.HideProgressBar();

            UiHelper.SafeDispatch(() =>
                                      {
                                          UiHelper.ShowToast("search has been saved");
                                          ((ApplicationBarIconButton)ApplicationBar.Buttons[1]).IsEnabled = true;
                                      });
        }


        private void mnuCompose_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(
                new Uri(
                    "/NewTweet.xaml?accountId=" + AccountId + "&text=" +
                    HttpUtility.UrlEncodeUnicode(_query.Replace("\"", "")), UriKind.Relative));
        }

        private void mnuPin_Click(object sender, EventArgs e)
        {
            var newUrl = "/SearchResults.xaml?accountId=" + AccountId + "&term=" + HttpUtility.UrlEncodeUnicode(_query);

            var tile =
                ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().ToLower() == newUrl.ToLower());

            if (tile != null)
            {
                tile.Delete();
            }

            var secondaryTile = new StandardTileData
                                    {
                                        BackgroundImage = new Uri("/Background-Search.png", UriKind.RelativeOrAbsolute),
                                        Title = _query,
                                        BackTitle = ApplicationConstants.ApplicationName,
                                        BackContent = "search for " + _query
                                    };

            ShellTile.Create(new Uri(newUrl, UriKind.Relative), secondaryTile);
        }

        private void mnuEditRetweet_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            if (menuItem == null || menuItem.Tag == null)
                return;

            var tag = menuItem.Tag.ToString(); // this has the description

            var newTag = HttpUtility.UrlEncode(tag);

            NavigationService.Navigate(new Uri(
                                           "/NewTweet.xaml?accountId=" + AccountId + "&isEditRt=true&text=" + newTag,
                                           UriKind.Relative));
        }

        private void mnuFavourite_Click(object sender, RoutedEventArgs e)
        {
        }

        private void mnuReply_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            if (menuItem == null || menuItem.Tag == null)
                return;

            var tag = menuItem.Tag.ToString();

            NavigationService.Navigate(new Uri("/NewTweet.xaml?" + tag, UriKind.Relative));
        }

        private void mnuRetweet_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            if (menuItem == null || menuItem.Tag == null)
                return;

            var tag = menuItem.Tag as TimelineViewModel; // this is the viewmodel

            UiHelper.ShowProgressBar("retweeting");

            var api = new TwitterApi(AccountId);
            api.RetweetCompletedEvent += delegate
                                             {
                                                 UiHelper.HideProgressBar();
                                                 UiHelper.ShowToast("tweet retweeted!");
                                             };
            api.Retweet(tag.Id);
        }

        private void mnuProfile_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var userScreen = menuItem.Tag;

            NavigationService.Navigate(new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + userScreen,
                                               UriKind.Relative));
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

            IList groups = VisualStateManager.GetVisualStateGroups(element);
            foreach (VisualStateGroup group in groups)
                if (group.Name == name)
                    return group;

            return null;
        }

        #endregion
    }
}