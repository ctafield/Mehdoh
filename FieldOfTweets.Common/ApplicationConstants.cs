// *********************************************************************************************************
// <copyright file="ApplicationConstants.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System;
using System.ComponentModel;

namespace FieldOfTweets.Common
{
    public static class ApplicationConstants
    {

        public enum VideoPlayerAppEnum
        {
            /// <summary>
            /// internal app
            /// </summary>
            Default = 0,

            /// <summary>
            /// This is now any external app
            /// </summary>
            OfficialYoutube = 1,

            [Obsolete()]
            Metrotube = 2
        }

        #region AccountTypeEnum enum

        public enum AccountTypeEnum
        {
            Twitter = 0,
            Facebook = 1,
            Instagram = 2,
            Soundcloud = 3,
            Deleted = 99
        }

        #endregion

        #region ImageHostEnum enum

        public enum ImageHostEnum
        {
            yFrog = 0,
            TwitPic = 1,
            Imgly = 2,
            TwitVid = 3, // this reutnrs twitter now
            Twitter = 4,
            MobyPicture = 5,
            SkyDrive = 6
        }

        #endregion

        #region Instagram Feed Style enum

        public enum InstagramFeedStyleEnum
        {
            Original = 0,
            Thumbnails = 1
        }

        #endregion

        #region NameDisplayModeEnum enum

        public enum NameDisplayModeEnum
        {
            [Description("screen name")] ScreenName = 0,

            [Description("full name")] FullName = 1
        }

        #endregion

        #region OrientationLockEnum enum

        public enum OrientationLockEnum
        {
            Off = 0,
            Portrait = 1,
            Landscape = 2
        }

        #endregion

        #region RetweetStyleEnum enum

        public enum RetweetStyleEnum
        {
            RT = 0,
            MT = 1,
            QuotesVia = 2,
            Quotes = 3,
        }

        #endregion

        #region SoundcloudTypeEnum enum

        public enum SoundcloudTypeEnum
        {
            Dashboard = 0,
            Favourites = 1,
            SearchResults = 2
        }

        #endregion

        public const int ColumnTypeTwitter = 0;
        public const int ColumnTypeTwitterList = 1;
        public const int ColumnTypeFacebook = 2;
        public const int ColumnTypeInstagram = 3;
        public const int ColumnTypeTwitterSearch = 4;
        public const int ColumnTypeSoundcloud = 5;

        public const string ColumnTwitterPhotoView = "photo_view";
        public const string ColumnTwitterNewFollowers = "new_followers";
        public const string ColumnTwitterTimeline = "timeline";
        public const string ColumnTwitterMentions = "mentions";
        public const string ColumnTwitterFavourites = "favourites";
        public const string ColumnTwitterMessages = "messages";

        public const string ColumnTwitterRetweetsOfMe = "retweets_of_me";
        public const string ColumnTwitterRetweetedByMe = "retweeted_by_me";
        public const string ColumnTwitterRetweetedToMe = "retweeted_to_me";

        public const string ColumnFacebookNews = "news";

        public const string ColumnInstagramFeed = "feed";
        public const string ColumnInstagramPopular = "popular";

        public const string ColumnSoundcloudFavourites = "favourites";
        public const string ColumnSoundcloudDashboard = "dashboard";

        #region Properties

        public static string StateMessages
        {
            get { return "State_Messages_{0}.xml"; }
        }

        public static string StateMentions
        {
            get { return "State_Mentions_{0}.xml"; }
        }

        public static string SoundcloudPlaylist
        {
            get { return "Soundcloud_Playlist.xml"; }
        }

        public static string ShellContentFolder
        {
            get { return "Shared/ShellContent"; }
        }

        public static string BingMapsApiKey
        {
            get { return "AiusHSyYRMMomdvHWP5Qxa_8P8ElFfcs-17Ox_0YHxeFxTAx_kRfc5GJCGn0Vfmk"; }
        }

        public static string SoundcloudUserStorageFolder
        {
            get { return "SoundcloudUsers"; }
        }

        public static string InstagramUserStorageFolder
        {
            get { return "InstagramUsers"; }
        }

        public static string FacebookUserStorageFolder
        {
            get { return "FacebookUsers"; }
        }

        public static string UserStorageFolder
        {
            get { return "Users"; }
        }

        public static string UserCacheStorageFolder
        {
            get { return "UCC"; // had to shorten this as it was too long for wp7 file system
            }
        }

        public static string ThumbnailCacheStorageFolder
        {
            get { return "ThumbnailCache"; }
        }

        // TODO: Write some code to delete this at some point
        public static string UserCacheOldStorageFolder
        {
            get { return "UserCache"; }
        }

        public static string BackgroundTaskLastRun
        {
            get { return "BackgroundTask_LastRun.log"; }
        }

        public static string BackgroundTaskLastStarted
        {
            get { return "BackgroundTask_LastStarted.txt"; }
        }

        public static string ApplicationName
        {
            get
            {
#if MEHDOH_PRO
                return "Mehdoh Unity";
#elif MEHDOH_FREE
                return "Little Mehdoh";
#else
                return "Mehdoh";
#endif
            }
        }

        public static string SearchTileName
        {
            get
            {
#if MEHDOH_PRO
                return "Mehdoh Unity Search";
#elif MEHDOH_FREE
                return "Little Mehdoh Search";
#else
                return "Mehdoh Search";
#endif
            }
        }

        public static string TempImagesFolder
        {
            get { return "TempImages"; }
        }

        #endregion
    }
}