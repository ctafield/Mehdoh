using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.UI.Classes;
using FieldOfTweets.Common.UI.ImageCaching;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class TwitterAccountViewModel : INotifyPropertyChanged
    {

        public ResponseAccountSettings AccountSettings { get; set; }

        public long ListedCount { get; set; }
        public long ListsCount { get; set; }

        public string Url { get; set; }

        public bool IsLoaded { get; set; }

        public long Id { get; set; }

        // public string IdStr { get; set; }

        public string Bio { get; set; }
        public string ScreenName { get; set; }

        public string ScreenNameFormatted { get { return "@" + ScreenName; } }

        public string DisplayName { get; set; }
        public string Location { get; set; }


        #region ImageSource

        private Uri _imageSource;

        public Uri ImageSource
        {
            get
            {
                //if (_imageSource != null)
                //    return _imageSource;

                //if (ProfileImageUrl == null)
                //    return null;

                if (_imageSource != null)
                    return _imageSource;

                ThreadPool.QueueUserWorkItem(delegate(object state)
                {

                    try
                    {
                        var model = state as TwitterAccountViewModel;

                        if (model == null || string.IsNullOrEmpty(model._profileImageUrl))
                            return;

                        var urlParts = model._profileImageUrl.Split('/');

                        if (urlParts.Length < 2)
                            return;

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
                    }
                    catch
                    {
                        return;
                    }

                }, this);

                return null;
            }

        }

        #endregion

        public string OriginalProfileImageUrl
        {
            get
            {
                //return "http://api.twitter.com/1/users/profile_image?screen_name=" + ScreenName + "&size=original";

                if (ProfileImageUrl.Contains("_normal."))
                    return ProfileImageUrl.Replace("_normal", "");
                if (ProfileImageUrl.Contains("_bigger."))
                    return ProfileImageUrl.Replace("_bigger", "");
                return ProfileImageUrl;
            }
        }

        #region ImageUrl

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
                if (value != _profileImageUrl)
                {
                    _profileImageUrl = value;
                    NotifyPropertyChanged("ImageUrl");
                }
            }
        }

        #endregion

        public long TweetCount { get; set; }

        public string TweetCountPerDay
        {
            get
            {
                var span = DateTime.Now.Subtract(JoinDate);
                var res = TweetCount / span.TotalDays;
                return String.Format("{0:0.00} per day", res);
            }
        }

        public long FollowingCount { get; set; }
        public long FollowersCount { get; set; }

        public bool Following { get; set; }

        public string JoinDateString { get; set; }

        public DateTime JoinDate
        {
            get
            {
                const string format = "ddd MMM dd HH:mm:ss zzz yyyy";
                return DateTime.ParseExact(JoinDateString, format, CultureInfo.InvariantCulture);
            }
        }

        public string JoinDateDisplay
        {
            get { return JoinDate.ToShortDateString() + " " + JoinDate.ToShortTimeString(); }
        }


        public bool Verified { get; set; }
        public Visibility VerifiedVisibility
        {
            get
            {
                return (Verified) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public string BackgroundImageUrl { get; set; }

        public bool IsProtected { get; set; }
        public Visibility IsProtectedVisibility
        {
            get
            {
                return (IsProtected) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public string BannerUrl { get; set; }

        public BitmapImage BannerSource
        {
            get
            {
                if (!string.IsNullOrEmpty(BannerUrl))
                {
                    var bitmap = new BitmapImage(new Uri(BannerUrl, UriKind.Absolute));
#if WP8
                    bitmap.DecodePixelHeight = ResolutionHelper.CurrentResolution == Resolutions.WVGA ? 160 : 320;

#endif
                    return bitmap;
                }
                return null;
            }
        }

        public IList<AssetViewModel> Assets { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                UiHelper.SafeDispatch(() => PropertyChanged(this, new PropertyChangedEventArgs(propertyName)));
            }
        }


    }

}
