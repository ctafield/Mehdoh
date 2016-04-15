// *********************************************************************************************************
// <copyright file="UserProfile.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using Clarity.Phone.Extensions;
using Cloudoh.UserControls;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.ErrorLogging;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.Resources;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Telerik.Windows.Controls;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;
using SelectTwitterAccount = FieldOfTweets.Common.UI.UserControls.SelectTwitterAccount;

#endregion

namespace Mitter
{
    public partial class UserProfile : AnimatedBasePage
    {
        #region Constructor

        public UserProfile()
        {
            InitializeComponent();
            DataLoaded = false;

            // accountId, is friend
            AccountIds = new List<AccountFriendViewModel>();
            Loaded += UserProfile_Loaded;
            AnimationContext = LayoutRoot;

            Timeline = new SortedObservableCollection<TimelineViewModel>();
            Mentions = new SortedObservableCollection<SearchResultViewModel>();

            // This value is determined by the friendship response
            CanSendDm = false;
        }

        #endregion

        #region Properties

        private long AccountId { get; set; }

        public TwitterAccountViewModel Profile { get; set; }

        private bool CanSendDm { get; set; }

        private string ScreenName { get; set; }

        private bool DataLoaded { get; set; }

        protected DialogService SelectAccountPopup { get; set; }

        // Contains a list of the current account Ids
        protected List<AccountFriendViewModel> AccountIds { get; set; }

        private bool EventsSetOnTimeline { get; set; }
        private bool EventsSetOnMentions { get; set; }
        private bool EventsSetOnFavourites { get; set; }

        public SortedObservableCollection<TimelineViewModel> Timeline { get; set; }
        public SortedObservableCollection<TimelineViewModel> Favourites { get; set; }
        public SortedObservableCollection<SearchResultViewModel> Mentions { get; set; }

        private bool GettingMoreTimeline { get; set; }
        private bool GettingMoreFavourites { get; set; }

        private WebBrowser UserProfileBrowser { get; set; }

        #endregion

#if DEBUG
        ~UserProfile()
        {
            Debug.WriteLine("******************************************************** UserProfile GC");
        }
#endif

        #region Overrides

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardOut && toOrFrom.ToString().Contains("ListMembership"))
                return new SlideDownAnimator { RootElement = LayoutRoot };
            else if (animationType == AnimationType.NavigateBackwardIn && toOrFrom.ToString().Contains("ListMembership"))
                return new SlideUpAnimator { RootElement = LayoutRoot };

            return base.GetAnimation(animationType, toOrFrom);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            if (SelectAccountPopup != null)
            {
                UiHelper.SafeDispatch(HideSelectAccountPopup);
                e.Cancel = true;
                return;
            }

            if (Timeline != null)
                Timeline.Clear();
            Timeline = null;

            if (Favourites != null)
                Favourites.Clear();
            Favourites = null;

            if (Mentions != null)
                Mentions.Clear();
            Mentions = null;

            if (AccountIds != null)
                AccountIds.Clear();
            AccountIds = null;

            if (Profile != null)
            {
                Profile.ProfileImageUrl = null;
                Profile.BannerUrl = null;
            }
            Profile = null;

            UserProfileBrowser = null;

            RemoveAllListEvents();

            base.OnBackKeyPress(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            GC.Collect();
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!UiHelper.ValidateUser())
            {
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                return;
            }

            if (DataLoaded)
                return;

            AccountId = UiHelper.GetAccountId(NavigationContext);

            string screenName;
            if (!NavigationContext.QueryString.TryGetValue("screen", out screenName))
                NavigationService.GoBack();

            ScreenName = screenName;

