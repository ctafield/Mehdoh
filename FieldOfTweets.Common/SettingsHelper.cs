using System;
using System.Linq;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.ErrorLogging;
using FieldOfTweets.Common.Settings;
using Microsoft.Phone.Info;

namespace FieldOfTweets.Common
{
    public class SettingsHelper
    {

        public const string FontSizeOriginal = "original";
        public const string FontSizeSmallest = "smallest";
        public const string FontSizeSmaller = "smaller";
        public const string FontSizeLarger = "larger";
        public const string FontSizeLargest = "largest";

        #region Defaults

        private const short DefaultRefreshCount = 200;
        private const string DefaultFontSize = FontSizeOriginal;

        #endregion


        private static ApplicationConstants.NameDisplayModeEnum? _displayMode;

        private SettingsTable _settingsTable;

        private bool IsReadOnly { get; set; }

        public SettingsHelper()
        {

        }

        public SettingsHelper(bool cacheDatabase)
            : this(cacheDatabase, false)
        {

        }

        public SettingsHelper(bool cacheDatabase, bool isReadOnly)
        {

            IsReadOnly = isReadOnly;

            if (!cacheDatabase)
                return;

            _settingsTable = GetSettingsTable();
        }

        private SettingsTable GetDefaultSettingsTable()
        {
            return new SettingsTable()
            {
                DisplayLinks = true,
                DisplayMaps = true,
                ImageHost = (int)ApplicationConstants.ImageHostEnum.Twitter,
                NameDisplayMode = ApplicationConstants.NameDisplayModeEnum.ScreenName.ToString(),
                RefreshAllColumns = false,
                StartUpRefreshFavourites = true,
                StartUpRefreshMentions = true,
                StartUpRefreshMessages = true,
                StartUpRefreshTimeline = true,
                LocationServicesEnabled = true,
                FavouriteWoeId = 0,
                OnlyUpdateOnWifi = false,
                ShowMinimalAccountInfo = false,
                StartupTab = 0,
                RefreshCount = DefaultRefreshCount,
                BackgroundTaskEnabled = false,
                EnableToast = true,
                ShowSaveLinks = false,
                AlwaysGeoTag = false,
                FontSize = DefaultFontSize,
                LiveTileStyle = (int)ShellHelper.LiveTileStyleEnum.TwitterAvatarStyle,
                FrontTileStyle = (int)ShellHelper.FrontTileStyleEnum.MehdohLogo,
                RetweetStyle = ApplicationConstants.RetweetStyleEnum.RT,
                StramingEnabled = false,
                AutoScrollEnabled = false,
                WelcomeShown = false,
                SleepEnabled = false,
                SleepFrom = null,
                SleepTo = null,
                StreamingKeepScreenOn = false,
                StreamingVibrate = false,
                StreamingSound = false,
                UseImageEditingTools = false,
                UseTweetMarker = false,
                StreamingOnMobile = false,
                OrientationLock = (int)ApplicationConstants.OrientationLockEnum.Off,
                ShowTimelineImages = true,
                TileBackgroundColour = (int)ShellHelper.TileBackgroundColourEnum.System,
                InstgramFeedStyle = (short)ApplicationConstants.InstagramFeedStyleEnum.Original,
                VideoPlayerApp = (short)ApplicationConstants.VideoPlayerAppEnum.Default,
                UseLocationForTrends = true,
                UseCircularAvatars = false,
                ShowPivotHeaderAvatars = true,
                ShowPivotHeaderCounts = true,
                ReturnToTimeline = false
            };
        }

        public BackgroundSettings GetSettingsBackground()
        {
            var settings = new BackgroundSettings()
                           {
                               EnableToast = GetToastEnabled(),
                               OnlyUpdateOnWifi = GetOnlyUpdateOnWifi(),
                               SleepEnabled = GetSleepEnabled(),
                               SleepFrom = GetSleepFrom(),
                               SleepTo = GetSleepTo()
                           };

            return settings;
        }

