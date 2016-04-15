// *********************************************************************************************************
// <copyright file="TimelineViewModel.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.ImageHostParser;
using FieldOfTweets.Common.UI.ImageCaching;
using FieldOfTweets.Common.UI.ImageHostParser;
using FieldOfTweets.Common.UI.Interfaces;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class TimelineViewModel : INotifyPropertyChanged, IComparable<TimelineViewModel>
    {

        #region Fields

        private bool _isRetweet;

        #endregion

        #region Properties

        public BitmapImage RetweetImagePng
        {
            get { return UiHelper.GetRetweetImage(); }
        }

        public bool IsRetweet
        {
            get { return _isRetweet; }
            set
            {
                if (value != IsRetweet)
                {
                    _isRetweet = value;
                    NotifyPropertyChanged("IsRetweet");
                    NotifyPropertyChanged("RetweetVisibility");
                    NotifyPropertyChanged("NormalTweetVisibility");
                    NotifyPropertyChanged("Author");
                    NotifyPropertyChanged("DisplayName");
                    NotifyPropertyChanged("ScreenName");
                    NotifyPropertyChanged("RetweetAuthor");
                    NotifyPropertyChanged("RetweetUserScreenName");
                    NotifyPropertyChanged("RetweetUserDisplayName");
                    NotifyPropertyChanged("RetweetUserImageUrl");
                }
            }
        }

        public bool IsUserMentioned { get; set; }

        public double MentionOpacity
        {
            get { return (IsUserMentioned) ? 1 : 0; }
        }

        public Visibility MentionVisibility
        {
            get { return (IsUserMentioned) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool IsReply { get; set; }

        public double ReplyOpacity
        {
            get { return (IsReply) ? 1 : 0; }
        }

        public Visibility MultipleImagesVisibility
        {
            get { return (HasMultipleImages) ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool HasMultipleImages { get; set; }

        public Visibility ReplyVisibility
        {
            get { return (IsReply) ? Visibility.Visible : Visibility.Collapsed; }
        }

        // this is for quick access to the details page
        public ResponseTweet ResponseTweet { get; set; }

        public double ImageHeight { get; private set; }

        public BitmapImage MediaSource
        {
            get
            {
                if (MediaUrl != null)
                {
                    var loweredUrl = MediaUrl.ToString().ToLower();

                    if (loweredUrl.Contains("2instawithlove.com/p/"))
                    {
                        ThreadPool.QueueUserWorkItem(delegate { SetParsedImageContainer(loweredUrl, (sender, e) => SetParsedImage(e, new ToInstaWithLoveParser())); });
                    }
                    else if (loweredUrl.Contains("photoplay.net/photos/"))
                    {
                        ThreadPool.QueueUserWorkItem(delegate { SetParsedImageContainer(loweredUrl, (sender, e) => SetParsedImage(e, new PhotoplayParser())); });
                    }
                    else
                    {
                        var bitmap = new BitmapImage(MediaUrl);
#if WP8
                        bitmap.DecodePixelHeight = (int) ImageHeight;
#endif
                        return bitmap;
                    }
                }
                return null;
            }
        }

        // for inline preview
        public Uri MediaUrl
        {
            get { return _mediaUrl; }
            set
            {
                if (!((IMehdohApp)(Application.Current)).ShowTimelineImages)
                {
                    _mediaUrl = null;
                    ImageHeight = 0.0;
                }
                else
                {
                    _mediaUrl = value;
                    ImageHeight = _mediaUrl != null ? 200 : 0;
                }
            }
        }

        public Visibility MediaVisibility
        {
            get { return ImageHeight > 0 ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool IsSyncSetting
        {
            get { return _isSyncSetting; }
            set
            {
                if (value != _isSyncSetting)
                {
                    _isSyncSetting = value;
                    NotifyPropertyChanged("IsSyncSetting");
                    NotifyPropertyChanged("Decorations");
                }
            }
        }

        public object Decorations
        {
            get
            {
                if (IsSyncSetting)
                    return TextDecorations.Underline;
                return null;
            }
        }

        #endregion

        #region RetweetUser

        #region Fields

        private string _retweetAuthor;
        private string _retweetUserDisplayName;
        private string _retweetUserImageUrl;
        private string _retweetUserScreenName;

        #endregion

        #region Properties

        public string RetweetAuthor
        {
            get
            {
                if (string.IsNullOrEmpty(_retweetAuthor))
                {
                    _retweetAuthor = SettingsHelper.GetNameDisplayModeCached() == ApplicationConstants.NameDisplayModeEnum.ScreenName ? RetweetUserScreenNameDisplay : RetweetUserDisplayName;
                }
                return _retweetAuthor;
            }
        }

        public string RetweetUserScreenNameDisplay
        {
            get { return "@" + RetweetUserScreenName; }
        }


        public string RetweetUserScreenName
        {
            get { return _retweetUserScreenName; }
            set
            {
                if (value != _retweetUserScreenName)
                {
                    _retweetUserScreenName = value;
                    NotifyPropertyChanged("RetweetUserScreenName");
                }
            }
        }


        public string RetweetUserDisplayName
        {
            get { return _retweetUserDisplayName; }
            set
            {
                if (value != _retweetUserDisplayName)
                {
                    _retweetUserDisplayName = value;
                    NotifyPropertyChanged("RetweetUserDisplayName");
                }
            }
        }


        public string RetweetUserImageUrl
        {
            get { return _retweetUserImageUrl; }

            set
            {
                if (value != _retweetUserImageUrl)
                {
                    _retweetUserImageUrl = value;
                    NotifyPropertyChanged("RetweetUserImageUrl");
                }
            }
        }

        #endregion

        #endregion

        #region ReplyUrl

        public string ReplyUrl
        {
            get { return "accountId=" + AccountId + "&replyToAuthor=" + ScreenName + "&replyToId=" + Id + "&others=" + OtherAuthors; }
        }

        // Other authors too
        public string OtherAuthors { get; set; }

        #endregion

        #region Description

        #region Fields

        private string _description;
        private string _descriptionRt;

        #endregion

        #region Properties

        public string DescriptionRT
        {
            get
            {
                if (string.IsNullOrEmpty(_descriptionRt))
                {
                    if (string.IsNullOrEmpty(RetweetUserScreenName))
                        _descriptionRt = GetRetweetText(ScreenName, Description);
                    else
                        _descriptionRt = GetRetweetText(RetweetUserScreenName, Description);
                }
                return _descriptionRt;
            }
        }

        //public string DescriptionFull
        //{
        //    get
        //    {
        //        if (String.IsNullOrEmpty(_descResolved))
        //        {

        //            _descResolved = Description;

        //            if (Urls != null && Urls.Count > 0)
        //                foreach (var url in Urls.Where(url => url.Type == AssetTypeEnum.Url && !string.IsNullOrEmpty(url.LongValue)))
        //                {
        //                    //_descResolved = _descResolved.Replace(url.ShortValue, url.LongValue);
        //                }

        //        }

        //        return HttpUtility.HtmlDecode(_descResolved);
        //    }
        //}

        /// <summary>
        ///     Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
        public string Description
        {
            get { return _description; }
            set
            {
                if (value != _description)
                {
                    _description = value;
                    NotifyPropertyChanged("Description");
                }
            }
        }

        #endregion

        private string GetRetweetText(string screenName, string description)
        {
            var sh = new SettingsHelper();
            var res = sh.GetRetweetStlye();

            switch (res)
            {
                case ApplicationConstants.RetweetStyleEnum.MT:
                    return "MT @" + screenName + ": " + description;
                case ApplicationConstants.RetweetStyleEnum.RT:
                    return "RT @" + screenName + ": " + description;
                case ApplicationConstants.RetweetStyleEnum.QuotesVia:
                    return "\"" + description + "\" via @" + screenName;
                case ApplicationConstants.RetweetStyleEnum.Quotes:
                    return "\"@" + screenName + ": " + description + "\"";
                default:
                    return "RT @" + screenName + ": " + description;
            }
        }

        #endregion

        #region Created At

        #region Fields

        private string _createdAt;

        private DateTime? _createdAtDate;

        #endregion

        #region Properties

        /// <summary>
        ///     Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
        public string CreatedAt
        {
            get
            {
                if (DesignerProperties.IsInDesignTool)
                    return "59m";

                var properDate = CreatedAtDate;

                var age = DateTime.Now.Subtract(properDate);

                if (age.TotalDays > 1)
                {
                    return string.Format("{0}d", age.Days);
                }

                if (age.TotalHours > 1)
                {
                    return string.Format("{0}h", age.Hours);
                }

                if (age.TotalMinutes > 1)
                    return string.Format("{0}m", age.Minutes);

                return string.Format("{0}s", age.Seconds < 0 ? 0 : age.Seconds);
            }
            set
            {
                if (value != _createdAt)
                {
                    _createdAt = value;
                    NotifyPropertyChanged("CreatedAt");
                }
            }
        }

        public DateTime CreatedAtDate
        {
            get
            {
                if (_createdAtDate.HasValue)
                    return _createdAtDate.Value;

                const string format = "ddd MMM dd HH:mm:ss zzzz yyyy";

                if (IsGap)
                {
                    _createdAtDate = DateTime.MinValue;
                }
                else
                {
                    _createdAtDate = DateTime.ParseExact(_createdAt, format, CultureInfo.InvariantCulture);
                }

                return _createdAtDate.Value;
            }
            set { _createdAtDate = value; }
        }

        #endregion

        #endregion

        #region Id

        #region Fields

        private long _id;

        #endregion

        #region Properties

        public long? RetweetOriginalId
        {
            get { return _retweetOriginalId; }
            set
            {
                if (value != _retweetOriginalId)
                {
                    _retweetOriginalId = value;
                    NotifyPropertyChanged("RetweetOriginalId");
                }                
            }
        }

        public long Id
        {
            get { return _id; }
            set
            {
                if (value != _id)
                {
                    _id = value;
                    NotifyPropertyChanged("Id");
                }
            }
        }

        #endregion

        #endregion Id

        #region ImageSource

        #region Fields

        private Uri _imageSource;

        #endregion

        #region Properties

        public Uri ImageSource
        {
            get
            {

                if (DesignerProperties.IsInDesignTool)
                    return new Uri(_imageUrl);

                if (IsGap)
                    return null;

                if (_imageSource != null)
                    return _imageSource;

                if (_imageUrl == null)
                    return null;

                ThreadPool.QueueUserWorkItem(delegate(object state)
                                                 {
                                                     var model = state as TimelineViewModel;

                                                     var urlParts = model._imageUrl.Split('/');
                                                     var currentImage = urlParts[urlParts.Length - 1];
                                                     var userId = urlParts[urlParts.Length - 2];

                                                     if (ImageCacheHelper.IsProfileImageCached(userId, currentImage))
                                                     {
                                                         model._imageSource = ImageCacheHelper.GetUriForCachedImage(userId, currentImage);
                                                         model.NotifyPropertyChanged("ImageSource");
                                                     }
                                                     else
                                                     {
                                                         ImageCacheHelper.CacheImage(model.ImageUrl, userId, currentImage, () => model.NotifyPropertyChanged("ImageSource"));
                                                     }

                                                 }, this);

                return null;
            }
        }

        #endregion

        #endregion

        #region ImageUrl

        #region Fields

        public string _imageUrl;

        #endregion

        #region Properties

        public Uri ImageUri
        {
            get
            {
                if (IsGap)
                    return null;

                if (_imageUrl == null)
                    return null;

                //return _imageUrl.ToLower().EndsWith("gif") ? new Uri(_imageUrl, UriKind.Absolute) : null;
                return new Uri(_imageUrl, UriKind.Absolute);
            }
        }

        public string ImageUrl
        {
            get
            {
                if (IsGap) return null;

                if (_imageUrl.ToLower().EndsWith(".gif"))
                    return _imageUrl;
                else
                    return _imageUrl.Replace("_normal.", "_bigger.");

                //return "http://api.twitter.com/1/users/profile_image?screen_name=" + ScreenName + "&size=bigger";
            }
            set
            {
                if (value != _imageUrl)
                {
                    _imageUrl = value;
                    NotifyPropertyChanged("ImageUrl");
                }
            }
        }

        #endregion

        #endregion

        #region Display Name

        #region Fields

        private string _displayName;

        #endregion

        #region Properties

        public string DisplayName
        {
            get { return _displayName; }
            set
            {
                if (value != _displayName)
                {
                    _displayName = value;
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }

        #endregion

        #endregion

        #region ScreenName

        #region Fields

        private string _screenName;

        #endregion

        #region Properties

        public string ScreenNameDisplay
        {
            get { return "@" + ScreenName; }
        }

        public string ScreenName
        {
            get { return _screenName; }
            set
            {
                if (value != _screenName)
                {
                    //FriendsCache.AddFriend(value);
                    _screenName = value;
                    NotifyPropertyChanged("ScreenName");
                }
            }
        }

        #endregion

        #endregion

        #region Author

        #region Fields

        private string _author;

        #endregion

        #region Properties

        public Visibility? NewSignVisibility
        {
            get
            {
                return _newSignVisibility.HasValue ? _newSignVisibility.Value : Visibility.Collapsed;
            }
            set
            {
                if (value != _newSignVisibility)
                {
                    _newSignVisibility = value;
                    NotifyPropertyChanged("NewSignVisibility");
                }
            }
        }

        public string Author
        {
            get
            {
                if (DesignerProperties.IsInDesignTool)
                    return ScreenNameDisplay;
        
                _author = SettingsHelper.GetNameDisplayModeCached() == ApplicationConstants.NameDisplayModeEnum.ScreenName ? ScreenNameDisplay : DisplayName;
                return _author;
            }
        }

        #endregion

        #endregion

        #region IComparable<TimelineViewModel> Members

        public int CompareTo(TimelineViewModel obj)
        {
            if (obj == null)
                return 1;

            var res = obj.Id.CompareTo(Id);

            if (res == 0)
                if (obj.IsGap || IsGap)
                    return 1;

            return res;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void UpdateTime()
        {
            NotifyPropertyChanged("CreatedAt");
        }

        private void NotifyPropertyChanged(String propertyName)
        {
            UiHelper.SafeDispatch(() =>
                                      {
                                          if (PropertyChanged != null)
                                              PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                                      });
        }

        private void SetParsedImageContainer(string targetUrl, OpenReadCompletedEventHandler eventHandler)
        {
            var imageUri = new Uri(targetUrl, UriKind.RelativeOrAbsolute);
            var client = new WebClient();
            client.OpenReadCompleted += eventHandler;
            client.OpenReadAsync(imageUri);
        }

        private void SetParsedImage(OpenReadCompletedEventArgs e, IImageHostParser parser)
        {
            try
            {
                var resInfo = new StreamResourceInfo(e.Result, null);
                using (var reader = new StreamReader(resInfo.Stream))
                {
                    using (var bReader = new BinaryReader(reader.BaseStream))
                    {
                        var contents = bReader.ReadBytes((int) reader.BaseStream.Length);
                        var fileContents = System.Text.Encoding.UTF8.GetString(contents, 0, contents.Length);
                        var targetUrl = parser.GetImageUrl(fileContents);
                        if (!string.IsNullOrEmpty(targetUrl))
                        {
                            MediaUrl = new Uri(targetUrl);
                            NotifyPropertyChanged("MediaUrl");
                            NotifyPropertyChanged("MediaSource");
                        }
                    }
                }
            }
            catch
            {
            }
        }

        #region Client

        //private string _client;

        //public string Client
        //{
        //    get { return _client; }
        //    set
        //    {
        //        if (value != _client)
        //        {
        //            _client = value;
        //            NotifyPropertyChanged("Client");
        //        }
        //    }
        //}

        #region Fields

        private Visibility _gapVisibility = Visibility.Collapsed;
        private bool _isGap;
        private bool _isSyncSetting;
        private Uri _mediaUrl;
        private Visibility? _newSignVisibility;
        private long? _retweetOriginalId;

        #endregion

        #region Properties

        public Visibility GapVisibility
        {
            get { return _gapVisibility; }
        }

        public bool IsGap
        {
            get { return _isGap; }
            set
            {
                if (value != _isGap)
                {
                    _isGap = value;
                    _gapVisibility = IsGap ? Visibility.Visible : Visibility.Collapsed;
                    NotifyPropertyChanged("IsGap");
                }
            }
        }

        public long AccountId { get; set; }

        public string LanguageCode { get; set; }

        #endregion

        #endregion

        public void Reset()
        {
            _imageSource = null;
            _author = null;
        }

        public FlowDirection FlowDirection
        {
            get { return UiHelper.GetFlowDirection(LanguageCode); }
        }

    }
}