using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using FieldOfTweets.Common.UI.ImageCaching;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class DetailsPageViewModel : INotifyPropertyChanged
    {

        public TweetTypeEnum TweetType { get; set; }

        public long AccountId { get; set; }

        public long Id { get; set; }

        public string LocationFull { get; set; }

        public string DisplayName { get; set; }
        public string ScreenName { get; set; }

        public string Author { get { return "@" + ScreenName; } }

        public string RetweetScreenName { get; set; }
        public string RetweetScreenNameFormatted { get { return "@" + RetweetScreenName; } }

        public string RetweetDisplayName { get; set; }

        public string RetweetDescription { get; set; }

        public long? OriginalRetweetId { get; set; }

        public string RetweetAuthor
        {
            get
            {
                if (SettingsHelper.GetNameDisplayModeCached() == ApplicationConstants.NameDisplayModeEnum.ScreenName)
                    return RetweetScreenNameFormatted;
                return RetweetDisplayName;
            }
        }

        public BitmapImage RetweetImagePng
        {
            get
            {
                return UiHelper.GetRetweetImage();
            }
        }

        public Visibility RetweetVisibility
        {
            get { return IsRetweet ? Visibility.Visible : Visibility.Collapsed; }
        }

        public bool IsRetweet { get; set; }

        public string ScreenNameFormatted
        {
            get { return "@" + ScreenName; }
        }

        #region ImageSource

        private Uri _imageSource;

        public Uri ImageSource
        {
            get
            {
                if (_imageSource != null)
                    return _imageSource;

                ThreadPool.QueueUserWorkItem(delegate(object state)
                {
                    var model = state as DetailsPageViewModel;

                    var urlParts = model._profileImageUrl.Split('/');
                    var currentImage = urlParts[urlParts.Length - 1];
                    var userId = urlParts[urlParts.Length - 2];

                    if (ImageCacheHelper.IsProfileImageCached(userId, currentImage))
                    {
                        model._imageSource = ImageCacheHelper.GetUriForCachedImage(userId, currentImage);
                        model.NotifyPropertyChanged("ImageSource");
                    }
                    else
                    {
                        ImageCacheHelper.CacheImage(model.ProfileImageUrl, userId, currentImage, () => model.NotifyPropertyChanged("ImageSource"));
                    }
                }, this);

                return null;

                //if (_imageSource != null)
                //    return _imageSource;

                //if (ProfileImageUrl == null)
                //    return null;

                //var urlParts = ProfileImageUrl.Split('/');
                //var currentImage = urlParts[urlParts.Length - 1];
                //var userId = urlParts[urlParts.Length - 2];

                //if (ImageCacheHelper.IsProfileImageCached(userId, currentImage))
                //{
                //    _imageSource = ImageCacheHelper.GetUriForCachedImage(userId, currentImage);
                //    return _imageSource;
                //}

                //ImageCacheHelper.CacheImage(ProfileImageUrl, userId, currentImage, () =>
                //{
                //    // Notifying the property has changed will cause the image to repaint, and use the cached version
                //    NotifyPropertyChanged("ImageUrl");
                //    NotifyPropertyChanged("ImageSource");
                //});

                ////_imageSource = ImageCacheHelper.GetUriForCachedImage(userId, currentImage);
                ////return new Uri(_imageUrl, UriKind.Absolute);

                //return null;
            }

        }

        #endregion


        private string _profileImageUrl;

        public string ProfileImageUrl
        {
            get
            {
                if (_profileImageUrl.ToLower().EndsWith(".gif"))
                    return _profileImageUrl;
                else
                    return _profileImageUrl.Replace("_normal.", "_bigger.");
                //return "http://api.twitter.com/1/users/profile_image?screen_name=" + ScreenName + "&size=bigger";
            }
            set
            {
                if (_profileImageUrl != value)
                {
                    _profileImageUrl = value;
                    NotifyPropertyChanged("ProfileImageUrl");
                }
            }
        }

        public bool Verified { get; set; }

        public Visibility VerifiedVisibility
        {
            get { return Verified ? Visibility.Visible : Visibility.Collapsed; }
        }

        public string Description { get; set; }

        public string ClientVia
        {
            get
            {
                return string.IsNullOrWhiteSpace(Client) ? string.Empty : "via " + Client;
            }
        }

        private string _client;

        public string Client
        {
            set { _client = value; }
            get
            {
                if (!string.IsNullOrEmpty(_client) && _client.Contains(">"))
                {
                    var newVal = _client.Substring(_client.IndexOf(">"));
                    newVal = newVal.Substring(1, newVal.IndexOf("</a>") - 1);
                    return newVal;
                }
                else
                {
                    return _client;
                }
            }
        }

        public DateTime CreatedAt { get; set; }

        public string CreatedAtDisplay
        {

            get { return CreatedAt.ToString(CultureInfo.CurrentUICulture); }
        }

        public long? InReplyToId { get; set; }

        public IList<AssetViewModel> Assets { get; set; }

        public double Location1X { get; set; }
        public double Location1Y { get; set; }

        public double Location2X { get; set; }
        public double Location2Y { get; set; }

        public double Location3X { get; set; }
        public double Location3Y { get; set; }

        public double Location4X { get; set; }
        public double Location4Y { get; set; }

        public string LanguageCode { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                UiHelper.SafeDispatch(() =>
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
            }
        }

    
    }
}