        public GeneralSettings GetSettingsGeneral()
        {
            var settings = new GeneralSettings()
                               {
                                   NameDisplayMode = GetNameDisplayMode(),
                                   FontSize = GetFontSize(),
                                   RefreshAllColumns = GetRefreshAllColumns(),
                                   RefreshCount = GetRefreshCount(),
                                   ImageHost = GetImageHost(),
                                   UseImageEditingTools = GetUseImageEditingTools(),
                                   RetweetStyle = GetRetweetStlye(),
                                   DisplayLinks = GetDisplayLinks(),
                                   DisplayMaps = GetDisplayMaps(),
                                   OrientationLock = GetOrientationLock(),
                                   ShowTimelineImages = GetShowTimelineImages(),
                                   InstagramFeedStyle = GetInstagramFeedStyle(),
                                   VideoPlayerApp = GetVideoPlayerApp(),
                                   UseCircularAvatars = GetUseCircularAvatars(),
                                   ShowPivotHeaderAvatars = GetShowPivotHeaderAvatars(),
                                   ShowPivotHeaderCounts = GetShowPivotHeaderCounts(),
                                   ReturnToTimeline = GetReturnToTimeline()

                                   //LocationServicesEnabled = GetLocationServicesEnabled(),
                                   //FavouriteWoeId = GetFavouriteWoeId(),
                                   //OnlyUpdateOnWifi = GetOnlyUpdateOnWifi(),
                                   //ShowMinimalAccountInfo = GetShowMinimalAccountInfo(),
                                   //StartupTab = GetStartupTab(),

                                   //BackgroundTaskEnabled = GetBackgroundTaskEnabled(),
                                   //EnableToast = GetToastEnabled(),
                                   //ShowSaveLinks = GetShowSaveLinksEnabled(),
                                   //AlwaysGeoTag = GetAlwaysGeoTag(),

                                   //LiveTileStyle = GetLiveTileStyle(),

                                   //StreamingEnabled = GetStreamingEnabled(),
                                   //AutoScrollEnabled = GetAutoScrollEnabled(),
                                   //WelcomeShown = GetWelcomeShown(),
                                   //FrontTileStyle = GetFrontTileStyle(),

                               };

            return settings;
        }

        private SettingsTable GetSettingsTable()
        {

            try
            {

                SettingsTable setting;

                using (var dh = new SettingsDataContext())
                {
                    if (!dh.DatabaseExists())
                        return null;

                    setting = dh.Settings.FirstOrDefault();
                }

                if (setting == null)
                {
                    using (var dh = new SettingsDataContext())
                    {
                        setting = GetDefaultSettingsTable();
                        dh.Settings.InsertOnSubmit(setting);
                        dh.SubmitChanges();
                    }
                }

                return setting;

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("GetSettingsTable", ex);
                return GetDefaultSettingsTable();
            }
          
        }

        public static ApplicationConstants.NameDisplayModeEnum GetNameDisplayModeCached()
        {

            if (!_displayMode.HasValue)
            {
                var settings = new SettingsHelper();
                _displayMode = settings.GetNameDisplayMode();
            }

            return _displayMode.Value;

        }

        public ApplicationConstants.NameDisplayModeEnum GetNameDisplayMode()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return (ApplicationConstants.NameDisplayModeEnum)Enum.Parse(typeof(ApplicationConstants.NameDisplayModeEnum), _settingsTable.NameDisplayMode.ToString(), true);
        }

