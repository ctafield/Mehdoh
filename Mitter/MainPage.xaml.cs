// <copyright file="MainPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>
// Mehdoh for Windows Phone
// </summary>
// 

#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Windows.Networking.PushNotifications;
using Windows.UI.Notifications;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Responses.Twitter;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.ErrorLogging;
using FieldOfTweets.Common.POCO;
using FieldOfTweets.Common.Settings;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Classes;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Friends;
using FieldOfTweets.Common.UI.Helpers;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.LockBase;
using FieldOfTweets.Common.UI.Resources;
using FieldOfTweets.Common.UI.ThirdPartyApi;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Mitter.Classes;
using Mitter.Helpers;
using Mitter.UserControls;
using Windows.Phone.Speech.VoiceCommands;

using Telerik.Windows.Controls;
using Environment = System.Environment;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

using Microsoft.Phone.Notification;

#if DEBUG
using System.Diagnostics;
#endif


//using Mitter.Animations;

#endregion

namespace Mitter
{

    public partial class MainPage : LockBasePage
    {

        private Dictionary<long, TwitterApi> StreamingApi { get; set; }

        private Pivot mainPivot { get; set; }

        private Popup postWelcomePopup { get; set; }
        private Popup popupWindow { get; set; }

        private SelectPivot pivotSelect { get; set; }

        private Dictionary<long, bool> RefreshingTimeline { get; set; }
        private Dictionary<long, bool> RefreshingMentions { get; set; }
        private Dictionary<long, bool> RefreshingMessages { get; set; }
        private Dictionary<long, bool> RefreshingFavourites { get; set; }
        private Dictionary<long, bool> RefreshingRetweetsOfMe { get; set; }
        private Dictionary<string, bool> RefreshingTwitterSearch { get; set; }
        private Dictionary<string, bool> RefreshingList { get; set; }

        private int RefreshingCount { get; set; }

        private bool PerformedInitialRefresh { get; set; }

        private StreamingSettings StreamingSettings { get; set; }

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            InitialisedEvents = false;

            ConfiguredEvents = new List<string>();

            RefreshingTimeline = new Dictionary<long, bool>();
            RefreshingMentions = new Dictionary<long, bool>();
            RefreshingMessages = new Dictionary<long, bool>();
            RefreshingFavourites = new Dictionary<long, bool>();
            RefreshingRetweetsOfMe = new Dictionary<long, bool>();
            RefreshingTwitterSearch = new Dictionary<string, bool>();
            RefreshingList = new Dictionary<string, bool>();

            // Streaming API 
            StreamingApi = new Dictionary<long, TwitterApi>();

            UiHelper.ImageHasBeenShared = false;

            Loaded += MainPage_Loaded;

            var dt = new DispatcherTimer
                         {
                             Interval = TimeSpan.FromMilliseconds(33)
                         };
            dt.Tick += delegate
                           {
                               try
                               {
                                   FrameworkDispatcher.Update();
                               }
                               catch
                               {
                               }
                           };
            dt.Start();

            App.SuspendStreamingEvent += App_SuspendStreamingEvent;
            App.ReigniteStreamingEvent += App_ReigniteStreamingEvent;
            App.FontSizeChangedEvent += App_FontSizeChangedEvent;
            App.ColumnsChangedEvent += App_ColumnsChangedEvent;

            OrientationChanged += MainPage_OrientationChanged;

#if WP8
            RegisterVoiceCommands();
#endif

            InteractionEffectManager.AllowedTypes.Add(typeof(RadDataBoundListBoxItem));


#if WP8

            //BindPushChannel();

            //// Holds the push channel that is created or found.
            //HttpNotificationChannel pushChannel;

            //// Try to find the push channel.
            //pushChannel = HttpNotificationChannel.Find(channelName);

            //// If the channel was not found, then create a new connection to the push service.
            //if (pushChannel == null)
            //{
            //    pushChannel = new HttpNotificationChannel(channelName);

            //    // Register for all the events before attempting to open the channel.
            //    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
            //    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

            //    pushChannel.Open();

            //    // Bind this new channel for Tile events.
            //    pushChannel.BindToShellTile();


            //}
            //else
            //{
            //    // The channel was already open, so just register for all the events.
            //    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
            //    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

            //    // Display the URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
            //    System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());
            //    //MessageBox.Show(String.Format("Channel Uri is {0}", pushChannel.ChannelUri.ToString()));

            //}
#endif

        }

        //private PushNotificationChannel channel { get; set; }

        //private async void BindPushChannel()
        //{
        //    channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();


        //    String serverUrl = "http://192.168.2.112:56608/RegisterClient/";

        //    // Create the web request.
        //    HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(serverUrl);
        //    webRequest.Method = "POST";
        //    webRequest.ContentType = "application/x-www-form-urlencoded";
        //    byte[] channelUriInBytes = System.Text.Encoding.UTF8.GetBytes("pushUri=" + channel.Uri);

        //    // Write the channel URI to the request stream.
        //    Stream requestStream = await webRequest.GetRequestStreamAsync();
        //    requestStream.Write(channelUriInBytes, 0, channelUriInBytes.Length);

        //    try
        //    {
        //        // Get the response from the server.
        //        WebResponse response = await webRequest.GetResponseAsync();
        //        StreamReader requestReader = new StreamReader(response.GetResponseStream());
        //        String webResponse = requestReader.ReadToEnd();
        //    }

        //    catch (Exception ex)
        //    {
        //        // Could not send channel URI to server.
        //    }


        //}

#if WP8

        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {

            Dispatcher.BeginInvoke(() =>
            {
                // Display the new URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                System.Diagnostics.Debug.WriteLine(e.ChannelUri.ToString());
                MessageBox.Show(String.Format("Channel Uri is {0}",
                    e.ChannelUri.ToString()));

            });
        }

        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic for your particular application would be here.
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
                    );
        }
#endif


        void MainPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            SetAppBarOpacity();
        }

#if WP8
        private async void RegisterVoiceCommands()
        {
            try
            {
                await VoiceCommandService.InstallCommandSetsFromFileAsync(new Uri("ms-appx:///vcd.xml"));
            }
            catch (Exception)
            {
                // deal with it .gif
            }
        }
#endif

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (pivotSelect != null && pivotSelect.IsShown)
            {
                e.Cancel = true;
                pivotSelect.Hide();
                return;
            }

            base.OnBackKeyPress(e);
        }

        private void App_SuspendStreamingEvent(object sender, EventArgs e)
        {
            // zap these
            if (StreamingApi != null && StreamingApi.Any())
            {
                foreach (var item in StreamingApi.Values)
                {
                    item.StreamingEvent -= StreamingApi_StreamingEvent;
                    item.StopStreaming();
                }
            }

            StreamingApi = new Dictionary<long, TwitterApi>();
            ((IMehdohApp)(Application.Current)).ViewModel.CurrentlyStreaming = false;
        }

        private void App_ReigniteStreamingEvent(object sender, EventArgs e)
        {
            StartStreaming();
        }

        private void App_ColumnsChangedEvent(object sender, EventArgs e)
        {
            // reset the list of configured scrolling
            ConfiguredEvents = new List<string>();

            // Flag we need a rebuild
            FlagNeedsRebuild = true;
        }


        private bool InitialisedEvents { get; set; }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (InitialisedEvents)
                return;

            NavigationService.PauseOnBack = true;

            if (FlagNeedsRebuild)
                BuildColumns();

            CheckListViewEventsAreSet();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            base.OnNavigatedTo(e);

            if (((IMehdohApp)(Application.Current)).ViewModel.JustRestored)
                FlagNeedsRebuild = true;

            if (e.NavigationMode == NavigationMode.Back && LayoutRoot.Visibility == Visibility.Visible && !((IMehdohApp)(Application.Current)).ViewModel.JustRestored)
            {

                ThreadPool.QueueUserWorkItem(delegate
                {
                    if (StreamingSettings == null || ((IMehdohApp)(Application.Current)).ViewModel.HasBeenToStreamingSettingsPage)
                    {
                        var sh = new SettingsHelper();
                        StreamingSettings = sh.GetSettingsStreaming();
                    }

                    ((IMehdohApp)(Application.Current)).ViewModel.HasBeenToStreamingSettingsPage = false;

                    if (((IMehdohApp)(Application.Current)).ViewModel.CurrentlyStreaming)
                    {
                        StartStreamingNow();
                    }
                });

                AssignCorrectMenu();
                return;
            }

            // reset this
            ((IMehdohApp)(Application.Current)).ViewModel.JustRestored = false;


#if WP8
            // Dont care if its reset, just means it's come back from being idle
            if (e.NavigationMode == NavigationMode.Reset)
                return;
