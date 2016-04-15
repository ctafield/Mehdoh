#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Diagnostics;
using System.Globalization;
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
using System.Windows.Shapes;
using Windows.ApplicationModel.DataTransfer;
using Clarity.Phone.Extensions;
using Coding4Fun.Toolkit.Controls;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.ErrorLogging;
using FieldOfTweets.Common.ImageHostParser;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.Classes;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Helpers;
using FieldOfTweets.Common.UI.ImageHostParser;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.Resources;
using FieldOfTweets.Common.UI.ThirdPartyApi;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;

#if WP7
using Microsoft.Phone.Controls.Maps;
#endif
using Newtonsoft.Json;
#if WP8
using Microsoft.Phone.Maps.Controls;
#endif

using PocketWP;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
//using Mitter.Animations.Page.Extensions;
using Mitter.Helpers;
using Mitter.UserControls;
using MyToolkit.Multimedia;
using Telerik.Windows.Controls;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using SelectTwitterAccount = FieldOfTweets.Common.UI.UserControls.SelectTwitterAccount;

#endregion

namespace Mitter
{
    public partial class DetailsPage : AnimatedBasePage
    {

        #region Fields

        private bool _isMenuOpen;
        private string _rotatingText;

        #endregion

        #region Properties

        private AppBarPrompt ShareBarPrompt { get; set; }
        private AppBarPrompt RetweetBarPrompt { get; set; }
        private RadContextMenu _menu;
        private DialogService ImagePopup { get; set; }
        public SortedObservableCollection<TimelineViewModel> Timeline { get; set; }
        private DetailsPageViewModel ViewModel { get; set; }

        /// <summary>
        /// The tweet belongs to this account
        /// </summary>
        private long AccountId { get; set; }

        private bool IsPhoto { get; set; }

        private bool IsCurrentlyFavourite { get; set; }

        private bool PageLoaded { get; set; }
        private Uri VineVideoSourceUri { get; set; }
        private bool HistoryRequired { get; set; }
        private long? HistoryReplyId { get; set; }

        private string ReplyUrl
        {
            get
            {
                try
                {
                    string replyUrl;

                    if (ViewModel.TweetType == TweetTypeEnum.Message)
                    {
                        replyUrl = "accountId=" + ViewModel.AccountId + " &replyToAuthor=" + ViewModel.ScreenName +
                                   "&replyToId=" + ViewModel.Id + "&dm=true";
                    }
                    else
                    {
                        var otherAuthors = GetAuthors();
                        var hashTags = GetHashTags();

                        replyUrl = "accountId=" + ViewModel.AccountId + "&replyToAuthor=" + ViewModel.ScreenName +
                                   "&replyToId=" + ViewModel.Id + "&others=" + otherAuthors;

                        if (!string.IsNullOrEmpty(hashTags))
                        {
                            replyUrl = replyUrl + "&hashtags=" + HttpUtility.UrlEncode(hashTags);
                        }
                    }

                    return replyUrl;
                }
                catch (Exception)
                {
                    if (ViewModel != null)
                    {
                        string replyUrl = "accountId=" + ViewModel.AccountId + " &replyToAuthor=" + ViewModel.ScreenName +
                                          "&replyToId=" + ViewModel.Id;
                        return replyUrl;
                    }
                    else
                    {
                        // dont know why we would ever be in here?
                        return "accountId=" + AccountId;
                    }
                }
            }
        }

        #endregion

        #region Constructor

        public DetailsPage()
        {
            InitializeComponent();

            InteractionEffectManager.AllowedTypes.Add(typeof(ReplyView));

            Timeline = new SortedObservableCollection<TimelineViewModel>();
            lstTimeline.DataContext = Timeline;
            AnimationContext = LayoutRoot;
        }

#if DEBUG
        ~DetailsPage()
        {
            Debug.WriteLine("*****************************************  DetailsPage GC");
        }
#endif

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            if (Timeline != null)
            {
                Timeline.Clear();
                Timeline = null;
            }

            ShareBarPrompt = null;
            RetweetBarPrompt = null;
            _menu = null;
            ImagePopup = null;
            ViewModel = null;

            GC.Collect();

            base.OnRemovedFromJournal(e);
        }

        #endregion

        #region Common Screen Name from Current Account id Code

        private string CurrentAccountScreenName
        {
            get { return ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(AccountId); }
        }

        #endregion

