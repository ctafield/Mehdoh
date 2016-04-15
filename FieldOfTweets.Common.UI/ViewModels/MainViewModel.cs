#if DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.ErrorLogging;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Interfaces;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class MainViewModel : INotifyPropertyChanged
    {

        private IMehdohApp App
        {
            get { return ((IMehdohApp) (Application.Current)); }
        }

        private const int InitialCount = 20;
        private const int ChunkCount = 20;

        public Dictionary<string, SortedObservableCollection<TimelineViewModel>> Lists { get; set; }

        // State 
        public ResponseTweet CurrentTweet { get; set; }

        public Dictionary<long, SortedObservableCollection<TimelineViewModel>> Timeline { get; set; }        
        public Dictionary<long, SortedObservableCollection<MessagesViewModel>> Messages { get; set; }
        public Dictionary<long, SortedObservableCollection<FavouritesViewModel>> Favourites { get; set; }
        public Dictionary<long, SortedObservableCollection<MentionsViewModel>> Mentions { get; set; }
        public Dictionary<long, SortedObservableCollection<TimelineViewModel>> RetweetsOfMe { get; set; }

        // Searches
        public Dictionary<long, Dictionary<string, SortedObservableCollection<TimelineViewModel>>> TwitterSearch { get; set; }

        // New Followers
        public Dictionary<long, ObservableCollection<FriendViewModel>> NewFollowers { get; set; }

        // Photo view
        public Dictionary<long, SortedObservableCollection<PhotoViewModel>> PhotoView { get; set; }

        public ObservableCollection<string> ItemCounts { get; set; }

        public MainViewModel()
        {

            // Twitter
            Timeline = new Dictionary<long, SortedObservableCollection<TimelineViewModel>>();
            Mentions = new Dictionary<long, SortedObservableCollection<MentionsViewModel>>();
            Favourites = new Dictionary<long, SortedObservableCollection<FavouritesViewModel>>();
            Messages = new Dictionary<long, SortedObservableCollection<MessagesViewModel>>();
            Lists = new Dictionary<string, SortedObservableCollection<TimelineViewModel>>();
            RetweetsOfMe = new Dictionary<long, SortedObservableCollection<TimelineViewModel>>();

            NewFollowers = new Dictionary<long, ObservableCollection<FriendViewModel>>();
            PhotoView = new Dictionary<long, SortedObservableCollection<PhotoViewModel>>();
            TwitterSearch = new Dictionary<long, Dictionary<string, SortedObservableCollection<TimelineViewModel>>>();

            // This is for the column headers
            ItemCounts = new ObservableCollection<string>();

        }

        public bool IsDataLoaded { get; private set; }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void ProfileImageUpdated()
        {
            NotifyPropertyChanged("ImageUri");
        }

        public void RetweetsOfMe_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("RetweetsOfMe");
        }

        public void TwitterList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Lists");
        }

        private void PhotoView_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("PhotoView");
        }

        public void RetweetedByMe_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("RetweetedByMe");
        }

        public void RetweetedToMe_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("RetweetedToMe");
        }

        public void Messages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Messages");
        }

        public void Favourites_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Favourites");
        }

        public void Mentions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Mentions");
        }

        public void TwitterSearch_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("TwitterSearch");
        }

        public void Timeline_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Timeline");
        }

        public void NewFollowers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("NewFollowers");
        }

        public List<MessagesViewModel> GetMoreMessages(long oldestItem, long accountId)
        {

            using (var dh = new MainDataContext())
            {
                var res = Queryable.Take<MessagesViewModel>((from t in dh.Messages
                              where !Enumerable.Contains((from o in ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId]
                                  select o.Id), t.Id)
                                    && t.ProfileId == accountId
                              orderby t.Id descending
                              select new MessagesViewModel
                              {
                                  AccountId = accountId,
                                  ScreenName = t.ScreenName,
                                  DisplayName = t.DisplayName,
                                  CreatedAt = t.CreatedAt,
                                  Description = t.Description,
                                  ImageUrl = t.ProfileImageUrl,
                                  Id = t.Id
                              }
                              ), ChunkCount).ToList();

                return res;
            }

        }

        public List<FavouritesViewModel> GetMoreFavourites(long oldestItem, long accountId)
        {
            using (var dh = new MainDataContext())
            {
                var res = Queryable.Take<FavouritesViewModel>((from t in dh.Favourites
                              where !Enumerable.Contains((from o in ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId]
                                  select o.Id), t.Id)
                                    && t.ProfileId == accountId
                              orderby t.Id descending
                              select new FavouritesViewModel
                              {
                                  AccountId = accountId,
                                  ScreenName = t.ScreenName,
                                  DisplayName = t.DisplayName,
                                  CreatedAt = t.CreatedAt,
                                  Description = t.Description,
                                  ImageUrl = t.ProfileImageUrl,
                                  Id = t.Id,
                                  RetweetUserDisplayName = t.RetweetUserDisplayName,
                                  RetweetUserImageUrl = t.RetweetUserImageUrl,
                                  RetweetUserScreenName = t.RetweetUserScreenName,
                                  IsRetweet = t.IsRetweet
                              }
                              ), ChunkCount).ToList();

                return res;
            }
        }

        public void MoveStateToDb()
        {

            using (var dh = new MainDataContext())
            {

                if (!dh.DatabaseExists())
                    return;

                var profile = dh.Profiles.FirstOrDefault();
                if (profile == null)
                    return;

                var shellHelper = new ShellHelper();
                shellHelper.ClearShellStatus();
 
                if (dh.Profiles.Any(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                {
                    foreach (var account in dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                    {

                        var dsh = new DataStorageHelper();

                        try
                        {
                            dsh.MoveMessageStateToDb(account.Id);
                        }
                        catch (Exception)
                        {
                            //BugSense.BugSenseHandler.Instance.LogException(ex, "MoveMessageStateToDb");
                        }

                        try
                        {
                            dsh.MoveMentionStateToDb(account.Id);
                        }
                        catch (Exception)
                        {
                            //BugSense.BugSenseHandler.Instance.LogException(ex, "MoveMentionStateToDb");                                
                        }

                    }
                }

            }

            IsDataLoaded = true;
        }


        private void LoadRetweetsOfMe(long accountId)
        {

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmRetweetsOfMe(accountId);

            using (var dh = new MainDataContext())
            {
                ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId].AddRange(Queryable.Take<TimelineViewModel>((from t in dh.RetweetsOfMe
                                                                    where !Enumerable.Contains((from o in ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId]
                                                                        select o.Id), t.Id)
                                                                          && t.ProfileId == accountId
                                                                    orderby t.Id descending
                                                                    select t.AsViewModel(accountId)), InitialCount));
            }

        }

        private void LoadTwitterSearch(long accountId, string value)
        {

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTwitterSearch(accountId, value);

            using (var dh = new MainDataContext())
            {

                var res = Queryable.Select<TwitterSearchTable, TimelineViewModel>((from t in dh.TwitterSearch
                               where t.ProfileId == accountId &&
                                     t.SearchQuery == value
                               orderby t.Id descending
                               select t).Take(InitialCount), x => x.AsViewModel(accountId)).ToList();

                ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][value].AddRange(res);

                //((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][value].AddRange((from t in dh.TwitterSearch
                //                                                        where t.SearchQuery == value && t.ProfileId == accountId &&
                //                                                        !(from o in ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][value]
                //                                                          select o.Id).Contains(t.Id)
                //                                                        orderby t.Id descending
                //                                                        select t.AsViewModel(accountId)
                //                               ).Take(InitialCount));
            }

        }

        public void LoadList(long accountId, string slug)
        {

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTwitterList(slug);

            using (var dh = new MainDataContext())
            {
                ((IMehdohApp)(Application.Current)).ViewModel.Lists[slug].AddRange(Queryable.Take<TimelineViewModel>((from t in dh.TwitterList
                                                   where t.ListId == slug &&
                                                         t.ProfileId == accountId &&
                                                         !Enumerable.Contains((from o in ((IMehdohApp)(Application.Current)).ViewModel.Lists[slug]
                                                             select o.Id), t.Id)
                                                   orderby t.Id descending
                                                   select t.AsViewModel(accountId)
                                                   ), InitialCount));
            }

        }


        public void LoadMentions(long accountId)
        {

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMentions(accountId);

            using (var dh = new MainDataContext())
            {
                var res = dh.Mentions
                            .Where(x => x.ProfileId == accountId)
                            .OrderByDescending(x => x.Id)
                            .Take(InitialCount)
                            .Select(x => x.AsViewModel(accountId));                            

                foreach (var item in res)
                    ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId].Add(item);

            }
        }


        public void LoadNewFollowers(long accountId)
        {
            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmNewFollowers(accountId);
        }

        public void LoadFavourites(long accountId)
        {

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmFavourites(accountId);

            using (var dh = new MainDataContext())
            {

                var res = Queryable.Select<FavouriteTable, FavouritesViewModel>(dh.Favourites
                                .Where(x => x.ProfileId == accountId)
                                .OrderByDescending(x => x.Id)
                                .Take(InitialCount), x => x.AsFavouriesViewModel(accountId));

                foreach (var favouritesViewModel in res)
                {
                    ((IMehdohApp)(Application.Current)).ViewModel.Favourites[accountId].Add(favouritesViewModel);    
                }
                
            }
        }

        public void LoadPhotoView(long accountId)
        {

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmPhotoView(accountId);

            ThreadPool.QueueUserWorkItem(delegate
                                             {
                                                 using (var dh = new MainDataContext())
                                                 {
                                                     if (!dh.Timeline.Any())
                                                     {
                                                         return;
                                                     }

                                                     int currentCount = 0;

                                                     var newmodels = new List<PhotoViewModel>();

                                                     // todo: this is crap. has to be a better way.
                                                     foreach (var timeline in dh.Timeline
                                                                        .Where(x => x.ProfileId == accountId)
                                                                        .OrderByDescending(x => x.Id)
                                                                        .Take(300))
                                                     {
                                                         var models = timeline.AsPhotoViewModel(accountId);
                                                         if (models.Count > 0)
                                                         {
                                                             foreach (var model in models)
                                                             {
                                                                 if (model.HasImage)
                                                                 {
                                                                     newmodels.Add(model);
                                                                     currentCount += models.Count;
                                                                     if (currentCount >= InitialCount)
                                                                     {
                                                                         break;
                                                                     }
                                                                 }
                                                             }
                                                         }
                                                     }

                                                     UiHelper.SafeDispatch(() =>
                                                                               {
                                                                                   foreach (var item in newmodels.Where(item => Enumerable.All<PhotoViewModel>(((IMehdohApp)(Application.Current)).ViewModel.PhotoView[accountId], x => x.Id != item.Id)))
                                                                                   {
                                                                                       ((IMehdohApp)(Application.Current)).ViewModel.PhotoView[accountId].Add(item);
                                                                                   }
                                                                               });

                                                 }
                                             });

        }

        public void LoadTimeline(long accountId)
        {

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmTimeline(accountId);

            using (var dh = new MainDataContext())
            {
               
                var res = dh.Timeline
                    .Where(x => x.ProfileId == accountId)
                    .OrderByDescending(x => x.Id)
                    .Take(InitialCount)
                    .Select(x => x.AsViewModel(accountId));

                foreach (var item in res)
                    ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId].Add(item);

            }

        }


        public void LoadDataAfterTombstoning()
        {

            foreach (var column in ColumnHelper.ColumnConfig)
            {

                var columnType = column.ColumnType;
                var value = column.Value;
                var accountId = column.AccountId;

                switch (columnType)
                {

                    case ApplicationConstants.ColumnTypeTwitter: // core

                        switch (value)
                        {
                            case ApplicationConstants.ColumnTwitterTimeline:
                                LoadTimeline(accountId);
                                break;
                            case ApplicationConstants.ColumnTwitterMentions:
                                LoadMentions(accountId);
                                break;
                            case ApplicationConstants.ColumnTwitterMessages:
                                LoadMessages(accountId);
                                break;
                            case ApplicationConstants.ColumnTwitterFavourites:
                                LoadFavourites(accountId);
                                break;
                            case ApplicationConstants.ColumnTwitterRetweetsOfMe:
                                break;

                            //case ApplicationConstants.Column_Twitter_Retweeted_By_Me:
                            //    return GetRetweetedByMe(accountId);

                            //case ApplicationConstants.Column_Twitter_Retweeted_To_Me:
                            //    return GetRetweetedToMe(accountId);

                            case ApplicationConstants.ColumnTwitterNewFollowers:
                                break;
                            case ApplicationConstants.ColumnTwitterPhotoView:
                                break;
                        }

                        break;

                    case ApplicationConstants.ColumnTypeTwitterSearch:
                        break;

                    case ApplicationConstants.ColumnTypeTwitterList: // list
                        LoadList(accountId, value);
                        break;

                    case ApplicationConstants.ColumnTypeFacebook: // facebook

                        switch (value)
                        {
                            case ApplicationConstants.ColumnFacebookNews:
                                break;
                        }
                        break;

                }

            }

        }

        public void LoadMessages(long accountId)
        {

            ((IMehdohApp)(Application.Current)).ViewModel.ConfirmMessages(accountId);

            using (var dh = new MainDataContext())
            {

                var resGrouped = from m in dh.Messages
                                 where m.ProfileId == accountId
                                 group m by m.ScreenName
                                 into g
                                select new
                                       {
                                           ScreeName = g.Key, 
                                           Item = g.OrderByDescending(x => x.Id).Select<MessageTable, MessagesViewModel>(x => x.AsViewModel(accountId)).FirstOrDefault()
                                       };

                var res = Queryable.Select(resGrouped, x => x.Item);

                foreach (var item in res)
                {
                    ((IMehdohApp)(Application.Current)).ViewModel.Messages[accountId].Add(item);
                }

            }
        }

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                UiHelper.SafeDispatch(() => PropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
            }
        }

        public void ConfirmTwitterSearch(long accountId, string value)
        {

            // TwitterSearch
            if (!((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch.ContainsKey(accountId))
            {
                ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch.Add(accountId, new Dictionary<string, SortedObservableCollection<TimelineViewModel>>());
            }

            if (!((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId].ContainsKey(value))
            {
                var newList = new SortedObservableCollection<TimelineViewModel>();
                newList.CollectionChanged += TwitterSearch_CollectionChanged;
                ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId].Add(value, newList);
            }

        }

        public void ConfirmTimeline(long accountId)
        {
            if (!((IMehdohApp)(Application.Current)).ViewModel.Timeline.ContainsKey(accountId))
            {
                var newList = new SortedObservableCollection<TimelineViewModel>();
                newList.CollectionChanged += Timeline_CollectionChanged;
                ((IMehdohApp)(Application.Current)).ViewModel.Timeline.Add(accountId, newList);
            }
        }

        public void ConfirmNewFollowers(long accountId)
        {
            if (!((IMehdohApp)(Application.Current)).ViewModel.NewFollowers.ContainsKey(accountId))
            {
                var newList = new ObservableCollection<FriendViewModel>();
                newList.CollectionChanged += NewFollowers_CollectionChanged;
                ((IMehdohApp)(Application.Current)).ViewModel.NewFollowers.Add(accountId, newList);
            }
        }

        public void ConfirmFavourites(long accountId)
        {
            if (!((IMehdohApp)(Application.Current)).ViewModel.Favourites.ContainsKey(accountId))
            {
                var newList = new SortedObservableCollection<FavouritesViewModel>();
                newList.CollectionChanged += Favourites_CollectionChanged;
                ((IMehdohApp)(Application.Current)).ViewModel.Favourites.Add(accountId, newList);
            }
        }

        public void ConfirmMentions(long accountId)
        {
            if (!((IMehdohApp)(Application.Current)).ViewModel.Mentions.ContainsKey(accountId))
            {
                var newList = new SortedObservableCollection<MentionsViewModel>();
                newList.CollectionChanged += Mentions_CollectionChanged;
                ((IMehdohApp)(Application.Current)).ViewModel.Mentions.Add(accountId, newList);
            }
        }

        public void ConfirmMessages(long accountId)
        {
            if (!((IMehdohApp)(Application.Current)).ViewModel.Messages.ContainsKey(accountId))
            {
                var newList = new SortedObservableCollection<MessagesViewModel>();
                newList.CollectionChanged += Messages_CollectionChanged;
                ((IMehdohApp)(Application.Current)).ViewModel.Messages.Add(accountId, newList);
            }
        }

        public void ConfirmRetweetsOfMe(long accountId)
        {
            if (!((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe.ContainsKey(accountId))
            {
                var newList = new SortedObservableCollection<TimelineViewModel>();
                newList.CollectionChanged += RetweetsOfMe_CollectionChanged;
                ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe.Add(accountId, newList);
            }
        }

        //public void ConfirmRetweetedToMe(long accountId)
        //{
        //    if (!((IMehdohApp)(Application.Current)).ViewModel.RetweetedToMe.ContainsKey(accountId))
        //    {
        //        var newList = new SortedObservableCollection<TimelineViewModel>();
        //        newList.CollectionChanged += RetweetedToMe_CollectionChanged;
        //        ((IMehdohApp)(Application.Current)).ViewModel.RetweetedToMe.Add(accountId, newList);
        //    }
        //}

        //public void ConfirmRetweetedByMe(long accountId)
        //{
        //    if (!((IMehdohApp)(Application.Current)).ViewModel.RetweetedByMe.ContainsKey(accountId))
        //    {
        //        var newList = new SortedObservableCollection<TimelineViewModel>();
        //        newList.CollectionChanged += RetweetedByMe_CollectionChanged;
        //        ((IMehdohApp)(Application.Current)).ViewModel.RetweetedByMe.Add(accountId, newList);
        //    }
        //}

        //public void ConfirmRetweetedTofMe(long accountId)
        //{
        //    if (!((IMehdohApp)(Application.Current)).ViewModel.RetweetedToMe.ContainsKey(accountId))
        //    {
        //        var newList = new SortedObservableCollection<TimelineViewModel>();
        //        newList.CollectionChanged += RetweetedToMe_CollectionChanged;
        //        ((IMehdohApp)(Application.Current)).ViewModel.RetweetedToMe.Add(accountId, newList);
        //    }
        //}

        public void ConfirmTwitterList(string slug)
        {
            if (!((IMehdohApp)(Application.Current)).ViewModel.Lists.ContainsKey(slug))
            {
                var newList = new SortedObservableCollection<TimelineViewModel>();
                newList.CollectionChanged += TwitterList_CollectionChanged;
                ((IMehdohApp)(Application.Current)).ViewModel.Lists.Add(slug, newList);
            }
        }

        public void ConfirmPhotoView(long accountId)
        {
            if (!((IMehdohApp)(Application.Current)).ViewModel.PhotoView.ContainsKey(accountId))
            {
                var newList = new SortedObservableCollection<PhotoViewModel>();
                newList.CollectionChanged += PhotoView_CollectionChanged;
                ((IMehdohApp)(Application.Current)).ViewModel.PhotoView.Add(accountId, newList);
            }
        }

        private Dictionary<long, string> ScreenNames = new Dictionary<long, string>();
        public bool CurrentlyStreaming { get; set; }

        public bool HasBeenToStreamingSettingsPage { get; set; }

        public bool LoadingInitialData { get; set; }

        public bool JustRestored { get; set; }

        private static object GetScreenNameLock = new object();

        public string GetScreenNameForAccountId(long accountId)
        {

            lock (GetScreenNameLock)
            {
                if (!ScreenNames.ContainsKey(accountId))
                {
                    using (var dh = new MainDataContext())
                    {
                        var profile = dh.Profiles.SingleOrDefault(x => x.Id == accountId);
                        if (profile != null)
                            ScreenNames.Add(accountId, profile.ScreenName);
                        else
                            return string.Empty;
                    }
                }

                return ScreenNames[accountId];
            }

        }

        public void ClearDownUser(long id)
        {
            if (Timeline.ContainsKey(id))
                Timeline.Remove(id);

            if (Messages.ContainsKey(id))
                Messages.Remove(id);

            if (Favourites.ContainsKey(id))
                Favourites.Remove(id);

            if (Mentions.ContainsKey(id))
                Mentions.Remove(id);

            if (RetweetsOfMe.ContainsKey(id))
                RetweetsOfMe.Remove(id);

            if (NewFollowers.ContainsKey(id))
                NewFollowers.Remove(id);

            if (PhotoView.ContainsKey(id))
                PhotoView.Remove(id);

        }

        public void SaveViewStateForTombstoning()
        {

            try
            {
                var serialisedTweet = DataStorageHelper.SerialiseResponseObject<ResponseTweet>(((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet);
                DataStorageHelper.SaveContentsToFile("tweet.state", serialisedTweet);
            }
            catch (Exception ex)
            {
                // ignore
                ErrorLogger.LogException("SaveViewStateForTombstoning", ex);
            }

        }

        public void LoadViewStateForTombstoning()
        {

            try
            {
                var dsh = new DataStorageHelper();

                var serialisedTweet = dsh.LoadContentsFromFile("tweet.state");
                ((IMehdohApp)(Application.Current)).ViewModel.CurrentTweet = DataStorageHelper.GetResponseObject<ResponseTweet>(serialisedTweet);
                dsh.DeleteFile("tweet.state");
            }
            catch (Exception ex)
            {
                // ignore
                ErrorLogger.LogException("LoadViewStateForTombstoning", ex);
            }

        }

    }
}