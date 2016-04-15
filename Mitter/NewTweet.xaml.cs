using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.Storage;
using Clarity.Phone.Extensions;
using Coding4Fun.Toolkit.Controls;
using FieldOfTweets.Common;

#if FACEBOOK
using FieldOfTweets.Common.Api.Facebook;
#endif

using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.ErrorLogging;
using FieldOfTweets.Common.ImageHost;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Friends;
using FieldOfTweets.Common.UI.Helpers;
using FieldOfTweets.Common.UI.ImageHost;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.ThirdPartyApi;
using FieldOfTweets.Common.UI.ViewModels;
using Lumia.Imaging;
using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Primitives;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
#if WP8
using Windows.Phone.Speech.Recognition;
#endif

using GestureEventArgs = System.Windows.Input.GestureEventArgs;
using SelectTwitterAccount = FieldOfTweets.Common.UI.UserControls.SelectTwitterAccount;

namespace Mitter
{

    public partial class NewTweet : AnimatedBasePage
    {

        private bool IsEditRetweet { get; set; }

        private string HashTags { get; set; }
        private string OtherAuthors { get; set; }

        private bool TweetPosted { get; set; }

        private int AccountPostTotal { get; set; }

        private DialogService SelectAccountPopup { get; set; }

        private ObservableCollection<AccountViewModel> PostingAccounts { get; set; }

        private ListBox lstTimeline { get; set; }

        // Used when the user is replying via voice
        protected long? VoiceReplyId { get; set; }
        protected string VoiceReplyName { get; set; }
        protected string VoiceDescription { get; set; }

        protected ResponseAccountSettings CurrentAccountSettings { get; set; }

        public class HashTagsViewModel
        {
            public string Text { get; set; }

            public BitmapImage AddMentionPng
            {
                get
                {
                    return UiHelper.GetAddMentionPng();
                }
            }

            public HashTagsViewModel(string text)
            {
                Text = text;
            }
        }

        private IImageHost _imageHost;
        public IImageHost ImageHost
        {
            get
            {
                return _imageHost ?? (_imageHost = ImageHostFactory.Host);
            }
            set { _imageHost = value; }
        }

        public class OtherAuthorsViewModel
        {
            public string Text { get; set; }

            public BitmapImage AddMentionPng
            {
                get
                {
                    return UiHelper.GetAddMentionPng();
                }
            }

            public OtherAuthorsViewModel(string text)
            {
                Text = text;
            }
        }

        // Declare the CameraCaptureTask object with page scope.

        private long AccountId { get; set; }

        private string ReplyToAuthor { get; set; }
        private long ReplyToId { get; set; }
        private bool IsDM { get; set; }

        private Image img { get; set; }
        private Popup popUp { get; set; }

        private bool IsFirstGeoLocation { get; set; }
        private bool IsConfigured { get; set; }

        private SortedObservableCollection<TimelineViewModel> Timeline { get; set; }
        private ObservableCollection<OtherAuthorsViewModel> OtherAuthorsModel { get; set; }

        private string PlaceId { get; set; }

        private string LocationName { get; set; }
        private GeoCoordinate Position { get; set; }
        private GeoCoordinateWatcher LocationWatcher { get; set; }
        private PhotoChooserTask PhotoChooserTask { get; set; }

        private bool IsQuickMentionEnabled { get; set; }
        private bool IsQuickHashtagEnabled { get; set; }

        private int QuickMentionStartPosition { get; set; }
        private int QuickHashtagStartPosition { get; set; }

        private bool TerminateOnPost { get; set; }

        public NewTweet()
        {
            IsConfigured = false;

            InitializeComponent();

            AnimationContext = LayoutRoot;

            Position = new GeoCoordinate(0, 0);
            Timeline = new SortedObservableCollection<TimelineViewModel>();

            //lstTimeline.DataContext = Timeline;

            ImageHost.Dispose();

            Loaded += NewTweet_Loaded;

        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn)
                return new SlideUpAnimator { RootElement = LayoutRoot };

            if (animationType == AnimationType.NavigateBackwardOut)
                return new SlideDownAnimator { RootElement = LayoutRoot };

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

            if (ImageHost != null)
                ImageHost.Dispose();

            base.OnBackKeyPress(e);