        #region Overrides

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn)
                //return new SlideUpAnimator() { RootElement = LayoutRoot };
                return null;
            
            return new SlideDownAnimator { RootElement = LayoutRoot };
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            if (YouTube.CancelPlay()) // used to abort current youtube download
            {
                e.Cancel = true;
                return;
            }

            if (_isMenuOpen)
            {
                _menu.IsOpen = false;
                _isMenuOpen = false;
                e.Cancel = true;
                return;
            }

            if (ImagePopup != null)
            {
                HideImagePopup();
                e.Cancel = true;
                return;
            }

            if (RetweetBarPrompt != null)
            {
                if (RetweetBarPrompt.IsOpen)
                {
                    RetweetBarPrompt.Hide();
                    e.Cancel = true;
                    return;
                }
            }

            if (ShareBarPrompt != null)
            {
                if (ShareBarPrompt.IsOpen)
                {
                    ShareBarPrompt.Hide();
                    e.Cancel = true;
                    return;
                }
            }

            base.OnBackKeyPress(e);

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_isMenuOpen)
            {
                _menu.IsOpen = false;
                _isMenuOpen = false;
            }

            ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet = null;

            base.OnNavigatedFrom(e);
        }

        // When page is navigated to set data context to selected item in list
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {

            YouTube.CancelPlay(); // used to re enable page
            UiHelper.HideProgressBar();

            if (PageLoaded)
            {
                return;
            }

            if (NavigationService.CanGoBack)
            {
                bool done = false;
                while (!done)
                {
                    var last = NavigationService.BackStack.FirstOrDefault();
                    if (last != null)
                    {
                        if (last.Source.OriginalString.ToLower().Contains("detailspage.xaml"))
                        {
                            NavigationService.RemoveBackEntry();
                        }
                        else
                        {
                            done = true;
                        }
                    }
                    else
                    {
                        done = true;
                    }
                }
            }

            gridDetails.Visibility = Visibility.Collapsed;

            UiHelper.ShowProgressBar("retrieving tweet");

            string tempy;
            if (!NavigationContext.QueryString.TryGetValue("accountId", out tempy))
            {
                throw new Exception("missing account id");
            }

            AccountId = long.Parse(tempy);

            string temp;

            if (NavigationContext.QueryString.TryGetValue("photo", out temp))
            {
                bool tempBool;
                if (bool.TryParse(temp, out tempBool))
                {
                    IsPhoto = tempBool;
                }
            }

            if (((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet != null)
            {
                var currentTweet = ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet;

                ViewModel = currentTweet.AsDetailsViewModel(AccountId);

                SetTweetSync(currentTweet.id);

                if (ViewModel.InReplyToId > 0)
                    StartGetHistory(currentTweet.id);
                else
                    RemoveUnusedPivot();

                IsCurrentlyFavourite = false;

                if (NavigationContext.QueryString.ContainsKey("isFave"))
                    IsCurrentlyFavourite = true;

                SetDataContext(ViewModel);
            }
            else if (NavigationContext.QueryString.TryGetValue("id", out temp))
            {
                #region Regular Tweet

                long id = long.Parse(temp);

                SetTweetSync(id);

                var dvh = new DetailsViewHelper();
                ViewModel = dvh.TryGetDetailsViewFromTimelineTweet(id, AccountId);

                if (ViewModel == null)
                {
                    if (NavigationContext.QueryString.ContainsKey("isFave"))
                        IsCurrentlyFavourite = true;

                    await GetTweetById(id);

                }
                else
                {
                    IsCurrentlyFavourite = false;

                    if (NavigationContext.QueryString.ContainsKey("isFave"))
                        IsCurrentlyFavourite = true;

                    if (ViewModel.InReplyToId.HasValue && ViewModel.InReplyToId.Value > 0)
                        StartGetHistory(id);
                    else
                        RemoveUnusedPivot();

                    SetDataContext(ViewModel);
                }

                #endregion
            }
            else if (NavigationContext.QueryString.TryGetValue("favId", out temp))
            {
                #region Favourite Tweet

                long id = long.Parse(temp);

                using (var dh = new MainDataContext())
                {
                    IQueryable<FavouriteTable> newRes = from faves in dh.Favourites
                                                        where faves.Id == id && faves.ProfileId == AccountId
                                                        select faves;

                    FavouriteTable newTweet = newRes.FirstOrDefault();

                    if (newTweet != null)
                    {
                        ViewModel = newTweet.AsDetailsViewModel();

                        if (ViewModel.InReplyToId > 0)
                            StartGetHistory(id);
                        else
                            RemoveUnusedPivot();

                        IsCurrentlyFavourite = true;

                        SetDataContext(ViewModel);
                    }
                    else
                    {

                        if (NavigationContext.QueryString.ContainsKey("isFave"))
                            IsCurrentlyFavourite = true;

                        await GetTweetById(id);
                    }
                }

                #endregion

            }
            else if (NavigationContext.QueryString.TryGetValue("mentionId", out temp))
            {
                #region Mention Tweet

                long id = long.Parse(temp);

                using (var dh = new MainDataContext())
                {
                    IQueryable<MentionTable> newRes = from mention in dh.Mentions
                                                      where
                                                          mention.Id == id &&
                                                          mention.ProfileId == AccountId
                                                      select mention;

                    MentionTable newTweet = newRes.FirstOrDefault();

                    if (newTweet != null)
                    {
                        ViewModel = newTweet.AsDetailsViewModel();

                        if (ViewModel.InReplyToId > 0)
                            StartGetHistory(id);
                        else
                            RemoveUnusedPivot();

                        IsCurrentlyFavourite = false;
                        SetDataContext(ViewModel);
                    }
                    else
                    {
                        var api = new TwitterApi(AccountId);
                        var tweet = await api.GetTweet(id);
                        api_GetTweetCompletedEvent(AccountId, null, tweet);
                    }
                }

                #endregion
            }
            else if (NavigationContext.QueryString.TryGetValue("messageId", out temp))
            {
                #region Message Tweet

                long id = long.Parse(temp);

                pivotNewTweet.Header = "message";

                pivotMain.SelectedIndex = 1;

                using (var dh = new MainDataContext())
                {
                    IQueryable<MessageTable> newRes = from message in dh.Messages
                                                      where
                                                          message.Id == id &&
                                                          message.ProfileId == AccountId
                                                      select message;

                    MessageTable newTweet = newRes.FirstOrDefault();

                    if (newTweet != null)
                    {
                        ViewModel = newTweet.AsDetailsViewModel();

                        StartGetDMHistory(newTweet.ScreenName);

                        IsCurrentlyFavourite = false;

                        SetDataContext(ViewModel);
                    }
                }

                #endregion
            }

            ConfigureApplicationBar();

            PageLoaded = true;
        }

        private async Task GetTweetById(long id)
        {
            var api = new TwitterApi(AccountId);

            var cachedTweet = api.IsCachedTweet(id);

            if (cachedTweet != null)
            {
                api_GetTweetCompletedEvent(AccountId, cachedTweet, null);
            }
            else
            {
                var result = await api.GetTweet(id);
                api_GetTweetCompletedEvent(AccountId, null, result);
            }
        }

        #endregion

        #region Members

        private string GetTweetUrl()
        {
            return string.Format("http://twitter.com/{0}/status/{1}", ViewModel.ScreenName, ViewModel.Id);
        }

        private string GetDescription()
        {
            return ViewModel.IsRetweet ? ViewModel.RetweetDescription : ViewModel.Description;
        }

        private string GetHashTags()
        {
            if (ViewModel == null || ViewModel.Assets == null)
                return string.Empty;

            var assets = ViewModel.Assets.Where(x => x.Type == AssetTypeEnum.Hashtag).Select(x => x.ShortValue);
            var newAss = assets.ToList();
            return string.Join(",", newAss);
        }

        private string GetAuthors()
        {
            try
            {
                string originalAuthor = string.Empty;
                IEnumerable<string> assets = ViewModel.Assets.Where(x => x.Type == AssetTypeEnum.Mention).Select(x => x.ShortValue).Distinct();

                if (ViewModel.IsRetweet)
                    originalAuthor = ViewModel.RetweetScreenName;

                var newAss = assets.ToList();
                var newList = new List<string>();

                if (!string.IsNullOrEmpty(originalAuthor))
                    if (!originalAuthor.StartsWith("@"))
                        newList.Add("@" + originalAuthor);
                    else
                        newList.Add(originalAuthor);

                foreach (var s in newAss.Where(s => String.Compare(s.Replace("@", ""), CurrentAccountScreenName.Replace("@", ""), StringComparison.CurrentCultureIgnoreCase) != 0))
                {
                    string newValue;

                    if (!s.StartsWith("@"))
                        newValue = "@" + s;
                    else
                        newValue = s;

                    if (!newList.Contains(newValue)) newList.Add(newValue);
                }

                return string.Join(",", newList);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void RemoveUnusedPivot()
        {
            try
            {
                pivotMain.Items.RemoveAt(1);
            }
            catch (Exception)
            {
            }
        }

        private void SetTweetSync(long id)
        {
            ThreadPool.QueueUserWorkItem(delegate
                                             {
                                                 var sh = new SettingsHelper();
                                                 if (!sh.GetUseTweetMarker())
                                                     return;

                                                 try
                                                 {
                                                     var twApi = new TweetMarkerApi();
                                                     twApi.SetLastTimelineRead(AccountId, id);

                                                     ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTimeline(AccountId);

                                                     var thisItem = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[AccountId].FirstOrDefault(x => x.Id == id && x.AccountId == AccountId);

                                                     if (thisItem != null)
                                                     {
                                                         // reset all of them
                                                         ((IMehdohApp)(Application.Current)).ViewModel.Timeline[AccountId].Apply(x => x.IsSyncSetting = false);
                                                         // set them all on again
                                                         thisItem.IsSyncSetting = true;
                                                     }
                                                 }
                                                 catch (Exception ex)
                                                 {
#if WP8
                                                     CrittercismSDK.Crittercism.LogHandledException(ex);
#endif
                                                     ErrorLogger.LogException("SetTweetSync", ex);

                                                 }
                                             });
        }

        private void showRetweetsButton_Click(object sender, EventArgs e)
        {
            string newUrl = string.Format("/RetweetsOfMe.xaml?accountId=" + AccountId + "&id=" + ViewModel.Id);
            NavigationService.Navigate(new Uri(newUrl, UriKind.Relative));
        }

        private void mnuSaveTweet_Click(object sender, EventArgs e)
        {
            var slh = new SaveLaterHelper();
            slh.SaveFinishedEvent += slh_SaveFinishedEvent;

            UiHelper.ShowProgressBar("saving link for later");

            if (!slh.SaveUrl(GetTweetUrl(), GetDescription(), ViewModel.Id))
            {
                UiHelper.HideProgressBar();

                UiHelper.SafeDispatch(() =>
                                          {
                                              if (MessageBox.Show("Instapaper and Pocket are not configured. Would you like to configure them now?",
                                                                  "save for later", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                                              {
                                                  NavigationService.Navigate(new Uri("/SaveLater.xaml?url=" + HttpUtility.UrlEncode(GetTweetUrl()) +
                                                          "&desc=" + HttpUtility.UrlEncode(GetDescription()) + "&id=" +
                                                          ViewModel.Id, UriKind.Relative));
                                              }
                                          });
            }
        }

        private void slh_SaveFinishedEvent(object sender, EventArgs e)
        {
            var slh = sender as SaveLaterHelper;

            if (slh == null || slh.RefreshingCountRemaining != 0)
                return;

            slh.SaveFinishedEvent -= slh_SaveFinishedEvent;

            UiHelper.HideProgressBar();

            if (slh.ErrorReadLater && slh.ErrorReadLater)
            {
                UiHelper.ShowToast("failed to save to pocket and instapaper");
            }
            else if (slh.ErrorInstapaper)
            {
                UiHelper.ShowToast("failed to save to instapaper");
            }
            else if (slh.ErrorReadLater)
            {
                UiHelper.ShowToast("failed to save to pocket");
            }
            else
            {
                UiHelper.ShowToast("link saved for later");
            }
        }

        private void mnuTranslate_Click(object sender, EventArgs e)
        {
            var description = GetDescription();

            string language;

            try
            {
                language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
            }
            catch (Exception)
            {

                language = "en";
            }

            UiHelper.ShowProgressBar("translating");

            var bing = new BingTranslateApi();
            bing.TranslateTextCompleted += bing_TranslateTextCompleted;
            bing.TranslateText(description, language);
        }

        private void api_GetTweetCompletedEvent(long accountId, TimelineTable tweetCached, ResponseTweet tweet)
        {

            Dispatcher.BeginInvoke(delegate
            {

                try
                {
                    if (tweetCached != null)
                    {
                        ViewModel = tweetCached.AsDetailsViewModel();
                    }
                    else
                    {
                        var dsh = new DataStorageHelper();
                        ViewModel = dsh.TimelineResponseToTable(tweet, accountId).AsDetailsViewModel(false);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException("api_GetTweetCompletedEvent", ex);
                }

                ConfigureApplicationBar();

                if (ViewModel != null)
                {
                    if (ViewModel.InReplyToId > 0)
                        StartGetHistory(ViewModel.Id);
                    else
                        pivotMain.Items.RemoveAt(1);

                    SetDataContext(ViewModel);
                }
                else
                {
                    MessageBox.Show("There was a problem fetching this tweet :(");
                }

            });
        }


        private void ConfigureApplicationBar()
        {
            // resolve the current account id to delete if its the current user
            string currentScreenName = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(AccountId);

            if (ViewModel != null && ViewModel.TweetType != TweetTypeEnum.Message)
            {

                // buttons
                var favouriteButton = new ApplicationBarIconButton
                                          {
                                              Text = ApplicationResources.favourite,
                                              IconUri = IsCurrentlyFavourite
                                                      ? new Uri("/Images/76x76/dark/appbar.star.minus.png", UriKind.Relative)
                                                      : new Uri("/Images/76x76/dark/appbar.star.add.png", UriKind.Relative)
                                          };

                favouriteButton.Click += mnuFavorite_Click;
                ApplicationBar.Buttons.Add(favouriteButton);

                // if its the current user, then we can't show retweet, so show delete instead.
                if (string.Compare(ViewModel.ScreenName, currentScreenName, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    var retweetButton = new ApplicationBarIconButton
                                            {
                                                IconUri = new Uri("/Images/appbar.retweet.new.png", UriKind.Relative),
                                                Text = "retweet"
                                            };
                    retweetButton.Click += mnuRetweet_Click;
                    ApplicationBar.Buttons.Add(retweetButton);
                }
                else
                {
                    var deletemenu = new ApplicationBarIconButton
                                         {
                                             IconUri = new Uri("/Images/76x76/dark/appbar.delete.png", UriKind.Relative),
                                             Text = "delete"
                                         };
                    deletemenu.Click += mnuDelete_Click;
                    ApplicationBar.Buttons.Add(deletemenu);
                }

                var shareButton = new ApplicationBarIconButton
                                      {
                                          IconUri = new Uri("/Images/76x76/dark/appbar.share.png", UriKind.Relative),
                                          Text = "share..."
                                      };
                shareButton.Click += mnuShare_Click;
                ApplicationBar.Buttons.Add(shareButton);

                //// menu items
                //var editRetweetMenu = new ApplicationBarMenuItem
                //                          {
                //                              Text = "edit and retweet"
                //                          };
                //editRetweetMenu.Click += mnuEditRetweet_Click;
                //ApplicationBar.MenuItems.Add(editRetweetMenu);

                var showRetweetsButton = new ApplicationBarMenuItem
                                             {
                                                 Text = "show retweets"
                                             };
                showRetweetsButton.Click += showRetweetsButton_Click;
                ApplicationBar.MenuItems.Add(showRetweetsButton);

                var saveTweet = new ApplicationBarMenuItem
                                    {
                                        Text = "save for later"
                                    };
                saveTweet.Click += mnuSaveTweet_Click;
                ApplicationBar.MenuItems.Add(saveTweet);

                var copyMenu = new ApplicationBarMenuItem
                {
                    Text = "copy tweet to clipboard"
                };
                copyMenu.Click += delegate
                {
                    Clipboard.SetText(string.Format("@{0}: {1}", ViewModel.ScreenName,
                             HttpUtility.HtmlDecode(GetDescription())));
                };
                ApplicationBar.MenuItems.Add(copyMenu);

                var translateTweet = new ApplicationBarMenuItem
                                         {
                                             Text = "translate tweet with BING\u2122"
                                         };
                translateTweet.Click += mnuTranslate_Click;
                ApplicationBar.MenuItems.Add(translateTweet);

                if (ViewModel.IsRetweet)
                {
                    // Other profile menu item
                    var otherProfile = new ApplicationBarMenuItem
                                           {
                                               Text = "view @" + ViewModel.RetweetScreenName
                                           };
                    otherProfile.Click += mnuOtherProfile_Click;
                    ApplicationBar.MenuItems.Add(otherProfile);
                }

            }

            if (ViewModel != null)
            {
                var authorProfile = new ApplicationBarMenuItem
                                        {
                                            Text = "view " + ViewModel.ScreenNameFormatted
                                        };
                authorProfile.Click += mnuProfile_Click;
                ApplicationBar.MenuItems.Add(authorProfile);


                if (ViewModel.TweetType != TweetTypeEnum.Message)
                {
                    var spamButton = new ApplicationBarMenuItem
                                         {
                                             Text = "report spam"
                                         };
                    spamButton.Click += mnuSpam_Click;
                    ApplicationBar.MenuItems.Add(spamButton);
                }

                if (ViewModel.TweetType == TweetTypeEnum.Message)
                {
                    // you can always delete your own messages
                    var deletemenu = new ApplicationBarIconButton
                                         {
                                             IconUri = new Uri("/Images/76x76/dark/appbar.delete.png", UriKind.Relative),
                                             Text = "delete"
                                         };
                    deletemenu.Click += mnuDelete_Click;
                    ApplicationBar.Buttons.Add(deletemenu);
                }
            }

            ApplicationBar.MatchOverriddenTheme();

        }

        private void SetDataContext(DetailsPageViewModel model)
        {

            SetFlowDirection(model);

            SetDescription();

            try
            {
                SetExtraPanels();
            }
            catch (Exception)
            {
                // Dealt with
            }

            DataContext = model;

            txtLocationFull.Visibility = string.IsNullOrWhiteSpace(model.LocationFull)
                                             ? Visibility.Collapsed
                                             : Visibility.Visible;

            UiHelper.HideProgressBar();

            GetReplies();

            ShowForm();
        }

        private async void GetReplies()
        {
            if (ViewModel.TweetType == TweetTypeEnum.Message)
                return;

            ReplyIds = new List<long>();

            var api = new TwitterApi(AccountId);

            long id = (ViewModel.OriginalRetweetId.HasValue) ? ViewModel.OriginalRetweetId.Value : ViewModel.Id;
            var result = await api.SearchForReplies(ViewModel.ScreenName, sinceId: id);
            apiSearchForRepliesCompleted(result);

        }

        private List<long> ReplyIds { get; set; }

        private async void apiSearchForRepliesCompleted(ResponseSearch searchResult)
        {

            if (searchResult == null || searchResult.statuses == null)
                return;

            UiHelper.SafeDispatch(async () =>
            {
                try
                {
                    if (ViewModel == null)
                        return;

                    long currentId = (ViewModel.OriginalRetweetId.HasValue) ? ViewModel.OriginalRetweetId.Value : ViewModel.Id;

                    foreach (var item in searchResult.statuses.OrderByDescending(x => x.id))
                    {

                        if (item.in_reply_to_status_id_str == currentId.ToString(CultureInfo.InvariantCulture) && ReplyIds.All(x => x != item.id))
                        {
                            var replyModel = item.AsViewModel(AccountId);
                            if (!replyModel.IsRetweet)
                            {
                                var reply = new ReplyViewModel()
                                {
                                    Message = replyModel.Description,
                                    PictureUrl = replyModel.ImageUrl,
                                    Time = replyModel.CreatedAt,
                                    UserName = replyModel.Author,
                                    DisplayName = replyModel.DisplayName,
                                    Id = replyModel.Id,
                                    AccountId = AccountId
                                };

                                var replyControl = new ReplyView();
                                replyControl.Tap += delegate(object o, GestureEventArgs args)
                                {
                                    var control = o as ReplyView;
                                    if (control != null)
                                    {
                                        var thisItem = control.DataContext as ReplyViewModel;

                                        if (thisItem != null)
                                        {
                                            string query = "accountId=" + thisItem.AccountId + "&id=" + thisItem.Id;
                                            NavigationService.Navigate(new Uri("/DetailsPage.xaml?" + query, UriKind.Relative));
                                        }
                                    }
                                };
                                replyControl.SetValue(reply);
                                repliesPanel.Children.Add(replyControl);

                                ReplyIds.Add(replyModel.Id);
                            }
                        }
                    }

                    if (searchResult.statuses.Any())
                    {
                        long oldestId = searchResult.statuses.Min(x => x.id);
                        long id = (ViewModel.OriginalRetweetId.HasValue) ? ViewModel.OriginalRetweetId.Value : ViewModel.Id;

                        if (oldestId > id)
                        {
                            var newApi = new TwitterApi(AccountId);
                            var result = await newApi.SearchForReplies(ViewModel.ScreenName, newestId: oldestId);
                            apiSearchForRepliesCompleted(result);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (ViewModel == null)
                        return;
                    Console.WriteLine(e);
                }

            });
        }

        private void SetFlowDirection(DetailsPageViewModel model)
        {
            wrapText.FlowDirection = UiHelper.GetFlowDirection(model.LanguageCode);
        }

        private static DoubleAnimation CreateAnimation(double from, double to, double duration,
                                               PropertyPath targetProperty, DependencyObject target)
        {
            var db = new DoubleAnimation
            {
                To = to,
                From = from,
                EasingFunction = new SineEase(),
                Duration = TimeSpan.FromSeconds(duration),
            };
            Storyboard.SetTarget(db, target);
            Storyboard.SetTargetProperty(db, targetProperty);
            return db;
        }

        private void ShowForm()
        {

            var sb = new Storyboard();

            sb.Children.Add(CreateAnimation(0, 1, 0.5, new PropertyPath(OpacityProperty), gridDetails));
            sb.Children.Add(CreateAnimation(150, 0, 0.5, new PropertyPath(TranslateTransform.YProperty), gridDetails.RenderTransform));

            gridDetails.Opacity = 0;
            gridDetails.Visibility = Visibility.Visible;

            sb.Begin();
        }


        private void mnuSpam_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to report this user as spam? This will also block the user.",
                                "report spam", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                UiHelper.ShowProgressBar("reporting spam");

                var api = new TwitterApi(ViewModel.AccountId);
                api.ReportSpamCompletedEvent += api_ReportSpamCompletedEvent;
                api.ReportSpam(ViewModel.ScreenName);
            }
        }

        private async void api_ReportSpamCompletedEvent(object sender, EventArgs e)
        {
            var api = new TwitterApi(ViewModel.AccountId);

            await api.BlockUser(ViewModel.ScreenName);
            api_BlockUserForSpamCompletedEvent(ViewModel.AccountId, ViewModel.ScreenName);

        }

        private void api_BlockUserForSpamCompletedEvent(long accountId, string screenName)
        {

            UiHelper.SafeDispatch(() =>
                                      {
                                          try
                                          {

                                              long tweetId = ViewModel.Id;

                                              // Update the data context
                                              using (var dc = new MainDataContext())
                                              {
                                                  var timelineRecs = from updates in dc.Timeline
                                                                     where updates.Id == tweetId
                                                                           && updates.ProfileId == ViewModel.AccountId
                                                                     select updates;

                                                  foreach (var rec in timelineRecs)
                                                  {
                                                      var asses = from assets in dc.TimelineAsset
                                                                  where assets.ParentId == tweetId
                                                                  select assets;

                                                      dc.TimelineAsset.DeleteAllOnSubmit(asses);
                                                      dc.Timeline.DeleteOnSubmit(rec);
                                                  }

                                                  var mentionRecs = from updates in dc.Mentions
                                                                    where updates.Id == tweetId
                                                                          && updates.ProfileId == ViewModel.AccountId
                                                                    select updates;

                                                  foreach (var rec in mentionRecs)
                                                  {
                                                      var mentionAssets = from assets in dc.MentionAsset
                                                                          where assets.ParentId == tweetId
                                                                          select assets;

                                                      dc.MentionAsset.DeleteAllOnSubmit(mentionAssets);
                                                      dc.Mentions.DeleteOnSubmit(rec);
                                                  }

                                                  dc.SubmitChanges();
                                              }

                                              if (((IMehdohApp)(Application.Current)).ViewModel.Timeline != null &&
                                                  ((IMehdohApp)(Application.Current)).ViewModel.Timeline.ContainsKey(accountId))
                                              {
                                                  var res =
                                                      ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].FirstOrDefault(
                                                          x => x.Id == tweetId);
                                                  if (res != null)
                                                      ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Remove(res);
                                              }

                                              if (((IMehdohApp)(Application.Current)).ViewModel.Mentions != null &&
                                                  ((IMehdohApp)(Application.Current)).ViewModel.Mentions.ContainsKey(accountId))
                                              {
                                                  var res2 =
                                                      ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].FirstOrDefault(
                                                          x => x.Id == tweetId);
                                                  if (res2 != null)
                                                      ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Remove(res2);
                                              }

                                          }
                                          catch (Exception ex)
                                          {

                                              ErrorLogger.LogException(ex);
                                          }
                                          finally
                                          {

                                              UiHelper.HideProgressBar();

                                              if (NavigationService.CanGoBack)
                                                  NavigationService.GoBack();

                                              UiHelper.HideProgressBar();

                                              if (!string.IsNullOrEmpty(screenName))
                                              {
                                                  MessageBox.Show("Successfully reported as spam and blocked @" +
                                                                  screenName,
                                                      "report spam", MessageBoxButton.OK);
                                              }
                                          }

                                      });
        }


#if WP8

        private void GetMap()
        {
            if (!((IMehdohApp)Application.Current).DisplayMaps)
                return;

            var locationFullName = ViewModel.LocationFull;

            if (string.IsNullOrEmpty(locationFullName))
                return;

            if (ViewModel.Location1Y == 0 && ViewModel.Location2Y == 0)
                return;

            var pivotItem = new PivotItem
                                {
                                    Header = "map",
                                };

            if (LicenceInfo.IsTrial())
            {
                pivotItem.Content = GetTrialPanel("map");
            }
            else
            {
                var map = new Map();

                try
                {
                    // northwest, southeast                    
                    map.CartographicMode = MapCartographicMode.Road;

                    map.Center = new GeoCoordinate(ViewModel.Location1X, ViewModel.Location1Y);

                    map.ZoomLevel = 15;

                    map.Layers.Add(new MapLayer
                                       {
                                           new MapOverlay
                                               {
                                                   GeoCoordinate = new GeoCoordinate(ViewModel.Location1X, ViewModel.Location1Y),
                                                   Content = GetPushPin()
                                               }
                                       });

                    map.Width = 350;

                    pivotItem.Content = map;
                }
                catch (Exception ex)
                {
                    return;
                }
            }


            pivotMain.Items.Add(pivotItem);
        }

        private UIElement GetPushPin()
        {
            var gridPin = new Grid();

            gridPin.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20.0) });

            gridPin.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20.0) });
            gridPin.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20.0) });

            var rect = new Rectangle();
            rect.SetValue(Grid.RowProperty, 0);
            rect.Width = 20;
            rect.Height = 20;
            rect.Fill = Resources["PhoneAccentBrush"] as SolidColorBrush;

            var poly = new Polygon();
            poly.SetValue(Grid.RowProperty, 1);
            poly.Points.Add(new Point(0, 0));
            poly.Points.Add(new Point(20, 0));
            poly.Points.Add(new Point(20, 20));
            poly.Fill = Resources["PhoneAccentBrush"] as SolidColorBrush;

            gridPin.Children.Add(poly);
            gridPin.Children.Add(rect);

            gridPin.Opacity = 0.7;

            return gridPin;
        }

