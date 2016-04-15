#region

using System;
using System.Data.Linq.Mapping;

#endregion

namespace FieldOfTweets.Common.DataStorage
{

    [Table]
    public class SettingsTable
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int Id { get; set; }

        [Column()]
        public string NameDisplayMode { get; set; }

        [Column]
        public int ImageHost { get; set; }

        [Column]
        public bool DisplayMaps { get; set; }

        [Column]
        public bool DisplayLinks { get; set; }

        [Column]
        public bool RefreshAllColumns { get; set; }

        [Column]
        public bool StartUpRefreshTimeline { get; set; }

        [Column]
        public bool StartUpRefreshMentions { get; set; }

        [Column]
        public bool StartUpRefreshMessages { get; set; }

        [Column]
        public bool StartUpRefreshFavourites { get; set; }

        [Column]
        public bool OnlyUpdateOnWifi { get; set; }

        [Column]
        public long FavouriteWoeId { get; set; }

        [Column]
        public bool LocationServicesEnabled { get; set; }

        [Column]
        public bool? ShowMinimalAccountInfo { get; set; }

        [Column]
        public int? StartupTab { get; set; }

        [Column]
        public int? RefreshCount { get; set; }

        [Column]
        public bool? BackgroundTaskEnabled { get; set; }

        [Column]
        public bool? EnableToast { get; set; }

        [Column]
        public bool? ShowSaveLinks { get; set; }

        [Column]
        public bool? AlwaysGeoTag { get; set; }

        [Column]
        public string FontSize { get; set; }

        [Column]
        public int? LiveTileStyle { get; set; }

        // these are all 2.0 additions

        [Column]
        public ApplicationConstants.RetweetStyleEnum? RetweetStyle { get; set; }

        [Column]
        public bool? StramingEnabled { get; set; }

        [Column]
        public bool? AutoScrollEnabled { get; set; }

        [Column]
        public bool? WelcomeShown { get; set; }

        [Column]
        public bool? SleepEnabled { get; set; }

        [Column]
        public DateTime? SleepFrom { get; set; }

        [Column]
        public DateTime? SleepTo { get; set; }

        [Column]
        public int? FrontTileStyle { get; set; }

        [Column]
        public bool? StreamingKeepScreenOn { get; set; }

        [Column]
        public bool? StreamingVibrate { get; set; }

        [Column]
        public bool? StreamingSound { get; set; }

        [Column]
        public bool? AutoFFTag { get; set; }

        [Column]
        public bool? UseImageEditingTools { get; set; }

        [Column]
        public bool? UseTweetMarker { get; set; }

        [Column]
        public bool? StreamingOnMobile { get; set; }

        [Column]
        public int? OrientationLock { get; set; }

        [Column]
        public bool? ShowTimelineImages { get; set; }

        [Column]
        public int? TileBackgroundColour { get; set; }

        [Column]
        public short? InstgramFeedStyle { get; set; }

        [Column]
        public short? VideoPlayerApp { get; set; }

        [Column]
        public bool? UseLocationForTrends { get; set; }

        [Column]
        public bool? UseCircularAvatars { get; set; }

        [Column]
        public bool? ShowPivotHeaderCounts { get; set; }

        [Column]
        public bool? ShowPivotHeaderAvatars { get; set; }
        
        [Column]
        public bool? ReturnToTimeline { get; set; }
    }

}