            Timeline = null;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {

            ((IMehdohApp)Application.Current).ShareLink = null;

            base.OnNavigatedTo(e);

#if WP8
            bool needsSpeak = false;
#endif

            try
            {

                if (NavigationService.CanGoBack)
                {
                    bool done = false;
                    while (!done)
                    {
                        var last = NavigationService.BackStack.FirstOrDefault();
                        if (last != null)
                        {
                            if (last.Source.OriginalString.ToLower().Contains("newtweet.xaml"))
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

                if (e.NavigationMode == NavigationMode.New || e.NavigationMode == NavigationMode.Forward)
                {

                    if (IsConfigured)
                        return;

                    if (!UiHelper.ValidateUser())
                    {
                        NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                        return;
                    }

                    // Get account id
                    AccountId = UiHelper.GetAccountId(NavigationContext);

                    TweetPosted = false;

                    DisableQuickMention();

                    GetAccountSettings();

                    IsConfigured = true;

#if WP8
                    if (NavigationContext.QueryString.ContainsKey("voiceCommandName"))
                    {
                        if (NavigationContext.QueryString["voiceCommandName"] == "NewTweet" || NavigationContext.QueryString["voiceCommandName"] == "Compose")
                        {
                            needsSpeak = true;
                        }
                        else if (NavigationContext.QueryString["voiceCommandName"] == "Reply")
                        {
                            // get the last mention, and turn this back into a reply
                            GetReplyTweetDetails();
                            needsSpeak = true;
                        }
                    }

                    if (NavigationContext.QueryString.ContainsKey("terminate"))
                    {
                        TerminateOnPost = true;
                    }
#endif

                }
                else if (e.NavigationMode == NavigationMode.Back)
                {
                    pivotMain.SelectedIndex = 0;
                    SetAccountHeader();
                }

            }
            catch
            {

            }

            await ProcessHeaders();

#if WP8
            if (needsSpeak)
                Speak();
#endif

        }

#if WP8
        private void GetReplyTweetDetails()
        {

            using (var dh = new MainDataContext())
            {
                if (!dh.Mentions.Any())
                    return;

                var lastMention = dh.Mentions.OrderByDescending(x => x.Id).FirstOrDefault();
                if (lastMention == null)
                    return;

                VoiceReplyId = lastMention.Id;
                VoiceReplyName = lastMention.ScreenName;
            }

        }
#endif

#if WP8
        private async void Speak()
        {

            var sr = new SpeechRecognizerUI();
            sr.Settings.ListenText = "Listening...";
            sr.Settings.ReadoutEnabled = true;
            sr.Settings.ShowConfirmation = false;

            var result = await sr.RecognizeWithUIAsync();

            if (result.ResultStatus == SpeechRecognitionUIStatus.Succeeded &&
                result.RecognitionResult != null &&
                result.RecognitionResult.TextConfidence != SpeechRecognitionConfidence.Rejected)
            {
                VoiceDescription = result.RecognitionResult.Text;

                txtTweet.Text += VoiceDescription;
                txtTweet.SelectionStart = txtTweet.Text.Length;
            }

        }
#endif

        private void GetAccountSettings()
        {

            ThreadPool.QueueUserWorkItem(async delegate
                                             {
                                                 // Get the user account settings
                                                 var accountHelper = new AccountSettingsHelper(AccountId);                                                 
                                                 CurrentAccountSettings = await accountHelper.GetAccountSettings();
                                                 ContinueLoading();
                                             });
        }

        private void ContinueLoading()
        {
            ThreadPool.QueueUserWorkItem(delegate
                                             {
                                                 var sh = new SettingsHelper();
                                                 if (sh.GetLocationServicesEnabled() && sh.GetAlwaysGeoTag())
                                                 {
                                                     StartGetLocation();
                                                 }
                                             });
        }

        private string GetMusicGenre()
        {

            string genre = string.Empty;

            try
            {
                if (MediaPlayer.State == MediaState.Playing)
                {
                    if (MediaPlayer.Queue.ActiveSong.Artist != null && !string.IsNullOrEmpty(MediaPlayer.Queue.ActiveSong.Artist.Name))
                    {
                        if (MediaPlayer.Queue.ActiveSong.Genre != null && !string.IsNullOrEmpty(MediaPlayer.Queue.ActiveSong.Genre.Name))
                            genre = MediaPlayer.Queue.ActiveSong.Genre.Name.Replace(" ", "");
                    }
                }
            }
            catch
            {
                // Ignore
            }

            return genre;

        }

        private bool IsPlayingMusic()
        {
            try
            {
                if (MediaPlayer.State == MediaState.Playing)
                    return true;

                if (BackgroundAudioPlayer.Instance != null && (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing))
                    return true;

            }
            catch
            {
                return false;
            }

            return false;
        }

        public bool HasArt()
        {
            try
            {
                return MediaPlayer.Queue.ActiveSong.Album.HasArt;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Stream GetAlbumArt()
        {
            try
            {
                return MediaPlayer.Queue.ActiveSong.Album.GetAlbumArt();
            }
            catch (Exception)
            {
                return null;
            }

        }

        private async Task ProcessHeaders()
        {

            try
            {

                // default to this
                ApplicationBar = Resources["mnuNormal"] as ApplicationBar;
                ApplicationBar.MatchOverriddenTheme();

                if (!string.IsNullOrEmpty(UiHelper.SelectedUser))
                {
                    txtTweet.Text += (UiHelper.SelectedUser + " ");
                    UiHelper.SelectedUser = string.Empty;
                }

                if (DataLoaded)
                    return;

                // only focus on the tweet if current pivot is the one with the text in
                if (pivotMain.SelectedIndex == 0)
                    txtTweet.Focus();

                string text;
                if (NavigationContext.QueryString.TryGetValue("text", out text))
                {
                    if (!string.IsNullOrEmpty(VoiceDescription))
                        text = VoiceDescription;

                    var newDesc = HttpUtility.HtmlDecode(text);
                    txtTweet.Text = " " + newDesc;
                    txtTweet.SelectionStart = 0;
                }

                string desc;
                if (NavigationContext.QueryString.TryGetValue("desc", out desc))
                {
                    var newDesc = HttpUtility.HtmlDecode(desc);
                    txtTweet.Text = newDesc;
                    txtTweet.SelectionStart = 0;

                    // Too small? Auto shorten
                    if (UpdateCount() < 0)
                    {
                        ShortenUrls();
                    }
                }

                string soundcloudUrl;
                if (NavigationContext.QueryString.TryGetValue("soundcloudUrl", out soundcloudUrl))
                {
                    // todo: fix this when we get snd.sc links

                    //SoundcloudUrl = HttpUtility.HtmlDecode(soundcloudUrl);
                    //var api = new BitlyApi();
                    //api.GetShortUrlCompletedEvent += delegate(object sender1, EventArgs e1)
                    //                                     {
                    //                                         try
                    //                                         {
                    //                                             var bitlyApi = sender1 as BitlyApi;
                    //                                             UiHelper.SafeDispatch(() =>
                    //                                                                       {
                    //                                                                           txtTweet.Text = txtTweet.Text.Replace(soundcloudUrl, api.ShortUrl);
                    //                                                                       });
                    //                                         }
                    //                                         catch (Exception)
                    //                                         {
                    //                                         }

                    //                                     };
                    //api.GetShortUrl(soundcloudUrl);
                }

                // soundcloudUrl

                //string fileId = "{08B03D38-EF6D-EBD7-318D-1FD92C362DBC}";
                string fileId;
                //if (!String.IsNullOrEmpty(fileId) || NavigationContext.QueryString.TryGetValue("FileId", out fileId))
                if (NavigationContext.QueryString.TryGetValue("FileId", out fileId))
                {
                    //FileId = fileId;

                    var library = new MediaLibrary();
                    var picture = library.GetPictureFromToken(fileId);

                    SetAccountHeader();

                    if (PostingAccounts.Count > 1)
                    {
                        ShowSelectAccount();
                    }

                    await ProcessChosenImage(picture.GetImage());

                }

                string shareImage;
                if (NavigationContext.QueryString.TryGetValue("shareImage", out shareImage))
                {

                    var file = await ApplicationData.Current.TemporaryFolder.GetFileAsync(shareImage);
                    var fileStream = await file.OpenStreamForReadAsync();
                    await ProcessChosenImage(fileStream);

                }

                string temp;

                if (!string.IsNullOrEmpty(VoiceReplyName))
                {
                    ReplyToAuthor = VoiceReplyName;
                }
                else
                {
                    if (NavigationContext.QueryString.TryGetValue("replyToAuthor", out temp))
                        ReplyToAuthor = temp;
                }

                if (NavigationContext.QueryString.TryGetValue("others", out temp))
                {
                    OtherAuthors = temp;
                }

                if (NavigationContext.QueryString.TryGetValue("hashtags", out temp))
                {
                    HashTags = HttpUtility.UrlDecode(temp);
                }

                if (NavigationContext.QueryString.TryGetValue("dm", out temp))
                {
                    IsDM = temp == "true";
                }

                SetAppBarMenu();

                if (IsDM)
                {
                    StartGetDMHistory(ReplyToAuthor);
                }

                temp = string.Empty;
                if (VoiceReplyId.HasValue || NavigationContext.QueryString.TryGetValue("replyToId", out temp))
                {
                    ReplyToId = VoiceReplyId.HasValue ? VoiceReplyId.Value : long.Parse(temp);

                    SetHeaderReply();
                    CreateConversationPivot();
                    StartGetHistory();
                }

                IsEditRetweet = false;

                if (NavigationContext.QueryString.TryGetValue("isEditRt", out temp))
                {
                    bool tempBool;
                    if (bool.TryParse(temp, out tempBool))
                    {
                        if (tempBool)
                            IsEditRetweet = true;
                    }
                }


                if (IsDM)
                {
                    SetHeaderReplyDm(ReplyToAuthor.ToLower());
                }
                else
                {

                    if (!string.IsNullOrEmpty(ReplyToAuthor))
                    {
                        string prePopText = string.Empty;

                        if (!ReplyToAuthor.StartsWith("@"))
                            prePopText = prePopText + "@";
                        prePopText = prePopText + ReplyToAuthor + " ";
                        txtTweet.Text = prePopText;
                        txtTweet.SelectionStart = prePopText.Length;
                    }

                    OtherAuthorsModel = new ObservableCollection<OtherAuthorsViewModel>();

                    if (!string.IsNullOrEmpty(OtherAuthors))
                    {
                        var list = OtherAuthors.Split(',');
                        var othersTemp = list.Select(l => new OtherAuthorsViewModel(l)).ToList();

                        foreach (var otherAuthorsViewModel in othersTemp)
                        {
                            OtherAuthorsModel.Add(otherAuthorsViewModel);
                        }

                        int startPos = txtTweet.Text.Length;
                        string newText = OtherAuthors.Replace(",", " ") + " ";
                        txtTweet.Text += newText;
                        txtTweet.Select(startPos, newText.Length);

                    }

                    if (!string.IsNullOrEmpty(HashTags))
                    {
                        var hashList = HashTags.Split(',').ToList();

                        foreach (var item in hashList)
                        {
                            if (!item.StartsWith("#"))
                                OtherAuthorsModel.Add(new OtherAuthorsViewModel("#" + item));
                            else
                            {
                                OtherAuthorsModel.Add(new OtherAuthorsViewModel(item));
                            }
                        }
                    }

                    lstOthers.DataContext = OtherAuthorsModel;

                }

                SetAccountHeader();

            }
            catch (Exception ex)
            {
#if WP8
                CrittercismSDK.Crittercism.LogHandledException(ex);
#endif
                ErrorLogger.LogException("ProcessHeaders", ex);
            }
            finally
            {
                DataLoaded = true;
            }


        }

        private void SetAppBarMenu()
        {

            if (pivotMain.SelectedIndex == 0)
            {
                if (IsDM)
                {
                    ApplicationBar = Resources["mnuMessage"] as ApplicationBar;
                }
                else
                {
                    ApplicationBar = Resources["mnuNormal"] as ApplicationBar;

                    if (ApplicationBar != null)
                    {

                        var isPlayingMusic = IsPlayingMusic();
                        bool exists = false;

                        foreach (ApplicationBarMenuItem button in ApplicationBar.MenuItems)
                        {
                            if (button.Text == "tweet now playing")
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (isPlayingMusic && !exists)
                        {
                            var item = new ApplicationBarMenuItem
                                       {
                                           Text = "tweet now playing",
                                           IsEnabled = true
                                       };
                            item.Click += mnuNowPlaying_Click;
                            ApplicationBar.MenuItems.Insert(0, item);
                        }
                    }

                }

                if (ApplicationBar != null)
                    ApplicationBar.IsVisible = true;
            }
            else
            {
                ApplicationBar = Resources["mnuBlank"] as ApplicationBar;
                if (ApplicationBar != null)
                    ApplicationBar.IsVisible = false;
            }

            if (ApplicationBar != null)
                ApplicationBar.MatchOverriddenTheme();
        }

        private void CreateConversationPivot()
        {
            lstTimeline = new ListBox()
                              {
                                  Margin = new Thickness(0),
                              };


            //lstTimeline.SetValue(Grid.RowProperty, 1);
            //lstTimeline.SetValue(Grid.ColumnSpanProperty, 2);

            lstTimeline.SelectionChanged += lstTimeline_SelectionChanged;
            lstTimeline.ItemTemplate = Resources["ConversationTemplate"] as DataTemplate;
            lstTimeline.ItemsSource = Timeline;

            var pivot = new PivotItem { Content = lstTimeline, Header = new TextBlock() { Text = "conversation", FontSize = 48 } };

            pivotMain.Items.Add(pivot);

        }

        private void NewTweet_Loaded(object sender, RoutedEventArgs e)
        {
            txtTweet.Focus();
        }

        private void SetHeaderReplyDm(string username)
        {
            txtMainPivotHeader.Text = "dm @" + username;
        }

        private void SetHeaderReply()
        {
            txtMainPivotHeader.Text = "reply";
        }

        private void SetAccountHeader()
        {

            try
            {

                if (PostingAccounts == null)
                {
                    PostingAccounts = new ObservableCollection<AccountViewModel>();

                    var aph = new AccountPostingHelper();

                    if (AccountId == 0)
                    {
                        // lets see what we need to add
                        foreach (var postingAccount in aph.GetAllValidAccountsForPosting())
                            PostingAccounts.Add(postingAccount);
                    }
                    else
                    {
                        PostingAccounts.AddRange(aph.GetReplyAccount(AccountId));
                    }
                }

                ShowHideAccountButton();

            }
            catch
            {

            }

        }

        private void ShowHideAccountButton()
        {
            if (!PostingAccounts.Any())
            {
                buttonAddAccount.Visibility = Visibility.Visible;
                headerAccounts.DataContext = null;
            }
            else
            {
                buttonAddAccount.Visibility = Visibility.Collapsed;
                headerAccounts.DataContext = PostingAccounts;
            }

        }

        //private string FileId { get; set; }

        protected bool DataLoaded { get; set; }


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
                    var item = cachedStatus.AsViewModel(AccountId);

                    if (Timeline.All(x => x.Id != item.Id))
                        Timeline.Add(item);
                }
            }

            // Now get sender items
            StartGetDMHistoryThread(null);
        }

        private async void StartGetDMHistoryThread(object state)
        {
            var recipient = ReplyToAuthor;
            var sender = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(AccountId);

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
                            AccountId = AccountId,
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
            var result = await api.GetSentDirectMessages(maxId);
            api_GetSentDirectMessagesCompletedEvent(result);

        }

        private void api_GetSentDirectMessagesCompletedEvent(List<ResponseGetSentDirectMessage> sentDirectMessages)
        {
            if (sentDirectMessages == null || !sentDirectMessages.Any())
                return;

            UiHelper.SafeDispatch(() =>
            {
                var recipient = ReplyToAuthor;
                var sendee = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(AccountId);

                // Lets see what we got.
                foreach (var item in sentDirectMessages.Where(x => string.Compare(x.recipient_screen_name, recipient, StringComparison.CurrentCultureIgnoreCase) == 0 &&
                                                                   string.Compare(x.sender_screen_name, sendee, StringComparison.CurrentCultureIgnoreCase) == 0))
                {
                    var newView = new TimelineViewModel
                    {
                        AccountId = AccountId,
                        ScreenName = item.sender_screen_name,
                        DisplayName = item.sender.name,
                        Description = HttpUtility.HtmlDecode(item.text),
                        CreatedAt = item.created_at,
                        ImageUrl = item.sender.profile_image_url,
                        Id = item.id,
                        IsRetweet = false
                    };

                    if (Timeline.All(x => x.Id != item.id))
                        Timeline.Add(newView);
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


        private void StartGetHistory()
        {

            if (!IsDM)
            {
                ThreadPool.QueueUserWorkItem(async delegate
                                                 {

                                                     var api = new TwitterApi(AccountId);

                                                     var cachedTweet = api.IsCachedTweet(ReplyToId);

                                                     if (cachedTweet != null)
                                                     {
                                                         api_GetTweetCompletedEvent(AccountId, cachedTweet, null);
                                                     }
                                                     else
                                                     {
                                                         var result = await api.GetTweet(ReplyToId);
                                                         api_GetTweetCompletedEvent(AccountId, null, result);
                                                     }


                                                 });
            }

        }
        
        private void api_GetTweetCompletedEvent(long accountId, TimelineTable cachedTweet, ResponseTweet tweet)
        {

            try
            {

                var dsh = new DataStorageHelper();
                var thisTweet = cachedTweet ?? dsh.TweetResponseToTable(accountId, tweet);

                if (thisTweet == null)
                {
                    return;
                }

                var item = new TimelineViewModel
                               {
                                   ScreenName = thisTweet.ScreenName,
                                   DisplayName = thisTweet.DisplayName,
                                   Description = HttpUtility.HtmlDecode(thisTweet.Description),
                                   CreatedAt = thisTweet.CreatedAt,
                                   ImageUrl = thisTweet.ProfileImageUrl,
                                   Id = thisTweet.Id,
                                   AccountId = accountId,
                                   IsRetweet = false
                               };

                if (Timeline == null)
                {
                    Timeline = new SortedObservableCollection<TimelineViewModel>();
                }

                var thisTimeline = Timeline.ToList();

                if (thisTimeline.FirstOrDefault(x => x.Id == item.Id) == null)
                {
                    UiHelper.SafeDispatch(() =>
                    {
                        try
                        {
                            if (Timeline != null)
                                Timeline.Add(item);
                        }
                        catch (Exception)
                        {
                        }                        
                    });
                }

                if (thisTweet.InReplyToId.HasValue && thisTweet.InReplyToId.Value != 0)
                {
                    if (thisTimeline.All(x => x.Id != thisTweet.InReplyToId))
                    {
                        if (!IsDM)
                        {
                            ThreadPool.QueueUserWorkItem(async delegate
                                                             {
                                                                 try
                                                                 {
                                                                     var historyApi = new TwitterApi(accountId);
                                                                     var nextTweet = await historyApi.GetTweet(thisTweet.InReplyToId.Value);
                                                                     api_GetTweetCompletedEvent(accountId, null, nextTweet);
                                                                 }
                                                                 catch (Exception exception)
                                                                 {
                                                                     Console.WriteLine(exception);
                                                                 }
                                                             });
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("api_GetTweetCompletedEvent", ex);
            }


        }

        private bool NeedsInsertSpace { get; set; }

        private void txtTweet_TextChanged(object sender, TextChangedEventArgs e)
        {

            UpdateCount();

            if (IsQuickMentionEnabled)
            {

                try
                {
                    // Have they deleted the @ ?
                    if (string.IsNullOrEmpty(txtTweet.Text))
                    {
                        DisableQuickMention();
                        return;
                    }
                    if (txtTweet.Text.Length <= QuickMentionStartPosition - 1)
                    {
                        DisableQuickMention();
                        return;
                    }
                    if (txtTweet.Text.Substring(QuickMentionStartPosition - 1, 1) != "@")
                    {
                        DisableQuickMention();
                        return;
                    }
                    if (txtTweet.Text.Substring(txtTweet.SelectionStart - 1, 1) == " ")
                    {
                        DisableQuickMention();
                        return;
                    }

                    string searchQuery;

                    // At the end or in the middle?
                    if (txtTweet.Text.IndexOf(" ", QuickMentionStartPosition - 1, StringComparison.Ordinal) < 0)
                    {
                        // end
                        searchQuery = txtTweet.Text.Substring(QuickMentionStartPosition - 1);
                    }
                    else
                    {
                        // middle
                        var endBit = txtTweet.Text.Substring(QuickMentionStartPosition - 1);
                        var length = endBit.IndexOf(" ", StringComparison.Ordinal);
                        searchQuery = txtTweet.Text.Substring(QuickMentionStartPosition - 1, length);
                    }

                    // grab the last big after the @
                    lstQuick.DataContext = FriendsCache.SearchFriend(searchQuery.Replace("@", ""));

                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException("txtTweet_TextChanged:1", ex);
                }

            }
            else if (IsQuickHashtagEnabled)
            {

                try
                {
                    // Have they deleted the # ?
                    if (string.IsNullOrEmpty(txtTweet.Text))
                    {
                        DisableQuickHashtag();
                        return;
                    }

                    if (txtTweet.Text.Length <= QuickHashtagStartPosition - 1)
                    {
                        DisableQuickHashtag();
                        return;
                    }

                    if (txtTweet.Text.Substring(QuickHashtagStartPosition - 1, 1) != "#")
                    {
                        DisableQuickHashtag();
                        return;
                    }

                    if (txtTweet.Text.Substring(txtTweet.SelectionStart - 1, 1) == " ")
                    {
                        DisableQuickHashtag();
                        return;
                    }

                    string searchQuery;

                    // At the end or in the middle?
                    if (txtTweet.Text.IndexOf(" ", QuickHashtagStartPosition - 1, StringComparison.Ordinal) < 0)
                    {
                        // end
                        searchQuery = txtTweet.Text.Substring(QuickHashtagStartPosition - 1);
                    }
                    else
                    {
                        // middle
                        var endBit = txtTweet.Text.Substring(QuickHashtagStartPosition - 1);
                        var length = endBit.IndexOf(" ", StringComparison.Ordinal);
                        searchQuery = txtTweet.Text.Substring(QuickHashtagStartPosition - 1, length);
                    }

                    // grab the last big after the @
                    lstQuick.DataContext = HashtagCache.SearchHashtag(searchQuery.Replace("#", ""));

                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException("txtTweet_TextChanged:2", ex);
                }

            }
            else
            {

                try
                {
                    var currentPosition = txtTweet.SelectionStart;
                    if (currentPosition != 0)
                    {
                        if (txtTweet.Text.Substring(currentPosition - 1, 1) == "@")
                            EnableQuickMetion();
                        else if (txtTweet.Text.Substring(currentPosition - 1, 1) == "#")
                            EnableQuickHashtag();
                    }

                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException("txtTweet_TextChanged:3", ex);
                }

            }

        }

        private int UpdateCount()
        {
            try
            {
                int remainingCount = 140 - GetShrunkenLength();

                if (ImageHost.CurrentlyHasImage())
                    remainingCount -= ImageHost.ReserveSize;

                txtCount.Text = remainingCount.ToString(CultureInfo.CurrentUICulture);
                txtCount.Foreground = (remainingCount < 0) ? Resources["PhoneAccentBrush"] as SolidColorBrush : new SolidColorBrush(Colors.DarkGray);
                txtCount.SetValue(Canvas.ZIndexProperty, 1);

                return remainingCount;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("UpdateCount", ex);
            }

            return 0;
        }

        private async void mnuPost_Click(object sender, EventArgs e)
        {
            try
            {
                await PostTheTweet();
            }
            catch (Exception ex)
            {
#if WP8
                CrittercismSDK.Crittercism.LogHandledException(ex);
#endif
                ErrorLogger.LogException("mnuPost_Click", ex);

            }

        }

        private async Task PostTheTweet()
        {

            if (TweetPosted)
            {
                MessageBox.Show("This update has already been posted. Please press the back button.", "Post Tweet", MessageBoxButton.OK);
                return;
            }

            string newText = txtTweet.Text.Trim();

            // if its got a twitter picture attached, then the following doesn't matter
            bool hasWorkingImage = false;

            try
            {
                hasWorkingImage = ImageHost.CurrentlyHasImage();
            }
            catch (Exception)
            {
                hasWorkingImage = false;
            }

            if (!hasWorkingImage)
            {
                if (String.IsNullOrEmpty(newText))
                    return;

                bool hasThingsOtherThanMentions = false;

                var splitContent = newText.Split(' ');
                if (splitContent.Any())
                {
                    foreach (var item in splitContent)
                    {
                        if (!item.StartsWith("@"))
                        {
                            hasThingsOtherThanMentions = true;
                            break;
                        }
                    }

                    if (!hasThingsOtherThanMentions)
                    {
                        if (MessageBox.Show("Looks like you've only got mentions in this tweet. Is that what you really want to do?", "missing text?", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                            return;
                    }
                }
            }

            // TODO: This is only relevant if we're posting to twitter
            if (IsTweetTooLongWhenShrinked())
            {
                MessageBox.Show("The update is too long. It must be 140 characters or fewer (NB: urls will be shrunk by Twitter to t.co urls).", "too long", MessageBoxButton.OK);
                return;
            }

            // Lets post it to twitter!
            AccountPostTotal = PostingAccounts.Count();

            if (AccountPostTotal == 0)
            {
                MessageBox.Show("You need to select which account(s) to post with.", "Post Tweet", MessageBoxButton.OK);
                return;
            }

            foreach (var button in ApplicationBar.Buttons)
            {
                if (button is ApplicationBarIconButton)
                    ((ApplicationBarIconButton)button).IsEnabled = false;
            }


            txtTweet.IsReadOnly = true;
            ApplicationBar.IsMenuEnabled = false;

            if (hasWorkingImage)
            {

                var bi = new BitmapImage();
                bi.CreateOptions = BitmapCreateOptions.None;
                bi.SetSource(ImageHost.ImageStream);

                var imageHeight = bi.PixelHeight;
                var imageWidth = bi.PixelWidth;

                Debug.WriteLine(string.Format("{0} x {1}", imageWidth, imageHeight));

                ImageHost.ImageStream.Close();
                ImageHost.ImageStream.Dispose();

                bi = null;

                // then we can pick out a filter

                var imageStream = new StreamImageSource(ImageHost.ImageStream);

                var cartoonEffect = new FilterEffect(imageStream)
                                    {
                                        Filters = new[] { filterSelect.SelectedFilter }
                                    };

                var imageBitmap = new WriteableBitmap(imageWidth, imageHeight);

                // Render the image to a WriteableBitmap.
                var renderer = new WriteableBitmapRenderer(cartoonEffect, imageBitmap);

                imageBitmap = await renderer.RenderAsync();

                ImageHost.ImageStream.Close();
                ImageHost.ImageStream.Dispose();

                ImageHost.UpdateImage(imageBitmap, imageWidth, imageHeight);

                UiHelper.ShowProgressBar("uploading image");

                string resultUrl;

                try
                {
                    resultUrl = await ImageHost.UploadImage(PostingAccounts.First().Id, "MehdohImage_" + DateTime.Now.ToString("yyMMddHHmmss") + ".jpg");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Sorry! Looks like there was a problem uploading the image.", "error uploading image", MessageBoxButton.OK);
                    UiHelper.HideProgressBar();
                    ErrorLogger.LogException(ex);
                    return;
                }

                // todo check for error, assume best for now
                newText += " " + resultUrl;

                UiHelper.HideProgressBar();
            }

            if (IsDM)
            {

                UiHelper.ShowProgressBar("sending direct message");

                foreach (var profile in PostingAccounts)
                {
                    var api = new TwitterApi(profile.Id);
                    await api.SendNewMessage(ReplyToAuthor, txtTweet.Text);
                    api_PostNewTweetCompletedEvent(profile.Id, api.HasError, api.ErrorMessage);
                }

            }
            else
            {

                UiHelper.ShowProgressBar("posting status update");

                foreach (var profile in PostingAccounts)
                {

                    try
                    {
                        if (profile.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter)
                        {
                            var api = new TwitterApi(profile.Id);

                            if (!string.IsNullOrEmpty(ReplyToAuthor) && ReplyToId > 0)
                            {
                                if (ImageHost.IsTwitter)
                                    await api.ReplyToTweet(newText, ReplyToId, Position.Latitude, Position.Longitude, PlaceId, ImageHost.ImageStream);
                                else
                                    await api.ReplyToTweet(newText, ReplyToId, Position.Latitude, Position.Longitude, PlaceId);
                            }
                            else
                            {
                                if (ImageHost.IsTwitter)
                                    await api.PostNewTweet(newText, Position.Latitude, Position.Longitude, PlaceId, ImageHost.ImageStream);
                                else
                                    await api.PostNewTweet(newText, Position.Latitude, Position.Longitude, PlaceId);
                            }

                            api_PostNewTweetCompletedEvent(profile.Id, api.HasError, api.ErrorMessage);

                        }

                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException("PostTheTweet", ex);
                        UiHelper.SafeDispatch(() => MessageBox.Show("There was an error while tweeting."));
                    }
                }

            }

            if (hasWorkingImage)
            {
                ImageHost.Dispose();
            }

        }

        private static void ShowWaitWhileImageLoads()
        {
            MessageBox.Show("Please wait until the current image has finished uploading.", "image uploading", MessageBoxButton.OK);
        }

        private static DoubleAnimation CreateAnimation(double from, double to, double duration,
                                                       PropertyPath propertyPath, DependencyObject target, TimeSpan? beginTime = null)
        {
            var db = new DoubleAnimation
            {
                To = to,
                From = from,
                EasingFunction = new SineEase(),
                Duration = TimeSpan.FromSeconds(duration),
            };

            if (beginTime != null)
                db.BeginTime = beginTime;

            Storyboard.SetTarget(db, target);
            Storyboard.SetTargetProperty(db, propertyPath);
            return db;
        }

        void api_PostNewTweetCompletedEvent(long accountId, bool hasError, string errorMessage)
        {

            AccountPostTotal--;

            if (AccountPostTotal > 0)
                return;

#if WP8
            if (TerminateOnPost)
                Application.Current.Terminate();
#endif

            UiHelper.HideProgressBar();

            if (hasError)
            {
                UiHelper.SafeDispatch(async () =>
                                          {

                                              if (!string.IsNullOrWhiteSpace(errorMessage))
                                              {
                                                  errorMessage = "There was a problem posting the tweet. Would you like to try again?\n\nTwitter response: " + errorMessage;
                                              }
                                              else
                                              {
                                                  errorMessage = "There was a problem posting the tweet. Would you like to try again?";
                                              }

                                              var res = MessageBox.Show(errorMessage, "twitter problem", MessageBoxButton.OKCancel);
                                              if (res == MessageBoxResult.OK)
                                              {
                                                  // do it again
                                                  await PostTheTweet();
                                              }
                                          });

                UiHelper.SafeDispatch(() =>
                {
                    ApplicationBar.IsMenuEnabled = true;
                    txtTweet.IsReadOnly = false;

                    foreach (var button in ApplicationBar.Buttons)
                    {
                        if (button is ApplicationBarIconButton)
                            ((ApplicationBarIconButton)button).IsEnabled = true;
                    }

                });

                return;
            }

            TweetPosted = true;

            ProcessPostComplete();

        }

        private void ProcessPostComplete()
        {

            Timeline = null;

            UiHelper.SafeDispatch(() =>
            {

                if (!NavigationService.CanGoBack)
                {
                    NavigateBack();
                }
                else
                {
                    animateOut();
                }
            });

        }

        private void animateOut()
        {

            // Capture screen
            var bmp = new WriteableBitmap((int)ActualWidth, (int)ActualHeight);
            bmp.Render(this, null);
            bmp.Invalidate();

            popUp = new Popup();

            var canvas = new Canvas
                             {
                                 Width = ActualWidth,
                                 Height = ActualHeight
                             };
            //canvas.Background = new SolidColorBrush(Colors.Purple);

            img = new Image
                      {
                          Width = ActualWidth,
                          Height = ActualHeight,
                          Source = bmp,
                          VerticalAlignment = VerticalAlignment.Top
                      };

            img.SetValue(VerticalAlignmentProperty, VerticalAlignment.Top);

            img.SetValue(Canvas.LeftProperty, 0.0);
            img.SetValue(Canvas.TopProperty, 0.0);

            canvas.Children.Add(img);

            // Set the child to the screen
            popUp.Child = canvas;

            Visibility = Visibility.Collapsed;

            popUp.IsOpen = true;

            // Now animate it
            var sb = new Storyboard
            {
                BeginTime = TimeSpan.FromSeconds(0)
            };

            const double duration = 0.3;

            sb.Children.Add(CreateAnimation(0, 100, duration, new PropertyPath(Canvas.LeftProperty), img));
            sb.Children.Add(CreateAnimation(0, 0, duration, new PropertyPath(Canvas.TopProperty), img));
            sb.Children.Add(CreateAnimation(img.ActualWidth, (img.ActualWidth / 3) * 2, duration, new PropertyPath(WidthProperty), img));

            sb.Duration = new Duration(TimeSpan.FromSeconds(duration));
            sb.Completed += sb_Completed;
            sb.Begin();

        }

        void sb_Completed(object sender, EventArgs e)
        {
            // now throw it off the screen!

            var sb = new Storyboard
            {
                BeginTime = TimeSpan.FromSeconds(0)
            };

            //sb.Children.Add(CreateAnimation(0, -500, 0.3, "(Canvas.Top)", img));
            sb.Children.Add(CreateAnimation(0, -500, 0.3, new PropertyPath(Canvas.TopProperty), img));

            sb.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            sb.Completed += sb2_Completed;
            sb.Begin();

        }

        private void sb2_Completed(object sender, EventArgs e)
        {
            popUp.IsOpen = false;

            Dispatcher.BeginInvoke(() =>
                                       {
                                           UiHelper.HideProgressBar();

                                           var sh = new SettingsHelper();
                                           if (sh.GetReturnToTimeline())
                                           {
                                               UiHelper.RemoveBackStackUntil("MainPage.xaml", NavigationService);
                                           }

                                           if (NavigationService.CanGoBack)
                                               NavigationService.GoBack();


                                       });

        }

        private void NavigateBack()
        {
            Dispatcher.BeginInvoke(delegate
                                       {
                                           UiHelper.HideProgressBar();
                                           if (NavigationService.CanGoBack)
                                               NavigationService.GoBack();
                                           else
                                           {
                                               UiHelper.ShowToast("tweet has been posted");
                                               ResetState();
                                           }
                                       });
        }

        private void ResetState()
        {
            OtherAuthorsModel = new ObservableCollection<OtherAuthorsViewModel>();
            txtTweet.Text = string.Empty;
            txtLocation.Text = string.Empty;
            stackLocation.Visibility = Visibility.Collapsed;

            stackImages.Children.Clear();

            PlaceId = null;
            TweetPosted = false;

            ApplicationBar.IsMenuEnabled = true;
            txtTweet.IsReadOnly = false;

            filterSelect.ClearImages();

            if (ImageHost != null)
            {
                ImageHost.Dispose();
                ImageHost = null;
            }

            foreach (var button in ApplicationBar.Buttons)
            {
                if (button is ApplicationBarIconButton)
                    ((ApplicationBarIconButton)button).IsEnabled = true;
            }

            // Scroll to the top

        }

        private void mnuCamera_Click(object sender, EventArgs e)
        {

            if (ShowingPhotoChooser)
                return;

            ShowingPhotoChooser = true;

            var t = new MediaLibrary();
            var cameraRoll = t.Pictures.OrderByDescending(x => x.Date).Where(x => x.Album.Name == "Camera Roll").Take(2).ToList();

            var count = cameraRoll.Count();

            if (count > 0)
            {
                filterSelect.ClearImages();
                stackImages.Children.Clear();

                var sb = new Storyboard();
                sb.Duration = TimeSpan.FromSeconds(1);
                double delay = 0;

                foreach (var photo in cameraRoll)
                {
                    var image = new Image()
                    {
                        Width = 150,
                        Height = 150,
                        Stretch = Stretch.UniformToFill
                    };

                    var thumbnailBitmap = new BitmapImage();
                    thumbnailBitmap.SetSource(photo.GetThumbnail());

                    image.Source = thumbnailBitmap;

                    var button = new Button
                    {
                        BorderBrush = new SolidColorBrush(Colors.Transparent),
                        BorderThickness = new Thickness(3),
                        Margin = new Thickness(0, 0, 5, 0),
                        Padding = new Thickness(0),
                        Content = image,
                        Tag = photo.Name,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Opacity = 0,
                        RenderTransform = new TranslateTransform()
                    };

                    button.Click += async delegate(object sender1, RoutedEventArgs e1)
                    {
                        var senderButton = sender1 as Button;
                        var photoName = (string)(senderButton.Tag);
                        var mediaLib = new MediaLibrary();
                        var picture = mediaLib.Pictures.FirstOrDefault(x => x.Name == photoName);
                        if (picture != null)
                        {
                            await ProcessChosenImage(picture.GetImage());
                            ShowingPhotoChooser = false;
                        }
                        else
                        {
                            ShowPhotoPicker();
                        }
                    };

                    sb.Children.Add(CreateAnimation(-30, 0, 0.5, new PropertyPath(TranslateTransform.XProperty), button.RenderTransform, TimeSpan.FromSeconds(delay)));
                    sb.Children.Add(CreateAnimation(0, 1, 0.5, new PropertyPath(OpacityProperty), button, TimeSpan.FromSeconds(delay)));

                    delay += 0.3;

                    stackImages.Children.Add(button);
                }

                var moreButton = new RoundButton
                                                {
                                                    ImageSource = new BitmapImage(new Uri("/Images/76x76/light/overflowdots.png", UriKind.Relative)),
                                                    VerticalAlignment = VerticalAlignment.Center,
                                                    Opacity = 0,
                                                    RenderTransform = new TranslateTransform()
                                                };

                moreButton.Click += (o, args) => ShowPhotoPicker();
                stackImages.Children.Add(moreButton);
                moreButton.Focus();

                sb.Children.Add(CreateAnimation(-20, 0, 0.5, new PropertyPath(TranslateTransform.XProperty), moreButton.RenderTransform, TimeSpan.FromSeconds(0.6)));
                sb.Children.Add(CreateAnimation(0, 1, 0.5, new PropertyPath(OpacityProperty), moreButton, TimeSpan.FromSeconds(0.6)));

                sb.Begin();

            }
            else
            {
                ShowPhotoPicker();
            }
        }

        private void ShowPhotoPicker()
        {
            var shu = new StorageHelperUI();
            shu.SaveContentsToFile("chosenaccounts.json", PostingAccounts);

            PhotoChooserTask = new PhotoChooserTask
            {
                ShowCamera = true
            };
            PhotoChooserTask.Completed += photoCaptureOrSelectionCompleted;
            PhotoChooserTask.Show();
        }

        protected bool ShowingPhotoChooser { get; set; }

        private async void photoCaptureOrSelectionCompleted(object sender, PhotoResult e)
        {

            var shu = new StorageHelperUI();
            PostingAccounts = shu.LoadContentsFromFile<ObservableCollection<AccountViewModel>>("chosenaccounts.json");

            ShowHideAccountButton();

            ShowingPhotoChooser = false;

            if (e.TaskResult != TaskResult.OK)
                return;

            await ProcessChosenImage(e.ChosenPhoto);
        }

        private async Task ProcessChosenImage(Stream stream)
        {
            stackImages.Children.Clear();

            ImageHost.StoreImage(stream);

            UpdateCount();

            foreach (ApplicationBarIconButton button in ApplicationBar.Buttons)
            {
                button.IsEnabled = false;
            }

            await filterSelect.PrepImages(stream);

            foreach (ApplicationBarIconButton button in ApplicationBar.Buttons)
            {
                button.IsEnabled = true;
            }

        }

        // Handle selection changed on ListBox
        private void lstTimeline_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
            if (lstTimeline.SelectedItem == null)
                return;

            var item = lstTimeline.SelectedItem as TimelineViewModel;
            if (item == null)
                return;

            string query;

            if (IsDM)
                query = string.Format("messageId={0}&accountId={1}", item.Id, item.AccountId);
            else
                query = string.Format("id={0}&accountId={1}", item.Id, item.AccountId);

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/DetailsPage.xaml?" + query, UriKind.Relative));

            // Reset selected index to -1 (no selection)
            lstTimeline.SelectedItem = null;

        }

        private void mnuGeoTag_Click(object sender, EventArgs e)
        {

            var sh = new SettingsHelper();
            if (sh.GetLocationServicesEnabled() == false)
            {
                MessageBox.Show("You have currently disabled location services within Mehdoh.\n\nIf you would like to re-enable them then please visit the 'location' section in settings.", "location services", MessageBoxButton.OK);
                return;
            }

            // If there is already geotagging data, then remove it.
            if (!Equals(Position.Longitude, 0.0) && !Equals(Position.Latitude, 0.0))
            {

                Position = new GeoCoordinate(0, 0);
                PlaceId = string.Empty;

                txtLocation.Text = string.Empty;
                stackLocation.Visibility = Visibility.Collapsed;

            }
            else
            {

                if (CurrentAccountSettings != null && !CurrentAccountSettings.geo_enabled)
                {
                    MessageBox.Show("Tweet Location is currently disabled on your account. In order for locations to show up on your tweets you will need to enable them via Twitter.com.\n\nYou may still post this tweet with the location information however the location may not show up.", "location services", MessageBoxButton.OK);
                }

                SystemTray.ProgressIndicator = new ProgressIndicator
                {
                    IsVisible = true,
                    IsIndeterminate = true,
                    Text = "obtaining location"
                };

                StartGetLocation();

            }

        }

        private void StartGetLocation()
        {
            LocationWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            LocationWatcher.PositionChanged += LocationWatcher_PositionChanged;
            LocationWatcher.Start();
            IsFirstGeoLocation = true;
        }

        void LocationWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {

            if (IsFirstGeoLocation)
            {
                IsFirstGeoLocation = false;
                return;
            }

            UiHelper.SafeDispatch(() =>
            {
                txtLocation.Text = string.Format("{0},{1}", e.Position.Location.Latitude.ToString(CultureInfo.CurrentUICulture), e.Position.Location.Longitude.ToString(CultureInfo.CurrentUICulture));
                stackLocation.Visibility = Visibility.Visible;
            });

            Position = e.Position.Location;

            LocationWatcher.Stop();

            var api = new TwitterApi(AccountId);
            api.ReverseGeocodeCompletedEvent += api_ReverseGeocodeCompletedEvent;
            api.ReverseGeocode(Position.Latitude, Position.Longitude);            
        }

        void api_ReverseGeocodeCompletedEvent(object sender, EventArgs e)
        {

            UiHelper.SafeDispatch(() =>
                            SystemTray.ProgressIndicator = new ProgressIndicator
                                {
                                    IsVisible = false,
                                    IsIndeterminate = false
                                }
                );

            var api = sender as TwitterApi;

            if (api == null)
                return;

            api.ReverseGeocodeCompletedEvent -= api_ReverseGeocodeCompletedEvent;

            if (api.Geocode == null)
                return;

            if (api.Geocode.result == null || api.Geocode.result.places == null)
                return;

            PlaceId = api.Geocode.result.places[0].id;

            UiHelper.SafeDispatch(() =>
                                      {
                                          try
                                          {
                                              string fullName = api.Geocode.result.places[0].full_name;
                                              string country = api.Geocode.result.places[0].country_code;
                                              LocationName = api.Geocode.result.places[0].name;
                                              txtLocation.Text = string.Format("{0}, {1}", fullName, country);
                                          }
                                          catch (Exception)
                                          {
                                          }
                                      });

        }

        private void lstOthers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            // If selected index is -1 (no selection) do nothing
            if (lstOthers.SelectedIndex == -1)
                return;

            var item = lstOthers.SelectedValue as OtherAuthorsViewModel;
            if (item == null)
                return;

            if (NeedsInsertSpace)
                txtTweet.Text = txtTweet.Text.Insert(txtTweet.SelectionStart, " " + item.Text);
            else
                txtTweet.Text = txtTweet.Text.Insert(txtTweet.SelectionStart, item.Text);

            NeedsInsertSpace = true;

            txtTweet.SelectionStart = txtTweet.Text.Length;

            //txtTweet.Text = txtTweet.Text.Trim() + " " + item.Text + " ";

            txtTweet.Focus();

            lstOthers.SelectedIndex = -1;

        }

        private void mnuQuickMention_Click(object sender, EventArgs e)
        {
            QuickMentionStartPosition = txtTweet.SelectionStart;

            string character = "@";

            try
            {
                if (txtTweet.SelectionStart > 0)
                {
                    var lastPost = txtTweet.SelectionStart - 1;
                    if (txtTweet.Text.Substring(lastPost, 1) != " ")
                        character = " @";
                }
            }
            catch (Exception)
            {
            }

            txtTweet.Text = txtTweet.Text.Insert(QuickMentionStartPosition, character);
            txtTweet.SelectionStart = QuickMentionStartPosition + character.Length;
            EnableQuickMetion();
            txtTweet.Focus();
        }

        private void EnableQuickHashtag()
        {
            IsQuickHashtagEnabled = true;
            QuickHashtagStartPosition = txtTweet.SelectionStart;
            lstQuick.Visibility = Visibility.Visible;
        }

        private void EnableQuickMetion()
        {
            IsQuickMentionEnabled = true;
            QuickMentionStartPosition = txtTweet.SelectionStart;
            lstQuick.Visibility = Visibility.Visible;
        }

        private void DisableQuickHashtag()
        {
            IsQuickHashtagEnabled = false;
            QuickHashtagStartPosition = -1;
            lstQuick.Visibility = Visibility.Collapsed;
        }

        private void DisableQuickMention()
        {
            IsQuickMentionEnabled = false;
            QuickMentionStartPosition = -1;
            lstQuick.Visibility = Visibility.Collapsed;
        }

        private void TextBlock_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            var textBlock = sender as TextBlock;

            if (textBlock == null)
                return;

            int quickPosition = IsQuickMentionEnabled ? QuickMentionStartPosition : QuickHashtagStartPosition;

            // At the end or in the middle?
            if (txtTweet.Text.IndexOf(" ", quickPosition - 1, StringComparison.Ordinal) < 0)
            {
                // end
                var endBit = txtTweet.Text.Substring(quickPosition - 1);
                Dispatcher.BeginInvoke(() =>
                {
                    //txtTweet.Text = txtTweet.Text.Replace(endBit, textBlock.Text);
                    txtTweet.Text = txtTweet.Text.Remove(quickPosition - 1, endBit.Length);
                    txtTweet.Text = txtTweet.Text.Insert(quickPosition - 1, textBlock.Text + " ");
                    txtTweet.SelectionStart = quickPosition + textBlock.Text.Length;
                    txtTweet.Focus();

                    if (IsQuickMentionEnabled)
                        DisableQuickMention();
                    if (IsQuickHashtagEnabled)
                        DisableQuickHashtag();

                });

            }
            else
            {
                // middle
                var endBit = txtTweet.Text.Substring(quickPosition - 1);
                var length = endBit.IndexOf(" ", System.StringComparison.Ordinal);
                endBit = txtTweet.Text.Substring(quickPosition - 1, length);

                Dispatcher.BeginInvoke(() =>
                                           {
                                               txtTweet.Text = txtTweet.Text.Replace(endBit, textBlock.Text);
                                               txtTweet.SelectionStart = quickPosition + textBlock.Text.Length;
                                               txtTweet.Focus();

                                               if (IsQuickMentionEnabled)
                                                   DisableQuickMention();
                                               if (IsQuickHashtagEnabled)
                                                   DisableQuickHashtag();
                                           });
            }

        }

        private void ScrollViewer_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            e.Handled = true;
        }

        private async void mnuNowPlaying_Click(object sender, EventArgs e)
        {

            if (LicenceInfo.IsTrial())
            {

                const string message = "This feature is only available in the full version of Mehdoh.\n\nIt will tweet the title and artist of the song you're currently listening to.\n\nWould you like to upgrade to the full version now?";

                if (MessageBox.Show(message, "now playing", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    try
                    {
                        var marketplaceTask = new MarketplaceDetailTask();
                        marketplaceTask.Show();
                    }
                    catch
                    {
                    }
                }
                return;
            }

            try
            {
                string nowPlaying;

                if (MediaPlayer.State == MediaState.Playing)
                {

                    if (MediaPlayer.Queue.ActiveSong.Artist != null && !string.IsNullOrEmpty(MediaPlayer.Queue.ActiveSong.Artist.Name))
                    {
                        nowPlaying = "#nowplaying " + MediaPlayer.Queue.ActiveSong.Artist + " - " + MediaPlayer.Queue.ActiveSong.Name;
                    }
                    else
                    {
                        nowPlaying = "#nowplaying " + MediaPlayer.Queue.ActiveSong.Name;
                    }

                    txtTweet.Text = txtTweet.Text + nowPlaying;
                    txtTweet.SelectionStart = txtTweet.Text.Length;

                    if (HasArt() && (MessageBox.Show("would you like to include the album art in your tweet?", "album art", MessageBoxButton.OKCancel) == MessageBoxResult.OK))
                    {
                        using (var albumArt = GetAlbumArt())
                        {
                            if (albumArt != null)
                            {
                                await ProcessChosenImage(albumArt);
                            }
                        }
                    }

                    txtTweet.Focus();

                    if (IsPlayingMusic())
                    {
                        string genre = GetMusicGenre();

                        if (!string.IsNullOrEmpty(genre))
                            OtherAuthorsModel.Add(new OtherAuthorsViewModel("#" + genre));

                        OtherAuthorsModel.Add(new OtherAuthorsViewModel("#nowplaying"));
                        OtherAuthorsModel.Add(new OtherAuthorsViewModel("#music"));
                        OtherAuthorsModel.Add(new OtherAuthorsViewModel("\u266A"));
                        OtherAuthorsModel.Add(new OtherAuthorsViewModel("\u266B"));
                        OtherAuthorsModel.Add(new OtherAuthorsViewModel("\u260A"));
                    }


                }
                else if (BackgroundAudioPlayer.Instance != null && (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing))
                {

                    nowPlaying = "I'm currently listening to " + BackgroundAudioPlayer.Instance.Track.Title + " by " +
                                     BackgroundAudioPlayer.Instance.Track.Artist + " #nowplaying";
                    txtTweet.Text = nowPlaying;
                    txtTweet.SelectionStart = txtTweet.Text.Length;
                    txtTweet.Focus();

                    if (IsPlayingMusic())
                    {
                        string genre = GetMusicGenre();

                        if (!string.IsNullOrEmpty(genre))
                            OtherAuthorsModel.Add(new OtherAuthorsViewModel("#" + genre));

                        OtherAuthorsModel.Add(new OtherAuthorsViewModel("#nowplaying"));
                        OtherAuthorsModel.Add(new OtherAuthorsViewModel("#music"));
                        OtherAuthorsModel.Add(new OtherAuthorsViewModel("\u266A"));
                        OtherAuthorsModel.Add(new OtherAuthorsViewModel("\u266B"));
                        OtherAuthorsModel.Add(new OtherAuthorsViewModel("\u260A"));
                    }

                }
                else
                {
                    MessageBox.Show("You're not currently playing any music.", "now playing", MessageBoxButton.OK);
                }

            }
            catch (Exception)
            {
                MessageBox.Show("Sorry. Unable to work out what you're currently listening to.");
            }

        }

        private void mnuPinToStart_Click(object sender, EventArgs e)
        {
            ShellHelperUi.PinNewTweet();
        }

        private void txtTweet_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            NeedsInsertSpace = false;
        }

        private void HeaderImage_Tap(object sender, GestureEventArgs gestureEventArgs)
        {
            ShowSelectAccount();
        }

        private void ShowSelectAccount()
        {

            ApplicationBar.IsVisible = false;

            var allowedAccounts = new List<long>();

            if (IsDM)
            {

                string temp;
                if (NavigationContext.QueryString.TryGetValue("allowedAccountIds", out temp))
                {
                    var ids = temp.Split('|');
                    allowedAccounts.AddRange(ids.Select(long.Parse));
                }

            }

            List<AccountFriendViewModel> accountIds = PostingAccounts.Select(acccount => new AccountFriendViewModel()
                                                                 {
                                                                     Id = acccount.Id,
                                                                     ScreenName = acccount.ScreenName,
                                                                     StatusChecked = true,
                                                                     IsFriend = true
                                                                 }).ToList();

            var post = new SelectTwitterAccount();
            post.ExistingValues = accountIds;
            post.CheckPressed += new EventHandler(post_CheckPressed);

            if (allowedAccounts.Any())
            {
                post.AllowedIds = allowedAccounts;
                post.UpdateAllowed();
            }

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
                    if (!PostingAccounts.Any(x => x.Id == account.Id))
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

            // this decides to show the + button or not
            ShowHideAccountButton();

            HideSelectAccountPopup();

        }

        private void HideSelectAccountPopup()
        {
            UiHelper.SafeDispatch(() =>
            {
                EventHandler selectAccountPopupClosed = (sender, args) =>
                {
                    SelectAccountPopup = null;
                    ApplicationBar.IsVisible = true;
                    txtTweet.Focus();
                };
                SelectAccountPopup.Closed += new EventHandler(selectAccountPopupClosed);
                SelectAccountPopup.Hide();
            });
        }

        private void mnuSelectAccount_Click(object sender, EventArgs e)
        {
            ShowSelectAccount();
        }

        #region URL Shrinking

        private int GetShrunkenLength()
        {
            var originalText = txtTweet.Text;

            // replace HTTP urls
            const string shortHttpUrl = "http://t.co/xxxxxxxxxx";

            var results = GetHttpUrlsFromText();
            if (results.Count > 0)
            {
                foreach (var result in results)
                {
                    originalText = originalText.Replace(result.ToString(), shortHttpUrl);
                }
            }

            // Now replace HTTPS urls
            const string shortHttpsUrl = "https://t.co/xxxxxxxxxx";

            var httpsResults = GetHttpsUrlsFromText();
            if (httpsResults.Count > 0)
            {
                foreach (var result in httpsResults)
                {
                    originalText = originalText.Replace(result.ToString(), shortHttpsUrl);
                }
            }

            return originalText.Length;
        }

        private bool IsTweetTooLongWhenShrinked()
        {
            // 140 is the max length for tweets
            return GetShrunkenLength() > 140;
        }

        private MatchCollection GetHttpUrlsFromText()
        {
            var urlRegex = new Regex(@"((http):\/\/)([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?", RegexOptions.IgnoreCase);
            string tweetText = txtTweet.Text;
            var results = urlRegex.Matches(tweetText);
            return results;
        }

        private MatchCollection GetHttpsUrlsFromText()
        {
            var urlRegex = new Regex(@"((https):\/\/)([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?", RegexOptions.IgnoreCase);
            string tweetText = txtTweet.Text;
            var results = urlRegex.Matches(tweetText);
            return results;
        }

        private Dictionary<string, string> ShortenedUrls = new Dictionary<string, string>();

        private int UrlsToShorten { get; set; }

        private void mnuShortenUrls_Click(object sender, EventArgs e)
        {
            ShortenUrls();
        }

        private void ShortenUrls()
        {

            var httpUrls = GetHttpUrlsFromText();
            var httpsUrls = GetHttpsUrlsFromText();

            var urls = new List<string>();

            foreach (var url in httpUrls)
                urls.Add(url.ToString());

            foreach (var url in httpsUrls)
                urls.Add(url.ToString());

            if (urls.Count > 0)
            {

                UiHelper.ShowProgressBar("shortening urls");

                UrlsToShorten = urls.Count;

                foreach (var url in urls)
                {
                    // is this a long or a short url?
                    if (ShortenedUrls.ContainsKey(url) || ShortenedUrls.ContainsValue(url))
                        continue;

                    var api = new BitlyApi();
                    api.GetShortUrlCompletedEvent += OnApiOnGetShortUrlCompletedEvent;
                    api.GetShortUrl(url);
                }
            }

        }

        private void OnApiOnGetShortUrlCompletedEvent(object sender1, EventArgs e1)
        {
            var bitlyApi = sender1 as BitlyApi;
            try
            {
                if (!string.IsNullOrWhiteSpace(bitlyApi.ShortUrl))
                {
                    ShortenedUrls.Add(bitlyApi.LongUrl, bitlyApi.ShortUrl);
                    UiHelper.SafeDispatch(() =>
                                              {
                                                  try
                                                  {
                                                      txtTweet.Text = txtTweet.Text.Replace(bitlyApi.LongUrl, bitlyApi.ShortUrl);
                                                  }
                                                  catch (Exception)
                                                  {
                                                  }
                                              });
                }
            }
            finally
            {
                if (--UrlsToShorten == 0)
                {
                    UiHelper.HideProgressBar();
                }
            }
        }

        #endregion

        /// <summary>
        /// When something inside of the template changes, then we need to invalidate the measure of the ancestor PivotHeadersControl
        /// </summary>
        private void OnHeaderSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var fe = (FrameworkElement)sender;
            while (fe != null)
            {
                if (fe is PivotHeadersControl)
                {
                    fe.InvalidateMeasure();
                    break;
                }
                fe = VisualTreeHelper.GetParent(fe) as FrameworkElement;
            }
        }

        private void PivotMain_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetAppBarMenu();
        }
    }

}