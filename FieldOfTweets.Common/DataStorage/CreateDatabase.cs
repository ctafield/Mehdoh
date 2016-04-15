using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.POCO;
using FieldOfTweets.Common.DataContext;
using Microsoft.Phone.Data.Linq;

namespace FieldOfTweets.Common.DataStorage
{

    public class DatabaseAdministration
    {

        /// <summary>
        /// This is the current version of the database scheme. Needs updating every time a new column / table is added.
        /// </summary>
        public const int DB_VERSION = 20;

        private static readonly object ValidateDatabaseLock = new object();

        public long GetDatabaseSize()
        {

            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var file = store.OpenFile(ApplicationSettings.DatabaseFileName, FileMode.Open, FileAccess.Read);
                return file.Length;
            }

        }

        public void ValidateDatabase()
        {

            lock (ValidateDatabaseLock)
            {
                // Lets create the database too 
                using (var dh = new MainDataContext())
                {
                    if (!dh.DatabaseExists())
                    {
                        dh.CreateDatabase();

                        var dbUpdater = dh.CreateDatabaseSchemaUpdater();
                        dbUpdater.DatabaseSchemaVersion = DB_VERSION;
                        dbUpdater.Execute();

                        //CheckDefaultColumnConfig();
                    }
                    else
                    {
                        UpdateDatabase();
                    }
                }

            }

        }