#endif

#if WP7

                private void GetMap()
        {

            if (!App.DisplayMaps)
                return;

            var locationFullName = ViewModel.LocationFull;

            if (string.IsNullOrEmpty(locationFullName))
                return;

            if (ViewModel.Location1Y == 0 && ViewModel.Location2Y == 0)
                return;

            var pivotItem = new PivotItem
            {
                Header = "map",
            };

            if (LicenceInfo.IsTrial())
            {
                pivotItem.Content = GetTrialPanel("map");
            }
            else
            {

                try
                {


                    //double x1, x2, y1, y2;

                    //x1 = Math.Min(ViewModel.Location1X, ViewModel.Location2X);
                    //x1 = Math.Min(ViewModel.Location3X, x1);
                    //x1 = Math.Min(ViewModel.Location4X, x1);

                    //y1 = Math.Max(ViewModel.Location1Y, ViewModel.Location2Y);
                    //y1 = Math.Max(ViewModel.Location3Y, y1);
                    //y1 = Math.Max(ViewModel.Location4Y, y1);

                    //y2 = Math.Min(ViewModel.Location1Y, ViewModel.Location2Y);
                    //y2 = Math.Min(ViewModel.Location3Y, y2);
                    //y2 = Math.Min(ViewModel.Location4Y, y2);

                    //x2 = Math.Max(ViewModel.Location1X, ViewModel.Location2X);
                    //x2 = Math.Max(ViewModel.Location3X, x2);
                    //x2 = Math.Max(ViewModel.Location4X, x2);

                    // northwest, southeast                    
                    var map = new Map
                    {
                        CredentialsProvider =
                            new ApplicationIdCredentialsProvider(ApplicationConstants.BingMapsApiKey)
                    };

                    map.SetView(new LocationRect
                    {
                        Northwest = new GeoCoordinate(ViewModel.Location1Y, ViewModel.Location1X),
                        Northeast = new GeoCoordinate(ViewModel.Location2Y, ViewModel.Location2X),
                        Southeast = new GeoCoordinate(ViewModel.Location3Y, ViewModel.Location3X),
                        Southwest = new GeoCoordinate(ViewModel.Location4Y, ViewModel.Location4X)
                    });

                    if (map.Center.Latitude != 0.0 && map.Center.Longitude != 0.0)
                    {
                        var pin = new Pushpin
                        {
                            Location = map.Center,
                            Background = Resources["PhoneAccentBrush"] as SolidColorBrush,
                            BorderBrush = Resources["PhoneForegroundBrush"] as SolidColorBrush,
                            BorderThickness = new Thickness(2)
                        };

                        map.Children.Add(pin);
                    }

                    map.ZoomLevel = 15;

                    map.Width = 350;

                    pivotItem.Content = map;

                }
                catch (Exception ex)
                {
                    var s = ex.Message;
                    return;
                }

            }


            pivotMain.Items.Add(pivotItem);

        }

        private UIElement GetPushPin()
        {

            var gridPin = new Grid();

            gridPin.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20.0) });

            gridPin.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(20.0) });
            gridPin.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(20.0) });

            var rect = new Rectangle();
            rect.SetValue(Grid.RowProperty, 0);
            rect.Width = 20;
            rect.Height = 20;
            rect.Fill = Resources["PhoneAccentBrush"] as SolidColorBrush;

            var poly = new Polygon();
            poly.SetValue(Grid.RowProperty, 1);
            poly.Points.Add(new Point(0, 0));
            poly.Points.Add(new Point(20, 0));
            poly.Points.Add(new Point(20, 20));
            poly.Fill = Resources["PhoneAccentBrush"] as SolidColorBrush;

            gridPin.Children.Add(poly);
            gridPin.Children.Add(rect);

            gridPin.Opacity = 0.7;

            return gridPin;
        }


