using System;
using System.Data.Linq;
using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.DataContext
{

    // isetool ts xd 3cca05df-559a-484d-a71d-a10aa1d8f135 c:\dev

    public class MainDataContext : System.Data.Linq.DataContext
    {

        public MainDataContext() : base(ApplicationSettings.ConnectionString)
        {
        }

        protected MainDataContext(string connectionString) : base(connectionString)
        {
        }


        public Table<SettingsTable> Settings
        {
            get
            {
                return GetTable<SettingsTable>();
            }
        }


        public Table<ShellStatusTable> ShellStatus
        {
            get
            {
                return GetTable<ShellStatusTable>();
            }
        }

        public Table<RecentPeopleSearchTable> RecentPeopleSearch
        {
            get
            {
                return GetTable<RecentPeopleSearchTable>();
            }
        }

        public Table<RecentTextSearchTable> RecentTextSearch
        {
            get
            {
                return GetTable<RecentTextSearchTable>();
            }
        }

        public Table<FavouriteTable> Favourites
        {
            get { return GetTable<FavouriteTable>(); }
        }


        public Table<TimelineTable> Timeline
        {
            get { return GetTable<TimelineTable>(); }
        }

        public Table<ProfileTable> Profiles
        {
            get
            {
                return this.GetTable<ProfileTable>();
            }
        }

        public Table<MentionTable> Mentions
        {
            get
            {
                return this.GetTable<MentionTable>();
            }
        }

        public Table<MessageTable> Messages
        {
            get
            {
                return this.GetTable<MessageTable>();
            }
        }

        public Table<FavouritesAssetTable> FavouritesAsset
        {
            get
            {
                return this.GetTable<FavouritesAssetTable>();
            }
        }


        public Table<TimelineAssetTable> TimelineAsset
        {
            get
            {
                return this.GetTable<TimelineAssetTable>();
            }
        }

        public Table<MessageAssetTable> MessageAsset
        {
            get
            {
                return this.GetTable<MessageAssetTable>();
            }
        }

        public Table<MentionAssetTable> MentionAsset
        {
            get
            {
                return this.GetTable<MentionAssetTable>();
            }
        }

        public Table<UserLookupTable> UserLookup
        {
            get
            {
                return this.GetTable<UserLookupTable>();
            }
        }

        [Obsolete("Not used any more")]
        public Table<FriendsSnapshot> FriendsSnapshot
        {
            get
            {
                return GetTable<FriendsSnapshot>();
            }
        }

        [Obsolete("Not used any more")]
        public Table<FollowersSnapshot> FollowersSnapshot
        {
            get
            {
                return GetTable<FollowersSnapshot>();
            }
        }

        public Table<AccountSettingCache> AccountSettingCache
        {
            get
            {
                return GetTable<AccountSettingCache>();
            }
        }

        public Table<ReadLaterTable> ReadLaterSettings
        {
            get
            {
                return GetTable<ReadLaterTable>();
            }
        }

        public Table<SentDirectMessageTable> SentDirectMessages
        {
            get
            {
                return GetTable<SentDirectMessageTable>();
            }
        }

        [Obsolete("Use ColumnHepler instead.")]
        public Table<ColumnConfigTable> ColumnConfig
        {
            get
            {
                return GetTable<ColumnConfigTable>();
            }
        }

        public Table<TwitterListTable> TwitterList
        {
            get
            {
                return GetTable<TwitterListTable>();
            }
        }

        public Table<TwitterListAssetTable> TwitterListAsset
        {
            get
            {
                return GetTable<TwitterListAssetTable>();
            }
        }

        public Table<RetweetsOfMeTable> RetweetsOfMe
        {
            get
            {
                return GetTable<RetweetsOfMeTable>();
            }
        }

        public Table<RetweetsOfMeAssetTable> RetweetsOfMeAsset
        {
            get
            {
                return GetTable<RetweetsOfMeAssetTable>();
            }
        }

        public Table<RetweetsByMeTable> RetweetedByMe
        {
            get
            {
                return GetTable<RetweetsByMeTable>();
            }
        }

        public Table<RetweetsByMeAssetTable> RetweetsByMeAsset
        {
            get
            {
                return GetTable<RetweetsByMeAssetTable>();
            }
        }

        public Table<InstagramLocationsTable> InstagramLocations
        {
            get
            {
                return GetTable<InstagramLocationsTable>();
            }
        }

        public Table<RetweetsToMeTable> RetweetsToMe
        {
            get
            {
                return GetTable<RetweetsToMeTable>();
            }
        }

        public Table<RetweetsToMeAssetTable> RetweetsToMeAsset
        {
            get
            {
                return GetTable<RetweetsToMeAssetTable>();
            }
        }

        public Table<ThumbnailCacheTable> ThumbnailCache
        {
            get
            {
                return GetTable<ThumbnailCacheTable>();
            }
        }

        public Table<InstagramNewsTable> InstagramNews
        {
            get
            {
                return GetTable<InstagramNewsTable>();
            }
        }

        public Table<TwitterSearchTable> TwitterSearch
        {
            get
            {
                return GetTable<TwitterSearchTable>();
            }
        }

        public Table<TwitterSearchAssetTable> TwitterSearchAsset
        {
            get
            {
                return GetTable<TwitterSearchAssetTable>();
            }
        }

        public Table<TrendLocationTable> TrendLocationTable
        {
            get
            {
                return GetTable<TrendLocationTable>();
            }
        }

    }
}
