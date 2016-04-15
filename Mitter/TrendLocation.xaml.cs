using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.Classes;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;
using Mitter.Classes;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Mitter
{

    public partial class TrendLocation : AnimatedBasePage
    {

        public bool LoadingData { get; set; }
        private long AccountId { get; set; }
        private bool DataLoaded { get; set; }
        private bool Loading { get; set; }
        private List<KeyedList<CollectionDataItemViewModel, DataItemViewModel>> ViewModel { get; set; }

        public TrendLocation()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Loading = true;

            if (!UiHelper.ValidateUser())
            {
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                return;
            }

            CreateCountryControl();

            AccountId = UiHelper.GetAccountId(NavigationContext);

            SetExistingValues();

            Loading = false;

        }

        private LongListSelector lstCountries;

        private void CreateCountryControl()
        {

            lstCountries = new LongListSelector()
                           {
                               Margin = new Thickness(12, 0, 12, 0),
                               GroupHeaderTemplate = Resources["headerTemplate"] as DataTemplate,
                               ItemTemplate = Resources["itemTemplate"] as DataTemplate,
                               Opacity = 0                               
                           };

            Grid.SetRow(lstCountries, 2);

            //lstCountries.SelectionChanged += lstCountries_SelectionChanged;
            //lstCountries.Tap += lstCountries_SelectionChanged;

            lstCountries.RenderTransform = new TranslateTransform() { X = 0 };

#if WP8
            lstCountries.IsGroupingEnabled = true;
            lstCountries.LayoutMode = LongListSelectorLayoutMode.List;
#endif

            LayoutRoot.Children.Add(lstCountries);

        }

        private void SetExistingValues()
        {

            var sh = new SettingsHelper();
            var useCurrentLocation = sh.GetUseLocationForTrends();
            toggleLocal.IsChecked = useCurrentLocation;

            if (!useCurrentLocation)
            {
                GetLocations();
            }

        }

        private void StartGetLocations(object state)
        {
            DataLoaded = true;

            UiHelper.SafeDispatchSync(() =>
            {
                toggleLocal.IsEnabled = false;
            });

            var api = new TwitterApi(AccountId);
            api.GetAvailableTrendLocationsCompletedEvent += api_GetAvailableTrendLocationsCompletedEvent;
            api.GetAvailableTrendLocations();
        }

        void api_GetAvailableTrendLocationsCompletedEvent(object sender, EventArgs e)
        {

            var api = sender as TwitterApi;

            var items = new List<DataItemViewModel>();

            UiHelper.SafeDispatch(() =>
            {

                LoadingData = true;

                var worldItem = new CollectionDataItemViewModel("Worldwide", 1, null);
                var worldModel = new DataItemViewModel() { Title = "Worldwide", WoeId = 1, ParentObject = worldItem };
                worldModel.PropertyChanged += propertyChanged;
                items.Add(worldModel);

                foreach (var item in api.AvailableTrendLocation.Where(x => x.parentid == 1))
                {
                    var newCollection = new CollectionDataItemViewModel(item.name, item.woeid, item.parentid);

                    var parentModel = new DataItemViewModel() { Title = item.name, WoeId = item.woeid, ParentObject = newCollection };
                    parentModel.PropertyChanged += propertyChanged;
                    items.Add(parentModel);

                    foreach (var childItems in api.AvailableTrendLocation.Where(x => x.parentid.HasValue && x.parentid == item.woeid))
                    {
                        var newModel = new DataItemViewModel() { Title = childItems.name, WoeId = childItems.woeid, ParentObject = newCollection };
                        newModel.PropertyChanged += propertyChanged;
                        items.Add(newModel);
                    }
                }

                using (var dc = new MainDataContext())
                {
                    if (dc.TrendLocationTable.Any())
                    {
                        foreach (var item in items)
                        {
                            if (dc.TrendLocationTable.Any(x => x.WoeId == item.WoeId))
                                item.IsSelected = true;
                        }
                    }
                }

                var res = from feedItem in items
                          orderby feedItem.ParentObject.ParentId
                          group feedItem by feedItem.ParentObject
                              into groupedItems
                              select new KeyedList<CollectionDataItemViewModel, DataItemViewModel>(groupedItems);

                ViewModel = new List<KeyedList<CollectionDataItemViewModel, DataItemViewModel>>(res);
                
                lstCountries.ItemsSource = ViewModel;

                LoadingData = false;

                txtLoading.Visibility = Visibility.Collapsed;

                DispatcherTimer dt = new DispatcherTimer();
                dt.Interval = TimeSpan.FromMilliseconds(400);
                dt.Tick += delegate
                           {
                               dt.Stop();
                               FadeCountriesOn();
                           };
                dt.Start();

                UiHelper.HideProgressBar();

                UiHelper.SafeDispatchSync(() =>
                {
                    toggleLocal.IsEnabled = true;
                });

            });

        }

        private void FadeCountriesOff()
        {

            var sb = new Storyboard();

            sb.Children.Add(CreateAnimation(1, 0, 0.4, new PropertyPath(OpacityProperty), lstCountries, TimeSpan.FromSeconds(0)));
            sb.Children.Add(CreateAnimation(0, -100, 0.4, new PropertyPath(TranslateTransform.XProperty), lstCountries.RenderTransform, TimeSpan.FromSeconds(0)));

            sb.Duration = new Duration(TimeSpan.FromSeconds(0.4));
            sb.Begin();

        }

        private void FadeCountriesOn()
        {

            var sb = new Storyboard();

            sb.Children.Add(CreateAnimation(0, 1, 0.4, new PropertyPath(OpacityProperty), lstCountries, TimeSpan.FromSeconds(0)));
            sb.Children.Add(CreateAnimation(100, 0, 0.4, new PropertyPath(TranslateTransform.XProperty), lstCountries.RenderTransform, TimeSpan.FromSeconds(0)));

            sb.Duration = new Duration(TimeSpan.FromSeconds(0.4));
            sb.Begin();

        }

        private static DoubleAnimation CreateAnimation(double from, double to, double duration,
                                               PropertyPath targetProperty, DependencyObject target, TimeSpan? beginTime)
        {
            var db = new DoubleAnimation
            {
                To = to,
                From = from,
                EasingFunction = new SineEase(),
                Duration = TimeSpan.FromSeconds(duration),
                BeginTime = beginTime
            };
            Storyboard.SetTarget(db, target);
            Storyboard.SetTargetProperty(db, targetProperty);
            return db;
        }


        private void ToggleLocal_OnChecked(object sender, RoutedEventArgs e)
        {

            if (Loading)
                return;

            bool useLocation = toggleLocal.IsChecked.HasValue && toggleLocal.IsChecked.Value;

            ThreadPool.QueueUserWorkItem(SaveToggleChange, useLocation);

            if (useLocation)
            {
                FadeCountriesOff();
            }
            else
            {
                if (lstCountries.DataContext == null)
                {
                    GetLocations();
                }
                else
                {
                    FadeCountriesOn();
                }
            }

        }

        private void SaveToggleChange(object state)
        {
            var useLocation = (bool)state;
            var sh = new SettingsHelper();
            sh.SetUseLocationForTrends(useLocation);
        }

        private void GetLocations()
        {
            UiHelper.ShowProgressBar();
            txtLoading.Visibility = Visibility.Visible;
            lstCountries.Opacity = 0;
            ThreadPool.QueueUserWorkItem(StartGetLocations);
        }

        //private void lstCountries_SelectionChanged(object sender, GestureEventArgs gestureEventArgs)
        //{

        //    var list = sender as LongListSelector;
            
        //    var item = list.SelectedItem as DataItemViewModel;

        //    if (item == null)
        //        return;

        //    if (item.IsSelected)
        //        item.IsSelected = false;
        //    else
        //        item.IsSelected = true;

        //    gestureEventArgs.Handled = true;

        //    list.SelectedItem = null;

        //    ThreadPool.QueueUserWorkItem(SaveSelectionChange, item);

        //}

        private void propertyChanged(object sender, PropertyChangedEventArgs e)
        {

            if (LoadingData)
                return;

            var item = sender as DataItemViewModel;
            ThreadPool.QueueUserWorkItem(SaveSelectionChange, item);
        }


        private void SaveSelectionChange(object state)
        {
            var item = state as DataItemViewModel;

            if (item == null)
                return;

            if (item.IsSelected)
            {
                // add
                using (var dc = new MainDataContext())
                {
                    var firstItem = dc.TrendLocationTable.FirstOrDefault(x => x.WoeId == item.WoeId);

                    if (firstItem == null)
                    {

                        dc.TrendLocationTable.InsertOnSubmit(new TrendLocationTable()
                        {
                            Name = item.Title,
                            WoeId = item.WoeId
                        });
                    }

                    dc.SubmitChanges();
                }
            }
            else
            {
                // remove
                using (var dc = new MainDataContext())
                {
                    var firstItem = dc.TrendLocationTable.FirstOrDefault(x => x.WoeId == item.WoeId);

                    if (firstItem != null)
                    {
                        dc.TrendLocationTable.DeleteOnSubmit(firstItem);
                    }

                    dc.SubmitChanges();
                }
            }

        }

    }

}