        public void UpdateDatabase()
        {

            // Check for DB updates
            using (var dh = new MainDataContext())
            {

                var dbUpdater = dh.CreateDatabaseSchemaUpdater();

                // any update?
                if (dbUpdater.DatabaseSchemaVersion == DB_VERSION)
                    return;

                if (dbUpdater.DatabaseSchemaVersion < 2)
                {
                    // Aviary tools implemented at this point
                    dbUpdater.AddColumn<SettingsTable>("UseImageEditingTools");
                }
                if (dbUpdater.DatabaseSchemaVersion < 3)
                {
                    // Aviary tools implemented at this point
                    dbUpdater.AddColumn<SettingsTable>("UseTweetMarker");
                }
                // below added in 2.2
                if (dbUpdater.DatabaseSchemaVersion < 4)
                {
                    // Aviary tools implemented at this point
                    dbUpdater.AddTable<TwitterSearchTable>();
                    dbUpdater.AddTable<TwitterSearchAssetTable>();
                }
                // also added in 2.2
                if (dbUpdater.DatabaseSchemaVersion < 5)
                {
                    dbUpdater.AddColumn<SettingsTable>("StreamingOnMobile");
                }

                // added in 2.4
                if (dbUpdater.DatabaseSchemaVersion < 6)
                {
                    dbUpdater.AddColumn<SettingsTable>("OrientationLock");
                }

                // added in 2.9
                if (dbUpdater.DatabaseSchemaVersion < 7)
                {
                    dbUpdater.AddColumn<SettingsTable>("ShowTimelineImages");
                }

                // added in 3.1
                if (dbUpdater.DatabaseSchemaVersion < 8)
                {
                    dbUpdater.AddColumn<SettingsTable>("TileBackgroundColour");
                }

                // added in 3.3
                if (dbUpdater.DatabaseSchemaVersion < 9)
                {
                    dbUpdater.AddColumn<TwitterListTable>("RetweetDescription");
                    dbUpdater.AddColumn<TwitterSearchTable>("RetweetDescription");
                }

                if (dbUpdater.DatabaseSchemaVersion < 10)
                {
                    dbUpdater.AddColumn<SettingsTable>("InstgramFeedStyle");
                }

                if (dbUpdater.DatabaseSchemaVersion < 11)
                {
                    dbUpdater.AddColumn<SettingsTable>("VideoPlayerApp");
                }

                if (dbUpdater.DatabaseSchemaVersion < 12)
                {
                    dbUpdater.AddColumn<TimelineTable>("LanguageCode");
                    dbUpdater.AddColumn<FavouriteTable>("LanguageCode");
                    dbUpdater.AddColumn<MentionTable>("LanguageCode");
                    dbUpdater.AddColumn<MessageTable>("LanguageCode");
                    dbUpdater.AddColumn<RetweetsByMeTable>("LanguageCode");
                    dbUpdater.AddColumn<RetweetsOfMeTable>("LanguageCode");
                    dbUpdater.AddColumn<RetweetsToMeTable>("LanguageCode");
                    dbUpdater.AddColumn<TwitterListTable>("LanguageCode");
                    dbUpdater.AddColumn<TwitterSearchTable>("LanguageCode");
                }

                if (dbUpdater.DatabaseSchemaVersion < 13)
                {
                    dbUpdater.AddColumn<InstagramNewsTable>("Type");
                    dbUpdater.AddColumn<InstagramNewsTable>("VideoUrl");
                }

                if (dbUpdater.DatabaseSchemaVersion < 14)
                {
                    dbUpdater.AddColumn<SettingsTable>("UseLocationForTrends");
                }

                if (dbUpdater.DatabaseSchemaVersion < 15)
                {
                    dbUpdater.AddTable<TrendLocationTable>();
                }

                if (dbUpdater.DatabaseSchemaVersion < 16)
                {
                    dbUpdater.AddColumn<TimelineTable>("RetweetOriginalId");
                    dbUpdater.AddColumn<TwitterListTable>("RetweetOriginalId");
                    dbUpdater.AddColumn<TwitterSearchTable>("RetweetOriginalId");
                    dbUpdater.AddColumn<RetweetsToMeTable>("RetweetOriginalId");
                    dbUpdater.AddColumn<RetweetsOfMeTable>("RetweetOriginalId");
                    dbUpdater.AddColumn<RetweetsByMeTable>("RetweetOriginalId");
                    dbUpdater.AddColumn<MessageTable>("RetweetOriginalId");
                    dbUpdater.AddColumn<MentionTable>("RetweetOriginalId");
                    dbUpdater.AddColumn<FavouriteTable>("RetweetOriginalId");
                }

                if (dbUpdater.DatabaseSchemaVersion < 17)
                {
                    // Migrate the Column Config to the file based version
                    MigrateColumnConfig();
                }

                if (dbUpdater.DatabaseSchemaVersion < 18)
                {
                    dbUpdater.AddColumn<SettingsTable>("UseCircularAvatars");
                }

                if (dbUpdater.DatabaseSchemaVersion < 19)
                {
                    dbUpdater.AddColumn<SettingsTable>("ShowPivotHeaderCounts");
                    dbUpdater.AddColumn<SettingsTable>("ShowPivotHeaderAvatars");
                }

                if (dbUpdater.DatabaseSchemaVersion < 20)
                {
                    dbUpdater.AddColumn<SettingsTable>("ReturnToTimeline");
                }

                // AND ALSO CHANGE THE ABOVE VERSION
                dbUpdater.DatabaseSchemaVersion = DatabaseAdministration.DB_VERSION;
                dbUpdater.Execute();

            }

        }

        private void MigrateColumnConfig()
        {
            using (var dh = new MainDataContext())
            {
                foreach (var col in dh.ColumnConfig)
                {
                    ColumnHelper.AddNewColumn(new ColumnModel
                    {
                        AccountId = col.AccountId,
                        ColumnType = col.ColumnType,
                        DisplayName = col.DisplayName,
                        Order = col.Order,
                        RefreshOnStartup = col.RefreshOnStartup,
                        Value = col.Value
                    });

                    dh.ColumnConfig.DeleteOnSubmit(col);
                }

                try
                {
                    dh.SubmitChanges();
                }
                catch (Exception)
                {
                    // Doesnt matter    
                }
            }
        }

        private void UpdatePostAccounts()
        {

            using (var dh = new MainDataContext())
            {
                foreach (var profile in dh.Profiles)
                {
                    profile.UseToPost = true;
                }
                dh.SubmitChanges();
            }

        }

