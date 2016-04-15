// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

namespace FieldOfTweets.Common.Settings
{
    public class GeneralSettings
    {
        #region Properties

        public ApplicationConstants.NameDisplayModeEnum NameDisplayMode { get; set; }
        public string FontSize { get; set; }
        public bool RefreshAllColumns { get; set; }
        public int RefreshCount { get; set; }
        public ApplicationConstants.ImageHostEnum ImageHost { get; set; }
        public bool UseImageEditingTools { get; set; }
        public ApplicationConstants.RetweetStyleEnum RetweetStyle { get; set; }
        public bool DisplayLinks { get; set; }
        public bool DisplayMaps { get; set; }
        public ApplicationConstants.OrientationLockEnum OrientationLock { get; set; }
        public bool ShowTimelineImages { get; set; }
        public ApplicationConstants.InstagramFeedStyleEnum InstagramFeedStyle { get; set; }        
        public ApplicationConstants.VideoPlayerAppEnum VideoPlayerApp { get; set; }
        public bool UseCircularAvatars { get; set; }
        public bool ShowPivotHeaderAvatars { get; set; }
        public bool ShowPivotHeaderCounts { get; set; }
        public bool ReturnToTimeline { get; set; }

        #endregion
    }
}