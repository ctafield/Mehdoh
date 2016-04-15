// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.Responses;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.Animations.Page.Extensions;
using FieldOfTweets.Common.UI.Search;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;

#if WP7
using Microsoft.Phone.Controls.Maps;
#endif

#if WP8
using Microsoft.Phone.Maps.Controls;
#endif

using Microsoft.Phone.Shell;

#endregion

namespace Mitter
{
    public partial class SearchPage : AnimatedBasePage
    {
        #region Constructor

        public SearchPage()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;

            RecentPeopleSearch = new ObservableCollection<string>();
            RecentTextSearch = new ObservableCollection<string>();
            SavedSearches = new ObservableCollection<ResponseGetSavedSearch>();

            lstRecentPeople.Opacity = 0;
            lstRecentPeople.DataContext = RecentPeopleSearch;

            lstRecentText.Opacity = 0;
            lstRecentText.DataContext = RecentTextSearch;

            lstSavedSearches.Opacity = 0;
            lstSavedSearches.DataContext = SavedSearches;

            LocalSearchResults = new SortedObservableCollection<SearchResultViewModel>();
            listLocal.DataContext = LocalSearchResults;


            NeedsRepaintLocal = true;

            SetMapControl();

            StartGetLocation();
        }

        #endregion

        #region Properties

        private bool LocalSearchPopulated { get; set; }
        private bool SavedSearchPopulated { get; set; }
        private bool NeedsRepaintLocal { get; set; }

        private SortedObservableCollection<SearchResultViewModel> LocalSearchResults { get; set; }

        private ObservableCollection<string> RecentPeopleSearch { get; set; }
        private ObservableCollection<string> RecentTextSearch { get; set; }
        private ObservableCollection<ResponseGetSavedSearch> SavedSearches { get; set; }

        private GeoCoordinate Position { get; set; }
        private GeoCoordinateWatcher LocationWatcher { get; set; }

        private long AccountId { get; set; }
        protected string Latitude { get; set; }
        protected string Longitude { get; set; }
        protected bool InitialisedEvents { get; set; }
        protected bool IsFetchingMore { get; set; }

        private Map SearchMap { get; set; }

        #endregion

        private void SetMapControl()
        {
            SearchMap = new Map { IsEnabled = false };

#if WP7
            SearchMap.CredentialsProvider = new ApplicationIdCredentialsProvider(ApplicationConstants.BingMapsApiKey);
#endif

            Grid.SetRow(SearchMap, 0);
            gridMap.Children.Add(SearchMap);


        }

        #region Overrides

        protected override void AnimationsComplete(AnimationType animationType)
        {
            switch (animationType)
            {
                case AnimationType.NavigateForwardIn:
                    ThreadPool.QueueUserWorkItem(GetData);
                    break;

                case AnimationType.NavigateBackwardIn:
                    //reset list so you can select the same element again
                    lstRecentPeople.SelectedIndex = -1;
                    lstRecentText.SelectedIndex = -1;
                    lstSavedSearches.SelectedIndex = -1;
                    break;
            }
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
                    switch (pivotMain.SelectedIndex)
                    {
                        case 0: // tweets
                            if (lstRecentText.SelectedIndex != -1)
                                return
                                    GetContinuumAnimation(
                                        lstRecentText.ItemContainerGenerator.ContainerFromIndex(
                                            lstRecentText.SelectedIndex) as FrameworkElement, animationType);
                            else
                                return new SlideUpAnimator { RootElement = LayoutRoot };
                        case 1: // people
                            if (lstRecentPeople.SelectedIndex != -1)
                                return
                                    GetContinuumAnimation(
                                        lstRecentPeople.ItemContainerGenerator.ContainerFromIndex(
                                            lstRecentPeople.SelectedIndex) as FrameworkElement, animationType);
                            else
                                return new SlideUpAnimator { RootElement = LayoutRoot };
                        case 2: // saved
                            if (lstSavedSearches.SelectedIndex != -1)
                                return
                                    GetContinuumAnimation(
                                        lstSavedSearches.ItemContainerGenerator.ContainerFromIndex(
                                            lstSavedSearches.SelectedIndex) as FrameworkElement, animationType);
                            else
                                return new SlideUpAnimator { RootElement = LayoutRoot };
                    }
                    break;

                case AnimationType.NavigateForwardOut:
                    switch (pivotMain.SelectedIndex)
                    {
                        case 0: // tweets
                            if (lstRecentText.SelectedIndex != -1)
                                return
                                    GetContinuumAnimation(
                                        lstRecentText.ItemContainerGenerator.ContainerFromIndex(
                                            lstRecentText.SelectedIndex) as FrameworkElement, animationType);
                            else
                                return new SlideDownAnimator { RootElement = LayoutRoot };
                        case 1: // people
                            if (lstRecentPeople.SelectedIndex != -1)
                                return
                                    GetContinuumAnimation(
                                        lstRecentPeople.ItemContainerGenerator.ContainerFromIndex(
                                            lstRecentPeople.SelectedIndex) as FrameworkElement, animationType);
                            else
                                return new SlideDownAnimator { RootElement = LayoutRoot };
                        case 2: // saved
                            if (lstSavedSearches.SelectedIndex != -1)
                                return
                                    GetContinuumAnimation(
                                        lstSavedSearches.ItemContainerGenerator.ContainerFromIndex(
                                            lstSavedSearches.SelectedIndex) as FrameworkElement, animationType);
                            else
                                return new SlideDownAnimator { RootElement = LayoutRoot };
                    }
                    break;
            }

