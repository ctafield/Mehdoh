using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.UI.Friends
{

    public class FriendsCache
    {

        static FriendsCache()
        {
            _friends = new List<string>
                       {
                           "mehdoh"
                       };
        }

        private static List<string> _friends;

        public static void AddFriend(string screenName)
        {

            try
            {

                if (string.IsNullOrEmpty(screenName))
                    return;

                var newName = screenName.Replace("@", "");
                if (string.IsNullOrEmpty(newName))
                    return;

                if (_friends.All(x => string.Compare(x, newName, StringComparison.InvariantCultureIgnoreCase) != 0))
                    _friends.Add(newName);
                else
                {
                    // remove it, and stick it at the front so it's persisted
                    _friends.Remove(newName);
                    _friends.Insert(0, newName);
                }

            }
            catch
            {
                // haven't got the foggiest why it would end up here, but there you go.
            }


        }

        public static List<string> SearchFriend(string screenName)
        {

            if (!_friends.Any())
                return null;

            if (string.IsNullOrEmpty(screenName) || screenName.Length < 1)
                return null;

            try
            {
                var friends = _friends.Where(x => x.ToLower(CultureInfo.InvariantCulture).Contains(screenName.ToLower(CultureInfo.InvariantCulture)));

                return friends.Select(x => "@" + x)
                              .OrderBy(x => x)
                              .Take(30)
                              .ToList();

            }
            catch
            {
                return null;
            }

        }


        private static object ParsingOldTimelinesLock = new object();


        [Obsolete("Use the state management one instead")]
        private static void ParseOldTimelines(MainDataContext mainDataContext)
        {

            lock (ParsingOldTimelinesLock)
            {
                foreach (var t in mainDataContext.Timeline.OrderByDescending(x => x.Id).Select(x => new { x.ScreenName, x.RetweetUserScreenName, x.Assets }).Take(300))
                {

                    AddFriend(t.ScreenName);

                    if (!string.IsNullOrEmpty(t.RetweetUserScreenName))
                        AddFriend(t.RetweetUserScreenName);

                    if (t.Assets == null || !t.Assets.Any())
                        continue;

                    foreach (var h in t.Assets.Where(x => x.Type == AssetTypeEnum.Hashtag))
                    {
                        HashtagCache.AddHashtag(h.ShortValue);
                    }

                    foreach (var h in t.Assets.Where(x => x.Type == AssetTypeEnum.Mention))
                    {
                        AddFriend(h.ShortValue);
                    }

                }

                foreach (var m in mainDataContext.Mentions.OrderByDescending(x => x.Id).Select(x => new { x.ScreenName, x.RetweetUserScreenName, x.Assets }).Take(300))
                {
                    AddFriend(m.ScreenName);

                    if (!string.IsNullOrEmpty(m.RetweetUserScreenName))
                        AddFriend(m.RetweetUserScreenName);

                    if (m.Assets == null || !m.Assets.Any())
                        continue;

                    foreach (var h in m.Assets.Where(x => x.Type == AssetTypeEnum.Hashtag))
                    {
                        HashtagCache.AddHashtag(h.ShortValue);
                    }

                    foreach (var h in m.Assets.Where(x => x.Type == AssetTypeEnum.Mention))
                    {
                        AddFriend(h.ShortValue);
                    }
                }


                SaveFriendsCache();

            }

        }

        public static void ParseNewTimelines(List<ResponseTweet> newTweets)
        {

            lock (ParsingOldTimelinesLock)
            {

                if (newTweets == null || !newTweets.Any())
                    return;

                foreach (var t in newTweets)
                {
                    AddFriend(t.user.screen_name);

                    if (t.retweeted_status != null && t.retweeted_status.user != null)
                    {
                        AddFriend(t.retweeted_status.user.screen_name);
                    }

                    if (t.entities == null || t.entities.hashtags == null || !t.entities.hashtags.Any())
                        continue;

                    foreach (var h in t.entities.hashtags)
                    {
                        HashtagCache.AddHashtag(h.text);
                    }
                }

                // Save the cache
                SaveFriendsCache();

            }
        }

        public static void LoadFriendsCache()
        {
            var newFriends = StorageHelper.LoadFriendsCache();

            if (newFriends != null)
            {
                foreach (var friend in newFriends)
                    AddFriend(friend);
            }
        }

        private static void SaveFriendsCache()
        {
            StorageHelper.SaveFriendsCache(_friends);
        }

    }

}