        public void AddDefaultTwitterColumns(long accountId)
        {

            var colHome = new ColumnModel()
                              {
                                  ColumnType = 0,
                                  DisplayName = "timeline",
                                  Value = "timeline",
                                  RefreshOnStartup = true,
                                  AccountId = accountId
                              };

            var colMentions = new ColumnModel()
                                  {
                                      ColumnType = 0,
                                      DisplayName = "mentions",
                                      Value = "mentions",
                                      RefreshOnStartup = true,
                                      AccountId = accountId
                                  };

            var colMessages = new ColumnModel()
                                  {
                                      ColumnType = 0,
                                      DisplayName = "messages",
                                      Value = "messages",
                                      RefreshOnStartup = true,
                                      AccountId = accountId
                                  };

            ColumnHelper.AddNewColumn(colHome);
            ColumnHelper.AddNewColumn(colMentions);
            ColumnHelper.AddNewColumn(colMessages);

        }

        public void AddDefaultInstagramColumns(long accountId)
        {

            var colFeed = new ColumnModel()
            {
                ColumnType = ApplicationConstants.ColumnTypeInstagram,
                DisplayName = "feed",
                Value = ApplicationConstants.ColumnInstagramFeed,
                RefreshOnStartup = true,
                AccountId = accountId
            };

            var colPopular = new ColumnModel()
            {
                ColumnType = ApplicationConstants.ColumnTypeInstagram,
                DisplayName = "explore",
                Value = ApplicationConstants.ColumnInstagramPopular,
                RefreshOnStartup = true,
                AccountId = accountId
            };

            ColumnHelper.AddNewColumn(colFeed);
            ColumnHelper.AddNewColumn(colPopular);

        }

        public void AddDefaultSoundcloudColumns(long accountId, string favouritesText)
        {

            var colDashboard = new ColumnModel()
            {
                ColumnType = ApplicationConstants.ColumnTypeSoundcloud,
                DisplayName = "dashboard",
                Value = ApplicationConstants.ColumnSoundcloudDashboard,
                RefreshOnStartup = true,
                AccountId = accountId
            };

            var colFavourites = new ColumnModel()
            {
                ColumnType = ApplicationConstants.ColumnTypeSoundcloud,
                DisplayName = favouritesText,
                Value = ApplicationConstants.ColumnSoundcloudFavourites,
                RefreshOnStartup = true,
                AccountId = accountId
            };

            ColumnHelper.AddNewColumn(colDashboard);
            ColumnHelper.AddNewColumn(colFavourites);

        }

        public void ClearDatabase()
        {

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myStore.FileExists(ApplicationSettings.DatabaseFileNameTemp))
                    myStore.DeleteFile(ApplicationSettings.DatabaseFileNameTemp);
            }

            // Lets create the database too 
            using (var copyDataContext = new TempDataContext())
            {
                copyDataContext.CreateDatabase();

                using (var dh = new MainDataContext())
                {

                    // Profiles
                    var profiles = dh.Profiles.ToList();
                    copyDataContext.Profiles.InsertAllOnSubmit(profiles);

                    // Account Settings
                    var accountSettings = dh.AccountSettingCache.ToList();
                    copyDataContext.AccountSettingCache.InsertAllOnSubmit(accountSettings);

                    // Read later settings
                    var ril = dh.ReadLaterSettings.ToList();
                    copyDataContext.ReadLaterSettings.InsertAllOnSubmit(ril);

                    // Settings
                    var settings = dh.Settings.ToList();
                    copyDataContext.Settings.InsertAllOnSubmit(settings);

                    // submit
                    copyDataContext.SubmitChanges();

                    dh.Dispose();
                }

                // Set the schema to be correct.
                var dbUpdater = copyDataContext.CreateDatabaseSchemaUpdater();

                dbUpdater.DatabaseSchemaVersion = DatabaseAdministration.DB_VERSION;
                dbUpdater.Execute();

                copyDataContext.Dispose();
            }

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myStore.FileExists(ApplicationSettings.DatabaseFileName))
                    myStore.DeleteFile(ApplicationSettings.DatabaseFileName);


                myStore.MoveFile(ApplicationSettings.DatabaseFileNameTemp, ApplicationSettings.DatabaseFileName);
            }

        }

    }

}