#endif

            // reset this
            if (e.NavigationMode == NavigationMode.Refresh)
            {
                RefreshFromIdle();
                return;
            }

            // Can this be moved after the IsLoaded?
            if (!UiHelper.ValidateUser())
            {
                ShowWelcomePopup();
                // Do popup
                return;
            }

            if (((IMehdohApp)(Application.Current)).ViewModel.IsDataLoaded)
            {
                UiHelper.HideProgressBar();
                return;
            }

            // Set the data context of the listbox control to the sample data
            //DataContext = ((IMehdohApp)(Application.Current)).ViewModel;

            LayoutRoot.Visibility = Visibility.Visible;

            if (ApplicationBar != null)
                ApplicationBar.IsVisible = true;

            // queryStrings["Action"] = "ShareContent",
            // http://msdn.microsoft.com/en-us/library/ff967563(v=vs.92).aspx

            string fileId;
            if (NavigationContext.QueryString.TryGetValue("FileId", out fileId) && !UiHelper.ImageHasBeenShared)
            {
                UiHelper.ImageHasBeenShared = true;
                NavigationService.Navigate(new Uri("/NewTweet.xaml?FileId=" + fileId, UriKind.Relative));
                return;
            }

            if (((IMehdohApp)(Application.Current)).ViewModel.IsDataLoaded)
                return;


            // goes in here after the user adds their very first account
            // that should set up some default columns 
            if (((IMehdohApp)(Application.Current)).JustAddedFirstUser)
            {

                bool hasTwitter;

                using (var dh = new MainDataContext())
                {
                    hasTwitter = dh.Profiles.Any(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter);
                }

                if (hasTwitter)
                {
                    UiHelper.SafeDispatch(ShowPostWelcomePopup);
                }

                ColumnHelper.RefreshConfig();
                PerformedInitialRefresh = false;
                StartRefreshingTask();

                ThreadPool.QueueUserWorkItem(RefreshAllColumns);
            }
            else
            {
                UiHelper.ShowProgressBar();
                ThreadPool.QueueUserWorkItem(StartUp);
            }

        }

        private void RefreshFromIdle()
        {

            ThreadPool.QueueUserWorkItem(delegate
                                             {
                                                 var sh = new SettingsHelper();
                                                 if (sh.GetUseTweetMarker())
                                                 {
                                                     GetTweetMarkerSettings();
                                                 }
                                                 else
                                                 {
                                                     UiHelper.ShowProgressBar("looking for new updates");
                                                     ThreadPool.QueueUserWorkItem(PerformInitialRefresh);
                                                     PerformedInitialRefresh = true;
                                                 }

                                             });
        }

        private void StartUp(object state)
        {


            UiHelper.SafeDispatchSync(BuildColumns);

            var sh = new SettingsHelper();
            StreamingSettings = sh.GetSettingsStreaming();

            FriendsCache.LoadFriendsCache();

            UiHelper.SafeDispatch(() =>
                                      {
                                          long accountId;

                                          try
                                          {
                                              accountId = GetAccountForCurrentColumn();
                                          }
                                          catch (Exception)
                                          {

                                              return;
                                          }

                                          // no columns set up.
                                          if (accountId == 0)
                                              return;

                                          var timer = new DispatcherTimer
                                                          {
                                                              Interval = new TimeSpan(0, 0, 5)
                                                          };

                                          timer.Tick += delegate(object sender, EventArgs e1)
                                                            {
                                                                var thisTimer = sender as DispatcherTimer;
                                                                thisTimer.Stop();
                                                                var shellHelper = new ShellHelper();
                                                                UiHelper.SafeDispatch(() => shellHelper.ResetLiveTile(accountId, false));
                                                            };

                                          timer.Start();

                                          // Get the user account settings
                                          ContinueRefreshing();

                                      });

        }

        private void lstHeader_Hold(object sender, GestureEventArgs e)
        {
            if (e != null)
                e.Handled = true;

            ShowPivotSelect();
        }

        private void ShowPivotSelect()
        {
            pivotSelect.CurrentIndex = mainPivot.SelectedIndex;
            pivotSelect.Show();
        }

        private object GetRetweetsOfMe(long accountId)
        {
            var listbox = new ListBox
                              {
                                  Margin = new Thickness(0, 0, -12, 0),
                                  ItemTemplate = Resources["templateTimeline"] as DataTemplate,
                              };

            listbox.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

            listbox.SelectionChanged += lstReweetsOfMe_SelectionChanged;
            // listbox.SetValue(ListAnimation.IsPivotAnimatedProperty, true);
            
            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmRetweetsOfMe(accountId);
            listbox.ItemsSource = ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId];

            //MainViewModel.LoadTimeline();

            return listbox;
        }

        private RadDataBoundListBox GetListControl(string templateName, EventHandler<EventArgs> fetchMoreDelegate, EventHandler<ScrollStateChangedEventArgs> scrollingDelegate,
            EventHandler<EventArgs> refreshDelegate, bool isPullToRefreshEnabled, DataVirtualizationMode defaultVirtualizationMode = DataVirtualizationMode.OnDemandAutomatic)
        {
            var listbox = new RadDataBoundListBox()
            {
                IsAsyncBalanceEnabled = true,
                AsyncBalanceMode = AsyncBalanceMode.FillViewportFirst,
                ItemTemplate = Resources[templateName] as DataTemplate,
                Margin = new Thickness(0, 0, -12, 0),
                IsPullToRefreshEnabled = isPullToRefreshEnabled,
                EmptyContent = null,
                Style = Resources["RadDataBoundListBoxStyle1"] as Style,
                PullToRefreshIndicatorStyle = Resources["PullToRefreshIndicatorControlStyle"] as Style
            };

            InteractionEffectManager.SetIsInteractionEnabled(listbox, true);

            listbox.DataVirtualizationItemContent = "loading more tweets";
            listbox.DataVirtualizationMode = defaultVirtualizationMode;
            listbox.DataRequested += fetchMoreDelegate;
            listbox.ScrollStateChanged += scrollingDelegate;
            listbox.RefreshRequested += refreshDelegate;
            listbox.UseOptimizedManipulationRouting = false;

            return listbox;
        }

        private FrameworkElement GetTimeline(long accountId)
        {

            EventHandler<ScrollStateChangedEventArgs> scrollingDelegate = delegate(object sender, ScrollStateChangedEventArgs e)
                {
                    try
                    {

                        var thisListBox = sender as RadDataBoundListBox;
                        if (thisListBox == null)
                            return;

                        var firstItem = thisListBox.TopVisibleItem as TimelineViewModel;

                        if (firstItem == null)
                            return;

                        int newCount = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].IndexOf(firstItem);

                        int existingCount;

                        var existingCountString = UiHelper.GetTimelineCount(mainPivot, accountId);

                        if (int.TryParse(existingCountString.Replace("+", ""), out existingCount))
                        {
                            if (newCount < existingCount)
                            {
                                UiHelper.SetTimelineCount(mainPivot, newCount.ToString(CultureInfo.CurrentUICulture), accountId);
                                if (newCount == 0)
                                {
                                    // at the top, so set the tweetmarker value
                                    UpdateTweetMarker();
                                }
                            }
                        }
                    }
                    catch
                    {
                        // ignore for now
                    }

                };

            EventHandler<EventArgs> fetchMoreDelegate = delegate(object sender, EventArgs e)
            {
                if (((RadDataBoundListBox) sender).Tag != null)
                {
                    ((RadDataBoundListBox) sender).Tag = null;
                    return;
                }

                UiHelper.ShowProgressBar("fetching more timeline");
                ThreadPool.QueueUserWorkItem(StartGetMoreTimeline, accountId);
            };

            EventHandler<EventArgs> refreshDelegate = delegate
                                                          {
                                                              RefreshTimeline(accountId, null);
                                                          };

            var listbox = GetListControl("templateTimeline", fetchMoreDelegate, scrollingDelegate, refreshDelegate, true);

            listbox.Tap += lstTimeline_Tap;

            listbox.BeginUpdate();

            listbox.Tag = true;

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTimeline(accountId);
            listbox.ItemsSource = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId];

            listbox.EndUpdate(true);


            return listbox;

        }

        private object GetMentions(long accountId)
        {

            //var listbox = new ListBox
            //                  {
            //                      Margin = new Thickness(0, 0, -12, 0),
            //                      ItemTemplate = Resources["templateMentions"] as DataTemplate
            //                  };

            //listbox.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

            EventHandler<ScrollStateChangedEventArgs> scrollingDelegate = delegate(object sender, ScrollStateChangedEventArgs e)
            {
                try
                {

                    var thisListBox = sender as RadDataBoundListBox;

                    var firstItem = thisListBox.TopVisibleItem as MentionsViewModel;

                    if (firstItem == null)
                        return;

                    int newCount = ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].IndexOf(firstItem);

                    int existingCount;

                    var existingCountString = UiHelper.GetMentionsCount(mainPivot, accountId);

                    if (int.TryParse(existingCountString.Replace("+", ""), out existingCount))
                    {
                        if (newCount < existingCount)
                            UiHelper.SetMentionsCount(mainPivot, newCount.ToString(CultureInfo.CurrentUICulture), accountId);
                    }

                }
                catch
                {
                    // ignore for now
                }

            };

            EventHandler<EventArgs> fetchMoreDelegate = delegate(object sender, EventArgs e)
                                                            {
                                                                if (((RadDataBoundListBox)sender).Tag != null)
                                                                {
                                                                    ((RadDataBoundListBox)sender).Tag = null;
                                                                    return;
                                                                }

                                                                UiHelper.ShowProgressBar("fetching more mentions");
                                                                ThreadPool.QueueUserWorkItem(StartGetMoreMentions, accountId);

                                                            };

            EventHandler<EventArgs> refreshDelegate = delegate
            {
                RefreshMentions(accountId, null);
            };

            var listbox = GetListControl("templateMentions", fetchMoreDelegate, scrollingDelegate, refreshDelegate, true);

            listbox.Tap += lstMentions_Tap;
            // listbox.SetValue(ListAnimation.IsPivotAnimatedProperty, true);

            listbox.BeginUpdate();

            listbox.Tag = true;

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMentions(accountId);
            listbox.ItemsSource = ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId];
            // MainViewModel.LoadMentions(accountId);

            listbox.EndUpdate(true);

            return listbox;
        }

        private object GetMessages(long accountId)
        {

            EventHandler<ScrollStateChangedEventArgs> scrollingDelegate = delegate(object sender, ScrollStateChangedEventArgs e)
            {
                try
                {
                    var thisListBox = sender as RadDataBoundListBox;

                    var firstItem = thisListBox.TopVisibleItem as MessagesViewModel;

                    if (firstItem == null)
                        return;

                    int newCount = ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].IndexOf(firstItem);

                    int existingCount;

                    var existingCountString = UiHelper.GetMessagesCount(mainPivot, accountId);

                    if (int.TryParse(existingCountString.Replace("+", ""), out existingCount))
                    {
                        if (newCount < existingCount)
                            UiHelper.SetMessagesCount(mainPivot, newCount.ToString(CultureInfo.CurrentUICulture), accountId);
                    }
                }
                catch
                {
                    // ignore for now
                }

            };

            EventHandler<EventArgs> fetchMoreDelegate = delegate(object sender, EventArgs e)
            {
                if (((RadDataBoundListBox)sender).Tag != null)
                {
                    ((RadDataBoundListBox)sender).Tag = null;
                    return;
                }

                UiHelper.ShowProgressBar("fetching more messages");
                ThreadPool.QueueUserWorkItem(StartGetMoreMessages, accountId);
            };

            EventHandler<EventArgs> refreshDelegate = delegate
            {
                RefreshMessages(accountId, null);
            };

            var listbox = GetListControl("templateMessages", fetchMoreDelegate, scrollingDelegate, refreshDelegate, true, DataVirtualizationMode.None);

            listbox.Tap += lstMessages_Tap;

            listbox.BeginUpdate();

            listbox.Tag = true;

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMessages(accountId);
            listbox.ItemsSource = ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId];

            listbox.EndUpdate(true);

            return listbox;
        }

        private object GetFavourites(long accountId)
        {

            EventHandler<ScrollStateChangedEventArgs> scrollingDelegate = delegate(object sender, ScrollStateChangedEventArgs e)
            {
                try
                {
                    var thisListBox = sender as RadDataBoundListBox;

                    var firstItem = thisListBox.TopVisibleItem as FavouritesViewModel;

                    if (firstItem == null)
                        return;

                    int newCount = ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].IndexOf(firstItem);

                    int existingCount;

                    var existingCountString = UiHelper.GetFavouritesCount(mainPivot, accountId);

                    if (int.TryParse(existingCountString.Replace("+", ""), out existingCount))
                    {
                        if (newCount < existingCount)
                            UiHelper.SetFavouritesCount(mainPivot, newCount.ToString(CultureInfo.CurrentUICulture), accountId);
                    }
                }
                catch
                {
                    // ignore for now
                }

            };

            EventHandler<EventArgs> fetchMoreDelegate = delegate(object sender, EventArgs e)
            {
                //if (!((IMehdohApp)(Application.Current)).ViewModel.IsDataLoaded)
                //    return;

                if (((RadDataBoundListBox)sender).Tag != null)
                {
                    ((RadDataBoundListBox)sender).Tag = null;
                    return;
                }

                UiHelper.ShowProgressBar(ApplicationResources.fetchingmorefavourites);
                ThreadPool.QueueUserWorkItem(StartGetMoreFavourites, accountId);
            };

            EventHandler<EventArgs> refreshDelegate = delegate
            {
                RefreshMessages(accountId, null);
            };

            var listbox = GetListControl("templateFavourites", fetchMoreDelegate, scrollingDelegate, refreshDelegate, false);


            //var listbox = new ListBox
            //                  {
            //                      Margin = new Thickness(0, 0, -12, 0),
            //                      ItemTemplate = Resources["templateFavourites"] as DataTemplate
            //                  };

            //listbox.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

            listbox.Tap += lstFavourites_Tap;
            // listbox.SetValue(ListAnimation.IsPivotAnimatedProperty, true);

            listbox.BeginUpdate();
            listbox.Tag = true;

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmFavourites(accountId);
            listbox.ItemsSource = ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId];
            // MainViewModel.LoadFavourites(accountId);

            listbox.EndUpdate(true);

            return listbox;
        }

        private object GetList(long accountId, string slug)
        {

            EventHandler<ScrollStateChangedEventArgs> scrollingDelegate = delegate(object sender, ScrollStateChangedEventArgs e)
            {

                try
                {
                    var thisListBox = sender as RadDataBoundListBox;

                    var firstItem = thisListBox.TopVisibleItem as TimelineViewModel;

                    if (firstItem == null)
                        return;

                    int newCount = ((IMehdohApp)(Application.Current)).ViewModel.Lists[slug].IndexOf(firstItem);

                    int existingCount;

                    var existingCountString = UiHelper.GetListCount(mainPivot, slug, accountId);

                    if (int.TryParse(existingCountString.Replace("+", ""), out existingCount))
                    {
                        if (newCount < existingCount)
                            UiHelper.SetListCount(mainPivot, slug, newCount.ToString(CultureInfo.CurrentUICulture), accountId);
                    }
                }
                catch
                {
                    // ignore for now
                }

            };

            EventHandler<EventArgs> fetchMoreDelegate = delegate(object sender, EventArgs e)
            {
                if (((RadDataBoundListBox)sender).Tag != null)
                {
                    ((RadDataBoundListBox)sender).Tag = null;
                    return;
                }

                var state = new ListState
                {
                    AccountId = accountId,
                    ListId = slug
                };

                UiHelper.ShowProgressBar("fetching more list");
                ThreadPool.QueueUserWorkItem(StartGetMoreList, state);
            };

            EventHandler<EventArgs> refreshDelegate = delegate
            {
                RefreshList(slug, accountId);
            };

            var listbox = GetListControl("templateTimeline", fetchMoreDelegate, scrollingDelegate, refreshDelegate, false);

            listbox.Tap += lstList_Tap;

            // Configure the source
            if (!((IMehdohApp)(Application.Current)).ViewModel.Lists.ContainsKey(slug))
            {
                ((IMehdohApp)(Application.Current)).ViewModel.Lists.Add(slug, new SortedObservableCollection<TimelineViewModel>());
            }

            listbox.BeginUpdate();

            listbox.Tag = true;

            listbox.ItemsSource = ((IMehdohApp)(Application.Current)).ViewModel.Lists[slug];

            listbox.EndUpdate(true);

            return listbox;
        }

        private void api_GetListStatusesCompletedEvent(long accountId, string slug, List<ResponseTweet> listStatuses, bool hasError, string errorMessage)
        {

            UiHelper.HidePullToRefresh(mainPivot,
                           ApplicationConstants.ColumnTypeTwitterList,
                           slug,
                           accountId);

            if (hasError || listStatuses == null)
            {
                if (RefreshingList.ContainsKey(slug))
                    RefreshingList[slug] = false;
                else
                    RefreshingList.Add(slug, false);

                Dispatcher.BeginInvoke(FinishedRefreshingTask);

                if (hasError)
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                        UiHelper.ShowToast(errorMessage);
                    else
                        UiHelper.ShowToast("There was a problem connecting to Twitter.");
                }

                return;
            }

            if (!listStatuses.Any())
            {
                Dispatcher.BeginInvoke(FinishedRefreshingTask);
                if (RefreshingList.ContainsKey(slug))
                    RefreshingList[slug] = false;
                else
                    RefreshingList.Add(slug, false);
                return;
            }

            UiHelper.SafeDispatch(delegate
            {

                ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTwitterList(slug);

                var lastItem = ((IMehdohApp)(Application.Current)).ViewModel.Lists[slug].FirstOrDefault();
                long? newestId = null;

                if (lastItem != default(TimelineViewModel))
                    newestId = ((IMehdohApp)(Application.Current)).ViewModel.Lists[slug].Max(x => x.Id);

                bool hasGap = true;

                if (lastItem != null && ((IMehdohApp)(Application.Current)).ViewModel != null && ((IMehdohApp)(Application.Current)).ViewModel.Lists != null && ((IMehdohApp)(Application.Current)).ViewModel.Lists.ContainsKey(slug))
                {
                    if (((IMehdohApp)(Application.Current)).ViewModel.Lists[slug].Any())
                    {
                        var itemHasComeBack = listStatuses.Any(x => x.id == newestId);
                        if (itemHasComeBack)
                            hasGap = false;
                    }
                }
                else
                {
                    hasGap = false;
                }

                // Save the current position of the display
                UpdatePreRefreshColumnPositions();

                int newCount = 0;

                var newItems = new List<long>();

                foreach (var item in listStatuses)
                {
                    if (((IMehdohApp)(Application.Current)).ViewModel.Lists[slug].Any(x => x.Id == item.id))
                        continue;
                    ((IMehdohApp)(Application.Current)).ViewModel.Lists[slug].Add(item.AsViewModel(accountId));
                    newItems.Add(item.id);

                    newCount++;
                }

                if (hasGap && newItems.Any() && newestId != null)
                {
                    ((IMehdohApp)(Application.Current)).ViewModel.Lists[slug].Add(new TimelineViewModel
                    {
                        IsGap = true,
                        Id = newestId.Value,
                        AccountId = accountId
                    });
                }

                UiHelper.SetListCount(mainPivot, slug,
                                          newCount.ToString(CultureInfo.CurrentUICulture) + ((hasGap) ? "+" : ""),
                                          accountId);


                // Save the current position of the display
                RestorePreRefreshColumnPositions();

                CheckListViewEventsAreSet();

                ((IMehdohApp)(Application.Current)).ViewModel.Lists[slug].Apply(x => x.UpdateTime());

                FinishedRefreshingTask();

                if (RefreshingList.ContainsKey(slug))
                    RefreshingList[slug] = false;
                else
                    RefreshingList.Add(slug, false);

                ThreadPool.QueueUserWorkItem(delegate
                {
                    var dsh = new DataStorageHelper();
                    dsh.SaveTwitterListUpdates(accountId, listStatuses, slug);

                    FriendsCache.ParseNewTimelines(listStatuses);

                });

            });

        }

        private object GetColumnContent(int columnType, string value, long accountId)
        {

            switch (columnType)
            {
                case ApplicationConstants.ColumnTypeTwitter: // core

                    switch (value)
                    {
                        case ApplicationConstants.ColumnTwitterTimeline:
                            ((IMehdohApp)(Application.Current)).ViewModel.LoadTimeline(accountId);
                            return GetTimeline(accountId);

                        case ApplicationConstants.ColumnTwitterMentions:
                            ((IMehdohApp)(Application.Current)).ViewModel.LoadMentions(accountId);
                            return GetMentions(accountId);

                        case ApplicationConstants.ColumnTwitterMessages:
                            ((IMehdohApp)(Application.Current)).ViewModel.LoadMessages(accountId);
                            return GetMessages(accountId);

                        case ApplicationConstants.ColumnTwitterFavourites:
                            ((IMehdohApp)(Application.Current)).ViewModel.LoadFavourites(accountId);
                            return GetFavourites(accountId);

                        case ApplicationConstants.ColumnTwitterRetweetsOfMe:
                            return GetRetweetsOfMe(accountId);

                        case ApplicationConstants.ColumnTwitterNewFollowers:
                            return GetNewFollowers(accountId);

                        case ApplicationConstants.ColumnTwitterPhotoView:
                            return GetPhotoView(accountId);
                    }

                    break;

                case ApplicationConstants.ColumnTypeTwitterSearch:
                    return GetTwitterSearch(accountId, value); // twitter search

                case ApplicationConstants.ColumnTypeTwitterList: // list
                    ((IMehdohApp)(Application.Current)).ViewModel.LoadList(accountId, value);
                    return GetList(accountId, value);

            }

            return null;
        }


        private UIElement GetTwitterSearch(long accountId, string value)
        {


            EventHandler<ScrollStateChangedEventArgs> scrollingDelegate = delegate(object sender, ScrollStateChangedEventArgs e)
            {

                try
                {
                    var thisListBox = sender as RadDataBoundListBox;

                    var firstItem = thisListBox.TopVisibleItem as TimelineViewModel;

                    if (firstItem == null)
                        return;

                    int newCount = ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][value].IndexOf(firstItem);

                    int existingCount;

                    var existingCountString = UiHelper.GetTwitterSearchCount(mainPivot, value, accountId);

                    if (int.TryParse(existingCountString.Replace("+", ""), out existingCount))
                    {
                        if (newCount < existingCount)
                            UiHelper.SetTwitterSearchCount(mainPivot, newCount.ToString(CultureInfo.CurrentUICulture), accountId, value);
                    }
                }
                catch
                {
                    // ignore for now
                }

            };

            EventHandler<EventArgs> fetchMoreDelegate = delegate(object sender, EventArgs e)
            {
                //if (!((IMehdohApp)(Application.Current)).ViewModel.IsDataLoaded)
                //    return;

                if (((RadDataBoundListBox)sender).Tag != null)
                {
                    ((RadDataBoundListBox)sender).Tag = null;
                    return;
                }

                var state = new SearchState
                {
                    AccountId = accountId,
                    SearchQuery = value
                };

                UiHelper.ShowProgressBar("fetching more " + value);
                ThreadPool.QueueUserWorkItem(StartGetMoreSearch, state);
            };

            EventHandler<EventArgs> refreshDelegate = delegate
            {
                RefreshTwitterSearch(value, accountId);
            };

            var listbox = GetListControl("templateTimeline", fetchMoreDelegate, scrollingDelegate, refreshDelegate, false);

            //var listbox = new ListBox
            //                  {
            //                      Margin = new Thickness(0, 0, -12, 0),
            //                      ItemTemplate = Resources["templateTimeline"] as DataTemplate
            //                  };

            //listbox.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

            listbox.Tap += lstSearch_Tap;
            // listbox.SetValue(ListAnimation.IsPivotAnimatedProperty, true);

            listbox.BeginUpdate();

            listbox.Tag = true;

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTwitterSearch(accountId, value);
            listbox.ItemsSource = ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][value];

            listbox.EndUpdate(true);

            return listbox;
        }

        private UIElement GetPhotoView(long accountId)
        {
            var listbox = new ListBox
                              {
                                  Margin = new Thickness(0, 0, -12, 0),
                                  ItemTemplate = Resources["templateTwitterPhotos"] as DataTemplate,
                                  ItemsPanel = Resources["panelTemplateWrapPanel"] as ItemsPanelTemplate
                              };

            listbox.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

            listbox.Tap += lstPhotoView_Tap;
            
            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmPhotoView(accountId);
            listbox.ItemsSource = ((IMehdohApp)(Application.Current)).ViewModel.PhotoView[accountId];

            return listbox;
        }

        private UIElement GetNewFollowers(long accountId)
        {
            // templateFollower
            var listbox = new ListBox
                              {
                                  Margin = new Thickness(0, 0, -12, 0),
                                  ItemTemplate = Resources["templateFollower"] as DataTemplate
                              };

            listbox.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);

            listbox.Tap += lstNewFollowers_Tap;
            // listbox.SetValue(ListAnimation.IsPivotAnimatedProperty, true);

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmNewFollowers(accountId);
            listbox.ItemsSource = ((IMehdohApp)(Application.Current)).ViewModel.NewFollowers[accountId];
            // MainViewModel.LoadNewFollowers(accountId);

            RefreshNewFollowers(accountId);

            return listbox;
        }

        private void BuildColumns()
        {

            UiHelper.ShowProgressBar("arranging columns...");

            ((IMehdohApp)(Application.Current)).ViewModel.LoadingInitialData = true;

            // Move this in before loading anything
            ((IMehdohApp)(Application.Current)).ViewModel.MoveStateToDb();

            FriendsCache.LoadFriendsCache();

            try
            {

                // need to remove soundcloud and instagram columns
                ColumnHelper.CheckValidColumns();

                var columnConfig = ColumnHelper.ColumnConfig;

                int oldValue = -1;

                if (mainPivot != null)
                {
                    oldValue = mainPivot.SelectedIndex;
                    LayoutRoot.Children.Remove(mainPivot);
                }

                mainPivot = new Pivot();
                mainPivot.SetValue(Grid.RowProperty, 0);
                mainPivot.ItemTemplate = Resources["templateListBox"] as DataTemplate;
                mainPivot.SelectionChanged += mainPivot_SelectionChanged;

                if (columnConfig.Count == 0)
                {
                    mainPivot.Items.Add(GetBlankContent());
                }
                else
                {

                    ((IMehdohApp)(Application.Current)).ViewModel.ItemCounts = new ObservableCollection<string>();

                    var sh = new SettingsHelper();
                    var showPivotHeaderAvatars = sh.GetShowPivotHeaderAvatars();
                    var showPivotHeaderCounts = sh.GetShowPivotHeaderCounts();

                    for (int index = 0; index < columnConfig.Count; index++)
                    {
                        var col = columnConfig[index];

                        ((IMehdohApp)(Application.Current)).ViewModel.ItemCounts.Add("0");

                        var newPivot = new PivotItem
                                           {
                                               Header = UiHelper.GetColumnHeader(col.ColumnType, col.DisplayName.ToLower(), col.AccountId, lstheader_DoubleTap, null, lstHeader_Hold, showPivotHeaderCounts, showPivotHeaderAvatars, 0),
                                               Content = GetColumnContent(col.ColumnType, col.Value, col.AccountId),
                                               Name = "pivot" + col.Value.Replace(" ", "_") + "_" + col.AccountId
                                           };

                        if (mainPivot.Items.OfType<PivotItem>().All(x => x.Name != newPivot.Name))
                            mainPivot.Items.Add(newPivot);
                    }

                    FlagNeedsRebuild = false;

                    if (oldValue != -1 && oldValue < mainPivot.Items.Count)
                        mainPivot.SelectedIndex = oldValue;
                    else
                        mainPivot.SelectedIndex = 0;
                }

                LayoutRoot.Children.Add(mainPivot);

                AddPivotSelect();

            }
            finally
            {
                UiHelper.HideProgressBar();
                ((IMehdohApp)(Application.Current)).ViewModel.LoadingInitialData = false;
            }

        }

        private void AddPivotSelect()
        {
            pivotSelect = new SelectPivot(mainPivot)
                              {
                                  VerticalContentAlignment = VerticalAlignment.Stretch
                              };

            pivotSelect.SetValue(Grid.RowProperty, 0);
            pivotSelect.SetValue(Grid.RowSpanProperty, 2);

            LayoutRoot.Children.Add(pivotSelect);

            pivotSelect.PivotSelectedEvent += pivotSelect_PivotSelectedEvent;
        }

        private void pivotSelect_PivotSelectedEvent(object sender, SelectPivotSelectedArgs e)
        {
            mainPivot.SelectedIndex = e.SelectedIndex;
        }

        private object GetBlankContent()
        {
            var newPivot = new PivotItem
                               {
                                   Header = UiHelper.GetColumnHeader(0, "not configured", 0, lstheader_DoubleTap, null, lstHeader_Hold, false, false, 0)
                               };

            var stack = new StackPanel();

            var text = new TextBlock
                           {
                               FontSize = 26,
                               Style = Resources["PhoneTextSubtleStyle"] as Style,
                               TextWrapping = TextWrapping.Wrap,
                               Text = "You do not have any items configured on your home screen."
                           };

            var button = new Button
                             {
                                 Content = "add items",
                                 Margin = new Thickness(50)
                             };
            button.Click += (sender, e) => NavigationService.Navigate(new Uri("/Customise.xaml", UriKind.Relative));

            stack.Children.Add(text);
            stack.Children.Add(button);

            newPivot.Content = stack;

            return newPivot;
        }

        private void App_FontSizeChangedEvent(object sender, EventArgs e)
        {
            // reset the list of configured scrolling
            ConfiguredEvents = new List<string>();
            // Flag we need a rebuild
            FlagNeedsRebuild = true;
        }

        private bool FlagNeedsRebuild { get; set; }

        private async Task RefreshBackgroundTask(object state)
        {
            var sh = new SettingsHelper();
            if (!sh.GetBackgroundTaskEnabled())
                return;

            var bh = new BackgroundHelper();
            if (!bh.IsDisabled())
                await bh.ReigniteBackgroundTask();
        }

        private void ShowPostWelcomePopup()
        {
            var post = new UserControls.PostWelcome();
            post.LinkClickEvent += postWelcome_LinkClickEvent;

            postWelcomePopup = new Popup
                                   {
                                       Child = post,
                                       Height = ActualHeight,
                                       Width = ActualWidth,
                                       VerticalOffset = 0,
                                       HorizontalOffset = 0,
                                       IsOpen = true
                                   };

            LayoutRoot.Visibility = Visibility.Collapsed;

            if (ApplicationBar != null)
                ApplicationBar.IsVisible = false;
        }

        private void postWelcome_LinkClickEvent(object sender, EventArgs e)
        {
            LayoutRoot.Visibility = Visibility.Visible;

            if (ApplicationBar != null)
                ApplicationBar.IsVisible = true;

            postWelcomePopup.IsOpen = false;
        }

        private void ShowWelcomePopup()
        {
            var welcome = new UserControls.Welcome();
            welcome.LinkClickEvent += welcome_LinkClickEvent;

            //var width = ((PhoneApplicationFrame)Application.Current.RootVisual).ActualWidth;
            //var height = ((PhoneApplicationFrame)Application.Current.RootVisual).ActualHeight;

            SystemTray.IsVisible = false;

            popupWindow = new Popup
                              {
                                  //                    Width = width,
                                  //                  Height = height,
                                  Child = welcome,
                                  IsOpen = true,
                                  HorizontalOffset = 0,
                                  VerticalOffset = 0
                              };

            welcome.AnimateBoxesOn();

            LayoutRoot.Visibility = Visibility.Collapsed;

            if (ApplicationBar != null)
                ApplicationBar.IsVisible = false;
        }

        private void welcome_LinkClickEvent(object sender, Welcome.WelcomeEventArgs e)
        {

            SystemTray.IsVisible = true;

            if (popupWindow != null)
            {
                popupWindow.IsOpen = false;
                popupWindow = null;
            }

            App.AttempedLink = true;

            switch (e.SelectedAccountType)
            {
                case ApplicationConstants.AccountTypeEnum.Twitter:
                    NavigationService.Navigate(new Uri("/TwitterOAuth.xaml", UriKind.Relative));
                    break;
            }

            LayoutRoot.Visibility = Visibility.Collapsed;
        }

        private void ContinueRefreshing()
        {
            UiHelper.ShowProgressBar("checking for updates");

            StartRefreshingTask();
            FinishedRefreshingTask();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            RefreshingCount = 0;

            UiHelper.HideProgressBar();

            base.OnNavigatedFrom(e);

            if (e.NavigationMode == NavigationMode.Back ||
                (e.NavigationMode == NavigationMode.New && e.Uri.ToString() == "app://external/"))
            {
                StopStreaming();

                // Update tweet marker
                UpdateTweetMarker();

            }
        }

        private void StopStreaming()
        {
            try
            {
                if (StreamingApi != null && StreamingApi.Any())
                {
                    // exiting the app
                    foreach (var streamingApi in StreamingApi)
                    {
                        streamingApi.Value.StopStreaming();
                    }
                }

                UiHelper.EnablePullToRefresh(mainPivot);

            }
            catch (Exception)
            {
            }
            finally
            {
                ((IMehdohApp)(Application.Current)).ViewModel.CurrentlyStreaming = false;
                if (StreamingApi != null)
                    StreamingApi.Clear();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

        }

        private void lstReweetsOfMe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;

            // If selected index is -1 (no selection) do nothing
            if (listBox == null || listBox.SelectedIndex == -1)
                return;

            var item = listBox.SelectedValue as TimelineViewModel;

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/RetweetsOfMe.xaml?accountId=" + GetAccountForCurrentColumn() + "&id=" + item.Id, UriKind.Relative));

            // Reset selected index to -1 (no selection)
            listBox.SelectedIndex = -1;
        }

        private void lstList_Tap(object sender, GestureEventArgs gestureEventArgs)
        {

            var listBox = sender as RadDataBoundListBox;

            // If selected index is -1 (no selection) do nothing
            if (listBox == null || listBox.SelectedValue == null)
                return;

            var item = listBox.SelectedValue as TimelineViewModel;

            if (item == null)
                return;

            gestureEventArgs.Handled = true;

            if (item.IsGap)
            {

                var source = listBox.ItemsSource as SortedObservableCollection<TimelineViewModel>;

                if (source == null)
                    return;

                var nextTimeline = source[source.IndexOf(item) - 1];
                var newestId = nextTimeline.Id;

                // Reset selected index to -1 (no selection)
                listBox.SelectedItem = null;

                UiHelper.ShowProgressBar("getting more tweets");

                long accountId = item.AccountId;

                var currentCol = ColumnHelper.ColumnConfig[mainPivot.SelectedIndex];

                var bfh = new BackfillHelper(accountId)
                              {
                                  MoreTweetsButton = item
                              };
                bfh.GetMoreListCompletedEvent += bfh_GetMoreListCompletedEvent;
                bfh.GetMoreList(currentCol.Value, newestId, item.Id, accountId);

            }
            else
            {
                if (item.ResponseTweet != null)
                    ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet = item.ResponseTweet;
                else
                    ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet = null;

                string query = "accountId=" + item.AccountId + "&id=" + item.Id;

                if (item.MediaUrl != null)
                    query += "&photo=true";

                NavigateToDetailsPage(query);

                // Reset selected index to -1 (no selection)
                listBox.SelectedItem = null;
            }
        }


        private void lstNewFollowers_Tap(object sender, GestureEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            var lstFriends = sender as ListBox;

            if (lstFriends == null || lstFriends.SelectedItem == null)
                return;

            var item = lstFriends.SelectedItem as FriendViewModel;

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/UserProfile.xaml?accountId=" + item.AccountId + "&screen=" + item.ScreenName.Replace("@", ""), UriKind.Relative));

            lstFriends.SelectedIndex = -1;
        }


        private void lstPhotoView_Tap(object sender, GestureEventArgs e)
        {
            var listBox = sender as ListBox;

            // If selected index is -1 (no selection) do nothing
            if (listBox == null || listBox.SelectedIndex == -1)
                return;

            var item = listBox.SelectedValue as PhotoViewModel;

            if (item == null)
                return;

            string query = "accountId=" + item.AccountId + "&id=" + item.Id + "&photo=true";
            NavigateToDetailsPage(query);

            // Reset selected index to -1 (no selection)
            listBox.SelectedIndex = -1;
        }

        // Handle selection changed on ListBox
        private async void lstTimeline_Tap(object sender, GestureEventArgs gestureEventArgs)
        {

            var listBox = sender as RadDataBoundListBox;
            if (listBox == null || listBox.SelectedItem == null)
                return;

            var item = listBox.SelectedItem as TimelineViewModel;
            if (item == null)
                return;

            gestureEventArgs.Handled = true;


            try
            {
                if (item.IsGap)
                {

                    var source = listBox.ItemsSource as SortedObservableCollection<TimelineViewModel>;

                    if (source == null)
                        return;

                    // we want the one above
                    var nextTimeline = source[source.IndexOf(item) - 1];
                    var newestId = nextTimeline.Id;

                    // Reset selected index to -1 (no selection)
                    listBox.SelectedItem = null;

                    UiHelper.ShowProgressBar("getting more tweets");

                    // TODO: handle searches
                    long accountId = item.AccountId;

                    var bfh = new BackfillHelper(accountId)
                                  {
                                      MoreTweetsButton = item
                                  };

                    var result = await bfh.GetMoreTimeline(newestId, item.Id, accountId);
                    bfh_GetMoreTimelineCompletedEvent(accountId, result, item);

                }
                else
                {

                    if (item.ResponseTweet != null)
                        ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet = item.ResponseTweet;
                    else
                        ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet = null;

                    string query = "accountId=" + item.AccountId + "&id=" + item.Id;

                    if (item.MediaUrl != null)
                        query += "&photo=true";

                    NavigateToDetailsPage(query);

                    // Reset selected index to -1 (no selection)
                    listBox.SelectedItem = null;
                }

            }
            catch (Exception ex)
            {

            }


        }

        // Handle selection changed on ListBox
        private async void lstSearch_Tap(object sender, GestureEventArgs gestureEventArgs)
        {
            var listBox = sender as RadDataBoundListBox;

            // If selected index is -1 (no selection) do nothing
            if (listBox == null || listBox.SelectedItem == null)
                return;

            var item = listBox.SelectedValue as TimelineViewModel;

            if (item == null)
                return;

            gestureEventArgs.Handled = true;

            if (item.IsGap)
            {
                UiHelper.ShowProgressBar("getting more tweets");

                var source = listBox.ItemsSource as SortedObservableCollection<TimelineViewModel>;

                if (source == null)
                    return;

                var nextTimeline = source[source.IndexOf(item) + 1];
                var oldestId = nextTimeline.Id;

                // Reset selected index to -1 (no selection)
                listBox.SelectedItem = null;

                // TODO: handle searches
                long accountId = item.AccountId;

                var currentCol = ColumnHelper.ColumnConfig[mainPivot.SelectedIndex];

                var bfh = new BackfillHelper(accountId)
                              {
                                  MoreTweetsButton = item
                              };
                var results = await bfh.GetMoreTwitterSearch(currentCol.Value, item.Id, oldestId, accountId);
                bfh_GetMoreTwitterSearchCompletedEvent(accountId, results, currentCol.Value, item);

            }
            else
            {

                if (item.ResponseTweet != null)
                    ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet = item.ResponseTweet;
                else
                    ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet = null;

                string query = "accountId=" + item.AccountId + "&id=" + item.Id;

                if (item.MediaUrl != null)
                    query += "&photo=true";

                NavigateToDetailsPage(query);

                // Reset selected index to -1 (no selection)
                listBox.SelectedItem = null;
            }
        }

        private void lstMessages_Tap(object sender, GestureEventArgs gestureEventArgs)
        {
            var listBox = sender as RadDataBoundListBox;

            // If selected index is -1 (no selection) do nothing
            if (listBox == null)
                return;

            var item = listBox.SelectedValue as MessagesViewModel;

            if (item == null)
                return;

            // hide the new sign
            item.NewSignVisibility = Visibility.Collapsed;

            //var replyUrl = "accountId=" + item.AccountId + " &replyToAuthor=" + item.ScreenName + "&replyToId=" + item.Id + "&dm=true";
            //NavigationService.Navigate(new Uri("/NewTweet.xaml?" + replyUrl, UriKind.Relative));

            var replyUrl = "accountId=" + item.AccountId + "&messageId=" + item.Id;
            NavigateToDetailsPage(replyUrl);

            // Reset selected index to -1 (no selection)
            listBox.SelectedItem = null;
        }

        // Handle selection changed on ListBox
        private void lstFavourites_Tap(object sender, GestureEventArgs gestureEventArgs)
        {
            var listBox = sender as RadDataBoundListBox;

            // If selected index is -1 (no selection) do nothing
            if (listBox == null || listBox.SelectedValue == null)
                return;

            var item = listBox.SelectedItem as FavouritesViewModel;
            string query = "accountId=" + item.AccountId + "&favId=" + item.Id;

            NavigateToDetailsPage(query);

            // Reset selected index to -1 (no selection)
            listBox.SelectedItem = null;
        }

        private void NavigateToDetailsPage(string query)
        {
            UiHelper.SafeDispatch(() =>
            {
                try
                {
                    NavigationService.Navigate(new Uri("/DetailsPage.xaml?" + query, UriKind.Relative));
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex);
                }
            });
        }

        private void lstMentions_Tap(object sender, GestureEventArgs gestureEventArgs)
        {
            var listBox = sender as RadDataBoundListBox;

            // If selected index is -1 (no selection) do nothing
            if (listBox.SelectedItem == null)
                return;

            var item = listBox.SelectedItem as MentionsViewModel;

            if (item.ResponseTweet != null)
                ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet = item.ResponseTweet;
            else
                ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet = null;

            string query = "accountId=" + item.AccountId + "&mentionId=" + item.Id;

            if (item.MediaUrl != null)
                query += "&photo=true";

            NavigateToDetailsPage(query);

            // Reset selected index to -1 (no selection)
            listBox.SelectedItem = null;
        }

        private void StorageHelperUI_SaveProfileImageCompletedEvent(object sender, EventArgs e)
        {
            // Continue loading
            UiHelper.SafeDispatch(() =>
                                      {
                                          BuildColumns();
                                          CheckListViewEventsAreSet();
                                      });
        }

        private void mnuSearch_Click(object sender, EventArgs e)
        {
            var accountId = GetAccountForCurrentColumn();
            NavigationService.Navigate(new Uri("/SearchPage.xaml?accountId=" + accountId, UriKind.Relative));
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/SettingsNew.xaml", UriKind.Relative));
        }

        private void mnuRefresh_Click(object sender, EventArgs e)
        {

            // is it a refresh button or a streaming button?
            var button = sender as ApplicationBarIconButton;

            if (button == null)
                return;

            if (button.Text == "on")
            {
                UiHelper.ShowToast("stopping streaming");
                // stop streaming
                StopStreaming();
                AssignCorrectMenu();
                return;
            }

            if (button.Text == "off")
            {
                UiHelper.ShowToast("starting streaming");
                // start streaming
                StartStreaming();
                AssignCorrectMenu();
                return;
            }

            var settingHelper = new SettingsHelper();

            if (settingHelper.GetRefreshAllColumns())
            {
                UiHelper.ShowProgressBar("looking for new updates");
                ThreadPool.QueueUserWorkItem(RefreshAllColumns);
            }
            else
            {
                if (ColumnHelper.ColumnConfig == null || !ColumnHelper.ColumnConfig.Any())
                    return;
                if (mainPivot == null)
                    return;
                var currentIndex = mainPivot.SelectedIndex;
                ThreadPool.QueueUserWorkItem(RefreshColumn, currentIndex);
            }

        }

        protected Dictionary<RadDataBoundListBox, object> PreRefreshColumnPositions { get; set; }

        private void UpdatePreRefreshColumnPositions()
        {

            PreRefreshColumnPositions = new Dictionary<RadDataBoundListBox, object>();

            if (mainPivot == null || mainPivot.Items == null)
                return;

            try
            {

                foreach (var pivot in mainPivot.Items.OfType<PivotItem>())
                {
                    if (pivot == null)
                        continue;

                    var radContent = pivot.Content as RadDataBoundListBox;
                    if (radContent != null)
                    {
                        var topItem = radContent.TopVisibleItem;
                        if (topItem != null)
                        {
                            radContent.BeginAsyncBalance();
                            PreRefreshColumnPositions.Add(radContent, topItem);
                        }
                    }
                }
            }
            catch
            {
                // whatever
            }

        }

        private void RestorePreRefreshColumnPositions()
        {
            if (PreRefreshColumnPositions == null)
                return;

            foreach (var item in PreRefreshColumnPositions)
            {
                var listBox = item.Key;
                //listBox.EndUpdate(true);
                listBox.BringIntoView(item.Value);
                listBox.InvalidateArrange();
            }

            PreRefreshColumnPositions = null;
        }

        private void RefreshColumn(object state)
        {
            int currentIndex = (int)state;

            var currentItem = ColumnHelper.ColumnConfig[currentIndex];

            switch (currentItem.ColumnType)
            {
                case ApplicationConstants.ColumnTypeTwitter:
                    switch (currentItem.Value)
                    {
                        case ApplicationConstants.ColumnTwitterTimeline:
                            UiHelper.ShowProgressBar("refreshing timeline");
                            RefreshTimeline(currentItem.AccountId, null);
                            break;

                        case ApplicationConstants.ColumnTwitterMentions:
                            UiHelper.ShowProgressBar("refreshing mentions");
                            RefreshMentions(currentItem.AccountId, null);
                            break;

                        case ApplicationConstants.ColumnTwitterMessages:
                            UiHelper.ShowProgressBar("refreshing messages");
                            RefreshMessages(currentItem.AccountId, null);
                            break;

                        case ApplicationConstants.ColumnTwitterFavourites:
                            UiHelper.ShowProgressBar(ApplicationResources.refreshingfavourites);
                            RefreshFavourites(currentItem.AccountId);
                            break;

                        case ApplicationConstants.ColumnTwitterRetweetsOfMe:
                            UiHelper.ShowProgressBar("refreshing retweets");
                            RefreshRetweetsOfMe(currentItem.AccountId);
                            break;


                        case ApplicationConstants.ColumnTwitterNewFollowers:
                            UiHelper.ShowProgressBar("refreshing new followers");
                            RefreshNewFollowers(currentItem.AccountId);
                            break;

                        case ApplicationConstants.ColumnTwitterPhotoView:
                            //UiHelper.ShowProgressBar("refreshing photo view");
                            //RefreshPhotoView(currentItem.AccountId);
                            UiHelper.ShowProgressBar("refreshing timeline and photos");
                            RefreshTimeline(currentItem.AccountId, null);
                            break;
                    }
                    break;

                case ApplicationConstants.ColumnTypeTwitterList:
                    UiHelper.ShowProgressBar("refreshing " + currentItem.DisplayName);
                    RefreshList(currentItem.Value, currentItem.AccountId);
                    break;

                case ApplicationConstants.ColumnTypeTwitterSearch:
                    UiHelper.ShowProgressBar("refreshing search " + currentItem.DisplayName);
                    RefreshTwitterSearch(currentItem.Value, currentItem.AccountId);
                    break;

            }
        }

        private void RefreshPhotoView(long accountId)
        {
            StartRefreshingTask();

            UiHelper.SafeDispatch(() =>
                                      {
                                          ((IMehdohApp)(Application.Current)).ViewModel.ConfirmPhotoView(accountId);
                                          ((IMehdohApp)(Application.Current)).ViewModel.LoadPhotoView(accountId);
                                      });

            FinishedRefreshingTask();
        }

        private void RefreshNewFollowers(long accountId)
        {
            var api = new TwitterApi(accountId);
            string screenName;
            using (var dh = new MainDataContext())
            {
                var profile = dh.Profiles.SingleOrDefault(x => x.Id == accountId);
                if (profile == null)
                    return;

                screenName = profile.ScreenName;
            }

            StartRefreshingTask();

            UiHelper.SafeDispatch(() => ((IMehdohApp)(Application.Current)).ViewModel.NewFollowers[accountId].Clear());

            api.GetFollowers(screenName, "-1");
            api.GetFollowersCompletedEvent += api_GetFollowersCompletedEvent;
        }

        private void api_GetFollowersCompletedEvent(object sender, EventArgs e)
        {
            FinishedRefreshingTask();

            var api = sender as TwitterApi;

            if (api == null)
                return;

            api.GetFollowersCompletedEvent -= api_GetFollowersCompletedEvent;

            if (api.FollowerIds == null || !api.FollowerIds.Any())
                return;

            var accountId = api.AccountId;
            var topIds = api.FollowerIds.Take(20);

            var helper = new FriendsHelper(accountId);
            helper.GetFriendsCompletedEvent += helper_GetFriendsCompletedEvent;

            foreach (var friendId in topIds)
            {
                helper.AddFriendToSearch(friendId);
            }

            helper.GetFriends();
        }

        private void helper_GetFriendsCompletedEvent(object sender, GetFriendEventArgs e)
        {
            if (e.FriendUser == null)
                return;

            var friendsHelper = sender as FriendsHelper;
            if (friendsHelper == null)
                return;

            var accountId = friendsHelper.AccountId;

            Dispatcher.BeginInvoke(delegate
                                       {
                                           var newModel = new FriendViewModel
                                                              {
                                                                  AccountId = accountId,
                                                                  DisplayName = e.FriendUser.DisplayName,
                                                                  Id = e.FriendUser.Id,
                                                                  ProfileImageUrl = e.FriendUser.ProfileImageUrl,
                                                                  ScreenName = e.FriendUser.ScreenName
                                                              };
                                           ((IMehdohApp)(Application.Current)).ViewModel.NewFollowers[accountId].Add(newModel);
                                       });
        }


        private void RefreshFacebookNews(long accountId)
        {
#if FACEBOOK
            var api = new FacebookApi(accountId);

            api.GetNewsFeedCompletedEvent += new EventHandler(api_GetNewsFeedCompletedEvent);
            api.GetNewsFeed();
#endif
        }

