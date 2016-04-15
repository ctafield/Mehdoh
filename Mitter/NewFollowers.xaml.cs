using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.Animations.Page.Extensions;
using FieldOfTweets.Common.UI.Friends;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace Mitter
{
    public partial class NewFollowers : AnimatedBasePage
    {
        public NewFollowers()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            LoadedData = false;

            CurrentCursor = -1;
        }

        private string UserScreen { get; set; }
        private long AccountId { get; set; }
        private bool LoadedData { get; set; }

        private int CurrentCursor { get; set; }

        /// <summary>
        /// This is the total friends count
        /// </summary>
        private long FriendsCount { get; set; }

        /// <summary>
        /// This is the current number of friends counted in on the refresh
        /// </summary>
        private long FriendsCounted { get; set; }

        /// <summary>
        /// This is what we are expecting on the current refresh
        /// </summary>
        private long FriendsCountExpected { get; set; }

        private ObservableCollection<FriendViewModel> ViewModel { get; set; }

        private ObservableCollection<FriendViewModel> _filteredViewModel;

        private ObservableCollection<FriendViewModel> FilteredViewModel
        {
            get
            {
                if (ViewModel == null)
                    return null;

                if (string.IsNullOrEmpty(txtFilter.Text.Trim()))
                    return ViewModel;

                var results = ViewModel.Where(
                        x =>
                        x.ScreenName.ToLower().Contains(txtFilter.Text) ||
                        x.DisplayName.ToLower().Contains(txtFilter.Text));

                if (_filteredViewModel == null)
                    _filteredViewModel = new ObservableCollection<FriendViewModel>();

                _filteredViewModel.Clear();
                _filteredViewModel.AddRange(results);
                return _filteredViewModel;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            base.OnNavigatedTo(e);

            if (!UiHelper.ValidateUser())
            {
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                return;
            }

            UiHelper.SelectedUser = string.Empty;

            string mode;
            NavigationContext.QueryString.TryGetValue("mode", out mode);

            string userScreen;
            if (!NavigationContext.QueryString.TryGetValue("screen", out userScreen))
                NavigationService.GoBack();

            AccountId = UiHelper.GetAccountId(NavigationContext);

            UserScreen = userScreen;

            if (LoadedData)
                return;

            InitialiseViewModel();

            LongFriends.DataContext = FilteredViewModel;

            ThreadPool.QueueUserWorkItem(PopulateFriends);

            LoadedData = true;
        }

        private void InitialiseViewModel()
        {
            ViewModel = new ObservableCollection<FriendViewModel>();
        }

        private void PopulateFriends(object o)
        {
            UiHelper.ShowProgressBar("retrieving users");

            var api = new TwitterApi(AccountId);

            api.GetFollowersCompletedEvent += api_GetFollowersCompletedEvent;
            api.GetFollowers(UserScreen, CurrentCursor.ToString());
        }

        void api_GetFollowersCompletedEvent(object sender, EventArgs e)
        {
            var api = sender as TwitterApi;
            var friendIds = api.FollowerIds;

            ProcessFriendIds(friendIds);
        }

        private void ProcessFriendIds(List<long> friendIds)
        {
            FriendsCount = friendIds.Count();

            if (FriendsCount == 0)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    txtNoUser.Visibility = Visibility.Visible;
                    LongFriends.Visibility = Visibility.Collapsed;
                    UiHelper.HideProgressBar();
                });
                return;
            }

            Dispatcher.BeginInvoke(() =>
            {
                txtNoUser.Visibility = Visibility.Collapsed;
                LongFriends.Visibility = Visibility.Visible;
            });

            var helper = new FriendsHelper(AccountId);
            helper.GetFriendsCompletedEvent += new EventHandler<GetFriendEventArgs>(helper_GetFriendsCompletedEvent);

            foreach (var friendId in friendIds)
            {
                helper.AddFriendToSearch(friendId);
            }

            FriendsCountExpected = helper.UnknownFriends.Count;

            if (helper.UnknownFriends.Count > 0)
                helper.GetFriends();
        }

        private void lstFriends_Tap(object sender, GestureEventArgs gestureEventArgs)
        {
            var listBox = sender as ListBox;
            if (listBox == null)
                return;

            var chosenItem = listBox.SelectedItem as FriendViewModel;
            if (chosenItem == null)
                return;

            NavigationService.Navigate(new Uri("/UserProfile.xaml?accountId=" + chosenItem.AccountId + "&screen=" + chosenItem.ScreenName.Replace("@", ""), UriKind.Relative));
        }

        void helper_GetFriendsCompletedEvent(object sender, GetFriendEventArgs e)
        {

            if (e.FriendUser != null)
            {

                Dispatcher.BeginInvoke(delegate()
                {
                    var newModel = new FriendViewModel()
                    {
                        AccountId = AccountId,
                        DisplayName = e.FriendUser.DisplayName,
                        Id = e.FriendUser.Id,
                        ProfileImageUrl = e.FriendUser.ProfileImageUrl,
                        ScreenName = e.FriendUser.ScreenName
                    };

                    ViewModel.Add(newModel);
                    FriendsCounted++;

                    if (FriendsCounted == FriendsCount)
                    {
                        UiHelper.HideProgressBar();
                    }

                });

            }

        }

        private DispatcherTimer AutoSearchTimer { get; set; }

        private void txtTweets_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchPerson(true);
            }
            else
            {
                KillAutoTimer();

                AutoSearchTimer = new DispatcherTimer();
                AutoSearchTimer.Interval = new TimeSpan(0, 0, 1);
                AutoSearchTimer.Tick += delegate
                {
                    SearchPerson(false);
                };
                AutoSearchTimer.Start();
            }

        }

        private void txtTweets_ActionIconTapped(object sender, EventArgs e)
        {
            SearchPerson(true);
        }

        private void KillAutoTimer()
        {
            if (AutoSearchTimer != null)
                AutoSearchTimer.Stop();

            AutoSearchTimer = null;
        }

        private void SearchPerson(bool loseFocus)
        {

            KillAutoTimer();

            LongFriends.DataContext = FilteredViewModel;

            if (loseFocus)
                LongFriends.Focus();
        }

    }

}