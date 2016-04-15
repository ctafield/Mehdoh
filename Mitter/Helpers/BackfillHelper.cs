using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.ViewModels;

namespace Mitter.Helpers
{
    public class BackfillHelper
    {

        // reference to the view model that instigated the backfill
        public TimelineViewModel MoreTweetsButton { get; set; }

        // used in gaps
        public long OldestId { get; set; }

        public long AccountId { get; private set; }

        public BackfillHelper(long accountId)
        {
            AccountId = accountId;
        }

        private int _chunkCount;

        private int ChunkCount
        {
            get
            {
                if (_chunkCount == 0)
                {
                    var sh = new SettingsHelper();
                    _chunkCount = sh.GetRefreshCount();
                }
                return _chunkCount;
            }
        }

        #region Timeline


        public async Task<List<TimelineViewModel>> GetMoreTimeline(long newestId, long oldestId, long accountId)
        {

            OldestId = oldestId;

            if (oldestId > 0)
            {
                // Use the api to get more
                var api = new TwitterApi(accountId);
                var result = await api.GetTimelineOld(newestId, oldestId);
                var moreTimeline = ViewModelHelper.TimelineResponseToView(AccountId, result);
                ThreadPool.QueueUserWorkItem(SaveUpdates, result);
                return moreTimeline;
            }

            using (var dh = new MainDataContext())
            {

                List<TimelineViewModel> moreTimeline = null;

                try
                {
                    moreTimeline = (from t in dh.Timeline
                                    where !(from o in ((IMehdohApp)(Application.Current)).ViewModel.Timeline[accountId]
                                            select o.Id).Contains(t.Id)
                                          && t.Id < newestId
                                          && t.Id > oldestId
                                          && t.ProfileId == accountId
                                    orderby t.Id descending
                                    select t)
                        .Select(x => x.AsViewModel(accountId))
                        .Take(ChunkCount)
                        .ToList();
                }
                catch (Exception)
                {
                }

                if (moreTimeline.Count > 0)
                {
                    return moreTimeline;
                }

                // Use the api to get more
                var api = new TwitterApi(accountId);
                var timeline = await api.GetTimelineOld(newestId, oldestId);

                moreTimeline = ViewModelHelper.TimelineResponseToView(AccountId, timeline);
                ThreadPool.QueueUserWorkItem(SaveUpdates, timeline);

                return moreTimeline;
            }
        }

        private void SaveUpdates(object o)
        {
            var updates = o as List<ResponseTweet>;

            var dsh = new DataStorageHelper();
            dsh.SaveTimelineUpdates(AccountId, updates);

        }

        #endregion

        #region Mentions

        public event EventHandler GetMoreMentionCompletedEvent;

        public List<MentionsViewModel> MoreMention { get; set; }

        public async void GetMoreMention(long oldestItem, long accountId)
        {

            OldestId = oldestItem;

            using (var dh = new MainDataContext())
            {
                MoreMention = (from t in dh.Mentions
                               where !(from o in ((IMehdohApp)(Application.Current)).ViewModel.Mentions[accountId]
                                       select o.Id).Contains(t.Id)
                                     && t.ProfileId == accountId
                               orderby t.Id descending
                               select t.AsViewModel(accountId)).Take(ChunkCount).ToList();

                if (MoreMention.Count > 0)
                {
                    if (GetMoreMentionCompletedEvent != null)
                        GetMoreMentionCompletedEvent(this, null);
                }
                else
                {
                    // Use the api to get more
                    var api = new TwitterApi(accountId);
                    var result = await api.GetMentionsOld(oldestItem);
                    api_GetMentionsOldCompletedEvent(result);
                }

            }

        }

        private void api_GetMentionsOldCompletedEvent(List<ResponseTweet> oldMentions)
        {

            if (oldMentions == null || !oldMentions.Any())
            {
                if (GetMoreMentionCompletedEvent != null)
                    GetMoreMentionCompletedEvent(this, null);
                return;
            }

            try
            {
                MoreMention = ViewModelHelper.MentionsResponseToView(AccountId, oldMentions).ToList();
                ThreadPool.QueueUserWorkItem(SaveMentionUpdates, oldMentions);
            }
            finally
            {
                if (GetMoreMentionCompletedEvent != null)
                    GetMoreMentionCompletedEvent(this, null);
            }

        }

