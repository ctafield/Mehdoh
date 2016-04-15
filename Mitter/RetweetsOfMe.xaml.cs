using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ViewModels;
using Mitter.Helpers;
using HttpUtility = System.Web.HttpUtility;

namespace Mitter
{
    public partial class RetweetsOfMe : AnimatedBasePage
    {

        private bool DataLoaded { get; set; }
        private ObservableCollection<FriendViewModel> Users;
        private long AccountId { get; set; }

        public RetweetsOfMe()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;

            Users = new ObservableCollection<FriendViewModel>();
            lstUsers.DataContext = Users;

            Loaded += new RoutedEventHandler(RetweetsOfMe_Loaded);
        }

        void RetweetsOfMe_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private long Id;
        private TimelineViewModel Tweet { get; set; }
        protected long CurrentPage { get; set; }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (DataLoaded)
                return;

            string id;

            if (!NavigationContext.QueryString.TryGetValue("id", out id))
                NavigationService.GoBack();

            if (id == null)
                NavigationService.GoBack();

            if (!long.TryParse(id, out Id))
                return;

            AccountId = UiHelper.GetAccountId(NavigationContext);

            GetTweetDetails();

            ppBar.Visibility = Visibility.Visible;
            ppBar.IsIndeterminate = true;
            lstUsers.Visibility = Visibility.Collapsed;

        }

        private void GetTweetDetails()
        {

            // Get the tweet details itself from the reweet table
            using (var dh = new MainDataContext())
            {
                var results = from t in dh.RetweetsOfMe
                              where t.Id == Id
                              select new TimelineViewModel
                                         {
                                             ScreenName = t.ScreenName,
                                             DisplayName = t.DisplayName,
                                             CreatedAt = t.CreatedAt,
                                             Description =
                                                 string.IsNullOrEmpty(t.RetweetDescription)
                                                     ? HttpUtility.HtmlDecode(t.Description)
                                                     : HttpUtility.HtmlDecode(t.RetweetDescription),
                                             ImageUrl = t.ProfileImageUrl,
                                             Id = t.Id,
                                             RetweetUserDisplayName =
                                                 t.RetweetUserDisplayName,
                                             RetweetUserImageUrl = t.RetweetUserImageUrl,
                                             RetweetUserScreenName =
                                                 t.RetweetUserScreenName,
                                             IsRetweet = t.IsRetweet,
                                             AccountId = AccountId
                                             //Urls = t.Assets.ToList(),                                            
                                         };

                Tweet = results.FirstOrDefault();

                if (Tweet != null)
                {
                    gridTweet.DataContext = Tweet;
                    // Now get the retweet count
                    ThreadPool.QueueUserWorkItem(GetRetweetingUsers, Tweet);
                }
                else
                {
                    LoadTweet();
                }

            }

        }

        private async void LoadTweet()
        {
            var api = new TwitterApi(AccountId);
            var result = await api.GetTweet(Id);
            api_GetTweetCompletedEvent(result);
        }


        void api_GetTweetCompletedEvent(ResponseTweet status)
        {

            var tweet = new TimelineViewModel
            {
                ScreenName = status.user.screen_name,
                DisplayName = status.user.name,
                Description = HttpUtility.HtmlDecode(status.text),
                CreatedAt = status.created_at,
                ImageUrl = status.user.profile_image_url,
                Id = status.id,                
                IsRetweet = false
            };

            if (status.retweeted_status != null)
            {
                tweet.Description = HttpUtility.HtmlDecode(status.retweeted_status.text);
                tweet.ImageUrl = status.retweeted_status.user.profile_image_url;
                tweet.ScreenName = status.retweeted_status.user.screen_name;
                tweet.DisplayName = status.retweeted_status.user.name;
                tweet.CreatedAt = status.retweeted_status.created_at;
                tweet.IsRetweet = true;

                Id = status.retweeted_status.id;
            }

            UiHelper.SafeDispatch(() =>
            {
                gridTweet.DataContext = tweet;
            });


            // Now get the retweet count
            ThreadPool.QueueUserWorkItem(GetRetweetingUsers, Tweet);

        }

        private void GetRetweetingUsers(object state)
        {
            var api = new TwitterApi(AccountId);
            api.GetUsersWhoRetweetedTweetCompletedEvent += new EventHandler(api_GetUsersWhoRetweetedTweetCompletedEvent);
            api.GetUsersWhoRetweetedTweet(Id, CurrentPage);
        }


        void api_GetUsersWhoRetweetedTweetCompletedEvent(object sender, EventArgs e)
        {

            var api = sender as TwitterApi;

            if (api.RetweetUsers == null)
            {
                UiHelper.SafeDispatch(ShowNoUsers);
                return;
            }

            UiHelper.SafeDispatch(() =>
            {
                foreach (var item in api.RetweetUsers)
                {
                    Users.Add(new FriendViewModel()
                                  {
                                      DisplayName = item.name,
                                      ScreenName = item.screen_name,
                                      Id = item.id,
                                      ProfileImageUrl = item.profile_image_url
                                  });
                }

                if (!Users.Any())
                {
                    ShowNoUsers();
                }
                else
                {
                    txtRetweetCount.Text = Users.Count.ToString(CultureInfo.InvariantCulture);

                    ppBar.Visibility = Visibility.Collapsed;
                    ppBar.IsIndeterminate = false;
                    lstUsers.Visibility = Visibility.Visible;
                    stackCount.Visibility = Visibility.Visible;
                }

                DataLoaded = true;

            });

        }

        private void ShowNoUsers()
        {
            txtNoUsers.Visibility = Visibility.Visible;
            ppBar.Visibility = Visibility.Collapsed;
            ppBar.IsIndeterminate = false;
            lstUsers.Visibility = Visibility.Collapsed;
            stackCount.Visibility = Visibility.Collapsed;
        }

        private void lstUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstUsers.SelectedIndex == -1)
                return;

            var item = lstUsers.SelectedItem as FriendViewModel;
            if (item == null)
                return;

            var screen = item.ScreenName;

            NavigationService.Navigate(new Uri("/UserProfile.xaml?accountId=" + AccountId + "&screen=" + System.Net.HttpUtility.UrlEncode(screen), UriKind.Relative));

            lstUsers.SelectedIndex = -1;

        }
    }
}