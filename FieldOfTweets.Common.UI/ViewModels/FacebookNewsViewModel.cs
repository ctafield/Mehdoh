using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using FieldOfTweets.Common.UI.ImageCaching;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class FacebookNewsViewModel : INotifyPropertyChanged
    {

        private Uri _imageSource;

        public Uri ImageSource
        {
            get
            {
                var urlParts = ImageUrl.Split('/');

                // TODO: This is wrong, its always "picture" for facebook
                var currentImage = urlParts[urlParts.Length - 1];
                var userId = urlParts[urlParts.Length - 2];

                if (ImageCacheHelper.IsProfileImageCached(userId, currentImage))
                {
                    _imageSource = ImageCacheHelper.GetUriForCachedImage(userId, currentImage);
                    return _imageSource;
                }

                ImageCacheHelper.CacheImage(ImageUrl, userId, currentImage, () =>
                {
                    // Notifying the property has changed will cause the image to repaint, and use the cached version
                    NotifyPropertyChanged("ImageUrl");
                    NotifyPropertyChanged("ImageSource");
                });

                return null;

            }
            set
            {
                _imageSource = value;
            }
        }

        public string ImageUrl { get; set; }
        public string LocationFull { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public int LikeCount { get; set; }
        public bool UserLikes { get; set; }
        public int CommentCount { get; set; }

        private string _createdAt;

        public string CreatedAtDisplay
        {
            get
            {
                var properDate = DateTime.Parse(_createdAt);
                return properDate.ToString(CultureInfo.CurrentUICulture);
            }
        }

        public string CreatedAt
        {
            get
            {
                // ISO-8601
                // 2012-02-09T21:35:54+0000
                var properDate = DateTime.Parse(_createdAt);

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
                _createdAt = value;
            }
        }

        public FacebookPostTypeEnum FacebookPostType { get; set; }

        public string SourceVia
        {
            get
            {
                return string.IsNullOrWhiteSpace(Source) ? string.Empty : "via " + Source;
            }
        }

        public string Source { get; set; }

        public string PhotoUrl { get; set; }

        public IList<FacebookCommentViewModel> Comments { get; set; }

        public IList<string> WebLinks { get; set; }

        public double Longitude { get; set; }
        public double Latitude { get; set; }

        public string Id { get; set; }

        // Important!
        public long AccountId { get; set; }

        public string PhotoLink { get; set; }

        public string ObjectId { get; set; }

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