#if FACEBOOK
        void api_GetNewsFeedCompletedEvent(object sender, EventArgs e)
        {

            var api = sender as FacebookApi;

            if (api == null || api.News == null)
                return;

            var accountId = api.AccountId;

            UiHelper.SafeDispatch(() =>
            {
                foreach (var item in api.News.data)
                {
                    var news = item.AsViewModel(accountId);
                    ((IMehdohApp)(Application.Current)).ViewModel.ConfirmFacebookNews(accountId);
                    ((IMehdohApp)(Application.Current)).ViewModel.FacebookNews[accountId].Add(news);
                }

                FinishedRefreshingTask();

            });

        }
#endif

#if FACEBOOK
        void api_GetProfileFeedCompletedEvent(object sender, EventArgs e)
        {

        }
#endif


        private async void RefreshList(string value, long accountId)
        {
            if (RefreshingList.ContainsKey(value) && RefreshingList[value])
                return;

            long sinceId = 0;

            if (((IMehdohApp)(Application.Current)).ViewModel.Lists[value] != null && ((IMehdohApp)(Application.Current)).ViewModel.Lists[value] != null &&
                ((IMehdohApp)(Application.Current)).ViewModel.Lists[value].Count > 3)
            {
                var res = ((IMehdohApp)(Application.Current)).ViewModel.Lists[value].OrderByDescending(x => x.Id).Take(3).LastOrDefault();
                if (res != null)
                    sinceId = res.Id;
            }

            StartRefreshingTask();
            if (RefreshingList.ContainsKey(value))
                RefreshingList[value] = true;
            else
                RefreshingList.Add(value, true);

            var api = new TwitterApi(accountId);
            var result = await api.GetListStatuses(value, sinceId, 0);
            api_GetListStatusesCompletedEvent(accountId, value, result, api.HasError, api.ErrorMessage);

        }

        private void RefreshRetweetsOfMe(long accountId)
        {
            if (RefreshingRetweetsOfMe.ContainsKey(accountId) && RefreshingRetweetsOfMe[accountId])
                return;

            long retweetsSinceId = 0;

            if (((IMehdohApp)(Application.Current)).ViewModel != null && ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe != null &&
                ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe.ContainsKey(accountId) &&
                ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].Any() && ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].Count > 3)
            {
                var res = ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].OrderByDescending(x => x.Id).Take(3).LastOrDefault();
                if (res != null)
                    retweetsSinceId = res.Id;
            }

            var api = new TwitterApi(accountId);
            api.GetRetweetsOfMeCompletedEvent += api_GetRetweetsOfMeCompletedEvent;
            StartRefreshingTask();

            if (RefreshingRetweetsOfMe.ContainsKey(accountId))
                RefreshingRetweetsOfMe[accountId] = true;
            else
                RefreshingRetweetsOfMe.Add(accountId, true);

            api.GetRetweetsOfMe(retweetsSinceId, 0);
        }

        private void api_GetRetweetsOfMeCompletedEvent(object sender, EventArgs e)
        {
            var api = sender as TwitterApi;

            if (api == null || api.HasError || api.RetweetsOfMe == null)
            {
                if (api != null)
                {
                    api.GetRetweetsOfMeCompletedEvent -= api_GetRetweetsOfMeCompletedEvent;
                    var accountId = api.AccountId;
                    if (RefreshingRetweetsOfMe.ContainsKey(accountId))
                        RefreshingRetweetsOfMe[accountId] = false;
                    else
                        RefreshingRetweetsOfMe.Add(accountId, false);
                }
                Dispatcher.BeginInvoke(FinishedRefreshingTask);
                UiHelper.ShowToast("There was a problem connecting to Twitter.");
                return;
            }

            api.GetRetweetsOfMeCompletedEvent -= api_GetRetweetsOfMeCompletedEvent;

            if (!api.RetweetsOfMe.Any())
            {
                var accountId = api.AccountId;
                if (RefreshingRetweetsOfMe.ContainsKey(accountId))
                    RefreshingRetweetsOfMe[accountId] = false;
                else
                    RefreshingRetweetsOfMe.Add(accountId, false);
                Dispatcher.BeginInvoke(FinishedRefreshingTask);
                return;
            }

            Dispatcher.BeginInvoke(delegate
                                       {
                                           var accountId = api.AccountId;
                                           var lastItem = ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].FirstOrDefault();

                                           var hasGap = true;

                                           var newMin = api.RetweetsOfMe.Min(x => x.id);

                                           if (((IMehdohApp)(Application.Current)).ViewModel != null && ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe != null &&
                                               ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe.ContainsKey(accountId) &&
                                               ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].Any())
                                           {
                                               long oldMax = ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].Max(x => x.Id);
                                               if (newMin <= oldMax)
                                                   hasGap = false;
                                           }
                                           else
                                           {
                                               hasGap = false;
                                           }

                                           int newCount = 0;

                                           foreach (
                                               var item in
                                                   api.RetweetsOfMe.Where(
                                                       item =>
                                                       !((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].Any(x => x.Id == item.id))
                                               )
                                           {
                                               ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].Add(item.AsViewModel(accountId));
                                               newCount++;
                                               // TODO - This doesn't save FriendsHelper
                                           }

                                           UiHelper.SetRetweetsOfMeCount(mainPivot, newCount.ToString(), api.AccountId);

                                           if (hasGap)
                                           {
                                               UiHelper.SetRetweetsOfMeCount(mainPivot, newCount.ToString() + "+",
                                                                             api.AccountId);

                                               long lastId = api.RetweetsOfMe.Min(x => x.id);

                                               ((IMehdohApp)(Application.Current)).ViewModel.ConfirmRetweetsOfMe(accountId);
                                               ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].Add(new TimelineViewModel
                                                                                             {
                                                                                                 IsGap = true,
                                                                                                 Id = lastId,
                                                                                                 AccountId =
                                                                                                     api.AccountId
                                                                                             });
                                           }

                                           FinishedRefreshingTask();

                                           if (RefreshingRetweetsOfMe.ContainsKey(accountId))
                                               RefreshingRetweetsOfMe[accountId] = false;
                                           else
                                               RefreshingRetweetsOfMe.Add(accountId, false);

                                           if (newCount > 0 && lastItem != null)
                                           {
                                               UiHelper.ScrollIntoView(mainPivot, ApplicationConstants.ColumnTypeTwitter,
                                                                       ApplicationConstants.ColumnTwitterRetweetsOfMe,
                                                                       api.AccountId, lastItem);
                                           }
                                       });

            ThreadPool.QueueUserWorkItem(delegate(object state)
                                             {
                                                 var dsh = new DataStorageHelper();
                                                 dsh.SaveRetweetsOfMeUpdates(api.AccountId, api.RetweetsOfMe);
                                             });
        }


        private async void RefreshMentions(long accountId, int? count)
        {
            if (RefreshingMentions.ContainsKey(accountId) && RefreshingMentions[accountId])
                return;

            long mentionsSinceId = 0;

            using (var dh = new MainDataContext())
            {
                if (dh.Mentions.Any(x => x.ProfileId == accountId))
                    mentionsSinceId = dh.Mentions.Where(x => x.ProfileId == accountId).Max(x => x.Id);
            }

            var api = new TwitterApi(accountId);
            StartRefreshingTask();

            if (RefreshingMentions.ContainsKey(accountId))
                RefreshingMentions[accountId] = true;
            else
                RefreshingMentions.Add(accountId, true);

            var result = await api.GetMentions(mentionsSinceId, count);
            api_GetMentionsCompletedEvent(accountId, result, api.HasError, api.ErrorMessage);
        }


        private async void RefreshTwitterSearch(string value, long accountId)
        {
            if (RefreshingTwitterSearch.ContainsKey(value) && RefreshingTwitterSearch[value])
                return;

            long timelineSinceId = 0;

            if (((IMehdohApp)(Application.Current)).ViewModel != null && ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch != null &&
                ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch.ContainsKey(accountId) &&
                ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId].ContainsKey(value))
            {
                if (((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][value].Any() &&
                    ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][value].Count > 3)
                {
                    var res =
                        ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][value].OrderByDescending(x => x.Id)
                                                                     .Take(3)
                                                                     .LastOrDefault();
                    if (res != null)
                        timelineSinceId = res.Id;
                }
            }

            var api = new TwitterApi(accountId);

            StartRefreshingTask();

            var result = await api.Search(value, 0, timelineSinceId, true);
            api_SearchCompletedEvent(accountId, result, value, api.HasError, api.ErrorMessage);
        }

        private void api_SearchCompletedEvent(long accountId, ResponseSearch searchResult, string searchQuery, bool hasError, string errorMessage)
        {

            if (searchResult == null || searchResult.statuses == null)
            {
                if (RefreshingTwitterSearch.ContainsKey(searchQuery))
                    RefreshingTwitterSearch[searchQuery] = false;
                else
                    RefreshingTwitterSearch.Add(searchQuery, false);

                Dispatcher.BeginInvoke(FinishedRefreshingTask);

                if (hasError)
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                        UiHelper.ShowToast(errorMessage);
                    else
                        UiHelper.ShowToast("There was a problem connecting to Twitter.");
                }

                return;
            }

            if (!searchResult.statuses.Any())
            {
                Dispatcher.BeginInvoke(FinishedRefreshingTask);
                if (RefreshingTwitterSearch.ContainsKey(searchQuery))
                    RefreshingTwitterSearch[searchQuery] = false;
                else
                    RefreshingTwitterSearch.Add(searchQuery, false);
                return;
            }

            Dispatcher.BeginInvoke(delegate
            {

                ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTwitterSearch(accountId, searchQuery);

                var lastItem = ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].FirstOrDefault();
                long? newestId = null;

                if (lastItem != null)
                    newestId = ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Max(x => x.Id);

                bool hasGap = true;

                if (lastItem != null && ((IMehdohApp)(Application.Current)).ViewModel != null && ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch != null)
                {
                    if (((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Any())
                    {
                        var itemHasComeBack = searchResult.statuses.Any(x => x.id == newestId);
                        if (itemHasComeBack)
                            hasGap = false;
                    }
                }
                else
                {
                    hasGap = false;
                }

                // Save the current position of the display
                UpdatePreRefreshColumnPositions();

                int newCount = 0;

                var newItems = new List<long>();

                foreach (var item in searchResult.statuses)
                {
                    if (((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Any(x => x.Id == item.id))
                        continue;
                    ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Add(item.AsViewModel(accountId));
                    newItems.Add(item.id);

                    newCount++;
                }

                if (hasGap && newItems.Any() && newestId != null)
                {
                    //long oldestNewId = newItems.Min();
                    //long lastId = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Where(x => x.Id <= oldestNewId).Max(x => x.Id);

                    ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Add(new TimelineViewModel
                    {
                        IsGap = true,
                        Id = newestId.Value,
                        AccountId = accountId
                    });
                }

                UiHelper.SetTwitterSearchCount(mainPivot,
                                          newCount.ToString(CultureInfo.CurrentUICulture) + ((hasGap) ? "+" : ""),
                                          accountId,
                                          searchQuery);


                // Save the current position of the display
                RestorePreRefreshColumnPositions();

                CheckListViewEventsAreSet();

                ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Apply(x => x.UpdateTime());

                FinishedRefreshingTask();

                if (RefreshingTwitterSearch.ContainsKey(searchQuery))
                    RefreshingTwitterSearch[searchQuery] = false;
                else
                    RefreshingTwitterSearch.Add(searchQuery, false);

                ThreadPool.QueueUserWorkItem(delegate
                {
                    var dsh = new DataStorageHelper();
                    dsh.SaveTwitterSearches(accountId, searchQuery, searchResult.statuses);

                    FriendsCache.ParseNewTimelines(searchResult.statuses);
                });

            });

        }

        private async void RefreshTimeline(long accountId, int? count)
        {

            if (RefreshingTimeline.ContainsKey(accountId) && RefreshingTimeline[accountId])
                return;

            long timelineSinceId = 0;

            if (((IMehdohApp)(Application.Current)).ViewModel != null && ((IMehdohApp)(Application.Current)).ViewModel.Timeline != null && ((IMehdohApp)(Application.Current)).ViewModel.Timeline.ContainsKey(accountId))
            {
                if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Count > 10)
                {
                    var res = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].OrderByDescending(x => x.Id).Take(10).LastOrDefault();
                    if (res != null)
                        timelineSinceId = res.Id;
                }
                else
                {
                    var res = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].OrderByDescending(x => x.Id).LastOrDefault();
                    if (res != null)
                        timelineSinceId = res.Id;
                }
            }

            var api = new TwitterApi(accountId);

            //api.GetTimelineCompletedEvent += GetTimelineCompletedEvent;
            StartRefreshingTask();

            lock (RefreshingTimeline)
            {
                if (RefreshingTimeline.ContainsKey(accountId))
                    RefreshingTimeline[accountId] = true;
                else
                    RefreshingTimeline.Add(accountId, true);
            }

            var timeline = await api.GetTimeline(timelineSinceId, count);
            GetTimelineCompletedEvent(accountId, timeline, api.HasError, api.ErrorMessage);

        }

        private async void RefreshMessages(long accountId, int? count)
        {
            if (RefreshingMessages.ContainsKey(accountId) && RefreshingMessages[accountId])
                return;

            long dmsSinceId = 0;

            using (var dh = new MainDataContext())
            {
                if (dh.Messages.Any(x => x.ProfileId == accountId))
                    dmsSinceId = dh.Messages.Where(x => x.ProfileId == accountId).Max(x => x.Id);
            }

            var api = new TwitterApi(accountId);
            StartRefreshingTask();

            if (RefreshingMessages.ContainsKey(accountId))
                RefreshingMessages[accountId] = true;
            else
                RefreshingMessages.Add(accountId, true);

            var results = await api.GetDirectMessages(dmsSinceId, count);
            api_GetDirectMessagesCompletedEvent(accountId, results, api.HasError, api.ErrorMessage);
        }


        private void mnuFullRefreshFavourites_Click(object sender, EventArgs e)
        {
            UiHelper.ShowProgressBar(ApplicationResources.refreshingfavourites);
            StartRefreshingTask();

            var accountId = GetAccountForCurrentColumn();

            ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].Clear();
            UiHelper.SetFavouritesCount(mainPivot, "0", accountId);

            // thread off
            ThreadPool.QueueUserWorkItem(async delegate
                                             {
                                                 var dsh = new DataStorageHelper();
                                                 dsh.DeleteFavourites(accountId);

                                                 var api = new TwitterApi(accountId);
                                                 var result = await api.GetFavourites(0);
                                                 api_GetFavouritesCompletedEvent(accountId, result, api.HasError, api.ErrorMessage);
                                             });
        }

        private async void RefreshFavourites(long accountId)
        {
            if (RefreshingFavourites.ContainsKey(accountId) && RefreshingFavourites[accountId])
                return;

            long favouritesSinceId = 0;

            using (var dh = new MainDataContext())
            {
                if (dh.Favourites.Any(x => x.ProfileId == accountId))
                    favouritesSinceId = dh.Favourites.Where(x => x.ProfileId == accountId).Max(x => x.Id);
            }

            var api = new TwitterApi(accountId);
            StartRefreshingTask();

            if (RefreshingFavourites.ContainsKey(accountId))
                RefreshingFavourites[accountId] = true;
            else
                RefreshingFavourites.Add(accountId, true);

            var result = await api.GetFavourites(favouritesSinceId);
            api_GetFavouritesCompletedEvent(accountId, result, api.HasError, api.ErrorMessage);
        }

        private void RefreshAllColumns(object twitterOnly)
        {

            bool twitterColumnsOnly = false;

            if (twitterOnly != null)
            {
                twitterColumnsOnly = (bool)twitterOnly;
            }

            int? count = null;

            if (((IMehdohApp)(Application.Current)).JustAddedFirstUser)
            {
                count = 20;
            }

            foreach (var column in ColumnHelper.ColumnConfig)
            {
                switch (column.ColumnType)
                {
                    case ApplicationConstants.ColumnTypeTwitter:
                        // what is it?
                        switch (column.Value)
                        {
                            case ApplicationConstants.ColumnTwitterTimeline:
                                RefreshTimeline(column.AccountId, count);
                                break;

                            case ApplicationConstants.ColumnTwitterMentions:
                                RefreshMentions(column.AccountId, count);
                                break;

                            case ApplicationConstants.ColumnTwitterFavourites:
                                RefreshFavourites(column.AccountId);
                                break;

                            case ApplicationConstants.ColumnTwitterMessages:
                                RefreshMessages(column.AccountId, count);
                                break;

                            case ApplicationConstants.ColumnTwitterRetweetsOfMe:
                                RefreshRetweetsOfMe(column.AccountId);
                                break;

                            //case ApplicationConstants.Column_Twitter_Retweeted_To_Me:
                            //    RefreshRetweetsToMe(column.AccountId);
                            //    break;

                            //case ApplicationConstants.Column_Twitter_Retweeted_By_Me:
                            //    RefreshRetweetByMe(column.AccountId);
                            //    break;

                            case ApplicationConstants.ColumnTwitterPhotoView:
                                RefreshPhotoView(column.AccountId);
                                break;
                        }
                        break;

                    case ApplicationConstants.ColumnTypeTwitterList:
                        // List
                        RefreshList(column.Value, column.AccountId);
                        break;

                    case ApplicationConstants.ColumnTypeTwitterSearch:
                        // Search
                        RefreshTwitterSearch(column.Value, column.AccountId);
                        break;
                }
            }

            ((IMehdohApp)(Application.Current)).JustAddedFirstUser = false;

        }


        private void api_GetDirectMessagesCompletedEvent(long accountId, List<ResponseDirectMessage> directMessages, bool hasError, string errorMessage)
        {

            UiHelper.HidePullToRefresh(mainPivot,
               ApplicationConstants.ColumnTypeTwitter,
               ApplicationConstants.ColumnTwitterMessages,
               accountId);

            if (directMessages == null || !directMessages.Any())
            {
                Dispatcher.BeginInvoke(FinishedRefreshingTask);

                if (RefreshingMessages.ContainsKey(accountId))
                    RefreshingMessages[accountId] = false;
                else
                    RefreshingMessages.Add(accountId, false);

                if (hasError)
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                        UiHelper.ShowToast(errorMessage);
                    else
                        UiHelper.ShowToast("There was a problem connecting to Twitter.");
                }

                return;
            }

            var res = ViewModelHelper.MessagesResponseToView(accountId, directMessages);

            Dispatcher.BeginInvoke(delegate
                                       {

                                           ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMessages(accountId);
                                           var lastItem = ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].FirstOrDefault();
                                           int count = 0;

                                           foreach (var item in res.Where(item => ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].SingleOrDefault(x => x.Id == item.Id) == null).OrderBy(x => x.Id))
                                           {
                                               while (((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Any(x => string.CompareOrdinal(x.ScreenName, item.ScreenName) == 0))
                                               {
                                                   var thisItem = ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].FirstOrDefault(x => string.CompareOrdinal(x.ScreenName, item.ScreenName) == 0);

                                                   if (thisItem == default(MessagesViewModel))
                                                       break;

                                                   ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Remove(thisItem);
                                               }

                                               item.NewSignVisibility = Visibility.Visible;

                                               ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Insert(0, item);
                                               lastItem = item;
                                               count++;
                                           }

                                           UiHelper.SetMessagesCount(mainPivot,
                                                                     count.ToString(CultureInfo.CurrentUICulture),
                                                                     accountId);

                                           FinishedRefreshingTask();

                                           if (RefreshingMessages.ContainsKey(accountId))
                                               RefreshingMessages[accountId] = false;
                                           else
                                               RefreshingMessages.Add(accountId, false);

                                           if (lastItem != null)
                                           {
                                               UiHelper.ScrollIntoView(mainPivot,
                                                                       ApplicationConstants.ColumnTypeTwitter,
                                                                       ApplicationConstants.ColumnTwitterMessages,
                                                                       accountId,
                                                                       lastItem);
                                           }

                                           ThreadPool.QueueUserWorkItem(delegate
                                                                            {
                                                                                var dsh = new DataStorageHelper();
                                                                                dsh.SaveMessagesUpdate(directMessages, accountId);
                                                                            });
                                       });
        }

        private void StartRefreshingTask()
        {
            RefreshingCount++;
        }

        private void FinishedRefreshingTask()
        {
            RefreshingCount--;

            if (RefreshingCount <= 0)
            {

                UiHelper.HideProgressBar();

                if (!PerformedInitialRefresh)
                {
                    var sh = new SettingsHelper();
                    if (sh.GetUseTweetMarker())
                    {
                        GetTweetMarkerSettings();
                    }
                    else
                    {
                        UiHelper.ShowProgressBar("looking for new updates");
                        ThreadPool.QueueUserWorkItem(PerformInitialRefresh);
                        PerformedInitialRefresh = true;
                    }
                }

                if (RequestStartStreaming)
                {
                    StartStreamingNow();
                }
            }

        }

        private void StartStreaming()
        {
            ThreadPool.QueueUserWorkItem(StartStreamingThreadDelegate);
        }

        private void StartStreamingThreadDelegate(object state)
        {
            if (((IMehdohApp)(Application.Current)).ViewModel.CurrentlyStreaming)
                return;

            if (StreamingSettings == null)
            {
                var sh = new SettingsHelper();
                StreamingSettings = sh.GetSettingsStreaming();
            }

            if (!StreamingSettings.StreamingEnabled.HasValue || !StreamingSettings.StreamingEnabled.Value)
                return;

            if (!StreamingSettings.StreamingOnMobile.HasValue || !StreamingSettings.StreamingOnMobile.Value)
                if (!ConnectionHelper.IsOnWifi())
                    return;

            // If theres no twitter columns then dont bother
            var accountIds = ColumnHelper.ColumnConfig.Where(
                    x => x.ColumnType == 0 &&
                    (x.Value == ApplicationConstants.ColumnTwitterTimeline ||
                     x.Value == ApplicationConstants.ColumnTwitterMentions)).Select(x => x.AccountId).Distinct();

            if (!accountIds.Any())
                return;

            ((IMehdohApp)(Application.Current)).ViewModel.CurrentlyStreaming = true;

            UiHelper.DisablePullToRefresh(mainPivot);

            //var screenSetting = sh.GetStreamingKeepScreenOn();
            if (StreamingSettings.StreamingKeepScreenOn.HasValue && StreamingSettings.StreamingKeepScreenOn.Value)
            {
                // Stop the screen dimming
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
            else
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
            }

            EnableDisableStreamingButton(false);

            RequestRefreshAllColumnsAndStartStreaming();
        }

        private void RequestRefreshAllColumnsAndStartStreaming()
        {
            RequestStartStreaming = true;
            RefreshAllColumns(true);
        }

        private void StartStreamingNow()
        {
            // dont call this... this is called after a refresh of all columns
            RequestStartStreaming = false;

            AssignCorrectMenu();

            ThreadPool.QueueUserWorkItem(delegate
                                             {
                                                 var accountIds =
                                                     ColumnHelper.ColumnConfig.Where(
                                                         x =>
                                                         x.ColumnType == 0 &&
                                                         (x.Value == ApplicationConstants.ColumnTwitterTimeline ||
                                                          x.Value == ApplicationConstants.ColumnTwitterMentions ||
                                                          x.Value == ApplicationConstants.ColumnTwitterMessages))
                                                                 .Select(x => x.AccountId)
                                                                 .Distinct();

                                                 foreach (var accountId in accountIds)
                                                 {
                                                     try
                                                     {
                                                         if (StreamingApi.ContainsKey(accountId))
                                                         {
                                                             continue;
                                                         }

                                                         var api = new TwitterApi(accountId);
                                                         api.StartStreaming();
                                                         api.StreamingEvent += StreamingApi_StreamingEvent;
                                                         StreamingApi.Add(accountId, api);
                                                     }
                                                     catch (Exception)
                                                     {
                                                     }

                                                     EnableDisableStreamingButton(true);
                                                 }
                                             });
        }

        private void EnableDisableStreamingButton(bool value)
        {
            UiHelper.SafeDispatch(() =>
                                      {
                                          try
                                          {
                                              if (ApplicationBar == null || ApplicationBar.Buttons == null)
                                                  return;

                                              foreach (ApplicationBarIconButton button in ApplicationBar.Buttons)
                                              {
                                                  if (button.Text == "on" || button.Text == "off")
                                                  {
                                                      button.IsEnabled = value;
                                                      break;
                                                  }
                                              }
                                          }
                                          catch
                                          {
                                              // surpress
                                          }
                                      });
        }

        protected bool RequestStartStreaming { get; set; }

        private void StreamingApi_StreamingEvent(object sender, EventArgs e)
        {
            try
            {

                var api = sender as TwitterApi;
                if (api == null)
                    return;

                if (api.StreamingResult is ResponseTweet) // new tweet
                {
                    var accountId = api.AccountId;
                    var item = api.StreamingResult as ResponseTweet;

                    // make sure its there
                    ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTimeline(accountId);

                    if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].All(x => x.Id != item.id))
                    {
                        // could be BOTH?
                        if (item.IsTimelineTweet.HasValue && item.IsTimelineTweet.Value)
                        {
                            var dsh = new DataStorageHelper();
                            dsh.SaveTimelineUpdates(accountId, new List<ResponseTweet> { item });

                            UiHelper.SafeDispatch(() =>
                                                      {

                                                          if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].All(x => x.Id != item.id))
                                                          {
                                                              // todo: work out how to freeze the UI
                                                              var listBox = (RadDataBoundListBox)UiHelper.GetTimelineList(mainPivot, accountId);
                                                              var oldItem = listBox.TopVisibleItem;

                                                              ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Add(item.AsViewModel(accountId));

                                                              // remove the last item
                                                              if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Count > 20)
                                                                  ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].RemoveAt(((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Count - 1);

                                                              // Update the time on all the items
                                                              ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Apply(x => x.UpdateTime());

                                                              // if this is the current view, then check if we need to auto scroll it

                                                              // otherwise set the column header

                                                              var sh = new SettingsHelper(false, true);
                                                              if (sh.GetAutoScrollEnabled())
                                                              {
                                                                  // only if the current list is the one we just added to.
                                                                  var thisColumn = ColumnHelper.ColumnConfig[mainPivot.SelectedIndex];

                                                                  if (thisColumn.AccountId == accountId &&
                                                                      thisColumn.ColumnType == ApplicationConstants.ColumnTypeTwitter &&
                                                                      thisColumn.Value == ApplicationConstants.ColumnTwitterTimeline)
                                                                  {
                                                                      lstheader_DoubleTap(this, null);
                                                                  }
                                                              }
                                                              else
                                                              {
                                                                  //listBox.IsPullToRefreshEnabled = false;
                                                                  listBox.BringIntoView(oldItem);
                                                              }
                                                          }
                                                      });
                        }

                        if (item.IsMentionTweet.HasValue && item.IsMentionTweet.Value)
                        {
                            var dsh = new DataStorageHelper();
                            dsh.SaveMentionUpdates(new List<ResponseTweet> { item }, accountId);

                            var sh = new SettingsHelper(false, true);
                            var streamingSettings = sh.GetSettingsStreaming();

                            UiHelper.SafeDispatch(() =>
                                                      {
                                                          UiHelper.ShowToast("You have a new mention");

                                                          // make sure its there
                                                          ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMentions(accountId);

                                                          ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Add(
                                                              item.AsMentionViewModel(accountId));

                                                          // if this is the current view, then check if we need to auto scroll it

                                                          // otherwise set the column header

                                                          if (streamingSettings.AutoScrollEnabled.HasValue &&
                                                              streamingSettings.AutoScrollEnabled.Value)
                                                              lstheader_DoubleTap(this, null);

                                                          if (streamingSettings.StreamingVibrate.HasValue &&
                                                              streamingSettings.StreamingVibrate.Value)
                                                              UiHelper.VibrateNotification();

                                                          if (streamingSettings.StreamingSound.HasValue &&
                                                              streamingSettings.StreamingSound.Value)
                                                              UiHelper.SoundNotification();
                                                      });
                        }
                    }
                }
                else if (api.StreamingResult is StreamResponseDelete) // delete tweets
                {
                    var accountId = api.AccountId;
                    var item = api.StreamingResult as StreamResponseDelete;

                    // make sure its there

                    UiHelper.SafeDispatch(() =>
                                              {


                                                  try
                                                  {

                                                      if (item.delete.direct_message != null)
                                                      {
                                                          ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMessages(accountId);
                                                          var deleteMessages = ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Where(x => x.Id == item.delete.direct_message.id).ToList();
                                                          if (deleteMessages.Any())
                                                          {
                                                              foreach (var message in deleteMessages)
                                                              {
                                                                  ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Remove(message);
                                                              }
                                                          }

                                                          // remove from the storage
                                                          var dsh = new DataStorageHelper();
                                                          dsh.DeleteTweetFromStorage(accountId, item.delete.direct_message.id);

                                                      }
                                                      else
                                                      {
                                                          ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTimeline(accountId);

                                                          var deletedItems = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Where(x => x.Id == item.delete.status.id).ToList();
                                                          try
                                                          {
                                                              if (deletedItems.Any())
                                                              {
                                                                  foreach (var timelineViewModel in deletedItems)
                                                                  {
                                                                      ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Remove(timelineViewModel);
                                                                  }
                                                              }
                                                          }
                                                          catch (Exception)
                                                          {
                                                          }

                                                          ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMentions(accountId);
                                                          var deleteMentions = ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Where(x => x.Id == item.delete.status.id).ToList();
                                                          if (deleteMentions.Any())
                                                          {
                                                              foreach (var mention in deleteMentions)
                                                              {
                                                                  ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Remove(mention);
                                                              }
                                                          }

                                                          // remove from the storage
                                                          var dsh = new DataStorageHelper();
                                                          dsh.DeleteTweetFromStorage(accountId, item.delete.status.id);

                                                      }

                                                  }
                                                  catch (Exception ex)
                                                  {
                                                      ErrorLogger.LogException(ex);
                                                  }

                                              });

                }
                else if (api.StreamingResult is StreamDirectMessageReceived) // direct message received
                {
                    var accountId = api.AccountId;
                    var item = api.StreamingResult as StreamDirectMessageReceived;

                    if (item.direct_message != null && item.direct_message.sender_id == accountId)
                        return;

                    // make sure its there
                    ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMessages(accountId);

                    if (!((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Any(x => x.Id == item.direct_message.id))
                    {

                        var dsh = new DataStorageHelper();
                        dsh.SaveMessagesUpdate(new List<ResponseDirectMessage> { item.direct_message }, accountId);

                        var sh = new SettingsHelper(false, true);
                        var streamingSettings = sh.GetSettingsStreaming();

                        UiHelper.SafeDispatch(() =>
                                                  {
                                                      UiHelper.ShowToast("You have a new direct message");

                                                      // previous version
                                                      //((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Add(
                                                      //    item.direct_message.AsViewModel(accountId));

                                                      var newDm = item.direct_message.AsViewModel(accountId);

                                                      //if (var item in res.Where(item => ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].SingleOrDefault(x => x.Id == item.Id) == null).OrderBy(x => x.Id))
                                                      if (!((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Any(x => x.Id == newDm.Id && x.AccountId == newDm.AccountId))
                                                      {
                                                          while (((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Any(x => string.CompareOrdinal(x.ScreenName, newDm.ScreenName) == 0))
                                                          {
                                                              var thisItem = ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].FirstOrDefault(x => string.CompareOrdinal(x.ScreenName, newDm.ScreenName) == 0);

                                                              if (thisItem == default(MessagesViewModel))
                                                                  break;

                                                              ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Remove(thisItem);
                                                          }

                                                          newDm.NewSignVisibility = Visibility.Visible;

                                                          ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Insert(0, newDm);
                                                      }


                                                      // if this is the current view, then check if we need to auto scroll it

                                                      // otherwise set the column header

                                                      if (sh.GetAutoScrollEnabled())
                                                          lstheader_DoubleTap(this, null);

                                                      if (streamingSettings.StreamingVibrate.HasValue &&
                                                          streamingSettings.StreamingVibrate.Value)
                                                          UiHelper.VibrateNotification();

                                                      if (streamingSettings.StreamingSound.HasValue &&
                                                          streamingSettings.StreamingSound.Value)
                                                          UiHelper.SoundNotification();
                                                  });
                    }
                }
            }
            catch (Exception)
            {
                // chill for now
            }
        }

        private void PerformInitialRefresh(object state)
        {

            // test this value so its precached
            var test1 = ((IMehdohApp)Application.Current).DisplayLinks;
            var test2 = ((IMehdohApp)Application.Current).DisplayMaps;

            bool isRefreshing = false;

            foreach (var column in ColumnHelper.ColumnConfig.Where(column => column.RefreshOnStartup))
            {
                switch (column.ColumnType)
                {
                    case ApplicationConstants.ColumnTypeTwitter:
                        // what is it?
                        switch (column.Value)
                        {
                            case ApplicationConstants.ColumnTwitterTimeline:
                                isRefreshing = true;
                                RefreshTimeline(column.AccountId, null);
                                break;

                            case ApplicationConstants.ColumnTwitterMentions:
                                isRefreshing = true;
                                RefreshMentions(column.AccountId, null);
                                break;

                            case ApplicationConstants.ColumnTwitterFavourites:
                                isRefreshing = true;
                                RefreshFavourites(column.AccountId);
                                break;

                            case ApplicationConstants.ColumnTwitterMessages:
                                isRefreshing = true;
                                RefreshMessages(column.AccountId, null);
                                break;

                            case ApplicationConstants.ColumnTwitterRetweetsOfMe:
                                isRefreshing = true;
                                RefreshRetweetsOfMe(column.AccountId);
                                break;

                            //case ApplicationConstants.Column_Twitter_Retweeted_By_Me:
                            //    isRefreshing = true;
                            //    RefreshRetweetByMe(column.AccountId);
                            //    break;

                            //case ApplicationConstants.Column_Twitter_Retweeted_To_Me:
                            //    isRefreshing = true;
                            //    RefreshRetweetsToMe(column.AccountId);
                            //    break;
                        }
                        break;

                    case ApplicationConstants.ColumnTypeTwitterList:
                        // List
                        isRefreshing = true;
                        RefreshList(column.Value, column.AccountId);
                        break;

#if WP8
                    case ApplicationConstants.ColumnTypeFacebook:
                        // facebook
                        switch (column.Value)
                        {
                            case ApplicationConstants.ColumnFacebookNews:
                                RefreshFacebookNews(column.AccountId);
                                break;
                        }
                        break;
#endif

                    case ApplicationConstants.ColumnTypeTwitterSearch:
                        isRefreshing = true;
                        RefreshTwitterSearch(column.Value, column.AccountId);
                        break;
                }
            }

            if (!isRefreshing)
            {
                UiHelper.HideProgressBar();
            }

            // This is when the refreshing task has finished
            ThreadPool.QueueUserWorkItem(async delegate
            {
                await RefreshBackgroundTask(null);
            });

#if WP8
            ThreadPool.QueueUserWorkItem(delegate
                                             {
                                                 UiHelper.SafeDispatch(async () =>
                                                                           {
                                                                               var lockScreenWallpaperHelper = new LockScreenWallpaperHelper();
                                                                               await lockScreenWallpaperHelper.CheckAndUpdateLockScreenWallpaper();
                                                                           });
                                             });
#endif

        }

        private void api_GetFavouritesCompletedEvent(long accountId, List<ResponseTweet> favourites, bool hasError, string errorMessage)
        {

            if (favourites == null || !favourites.Any())
            {
                if (RefreshingFavourites.ContainsKey(accountId))
                    RefreshingFavourites[accountId] = false;
                else
                    RefreshingFavourites.Add(accountId, false);

                Dispatcher.BeginInvoke(FinishedRefreshingTask);

                if (hasError)
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                        UiHelper.ShowToast(errorMessage);
                    else
                        UiHelper.ShowToast("There was a problem connecting to Twitter.");
                }

                return;
            }

            var res = ViewModelHelper.FavouritesResponseToView(accountId, favourites);

            UiHelper.SafeDispatch(delegate
                                       {

                                           ((IMehdohApp)(Application.Current)).ViewModel.ConfirmFavourites(accountId);
                                           //var lastItem = ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].FirstOrDefault();

                                           UpdatePreRefreshColumnPositions();

                                           int count = 0;

                                           foreach (var item in res.Where(item => ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].SingleOrDefault(x => x.Id == item.Id) == null))
                                           {
                                               ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].Add(item);
                                               count++;
                                           }

                                           UiHelper.SetFavouritesCount(mainPivot,
                                                                       count.ToString(CultureInfo.CurrentUICulture),
                                                                       accountId);

                                           // Save the current position of the display
                                           RestorePreRefreshColumnPositions();

                                           FinishedRefreshingTask();

                                           if (RefreshingFavourites.ContainsKey(accountId))
                                               RefreshingFavourites[accountId] = false;
                                           else
                                               RefreshingFavourites.Add(accountId, false);

                                           //if (lastItem != null)
                                           //{
                                           //    UiHelper.ScrollIntoView(mainPivot,
                                           //                            ApplicationConstants.ColumnTypeTwitter,
                                           //                            ApplicationConstants.ColumnTwitterFavourites,
                                           //                            api.AccountId, lastItem);
                                           //}

                                           ThreadPool.QueueUserWorkItem(delegate
                                                                            {
                                                                                var dsh = new DataStorageHelper();
                                                                                dsh.SaveFavouritesUpdates(favourites, accountId);
                                                                            });

                                       });


        }

        private void GetTimelineCompletedEvent(long accountId, List<ResponseTweet> timeline, bool hasError, string errorMessage)
        {

            UiHelper.HidePullToRefresh(mainPivot,
                                       ApplicationConstants.ColumnTypeTwitter,
                                       ApplicationConstants.ColumnTwitterTimeline,
                                       accountId);

            if (hasError || timeline == null)
            {
                if (RefreshingTimeline.ContainsKey(accountId))
                    RefreshingTimeline[accountId] = false;
                else
                    RefreshingTimeline.Add(accountId, false);

                Dispatcher.BeginInvoke(FinishedRefreshingTask);

                if (hasError)
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                        UiHelper.ShowToast(errorMessage);
                    else
                        UiHelper.ShowToast("There was a problem connecting to Twitter.");
                }

                return;
            }

            if (!timeline.Any())
            {
                Dispatcher.BeginInvoke(FinishedRefreshingTask);
                if (RefreshingTimeline.ContainsKey(accountId))
                    RefreshingTimeline[accountId] = false;
                else
                    RefreshingTimeline.Add(accountId, false);
                return;
            }

            UiHelper.SafeDispatch(delegate
            {

                ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTimeline(accountId);

                var lastItem = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].FirstOrDefault();
                long? newestId = null;

                if (lastItem != default(TimelineViewModel))
                    newestId = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Max(x => x.Id);

                bool hasGap = true;

                if (lastItem != null && ((IMehdohApp)(Application.Current)).ViewModel != null && ((IMehdohApp)(Application.Current)).ViewModel.Timeline != null && ((IMehdohApp)(Application.Current)).ViewModel.Timeline.Any() && ((IMehdohApp)(Application.Current)).ViewModel.Timeline.ContainsKey(accountId))
                {
                    if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Any())
                    {
                        var itemHasComeBack = timeline.Any(x => x.id == newestId);
                        if (itemHasComeBack)
                            hasGap = false;
                    }
                }
                else
                {
                    hasGap = false;
                }

                // Save the current position of the display
                UpdatePreRefreshColumnPositions();

                int newCount = 0;

                var newItems = new List<long>();

                foreach (var item in timeline)
                {
                    if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Any(x => x.Id == item.id))
                        continue;
                    ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Add(item.AsViewModel(accountId));
                    newItems.Add(item.id);

                    newCount++;
                }

                if (hasGap && newItems.Any() && newestId != null)
                {
                    ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Add(new TimelineViewModel
                                                                {
                                                                    IsGap = true,
                                                                    Id = newestId.Value,
                                                                    AccountId = accountId
                                                                });
                }

                UiHelper.SetTimelineCount(mainPivot,
                                            newCount.ToString(CultureInfo.CurrentUICulture) + ((hasGap) ? "+" : ""),
                                            accountId);


                // Save the current position of the display
                RestorePreRefreshColumnPositions();

                CheckListViewEventsAreSet();

                ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Apply(x => x.UpdateTime());

                FinishedRefreshingTask();

                if (RefreshingTimeline.ContainsKey(accountId))
                    RefreshingTimeline[accountId] = false;
                else
                    RefreshingTimeline.Add(accountId, false);

                CheckRefreshPhotos(accountId);

                CheckTweetMarkerPositionForTimeline(accountId);

                ThreadPool.QueueUserWorkItem(delegate
                                                {
                                                    var dsh = new DataStorageHelper();
                                                    dsh.SaveTimelineUpdates(accountId, timeline);

                                                    FriendsCache.ParseNewTimelines(timeline);

                                                });

            });
        }

        private void CheckTweetMarkerPositionForTimeline(long accountId)
        {

            // Has it been set, and removed?
            if (InitialTweetMarkerValues == null || !InitialTweetMarkerValues.ContainsKey(accountId))
                return;

            // Has it actually got a value?
            long? markerPositionId = InitialTweetMarkerValues[accountId];
            if (!markerPositionId.HasValue)
                return;

            // Does the item already exist?
            var itemWeWant = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].FirstOrDefault(x => x.Id == markerPositionId && !x.IsGap);
            if (itemWeWant != null)
            {

                // reset all of them
                ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Apply(x => x.IsSyncSetting = false);
                // set them all on again
                itemWeWant.IsSyncSetting = true;

                UiHelper.ScrollIntoView(mainPivot, ApplicationConstants.ColumnTypeTwitter, ApplicationConstants.ColumnTwitterTimeline, accountId, itemWeWant);

                var newValue = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].IndexOf(itemWeWant);
                UiHelper.SetTimelineCount(mainPivot, newValue.ToString(CultureInfo.CurrentUICulture), accountId);

                InitialTweetMarkerValues.Remove(accountId);
                return;
            }

            // OK not found

            // Is it newer than what we have?
            if (((IMehdohApp) (Application.Current)).ViewModel.Timeline[accountId].Any())
            {
                if (markerPositionId.Value >
                    ((IMehdohApp) (Application.Current)).ViewModel.Timeline[accountId].Max(x => x.Id))
                {
                    // Refresh the timeline and hopefully end up back here again
                    RefreshTimeline(accountId, null);
                }

                // Is it older than what we have?
                if (markerPositionId.Value <
                    ((IMehdohApp) (Application.Current)).ViewModel.Timeline[accountId].Min(x => x.Id))
                {
                    // get older ones
                    ThreadPool.QueueUserWorkItem(StartGetMoreTimeline, accountId);
                }
            }

            // Is it in the middle, in a gap?
            // TODO: Fill that gap!

        }

        private void CheckRefreshPhotos(long accountId)
        {
            if (ColumnHelper.ColumnConfig.Any(x => x.Value == ApplicationConstants.ColumnTwitterPhotoView && x.AccountId == accountId))
                RefreshPhotoView(accountId);
        }

        private void api_GetMentionsCompletedEvent(long accountId, List<ResponseTweet> mentions, bool hasError, string errorMessage)
        {

            UiHelper.HidePullToRefresh(mainPivot,
               ApplicationConstants.ColumnTypeTwitter,
               ApplicationConstants.ColumnTwitterMentions,
               accountId);

            if (mentions == null || !mentions.Any())
            {
                if (RefreshingMentions.ContainsKey(accountId))
                    RefreshingMentions[accountId] = false;
                else
                    RefreshingMentions.Add(accountId, false);

                Dispatcher.BeginInvoke(FinishedRefreshingTask);

                if (hasError)
                {
                    if (!string.IsNullOrEmpty(errorMessage))
                        UiHelper.ShowToast(errorMessage);
                    else
                        UiHelper.ShowToast("There was a problem connecting to Twitter.");
                }

                return;
            }

            var res = ViewModelHelper.MentionsResponseToView(accountId, mentions);

            UiHelper.SafeDispatch(delegate
                                       {
                                           ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMentions(accountId);
                                           //var lastItem = ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].FirstOrDefault();

                                           var count = 0;

                                           UpdatePreRefreshColumnPositions();

                                           foreach (var item in res.Where(item => ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].SingleOrDefault(x => x.Id == item.Id) == default(MentionsViewModel)))
                                           {
                                               ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMentions(accountId);
                                               ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Add(item);
                                               count++;
                                           }

                                           RestorePreRefreshColumnPositions();

                                           UiHelper.SetMentionsCount(mainPivot,
                                                                     count.ToString(CultureInfo.CurrentUICulture),
                                                                     accountId);

                                           if (((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Any())
                                               ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Apply(x => x.UpdateTime());

                                           FinishedRefreshingTask();

                                           if (RefreshingMentions.ContainsKey(accountId))
                                               RefreshingMentions[accountId] = false;
                                           else
                                               RefreshingMentions.Add(accountId, false);

                                           //if (lastItem != null)
                                           //{
                                           //    UiHelper.ScrollIntoView(mainPivot, ApplicationConstants.ColumnTypeTwitter, ApplicationConstants.ColumnTwitterMentions,
                                           //                            api.AccountId, lastItem);
                                           //}

                                           ThreadPool.QueueUserWorkItem(delegate
                                                                            {
                                                                                var dsh = new DataStorageHelper();
                                                                                dsh.SaveMentionUpdates(mentions, accountId);
                                                                            });

                                       });
        }

        private void mnuCompose_Click(object sender, EventArgs e)
        {
            string hashtags = string.Empty;

            long accountId = 0;

            try
            {
                accountId = GetAccountForCurrentColumn();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("mnuCompose_Click", ex);
            }
            if (accountId == 0)
            {
                MessageBox.Show("there are no twitter accounts currently set up. please add one via manage accounts.", "no twitter account", MessageBoxButton.OK);
                return;
            }

            NavigationService.Navigate(new Uri("/NewTweet.xaml?accountId=" + accountId + "&hashtags=" + hashtags, UriKind.Relative));
        }

        private void lstheader_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // Get list box
            var index = mainPivot.SelectedIndex;
            var pivot = mainPivot.Items[index] as PivotItem;

            if (pivot != null)
            {
                var listBox = pivot.Content as ListBox;
                if (listBox != null)
                {
                    if (listBox.Items.Any())
                    {
                        var firstItem = listBox.Items[0];
                        listBox.ScrollIntoView(firstItem);
                    }
                }
                else
                {
                    var dbListBox = pivot.Content as RadDataBoundListBox;
                    if (dbListBox != null)
                    {
                        if (dbListBox.ItemsSource.Count() > 0)
                        {
                            var firstItem = dbListBox.ItemsSource.ElementAt(0);
                            dbListBox.BringIntoView(firstItem);
                        }
                    }
                    else
                    {
                        var longList = pivot.Content as LongListSelector;
                        if (longList != null)
                        {
                            if (longList.ItemsSource.Count() > 0)
                            {
                                var firstItem = longList.ItemsSource.ElementAt(0);
                                longList.ScrollTo(firstItem);
                            }
                        }
                    }
                }
            }
        }


        private void mnuDmReply_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            if (menuItem == null || menuItem.Tag == null)
                return;

            var tag = menuItem.Tag.ToString();

            NavigationService.Navigate(new Uri("/NewTweet.xaml?dm=true&" + tag, UriKind.Relative));
        }

        private void mnuReply_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            if (menuItem == null || menuItem.Tag == null)
                return;

            var tag = menuItem.Tag.ToString();

            NavigationService.Navigate(new Uri("/NewTweet.xaml?" + tag, UriKind.Relative));
        }

        private void mnuEditRetweet_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            if (menuItem == null || menuItem.Tag == null)
                return;

            var tag = menuItem.Tag.ToString(); // this has the description

            var newTag = HttpUtility.UrlEncode(tag);

            NavigationService.Navigate(new Uri("/NewTweet.xaml?accountId=" + GetAccountForCurrentColumn() + "&isEditRt=true&text=" + newTag, UriKind.Relative));
        }

        private void mnuRetweet_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            if (menuItem == null || menuItem.Tag == null)
                return;

            var tag = menuItem.Tag as TimelineViewModel; // this is the viewmodel

            UiHelper.ShowProgressBar("retweeting");

            StartRefreshingTask();

            var api = new TwitterApi(tag.AccountId);
            api.RetweetCompletedEvent += api_RetweetCompletedEvent;
            api.Retweet(tag.Id);

        }

        private void api_RetweetCompletedEvent(object sender, EventArgs e)
        {

            // Update the item
            var api = sender as TwitterApi;

            if (api == null)
            {
                UiHelper.ShowToast("retweet failed");
                FinishedRefreshingTask();
                return;
            }

            api.RetweetCompletedEvent -= api_RetweetCompletedEvent;

            if (api.HasError && !string.IsNullOrEmpty(api.ErrorMessage))
            {
                UiHelper.SafeDispatch(() => MessageBox.Show(api.ErrorMessage, "retweet failed", MessageBoxButton.OK));
                FinishedRefreshingTask();
                return;
            }

            if (api.RetweetResponse == null || api.RetweetResponse.id == 0)
            {
                Dispatcher.BeginInvoke(FinishedRefreshingTask);
                return;
            }

            // Update the data context
            using (var dc = new MainDataContext())
            {
                var recs = from updates
                               in dc.Timeline
                           where updates.IdStr == api.RetweetResponse.retweeted_status.id_str
                           select updates;

                // var rec = recs.FirstOrDefault();

                foreach (var rec in recs)
                {
                    rec.RetweetDescripton = api.RetweetResponse.retweeted_status.text;
                    rec.RetweetUserDisplayName = api.RetweetResponse.retweeted_status.user.name;
                    rec.RetweetUserImageUrl = api.RetweetResponse.retweeted_status.user.profile_image_url;
                    rec.RetweetUserScreenName = api.RetweetResponse.retweeted_status.user.screen_name;

                    rec.DisplayName = api.RetweetResponse.user.name;
                    rec.ScreenName = api.RetweetResponse.user.screen_name;
                    rec.ProfileImageUrl = api.RetweetResponse.user.profile_image_url;

                    rec.IsRetweet = true;
                }

                dc.SubmitChanges();
            }

            Dispatcher.BeginInvoke(delegate
                                       {
                                           var accountId = api.AccountId;

                                           // update the item in the view
                                           if (((IMehdohApp)(Application.Current)).ViewModel.Timeline != null && ((IMehdohApp)(Application.Current)).ViewModel.Timeline.ContainsKey(accountId))
                                           {
                                               if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Any(x => x.Id == api.RetweetResponse.retweeted_status.id))
                                               {

                                                   var newItems = new List<TimelineViewModel>();

                                                   foreach (var res in ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Where(x => x.Id == api.RetweetResponse.retweeted_status.id))
                                                   {
                                                       if (res != null)
                                                       {
                                                           res.RetweetUserDisplayName = api.RetweetResponse.retweeted_status.user.name;
                                                           res.RetweetUserImageUrl = api.RetweetResponse.retweeted_status.user.profile_image_url;
                                                           res.RetweetUserScreenName = api.RetweetResponse.retweeted_status.user.screen_name;
                                                           res.Description = HttpUtility.HtmlDecode(api.RetweetResponse.retweeted_status.text);
                                                           res.DisplayName = api.RetweetResponse.user.name;
                                                           res.ScreenName = api.RetweetResponse.user.screen_name;
                                                           res.ImageUrl = api.RetweetResponse.user.profile_image_url;

                                                           res.IsRetweet = true;

                                                           newItems.Add(res);
                                                       }
                                                   }

                                                   foreach (var item in newItems)
                                                   {
                                                       ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Remove(item);
                                                       item.Reset();
                                                       ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Add(item);
                                                   }

                                               }

                                               FinishedRefreshingTask();
                                           }

                                           UiHelper.ShowToast("tweet retweeted!");
                                       });
        }

        private async void mnuUnFavourite_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            if (menuItem == null)
                return;

            var model = menuItem.Tag as FavouritesViewModel;

            if (model == null)
                return;

            UiHelper.ShowProgressBar(ApplicationResources.removingfavourite);

            StartRefreshingTask();

            var api = new TwitterApi(model.AccountId);
            var result = await api.UnfavouriteTweet(model.Id);
            api_UnfavouriteCompletedEvent(model.AccountId, result);

        }

        private async void api_UnfavouriteCompletedEvent(long accountId, ResponseUnfavourite unfavouriteResponse)
        {

            if (unfavouriteResponse == null)
            {
                Dispatcher.BeginInvoke(FinishedRefreshingTask);
                return;
            }

            using (var dh = new MainDataContext())
            {

                var tweetId = unfavouriteResponse.id;

                long sinceId = dh.Favourites.Max(x => x.Id);

                var api = new TwitterApi(accountId);
                var result = await api.GetFavourites(sinceId);
                api_GetFavouritesCompletedEvent(accountId, result, api.HasError, api.ErrorMessage);

                // Update the data context
                using (var dc = new MainDataContext())
                {
                    var recs = dc.Favourites.Where(updates => (updates.Id == tweetId &&
                                                               updates.ProfileId == accountId));

                    if (recs.Any())
                    {
                        dc.Favourites.DeleteAllOnSubmit(recs);
                        dc.SubmitChanges();
                    }
                }
            }

            Dispatcher.BeginInvoke(delegate
                                       {
                                           if (((IMehdohApp)(Application.Current)).ViewModel.Favourites != null &&
                                               ((IMehdohApp)(Application.Current)).ViewModel.Favourites.ContainsKey(accountId))
                                           {
                                               var res = ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].SingleOrDefault(x => x.Id == unfavouriteResponse.id);
                                               if (res != null)
                                               {
                                                   ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].Remove(res);
                                               }
                                           }

                                           FinishedRefreshingTask();

                                           var message = ApplicationResources.removefavourite;
                                           UiHelper.ShowToast(message);
                                       });
        }

        private async void mnuFavourite_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null)
                return;

            var model = menuItem.Tag as TimelineViewModel;

            long id = model.Id;
            long accountId = model.AccountId;

            var api = new TwitterApi(accountId);

            UiHelper.ShowProgressBar("adding " + ApplicationResources.favourite);

            RefreshingCount++;

            await api.FavouriteTweet(id);
            api_FavouriteCompletedEvent(accountId);
        }

        private async void api_FavouriteCompletedEvent(long accountId)
        {
            long sinceId;

            using (var dh = new MainDataContext())
            {
                sinceId = dh.Favourites.Any(x => x.ProfileId == accountId)
                              ? dh.Favourites.Where(x => x.ProfileId == accountId).Max(x => x.Id)
                              : 0;
            }

            var api = new TwitterApi(accountId);
            var result = await api.GetFavourites(sinceId);
            api_GetFavouritesCompletedEvent(accountId, result, api.HasError, api.ErrorMessage);

            var message = ApplicationResources.addfavourite;
            UiHelper.ShowToast(message);
        }

        private void mnuTrends_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Trends.xaml?accountId=" + GetAccountForCurrentColumn(), UriKind.Relative));
        }

        private void mnuViewMyProfile_Click(object sender, EventArgs e)
        {
            var accountId = GetAccountForCurrentColumn();
            var userScreen = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(accountId);

            NavigationService.Navigate(new Uri("/UserProfile.xaml?accountId=" + accountId + "&screen=" + userScreen, UriKind.Relative));
        }

        private void mnuProfile_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var userScreen = menuItem.Tag;

            NavigationService.Navigate(
                new Uri("/UserProfile.xaml?accountId=" + GetAccountForCurrentColumn() + "&screen=" + userScreen,
                        UriKind.Relative));
        }

        private void mainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckListViewEventsAreSet();
            AssignCorrectMenu();
        }


        private void AssignCorrectMenu()
        {
            if (ColumnHelper.ColumnConfig == null || !ColumnHelper.ColumnConfig.Any())
            {
                UiHelper.SafeDispatchSync(() =>
                                              {
                                                  ApplicationBar = (ApplicationBar)Resources["menuBlank"];
                                                  ApplicationBar.MatchOverriddenTheme();
                                              });
                SetAppBarOpacity();
                return;
            }

            int currentIndex = 0;

            UiHelper.SafeDispatch(() =>
                                      {
                                          if (mainPivot == null)
                                              return;

                                          currentIndex = mainPivot.SelectedIndex;
                                      });

            ThreadPool.QueueUserWorkItem(delegate
                                             {
                                                 try
                                                 {
                                                     var currentColumn = ColumnHelper.ColumnConfig[currentIndex];

                                                     bool refreshEnabled = false;

                                                     string menuKey = string.Empty;

                                                     var isOnWifi = ConnectionHelper.IsOnWifi();

                                                     switch (currentColumn.ColumnType)
                                                     {
                                                         case ApplicationConstants.ColumnTypeTwitter: // twitter
                                                             switch (currentColumn.Value)
                                                             {
                                                                 case ApplicationConstants.ColumnTwitterNewFollowers:
                                                                     menuKey = "menuTwitter";
                                                                     refreshEnabled = true;
                                                                     break;

                                                                 case ApplicationConstants.ColumnTwitterPhotoView:
                                                                     menuKey = "menuTwitter";
                                                                     break;

                                                                 case ApplicationConstants.ColumnTwitterFavourites:
                                                                     menuKey = "menuTwitterFavourites";
                                                                     refreshEnabled = true;
                                                                     break;

                                                                 default:
                                                                     menuKey = "menuTwitter";
                                                                     break;
                                                             }
                                                             break;

                                                         case ApplicationConstants.ColumnTypeTwitterSearch:
                                                             menuKey = "menuTwitter";
                                                             refreshEnabled = true;
                                                             break;

                                                         case ApplicationConstants.ColumnTypeTwitterList:
                                                             // twitter list
                                                             menuKey = "menuTwitter";
                                                             refreshEnabled = true;
                                                             break;

                                                         case ApplicationConstants.ColumnTypeFacebook: // facebook
                                                             menuKey = "menuFacebook";
                                                             refreshEnabled = true;
                                                             break;

                                                     }

                                                     if (popupWindow == null)
                                                     {
                                                         if (!string.IsNullOrEmpty(menuKey))
                                                         {
                                                             UiHelper.SafeDispatch(() =>
                                                                                       {
                                                                                           ApplicationBar = (ApplicationBar)Resources[menuKey];
                                                                                           ApplicationBar.MatchOverriddenTheme();
                                                                                           ApplicationBar.LocaliseMenu();
                                                                                           SetAppBarOpacity();

                                                                                           foreach (ApplicationBarIconButton button in ApplicationBar.Buttons)
                                                                                           {
                                                                                               if (button.Text == "refresh" || button.Text == "on" || button.Text == "off")
                                                                                               {
                                                                                                   if (StreamingSettings == null)
                                                                                                   {
                                                                                                       var sh = new SettingsHelper();
                                                                                                       StreamingSettings = sh.GetSettingsStreaming();
                                                                                                   }

                                                                                                   bool streamingOnMobile = StreamingSettings.StreamingOnMobile.HasValue &&
                                                                                                           StreamingSettings.StreamingOnMobile.Value;

                                                                                                   bool streamingValidOnCurrentConnection = false;

                                                                                                   if (isOnWifi)
                                                                                                       streamingValidOnCurrentConnection = true;
                                                                                                   else if (streamingOnMobile)
                                                                                                       streamingValidOnCurrentConnection = true;

                                                                                                   if (streamingValidOnCurrentConnection &&
                                                                                                       !refreshEnabled)
                                                                                                   {
                                                                                                       //button.IsEnabled = refreshEnabled;                            
                                                                                                       if (StreamingSettings.StreamingEnabled.HasValue &&
                                                                                                           StreamingSettings.StreamingEnabled.Value &&
                                                                                                           ((IMehdohApp)(Application.Current)).ViewModel.CurrentlyStreaming)
                                                                                                       {
                                                                                                           button.IconUri = new Uri("/Images/dark/streaming-on.png", UriKind.Relative);
                                                                                                           button.Text = "on";
                                                                                                       }
                                                                                                       else if (StreamingSettings.StreamingEnabled.HasValue &&
                                                                                                                StreamingSettings.StreamingEnabled.Value &&
                                                                                                               !((IMehdohApp)(Application.Current)).ViewModel.CurrentlyStreaming)
                                                                                                       {
                                                                                                           button.IconUri = new Uri("/Images/dark/streaming-off.png", UriKind.Relative);
                                                                                                           button.Text = "off";
                                                                                                       }
                                                                                                       else
                                                                                                       {
                                                                                                           button.IconUri = new Uri("/Images/76x76/dark/appbar.refresh.png", UriKind.Relative);
                                                                                                           button.Text = "refresh";
                                                                                                       }
                                                                                                   }
                                                                                                   else
                                                                                                   {
                                                                                                       button.IconUri = new Uri("/Images/76x76/dark/appbar.refresh.png", UriKind.Relative);
                                                                                                       button.Text = "refresh";
                                                                                                   }
                                                                                               }
                                                                                           }
                                                                                       });
                                                         }
                                                     }
                                                 }
                                                 catch (Exception)
                                                 {
                                                     UiHelper.SafeDispatch(() =>
                                                                               {
                                                                                   ApplicationBar = (ApplicationBar)Resources["menuBlank"];
                                                                                   ApplicationBar.MatchOverriddenTheme();
                                                                                   ApplicationBar.LocaliseMenu();
                                                                                   SetAppBarOpacity();
                                                                               });
                                                 }
                                             });
        }

        private void SetAppBarOpacity()
        {

            try
            {
                var themeManager = new ThemeHelper();
                var currentTheme = themeManager.GetCurrentTheme();

                if (((PhoneApplicationFrame)Application.Current.RootVisual).Orientation == PageOrientation.LandscapeRight ||
                    ((PhoneApplicationFrame)Application.Current.RootVisual).Orientation == PageOrientation.Landscape ||
                    ((PhoneApplicationFrame)Application.Current.RootVisual).Orientation == PageOrientation.LandscapeLeft ||
                    currentTheme == ThemeHelper.Theme.GenericModernLight ||
                    currentTheme == ThemeHelper.Theme.GenericModernDark)
                {
                    ApplicationBar.Opacity = 1.0;
                }
                else
                {
                    ApplicationBar.Opacity = 0.8;
                }
            }
            catch (Exception)
            {
            }

        }

        private List<string> ConfiguredEvents { get; set; }

        private void CheckListViewEventsAreSet()
        {

            UiHelper.SafeDispatch(() =>
                                      {
                                          if (mainPivot == null || mainPivot.Items.Count == 0)
                                              return;

                                          // Any columns?
                                          if (!ColumnHelper.ColumnConfig.Any())
                                              return;

                                          var currentIndex = mainPivot.SelectedIndex;

                                          var currentColumn = ColumnHelper.ColumnConfig[currentIndex];

                                          var configuredKey = currentColumn.Value + "_" + currentColumn.AccountId;

                                          if (ConfiguredEvents.Contains(configuredKey))
                                              return;

                                          var pivotItem = mainPivot.Items[currentIndex] as PivotItem;
                                          if (pivotItem == null)
                                              return;

                                          var listBox = pivotItem.Content as ListBox;
                                          if (listBox == null)
                                              return;

                                          var currentListBox =
                                              (ScrollViewer)FindElementRecursive(listBox, typeof(ScrollViewer));
                                          if (currentListBox == null)
                                              return;

                                          // Visual States are always on the first child of the control template 
                                          var element = VisualTreeHelper.GetChild(currentListBox, 0) as FrameworkElement;
                                          if (element == null)
                                              return;

                                          var verticalVisualStateGroup = FindVisualState(element, "VerticalCompression");
                                          if (verticalVisualStateGroup != null)
                                          {
                                              verticalVisualStateGroup.CurrentStateChanging += lstViewVertical_CurrentStateChanging;
                                              ConfiguredEvents.Add(configuredKey);
                                          }
                                      });
        }

        private void lstViewVertical_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {

            if (e.NewState.Name == "CompressionBottom")
            {
                #region CompressionBottom

                var currentCol = ColumnHelper.ColumnConfig[mainPivot.SelectedIndex];

                switch (currentCol.ColumnType)
                {
                    case ApplicationConstants.ColumnTypeTwitter:
                        switch (currentCol.Value)
                        {
                            case ApplicationConstants.ColumnTwitterTimeline:
                                UiHelper.ShowProgressBar("fetching more timeline");
                                ThreadPool.QueueUserWorkItem(StartGetMoreTimeline, currentCol.AccountId);
                                break;

                            case ApplicationConstants.ColumnTwitterMentions:
                                UiHelper.ShowProgressBar("fetching more mentions");
                                ThreadPool.QueueUserWorkItem(StartGetMoreMentions, currentCol.AccountId);
                                break;

                            case ApplicationConstants.ColumnTwitterMessages:
                                UiHelper.ShowProgressBar("fetching more messages");
                                ThreadPool.QueueUserWorkItem(StartGetMoreMessages, currentCol.AccountId);
                                break;

                            case ApplicationConstants.ColumnTwitterFavourites:
                                UiHelper.ShowProgressBar(ApplicationResources.fetchingmorefavourites);
                                ThreadPool.QueueUserWorkItem(StartGetMoreFavourites, currentCol.AccountId);
                                break;

                            case ApplicationConstants.ColumnTwitterRetweetsOfMe:
                                UiHelper.ShowProgressBar("fetching more retweets");
                                ThreadPool.QueueUserWorkItem(StartGetMoreRetweetsOfMe, currentCol.AccountId);
                                break;

                            //case ApplicationConstants.Column_Twitter_Retweeted_By_Me:
                            //    UiHelper.ShowProgressBar("fetching more retweets");
                            //    ThreadPool.QueueUserWorkItem(StartGetMoreRetweetsByMe, currentCol.AccountId);
                            //    break;

                            //case ApplicationConstants.Column_Twitter_Retweeted_To_Me:
                            //    UiHelper.ShowProgressBar("fetching more retweets");
                            //    ThreadPool.QueueUserWorkItem(StartGetMoreRetweets, currentCol.AccountId);
                            //    break;

                            case ApplicationConstants.ColumnTwitterNewFollowers:
                                // do nothing
                                break;
                        }

                        break;

                    case ApplicationConstants.ColumnTypeTwitterList: // list
                        UiHelper.ShowProgressBar("fetching more " + currentCol.DisplayName.ToLower(CultureInfo.CurrentUICulture));
                        var state = new ListState
                                        {
                                            AccountId = currentCol.AccountId,
                                            ListId = currentCol.Value
                                        };
                        ThreadPool.QueueUserWorkItem(StartGetMoreList, state);
                        break;

                    case ApplicationConstants.ColumnTypeTwitterSearch:
                        UiHelper.ShowProgressBar("fetching more " + currentCol.DisplayName.ToLower(CultureInfo.CurrentUICulture) +
                                                 " search results");
                        var searchState = new SearchState
                                              {
                                                  AccountId = currentCol.AccountId,
                                                  SearchQuery = currentCol.Value
                                              };
                        ThreadPool.QueueUserWorkItem(StartGetMoreSearch, searchState);
                        break;

                }

                #endregion
            }

            if (e.NewState.Name == "CompressionTop")
            {
                var currentCol = ColumnHelper.ColumnConfig[mainPivot.SelectedIndex];

                switch (currentCol.ColumnType)
                {
                    case ApplicationConstants.ColumnTypeSoundcloud:
                        break;
                    default:
                        var selectedIndex = mainPivot.SelectedIndex;
                        UiHelper.SetColumnCount(mainPivot, selectedIndex, "0");
                        break;
                }

                switch (currentCol.ColumnType)
                {
                    case ApplicationConstants.ColumnTypeTwitter:
                        switch (currentCol.Value)
                        {
                            case ApplicationConstants.ColumnTwitterTimeline:
                                ThreadPool.QueueUserWorkItem(delegate
                                                                 {
                                                                     if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[currentCol.AccountId].Any())
                                                                     {
                                                                         var tmApi = new TweetMarkerApi();
                                                                         var tweetId = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[currentCol.AccountId].Max(x => x.Id);
                                                                         tmApi.SetLastTimelineRead(currentCol.AccountId, tweetId);
                                                                     }
                                                                 });
                                break;

                            case ApplicationConstants.ColumnTwitterMentions:
                                ThreadPool.QueueUserWorkItem(delegate
                                                                 {
                                                                     if (((IMehdohApp)(Application.Current)).ViewModel.Mentions[currentCol.AccountId].Any())
                                                                     {
                                                                         var tmApi = new TweetMarkerApi();
                                                                         var tweetId = ((IMehdohApp)(Application.Current)).ViewModel.Mentions[currentCol.AccountId].Max(x => x.Id);
                                                                         tmApi.SetLastMentionsRead(currentCol.AccountId, tweetId);
                                                                     }
                                                                 });
                                break;
                        }
                        break;
                }

            }

        }


        private async void StartGetMoreFavourites(object state)
        {
            long accountId = (long)state;

            long oldestItem = 0;

            if (((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].Any())
                oldestItem = ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].Min(x => x.Id);

            var res = ((IMehdohApp)(Application.Current)).ViewModel.GetMoreFavourites(oldestItem, accountId);
            if (res.Any())
            {
                ((IMehdohApp)(Application.Current)).ViewModel.ConfirmFavourites(accountId);

                foreach (var item in res)
                {
                    ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].Add(item);
                }

                UiHelper.HideProgressBar();
            }
            else
            {
                var api = new TwitterApi(accountId);
                var result = await api.GetFavourites(0, oldestItem);
                api_GetFavouritesCompletedEvent(accountId, result, api.HasError, api.ErrorMessage);
            }
        }

        protected bool GettingMoreTwitterMessages { get; set; }

        private void StartGetMoreMessages(object state)
        {

            if (GettingMoreTwitterMessages)
                return;

            GettingMoreTwitterMessages = true;

            long accountId = (long)state;
            long oldestItem = 0;

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMessages(accountId);

            if (((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Any())
                oldestItem = ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Min(x => x.Id);

            var res = ((IMehdohApp)(Application.Current)).ViewModel.GetMoreMessages(oldestItem, accountId);
            if (res != null && res.Any())
            {
                UiHelper.SafeDispatchSync(() =>
                {
                    foreach (var item in res)
                        ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Add(item);
                });
            }

            UiHelper.HideProgressBar();
            GettingMoreTwitterMessages = false;
        }


        protected bool GettingMoreTwitterSearch { get; set; }

        private async void StartGetMoreSearch(object state)
        {
            if (GettingMoreTwitterSearch)
                return;

            GettingMoreTwitterSearch = true;

            var listState = state as SearchState;

            long oldestItem = 0;

            if (((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch.ContainsKey(listState.AccountId) &&
                ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[listState.AccountId].ContainsKey(listState.SearchQuery))
                if (((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[listState.AccountId][listState.SearchQuery].Any())
                    oldestItem = ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[listState.AccountId][listState.SearchQuery].Min(x => x.Id);

            long accountId = listState.AccountId;

            var bfh = new BackfillHelper(accountId);
            //bfh.GetMoreTwitterSearchCompletedEvent += bfh_GetMoreTwitterSearchCompletedEvent;
            var result = await bfh.GetMoreTwitterSearch(listState.SearchQuery, oldestItem, 0, accountId);
            bfh_GetMoreTwitterSearchCompletedEvent(accountId, result, listState.SearchQuery, bfh.MoreTweetsButton);
        }

        protected bool GettingMoreList { get; set; }

        private void StartGetMoreList(object state)
        {
            if (GettingMoreList)
                return;

            var listState = state as ListState;

            if (listState == null)
                return;

            GettingMoreList = true;

            long oldestItem = 0;

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTwitterList(listState.ListId);

            if (((IMehdohApp)(Application.Current)).ViewModel.Lists.ContainsKey(listState.ListId) && ((IMehdohApp)(Application.Current)).ViewModel.Lists[listState.ListId].Any())
                oldestItem = ((IMehdohApp)(Application.Current)).ViewModel.Lists[listState.ListId].Min(x => x.Id);

            long accountId = listState.AccountId;

            var bfh = new BackfillHelper(accountId);
            bfh.GetMoreListCompletedEvent += bfh_GetMoreListCompletedEvent;
            bfh.GetMoreList(listState.ListId, oldestItem, 0, accountId);
        }

        private void bfh_GetMoreTwitterSearchCompletedEvent(long accountId, List<TimelineViewModel> moreTwitterSearch, string searchQuery, TimelineViewModel tweetsButton)
        {


            bool isMoreButton = false;

            UiHelper.SafeDispatch(() =>
            {
                try
                {
                    ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTwitterSearch(accountId, searchQuery);

                    TimelineViewModel moreTweetsButton = null;

                    if (tweetsButton != null)
                        moreTweetsButton = tweetsButton;

                    if (moreTweetsButton != null)
                    {
                        isMoreButton = true;
                    }

                    var newItems = new List<TimelineViewModel>();

                    if (moreTwitterSearch != null && moreTwitterSearch.Count > 0)
                    {
                        foreach (var item in moreTwitterSearch)
                        {
                            var all = ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].All(x => x.Id != item.Id);
                            if (!all)
                            {
                                continue;
                            }
                            newItems.Add(item);
                        }
                    }

                    int newCount = newItems.Count;

                    if (newCount == 0)
                    {
                        if (moreTweetsButton != null)
                        {
                            if (((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Contains(moreTweetsButton))
                            {
                                ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Remove(moreTweetsButton);
                            }

                        }

                        return;
                    }

                    int existingCount;

                    var existingCountString = UiHelper.GetTwitterSearchCount(mainPivot, searchQuery, accountId);

                    if (int.TryParse(existingCountString.Replace("+", ""), out existingCount))
                        newCount += existingCount;

                    //var api = new TwitterApi(accountId);

                    bool hasGap = true;

                    var oldMin = moreTwitterSearch.Min(x => x.Id);
                    long newMin;

                    if (((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Any(x => x.Id < oldMin))
                        newMin = ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Where(x => x.Id < oldMin).Max(x => x.Id);
                    else
                        newMin = oldMin;

                    if (isMoreButton)
                    {
                        // Save the current position of the display
                        UpdatePreRefreshColumnPositions();
                    }

                    foreach (var newItem in newItems)
                    {
                        // this should never be the case, as these are all new items, but check anyway
                        if (((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].All(x => x.Id != newItem.Id))
                        {
                            ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Add(newItem);
                        }
                    }

                    if (isMoreButton)
                    {
                        // Save the current position of the display
                        RestorePreRefreshColumnPositions();

                        // Only update the timeline count if they've pressed the button
                        UiHelper.SetTwitterSearchCount(mainPivot, newCount.ToString(), accountId, searchQuery);

                        if (hasGap)
                        {
                            ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Add(new TimelineViewModel
                            {
                                IsGap = true,
                                Id = newMin,
                                AccountId = accountId
                            });
                            UiHelper.SetTwitterSearchCount(mainPivot, newCount.ToString() + "+", accountId, searchQuery);
                        }
                    }

                    if (moreTweetsButton != null)
                    {
                        if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Contains(moreTweetsButton))
                        {
                            UiHelper.ScrollIntoView(mainPivot, ApplicationConstants.ColumnTypeTwitterSearch, searchQuery, accountId, moreTweetsButton);
                            ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Remove(moreTweetsButton);
                        }

                    }

                    ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery].Apply(x => x.UpdateTime());


                }
                catch (Exception)
                {
                    //BugSense.BugSenseHandler.Instance.LogException(exception, "error in bfh_GetMoreTimelineCompletedEvent");
                }
                finally
                {
                    //lstTimeline.UpdateLayout();
                    UiHelper.HideProgressBar();
                    GettingMoreTwitterSearch = false;
                }
            });

        }

        private void bfh_GetMoreListCompletedEvent(object sender, EventArgs e)
        {

            var bfh = sender as BackfillHelper;
            if (bfh == null)
                return;

            long accountId = bfh.AccountId;
            bool isMoreButton = false;

            UiHelper.SafeDispatch(() =>
            {
                try
                {
                    ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTwitterList(bfh.ListId);

                    TimelineViewModel moreTweetsButton = null;

                    if (bfh.MoreTweetsButton != null)
                        moreTweetsButton = bfh.MoreTweetsButton;

                    if (moreTweetsButton != null)
                    {
                        isMoreButton = true;
                    }

                    var res = bfh.MoreList;

                    var newItems = new List<TimelineViewModel>();

                    if (res != null && res.Count > 0)
                    {
                        foreach (var item in res)
                        {
                            var all = ((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].All(x => x.Id != item.Id);
                            if (!all)
                            {
                                continue;
                            }

                            newItems.Add(item);
                        }
                    }

                    int newCount = newItems.Count;

                    if (newCount == 0)
                    {
                        if (moreTweetsButton != null)
                        {
                            if (((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].Contains(moreTweetsButton))
                            {
                                ((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].Remove(moreTweetsButton);
                            }
                        }
                        return;
                    }

                    int existingCount;

                    var existingCountString = UiHelper.GetListCount(mainPivot, bfh.ListId, accountId);

                    if (int.TryParse(existingCountString.Replace("+", ""), out existingCount))
                        newCount += existingCount;

                    bool hasGap = true;

                    var oldMin = bfh.MoreList.Min(x => x.Id);
                    long newMin;

                    if (((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].Any(x => x.Id < oldMin))
                        newMin = ((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].Where(x => x.Id < oldMin).Max(x => x.Id);
                    else
                        newMin = oldMin;

                    if (isMoreButton)
                    {
                        // Save the current position of the display
                        UpdatePreRefreshColumnPositions();
                    }

                    foreach (var newItem in newItems)
                    {
                        // this should never be the case, as these are all new items, but check anyway
                        if (((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].All(x => x.Id != newItem.Id))
                        {
                            ((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].Add(newItem);
                        }
                    }

                    if (isMoreButton)
                    {
                        // Save the current position of the display
                        RestorePreRefreshColumnPositions();

                        // Only update the timeline count if they've pressed the button
                        UiHelper.SetListCount(mainPivot, bfh.ListId, newCount.ToString(), accountId);

                        if (hasGap)
                        {
                            ((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].Add(new TimelineViewModel
                            {
                                IsGap = true,
                                Id = newMin,
                                AccountId = accountId
                            });
                            UiHelper.SetListCount(mainPivot, bfh.ListId, newCount.ToString() + "+", accountId);
                        }
                    }

                    if (moreTweetsButton != null)
                    {
                        if (((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].Contains(moreTweetsButton))
                        {
                            UiHelper.ScrollIntoView(mainPivot, ApplicationConstants.ColumnTypeTwitterList, bfh.ListId, accountId, moreTweetsButton);
                            ((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].Remove(moreTweetsButton);
                        }
                    }

                    ((IMehdohApp)(Application.Current)).ViewModel.Lists[bfh.ListId].Apply(x => x.UpdateTime());

                }
                catch (Exception)
                {
                    //BugSense.BugSenseHandler.Instance.LogException(exception, "error in bfh_GetMoreListCompletedEvent");
                }
                finally
                {
                    //lstTimeline.UpdateLayout();
                    UiHelper.HideProgressBar();
                    GettingMoreList = false;
                }

            });

        }


        private bool GettingMoreMention { get; set; }

        private void StartGetMoreMentions(object state)
        {
            if (GettingMoreMention)
                return;

            GettingMoreMention = true;

            long accountId = (long)state;
            long oldestItem = 0;

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMentions(accountId);

            if (((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Any())
                oldestItem = ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Min(x => x.Id);

            var bfh = new BackfillHelper(accountId);
            bfh.GetMoreMentionCompletedEvent += bfh_GetMoreMentionCompletedEvent;
            bfh.GetMoreMention(oldestItem, accountId);
        }

        private void bfh_GetMoreMentionCompletedEvent(object sender, EventArgs e)
        {
            UiHelper.SafeDispatch(() =>
                                      {
                                          try
                                          {
                                              var bfh = sender as BackfillHelper;
                                              var accountId = bfh.AccountId;
                                              var res = bfh.MoreMention;
                                              if (res != null && res.Count > 0)
                                              {
                                                  foreach (
                                                      var item in
                                                          res.Where(
                                                              item =>
                                                              ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].SingleOrDefault(
                                                                  x => x.Id == item.Id) == null))
                                                  {
                                                      ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Add(item);
                                                  }
                                              }
                                          }
                                          finally
                                          {
                                              UiHelper.HideProgressBar();
                                              GettingMoreMention = false;
                                          }
                                      });
        }

        #region Timeline Backfill

        //private bool GettingMoreRetweets { get; set; }

        //private void StartGetMoreRetweets(object state)
        //{
        //    if (GettingMoreRetweets)
        //        return;

        //    GettingMoreRetweets = true;

        //    var accountId = (long)state;

        //    long maxId = 0;

        //    if (((IMehdohApp)(Application.Current)).ViewModel.RetweetedToMe[accountId].Any())
        //        maxId = ((IMehdohApp)(Application.Current)).ViewModel.RetweetedToMe[accountId].Min(x => x.Id) + 1;

        //    var bfh = new BackfillHelper(accountId);
        //    bfh.GetMoreRetweetedToMeCompletedEvent += new EventHandler(bfh_GetMoreRetweetedToMeCompletedEvent);
        //    bfh.GetMoreRetweetedToMe(maxId, 0, accountId);
        //}

        //void bfh_GetMoreRetweetedToMeCompletedEvent(object sender, EventArgs e)
        //{

        //    UiHelper.SafeDispatch(() =>
        //    {
        //        try
        //        {
        //            var bfh = sender as BackfillHelper;
        //            var accountId = bfh.AccountId;
        //            var res = bfh.MoreRetweetedToMe;
        //            if (res != null && res.Any())
        //            {
        //                foreach (var item in res.Where(item => ((IMehdohApp)(Application.Current)).ViewModel.RetweetedToMe[accountId].SingleOrDefault(x => x.Id == item.Id) == null))
        //                {
        //                    ((IMehdohApp)(Application.Current)).ViewModel.RetweetedToMe[accountId].Add(item);
        //                }
        //            }

        //        }
        //        finally
        //        {
        //            UiHelper.HideProgressBar();
        //            GettingMoreRetweets = false;
        //        }
        //    });

        //}


        //private bool GettingMoreRetweetedByMe { get; set; }

        //private void StartGetMoreRetweetsByMe(object state)
        //{

        //    if (GettingMoreRetweetedByMe)
        //        return;

        //    GettingMoreRetweetedByMe = true;

        //    var accountId = (long)state;

        //    long maxId = 0;

        //    if (((IMehdohApp)(Application.Current)).ViewModel.RetweetedByMe[accountId].Any())
        //        maxId = ((IMehdohApp)(Application.Current)).ViewModel.RetweetedByMe[accountId].Min(x => x.Id) + 1;

        //    var bfh = new BackfillHelper(accountId);
        //    bfh.GetMoreRetweetedByMeCompletedEvent += new EventHandler(bfh_GetMoreRetweetedByMeCompletedEvent);
        //    bfh.GetMoreRetweetedByMe(maxId, 0, accountId);
        //}

        //void bfh_GetMoreRetweetedByMeCompletedEvent(object sender, EventArgs e)
        //{

        //    UiHelper.SafeDispatch(() =>
        //    {
        //        try
        //        {
        //            var bfh = sender as BackfillHelper;
        //            var accountId = bfh.AccountId;
        //            var res = bfh.MoreRetweetedByMe;
        //            if (res != null && res.Count > 0)
        //            {
        //                foreach (var item in res.Where(item => ((IMehdohApp)(Application.Current)).ViewModel.RetweetedByMe[accountId].SingleOrDefault(x => x.Id == item.Id) == null))
        //                {
        //                    ((IMehdohApp)(Application.Current)).ViewModel.RetweetedByMe[accountId].Add(item);
        //                }
        //            }

        //        }
        //        finally
        //        {
        //            UiHelper.HideProgressBar();
        //            GettingMoreRetweetedByMe = false;
        //        }
        //    });

        //}


        private bool GettingMoreRetweetsOfMe { get; set; }

        private void StartGetMoreRetweetsOfMe(object state)
        {
            if (GettingMoreRetweetsOfMe)
                return;

            GettingMoreRetweetsOfMe = true;

            var accountId = (long)state;

            long maxId = 0;

            if (((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].Any())
                maxId = ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].Min(x => x.Id) + 1;

            var bfh = new BackfillHelper(accountId);
            bfh.GetMoreRetweetsOfMeCompletedEvent += bfh_GetMoreRetweetsOfMeCompletedEvent;
            bfh.GetMoreRetweetsOfMe(maxId, 0, accountId);
        }

        private void bfh_GetMoreRetweetsOfMeCompletedEvent(object sender, EventArgs e)
        {
            UiHelper.SafeDispatch(() =>
                                      {
                                          try
                                          {
                                              var bfh = sender as BackfillHelper;
                                              var accountId = bfh.AccountId;
                                              var res = bfh.MoreRetweetsOfMe;
                                              if (res != null && res.Count > 0)
                                              {
                                                  foreach (
                                                      var item in
                                                          res.Where(
                                                              item =>
                                                              ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].SingleOrDefault(
                                                                  x => x.Id == item.Id) == null))
                                                  {
                                                      ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].Add(item);
                                                  }
                                              }
                                          }
                                          finally
                                          {
                                              UiHelper.HideProgressBar();
                                              GettingMoreRetweetsOfMe = false;
                                          }
                                      });
        }

        private bool GettingMoreTimeline { get; set; }

        private async void StartGetMoreTimeline(object state)
        {
            if (GettingMoreTimeline)
                return;

            GettingMoreTimeline = true;

            var accountId = (long)state;

            long oldestItem = 0;

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTimeline(accountId);

            if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Any())
                oldestItem = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Min(x => x.Id);

            var bfh = new BackfillHelper(accountId);
            var result = await bfh.GetMoreTimeline(oldestItem, 0, accountId);
            bfh_GetMoreTimelineCompletedEvent(accountId, result, bfh.MoreTweetsButton);
        }

        private void bfh_GetMoreTimelineCompletedEvent(long accountId, List<TimelineViewModel> moreTimeline, TimelineViewModel tweetsButton)
        {

            bool isMoreButton = false;

            UiHelper.SafeDispatch(() =>
            {
                try
                {
                    ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTimeline(accountId);

                    TimelineViewModel moreTweetsButton = null;

                    if (tweetsButton != null)
                        moreTweetsButton = tweetsButton;

                    if (moreTweetsButton != null)
                    {
                        isMoreButton = true;
                    }

                    var res = moreTimeline;

                    var newItems = new List<TimelineViewModel>();

                    if (res != null && res.Count > 0)
                    {
                        foreach (var item in res)
                        {
                            var all = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].All(x => x.Id != item.Id);
                            if (!all)
                            {
                                continue;
                            }

                            newItems.Add(item);
                        }
                    }

                    int newCount = newItems.Count;

                    if (newCount == 0)
                    {
                        if (moreTweetsButton != null)
                        {
                            if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Contains(moreTweetsButton))
                            {
                                ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Remove(moreTweetsButton);
                            }
                        }
                        return;
                    }

                    int existingCount;

                    var existingCountString = UiHelper.GetTimelineCount(mainPivot, accountId);

                    if (int.TryParse(existingCountString.Replace("+", ""), out existingCount))
                        newCount += existingCount;

                    bool hasGap = true;

                    var oldMin = moreTimeline.Min(x => x.Id);
                    long newMin;

                    if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Any(x => x.Id < oldMin))
                        newMin = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Where(x => x.Id < oldMin).Max(x => x.Id);
                    else
                        newMin = oldMin;

                    if (isMoreButton)
                    {
                        // Save the current position of the display
                        UpdatePreRefreshColumnPositions();
                    }

                    foreach (var newItem in newItems)
                    {
                        // this should never be the case, as these are all new items, but check anyway
                        if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].All(x => x.Id != newItem.Id))
                        {
                            ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Add(newItem);
                        }
                    }

                    if (isMoreButton)
                    {
                        // Save the current position of the display
                        RestorePreRefreshColumnPositions();

                        // Only update the timeline count if they've pressed the button
                        UiHelper.SetTimelineCount(mainPivot, newCount.ToString(), accountId);

                        if (hasGap)
                        {
                            ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Add(new TimelineViewModel
                                                                    {
                                                                        IsGap = true,
                                                                        Id = newMin,
                                                                        AccountId = accountId
                                                                    });
                            UiHelper.SetTimelineCount(mainPivot, newCount.ToString(CultureInfo.InvariantCulture) + "+", accountId);
                        }
                    }

                    if (moreTweetsButton != null)
                    {
                        if (((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Contains(moreTweetsButton))
                        {
                            UiHelper.ScrollIntoView(mainPivot, ApplicationConstants.ColumnTypeTwitter, ApplicationConstants.ColumnTwitterTimeline, accountId, moreTweetsButton);
                            ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Remove(moreTweetsButton);
                        }

                    }

                    ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Apply(x => x.UpdateTime());

                }
                catch (Exception)
                {
                    //BugSense.BugSenseHandler.Instance.LogException(exception, "error in bfh_GetMoreTimelineCompletedEvent");
                }
                finally
                {
                    //lstTimeline.UpdateLayout();
                    UiHelper.HideProgressBar();
                    GettingMoreTimeline = false;

                    CheckTweetMarkerPositionForTimeline(accountId);
                }

            });
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

        private void mnuFollowSuggestions_Click(object sender, EventArgs e)
        {
            UiHelper.SafeDispatch(() => NavigationService.Navigate(new Uri("/FollowSuggestions.xaml?accountId=" + GetAccountForCurrentColumn(), UriKind.Relative)));
        }

        private void mnuCustomise_Click(object sender, EventArgs e)
        {
            UiHelper.SafeDispatch(() => NavigationService.Navigate(new Uri("/Customise.xaml", UriKind.Relative)));
        }

        private void mnuAccounts_Click(object sender, EventArgs e)
        {
            UiHelper.SafeDispatch(() => NavigationService.Navigate(new Uri("/AccountManagement.xaml", UriKind.Relative)));
        }

        private long GetAccountForCurrentColumn()
        {

            if (ColumnHelper.ColumnConfig == null || !ColumnHelper.ColumnConfig.Any())
            {

                // work out if we have any twitter ids
                var storageHelper = new StorageHelper();
                var users = storageHelper.GetAuthorisedTwitterUsers();

                if (users != null && users.Any())
                {
                    var firstUser = users.FirstOrDefault();
                    if (firstUser != null)
                    {
                        return firstUser.UserId;
                    }
                }

                return 0;
            }

            if (mainPivot == null)
                return 0;

            var currentIndex = mainPivot.SelectedIndex;

            ColumnModel currentColumn;

            try
            {
                currentColumn = ColumnHelper.ColumnConfig[currentIndex];
            }
            catch (Exception)
            {
                currentColumn = ColumnHelper.ColumnConfig.FirstOrDefault();
                if (currentColumn == default(ColumnModel))
                {
                    return 0;
                }
            }

            return currentColumn.AccountId;

        }

        private void mnuManageMutes_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Mutes.xaml", UriKind.Relative));         
        }

        #region Tweet Marker Support

        private Dictionary<long, long?> InitialTweetMarkerValues { get; set; }

        private void UpdateTweetMarker()
        {
            var sh = new SettingsHelper();
            if (!sh.GetUseTweetMarker())
                return;

            List<long> accountIds = null;

            try
            {
                accountIds = ColumnHelper.ColumnConfig.Where(x => x.ColumnType == ApplicationConstants.ColumnTypeTwitter).Select(x => x.AccountId).Distinct().ToList();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("UpdateTweetMarker", ex);
            }

            if (accountIds == null)
                return;

            foreach (var accountId in accountIds)
            {

                try
                {
                    RadDataBoundListBox timelineList;

                    try
                    {
                        timelineList = UiHelper.GetTimelineList(mainPivot, accountId) as RadDataBoundListBox;
                    }
                    catch (Exception)
                    {
                        // go onto the next 
                        break;
                    }

                    if (timelineList == null)
                        break;

                    var topItem = timelineList.TopVisibleItem as TimelineViewModel;

                    if (topItem == null)
                        break;

                    long timelineId = topItem.Id;

                    var tmApi = new TweetMarkerApi();
                    tmApi.SetLastReadCompletedEvent += tmApi_SetLastReadCompletedEvent;
                    tmApi.SetLastTimelineRead(accountId, timelineId);

                    // reset all of them
                    ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Apply(x => x.IsSyncSetting = false);

                    // set them all on again
                    topItem.IsSyncSetting = true;

                }
                catch
                {
                    // ignore for now. probably crashed in TopVisibleItem
                }


            }

        }

        void tmApi_SetLastReadCompletedEvent(object sender, EventArgs e)
        {
            var api = sender as TweetMarkerApi;
            if (api == null)
                return;

            api.SetLastReadCompletedEvent -= tmApi_SetLastReadCompletedEvent;
            api = null;
        }

        private void GetTweetMarkerSettings()
        {

            UiHelper.ShowProgressBar("getting tweetmarker settings");

            InitialTweetMarkerValues = new Dictionary<long, long?>();

            // get all visible twitter accounts
            var accountIds = ColumnHelper.ColumnConfig.Where(x => x.ColumnType == ApplicationConstants.ColumnTypeTwitter).Select(x => x.AccountId).Distinct().ToList();

            foreach (var accountId in accountIds)
            {
                InitialTweetMarkerValues.Add(accountId, null);
            }

            foreach (var accountId in accountIds)
            {
                // first we want the tweet marker settings
                var tmApi = new TweetMarkerApi();
                tmApi.GetLastReadCompletedEvent += tweetMarkerGetLastReadCompletedEvent;
                tmApi.GetLastRead(accountId);
            }

        }

        private object tweetMarkerLock = new object();

        private void tweetMarkerGetLastReadCompletedEvent(object sender, EventArgs eventArgs)
        {
            bool isCompleted;

            try
            {
                var api = sender as TweetMarkerApi;

                InitialTweetMarkerValues[api.AccountId] = api.LastTimelineId;

                lock (tweetMarkerLock)
                {
                    isCompleted = InitialTweetMarkerValues.ToList().All(x => x.Value != null);
                }
            }
            catch (Exception)
            {
                isCompleted = true;
            }

            if (isCompleted)
            {
                // OK we have all the settings we need, so let's go off and get the values!
                UiHelper.HideProgressBar();
                UiHelper.ShowProgressBar("looking for new updates");
                ThreadPool.QueueUserWorkItem(PerformInitialRefresh);
                PerformedInitialRefresh = true;
            }

        }

        #endregion


    }

}