        public ApplicationConstants.ImageHostEnum GetImageHost()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return (ApplicationConstants.ImageHostEnum)Enum.Parse(typeof(ApplicationConstants.ImageHostEnum), _settingsTable.ImageHost.ToString(), true);

        }

        public void SetImageHost(ApplicationConstants.ImageHostEnum value)
        {

            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.ImageHost = (int)value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.ImageHost = (int)value;
                }

                dh.SubmitChanges();
            }

        }

        public void SetNameDisplayMode(ApplicationConstants.NameDisplayModeEnum value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.NameDisplayMode = value.ToString();
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.NameDisplayMode = value.ToString();
                }

                dh.SubmitChanges();

            }

            // Update the cache value
            _displayMode = value;

        }


        #region Refresh All Columns

        public void SetRefreshAllColumns(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.RefreshAllColumns = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.RefreshAllColumns = value;
                }

                dh.SubmitChanges();

            }

        }

        public bool GetRefreshAllColumns()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.RefreshAllColumns;

        }

        #endregion

        #region Display Links

        public void SetDisplayLinks(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.DisplayLinks = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.DisplayLinks = value;
                }

                dh.SubmitChanges();

            }

        }

        public bool GetDisplayLinks()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.DisplayLinks;
        }

        #endregion

        #region Wifi Mode

        public void SetOnlyUpdateOnWifi(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.OnlyUpdateOnWifi = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.OnlyUpdateOnWifi = value;
                }

                dh.SubmitChanges();

            }

        }

        public bool GetOnlyUpdateOnWifi()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.OnlyUpdateOnWifi;
        }

        #endregion

        #region Set Refresh Columns

        [Obsolete("Replaced by Column Config Settings")]
        public void SetStartupRefreshTimline(bool value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.StartUpRefreshTimeline = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.StartUpRefreshTimeline = value;
                }

                dh.SubmitChanges();
            }
        }

        [Obsolete("Replaced by Column Config Settings")]
        public void SetStartupRefreshMentions(bool value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.StartUpRefreshMentions = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.StartUpRefreshMentions = value;
                }

                dh.SubmitChanges();
            }
        }

        [Obsolete("Replaced by Column Config Settings")]
        public void SetStartupRefreshMessages(bool value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.StartUpRefreshMessages = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.StartUpRefreshMessages = value;
                }

                dh.SubmitChanges();
            }
        }

        [Obsolete("Replaced by Column Config Settings")]
        public void SetStartupRefreshFavourites(bool value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.StartUpRefreshFavourites = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.StartUpRefreshFavourites = value;
                }

                dh.SubmitChanges();
            }
        }

        #endregion

        #region Get Refresh Columns

        [Obsolete("Replaced by Column Config Settings")]
        public bool GetStartupRefreshTimeline()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.StartUpRefreshTimeline;

        }

        [Obsolete("Replaced by Column Config Settings")]
        public bool GetStartupRefreshMentions()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.StartUpRefreshMentions;
        }

        [Obsolete("Replaced by Column Config Settings")]
        public bool GetStartupRefreshMessages()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.StartUpRefreshMessages;
        }

        [Obsolete("Replaced by Column Config Settings")]
        public bool GetStartupRefreshFavourites()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.StartUpRefreshFavourites;
        }

        #endregion

        #region Display Maps

        public void SetDisplayMaps(bool value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.DisplayMaps = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.DisplayMaps = value;
                }

                dh.SubmitChanges();

            }

        }

        public bool GetDisplayMaps()
        {

            return true;

            //if (_settingsTable == null)
            //{
            //    _settingsTable = GetSettingsTable();
            //}

            //return _settingsTable.DisplayMaps;

        }

        #endregion

        #region FavouriteWoeId

        public void SetFavouriteWoeId(long woeId)
        {

            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.FavouriteWoeId = woeId;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.FavouriteWoeId = woeId;
                }

                dh.SubmitChanges();

            }

        }

        public long GetFavouriteWoeId()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.FavouriteWoeId;

        }

        #endregion

        #region Location Services Enabled

        public void SetLocationServicesEnabled(bool value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.LocationServicesEnabled = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.LocationServicesEnabled = value;
                }

                dh.SubmitChanges();

            }

        }

        public bool GetLocationServicesEnabled()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.LocationServicesEnabled;
        }

        #endregion

        public void SetShowMinimalAccountInfo(bool value)
        {

            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.ShowMinimalAccountInfo = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.ShowMinimalAccountInfo = value;
                }

                dh.SubmitChanges();

            }

        }

        public bool GetShowMinimalAccountInfo()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.ShowMinimalAccountInfo != null && _settingsTable.ShowMinimalAccountInfo.Value;
        }

        [Obsolete("done with")]
        public void SetStartupTab(int value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.StartupTab = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.StartupTab = value;
                }
                dh.SubmitChanges();
            }

        }

        public int GetStartupTab()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.StartupTab.HasValue ? _settingsTable.StartupTab.Value : 0;

        }

        public void SetRefreshCount(int value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.RefreshCount = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.RefreshCount = value;
                }
                dh.SubmitChanges();
            }
        }

        public int GetRefreshCount()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.RefreshCount.HasValue ? _settingsTable.RefreshCount.Value : 20;
        }

        public bool GetBackgroundTaskEnabled()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.BackgroundTaskEnabled.HasValue && _settingsTable.BackgroundTaskEnabled.Value;
        }

        public void SetBackgroundTaskEnabled(bool value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.BackgroundTaskEnabled = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.BackgroundTaskEnabled = value;
                }
                dh.SubmitChanges();
            }
        }

        public bool GetToastEnabled()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return !_settingsTable.EnableToast.HasValue || _settingsTable.EnableToast.Value;
        }

        public void SetToastEnabled(bool value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.EnableToast = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.EnableToast = value;
                }
                dh.SubmitChanges();
            }
        }

        public bool GetAlwaysGeoTag()
        {
            try
            {

                if (_settingsTable == null)
                {
                    _settingsTable = GetSettingsTable();
                }

                return _settingsTable.AlwaysGeoTag.HasValue && _settingsTable.AlwaysGeoTag.Value;
                // default to false    
            }
            catch (Exception)
            {
            }

            return false;
        }

        public void SetAlwaysGeoTag(bool value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.AlwaysGeoTag = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.AlwaysGeoTag = value;
                }
                dh.SubmitChanges();
            }

        }


        public bool GetShowSaveLinksEnabled()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            return _settingsTable.ShowSaveLinks.HasValue && _settingsTable.ShowSaveLinks.Value; // default to false
        }

        public void SetShowSaveLinksEnabled(bool value)
        {
            using (var dh = new SettingsDataContext())
            {
                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.ShowSaveLinks = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.ShowSaveLinks = value;
                }
                dh.SubmitChanges();
            }

        }

        public string GetFontSize()
        {

            try
            {

                if (_settingsTable == null)
                {
                    _settingsTable = GetSettingsTable();
                }
                return string.IsNullOrWhiteSpace(_settingsTable.FontSize) ? FontSizeOriginal : _settingsTable.FontSize;
            }
            catch (Exception)
            {

            }

            return FontSizeOriginal;

        }

        public void SetFontSize(string value)
        {
            try
            {

                using (var dh = new SettingsDataContext())
                {
                    var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                    if (setting == null)
                    {
                        setting = GetDefaultSettingsTable();
                        setting.FontSize = value;
                        dh.Settings.InsertOnSubmit(setting);
                    }
                    else
                    {
                        setting.FontSize = value;
                    }
                    dh.SubmitChanges();
                }

            }
            catch (Exception)
            {

            }

        }

        public void SetLiveTileStyle(ShellHelper.LiveTileStyleEnum value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.LiveTileStyle = (int)value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.LiveTileStyle = (int)value;
                }

                dh.SubmitChanges();
            }

        }

        public void SetTileBackgroundColour(ShellHelper.TileBackgroundColourEnum value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.TileBackgroundColour = (int)value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.TileBackgroundColour = (int)value;
                }

                dh.SubmitChanges();
            }

        }



        private ShellHelper.TileBackgroundColourEnum GetTileBackgroundColour()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.TileBackgroundColour.HasValue)
            {
                _settingsTable.TileBackgroundColour = (int)ShellHelper.TileBackgroundColourEnum.System;
            }

            return (ShellHelper.TileBackgroundColourEnum)Enum.Parse(typeof(ShellHelper.TileBackgroundColourEnum), _settingsTable.TileBackgroundColour.ToString(), true);
        }

        public ShellHelper.LiveTileStyleEnum GetLiveTileStyle()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.LiveTileStyle.HasValue)
            {
                _settingsTable.LiveTileStyle = (int)ShellHelper.LiveTileStyleEnum.TwitterAvatarStyle;
            }

            return (ShellHelper.LiveTileStyleEnum)Enum.Parse(typeof(ShellHelper.LiveTileStyleEnum), _settingsTable.LiveTileStyle.ToString(), true);
        }

        public void SetFrontTileStyle(ShellHelper.FrontTileStyleEnum value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.FrontTileStyle = (int)value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.FrontTileStyle = (int)value;
                }

                dh.SubmitChanges();
            }

        }

        public ShellHelper.FrontTileStyleEnum GetFrontTileStyle()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.FrontTileStyle.HasValue)
            {
                _settingsTable.FrontTileStyle = (int)ShellHelper.FrontTileStyleEnum.MehdohLogo;
            }

            return (ShellHelper.FrontTileStyleEnum)Enum.Parse(typeof(ShellHelper.FrontTileStyleEnum), _settingsTable.FrontTileStyle.ToString(), true);
        }



        public ApplicationConstants.RetweetStyleEnum GetRetweetStlye()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.RetweetStyle.HasValue)
            {
                _settingsTable.RetweetStyle = ApplicationConstants.RetweetStyleEnum.RT;
            }

            return _settingsTable.RetweetStyle.Value;

        }

        public void SetRetweetStyle(ApplicationConstants.RetweetStyleEnum value)
        {

            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.RetweetStyle = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.RetweetStyle = value;
                }

                dh.SubmitChanges();
            }

        }

        public void SetAutoScrollEnabled(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.AutoScrollEnabled = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.AutoScrollEnabled = value;
                }

                dh.SubmitChanges();
            }
        }

        public bool GetAutoScrollEnabled()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.AutoScrollEnabled.HasValue)
            {
                _settingsTable.AutoScrollEnabled = false;
            }

            return _settingsTable.AutoScrollEnabled.Value;
        }

        public void SetStreamingEnabled(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.StramingEnabled = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.StramingEnabled = value;
                }

                dh.SubmitChanges();
            }
        }

        public bool GetStreamingEnabled()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.StramingEnabled.HasValue)
            {
                _settingsTable.StramingEnabled = false;
            }

            return _settingsTable.StramingEnabled.Value;
        }


        #region Welcome Shown

        public bool GetWelcomeShown()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.WelcomeShown.HasValue)
            {
                _settingsTable.WelcomeShown = false;
            }

            return _settingsTable.WelcomeShown.Value;
        }

        public void SetWelcomeShown(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.WelcomeShown = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.WelcomeShown = value;
                }

                dh.SubmitChanges();
            }
        }

        #endregion


        #region SleepEnabled

        public bool GetSleepEnabled()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.SleepEnabled.HasValue)
            {
                _settingsTable.SleepEnabled = false;
            }

            return _settingsTable.SleepEnabled.Value;
        }

        public void SetSleepEnabled(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.SleepEnabled = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.SleepEnabled = value;
                }

                dh.SubmitChanges();
            }
        }

        #endregion

        #region Sleep From

        public DateTime GetSleepFrom()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.SleepFrom.HasValue)
            {
                var now = DateTime.Now;
                _settingsTable.SleepFrom = new DateTime(now.Year, now.Month, now.Day, 23, 0, 0);
            }

            return _settingsTable.SleepFrom.Value;
        }

        public void SetSleepFrom(DateTime value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.SleepFrom = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.SleepFrom = value;
                }

                dh.SubmitChanges();
            }
        }

        #endregion

        #region Sleep To

        public DateTime GetSleepTo()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.SleepTo.HasValue)
            {
                var now = DateTime.Now;
                _settingsTable.SleepTo = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0);
            }

            return _settingsTable.SleepTo.Value;
        }

        public void SetSleepTo(DateTime value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.SleepTo = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.SleepTo = value;
                }

                dh.SubmitChanges();
            }
        }

        #endregion


        public void SetStreamingOnMobile(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.StreamingOnMobile = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.StreamingOnMobile = value;
                }

                dh.SubmitChanges();
            }

        }


        private bool? GetStreamingOnMobile()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.StreamingOnMobile.HasValue)
            {
                _settingsTable.StreamingOnMobile = false;
            }

            return _settingsTable.StreamingOnMobile;

        }


        #region GetStreamingKeepScreenOn

        public bool? GetStreamingKeepScreenOn()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.StreamingKeepScreenOn.HasValue)
            {
                _settingsTable.StreamingKeepScreenOn = false;
            }

            return _settingsTable.StreamingKeepScreenOn;
        }

        public void SetStreamingKeepScreenOn(bool? value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.StreamingKeepScreenOn = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.StreamingKeepScreenOn = value;
                }

                dh.SubmitChanges();
            }
        }

        #endregion


        public StreamingSettings GetSettingsStreaming()
        {
            var results = new StreamingSettings
                          {
                              AutoScrollEnabled = GetAutoScrollEnabled(),
                              StreamingEnabled = GetStreamingEnabled(),
                              StreamingKeepScreenOn = GetStreamingKeepScreenOn(),
                              StreamingVibrate = GetStreamingVibrate(),
                              StreamingSound = GetStreamingSound(),
                              StreamingOnMobile = GetStreamingOnMobile()
                          };

            return results;
        }

        public LocationSettings GetSettingsLocation()
        {
            var results = new LocationSettings
            {
                AlwaysGeoTag = GetAlwaysGeoTag(),
                LocationServicesEnabled = GetLocationServicesEnabled()
            };

            return results;
        }

        public LiveTileSettings GetSettingsLiveTileSettings()
        {
            var results = new LiveTileSettings
                          {
                              FrontTileStyle = GetFrontTileStyle(),
                              LiveTileStyle = GetLiveTileStyle(),
                              TileBackgroundColour = GetTileBackgroundColour()
                          };

            return results;
        }

        private bool? GetStreamingVibrate()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.StreamingVibrate.HasValue)
            {
                _settingsTable.StreamingVibrate = false;
            }

            return _settingsTable.StreamingVibrate;
        }

        public void SetStreamingVibrate(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.StreamingVibrate = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.StreamingVibrate = value;
                }

                dh.SubmitChanges();
            }
        }

        private bool? GetStreamingSound()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.StreamingSound.HasValue)
            {
                _settingsTable.StreamingSound = false;
            }

            return _settingsTable.StreamingSound;
        }


        public void SetStreamingSound(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.StreamingSound = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.StreamingSound = value;
                }

                dh.SubmitChanges();
            }
        }

        public void SettUseImageEditingTools(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.UseImageEditingTools = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.UseImageEditingTools = value;
                }

                dh.SubmitChanges();
            }
        }

        public bool GetUseImageEditingTools()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.UseImageEditingTools.HasValue)
            {
                _settingsTable.UseImageEditingTools = false;
            }

            return _settingsTable.UseImageEditingTools.Value;

        }

        public void SetUseTweetMarker(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.UseTweetMarker = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.UseTweetMarker = value;
                }

                dh.SubmitChanges();
            }
        }

        public bool GetUseTweetMarker()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.UseTweetMarker.HasValue)
            {
                _settingsTable.UseTweetMarker = false;
            }

            return _settingsTable.UseTweetMarker.Value;

        }

        public void SetOrientationLock(ApplicationConstants.OrientationLockEnum value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.OrientationLock = (int)value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.OrientationLock = (int)value;
                }

                dh.SubmitChanges();
            }

        }


        public ApplicationConstants.OrientationLockEnum GetOrientationLock()
        {

            try
            {
                if (_settingsTable == null)
                {
                    _settingsTable = GetSettingsTable();
                }

                if (!_settingsTable.OrientationLock.HasValue)
                {
                    _settingsTable.OrientationLock = (int)ApplicationConstants.OrientationLockEnum.Off;
                }

                return (ApplicationConstants.OrientationLockEnum)Enum.Parse(typeof(ApplicationConstants.OrientationLockEnum), _settingsTable.OrientationLock.Value.ToString(), true);
            }
            catch (Exception)
            {
                return ApplicationConstants.OrientationLockEnum.Off;
            }

        }


        public bool GetShowTimelineImages()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.ShowTimelineImages.HasValue)
            {
                _settingsTable.ShowTimelineImages = true;
            }

            return _settingsTable.ShowTimelineImages.Value;

        }

        public void SetShowTimelineImages(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.ShowTimelineImages = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.ShowTimelineImages = value;
                }

                dh.SubmitChanges();
            }
        }

        public ApplicationConstants.InstagramFeedStyleEnum GetInstagramFeedStyle()
        {

            try
            {
                if (_settingsTable == null)
                {
                    _settingsTable = GetSettingsTable();
                }

                if (!_settingsTable.InstgramFeedStyle.HasValue)
                {
                    _settingsTable.InstgramFeedStyle = (short) ApplicationConstants.InstagramFeedStyleEnum.Original;
                }

                return (ApplicationConstants.InstagramFeedStyleEnum) Enum.Parse(typeof (ApplicationConstants.InstagramFeedStyleEnum), _settingsTable.InstgramFeedStyle.Value.ToString(), true);

            }
            catch (Exception)
            {
                return ApplicationConstants.InstagramFeedStyleEnum.Original;
            }

        }

        public void SetInstagramFeedStyle(ApplicationConstants.InstagramFeedStyleEnum value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.InstgramFeedStyle = (short)value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.InstgramFeedStyle = (short)value;
                }

                dh.SubmitChanges();
            }
        }


        public void SetVideoPlayerApp(ApplicationConstants.VideoPlayerAppEnum value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.VideoPlayerApp = (short)value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.VideoPlayerApp = (short)value;
                }

                dh.SubmitChanges();
            }

        }

        public ApplicationConstants.VideoPlayerAppEnum GetVideoPlayerApp()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.VideoPlayerApp.HasValue)
            {
                _settingsTable.VideoPlayerApp = (short)ApplicationConstants.VideoPlayerAppEnum.Default;
            }

            // Nothing after official supported now
            var player = (ApplicationConstants.VideoPlayerAppEnum)Enum.Parse(typeof(ApplicationConstants.VideoPlayerAppEnum), _settingsTable.VideoPlayerApp.Value.ToString(), true);
            if (player == ApplicationConstants.VideoPlayerAppEnum.Metrotube)
                player = ApplicationConstants.VideoPlayerAppEnum.OfficialYoutube;

            return player;
        }

        public bool GetUseCircularAvatars()
        {
            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.UseCircularAvatars.HasValue)
            {
                _settingsTable.UseCircularAvatars = false;
            }

            // return the new value
            return _settingsTable.UseCircularAvatars.Value;
        }

        public void SetUseCircularAvatars(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.UseCircularAvatars = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.UseCircularAvatars = value;
                }

                dh.SubmitChanges();
            }
        }

        // UseLocationForTrends
        public bool GetUseLocationForTrends()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.UseLocationForTrends.HasValue)
            {
                _settingsTable.UseLocationForTrends = true;
            }

            // return the new value
            return _settingsTable.UseLocationForTrends.Value;
        }

        public void SetUseLocationForTrends(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.UseLocationForTrends = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.UseLocationForTrends = value;
                }

                dh.SubmitChanges();
            }            
        }

        // UseLocationForTrends
        public bool GetShowPivotHeaderAvatars()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.ShowPivotHeaderAvatars.HasValue)
            {
                _settingsTable.ShowPivotHeaderAvatars = true;
            }

            // return the new value
            return _settingsTable.ShowPivotHeaderAvatars.Value;
        }

        public void SetShowPivotHeaderAvatars(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.ShowPivotHeaderAvatars = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.ShowPivotHeaderAvatars = value;
                }

                dh.SubmitChanges();
            }
        }

        public bool GetShowPivotHeaderCounts()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.ShowPivotHeaderCounts.HasValue)
            {
                _settingsTable.ShowPivotHeaderCounts = true;
            }

            // return the new value
            return _settingsTable.ShowPivotHeaderCounts.Value;
        }

        public void SetShowPivotHeaderCounts(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.ShowPivotHeaderCounts = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.ShowPivotHeaderCounts = value;
                }

                dh.SubmitChanges();
            }
        }


        public bool GetReturnToTimeline()
        {

            if (_settingsTable == null)
            {
                _settingsTable = GetSettingsTable();
            }

            if (!_settingsTable.ReturnToTimeline.HasValue)
            {
                _settingsTable.ReturnToTimeline = false;
            }

            // return the new value
            return _settingsTable.ReturnToTimeline.Value;
        }

        public void SetReturnToTimeline(bool value)
        {
            using (var dh = new SettingsDataContext())
            {

                var setting = (from settingsTable in dh.Settings select settingsTable).FirstOrDefault();

                if (setting == null)
                {
                    setting = GetDefaultSettingsTable();
                    setting.ReturnToTimeline = value;
                    dh.Settings.InsertOnSubmit(setting);
                }
                else
                {
                    setting.ReturnToTimeline = value;
                }

                dh.SubmitChanges();
            }
        }

    }

}