        private void SaveMentionUpdates(object state)
        {
            var updates = state as List<ResponseTweet>;
            var dsh = new DataStorageHelper();
            dsh.SaveMentionUpdates(updates, AccountId);
        }

        #endregion

        #region Messages

        #endregion

        #region Favourites

        #endregion

        #region twitter search

        public event EventHandler GetMoreTwitterSearchCompletedEvent;

        public string SearchQuery { get; set; }

        public async Task<List<TimelineViewModel>> GetMoreTwitterSearch(string searchQuery, long newestId, long oldestId, long accountId)
        {

            SearchQuery = searchQuery;
            OldestId = oldestId;

            if (oldestId > 0)
            {
                // Use the api to get more
                var api = new TwitterApi(accountId);
                var result = await api.Search(searchQuery, newestId, oldestId, true);
                return ViewModelHelper.TimelineResponseToView(AccountId, result.statuses).ToList();

            }

            try
            {
                using (var dh = new MainDataContext())
                {
                    var moreTwitterSearch = (from t in dh.TwitterSearch
                                             where t.SearchQuery == searchQuery && t.ProfileId == accountId &&
                                                   !(from o in ((IMehdohApp)(Application.Current)).ViewModel.TwitterSearch[accountId][searchQuery]
                                                     select o.Id).Contains(t.Id)
                                             orderby t.Id descending
                                             select t.AsViewModel(accountId)).Take(ChunkCount).ToList();

                    if (MoreList != null && MoreList.Any())
                    {
                        return moreTwitterSearch;
                    }

                    // Use the api to get more
                    var api = new TwitterApi(accountId);
                    var result = await api.Search(searchQuery, newestId, oldestId, true);
                    return ViewModelHelper.TimelineResponseToView(AccountId, result.statuses).ToList();
                }
            }
            catch (Exception)
            {
                return new List<TimelineViewModel>();
            }

        }

        #endregion

        #region List

        public event EventHandler GetMoreListCompletedEvent;

        public List<TimelineViewModel> MoreList { get; set; }
        public string ListId { get; set; }

        public async void GetMoreList(string listId, long newestId, long oldestId, long accountId)
        {

            ListId = listId;
            OldestId = oldestId;

            if (oldestId > 0)
            {
                // Use the api to get more
                var api = new TwitterApi(accountId);                
                var result = await api.GetListStatuses(listId, oldestId, newestId);
                api_GetListStatusesCompletedEvent(accountId, listId, result);
            }
            else
            {

                // TODO: This needs the slug?
                using (var dh = new MainDataContext())
                {
                    MoreList = (from t in dh.TwitterList
                                where !(from o in ((IMehdohApp)(Application.Current)).ViewModel.Lists[listId]
                                        select o.Id).Contains(t.Id)
                                      && t.Id < newestId
                                      && t.Id > oldestId
                                      && t.ProfileId == accountId
                                orderby t.Id descending
                                select t.AsViewModel(accountId)).Take(ChunkCount).ToList();

                    if (MoreList != null && MoreList.Any())
                    {
                        if (GetMoreListCompletedEvent != null)
                            GetMoreListCompletedEvent(this, null);
                    }
                    else
                    {
                        // Use the api to get more
                        var api = new TwitterApi(accountId);                        
                        var result = await api.GetListStatuses(listId, oldestId, newestId);
                        api_GetListStatusesCompletedEvent(accountId, listId, result);
                    }
                }
            }

        }

        private void api_GetListStatusesCompletedEvent(long accountId, string slug, List<ResponseTweet> listStatuses)
        {

            if (listStatuses == null || !listStatuses.Any())
            {
                if (GetMoreListCompletedEvent != null)
                    GetMoreListCompletedEvent(this, null);
                return;
            }

            try
            {
                MoreList = ViewModelHelper.TimelineResponseToView(AccountId, listStatuses).ToList();
                ThreadPool.QueueUserWorkItem(delegate
                {
                    var dsh = new DataStorageHelper();
                    dsh.SaveTwitterListUpdates(accountId, listStatuses, slug);
                });
            }
            finally
            {
                if (GetMoreListCompletedEvent != null)
                    GetMoreListCompletedEvent(this, null);
            }

        }

        #endregion

        #region RetweetsOfMe

        public List<TimelineViewModel> MoreRetweetsOfMe { get; set; }