            return null;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!UiHelper.ValidateUser())
            {
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                return;
            }

            AccountId = UiHelper.GetAccountId(NavigationContext);
        }

        #endregion

        #region Members

        private void GetData(object state)
        {
            UiHelper.SafeDispatch(() =>
                                      {
                                          var sh = new SearchHelper();
                                          RecentPeopleSearch.AddRange(sh.GetRecentPeopleSearches());
                                          RecentTextSearch.AddRange(sh.GetRecentTextSearches());

                                          if (pivotMain.SelectedIndex == 0)
                                          {
                                              lstRecentTextFadeIn.Begin();
                                              lstRecentPeople.Opacity = 1;
                                          }
                                          else if (pivotMain.SelectedIndex == 1)
                                          {
                                              lstPeopleTextFadeIn.Begin();
                                              lstRecentText.Opacity = 1;
                                          }
                                      });
        }

        private void txtName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchPerson();
            }
        }

        private void txtTweets_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchText();
            }
        }

        private void txtName_ActionIconTapped(object sender, EventArgs e)
        {
            SearchPerson();
        }

        private void txtTweets_ActionIconTapped(object sender, EventArgs e)
        {
            SearchText();
        }

        private void SearchPerson()
        {
            var sh = new SearchHelper();
            var searchPhrase = txtName.Text.Trim();
            sh.AddToPeopleSearchRecents(searchPhrase);
            NavigationService.Navigate(new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + HttpUtility.UrlEncodeUnicode(searchPhrase), UriKind.Relative));
        }

        private void SearchText()
        {
            var sh = new SearchHelper();
            var searchPhrase = txtTweets.Text.Trim();
            sh.AddToTextSearchRecents(searchPhrase);
            NavigationService.Navigate(new Uri("/SearchResults.xaml?accountId=" + AccountId + "&term=" + HttpUtility.UrlEncodeUnicode(searchPhrase), UriKind.Relative));
        }

        private void lstRecentPeople_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (lstRecentPeople.SelectedIndex == -1)
                return;

            var item = lstRecentPeople.SelectedValue as string;
            if (item == null)
                return;

            item = item.Replace("@", "");

            var sh = new SearchHelper();
            sh.AddToPeopleSearchRecents(item);
            NavigationService.Navigate(new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + HttpUtility.UrlEncodeUnicode(item), UriKind.Relative));

            //lstRecentPeople.SelectedIndex = -1;
        }

        private void lstRecentText_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;

            // If selected index is -1 (no selection) do nothing
            if (listBox == null || listBox.SelectedIndex == -1)
                return;

            var item = listBox.SelectedValue as string;
            if (item == null)
                return;

            var sh = new SearchHelper();
            sh.AddToTextSearchRecents(item);

            NavigationService.Navigate(new Uri("/SearchResults.xaml?accountId=" + AccountId + "&term=" + HttpUtility.UrlEncodeUnicode(item), UriKind.Relative));

            //listBox.SelectedIndex = -1;
        }

        private void mnuClear_Click(object sender, EventArgs e)
        {
            switch (pivotMain.SelectedIndex)
            {
                case 1:
                    // people
                    if (
                        MessageBox.Show("Are you sure you want to clear the recent people searches?", "clear recent",
                                        MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        lstPeopleTextFadeOut.Completed += delegate
                                                              {
                                                                  var sh = new SearchHelper();
                                                                  sh.ClearRecentPeopleSearch();
                                                                  lstRecentPeople.DataContext = null;
                                                              };
                        lstPeopleTextFadeOut.Begin();
                    }
                    break;

                case 0:
                    // text
                    if (
                        MessageBox.Show("Are you sure you want to clear the recent tweet searches?", "clear recent",
                                        MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        lstRecentTextFadeOut.Completed += delegate
                                                              {
                                                                  var sh = new SearchHelper();
                                                                  sh.ClearRecentTextSearch();
                                                                  lstRecentText.DataContext = null;
                                                              };
                        lstRecentTextFadeOut.Begin();
                    }
                    break;
            }
        }

        private void lstSavedSearches_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;

            // If selected index is -1 (no selection) do nothing
            if (listBox == null || listBox.SelectedIndex == -1)
                return;

            var item = listBox.SelectedValue as ResponseGetSavedSearch;
            if (item == null)
                return;

            NavigationService.Navigate(new Uri("/SearchResults.xaml?accountId=" + AccountId + "&term=" + HttpUtility.UrlEncodeUnicode(item.query), UriKind.Relative));
        }

        private void StartGetLocation()
        {
            LocationWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            LocationWatcher.PositionChanged += LocationWatcher_PositionChanged;
            LocationWatcher.Start();
        }

        private void LocationWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            Latitude = e.Position.Location.Latitude.ToString(CultureInfo.InvariantCulture);
            Longitude = e.Position.Location.Longitude.ToString(CultureInfo.InvariantCulture);
            Position = e.Position.Location;

            LocationWatcher.Stop();

            SearchMap.Center = Position;
            SearchMap.SetView(Position, 10);

            if (NeedsRepaintLocal)
                RefreshLocalSearch();
        }

        private void pivotMain_SelectedChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivotMain.SelectedIndex == 2)
            {
                RefreshSavedSearches();
            }
            else if (pivotMain.SelectedIndex == 3)
            {
                // find search
                if (!LocalSearchPopulated && Position != null)
                {
                    ShowLocalSearchResults();
                }
            }

            AssignCorrectMenu();
        }

        private async void RefreshSavedSearches()
        {
            if (!SavedSearchPopulated)
            {
                progressSearches.Visibility = Visibility.Visible;
                progressSearches.IsIndeterminate = true;

                // Do we have the searches?
                using (var dh = new MainDataContext())
                {
                    if (dh.Profiles.Any(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                    {
                        foreach (var profile in dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                        {
                            var api = new TwitterApi(profile.Id);
                            var result = await api.GetSavedSearches();
                            api_GetSavedSearchesCompletedEvent(result);
                        }
                    }
                }
            }
        }

        private void ShowLocalSearchResults()
        {
            NeedsRepaintLocal = false;

            SearchMap.Center = Position;
            SearchMap.SetView(Position, 10);

            RefreshLocalSearch();
        }

        private void CheckListViewEventsAreSet()
        {
            if (InitialisedEvents)
                return;

            var timelineSv = (ScrollViewer)FindElementRecursive(listLocal, typeof(ScrollViewer));
            if (timelineSv != null)
            {
                // Visual States are always on the first child of the control template 
                var element = VisualTreeHelper.GetChild(timelineSv, 0) as FrameworkElement;
                if (element != null)
                {
                    var group = FindVisualState(element, "ScrollStates");
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

            ThreadPool.QueueUserWorkItem(GetMoreSearchResults);
        }

        private async void GetMoreSearchResults(object state)
        {
            if (IsFetchingMore)
                return;

            UiHelper.ShowProgressBar("fetching more search results");

            IsFetchingMore = true;

            var api = new TwitterApi(AccountId);

            long maxId = 0;

            if (LocalSearchResults != null && LocalSearchResults.Any())
            {
                maxId = LocalSearchResults.Min(x => x.Id);
            }

            var result = await api.Search(Latitude, Longitude, 50, maxId);
            api_SearchCompletedEvent(AccountId, result, api.HasError, api.ErrorMessage);
        }

        private async void RefreshLocalSearch()
        {
            if (string.IsNullOrEmpty(Latitude) || string.IsNullOrEmpty(Longitude))
                return;

            var api = new TwitterApi(AccountId);
            var result = await api.Search(Latitude, Longitude, 50, 0);
            api_SearchCompletedEvent(AccountId, result, api.HasError, api.ErrorMessage);
        }

        private void api_SearchCompletedEvent(long accountId, ResponseSearch searchResults, bool hasError, string errorMessage)
        {

            if (hasError)
                return;

            LocalSearchPopulated = true;

            Dispatcher.BeginInvoke(() =>
                                       {
                                           foreach (var o in searchResults.statuses)
                                           {
                                               if (!LocalSearchResults.Any(x => x.Id == o.id))
                                                   LocalSearchResults.Add((new SearchResultViewModel
                                                                               {
                                                                                   Id = o.id,
                                                                                   Description = HttpUtility.HtmlDecode(o.text),
                                                                                   ImageUrl = o.user.profile_image_url,
                                                                                   CreatedAt = o.created_at,
                                                                                   UserId = o.user.id,
                                                                                   Client = o.source,
                                                                                   ScreenName = o.user.screen_name,
                                                                                   DisplayName = o.user.name,
                                                                                   AccountId = accountId
                                                                               }));
                                           }

                                           txtLocalSearching.Visibility = Visibility.Collapsed;
                                           listLocal.Visibility = Visibility.Visible;


                                           if (IsFetchingMore)
                                           {
                                               IsFetchingMore = false;
                                               UiHelper.HideProgressBar();
                                           }

                                           CheckListViewEventsAreSet();
                                       });
        }

        private void api_GetSavedSearchesCompletedEvent(List<ResponseGetSavedSearch> savedSearches)
        {

            SavedSearchPopulated = true;

            Dispatcher.BeginInvoke(() =>
                                       {
                                           progressSearches.Visibility = Visibility.Collapsed;
                                           progressSearches.IsIndeterminate = false;

                                           if (savedSearches != null)
                                           {
                                               SavedSearches.AddRange(savedSearches);

                                               if (pivotMain.SelectedIndex == 2)
                                               {
                                                   lstSavedSearchesFadeIn.Begin();
                                               }
                                               else
                                               {
                                                   lstSavedSearches.Opacity = 1;
                                               }
                                           }
                                       });
        }


        private void mnuPin_Click(object sender, EventArgs e)
        {
            const string newUrl = "/SearchPage.xaml";

            var tile =
                ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().ToLower() == newUrl.ToLower());

            if (tile != null)
            {
                tile.Delete();
            }

            var secondaryTile = new StandardTileData
                                    {
                                        BackgroundImage = new Uri("/Background-Search.png", UriKind.RelativeOrAbsolute),
                                        Title = ApplicationConstants.SearchTileName,
                                        BackTitle = ApplicationConstants.ApplicationName,
                                        BackContent = "search page"
                                    };

            ShellTile.Create(new Uri(newUrl, UriKind.Relative), secondaryTile);
        }

        private void listLocal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;

            // If selected index is -1 (no selection) do nothing
            if (listBox == null || listBox.SelectedIndex == -1)
                return;

            var item = listBox.SelectedValue as SearchResultViewModel;
            var query = "accountId=" + AccountId + "&id=" + item.Id;

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/DetailsPage.xaml?" + query, UriKind.Relative));

            // Reset selected index to -1 (no selection)
            listBox.SelectedIndex = -1;
        }

        private void AssignCorrectMenu()
        {
            string menuKey = string.Empty;

            switch (pivotMain.SelectedIndex)
            {
                case 0: // tweets
                    menuKey = "menuDelete";
                    break;

                case 1: // people
                    menuKey = "menuDelete";
                    break;

                case 2: // saved
                    menuKey = "menuBlank";
                    break;

                case 3: // local
                    menuKey = "menuLocal";
                    break;
            }

            if (!string.IsNullOrEmpty(menuKey))
                ApplicationBar = (ApplicationBar)Resources[menuKey];

            ApplicationBar.MatchOverriddenTheme();
        }

        private void mnuRefresh_Click(object sender, EventArgs e)
        {
            if (LocalSearchResults != null)
            {
                LocalSearchResults.Clear();
            }
            else
            {
                LocalSearchResults = new SortedObservableCollection<SearchResultViewModel>();
                listLocal.DataContext = LocalSearchResults;
            }

            RefreshLocalSearch();
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

        private void mnuDeleteSaved_Click(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;

            var savedQueryId = (long)menu.Tag;

            var api = new TwitterApi(AccountId);

            api.DeleteSavedSearchCompletedEvent += delegate
                                                       {
                                                           UiHelper.SafeDispatch(() =>
                                                           {
                                                               SavedSearches.Clear();
                                                               SavedSearchPopulated = false;
                                                               RefreshSavedSearches();
                                                           });
                                                       };

            api.DeleteSavedSearch(savedQueryId);


        }
    }
}