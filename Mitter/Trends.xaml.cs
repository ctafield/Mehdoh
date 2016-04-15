using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Mitter
{

    public partial class Trends : AnimatedBasePage
    {

        private long AccountId { get; set; }
        private bool IsFirstGeoLocation { get; set; }
        private GeoCoordinateWatcher LocationWatcher { get; set; }
        private FieldOfTweets.Common.Api.Twitter.Responses.TrendLocation CurrentLocation { get; set; }

        private Dictionary<int, ObservableCollection<TrendViewModel>> CustomTrends { get; set; }

        private long WorldwideTrendWoeId
        {
            get { return 1; }
        }

        private bool Refreshed { get; set; }

        protected int RefreshingCount { get; set; }

        public TrendsViewModel ViewModel { get; set; }

        public Trends()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            ViewModel = new TrendsViewModel();

            this.DataContext = ViewModel;
        }

        protected override void AnimationsComplete(AnimationType animationType)
        {

            switch (animationType)
            {
                case AnimationType.NavigateForwardIn:
                    if (!Refreshed)
                    {
                        RefreshItems();
                        Refreshed = true;
                    }
                    break;

                case AnimationType.NavigateBackwardIn:
                    //reset list so you can select the same element again
                    if (pivotMain != null && pivotMain.SelectedItem != null)
                    {
                        var currentPivotIn = ((PivotItem) pivotMain.SelectedItem).Content as ListBox;
                        if (currentPivotIn != null)
                            currentPivotIn.SelectedIndex = -1;
                    }
                    break;
            }

        }


        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {

            switch (animationType)
            {
                case AnimationType.NavigateBackwardOut:
                    return new SlideDownAnimator() { RootElement = LayoutRoot };

                case AnimationType.NavigateBackwardIn:
                    if (pivotMain.SelectedItem != null)
                    {
                        var currentPivotIn = ((PivotItem) pivotMain.SelectedItem).Content as ListBox;
                        if (currentPivotIn != null)
                            return GetContinuumAnimation(currentPivotIn.ItemContainerGenerator.ContainerFromIndex(currentPivotIn.SelectedIndex) as FrameworkElement, animationType);
                    }
                    break;

                case AnimationType.NavigateForwardOut:
                    if (pivotMain.SelectedItem != null)
                    {
                        var currentPivotOut = ((PivotItem) pivotMain.SelectedItem).Content as ListBox;
                        if (currentPivotOut != null)
                            return GetContinuumAnimation(currentPivotOut.ItemContainerGenerator.ContainerFromIndex(currentPivotOut.SelectedIndex) as FrameworkElement, animationType);
                    }
                    break;
            }

            return base.GetAnimation(animationType, toOrFrom);
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

        private void RefreshItems()
        {

            var sh = new SettingsHelper();
            if (sh.GetUseLocationForTrends())
            {                
                SystemTray.ProgressIndicator = new ProgressIndicator
                {
                    IsVisible = true,
                    IsIndeterminate = true,
                    Text = "finding location"
                };
                
                ThreadPool.QueueUserWorkItem(FindLocations);
            }
            else
            {

                BuildCustomTrends();

            }

        }

        #region Location Based Trends

        private void FindLocations(object state)
        {
           
            UiHelper.SafeDispatchSync(() => stackWait.Visibility = Visibility.Visible);

            StartGetLocation();
        }

        private void StartGetLocation()
        {
            IsFirstGeoLocation = true;

            LocationWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);

            LocationWatcher.StatusChanged += LocationWatcher_StatusChanged;
            LocationWatcher.PositionChanged += LocationWatcher_PositionChanged;
            LocationWatcher.Start();
        }

        void LocationWatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {

            if (LocationWatcher.Status == GeoPositionStatus.Disabled)
            {
                LocationWatcher.Stop();

                // disabled, so just get the world one
                UiHelper.SafeDispatch(BuildPivots);
                UiHelper.HideProgressBar();
                RefreshWorldTrends();                
            }            
        }

        private void LocationWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {

            //if (IsFirstGeoLocation)
            //    return;

            //IsFirstGeoLocation = false;

            LocationWatcher.Stop();

            var api = new TwitterApi(AccountId);
            api.GetClosestTrendLocationCompletedEvent += new EventHandler(api_GetClosestTrendsCompletedEvent);
            api.GetClosestTrendLocation(e.Position.Location.Longitude, e.Position.Location.Latitude);
        }

        private void api_GetClosestTrendsCompletedEvent(object sender, EventArgs e)
        {

            UiHelper.HideProgressBar();

            var api = sender as TwitterApi;
            if (api == null)
                return;

            api.GetClosestTrendLocationCompletedEvent -= api_GetClosestTrendsCompletedEvent;

            var locations = api.ClosestTrendLocation;

            if (locations == null)
                return;

            CurrentLocation = locations.FirstOrDefault();

            if (CurrentLocation == null)
                return;

            // OK now we have the locations, show the screen
            UiHelper.SafeDispatch(BuildPivots);            

            RefreshWorldTrends();
        }

        private void BuildPivots()
        {

            if (CurrentLocation != null)
            {

                // add country location?
                if (CurrentLocation.parentid.HasValue)
                {
                    var countryPivot = new PivotItem()
                                           {
                                               Header = CurrentLocation.country.ToLower(),
                                               Name = "pivotCountry"
                                           };

                    var countryListBox = new ListBox
                                             {
                                                 ItemTemplate = Resources["trendsTemplate"] as DataTemplate,
                                                 ItemsSource = ViewModel.CountryTrends
                                             };
                    countryListBox.SelectionChanged += lstDaily_SelectionChanged;
                    countryPivot.Content = countryListBox;

                    pivotMain.Items.Add(countryPivot);
                }

                // add current location?
                var currentPivot = new PivotItem()
                                       {
                                           Header = CurrentLocation.name.ToLower(),
                                           Name = "pivotLocal"
                                       };

                var currentListBox = new ListBox
                                         {
                                             ItemTemplate = Resources["trendsTemplate"] as DataTemplate,
                                             ItemsSource = ViewModel.LocalTrends
                                         };
                currentListBox.SelectionChanged += lstDaily_SelectionChanged;
                currentPivot.Content = currentListBox;

                pivotMain.Items.Add(currentPivot);

            }

            stackWait.Visibility = Visibility.Collapsed;
            pivotMain.Visibility = Visibility.Visible;

        }

        private void CurrentTrendsToViewModel(long accountId, IEnumerable<ResponseTrend> currentTrends)
        {

            if (currentTrends == null)
                return;

            ViewModel.WorldwideTrends.Clear();

            foreach (var o in currentTrends)
            {
                ViewModel.WorldwideTrends.Add(new TrendViewModel()
                                      {
                                          AccountId = accountId,
                                          Name = o.name,
                                          Events = o.events,
                                          Promoted = o.promoted_content,
                                          Query = o.query
                                      });
            }

        }

        private void CountryTrendsToViewModel(long accountId, IEnumerable<ResponseTrend> currentTrends)
        {

            if (currentTrends == null)
                return;

            ViewModel.CountryTrends.Clear();

            foreach (var o in currentTrends)
            {
                ViewModel.CountryTrends.Add(new TrendViewModel()
                {
                    AccountId = accountId,
                    Name = o.name,
                    Events = o.events,
                    Promoted = o.promoted_content,
                    Query = o.query
                });
            }

        }

        private void LocalTrendsToViewModel(long accountId, IEnumerable<ResponseTrend> currentTrends)
        {

            if (currentTrends == null)
                return;

            ViewModel.LocalTrends.Clear();

            foreach (var o in currentTrends)
            {
                ViewModel.LocalTrends.Add(new TrendViewModel()
                                      {
                                          AccountId = accountId,
                                          Name = o.name,
                                          Events = o.events,
                                          Promoted = o.promoted_content,
                                          Query = o.query
                                      });
            }

        }

        private void FinishedRefreshingTask()
        {

            RefreshingCount--;

            if (RefreshingCount <= 0)
            {
                UiHelper.HideProgressBar();
            }
        }

        private void lstDaily_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

            var list = sender as ListBox;
            if (list.SelectedIndex == -1)
                return;

            var item = list.SelectedItem as TrendViewModel;
            NavigationService.Navigate(item.NavigateUri);

            //list.SelectedIndex = -1;
        }


        private void mnuPin_Click(object sender, EventArgs e)
        {

            var newUrl = "/Trends.xaml?accountId=" + AccountId;

            Uri backgroundImageUri = new Uri("/trends-current.png", UriKind.RelativeOrAbsolute);

            var tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().ToLower() == newUrl.ToLower());

            if (tile != null)
            {
                tile.Delete();
            }

            var secondaryTile = new StandardTileData
            {
                BackgroundImage = backgroundImageUri,
                Title = ApplicationConstants.ApplicationName
            };

            ShellTile.Create(new Uri(newUrl, UriKind.Relative), secondaryTile);

        }

        private void PivotMain_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var currentPivot = ((PivotItem) (pivotMain.SelectedItem)).Name;

            switch (currentPivot)
            {
                case "pivotWorld":
                    if (ViewModel.WorldwideTrends.Count > 0)
                        break;
                    RefreshWorldTrends();
                    break;

                case "pivotCountry":
                    if (ViewModel.CountryTrends.Count > 0)
                        break;
                    RefreshCountryTrends();
                    break;

                case "pivotLocal":
                    if (ViewModel.LocalTrends.Count > 0)
                        break;
                    RefreshLocalTrends();
                    break;
            }

        }

        private void RefreshWorldTrends()
        {
            var api = new TwitterApi(AccountId);
            api.GetCurrentTrendsByWoeidCompletedEvent += api_GetWorldTrendsCompletedEvent;
            api.GetCurrentTrendsByWoeid(WorldwideTrendWoeId);
        }

        void api_GetWorldTrendsCompletedEvent(object sender, EventArgs e)
        {

            var api = sender as TwitterApi;

            if (api != null)
            {
                api.GetCurrentTrendsByWoeidCompletedEvent -= api_GetWorldTrendsCompletedEvent;
            }

            if (api != null && api.HasError)
            {
                if (!string.IsNullOrEmpty(api.ErrorMessage))
                    UiHelper.ShowToast(api.ErrorMessage);
                else
                    UiHelper.ShowToast("There was a problem connecting to Twitter.");

                return;
            }

            Dispatcher.BeginInvoke(() => CurrentTrendsToViewModel(api.AccountId, api.CurrentTrends));
            
        }

        private void RefreshLocalTrends()
        {
            var api = new TwitterApi(AccountId);
            api.GetCurrentTrendsByWoeidCompletedEvent += api_GetLocalTrendsCompletedEvent;
            api.GetCurrentTrendsByWoeid(CurrentLocation.woeid);            
        }

        void api_GetLocalTrendsCompletedEvent(object sender, EventArgs e)
        {

            var api = sender as TwitterApi;

            if (api != null)
            {
                api.GetCurrentTrendsByWoeidCompletedEvent -= api_GetLocalTrendsCompletedEvent;
            }

            if (api != null && api.HasError)
            {
                if (!string.IsNullOrEmpty(api.ErrorMessage))
                    UiHelper.ShowToast(api.ErrorMessage);
                else
                    UiHelper.ShowToast("There was a problem connecting to Twitter.");

                return;
            }

            Dispatcher.BeginInvoke(() => LocalTrendsToViewModel(api.AccountId, api.CurrentTrends));

        }


        private void RefreshCountryTrends()
        {
            if (!CurrentLocation.parentid.HasValue)
                return;

            var api = new TwitterApi(AccountId);
            api.GetCurrentTrendsByWoeidCompletedEvent += api_GetCountryTrendsCompletedEvent;
            api.GetCurrentTrendsByWoeid(CurrentLocation.parentid.Value);                
        }

        private void api_GetCountryTrendsCompletedEvent(object sender, EventArgs e)
        {

            var api = sender as TwitterApi;

            if (api != null)
            {
                api.GetCurrentTrendsByWoeidCompletedEvent -= api_GetCountryTrendsCompletedEvent;
            }

            if (api != null && api.HasError)
            {
                if (!string.IsNullOrEmpty(api.ErrorMessage))
                    UiHelper.ShowToast(api.ErrorMessage);
                else
                    UiHelper.ShowToast("There was a problem connecting to Twitter.");

                return;
            }

            Dispatcher.BeginInvoke(() => CountryTrendsToViewModel(api.AccountId, api.CurrentTrends));

        }

        #endregion

        #region Custom Trends

        private void BuildCustomTrends()
        {

            CustomTrends = new Dictionary<int, ObservableCollection<TrendViewModel>>();

            pivotMain.Items.Clear();

            using (var dc = new MainDataContext())
            {

                if (!dc.TrendLocationTable.Any())
                {
                    // todo: display message teling user to configure
                }
                else
                {
                    foreach (var table in dc.TrendLocationTable)
                    {
                        var newPivot = new PivotItem();
                        newPivot.Header = table.Name.ToLowerInvariant();

                        var thisList = new ObservableCollection<TrendViewModel>();

                        var listBox = new ListBox
                                      {
                                          ItemTemplate = Resources["trendsTemplate"] as DataTemplate, 
                                          ItemsSource = thisList
                                      };
                        listBox.SelectionChanged += lstDaily_SelectionChanged;
                        listBox.Visibility = Visibility.Collapsed;

                        var textBox = new TextBlock()
                                      {
                                          Margin = new Thickness(12),
                                          Foreground = Resources["PhoneSubtleBrush"] as SolidColorBrush,
                                          FontSize = 26,
                                          Text = "please wait while mehdoh fetches the trends",
                                          TextWrapping = TextWrapping.Wrap
                                      };

                        var stackPanel = new StackPanel();

                        stackPanel.Children.Add(listBox);
                        stackPanel.Children.Add(textBox);

                        newPivot.Content = stackPanel;

                        pivotMain.Items.Add(newPivot);

                        CustomTrends.Add(table.WoeId, thisList);

                        var api = new TwitterApi(AccountId);
                        api.GetCurrentTrendsByWoeidCompletedEvent += delegate(object sender, EventArgs args)
                                                                     {
                                                                         Dispatcher.BeginInvoke(() =>
                                                                                                {
                                                                                                    var twitterApi = sender as TwitterApi;
                                                                                                    
                                                                                                    listBox.Visibility = Visibility.Visible;
                                                                                                    textBox.Visibility = Visibility.Collapsed;

                                                                                                    if (twitterApi.HasError)
                                                                                                    {
                                                                                                        if (!string.IsNullOrEmpty(api.ErrorMessage))
                                                                                                            UiHelper.ShowToast(api.ErrorMessage);
                                                                                                        else
                                                                                                            UiHelper.ShowToast("There was a problem connecting to Twitter.");
                                                                                                        return;
                                                                                                    }

                                                                                                    var currentTrends = twitterApi.CurrentTrends;

                                                                                                    if (currentTrends == null)
                                                                                                    {
                                                                                                        UiHelper.ShowToast("There was a problem connecting to Twitter.");
                                                                                                        return;
                                                                                                    }
                                                                                                    
                                                                                                    foreach (var o in currentTrends)
                                                                                                    {
                                                                                                        CustomTrends[table.WoeId].Add(new TrendViewModel()
                                                                                                        {
                                                                                                            AccountId = AccountId,
                                                                                                            Name = o.name,
                                                                                                            Events = o.events,
                                                                                                            Promoted = o.promoted_content,
                                                                                                            Query = o.query
                                                                                                        });
                                                                                                    }

                                                                                                });
                                                                     };
                        api.GetCurrentTrendsByWoeid(table.WoeId);

                    }

                    pivotMain.Visibility = Visibility.Visible;
                }
                
            }

        }

        #endregion

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/TrendLocation.xaml?accountId=" + AccountId, UriKind.Relative));
        }

    }

}