        public event EventHandler GetMoreRetweetsOfMeCompletedEvent;

        public void GetMoreRetweetsOfMe(long newestId, long oldestId, long accountId)
        {

            OldestId = oldestId;

            if (oldestId > 0)
            {
                // Use the api to get more
                var api = new TwitterApi(accountId);
                api.GetRetweetsOfMeCompletedEvent += new EventHandler(api_GetRetweetsOfMeCompletedEvent);
                api.GetRetweetsOfMe(newestId, oldestId);
            }
            else
            {

                using (var dh = new MainDataContext())
                {
                    MoreRetweetsOfMe = (from t in dh.RetweetsOfMe
                                        where !(from o in ((IMehdohApp)(Application.Current)).ViewModel.RetweetsOfMe[accountId]
                                                select o.Id).Contains(t.Id)
                                              && t.Id < newestId
                                              && t.Id > oldestId
                                              && t.ProfileId == accountId
                                        orderby t.Id descending
                                        select t.AsViewModel(accountId)).Take(ChunkCount).ToList();

                    if (MoreRetweetsOfMe.Any())
                    {
                        if (GetMoreRetweetsOfMeCompletedEvent != null)
                            GetMoreRetweetsOfMeCompletedEvent(this, null);
                    }
                    else
                    {
                        // Use the api to get more
                        var api = new TwitterApi(accountId);
                        api.GetRetweetsOfMeCompletedEvent += new EventHandler(api_GetRetweetsOfMeCompletedEvent);
                        api.GetRetweetsOfMe(oldestId, newestId);
                    }
                }
            }

        }

        private void api_GetRetweetsOfMeCompletedEvent(object sender, EventArgs e)
        {

            var api = sender as TwitterApi;

            if (api == null || api.RetweetsOfMe == null || !api.RetweetsOfMe.Any())
            {
                if (GetMoreRetweetsOfMeCompletedEvent != null)
                    GetMoreRetweetsOfMeCompletedEvent(this, null);
                return;
            }

            try
            {
                MoreRetweetsOfMe = ViewModelHelper.TimelineResponseToView(AccountId, api.RetweetsOfMe);
                ThreadPool.QueueUserWorkItem(SaveRetweetsOfMeUpdates, api.RetweetsOfMe);
            }
            finally
            {
                if (GetMoreRetweetsOfMeCompletedEvent != null)
                    GetMoreRetweetsOfMeCompletedEvent(this, null);
            }

        }


        private void SaveRetweetsOfMeUpdates(object state)
        {
            var updates = state as List<ResponseTweet>;
            var dsh = new DataStorageHelper();
            dsh.SaveRetweetsOfMeUpdates(AccountId, updates);
        }

        #endregion

        #region RetweetedByMe

        //public List<TimelineViewModel> MoreRetweetedByMe { get; set; }

        //public event EventHandler GetMoreRetweetedByMeCompletedEvent;

        //public void GetMoreRetweetedByMe(long newestId, long oldestId, long accountId)
        //{

        //    OldestId = oldestId;

        //    if (oldestId > 0)
        //    {
        //        // Use the api to get more
        //        var api = new TwitterApi(accountId);
        //        api.GetRetweetsByMeCompletedEvent += new EventHandler(api_GetRetweetsByMeCompletedEvent);
        //        api.GetRetweetsByMe(newestId, oldestId);
        //    }
        //    else
        //    {

        //        using (var dh = new MainDataContext())
        //        {
        //            MoreRetweetedByMe = (from t in dh.RetweetedByMe
        //                                 where !(from o in ((IMehdohApp)(Application.Current)).ViewModel.RetweetedByMe[accountId]
        //                                         select o.Id).Contains(t.Id)
        //                                       && t.Id < newestId
        //                                       && t.Id > oldestId
        //                                       && t.ProfileId == accountId
        //                                 orderby t.Id descending
        //                                 select t.AsViewModel(accountId)).Take(ChunkCount).ToList();

        //            if (MoreRetweetedByMe.Any())
        //            {
        //                if (GetMoreRetweetedByMeCompletedEvent != null)
        //                    GetMoreRetweetedByMeCompletedEvent(this, null);
        //            }
        //            else
        //            {
        //                // Use the api to get more
        //                var api = new TwitterApi(accountId);
        //                api.GetRetweetsByMeCompletedEvent += new EventHandler(api_GetRetweetsByMeCompletedEvent);
        //                api.GetRetweetsByMe(oldestId, newestId);
        //            }
        //        }
        //    }