            ThreadPool.QueueUserWorkItem(StartLoad);
        }

        #endregion

        #region Members

        private void UserProfile_Loaded(object sender, RoutedEventArgs e)
        {
            CheckListViewEventsAreSet();
        }

        private void RemoveAllListEvents()
        {

            if (EventsSetOnTimeline)
            {
                var timelineSv = (ScrollViewer)FindElementRecursive(lstTimeline, typeof(ScrollViewer));
                if (timelineSv != null)
                {
                    // Visual States are always on the first child of the control template 
                    var element = VisualTreeHelper.GetChild(timelineSv, 0) as FrameworkElement;
                    if (element != null)
                    {
                        var vgroup = FindVisualState(element, "VerticalCompression");
                        if (vgroup != null)
                        {
                            vgroup.CurrentStateChanging -= lstTimelineVertical_CurrentStateChanging;
                        }
                    }
                }
            }

            if (EventsSetOnMentions)
            {
                var timelineSv = (ScrollViewer)FindElementRecursive(lstMentions, typeof(ScrollViewer));
                if (timelineSv != null)
                {
                    // Visual States are always on the first child of the control template 
                    var element = VisualTreeHelper.GetChild(timelineSv, 0) as FrameworkElement;
                    if (element != null)
                    {
                        var vgroup = FindVisualState(element, "VerticalCompression");
                        if (vgroup != null)
                        {
                            vgroup.CurrentStateChanging -= lstMentionsVertical_CurrentStateChanging;
                        }
                    }
                }
            }

            if (EventsSetOnFavourites)
            {
                var timelineSv = (ScrollViewer)FindElementRecursive(lstFavourites, typeof(ScrollViewer));
                if (timelineSv != null)
                {
                    // Visual States are always on the first child of the control template 
                    var element = VisualTreeHelper.GetChild(timelineSv, 0) as FrameworkElement;
                    if (element != null)
                    {
                        var vgroup = FindVisualState(element, "VerticalCompression");
                        if (vgroup != null)
                        {
                            vgroup.CurrentStateChanging -= lstFavouritesVertical_CurrentStateChanging;
                        }
                    }
                }
            }


        }

        private void CheckListViewEventsAreSet()
        {
            if (pivotMain.SelectedIndex == 1)
            {
                if (EventsSetOnTimeline)
                    return;

                var timelineSv = (ScrollViewer)FindElementRecursive(lstTimeline, typeof(ScrollViewer));
                if (timelineSv != null)
                {
                    // Visual States are always on the first child of the control template 
                    var element = VisualTreeHelper.GetChild(timelineSv, 0) as FrameworkElement;
                    if (element != null)
                    {
                        var vgroup = FindVisualState(element, "VerticalCompression");
                        if (vgroup != null)
                        {
                            vgroup.CurrentStateChanging += lstTimelineVertical_CurrentStateChanging;
                        }
                    }
                    EventsSetOnTimeline = true;
                }
            }
            else if (pivotMain.SelectedIndex == 2) // mentions
            {
                if (EventsSetOnMentions)
                    return;

                var timelineSv = (ScrollViewer)FindElementRecursive(lstMentions, typeof(ScrollViewer));
                if (timelineSv != null)
                {
                    // Visual States are always on the first child of the control template 
                    var element = VisualTreeHelper.GetChild(timelineSv, 0) as FrameworkElement;
                    if (element != null)
                    {
                        var vgroup = FindVisualState(element, "VerticalCompression");
                        if (vgroup != null)
                        {
                            vgroup.CurrentStateChanging += lstMentionsVertical_CurrentStateChanging;
                        }
                    }
                    EventsSetOnMentions = true;
                }
            }
            else if (pivotMain.SelectedIndex == 3) // favourites
            {
                if (EventsSetOnFavourites)
                    return;

                var timelineSv = (ScrollViewer)FindElementRecursive(lstFavourites, typeof(ScrollViewer));
                if (timelineSv != null)
                {
                    // Visual States are always on the first child of the control template 
                    var element = VisualTreeHelper.GetChild(timelineSv, 0) as FrameworkElement;
                    if (element != null)
                    {
                        var vgroup = FindVisualState(element, "VerticalCompression");
                        if (vgroup != null)
                        {
                            vgroup.CurrentStateChanging += lstFavouritesVertical_CurrentStateChanging;
                        }
                    }
                    EventsSetOnFavourites = true;
                }
            }

        }

        private void lstFavouritesVertical_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState.Name == "CompressionBottom")
            {
                UiHelper.ShowProgressBar(ApplicationResources.fetchingmorefavourites);
                ThreadPool.QueueUserWorkItem(StartGetMoreFavourites);
            }
        }

        private void lstTimelineVertical_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState.Name == "CompressionBottom")
            {
                UiHelper.ShowProgressBar("fetching more tweets");
                ThreadPool.QueueUserWorkItem(StartGetMoreTimeline);
            }
        }

        private void lstMentionsVertical_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState.Name == "CompressionBottom")
            {
                UiHelper.ShowProgressBar("fetching more mentions");
                ThreadPool.QueueUserWorkItem(GetMoreMentions);
            }
        }

        private void GetMoreMentions(object state)
        {
            StartGetMentions(state);
        }

        private async void StartGetMoreFavourites(object state)
        {
            if (GettingMoreFavourites)
                return;

            if (Favourites == null)
            {
                UiHelper.HideProgressBar();
                return;
            }

            GettingMoreTimeline = true;

            long oldestItem = Favourites.Min(x => x.Id);

            var api = new TwitterApi(AccountId);
            var results = await api.GetFavourites(0, oldestItem, ScreenName);
            ApiOnGetFavouritesCompletedEvent(AccountId, results, api.HasError, api.ErrorMessage);
        }


        private async void StartGetMoreTimeline(object state)
        {
            if (GettingMoreTimeline)
                return;

            if (Timeline == null || !Timeline.Any())
                return;

            GettingMoreTimeline = true;

            long oldestItem = Timeline.Min(x => x.Id);

            var api = new TwitterApi(AccountId);
            var result = await api.GetPublicTimeline(ScreenName, oldestItem);
            api_GetPublicTimelineCompletedEvent(AccountId, result, api.HasError, api.ErrorMessage);
        }

        private void StartLoad(object state)
        {
            UiHelper.ShowProgressBar("fetching user profile");

            Timeline = new SortedObservableCollection<TimelineViewModel>();
            Favourites = new SortedObservableCollection<TimelineViewModel>();
            Mentions = new SortedObservableCollection<SearchResultViewModel>();

            UiHelper.SafeDispatchSync(() =>
                                      {
                                          lstTimeline.DataContext = Timeline;
                                          lstMentions.DataContext = Mentions;
                                          lstFavourites.DataContext = Favourites;
                                          gridUser.Visibility = Visibility.Collapsed;
                                      });

            ThreadPool.QueueUserWorkItem(StartGetUserProfile);

            var currentAccountScreenName = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(AccountId);

            // Disable any buttons?
            if (string.Compare(ScreenName.Replace("@", ""), currentAccountScreenName.Replace("@", ""),
                               StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                UiHelper.SafeDispatch(() =>
                                          {
                                              ApplicationBar.IsVisible = false;
                                              buttonChangePhoto.Visibility = Visibility.Visible;
                                              UpdateFriendshipStatus("this is your profile!");
                                          });
            }
            else
            {
                UiHelper.SafeDispatch(() => buttonChangePhoto.Visibility = Visibility.Collapsed);

                // Someone else, so lets see if we're friends
                ThreadPool.QueueUserWorkItem(StartGetFriendship);
            }
        }

        private async void StartGetMentions(object state)
        {
            UiHelper.ShowProgressBar("fetching mentions");

            var api = new TwitterApi(AccountId);

            long maxId = 0;

            if (Mentions == null)
                return;

            if (Mentions.Any())
                maxId = Mentions.Min(x => x.Id);

            var result = await api.Search("%40" + ScreenName + "%20-RT", maxId, 0, false);
            api_SearchCompletedEvent(AccountId, result, api.HasError, api.ErrorMessage);

        }

        private void api_SearchCompletedEvent(long accountId, ResponseSearch searchResult, bool hasError, string errorMessage)
        {


            try
            {

                if (searchResult == null)
                    return;

                if (hasError)
                {
                    UiHelper.ShowToast(!string.IsNullOrEmpty(errorMessage)
                        ? errorMessage
                        : "There was a problem connecting to Twitter.");
                    return;
                }

                UiHelper.SafeDispatchSync(() =>
                {
                    foreach (var o in searchResult.statuses)
                    {
                        var result = new SearchResultViewModel
                        {
                            Id = o.id,
                            Description = HttpUtility.HtmlDecode(o.text),
                            ImageUrl = o.user.profile_image_url,
                            CreatedAt = o.created_at,
                            UserId = o.user.id,
                            Client = o.source,
                            ScreenName = o.user.screen_name,
                            DisplayName = o.user.name,
                            AccountId = AccountId,
                            IsReply = o.in_reply_to_status_id.HasValue
                        };

                        if (Mentions != null)
                            Mentions.Add(result);
                    }
                });

            }
            catch (Exception)
            {
            }
            finally
            {
                UiHelper.HideProgressBar();
            }
        }

        private static object StartGetFriendshipLock = new object();

        private async void StartGetFriendship(object state)
        {

            if (AccountIds == null)
                return;

            lock (StartGetFriendshipLock)
            {
                using (var dh = new MainDataContext())
                {

                    if (!dh.Profiles.Any(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                        return;

                    AccountIds.AddRange(dh.Profiles.Where(
                        x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter).Select(
                            account =>
                                new AccountFriendViewModel()
                                {
                                    Id = account.Id,
                                    ScreenName = account.ScreenName,
                                    IsFriend = false,
                                    StatusChecked = false,
                                    IsFollowingYou = false
                                }
                        ));

                }
            }

            foreach (var account in AccountIds)
            {
                var api = new TwitterApi(account.Id);
                var result = await api.GetFriendship(account.ScreenName, ScreenName);
                api_GetFriendshipCompletedEvent(account.Id, result);
            }

        }


        private void api_GetFriendshipCompletedEvent(long accountId, ResponseGetFriendship friendship)
        {
            if (friendship == null || friendship.relationship == null ||
                friendship.relationship.target == null || friendship.relationship.source == null)
                return;

            if (AccountIds == null) // we exited
            {
                return;
            }

            var item = AccountIds.FirstOrDefault(x => x.Id == accountId);
            if (item != null)
                item.StatusChecked = true;

            // This is to detect if you're following them
            if (friendship.relationship.source.following)
            {
                if (item != null)
                    item.IsFriend = true;
            }

            UiHelper.SafeDispatch(() =>
                                      {
                                          // send DM button
                                          var b = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
                                          b.IsEnabled = true;
                                      });

            // this is to detect if theyre following you
            if (friendship.relationship.target.following)
            {
                CanSendDm = true;
                if (item != null)
                    item.IsFollowingYou = true;
            }

            // Only if they've all been checked
            if (AccountIds.Count == AccountIds.Count(x => x.StatusChecked))
                UiHelper.SafeDispatch(UpdateFollowingStatus);
        }

        private void UpdateFollowingStatus()
        {
            var sb = new Storyboard();

            var settingsFade = new DoubleAnimation
                                   {
                                       From = 1,
                                       To = 0,
                                       Duration = new Duration(TimeSpan.FromSeconds(0.5))
                                   };
            Storyboard.SetTarget(settingsFade, txtFriendshipStatus);
            Storyboard.SetTargetProperty(settingsFade, new PropertyPath(OpacityProperty));

            sb.Children.Add(settingsFade);

            sb.Completed += sb_Completed;
            sb.Begin();
        }

        private void UpdateFriendshipStatus(string text)
        {
            txtFriendshipStatus.Text = text;

            var sb = new Storyboard();

            var settingsFade = new DoubleAnimation
                                   {
                                       From = 0,
                                       To = 1,
                                       Duration = new Duration(TimeSpan.FromSeconds(0.5))
                                   };
            Storyboard.SetTarget(settingsFade, txtFriendshipStatus);
            Storyboard.SetTargetProperty(settingsFade, new PropertyPath(OpacityProperty));
            sb.Children.Add(settingsFade);
            sb.Begin();
        }

        private void sb_Completed(object sender, EventArgs e)
        {
            string statusText;

            try
            {
                if (CanSendDm)
                {
                    // all or some of the accounts?
                    if (AccountIds.Count == AccountIds.Count(x => x.IsFollowingYou))
                        statusText = ScreenName + " is following you";
                    else
                    {
                        var accountNames = AccountIds.Where(x => x.IsFollowingYou).Select(x => x.ScreenName);
                        var accounts = string.Join(",", accountNames);
                        statusText = ScreenName + " is following " + accounts;
                    }
                }
                else
                {
                    statusText = ScreenName + " is not following you";
                }
            }
            catch (Exception)
            {
                statusText = "not sure. sorry! something went wrong!";
            }

            UpdateFriendshipStatus(statusText);
        }

        private async void StartGetPublicTimeline(object state)
        {
            UiHelper.ShowProgressBar("fetching timeline");

            var api = new TwitterApi(AccountId);
            var result = await api.GetPublicTimeline(ScreenName, 0);
            api_GetPublicTimelineCompletedEvent(AccountId, result, api.HasError, api.ErrorMessage);
        }

        private async void StartGetUserProfile(object state)
        {
            var api = new TwitterApi(AccountId);
            var result = await api.GetUserProfile(ScreenName, 0);
            api_GetUserProfileCompletedEvent(AccountId, result);
        }

        private void api_GetPublicTimelineCompletedEvent(long accountId, List<ResponseTweet> publicTimeline, bool hasError, string errorMessage)
        {

            try
            {
                if (hasError)
                {
                    UiHelper.ShowToast(!string.IsNullOrEmpty(errorMessage)
                        ? errorMessage
                        : "There was a problem connecting to Twitter.");
                }
                else
                {
                    UiHelper.SafeDispatchSync(() => TimelineResponseToView(AccountId, publicTimeline));
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                GettingMoreTimeline = false;
                UiHelper.HideProgressBar();
            }
        }


        private void TimelineResponseToView(long accountId, IEnumerable<ResponseTweet> statuses)
        {

            if (statuses == null)
                return;

            var newStatuses = new List<TimelineViewModel>();

            if (Timeline == null)
                return;

            foreach (var status in statuses)
            {
                if (!Timeline.Any(x => x.Id == status.id))
                {
                    var item = status.AsViewModel(accountId);
                    newStatuses.Add(item);
                }
            }

            foreach (var item in newStatuses)
            {
                if (Timeline != null)
                    Timeline.Add(item);
            }
        }

        // Handle selection changed on ListBox
        private void lstTimeline_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (lstTimeline.SelectedIndex == -1)
                return;

            var item = lstTimeline.SelectedValue as TimelineViewModel;
            string query = string.Format("accountId={0}&id={1}", item.AccountId, item.Id);

            // Navigate to the new page
            UiHelper.SafeDispatch(() => NavigationService.Navigate(new Uri("/DetailsPage.xaml?" + query, UriKind.Relative)));

            // Reset selected index to -1 (no selection)
            lstTimeline.SelectedIndex = -1;
        }

        private void api_GetUserProfileCompletedEvent(long accountId, ResponseGetUserProfile userProfile)
        {

            if (userProfile == null || userProfile.id == 0)
            {
                UiHelper.HideProgressBar();
                ThreadPool.QueueUserWorkItem(tempy);
                return;
            }

            DataLoaded = true;

            Dispatcher.BeginInvoke(delegate()
                                       {
                                           try
                                           {
                                               Profile = userProfile.AsViewModel();

                                               var isFollowingUser = userProfile.following ?? false;
                                               var isCurrentUser = userProfile.id == AccountId;
                                               var getProfile = !(Profile.IsProtected && !isFollowingUser) || isCurrentUser;

                                               if (string.IsNullOrEmpty(Profile.BannerUrl))
                                               {
                                                   gridBanner.Visibility = Visibility.Collapsed;
                                                   gridNoBanner.Visibility = Visibility.Visible;

                                                   SupportedOrientations = ((IMehdohApp)Application.Current).SupportedOrientations;
                                               }
                                               else
                                               {
                                                   gridBanner.Visibility = Visibility.Visible;
                                                   gridNoBanner.Visibility = Visibility.Collapsed;

                                                   SupportedOrientations = SupportedPageOrientation.Portrait;
                                               }

                                               if (getProfile)
                                               {
                                                   txtProtectedTimeline.Visibility = Visibility.Collapsed;
                                                   lstTimeline.Visibility = Visibility.Visible;
                                               }
                                               else
                                               {
                                                   txtProtectedTimeline.Visibility = Visibility.Visible;
                                                   lstTimeline.Visibility = Visibility.Collapsed;
                                               }

                                               this.DataContext = Profile;
                                               FadeOutTweets.Begin();

                                               SetBio(Profile.Bio);

                                               if (!string.IsNullOrEmpty(Profile.Url))
                                               {
                                                   // get a resolved URL
                                                   var url = Profile.Url;

                                                   if (Profile.Assets != null && Profile.Assets.Count > 0)
                                                   {
                                                       if (Profile.Assets.Any(x => x.Type == AssetTypeEnum.Url))
                                                       {
                                                           var firstMatch = Profile.Assets.FirstOrDefault(x => x.Type == AssetTypeEnum.Url && x.ShortValue == url);
                                                           if (firstMatch != null)
                                                           {
                                                               url = firstMatch.LongValue;
                                                           }
                                                       }
                                                   }

                                                   AddSitePanel(url);
                                               }

                                               var sb = new Storyboard();

                                               var settingsFade = new DoubleAnimation
                                                                      {
                                                                          From = 0,
                                                                          To = 1,
                                                                          Duration = new Duration(TimeSpan.FromSeconds(0.7)),
                                                                          RepeatBehavior = new RepeatBehavior(1)
                                                                      };
                                               Storyboard.SetTarget(settingsFade, gridUser);
                                               Storyboard.SetTargetProperty(settingsFade, new PropertyPath(OpacityProperty));

                                               sb.Children.Add(settingsFade);

                                               gridUser.Opacity = 0;
                                               gridUser.Visibility = Visibility.Visible;

                                               sb.Begin();

                                               var b = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
                                               b.IsEnabled = true;

                                               // Only one? Then we can show the follow status here, otherwise we always do the following from the subsequent screen
                                               if (AccountIds.Count == 1)
                                               {
                                                   if (!Profile.Following)
                                                   {
                                                       b.IconUri = new Uri("/Images/76x76/dark/appbar.add.png", UriKind.Relative);
                                                       b.Text = "follow";
                                                   }
                                               }
                                               else
                                               {
                                                   b.IconUri = new Uri("/Images/76x76/light/appbar.math.plus.minus.png", UriKind.Relative);
                                                   b.Text = "follow";
                                               }
                                           }
                                           catch
                                           {
                                               // don#t care
                                           }
                                           finally
                                           {
                                               UiHelper.HideProgressBar();
                                           }

                                       });
        }

        private void tempy(object state)
        {
            UiHelper.SafeDispatch(() =>
                                      {
                                          base.AbortAnimations();
                                          ThreadPool.QueueUserWorkItem(o => UiHelper.ShowToast("unable to find anyone with that username."));

                                          if (NavigationService.CanGoBack)
                                              NavigationService.GoBack();
                                      });
        }

        private void SetBio(string bio)
        {
            textBio.Blocks.Clear();

            if (string.IsNullOrWhiteSpace(bio))
                return;

            if (!bio.Contains("@"))
            {
                var singleParagraph = new Paragraph();
                singleParagraph.Inlines.Add(HttpUtility.HtmlDecode(bio));
                textBio.Blocks.Add(singleParagraph);
                return;
            }

            string currentString = string.Empty;
            bool isInMention = false;

            var myParagraph = new Paragraph();

            for (int i = 0; i < bio.Length; i++)
            {
                var currentChar = bio.Substring(i, 1);

                if (currentChar == "@")
                {
                    isInMention = true;

                    if (!string.IsNullOrWhiteSpace(currentString))
                    {
                        myParagraph.Inlines.Add(HttpUtility.HtmlDecode(currentString));
                    }

                    currentString = string.Empty;
                }
                else if (!IsTwitterNameCharacter(currentChar) && isInMention)
                {
                    isInMention = false;
                    // add it

                    if (currentString.Length > 1)
                    {
                        var button2 = new Hyperlink
                                          {
                                              FontWeight = FontWeights.Bold,
                                              CommandParameter = currentString,
                                              TextDecorations = null,
                                              MouseOverTextDecorations = null,
                                              Foreground = Resources["PhoneAccentBrush"] as Brush
                                          };

                        button2.Click += delegate(object sender, RoutedEventArgs e)
                                             {
                                                 var link = sender as Hyperlink;
                                                 var screenName = link.CommandParameter;
                                                 textBio.Focus();
                                                 UiHelper.SafeDispatch(() =>
                                                                       NavigationService.Navigate(
                                                                           new Uri(
                                                                               "/UserProfile.xaml?accountId=" +
                                                                               AccountId + "&screen=" + screenName,
                                                                               UriKind.Relative))
                                                     );
                                             };
                        button2.Inlines.Add(currentString);
                        myParagraph.Inlines.Add(button2);
                    }
                    else
                    {
                        myParagraph.Inlines.Add(HttpUtility.HtmlDecode(currentString));
                    }

                    currentString = string.Empty;
                }

                currentString += currentChar;
            }

            if (!string.IsNullOrWhiteSpace(currentString))
            {
                if (isInMention)
                {
                    var button2 = new Hyperlink
                                      {
                                          FontWeight = FontWeights.Bold,
                                          CommandParameter = currentString,
                                          TextDecorations = null,
                                          MouseOverTextDecorations = null,
                                          Foreground = Resources["PhoneAccentBrush"] as Brush
                                      };

                    button2.Click += delegate(object sender, RoutedEventArgs e)
                                         {
                                             var link = sender as Hyperlink;
                                             var screenName = link.CommandParameter;
                                             textBio.Focus();
                                             UiHelper.SafeDispatch(() => NavigationService.Navigate(
                                                 new Uri(
                                                     "/UserProfile.xaml?accountId=" + AccountId + "&screen=" +
                                                     screenName, UriKind.Relative)));
                                         };
                    button2.Inlines.Add(currentString);
                    myParagraph.Inlines.Add(button2);
                }
                else
                {
                    myParagraph.Inlines.Add(HttpUtility.HtmlDecode(currentString));
                }
            }

            textBio.Blocks.Add(myParagraph);
        }

        private bool IsTwitterNameCharacter(string currentChar)
        {
            if (!char.IsLetterOrDigit(currentChar.ToCharArray()[0]) && currentChar != "_" && currentChar != "-")
                return false;

            return true;
        }

        private object GetTrialPanel(string contentType)
        {
            var stackPanel = new StackPanel();

            var textBlock = new TextBlock
                                {
                                    Text =
                                        "In the full version of Mehdoh the " + contentType + " would display here.\n\n" +
                                        "Would you like to upgrade to the full version now?",
                                    TextWrapping = TextWrapping.Wrap,
                                    FontSize = 24
                                };

            stackPanel.Children.Add(textBlock);

            var button = new Button
                             {
                                 Content = "Yes please!",
                                 Margin = new Thickness(50)
                             };

            button.Click += marketplaceButton_Click;

            stackPanel.Children.Add(button);

            return stackPanel;
        }

        private void marketplaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show("This will now open the Marketplace on the Mehdoh page. Do you want to continue?",
                                "Open Marketplace", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                return;

            try
            {
                var marketplaceTask = new MarketplaceDetailTask();
                marketplaceTask.Show();
            }
            catch (Exception)
            {
            }
        }

        private void AddSitePanel(string url)
        {
            try
            {

                if (url.ToLower().Contains("windowsphone.com/s?appid="))
                {
                    GetMarketplaceLink(url);
                }
                else if (url.ToLower().Contains("windowsphone.com/") && url.ToLower().Contains("/store/app/"))
                {
                    GetMarketplaceLink(url);
                }
                else
                {
                    CreateWebSitePanel(url);
                }
            }
            catch (Exception)
            {

            }
        }

        private void GetMarketplaceLink(string newUrl)
        {

            var pivotItem = new PivotItem
            {
                Header = "store"
            };

            var stackPanel = new StackPanel();

            var pb = new ProgressBar()
            {
                IsIndeterminate = true
            };

            stackPanel.Children.Add(pb);

            var appTitleTextBlock = new TextBlock
            {
                FontSize = 32,
                Margin = new Thickness(15, 0, 0, 15),
                TextWrapping = TextWrapping.NoWrap
            };

            stackPanel.Children.Add(appTitleTextBlock);

            var image = new Image
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxHeight = 200,
                MaxWidth = 200,
                Margin = new Thickness(15, 0, 0, 0)
            };

            stackPanel.Children.Add(image);

            var button = new Button
            {
                Content = "view in store",
                Width = 220,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(5, 20, 0, 0)
            };

            button.Click += delegate
            {
                var task = new WebBrowserTask
                {
                    Uri = new Uri(newUrl, UriKind.Absolute)
                };
                task.Show();
            };

            stackPanel.Children.Add(button);

            pivotItem.Content = stackPanel;

            //<meta property="og:title" content="Mehdoh" />
            //<meta property="og:image" content="http://cdn.marketplaceimages.windowsphone.com/v8/images/f686869e-c191-44c2-8f48-8ce280a1a634?imageType=ws_icon_large" />

            var client = new WebClient();
            client.OpenReadCompleted += delegate(object sender, OpenReadCompletedEventArgs e)
            {
                var resInfo = new StreamResourceInfo(e.Result, null);
                using (var reader = new StreamReader(resInfo.Stream))
                {
                    using (var bReader = new BinaryReader(reader.BaseStream))
                    {
                        var contents = bReader.ReadBytes((int)reader.BaseStream.Length);
                        var fileContents = System.Text.Encoding.UTF8.GetString(contents, 0, contents.Length);

                        var tags = e.UserState as Tuple<UIElement, UIElement, UIElement>;

                        var appTitleMatch = Regex.Match(fileContents, "meta property=\"og:title\" content=\"(?<AppTitle>.*)\"");
                        if (appTitleMatch.Success)
                        {
                            var appTitle = appTitleMatch.Groups["AppTitle"].Value;
                            ((TextBlock)tags.Item1).Text = HttpUtility.HtmlDecode(appTitle);
                        }

                        var appImageMatch = Regex.Match(fileContents, "meta property=\"og:image\" content=\"(?<AppImage>.*)\"");
                        if (appImageMatch.Success)
                        {
                            var appImage = new Uri(appImageMatch.Groups["AppImage"].Value, UriKind.Absolute);
                            var bitmapImage = new BitmapImage(appImage);
                            ((Image)tags.Item2).Source = bitmapImage;
                        }

                        ((ProgressBar)tags.Item3).IsIndeterminate = false;
                        tags.Item3.Visibility = Visibility.Collapsed;
                    }
                }
            };

            var marketplaceUrl = new Uri(newUrl, UriKind.Absolute);
            client.OpenReadAsync(marketplaceUrl, new Tuple<UIElement, UIElement, UIElement>(appTitleTextBlock, image, pb));

            pivotMain.Items.Insert(1, pivotItem);

        }


        private void CreateWebSitePanel(string url)
        {

            var button = new ApplicationBarMenuItem
            {
                Text = "View home page in IE"
            };

            button.Click += delegate
            {
                var task = new WebBrowserTask();
                task.Uri = new Uri(url, UriKind.Absolute);
                task.Show();
            };

            ApplicationBar.MenuItems.Add(button);

        }

        private void browser_Navigated(object sender, NavigationEventArgs e)
        {
            var img = sender as WebBrowser;
            if (img != null)
            {
                var sp = img.Parent as StackPanel;
                var pg = sp.Children.OfType<PerformanceProgressBar>();
                foreach (var p in pg)
                {
                    p.IsEnabled = false;
                    p.IsIndeterminate = false;
                }
            }
        }

        private void mnuMention_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(
                                           "/NewTweet.xaml?accountId=" + AccountId + "&replyToAuthor=" + ScreenName,
                                           UriKind.Relative));
        }

        private void mnuMessage_Click(object sender, EventArgs e)
        {
            if (!CanSendDm)
            {
                if (AccountIds.Any() && AccountIds.Count == 1)
                    MessageBox.Show(
                        "Twitter does not allow you to send a message to this user as they do not follow you.", "Sorry",
                        MessageBoxButton.OK);
                else
                    MessageBox.Show(
                        "Twitter does not allow you to send a message to this user as they do not follow any of your accounts.",
                        "Sorry", MessageBoxButton.OK);
            }
            else
            {
                if (!AccountIds.Any())
                {
                    NavigationService.Navigate(
                        new Uri("/NewTweet.xaml?accountId=" + AccountId + "&dm=true&replyToAuthor=" + ScreenName,
                                UriKind.Relative));
                }
                else
                {
                    long properAccountId;

                    if (AccountIds.Any(x => x.Id == AccountId))
                    {
                        properAccountId = AccountId;
                    }
                    else
                    {
                        var accountFriendViewModel = AccountIds.FirstOrDefault(x => x.IsFollowingYou);
                        if (accountFriendViewModel != null)
                        {
                            properAccountId = accountFriendViewModel.Id;
                        }
                        else
                        {
                            properAccountId = AccountId;
                        }
                    }

                    var allowedAccounts = String.Join("|", AccountIds.Where(x => x.IsFollowingYou).Select(x => x.Id));
                    NavigationService.Navigate(
                        new Uri(
                            "/NewTweet.xaml?accountId=" + properAccountId + "&dm=true&replyToAuthor=" + ScreenName +
                            "&allowedAccountIds=" + allowedAccounts, UriKind.Relative));
                }
            }
        }

        private void ShowAccountSelect()
        {

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

        private async void post_CheckPressed(object sender, EventArgs e)
        {
            var post = sender as SelectTwitterAccount;

            // any selected items?
            foreach (var account in post.Items)
            {
                var existingAccountId = AccountIds.FirstOrDefault(x => x.Id == account.Id);
                if (existingAccountId != null)
                {
                    if (existingAccountId.IsFriend && !account.IsSelected)
                    {
                        // this is an unfollow                       
                        UiHelper.ShowProgressBar("unfollowing");
                        var api = new TwitterApi(account.Id);

                        await api.UnfollowUser(ScreenName);
                        api_UnfollowUserCompletedEvent();

                        existingAccountId.IsFriend = false;
                    }
                    else if (!existingAccountId.IsFriend && account.IsSelected)
                    {
                        // this is a follow
                        UiHelper.ShowProgressBar("following");
                        var api = new TwitterApi(account.Id);

                        await api.FollowUser(ScreenName);
                        api_FollowUserCompletedEvent();

                        existingAccountId.IsFriend = true;
                    }
                    // anything else is unchanged
                }
            }

            HideSelectAccountPopup();
        }

        private void HideSelectAccountPopup()
        {
            UiHelper.SafeDispatch(() =>
                                      {
                                          if (SelectAccountPopup != null)
                                          {
                                              EventHandler selectAccountPopupClosed = (sender, args) =>
                                                                                          {
                                                                                              SelectAccountPopup = null;
                                                                                              ApplicationBar.IsVisible = true;
                                                                                          };
                                              SelectAccountPopup.Closed += selectAccountPopupClosed;
                                              SelectAccountPopup.Hide();
                                          }
                                      });
        }

        private async void mnuUnfollow_Click(object sender, EventArgs e)
        {

            if (AccountIds == null)
                return;

            // One account uses the button
            // Multiple accounts uses the screen
            if (AccountIds.Count == 1)
            {

                try
                {
                    var button = (ApplicationBarIconButton)ApplicationBar.Buttons[0];

                    if (button.Text == "unfollow")
                    {
                        UiHelper.ShowProgressBar("unfollowing");
                        var api = new TwitterApi(AccountId);
                        await api.UnfollowUser(ScreenName);
                        api_UnfollowUserCompletedEvent();
                    }
                    else if (button.Text == "follow")
                    {
                        UiHelper.ShowProgressBar("following");
                        var api = new TwitterApi(AccountId);
                        await api.FollowUser(ScreenName);
                        api_FollowUserCompletedEvent();
                    }

                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException("mnuUnfollow_Click", ex);
                }

            }
            else
            {
                ShowAccountSelect();
            }

        }

        private void api_FollowUserCompletedEvent()
        {
            Dispatcher.BeginInvoke(() =>
                                       {
                                           UiHelper.HideProgressBar();

                                           try
                                           {
                                               if (AccountIds != null && AccountIds.Count == 1)
                                               {
                                                   // Change to unfollow
                                                   var b = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
                                                   if (b == null)
                                                       return;

                                                   b.IconUri = new Uri("/Images/76x76/dark/appbar.minus.png", UriKind.Relative);
                                                   b.Text = "unfollow";
                                               }
                                           }
                                           catch
                                           {
                                           }

                                       });
        }

        private void api_UnfollowUserCompletedEvent()
        {
            // Change to follow
            Dispatcher.BeginInvoke(() =>
                                       {
                                           UiHelper.HideProgressBar();

                                           try
                                           {
                                               if (AccountIds != null && AccountIds.Count == 1)
                                               {
                                                   var b = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
                                                   b.IconUri = new Uri("/Images/76x76/dark/appbar.add.png", UriKind.Relative);
                                                   b.Text = "follow";
                                               }
                                           }
                                           catch
                                           {
                                           }

                                       });
        }

        private async void mnuBlock_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "Are you sure you wish to block @" + ScreenName.Replace("@", "") +
                    "? They will no longer be able to follow you, and you will not see their tweets.", "block user",
                    MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                SystemTray.ProgressIndicator = new ProgressIndicator
                                                   {
                                                       IsVisible = true,
                                                       IsIndeterminate = true,
                                                       Text = "blocking user"
                                                   };

                var api = new TwitterApi(AccountId);
                await api.BlockUser(ScreenName);
                api_BlockUserCompletedEvent(ScreenName);
            }

        }

        private void api_BlockUserCompletedEvent(string blockedUserName)
        {
            UiHelper.SafeDispatch(() =>
                                      {
                                          UiHelper.HideProgressBar();
                                          MessageBox.Show("Successfully blocked @" + blockedUserName, "block user",
                                                          MessageBoxButton.OK);
                                      });
        }

        private void mnuSpam_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to report this user as spam? This will also block the user.",
                                "report spam", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                SystemTray.ProgressIndicator = new ProgressIndicator
                                                   {
                                                       IsVisible = true,
                                                       IsIndeterminate = true,
                                                       Text = "reporting spam"
                                                   };
                var api = new TwitterApi(AccountId);
                api.ReportSpamCompletedEvent += api_ReportSpamCompletedEvent;
                api.ReportSpam(ScreenName);
            }
        }

        private async void api_ReportSpamCompletedEvent(object sender, EventArgs e)
        {
            var api = new TwitterApi(AccountId);
            await api.BlockUser(ScreenName);
            api_BlockUserForSpamCompletedEvent(ScreenName);
        }

        private void api_BlockUserForSpamCompletedEvent(string screenName)
        {
            UiHelper.SafeDispatch(() =>
                                      {
                                          UiHelper.HideProgressBar();
                                          MessageBox.Show("Successfully reported as spam and blocked @" + screenName, "report spam", MessageBoxButton.OK);
                                      });
        }


        private void mnuPin_Click(object sender, EventArgs e)
        {
            var tileCount = ShellTile.ActiveTiles.Count() - 1;

            if (tileCount >= 2 && LicenceInfo.IsTrial())
            {
                if (
                    MessageBox.Show(
                        "You may only pin 2 users to the start menu in the trial version of mehdoh.\n\nWould you like to visit the Marketplace and purchase the full version?",
                        "Trial version", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    var marketplaceTask = new MarketplaceDetailTask();
                    marketplaceTask.Show();
                }
                return;
            }

            PinTile();
        }

        private void PinTile()
        {
            UiHelper.ShowProgressBar("pinning user to start");

            var client = new WebClient();
            client.OpenReadCompleted += imagedownloadCompleted;
            var remoteUri = new Uri(Profile.OriginalProfileImageUrl, UriKind.Absolute);
            client.OpenReadAsync(remoteUri);
        }

        private void imagedownloadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            var resInfo = new StreamResourceInfo(e.Result, null);

            byte[] contents;

            using (var reader = new StreamReader(resInfo.Stream))
            {
                using (var bReader = new BinaryReader(reader.BaseStream))
                {
                    contents = bReader.ReadBytes((int)reader.BaseStream.Length);
                }
            }

            try
            {
                var targetFile = ApplicationConstants.ShellContentFolder + "/pinned_profile_" + Profile.Id + ".png";

                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var myStream = new IsolatedStorageFileStream(targetFile, FileMode.Create, myStore))
                    {
                        myStream.Write(contents, 0, contents.Length);
                    }
                }

                var tile = new CloudohTile();
                tile.SetValues(ScreenName, targetFile);
                tile.UpdateLayout();

                var tileSmall = new CloudohTileSmall();
                tileSmall.SetValues(ScreenName, targetFile);
                tileSmall.UpdateLayout();

                var newTile = new RadFlipTileData()
                {
                    SmallVisualElement = tileSmall,
                    VisualElement = tile,
                    Title = "",
                    MeasureMode = MeasureMode.Element,
                    IsTransparencySupported = false
                };

                LiveTileHelper.CreateOrUpdateTile(newTile, new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + ScreenName, UriKind.Relative), false);
            }
            catch (Exception)
            {
                UiHelper.ShowToast("Unable to pin user. Sorry :(");
            }
            finally
            {
                UiHelper.HideProgressBar();
            }

        }

        private void buttonChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            var photoChooserTask = new PhotoChooserTask
                                       {
                                           ShowCamera = true
                                       };

            photoChooserTask.Completed += photoCaptureOrSelectionCompleted;
            photoChooserTask.Show();
        }

        private void photoCaptureOrSelectionCompleted(object sender, PhotoResult e)
        {
            if (e.TaskResult != TaskResult.OK)
                return;

            var selectedImage = new BitmapImage();
            selectedImage.SetSource(e.ChosenPhoto);
            newImage.Source = selectedImage;

            newImage.Visibility = Visibility.Visible;

            this.UpdateLayout();

            Dispatcher.BeginInvoke(() =>
                                       {
                                           if (MessageBox.Show(
                                               "Are you sure you want to change to this new profile picture?",
                                               "Change profile picture", MessageBoxButton.OKCancel) ==
                                               MessageBoxResult.OK)
                                           {
                                               UiHelper.ShowProgressBar("uploading new photo");

                                               var newStream = UiHelper.PrepareImageForUpload(e, 700, false);

                                               if (newStream == null)
                                               {
                                                   UiHelper.HideProgressBar();
                                                   return;
                                               }

                                               var stream = StorageHelperUI.UpdateCachedImage(AccountId,
                                                                                              newStream,
                                                                                              e.OriginalFileName,
                                                                                              ApplicationConstants.AccountTypeEnum.Twitter);

                                               var api = new TwitterApi(AccountId);
                                               api.UpdateProfilePhotoCompletedEvent += api_UpdateProfilePhotoCompletedEvent;
                                               api.UpdateProfilePhoto(stream, e.OriginalFileName);
                                           }
                                           else
                                           {
                                               newImage.Visibility = Visibility.Collapsed;
                                           }
                                       });
        }

        private void api_UpdateProfilePhotoCompletedEvent(object sender, EventArgs e)
        {
            UiHelper.HideProgressBar();

            var api = sender as TwitterApi;

            if (api.HasError)
            {
                UiHelper.ShowToast("There was a problem upload the image.");
                return;
            }

            ((IMehdohApp)(Application.Current)).ViewModel.ProfileImageUpdated();

            UiHelper.SafeDispatch(() =>
                                      {
                                          var sh = new ShellHelper();
                                          sh.ResetLiveTile(api.AccountId, false);
                                          MessageBox.Show(
                                              "Your profile picture has now been uploaded, but it may take a few minutes to update completely on Twitter.",
                                              "Update Profile Picture", MessageBoxButton.OK);
                                      });
        }

        private void txtFollowing_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigationService.Navigate(
                new Uri("/NewFollowing.xaml?accountId=" + AccountId + "&mode=view&screen=" + ScreenName,
                        UriKind.Relative));
        }

        private void txtFollowers_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigationService.Navigate(
                new Uri("/NewFollowers.xaml?accountId=" + AccountId + "&mode=view&screen=" + ScreenName,
                        UriKind.Relative));
        }

        private void pivotMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckListViewEventsAreSet();

            switch (pivotMain.SelectedIndex)
            {
                case 0: // info
                    break;
                case 1: // timeline
                    if (Timeline == null || Timeline.Count == 0 && lstTimeline.Visibility == Visibility.Visible)
                        // only show if the user isn't protected
                        ThreadPool.QueueUserWorkItem(StartGetPublicTimeline);
                    break;
                case 2: // mentions                    
                    if (Mentions == null || Mentions.Count == 0)
                        ThreadPool.QueueUserWorkItem(StartGetMentions);
                    break;
                case 3: // favourites
                    if (Favourites == null || Favourites.Count == 0)
                        ThreadPool.QueueUserWorkItem(StartGetFavourites);
                    break;
                case 4: // photo
                    break;
            }
        }

        private async void StartGetFavourites(object state)
        {
            UiHelper.ShowProgressBar(ApplicationResources.refreshingfavourites);

            var api = new TwitterApi(AccountId);
            var results = await api.GetFavourites(0, 0, ScreenName);
            ApiOnGetFavouritesCompletedEvent(AccountId, results, api.HasError, api.ErrorMessage);
        }

        private void ApiOnGetFavouritesCompletedEvent(long accountId, List<ResponseTweet> favourites, bool hasError, string errorMessage)
        {

            UiHelper.HideProgressBar();

            if (hasError && !string.IsNullOrEmpty(errorMessage))
            {
                UiHelper.ShowToast("twitter error", errorMessage);
                return;
            }

            if (favourites == null) // nothing, no favourites?
                return;

            List<FavouritesViewModel> res = null;

            try
            {
                res = ViewModelHelper.FavouritesResponseToView(AccountId, favourites);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("ApiOnGetFavouritesCompletedEvent", ex);
            }

            if (res == null)
                return;

            UiHelper.SafeDispatch(() =>
                                      {
                                          try
                                          {
                                              foreach (var favouritesViewModel in res)
                                              {
                                                  Favourites.Add(favouritesViewModel);
                                              }
                                          }
                                          catch (Exception ex)
                                          {
                                          }
                                      });

        }


        private void lstMentions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (lstMentions.SelectedIndex == -1)
                return;

            var item = lstMentions.SelectedValue as SearchResultViewModel;
            string query = string.Format("accountId={0}&id={1}", item.AccountId, item.Id);

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/DetailsPage.xaml?" + query, UriKind.Relative));

            // Reset selected index to -1 (no selection)
            lstMentions.SelectedIndex = -1;
        }

        private void pivotMain_DoubleTap(object sender, GestureEventArgs e)
        {
            switch (pivotMain.SelectedIndex)
            {
                case 1:
                    if (Timeline == null)
                        break;
                    var firstTimeline = Timeline.FirstOrDefault();
                    if (firstTimeline != null)
                        lstTimeline.ScrollIntoView(firstTimeline);
                    break;

                case 2:
                    if (Mentions == null)
                        break;
                    var firstMention = Mentions.FirstOrDefault();
                    if (firstMention != null)
                        lstMentions.ScrollIntoView(firstMention);
                    break;
            }
        }

        private void txtLists_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var newUri = new Uri("/UserListed.xaml?accountId=" + AccountId + "&page=1&screen=" + ScreenName, UriKind.Relative);
            UiHelper.SafeDispatch(() => NavigationService.Navigate(newUri));
        }

        private void txtListed_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var newUri = new Uri("/UserListed.xaml?accountId=" + AccountId + "&page=0&screen=" + ScreenName, UriKind.Relative);
            UiHelper.SafeDispatch(() => NavigationService.Navigate(newUri));
        }

        private void mnuLists_Click(object sender, EventArgs e)
        {
            var newUri = new Uri("/ListMembership.xaml?" + AccountId + "&Screen=" + ScreenName, UriKind.Relative);
            UiHelper.SafeDispatch(() => NavigationService.Navigate(newUri));
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

        private void lstFavourites_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (lstFavourites.SelectedIndex == -1)
                return;

            var item = lstFavourites.SelectedValue as TimelineViewModel;

            bool isFave = (ScreenName == ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(AccountId));

            string query = string.Format("accountId={0}&favId={1}", item.AccountId, item.Id);

            if (isFave)
                query += "&isFave=1";

            // Navigate to the new page
            UiHelper.SafeDispatch(() => NavigationService.Navigate(new Uri("/DetailsPage.xaml?" + query, UriKind.Relative)));

            // Reset selected index to -1 (no selection)
            lstFavourites.SelectedIndex = -1;

        }

        private async void mnuMute_Click(object sender, EventArgs e)
        {
            UiHelper.ShowProgressBar("muting user");
            var api = new TwitterApi(AccountId);
            var result = await api.MuteUserAsync(ScreenName);
            UiHelper.HideProgressBar();
            UiHelper.ShowToast(ScreenName + " has been muted");            
        }

    }

}