#endif

        private void GetMedia(string longValue, string shortValue)
        {
            string targetUrl = shortValue;

            if (!string.IsNullOrEmpty(longValue))
                targetUrl = longValue;

            var pivotItem = new PivotItem
                                {
                                    Header = "image"
                                };


            var sp = new StackPanel();

            BitmapImage sourceImage;

            // is it cached?
            var res = IsImageCached(targetUrl);

            if (res != null)
            {
                sourceImage = GetImageFromLocalCache(res);
            }
            else
            {

                if (!IsPhoto)
                {
                    var pb = new PerformanceProgressBar
                    {
                        IsIndeterminate = true,
                        IsEnabled = true
                    };
                    sp.Children.Add(pb);
                }

                if (ViewModel.TweetType == TweetTypeEnum.Message)
                {
                    var api = new TwitterApi(AccountId);
                    sourceImage = new BitmapImage()
                    {
                        CreateOptions = BitmapCreateOptions.None
                    };
                    api.GetDirectMessageImageCompletedEvent += delegate(object sender, EventArgs args)
                    {
                        var twitterApi = sender as TwitterApi;
                        if (twitterApi == null || twitterApi.DmImageConent == null)
                            return;

                        UiHelper.SafeDispatch(() =>
                        {
                            try
                            {
                                using (var ms = new MemoryStream(twitterApi.DmImageConent))
                                {
                                    sourceImage.SetSource(ms);
                                }
                            }
                            catch (Exception)
                            {
                                // ignore
                            }
                        });

                    };
                    api.GetDirectMessageImage(targetUrl);
                }
                else
                {
                    sourceImage = new BitmapImage(new Uri(targetUrl, UriKind.Absolute));
                }
            }

            var image = new Image
                                {
                                    Source = sourceImage,
                                    MaxHeight = 460
                                };
            sp.Children.Add(image);
            sp.Children.Add(GetImageHelpTip());

            image.SizeChanged += image_SizeChanged;

            image.DoubleTap += ShowImagePopup;

            var save = new ContextMenu();
            var saveMenu = new MenuItem
                               {
                                   Header = "save to library",
                                   Tag = image,
                               };
            saveMenu.Click += saveImage_Click;

            save.Items.Add(saveMenu);
            image.SetValue(ContextMenuService.ContextMenuProperty, save);

            var scrollViewer = new ScrollViewer
            {
                Content = sp
            };
            pivotItem.Content = scrollViewer;

            pivotMain.Items.Insert(1, pivotItem);

            if (IsPhoto)
            {
                SetPhotoPivot();
            }
        }

        private UIElement GetImageHelpTip()
        {
            return new ImageHelpTip();
        }

        private void ShowImagePopup(object sender, GestureEventArgs e)
        {

            UiHelper.SafeDispatch(() =>
                                      {
                                          var imageSource = sender as Image;

                                          if (imageSource == null || imageSource.Source == null)
                                              return;

                                          ApplicationBar.IsVisible = false;

                                          var panImage = new PanScanImage()
                                                             {
                                                                 Source = imageSource.Source
                                                             };

                                          ImagePopup = new DialogService
                                          {
                                              AnimationType = DialogService.AnimationTypes.Fade,
                                              Child = panImage
                                          };

                                          ImagePopup.Show();

                                      });
        }

        private void HideImagePopup()
        {
            UiHelper.SafeDispatch(() =>
            {
                if (ImagePopup != null)
                {
                    EventHandler selectAccountPopupClosed = (sender, args) =>
                    {
                        ImagePopup = null;
                        ApplicationBar.IsVisible = true;
                    };
                    ImagePopup.Closed += selectAccountPopupClosed;
                    ImagePopup.Hide();
                }
            });
        }

        private void GetBrowser(string longValue, string shortValue)
        {

            string targetUrl = shortValue;

            if (!string.IsNullOrEmpty(longValue))
                targetUrl = longValue;

            if (targetUrl.ToLower().EndsWith(".jpg") || targetUrl.ToLower().EndsWith(".png") ||
                     targetUrl.ToLower().EndsWith(".jpeg"))
            {
                GetImagePivotForSimpleUrl(targetUrl);
            }
            else if (targetUrl.ToLower().Contains("d.pr/i/"))
            {
                var newUrl = string.Format("{0}+", targetUrl);
                GetImagePivotForSimpleUrl(newUrl);
            }
            else if (targetUrl.ToLower().Contains("youtube.com") || targetUrl.ToLower().Contains("youtu.be"))
            {
                var pivotItem = new PivotItem
                                    {
                                        Header = "video"
                                    };

                if (LicenceInfo.IsTrial())
                {
                    pivotItem.Content = GetTrialPanel("video");
                }
                else
                {
                    var sp = GetYoutubeContent(targetUrl);

                    if (sp == null)
                        return;

                    var scrollViewer = new ScrollViewer
                                           {
                                               Content = sp
                                           };
                    pivotItem.Content = scrollViewer;
                }

                pivotMain.Items.Insert(1, pivotItem);
            }
            else if (targetUrl.ToLower().Contains("imgur.com") && !targetUrl.ToLower().EndsWith(".gif"))
            {
                SetParsedImageContainer(targetUrl, (sender, e) => SetParsedImage(e, new ImgurParser()));
            }
            else if (targetUrl.ToLower().Contains("img.ly"))
            {
                if (targetUrl.Contains("/"))
                {
                    try
                    {
                        var splitUrl = targetUrl.Split('/');
                        var id = splitUrl.Last();
                        var newUrl = string.Format("http://img.ly/show/large/{0}", id);
                        GetImagePivotForSimpleUrl(newUrl);
                    }
                    catch
                    {
                    }
                }
            }
            else if (targetUrl.ToLower().Contains("instagr.am/p/") || targetUrl.ToLower().Contains("instagram.com/p/"))
            {
                string newUrl = targetUrl;

                if (!newUrl.EndsWith("/"))
                    newUrl += "/";
                newUrl += "media?size=l";

                GetImagePivotForSimpleUrl(newUrl);
            }
            else if (targetUrl.ToLower().Contains("sdrv.ms") || targetUrl.ToLower().Contains("1drv.ms"))
            {
                var newUrl = "https://apis.live.net/v5.0/skydrive/get_item_preview?url=" + targetUrl;
                GetImagePivotForSimpleUrl(newUrl);
            }
            else if (targetUrl.ToLower().Contains("photoplay.net/photos/"))
            {
                SetParsedImageContainer(targetUrl, (sender, e) => SetParsedImage(e, new PhotoplayParser()));
            }
            else if (targetUrl.ToLower().Contains("picplz.com"))
            {
                SetParsedImageContainer(targetUrl, (sender, e) => SetParsedImage(e, new PicPlzParser()));
            }
            else if (targetUrl.ToLower().Contains("lockerz.com"))
            {
                var newUrl = "http://api.plixi.com/api/tpapi.svc/imagefromurl?size=medium&url=" + targetUrl;
                GetImagePivotForSimpleUrl(newUrl);
            }
            else if (targetUrl.ToLower().Contains("twitgoo.com"))
            {
                SetParsedImageContainer(targetUrl, (sender, e) => SetParsedImage(e, new TwitgooParser()));
            }
            else if (targetUrl.ToLower().Contains("flic.kr/p/") || targetUrl.ToLower().Contains("flickr.com/photos/"))
            {
                SetParsedImageContainer(targetUrl, (sender, e) => SetParsedImage(e, new FlickrPhotoParser()));
            }
            else if (targetUrl.ToLower().Contains("vine.co/v/"))
            {
                SetParsedVineContainer(targetUrl, (sender, e) => SetParsedImage(e, new VineParser()));
            }
            else if (targetUrl.ToLower().Contains("mobypicture.com") || targetUrl.ToLower().Contains("moby.to"))
            {
                SetParsedImageContainer(targetUrl, (sender, e) => SetParsedImage(e, new MobyPictureParser()));
            }
            else if (targetUrl.ToLower().Contains("500px.com/photo"))
            {
                SetParsedImageContainer(targetUrl,
                                        (sender, e) => SetParsedImage(e, new FiveHundredPxPictureParser()));
            }
            else if (targetUrl.ToLower().Contains("eyeem.com/p"))
            {
                // http://www.eyeem.com/p/957852
                SetParsedImageContainer(targetUrl, (sender, e) => SetParsedImage(e, new EyeEmParser()));
            }
            else if (targetUrl.ToLower().Contains("2instawithlove.com/p/"))
            {
                SetParsedImageContainer(targetUrl, (sender, e) => SetParsedImage(e, new ToInstaWithLoveParser()));
            }
            else if (targetUrl.ToLower().Contains("molo.me") || targetUrl.ToLower().Contains("molome.com"))
            {
                if (targetUrl.Contains("/"))
                {
                    try
                    {
                        var splitUrl = targetUrl.Split('/');
                        var id = splitUrl.Last();
                        var newUrl = string.Format("http://p480x480.molo.me/{0}_480x480", id);
                        GetImagePivotForSimpleUrl(newUrl);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else if (targetUrl.ToLower().Contains("twitpic.com"))
            {
                if (targetUrl.Contains("/"))
                {
                    try
                    {
                        var imageUri = new Uri(targetUrl, UriKind.Absolute);
                        string filePath = imageUri.AbsolutePath;

                        if (!filePath.StartsWith("/"))
                            filePath = "/" + filePath;

                        var newPath = string.Format("http://twitpic.com/show/full/{0}", filePath);

                        GetImagePivotForSimpleUrl(newPath);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else if (targetUrl.ToLower().Contains("yfrog.com"))
            {
                targetUrl += ":iphone"; // can also use :small :medium :frame :iphone
                GetImagePivotForSimpleUrl(targetUrl);
            }
            else if (targetUrl.ToLower().Contains("windowsphone.com/s?appid="))
            {
                GetMarketplaceLink(targetUrl);
            }
            else if (targetUrl.ToLower().Contains("windowsphone.com/") && targetUrl.ToLower().Contains("/store/app/"))
            {
                GetMarketplaceLink(targetUrl);
            }
            else
            {
                var pivotItem = new PivotItem
                                    {
                                        Header = "link",
                                    };

                if (LicenceInfo.IsTrial())
                {
                    pivotItem.Content = GetTrialPanel("web page");
                }
                else
                {
                    var newUrl = !string.IsNullOrEmpty(longValue) ? longValue : shortValue;
                    if (!string.IsNullOrWhiteSpace(newUrl))
                    {
                        if (!newUrl.ToLower().StartsWith("http://") &&
                            !newUrl.ToLower().StartsWith("https://"))
                        {
                            newUrl = "http://" + newUrl;
                        }
                    }

                    var newGrid = new Grid();

                    newGrid.ColumnDefinitions.Add(new ColumnDefinition
                                                      {
                                                          Width = new GridLength(10)
                                                      });

                    newGrid.ColumnDefinitions.Add(new ColumnDefinition
                                                      {
                                                          Width = new GridLength(1, GridUnitType.Star)
                                                      });

                    newGrid.ColumnDefinitions.Add(new ColumnDefinition
                                                      {
                                                          Width = new GridLength(10)
                                                      });

                    newGrid.RowDefinitions.Add(new RowDefinition()
                                                        {
                                                            Height = new GridLength(1, GridUnitType.Star)
                                                        });

                    var browser = new WebBrowser
                    {
                        IsScriptEnabled = true, 
                        //RenderTransform = new CompositeTransform(), 
                        //RenderTransformOrigin = new Point(0.5, 0.5), 
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Name = "browser"
                    };

                    browser.SetValue(Grid.ColumnProperty, 1);

                    // this for READABILITY
                    //var readabilityUrl = "https://www.readability.com/api/content/v1/parser?url=" + newUrl +
                    //                     "&token=c1bed8b6496131fafdf357d287e830966d8ff4c8";

                    //WebClient wc = new WebClient();
                    //wc.DownloadStringCompleted += delegate(object sender, DownloadStringCompletedEventArgs args)
                    //{
                    //    try
                    //    {
                    //        var value = args.Result;
                    //        var res = JsonConvert.DeserializeObject<ReadabilityResult>(value);
                    //        browser.NavigateToString(res.content);
                    //    }
                    //    catch (Exception)
                    //    {
                    //        browser.Navigate(new Uri(newUrl, UriKind.Absolute));
                    //    }
                    //};

                    //wc.DownloadStringAsync(new Uri(readabilityUrl, UriKind.Absolute));


                    // or this for GOOGLE
                    //var mobileUrl = "http://www.google.com/gwt/x?u=" + HttpUtility.UrlEncode(newUrl) + "&ie=UTF-8&oe=UTF-8";

                    browser.Navigate(new Uri(newUrl, UriKind.Absolute));

                    newGrid.Children.Add(browser);

                    //browser.Height = newGrid.Height;    

                    if (ResolutionHelper.CurrentResolution == Resolutions.WVGA ||
                        ResolutionHelper.CurrentResolution == Resolutions.WXGA)
                        browser.Height = 480;
                    else
                        browser.Height = 510;

                    browser.Navigated += browser_Navigated;

                    var sp = new StackPanel();

                    if (!IsPhoto)
                    {
                        var pb = new PerformanceProgressBar
                                     {
                                         IsIndeterminate = true,
                                         IsEnabled = true
                                     };
                        sp.Children.Add(pb);
                    }

                    sp.Children.Add(newGrid);

                    pivotItem.Content = sp;
                }

                try
                {
                    pivotMain.Items.Insert(1, pivotItem);
                }
                catch (Exception)
                {
                }
            }

            if (IsPhoto)
            {
                SetPhotoPivot();
            }
        }

        private UIElement GetYoutubeContent(string targetUrl)
        {

            string newUrl = targetUrl.GetYoutubeVideoIdFromUrl();

            if (string.IsNullOrEmpty(newUrl))
                return null;

            const int CanvasWidth = 460;
            const int CanvasHeight = 460;

            var canvas = new Canvas { Width = CanvasWidth, Height = CanvasHeight };

            string thumbNail;

            thumbNail = string.Format("http://img.youtube.com/vi/{0}/hqdefault.jpg", newUrl);

            var sourceImage = new BitmapImage(new Uri(thumbNail, UriKind.Absolute));

#if WP8
            sourceImage.DecodePixelWidth = CanvasWidth;
#endif

            var image = new Image
            {
                Source = sourceImage,
                MaxWidth = CanvasWidth,
                Stretch = Stretch.UniformToFill,
                VerticalAlignment = VerticalAlignment.Top
            };

            Canvas.SetLeft(image, 0);
            Canvas.SetTop(image, 0);

            canvas.Children.Add(image);

            // now the grey'd area
            var rect = new Rectangle
            {
                Width = CanvasWidth,
                Height = CanvasHeight,
                Fill = new SolidColorBrush(Colors.Black),
                Opacity = 0.3
            };

            Canvas.SetLeft(rect, 0);
            Canvas.SetTop(rect, 0);

            canvas.Children.Add(rect);

            // now the button
            var playImageUri = new Uri("/images/bigplay.png", UriKind.Relative);
            var playImageBitmap = new BitmapImage(playImageUri);
            var playImage = new Image { Source = playImageBitmap };

            Canvas.SetLeft(playImage, 176);
            Canvas.SetTop(playImage, 110);

            canvas.Children.Add(playImage);

#if WP8

            var border = new Border()
                             {
                                 Width = CanvasWidth,
                                 Height = 90,
                                 Opacity = 0.6,
                                 Background = Resources["PhoneAccentBrush"] as SolidColorBrush
                             };
            Canvas.SetLeft(border, 0);
            Canvas.SetTop(border, CanvasWidth - 90);

            canvas.Children.Add(border);

            var textBlock = new TextBlock()
                                {
                                    FontSize = 22,
                                    Width = 400,
                                    Foreground = new SolidColorBrush(Colors.White),
                                    Text = "tip: tap here to change the app used to play videos",
                                    TextWrapping = TextWrapping.Wrap
                                };

            Canvas.SetLeft(textBlock, 10);
            Canvas.SetTop(textBlock, CanvasWidth - 90 + 10);

            textBlock.Tap += (sender, args) => UiHelper.SafeDispatch(() => NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_General.xaml", UriKind.Relative)));

            canvas.Children.Add(textBlock);

            var sourceGeneralImage = new BitmapImage(new Uri("/Images/settings/Settings.png", UriKind.Relative));

            var settingsImage = new Image
            {
                Source = sourceGeneralImage,
                Width = 60,
                Height = 60,
                Stretch = Stretch.UniformToFill
            };

            Canvas.SetLeft(settingsImage, CanvasWidth - 60);
            Canvas.SetTop(settingsImage, CanvasHeight - 90 + 10);

            settingsImage.Tap += (sender, args) => UiHelper.SafeDispatch(() => NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_General.xaml", UriKind.Relative)));

            canvas.Children.Add(settingsImage);

#endif

            playImage.Tap += (sender, e) =>
            {

#if WP8

                var sh = new SettingsHelper();
                var player = sh.GetVideoPlayerApp();

                switch (player)
                {
                    case ApplicationConstants.VideoPlayerAppEnum.Default:
                        LaunchYoutubeVideoInternal(newUrl);

                        //var url = "http://www.youtube.com/embed/" + newUrl;
                        //var launchTask = new WebBrowserTask
                        //{
                        //    Uri = new Uri(url, UriKind.Absolute)
                        //};
                        //launchTask.Show();
                        break;

                    case ApplicationConstants.VideoPlayerAppEnum.Metrotube:
                        UiHelper.SafeDispatch(async () => await Windows.System.Launcher.LaunchUriAsync(new System.Uri("metrotube:VideoPage?VideoID=" + newUrl)));
                        break;

                    case ApplicationConstants.VideoPlayerAppEnum.OfficialYoutube:
                        UiHelper.SafeDispatch(async () => await Windows.System.Launcher.LaunchUriAsync(new System.Uri("vnd.youtube:" + newUrl)));
                        break;
                }

#elif WP7

                //var url = "http://www.youtube.com/embed/" + newUrl;
                //var launchTask = new WebBrowserTask
                //{
                //    Uri = new Uri(url, UriKind.Absolute)
                //};
                //launchTask.Show();
                LaunchYoutubeVideoInternal(newUrl);

#endif

            };

            return canvas;

        }

        private void LaunchYoutubeVideoInternal(string newUrl)
        {
            YouTube.Play(newUrl, true, YouTubeQuality.Quality480P, (e) =>
            {
                if (e != null)
                {
                    MessageBox.Show(e.Message);
                }
            });
        }

        private void GetImagePivotForSimpleUrl(string targetUrl)
        {
            var pivotItem = new PivotItem
                                {
                                    Header = "image"
                                };

            if (LicenceInfo.IsTrial())
            {
                pivotItem.Content = GetTrialPanel("image");
            }
            else
            {
                var sourceImage = new BitmapImage(new Uri(targetUrl, UriKind.RelativeOrAbsolute));

                var image = new Image
                                {
                                    Source = sourceImage,
                                    MaxHeight = 460
                                };

                var menu = new ContextMenu();
                var saveMenu = new MenuItem
                                   {
                                       Header = "save to library",
                                       Tag = sourceImage,
                                   };
                saveMenu.Click += saveImage_Click;
                menu.Items.Add(saveMenu);
                image.SetValue(ContextMenuService.ContextMenuProperty, menu);

                image.SizeChanged += image_SizeChanged;

                image.DoubleTap += ShowImagePopup;

                var sp = new StackPanel();

                if (!IsPhoto)
                {
                    var pb = new PerformanceProgressBar
                                 {
                                     IsIndeterminate = true,
                                     IsEnabled = true
                                 };
                    sp.Children.Add(pb);
                }

                sp.Children.Add(image);
                sp.Children.Add(GetImageHelpTip());

                var scrollViewer = new ScrollViewer
                                       {
                                           Content = sp
                                       };
                pivotItem.Content = scrollViewer;
            }

            pivotMain.Items.Insert(1, pivotItem);
        }

        private void SetPhotoPivot()
        {
            try
            {
                var t = pivotMain.Items.FirstOrDefault(x => (string)((PivotItem)x).Header == "image");
                if (t != null)
                {
                    pivotMain.SelectedItem = t;
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private void saveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var menu = sender as MenuItem;
                //var image = menu.Tag as Image;
                if (menu != null)
                {
                    var newFileName = "mehdoh_twitter_" + Guid.NewGuid().ToString().Replace("-", "") + ".jpg";

                    if (menu.Tag is BitmapImage)
                    {
                        var source = menu.Tag as BitmapImage;
                        var tempJpeg = ConvertToBytes(source);
                        var library = new MediaLibrary();
                        library.SavePicture(newFileName, tempJpeg);
                    }
                    else if (menu.Tag is Image)
                    {
                        var image = menu.Tag as Image;
                        var source = image.Source as BitmapImage;
                        var tempJpeg = ConvertToBytes(source);
                        var library = new MediaLibrary();
                        library.SavePicture(newFileName, tempJpeg);
                    }

                    UiHelper.ShowToast("image has been saved to your library!");
                }
                else
                {
                    UiHelper.ShowToast("sorry. something went wrong saving the image");
                }
            }
            catch (Exception)
            {
                UiHelper.ShowToast("sorry. something went wrong saving the image");
            }
        }

        public static byte[] ConvertToBytes(WriteableBitmap bitmapImage)
        {
            using (var ms = new MemoryStream())
            {
                // write an image into the stream
                bitmapImage.SaveJpeg(ms, bitmapImage.PixelWidth, bitmapImage.PixelHeight, 0, 100);

                return ms.ToArray();
            }
        }


        public static byte[] ConvertToBytes(BitmapImage bitmapImage)
        {
            using (var ms = new MemoryStream())
            {
                var btmMap = new WriteableBitmap(bitmapImage);

                // write an image into the stream
                btmMap.SaveJpeg(ms, bitmapImage.PixelWidth, bitmapImage.PixelHeight, 0, 100);

                return ms.ToArray();
            }
        }

        private void SetParsedVineContainer(string targetUrl, OpenReadCompletedEventHandler eventHandler)
        {
            var pivotItem = new PivotItem
                                {
                                    Header = "vine"
                                };

            if (LicenceInfo.IsTrial())
            {
                pivotItem.Content = GetTrialPanel("vine");
            }
            else
            {
                var canvas = new Canvas { Width = 460, Height = 460 };

                var sourceImage = new BitmapImage();

                var image = new Image
                                {
                                    Source = sourceImage,
                                    MaxHeight = 460,
                                };

                Canvas.SetLeft(image, 0);
                Canvas.SetTop(image, 0);

                canvas.Children.Add(image);

                // now the grey'd area
                var rect = new Rectangle
                               {
                                   Width = 480,
                                   Height = 480,
                                   Fill = new SolidColorBrush(Colors.Black),
                                   Opacity = 0.5
                               };

                Canvas.SetLeft(rect, 0);
                Canvas.SetTop(rect, 0);

                canvas.Children.Add(rect);

                // now the button
                var playImageUri = new Uri("/images/bigplay.png", UriKind.Relative);
                var playImageBitmap = new BitmapImage(playImageUri);
                var playImage = new Image { Source = playImageBitmap };

                Canvas.SetLeft(playImage, 176);
                Canvas.SetTop(playImage, 176);

                canvas.Children.Add(playImage);

                var player = new MediaElement
                                 {
                                     Width = 480,
                                     Height = 480
                                 };

                Canvas.SetLeft(player, 0);
                Canvas.SetTop(player, 0);
                Canvas.SetZIndex(player, -1);

                playImage.Visibility = Visibility.Visible;
                player.Visibility = Visibility.Collapsed;

                playImage.Tap += delegate
                                     {
                                         playImage.Visibility = Visibility.Collapsed;
                                         player.Visibility = Visibility.Visible;

                                         player.Source = VineVideoSourceUri;
                                         player.MediaEnded += delegate
                                                                  {
                                                                      playImage.Visibility = Visibility.Visible;
                                                                      player.Visibility = Visibility.Collapsed;
                                                                      Canvas.SetZIndex(player, -1);
                                                                  };
                                         Canvas.SetZIndex(player, 100);
                                         player.Play();
                                     };

                canvas.Children.Add(player);

                var sourcePageUri = new Uri(targetUrl, UriKind.RelativeOrAbsolute);

                var client = new WebClient();
                client.OpenReadCompleted += eventHandler;
                client.OpenReadAsync(sourcePageUri, image);

                var scrollViewer = new ScrollViewer
                                       {
                                           Content = canvas
                                       };

                pivotItem.Content = scrollViewer;
            }

            pivotMain.Items.Insert(1, pivotItem);
        }

        private void SetParsedImageContainer(string targetUrl, OpenReadCompletedEventHandler eventHandler)
        {
            var pivotItem = new PivotItem
                                {
                                    Header = "image"
                                };

            if (LicenceInfo.IsTrial())
            {
                pivotItem.Content = GetTrialPanel("image");
            }
            else
            {

                var imageUri = new Uri(targetUrl, UriKind.RelativeOrAbsolute);
                var sourceImage = new BitmapImage();

                var image = new Image
                                {
                                    Source = sourceImage,
                                    MaxHeight = 460
                                };

                image.SizeChanged += image_SizeChanged;

                image.DoubleTap += ShowImagePopup;

                var sp = new StackPanel();

                if (!IsPhoto)
                {
                    var pb = new PerformanceProgressBar
                                 {
                                     IsIndeterminate = true,
                                     IsEnabled = true
                                 };
                    sp.Children.Add(pb);
                }

                sp.Children.Add(image);

                var client = new WebClient();
                client.OpenReadCompleted += eventHandler;
                client.OpenReadAsync(imageUri, image);

                var scrollViewer = new ScrollViewer
                                       {
                                           Content = sp
                                       };
                pivotItem.Content = scrollViewer;
            }

            pivotMain.Items.Insert(1, pivotItem);
        }

        private void SetParsedImage(OpenReadCompletedEventArgs e, IImageHostParser parser)
        {

            var imageControl = e.UserState as Image;
            if (imageControl == null)
                return;

            try
            {
                var resInfo = new StreamResourceInfo(e.Result, null);
                using (var reader = new StreamReader(resInfo.Stream))
                {
                    using (var bReader = new BinaryReader(reader.BaseStream))
                    {
                        var contents = bReader.ReadBytes((int)reader.BaseStream.Length);
                        var fileContents = System.Text.Encoding.UTF8.GetString(contents, 0, contents.Length);
                        var newUrl = parser.GetImageUrl(fileContents);

                        var vineParser = parser as VineParser;
                        if (vineParser != null)
                        {
                            VineVideoSourceUri = vineParser.VineVideoUri;
                        }

                        if (!string.IsNullOrEmpty(newUrl))
                        {
                            BitmapImage sourceImage;

                            // is it cached?
                            var res = IsImageCached(newUrl);
                            if (res != null)
                            {
                                sourceImage = GetImageFromLocalCache(res);

                                var sp = imageControl.Parent as StackPanel;
                                if (sp != null)
                                {
                                    var pg = sp.Children.OfType<PerformanceProgressBar>();
                                    foreach (var p in pg)
                                    {
                                        p.IsEnabled = false;
                                        p.IsIndeterminate = false;
                                    }
                                }
                            }
                            else
                            {
                                sourceImage = new BitmapImage(new Uri(newUrl, UriKind.Absolute));
                            }

                            imageControl.Source = sourceImage;
                            var menu = new ContextMenu();
                            var saveMenu = new MenuItem
                                               {
                                                   Header = "save to library",
                                                   Tag = sourceImage,
                                               };
                            saveMenu.Click += saveImage_Click;
                            menu.Items.Add(saveMenu);
                            imageControl.SetValue(ContextMenuService.ContextMenuProperty, menu);
                        }
                        else
                        {
                            imageControl.Source = new BitmapImage(new Uri("/Images/wentwrong.png", UriKind.Relative));
                        }
                    }
                }
            }
            catch (Exception)
            {
                var sp = imageControl.Parent as StackPanel;
                if (sp != null)
                {
                    var pg = sp.Children.OfType<PerformanceProgressBar>();
                    foreach (var p in pg)
                    {
                        p.IsEnabled = false;
                        p.IsIndeterminate = false;
                    }
                }

                imageControl.Source = new BitmapImage(new Uri("/Images/wentwrong.png", UriKind.Relative));
            }
        }

        private BitmapImage GetImageFromLocalCache(Uri res)
        {
            // byte[] data;

            using (var mystore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var fs = mystore.OpenFile(res.ToString(), FileMode.Open))
                {
                    var bitmap = new BitmapImage();
                    bitmap.SetSource(fs);
                    //data = new byte[fs.Length];
                    //fs.Read(data, 0, (Int32)fs.Length);
                    return bitmap;
                }
            }


            //using (var memStream = new MemoryStream(data))
            //{
            //    bitmap.SetSource(memStream);
            //}

            //return bitmap;
        }

        private Uri IsImageCached(string newUrl)
        {

            try
            {
                using (var dh = new MainDataContext())
                {
                    var res = dh.ThumbnailCache.FirstOrDefault(x => x.LongUrl.ToLower() == newUrl.ToLower());
                    if (res != null)
                    {
                        var largeImage = System.IO.Path.ChangeExtension(res.LocalUri, ".large" + System.IO.Path.GetExtension(res.LocalUri));

                        using (var mystore = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (mystore.FileExists(largeImage))
                                return new Uri(largeImage, UriKind.Relative);
                        }

                        return new Uri(res.LocalUri, UriKind.Relative);

                    }
                }

                return null;

            }
            catch
            {
                return null;
            }

        }

        private void browser_Navigated(object sender, NavigationEventArgs e)
        {
            var img = sender as WebBrowser;

            img.Navigated -= browser_Navigated;

            if (img != null)
            {
                var grid = img.Parent as Grid;
                var sp = grid.Parent as StackPanel;
                var pg = sp.Children.OfType<PerformanceProgressBar>();
                foreach (var p in pg)
                {
                    p.IsEnabled = false;
                    p.IsIndeterminate = false;
                }
            }
        }

        private void image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsPhoto)
                return;

            if (sender is Image)
            {
                var img = sender as Image;
                var sp = img.Parent as StackPanel;
                var pg = sp.Children.OfType<PerformanceProgressBar>();
                foreach (var p in pg)
                {
                    p.IsEnabled = false;
                    p.IsIndeterminate = false;
                }
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
                                MaxHeight = 280,
                                MaxWidth = 280,
                                Margin = new Thickness(15, 0, 0, 0)
                            };

            stackPanel.Children.Add(image);

            var button = new Button
                             {
                                 Content = "view in store",
                                 Width = 280,
                                 HorizontalAlignment = HorizontalAlignment.Left,
                                 Margin = new Thickness(15, 20, 50, 0)
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

            pivotMain.Items.Add(pivotItem);


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

        private void SetExtraPanels()
        {

            try
            {

                if (ViewModel.Assets != null && ViewModel.Assets.Any())
                {
                    if (((IMehdohApp)Application.Current).DisplayLinks)
                    {

                        foreach (var item in ViewModel.Assets.Where(x => x.Type == AssetTypeEnum.Url))
                        {
                            // idiots at twitter duplicate media and url, so ignore url if we have the same media
                            if (!ViewModel.Assets.Any(x => x.StartOffset == item.StartOffset && x.EndOffset == item.EndOffset && x.Type == AssetTypeEnum.Media))
                            {
                                GetBrowser(item.LongValue, item.ShortValue);
                            }
                        }

                        foreach (var item in ViewModel.Assets.Where(x => x.Type == AssetTypeEnum.Media))
                        {
                            GetMedia(item.LongValue, item.ShortValue);
                        }
                    }
                }

                // Any map info?
                GetMap();

            }
            catch (Exception)
            {
                // not the end of the world
            }

        }

        private void ConstructWrapPanel(string description, IEnumerable<AssetViewModel> assets)
        {
            wrapText.Blocks.Clear();

            var fr = new FontResources();

            if (assets == null)
            {
                var singleParagraph = new Paragraph();
                singleParagraph.FontSize = fr.FontSizeNormalDetails;
                singleParagraph.Inlines.Add(HttpUtility.HtmlDecode(description));
                wrapText.Blocks.Add(singleParagraph);
                wrapText.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrEmpty(description))
            {
                var singleParagraph = new Paragraph();
                singleParagraph.FontSize = fr.FontSizeNormalDetails;
                singleParagraph.Inlines.Add(HttpUtility.HtmlDecode("Oops! Something went wrong as the text for this tweet is empty!"));
                wrapText.Blocks.Add(singleParagraph);
                wrapText.Visibility = Visibility.Visible;
                return;
            }

            //description = HttpUtility.HtmlDecode(description);

            var currentOffset = 0;

            var myParagraph = new Paragraph();

            myParagraph.FontSize = fr.FontSizeNormalDetails;

            foreach (var currentItem in assets.OrderBy(x => x.StartOffset))
            {
                var nextOffset = currentItem.StartOffset;
                if (nextOffset < currentOffset)
                    nextOffset = currentOffset;

                var length = nextOffset - currentOffset;
                var desc = description.Substring(currentOffset, length);

                if (!string.IsNullOrEmpty(desc))
                    myParagraph.Inlines.Add(HttpUtility.HtmlDecode(desc));

                var button2 = new Hyperlink
                                  {
                                      FontWeight = FontWeights.Bold,
                                      CommandParameter = currentItem,
                                      Foreground = Resources["PhoneAccentBrush"] as Brush,
                                      TextDecorations = null,
                                      MouseOverTextDecorations = null,
                                      MouseOverForeground = Resources["PhoneAccentBrush"] as Brush
                                  };

                button2.Click += button2_Click;

                string linkValue;

                if (!string.IsNullOrEmpty(currentItem.LongValue) && currentItem.Type == AssetTypeEnum.Url)
                    linkValue = currentItem.LongValue;
                else
                    linkValue = currentItem.ShortValue;

                if (currentItem.Type == AssetTypeEnum.Media)
                    linkValue = currentItem.ShortValue;

                if (currentItem.Type == AssetTypeEnum.Mention && !linkValue.StartsWith("@"))
                    linkValue = "@" + linkValue;

                if (currentItem.Type == AssetTypeEnum.Hashtag && !linkValue.StartsWith("#"))
                    linkValue = "#" + linkValue;

                button2.Inlines.Add(linkValue);

                myParagraph.Inlines.Add(button2);

                currentOffset = currentItem.EndOffset;
            }

            if (currentOffset < description.Length)
            {
                var desc = description.Substring(currentOffset);
                if (!string.IsNullOrEmpty(desc))
                    myParagraph.Inlines.Add(HttpUtility.HtmlDecode(desc));
            }

            wrapText.Blocks.Add(myParagraph);

            wrapText.Visibility = Visibility.Visible;

        }

        private void bing_TranslateTextCompleted(object sender, EventArgs e)
        {
            var api = sender as BingTranslateApi;
            var text = api.TranslatedText;

            UiHelper.HideProgressBar();
            UiHelper.SafeDispatch(() => RotateDescription(text));
        }

        private void RotateDescription(string text)
        {
            _rotatingText = text;

            const double duration = 0.5;

            var sb = new Storyboard
                         {
                             BeginTime = new TimeSpan(0)
                         };

            sb.Children.Add(CreateAnimation(1, 0, duration, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleX)"), wrapText));
            sb.Children.Add(CreateAnimation(1, 0, duration, new PropertyPath(OpacityProperty), wrapText));

            sb.Duration = new Duration(TimeSpan.FromSeconds(duration));
            sb.Completed += rotateText_Completed;
            sb.Begin();
        }

        private void rotateText_Completed(object sender, EventArgs e)
        {
            const double duration = 0.5;

            var sb = new Storyboard
                         {
                             BeginTime = new TimeSpan(0)
                         };

            ConstructWrapPanel(_rotatingText, null);

            sb.Children.Add(CreateAnimation(0, 1, duration, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleX)"), wrapText));
            sb.Children.Add(CreateAnimation(0, 1, duration, new PropertyPath(OpacityProperty), wrapText));
            sb.Duration = new Duration(TimeSpan.FromSeconds(duration));
            sb.Completed += rotateText2_Completed;
            sb.Begin();
        }

        private void rotateText2_Completed(object sender, EventArgs e)
        {
            UiHelper.ShowToast("tip", "double tap translated text for original");
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Hyperlink;
            var asset = button.CommandParameter as AssetViewModel;

            switch (asset.Type)
            {
                case AssetTypeEnum.Url:
                case AssetTypeEnum.Media:

                    var targetValue = (string.IsNullOrEmpty(asset.LongValue)
                       ? asset.ShortValue
                       : asset.LongValue);

                    var tweetRegex = new Regex(@"^(https|http)://(www.)?twitter\.com/(?<UserName>.*)/status/(?<StatusId>[0-9]*)$", RegexOptions.IgnoreCase);
                    var tweetResults = tweetRegex.Match(targetValue);
                    var tweetId = tweetResults.Groups["StatusId"].Value;

                    var sh = new SettingsHelper();
                    if (!sh.GetShowSaveLinksEnabled())
                    {
                        var mnuClipboard = new RadContextMenuItem
                                               {
                                                   Content = "copy to clipboard"
                                               };

                        mnuClipboard.Tap += delegate
                        {
                            _isMenuOpen = false;
                            _menu.IsOpen = false;
                            Clipboard.SetText(targetValue);
                            UiHelper.ShowToast("copied to clipboard");
                        };


                        var mnuOpenTwitter = new RadContextMenuItem()
                                                 {
                                                     Content = "open tweet in mehdoh"
                                                 };

                        mnuOpenTwitter.Tap += delegate(object o, GestureEventArgs args)
                                                  {
                                                      _isMenuOpen = false;
                                                      _menu.IsOpen = false;

                                                      var newUri = new Uri("/DetailsPage.xaml?accountId=" + AccountId + "&id=" + tweetId, UriKind.Relative);
                                                      NavigationService.Navigate(newUri);
                                                  };

                        var mnuOpen = new RadContextMenuItem
                                          {
                                              Content = "open in browser"
                                          };

                        mnuOpen.Tap += delegate
                                             {
                                                 _isMenuOpen = false;
                                                 _menu.IsOpen = false;

                                                 // is it a tweet?
                                                 var task = new WebBrowserTask
                                                 {
                                                     Uri = new Uri(targetValue, UriKind.Absolute)
                                                 };
                                                 task.Show();

                                             };


                        var mnuSave = new RadContextMenuItem
                                          {
                                              Content = "save for later"
                                          };
                        //mnuSave.Click += mnuSaveTweet_Click;
                        mnuSave.Tap += delegate
                                             {
                                                 _isMenuOpen = false;
                                                 _menu.IsOpen = false;

                                                 var slh = new SaveLaterHelper();
                                                 slh.SaveFinishedEvent += slh_SaveFinishedEvent;

                                                 UiHelper.ShowProgressBar("saving link for later");

                                                 var desc = GetDescription();

                                                 if (!slh.SaveUrl(targetValue, desc, 0))
                                                 {
                                                     UiHelper.HideProgressBar();
                                                     UiHelper.SafeDispatch(() =>
                                                                               {
                                                                                   if (MessageBox.Show(
                                                                                       "Instapaper and Pocket are not configured. Would you like to configure them now?",
                                                                                       "save for later",
                                                                                       MessageBoxButton.OKCancel) ==
                                                                                       MessageBoxResult.OK)
                                                                                   {
                                                                                       NavigationService.Navigate(new Uri("/SaveLater.xaml", UriKind.Relative));
                                                                                   }
                                                                               });
                                                 }
                                             };

                        var mnuSavePouch = new RadContextMenuItem
                                          {
                                              Content = "send to a pocket app"
                                          };
                        //mnuSave.Click += mnuSaveTweet_Click;
                        mnuSavePouch.Tap += async delegate
                                                {
                                                    _isMenuOpen = false;
                                                    _menu.IsOpen = false;

                                                    PocketHelper.AddItemToPocket(targetValue, callbackUri: "mehdoh:TweetDetails?Id=" + ViewModel.Id.ToString(CultureInfo.InvariantCulture));

                                                    //await Windows.System.Launcher.LaunchUriAsync(new Uri("Pouch:Add?Url=" + targetValue));
                                                };

                        var mnuShare = new RadContextMenuItem
                        {
                            Content = "share..."
                        };

                        mnuShare.Tap += delegate
                        {
                            _isMenuOpen = false;
                            _menu.IsOpen = false;

                            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();

                            dataTransferManager.DataRequested +=
                                delegate(DataTransferManager manager, DataRequestedEventArgs args)
                                {
                                    DataRequest request = args.Request;
                                    request.Data.Properties.Title = GetDescription();
                                    request.Data.Properties.Description = GetDescription();
                                    request.Data.SetWebLink(new Uri(targetValue, UriKind.Absolute));


                                };

                            DataTransferManager.ShowShareUI();
                        };


                        _menu = new RadContextMenu();
                        _menu.Items.Add(mnuShare);
                        _menu.Items.Add(mnuClipboard);
                        _menu.Items.Add(mnuSave);
                        _menu.Items.Add(mnuSavePouch);

                        if (!string.IsNullOrWhiteSpace(tweetId))
                        {
                            _menu.Items.Add(mnuOpenTwitter);
                        }

                        _menu.Items.Add(mnuOpen);


                        //if (targetValue.Contains("vine.co/v/"))
                        //{

                        //    var mnu6Sec = new RadContextMenuItem
                        //    {
                        //        Content = "open in 6sec"
                        //    };

                        //    mnu6Sec.Tap += delegate
                        //    {
                        //        _isMenuOpen = false;
                        //        _menu.IsOpen = false;

                        //        var targetId = targetValue.Substring(targetValue.LastIndexOf("/") + 1);

                        //        // is it a tweet?
                        //        Windows.System.Launcher.LaunchUriAsync(new Uri("vine://post/968633548745629696"));
                        //    };

                        //    _menu.Items.Add(mnu6Sec);

                        //}

                        _menu.IsFadeEnabled = true;
                        _menu.IsZoomEnabled = true;

                        _menu.Closed += delegate { _isMenuOpen = false; };

                        _isMenuOpen = true;

                        _menu.PortraitAlignment = VerticalAlignment.Bottom;
                        //button.SetValue(ContextMenuService.ContextMenuProperty, _menu);

                        //RadContextMenu.SetContextMenu(button, _menu);

                        _menu.IsOpen = true;
                    }
                    else
                    {
                        var task = new WebBrowserTask
                                       {
                                           Uri = new Uri(targetValue, UriKind.Absolute)
                                       };
                        task.Show();
                    }

                    break;

                case AssetTypeEnum.Mention:
                    NavigationService.Navigate(new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + asset.ShortValue, UriKind.Relative));
                    break;

                case AssetTypeEnum.Hashtag:
                    var resolvedAsset = asset.ShortValue.StartsWith("#") ? asset.ShortValue : "#" + asset.ShortValue.Trim();
                    NavigationService.Navigate(new Uri("/SearchResults.xaml?accountId=" + AccountId + "&term=" + System.Web.HttpUtility.UrlEncodeUnicode(resolvedAsset), UriKind.Relative));
                    break;
            }
        }

        private void SetDescription()
        {
            string description;

            if (ViewModel.IsRetweet)
                description = ViewModel.RetweetDescription;
            else
                description = ViewModel.Description;

            ConstructWrapPanel(description, ViewModel.Assets);
        }

        private void StartGetDMHistory(string screenName)
        {
            using (var dh = new MainDataContext())
            {
                var res = (from t in dh.Messages
                           where t.ScreenName == screenName
                           select t).Take(20);

                if (!res.Any()) return;

                foreach (var cachedStatus in res)
                {
                    var newItem = cachedStatus.AsViewModel(AccountId);

                    if (Timeline.All(x => x.Id != newItem.Id))
                        Timeline.Add(newItem);
                }
            }

            // Now get sender items
            //ThreadPool.QueueUserWorkItem(StartGetDMHistoryThread);
            StartGetDMHistoryThread(null);
        }

        private async void StartGetDMHistoryThread(object state)
        {
            var recipient = ViewModel.ScreenName;
            var sender = CurrentAccountScreenName;

            long maxId = 0;

            using (var dh = new MainDataContext())
            {
                var res =
                    dh.SentDirectMessages.Where(
                        x =>
                        string.Compare(x.RecipientScreenName, recipient, StringComparison.CurrentCultureIgnoreCase) == 0 &&
                        string.Compare(x.SenderScreenName, sender, StringComparison.CurrentCultureIgnoreCase) == 0);

                if (res.Any())
                {
                    foreach (var item in res)
                    {
                        var newView = new TimelineViewModel
                                          {
                                              ScreenName = item.SenderScreenName,
                                              DisplayName = item.SenderDisplayName,
                                              Description = HttpUtility.HtmlDecode(item.Text),
                                              CreatedAt = item.CreatedAt,
                                              ImageUrl = item.ProfileImageUrl,
                                              Id = item.Id,
                                              IsRetweet = false
                                          };

                        if (!Timeline.Any(x => x.Id == item.Id))
                            Timeline.Add(newView);
                    }

                    maxId = res.Max(x => x.Id);
                }
            }

            // Now grab any more
            var api = new TwitterApi(AccountId);
            //api.GetSentDirectMessagesCompletedEvent += api_GetSentDirectMessagesCompletedEvent;
            var result = await api.GetSentDirectMessages(maxId);
            api_GetSentDirectMessagesCompletedEvent(result);
        }

        private void api_GetSentDirectMessagesCompletedEvent(List<ResponseGetSentDirectMessage> sentDirectMessages)
        {

            if (sentDirectMessages == null || !sentDirectMessages.Any())
                return;

            UiHelper.SafeDispatch(() =>
                                      {
                                          try
                                          {
                                              var recipient = ViewModel.ScreenName;
                                              var sendee = CurrentAccountScreenName;

                                              // Lets see what we got.
                                              foreach (var item in sentDirectMessages.Where(x => string.Compare(x.recipient_screen_name, recipient, StringComparison.CurrentCultureIgnoreCase) == 0 &&
                                                                                                 string.Compare(x.sender_screen_name, sendee, StringComparison.CurrentCultureIgnoreCase) == 0))
                                              {

                                                  var newView = new TimelineViewModel
                                                                    {
                                                                        ScreenName = item.sender_screen_name,
                                                                        DisplayName = item.sender.name,
                                                                        Description = HttpUtility.HtmlDecode(item.text),
                                                                        CreatedAt = item.created_at,
                                                                        ImageUrl = item.sender.profile_image_url,
                                                                        Id = item.id,
                                                                        IsRetweet = false
                                                                    };

                                                  if (Timeline != null && Timeline.All(x => x.Id != item.id))
                                                      Timeline.Add(newView);
                                              }
                                          }
                                          catch (Exception ex)
                                          {
                                              ErrorLogger.LogException("api_GetSentDirectMessagesCompletedEvent", ex);
                                          }

                                      });


            try
            {
                using (var dh = new MainDataContext())
                {
                    foreach (var item in sentDirectMessages)
                    {
                        if (!dh.SentDirectMessages.Any(x => item.id == x.Id))
                        {
                            var newItem = new SentDirectMessageTable
                                              {
                                                  CreatedAt = item.created_at,
                                                  Id = item.id,
                                                  RecipientDisplayName = item.recipient.name,
                                                  RecipientScreenName = item.recipient.screen_name,
                                                  SenderDisplayName = item.sender.name,
                                                  SenderScreenName = item.sender.screen_name,
                                                  Text = item.text,
                                                  ProfileImageUrl = item.sender.profile_image_url
                                              };

                            dh.SentDirectMessages.InsertOnSubmit(newItem);
                        }
                    }

                    dh.SubmitChanges();
                }
            }
            catch (Exception)
            {
                // don't care, ultimately.
            }
        }


        private async void StartGetHistory(long startId)
        {
            HistoryRequired = true;
            HistoryReplyId = startId;

            ThreadPool.QueueUserWorkItem(async delegate
                                             {
                                                 var api = new TwitterApi(AccountId);

                                                 var cachedTweet = api.IsCachedTweet(startId);

                                                 if (cachedTweet != null)
                                                 {
                                                     await api_GetTweetCompletedEvent(AccountId, cachedTweet, null, false, string.Empty);
                                                 }
                                                 else
                                                 {
                                                     var tweet = await api.GetTweet(startId);
                                                     await api_GetTweetCompletedEvent(AccountId, null, tweet, api.HasError, api.ErrorMessage);
                                                 }

                                             });
        }


        private void pivotMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private async Task api_GetTweetCompletedEvent(long accountId, TimelineTable cachedStatus, ResponseTweet status, bool hasError, string errorMessage)
        {

            if (hasError)
            {
                if (!string.IsNullOrEmpty(errorMessage))
                    UiHelper.ShowToast(errorMessage);
                else
                    UiHelper.ShowToast("There was a problem connecting to Twitter.");
                return;
            }

            if ((status == null || status.id == 0) && (cachedStatus == null || cachedStatus.Id == 0))
                return;

            if (Timeline == null) // if timeline is null then we exited this page
                return;

            TimelineViewModel item;

            try
            {
                item = cachedStatus != null
                                             ? cachedStatus.AsViewModel(accountId)
                                             : status.AsViewModel(accountId);
            }
            catch
            {
                return;
            }

            if (Timeline.All(x => x.Id != item.Id))
            {
                UiHelper.SafeDispatch(() =>
                                          {
                                              try
                                              {
                                                  Timeline.Add(item);
                                              }
                                              catch
                                              {
                                              }
                                          }
                    );
            }

            try
            {
                var tempTimeline = new List<TimelineViewModel>();
                tempTimeline.AddRange(Timeline);

                if (cachedStatus != null && cachedStatus.InReplyToId.HasValue && cachedStatus.InReplyToId != 0)
                {
                    if (tempTimeline.All(x => x.Id != cachedStatus.InReplyToId))
                    {
                        var newapi = new TwitterApi(AccountId);

                        var cachedTweet = newapi.IsCachedTweet(cachedStatus.InReplyToId.Value);

                        if (cachedTweet != null)
                        {
                            await api_GetTweetCompletedEvent(AccountId, cachedTweet, null, false, string.Empty);
                        }
                        else
                        {
                            var tweet = await newapi.GetTweet(cachedStatus.InReplyToId.Value);
                            await api_GetTweetCompletedEvent(AccountId, null, tweet, newapi.HasError, newapi.ErrorMessage);
                        }
                    }
                }
                else if (status != null && status.in_reply_to_status_id.HasValue && status.in_reply_to_status_id != 0)
                {
                    if (tempTimeline.All(x => x.Id != status.in_reply_to_status_id))
                    {
                        var newapi = new TwitterApi(AccountId);

                        var cachedTweet = newapi.IsCachedTweet(status.in_reply_to_status_id.Value);

                        if (cachedTweet != null)
                        {
                            await api_GetTweetCompletedEvent(AccountId, cachedTweet, null, false, string.Empty);
                        }
                        else
                        {
                            var tweet = await newapi.GetTweet(status.in_reply_to_status_id.Value);
                            await api_GetTweetCompletedEvent(AccountId, null, tweet, newapi.HasError, newapi.ErrorMessage);
                        }

                    }
                }
            }
            catch
            {
            }

        }


        private void mnuProfile_Click(object sender, EventArgs e)
        {
            //if (ViewModel.IsRetweet)
            //{
            //    NavigationService.Navigate(new Uri("/UserProfile.xaml?screen=" + ViewModel.RetweetScreenName, UriKind.Relative));
            //}
            //else
            //{
            NavigationService.Navigate(
                new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + ViewModel.ScreenName, UriKind.Relative));
            //}
        }


        private void mnuOtherProfile_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(
                new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + ViewModel.RetweetScreenName,
                        UriKind.Relative));
        }

        private void mnuReply_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/NewTweet.xaml?" + ReplyUrl, UriKind.Relative));
        }

        private async void mnuFavorite_Click(object sender, EventArgs e)
        {
            long id = ViewModel.Id;

            if (IsCurrentlyFavourite)
            {
                SystemTray.ProgressIndicator = new ProgressIndicator
                                                   {
                                                       IsVisible = true,
                                                       IsIndeterminate = true,
                                                       Text = ApplicationResources.removingfavourite
                                                   };

                var api = new TwitterApi(ViewModel.AccountId);
                var result = await api.UnfavouriteTweet(id);
                api_UnfavouriteCompletedEvent(ViewModel.AccountId);
            }
            else
            {
                SystemTray.ProgressIndicator = new ProgressIndicator
                                                   {
                                                       IsVisible = true,
                                                       IsIndeterminate = true,
                                                       Text = ApplicationResources.savingfavourite
                                                   };

                var api = new TwitterApi(ViewModel.AccountId);
                await api.FavouriteTweet(id);
                api_FavouriteCompletedEvent();
            }
        }

        private void api_UnfavouriteCompletedEvent(long accountId)
        {
            IsCurrentlyFavourite = false;

            Dispatcher.BeginInvoke(delegate
                                       {

                                           try
                                           {

                                               if (ViewModel.TweetType == TweetTypeEnum.Favourite)
                                               {
                                                   long tweetId = ViewModel.Id;

                                                   // Update the data context
                                                   using (var dc = new MainDataContext())
                                                   {
                                                       var recs = from updates in dc.Favourites
                                                                  where updates.Id == tweetId
                                                                        && updates.ProfileId == accountId
                                                                  select updates;

                                                       dc.Favourites.DeleteAllOnSubmit(recs);

                                                       dc.SubmitChanges();
                                                   }

                                                   if (((IMehdohApp)(Application.Current)).ViewModel.Favourites != null && ((IMehdohApp)(Application.Current)).ViewModel.Favourites.ContainsKey(accountId))
                                                   {
                                                       var res = ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].FirstOrDefault(x => x.Id == tweetId);

                                                       if (res != null)
                                                       {
                                                           ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].Remove(res);
                                                       }
                                                   }

                                                   ThreadPool.QueueUserWorkItem(delegate
                                                                                    {
                                                                                        Thread.Sleep(500);
                                                                                        var message = ApplicationResources.removefavourite;
                                                                                        UiHelper.ShowToast(message);
                                                                                    });

                                                   UiHelper.SafeDispatch(NavigationService.GoBack);

                                               }
                                               else
                                               {

                                                   try
                                                   {
                                                       // Change the favourite button
                                                       (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IconUri = UiHelper.GetFavouriteImage();
                                                   }
                                                   catch (Exception ex)
                                                   {
                                                       ErrorLogger.LogException("api_UnfavouriteCompletedEvent", ex);
                                                   }

                                                   var message = ApplicationResources.removefavourite;
                                                   UiHelper.ShowToast(message);

                                               }

                                           }
                                           catch (Exception ex)
                                           {
                                               ErrorLogger.LogException(ex);
                                           }
                                           finally
                                           {
                                               UiHelper.HideProgressBar();
                                           }

                                       });
        }

        private void api_FavouriteCompletedEvent()
        {
            IsCurrentlyFavourite = true;

            Dispatcher.BeginInvoke(delegate
                                       {
                                           try
                                           {
                                               // Change the favourite button
                                               (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IconUri = UiHelper.GetUnFavouriteImage();
                                           }
                                           catch (Exception ex)
                                           {
                                               ErrorLogger.LogException(ex);
                                           }
                                           finally
                                           {
                                               UiHelper.HideProgressBar();

                                               var message = ApplicationResources.addfavourite;
                                               UiHelper.ShowToast("mehdoh", message);
                                           }
                                       });
        }

        private void mnuShare_Click(object sender, EventArgs e)
        {
            if (!PageLoaded)
                return;

            if (ViewModel == null)
                return;

            if (string.IsNullOrEmpty(ViewModel.ScreenName))
                return;

            var actions = new List<AppBarPromptAction>();




            actions.Add(new AppBarPromptAction(GetPrompt("share..."), () =>
                                                                            {

                                                                                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();

                                                                                dataTransferManager.DataRequested +=
                                                                                    delegate(DataTransferManager manager, DataRequestedEventArgs args)
                                                                                    {
                                                                                        DataRequest request = args.Request;
                                                                                        request.Data.Properties.Title = GetDescription();
                                                                                        request.Data.Properties.Description = GetDescription();
                                                                                        request.Data.SetWebLink(new Uri(string.Format("https://twitter.com/{0}/status/{1}", ViewModel.ScreenName.Replace("@", ""), ViewModel.Id, UriKind.Absolute)));

                                                                                    };

                                                                                DataTransferManager.ShowShareUI();
                                                                            }));

            actions.Add(new AppBarPromptAction(GetPrompt("share via email"), () =>
                                                                                 {

                                                                                     var emailComposeTask = new EmailComposeTask
                                                                                                                {
                                                                                                                    Subject = "Tweet shared from Mehdoh"
                                                                                                                };

                                                                                     var res = string.Format("@{0}: {1}\n\n{2}", ViewModel.ScreenName,
                                                                                         HttpUtility.HtmlDecode(GetDescription()), ViewModel.CreatedAtDisplay);

                                                                                     emailComposeTask.Body = res;
                                                                                     emailComposeTask.Show();
                                                                                 }));

            actions.Add(new AppBarPromptAction(GetPrompt("share via sms"), () =>
                                                                               {
                                                                                   var smsComposeTask = new SmsComposeTask();

                                                                                   var res = string.Format("@{0}: {1}\n\n{2}", ViewModel.ScreenName,
                                                                                           HttpUtility.HtmlDecode(GetDescription()), ViewModel.CreatedAtDisplay);

                                                                                   smsComposeTask.Body = res;
                                                                                   smsComposeTask.Show();
                                                                               }));

            actions.Add(new AppBarPromptAction(GetPrompt("share to storify"), CreateStorifyStory));

            actions.Add(new AppBarPromptAction(GetPrompt("copy link to clipboard"), () =>
            {
                Clipboard.SetText(GetTweetUrl());
                UiHelper.ShowToast("link copied to clipboard");
            }));


            ShareBarPrompt = new AppBarPrompt(actions.ToArray());

            ShareBarPrompt.Show();
        }

        private void api_CreateStoryCompletedEvent(object sender, EventArgs e)
        {
            var api = sender as StorifyApi;

            UiHelper.HideProgressBar();

            if (api == null || api.HasError)
            {
                UiHelper.ShowToast("unable to create story");
                return;
            }

            UiHelper.SafeDispatch(
                () => NavigationService.Navigate(new Uri("/NewTweet.xaml?accountId=" + AccountId + "&text=" + HttpUtility.UrlEncode(api.Story.content.permalink), UriKind.Relative)));
        }

        private void CreateStorifyStory()
        {
            var api = new StorifyApi();
            var urls = new List<string>();
            var screenNames = new List<string>();

            const string baseUrl = "http://www.twitter.com/";

            if (Timeline == null || Timeline.Count == 0)
            {
                urls.Add(baseUrl + "/" + ViewModel.ScreenName + "/status/" + ViewModel.Id);
                screenNames.Add(ViewModel.ScreenName);
            }
            else
            {
                foreach (var tm in Timeline.OrderBy(x => x.Id))
                {
                    urls.Add(baseUrl + "/" + tm.ScreenName + "/status/" + tm.Id);

                    if (!screenNames.Any(x => x.ToLower() == tm.ScreenName.ToLower()))
                        screenNames.Add(tm.ScreenName);
                }
            }

            UiHelper.ShowProgressBar("creating story via Storify");

            api.CreateStoryCompletedEvent += api_CreateStoryCompletedEvent;
            api.CreateStory(AccountId, CurrentAccountScreenName, screenNames, urls);
        }

        private object GetPrompt(string content)
        {
            return content;
        }


        private void mnuDelete_Click(object sender, EventArgs e)
        {
            var currentItem = (ViewModel.TweetType == TweetTypeEnum.Message) ? "direct message" : "tweet";
            var message = string.Format("Are you sure you want to delete this {0}?", currentItem);

            if (MessageBox.Show(message, "Delete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                SystemTray.ProgressIndicator = new ProgressIndicator
                                                   {
                                                       IsVisible = true,
                                                       IsIndeterminate = true,
                                                       Text = "deleting " + currentItem
                                                   };

                var api = new TwitterApi(ViewModel.AccountId);

                if (ViewModel.TweetType == TweetTypeEnum.Message)
                {
                    api.DeleteDirectMessageCompletedEvent += api_DeleteDirectMessageCompletedEvent;
                    api.DeleteDirectMessage(ViewModel.Id);
                }
                else
                {
                    api.DeleteTweetedCompletedEvent += api_DeleteTweetedCompletedEvent;
                    api.DeleteTweet(ViewModel.Id);
                }
            }
        }

        private void api_DeleteDirectMessageCompletedEvent(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(delegate
                                       {

                                           try
                                           {
                                               long tweetId = ViewModel.Id;

                                               // Update the data context
                                               using (var dc = new MainDataContext())
                                               {
                                                   var recs = from updates in dc.Messages
                                                              where updates.Id == tweetId
                                                              select updates;


                                                   foreach (var rec in recs)
                                                   {
                                                       var asses = from assets in dc.MessageAsset
                                                                   where assets.ParentId == tweetId
                                                                   select assets;

                                                       dc.MessageAsset.DeleteAllOnSubmit(asses);
                                                       dc.Messages.DeleteOnSubmit(rec);
                                                   }

                                                   dc.SubmitChanges();
                                               }

                                               var api = sender as TwitterApi;
                                               var accountId = api.AccountId;

                                               ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Clear();
                                               ((IMehdohApp)(Application.Current)).ViewModel.LoadMessages(accountId);
                                           }
                                           catch (Exception ex)
                                           {
                                               Console.WriteLine(ex.Message);
                                               ErrorLogger.LogException(ex);
                                           }
                                           finally
                                           {
                                               UiHelper.HideProgressBar();
                                               NavigationService.GoBack();
                                           }

                                       });
        }

        private void api_DeleteTweetedCompletedEvent(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(delegate
            {
                try
                {
                    long tweetId = ViewModel.Id;

                    // Update the data context
                    using (var dc = new MainDataContext())
                    {
                        var recs = from updates in dc.Timeline
                                   where updates.Id == tweetId
                                   select updates;

                        foreach (var rec in recs)
                        {
                            var asses = from assets in dc.TimelineAsset
                                        where assets.ParentId == tweetId
                                        select assets;

                            dc.TimelineAsset.DeleteAllOnSubmit(asses);
                            dc.Timeline.DeleteOnSubmit(rec);
                        }

                        dc.SubmitChanges();
                    }

                    var api = sender as TwitterApi;

                    if (api != null)
                    {
                        long accountId = api.AccountId;

                        if (((IMehdohApp)(Application.Current)).ViewModel.Timeline != null &&
                            ((IMehdohApp)(Application.Current)).ViewModel.Timeline.ContainsKey(accountId))
                        {
                            var res = ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].FirstOrDefault(x => x.Id == tweetId);
                            ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Remove(res);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    ErrorLogger.LogException(ex);
                }
                finally
                {
                    UiHelper.HideProgressBar();

                    if (NavigationService.CanGoBack)
                        NavigationService.GoBack();
                }

            });
        }

        private DialogService SelectAccountPopup { get; set; }

        private void post_CheckPressed(object sender, EventArgs e)
        {

            var post = sender as SelectTwitterAccount;

            // any selected items?
            foreach (var account in post.Items)
            {
                if (account.IsSelected)
                {
                    if (PostingAccounts.All(x => x.Id != account.Id))
                    {
                        // add it
                        PostingAccounts.Add(new AccountViewModel()
                        {
                            Id = account.Id,
                            DisplayName = account.DisplayName,
                            ImageUrl = account.ImageUrl,
                            ProfileType = account.ProfileType,
                            ScreenName = account.ScreenName
                        });
                    }
                }
                else
                {
                    if (PostingAccounts.Any(x => x.Id == account.Id))
                    {
                        // remove it
                        var item = PostingAccounts.SingleOrDefault(x => x.Id == account.Id);
                        if (item != null)
                        {
                            PostingAccounts.Remove(item);
                        }
                    }
                }
            }

            HideSelectAccountPopup();

        }

        private ObservableCollection<AccountViewModel> PostingAccounts { get; set; }


        private void selectAccountPopupClosed(object sender, EventArgs eventArgs)
        {

            SelectAccountPopup.Closed -= selectAccountPopupClosed;

            SelectAccountPopup = null;
            ApplicationBar.IsVisible = true;

            if (PostingAccounts.Count >= 1)
            {
                UiHelper.ShowProgressBar("retweeting");
                foreach (var account in PostingAccounts)
                    DoRetweet(account.Id);
            }
        }

        private void HideSelectAccountPopup()
        {
            SelectAccountPopup.Closed += new EventHandler(selectAccountPopupClosed);

            UiHelper.SafeDispatch(() => SelectAccountPopup.Hide());

        }

        private bool ShowSelectAccount()
        {

            var allowedAccounts = new List<long>();

            PostingAccounts = new ObservableCollection<AccountViewModel>();

            var aph = new AccountPostingHelper();
            PostingAccounts.AddRange(aph.GetAllValidAccountsForPosting());

            if (PostingAccounts.Count < 2) // anything less than 2 and we're not interested in showing this
                return false;

            List<AccountFriendViewModel> accountIds = PostingAccounts.Select(acccount => new AccountFriendViewModel()
            {
                Id = acccount.Id,
                ScreenName = acccount.ScreenName,
                StatusChecked = true,
                IsFriend = true
            }).ToList();

            var post = new SelectTwitterAccount
                       {
                           ExistingValues = accountIds
                       };
            post.CheckPressed += new EventHandler(post_CheckPressed);

            if (allowedAccounts.Any())
            {
                post.AllowedIds = allowedAccounts;
                post.UpdateAllowed();
            }

            ApplicationBar.IsVisible = false;

            SelectAccountPopup = new DialogService
            {
                AnimationType = DialogService.AnimationTypes.Slide,
                Child = post
            };

            SelectAccountPopup.Show();

            return true;

        }

        private void mnuRetweet_Click(object sender, EventArgs e)
        {

            var actions = new List<AppBarPromptAction>
                          {
                              new AppBarPromptAction(GetPrompt("retweet"), () =>
                                                                           {
                                                                               if (!ShowSelectAccount())
                                                                               {
                                                                                   UiHelper.ShowProgressBar("retweeting");
                                                                                   DoRetweet(ViewModel.AccountId);
                                                                               }
                                                                           }),

                              new AppBarPromptAction(GetPrompt("edit and retweet"), () =>
                                                                                    {
                                                                                        var newDesc = GetRetweetText(ViewModel.ScreenName, GetDescription()) ?? "";

                                                                                        RetweetBarPrompt.Hide();

                                                                                        // Navigate to the new page
                                                                                        NavigationService.Navigate(new Uri("/NewTweet.xaml?accountId=" + ViewModel.AccountId + "&isEditRt=true&text=" + newDesc, UriKind.Relative));
                                                                                    })
                          };

            RetweetBarPrompt = new AppBarPrompt(actions.ToArray());
            RetweetBarPrompt.Show();

        }

        private void DoRetweet(long accountId)
        {
            var api = new TwitterApi(accountId);
            api.RetweetCompletedEvent += api_RetweetCompletedEvent;
            api.Retweet(ViewModel.Id);
        }

        private void api_RetweetCompletedEvent(object sender, EventArgs e)
        {
            // Update the item
            var api = sender as TwitterApi;

            if (api == null)
                return;

            try
            {

                if (api.HasError && !string.IsNullOrEmpty(api.ErrorMessage))
                {
                    UiHelper.SafeDispatch(() => MessageBox.Show(api.ErrorMessage, "retweet failed", MessageBoxButton.OK));
                    UiHelper.HideProgressBar();
                    return;
                }

                if (api.RetweetResponse != null && api.RetweetResponse.id > 0)
                {
                    // Update the data context
                    using (var dc = new MainDataContext())
                    {
                        var recs = from updates in dc.Timeline
                                   where updates.IdStr == api.RetweetResponse.retweeted_status.id_str
                                   select updates;

                        var rec = recs.FirstOrDefault();
                        if (rec != null)
                        {
                            // Convert the tweet to a retweet
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

                }

            }
            catch (Exception ex)
            {
                return;
            }

            Dispatcher.BeginInvoke(delegate
                                           {
                                               try
                                               {
                                                   long accountId = api.AccountId;

                                                   // update the item in the view
                                                   if (((IMehdohApp)(Application.Current)).ViewModel.Timeline != null && ((IMehdohApp)(Application.Current)).ViewModel.Timeline.ContainsKey(accountId))
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
                                               }
                                               catch (Exception ex)
                                               {
                                                   ErrorLogger.LogException(ex);
                                               }
                                               finally
                                               {
                                                   UiHelper.ShowToast("tweet retweeted!");
                                               }

                                           });

            UiHelper.HideProgressBar();
        }

        private void imageProfile1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UiHelper.SafeDispatch(() =>
            {
                try
                {
                    NavigationService.Navigate(
                        new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + ViewModel.ScreenName,
                            UriKind.Relative));
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex);
                }

            });
        }

        private void retweetedUser_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            UiHelper.SafeDispatch(() =>
            {
                try
                {
                    NavigationService.Navigate(
                        new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + ViewModel.RetweetScreenName,
                            UriKind.Relative));
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex);
                }

            });
        }

        // Handle selection changed on ListBox
        private void lstTimeline_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (lstTimeline.SelectedIndex == -1)
                return;

            var item = lstTimeline.SelectedValue as TimelineViewModel;
            string query = string.Empty;

            if (item != null)
            {
                if (ViewModel.TweetType == TweetTypeEnum.Timeline)
                    query = string.Format("id={0}", item.Id);
                else if (ViewModel.TweetType == TweetTypeEnum.Favourite)
                    query = string.Format("favId={0}", item.Id);
                else if (ViewModel.TweetType == TweetTypeEnum.Mention)
                    query = string.Format("mentionId={0}", item.Id);
                else if (ViewModel.TweetType == TweetTypeEnum.Message)
                {
                    var thisUser = CurrentAccountScreenName;

                    if (string.Compare(item.ScreenName, thisUser, StringComparison.InvariantCultureIgnoreCase) != 0)
                        query = string.Format("messageId={0}", item.Id);
                    else
                        query = string.Empty;
                }
                else
                    query = string.Format("id={0}", item.Id);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                // Navigate to the new page
                NavigationService.Navigate(new Uri("/DetailsPage.xaml?accountId=" + AccountId + "&" + query,
                                                   UriKind.Relative));
            }

            // Reset selected index to -1 (no selection)
            lstTimeline.SelectedIndex = -1;
        }

        private string GetRetweetText(string screenName, string description)
        {
            var sh = new SettingsHelper();
            var res = sh.GetRetweetStlye();

            switch (res)
            {
                case ApplicationConstants.RetweetStyleEnum.MT:
                    return HttpUtility.UrlEncode("MT @" + screenName + ": " + description);
                case ApplicationConstants.RetweetStyleEnum.RT:
                    return HttpUtility.UrlEncode("RT @" + screenName + ": " + description);
                case ApplicationConstants.RetweetStyleEnum.QuotesVia:
                    return HttpUtility.UrlEncode("\"" + description + "\" via @" + screenName);
                case ApplicationConstants.RetweetStyleEnum.Quotes:
                    return HttpUtility.UrlEncode("\"@" + screenName + ": " + description + "\"");
                default:
                    return HttpUtility.UrlEncode("RT @" + screenName + ": " + description);
            }
        }

        //private void mnuEditRetweet_Click(object sender, EventArgs e)
        //{
        //    var newDesc = GetRetweetText(ViewModel.ScreenName, GetDescription()) ?? "";

        //    // Navigate to the new page
        //    NavigationService.Navigate(
        //        new Uri("/NewTweet.xaml?accountId=" + ViewModel.AccountId + "&isEditRt=true&text=" + newDesc,
        //                UriKind.Relative));
        //}

        private void wrapText_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SetDescription();
        }

        #endregion
    }