        //}

        //private void api_GetRetweetsByMeCompletedEvent(object sender, EventArgs e)
        //{

        //    var api = sender as TwitterApi;

        //    if (api == null || api.RetweetsByMe == null || !api.RetweetsByMe.Any())
        //    {
        //        if (GetMoreRetweetedByMeCompletedEvent != null)
        //            GetMoreRetweetedByMeCompletedEvent(this, null);
        //        return;
        //    }

        //    try
        //    {
        //        MoreRetweetedByMe = ViewModelHelper.TimelineResponseToView(AccountId, api.RetweetsByMe);
        //        ThreadPool.QueueUserWorkItem(SaveRetweetedByMeUpdates, api.RetweetsByMe);
        //    }
        //    finally
        //    {
        //        if (GetMoreRetweetedByMeCompletedEvent != null)
        //            GetMoreRetweetedByMeCompletedEvent(this, null);
        //    }

        //}


        //private void SaveRetweetedByMeUpdates(object state)
        //{
        //    var updates = state as List<ResponseTweet>;
        //    var dsh = new DataStorageHelper();
        //    dsh.SaveRetweetedByMeUpdates(AccountId, updates);
        //}

        #endregion

        #region RetweetedToMe

        //public List<TimelineViewModel> MoreRetweetedToMe { get; set; }

        //public event EventHandler GetMoreRetweetedToMeCompletedEvent;

        //public void GetMoreRetweetedToMe(long newestId, long oldestId, long accountId)
        //{

        //    OldestId = oldestId;

        //    if (oldestId > 0)
        //    {
        //        // Use the api to get more
        //        var api = new TwitterApi(accountId);
        //        api.GetRetweetsToMeCompletedEvent += new EventHandler(api_GetRetweetsToMeCompletedEvent);
        //        api.GetRetweetsToMe(newestId, oldestId);
        //    }
        //    else
        //    {

        //        using (var dh = new MainDataContext())
        //        {
        //            MoreRetweetedToMe = (from t in dh.RetweetsToMe
        //                                 where !(from o in ((IMehdohApp)(Application.Current)).ViewModel.RetweetedToMe[accountId]
        //                                         select o.Id).Contains(t.Id)
        //                                       && t.Id < newestId
        //                                       && t.Id > oldestId
        //                                       && t.ProfileId == accountId
        //                                 orderby t.Id descending
        //                                 select t.AsViewModel(accountId)).Take(ChunkCount).ToList();

        //            if (MoreRetweetedToMe.Any())
        //            {
        //                if (GetMoreRetweetedToMeCompletedEvent != null)
        //                    GetMoreRetweetedToMeCompletedEvent(this, null);
        //            }
        //            else
        //            {
        //                // Use the api to get more
        //                var api = new TwitterApi(accountId);
        //                api.GetRetweetsToMeCompletedEvent += new EventHandler(api_GetRetweetsToMeCompletedEvent);
        //                api.GetRetweetsToMe(oldestId, newestId);
        //            }
        //        }
        //    }

        //}

        //private void api_GetRetweetsToMeCompletedEvent(object sender, EventArgs e)
        //{

        //    var api = sender as TwitterApi;

        //    if (api == null || api.RetweetsToMe == null || !api.RetweetsToMe.Any())
        //    {
        //        if (GetMoreRetweetedToMeCompletedEvent != null)
        //            GetMoreRetweetedToMeCompletedEvent(this, null);
        //        return;
        //    }

        //    try
        //    {
        //        MoreRetweetedToMe = ViewModelHelper.TimelineResponseToView(AccountId, api.RetweetsToMe);
        //        ThreadPool.QueueUserWorkItem(SaveRetweetedToMeUpdates, api.RetweetsToMe);
        //    }
        //    finally
        //    {
        //        if (GetMoreRetweetedToMeCompletedEvent != null)
        //            GetMoreRetweetedToMeCompletedEvent(this, null);
        //    }

        //}


        //private void SaveRetweetedToMeUpdates(object state)
        //{
        //    var updates = state as List<ResponseTweet>;
        //    var dsh = new DataStorageHelper();
        //    dsh.SaveRetweetedToMeUpdates(AccountId, updates);
        //}

        #endregion

    }

}