#if WP7
    /// <summary>
    /// Represents a 2-tuple, or pair.
    /// </summary>
    /// <typeparam name="T1">The type of the tuple's first component.</typeparam>
    /// <typeparam name="T2">The type of the tuple's second component.</typeparam>
    /// <typeparam name="T3">The type of the tuple's second component.</typeparam>
    internal class Tuple<T1, T2, T3>
    {
        /// <summary>
        /// Gets the value of the current Tuple(T1, T2) object's first component.
        /// </summary>
        public T1 Item1 { get; private set; }

        /// <summary>
        /// Gets the value of the current Tuple(T1, T2) object's second component.
        /// </summary>
        public T2 Item2 { get; private set; }

        /// <summary>
        /// Gets the value of the current Tuple(T1, T2) object's second component.
        /// </summary>
        public T3 Item3 { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Tuple(T1, T2) class.
        /// </summary>
        /// <param name="item1">The value of the tuple's first component.</param>
        /// <param name="item2">The value of the tuple's second component.</param>
        /// <param name="item3">The value of the tuple's second component.</param>
        public Tuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
    }

#endif

    public class ReadabilityResult
    {
        public string domain { get; set; }
        public object next_page_id { get; set; }
        public string url { get; set; }
        public string short_url { get; set; }
        public string author { get; set; }
        public string excerpt { get; set; }
        public string direction { get; set; }
        public int? word_count { get; set; }
        public int? total_pages { get; set; }
        public string content { get; set; }
        public string date_published { get; set; }
        public object dek { get; set; }
        public object lead_image_url { get; set; }
        public string title { get; set; }
        public int? rendered_pages { get; set; }
    }

}

