using System;
using System.ComponentModel;
using System.Threading;
using FieldOfTweets.Common.UI.ImageCaching;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class InstagramFeedViewModel : IComparable<InstagramFeedViewModel>, INotifyPropertyChanged
    {

        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(Id))
                return -1;

            return Id.GetHashCode();
        }

        public int CompareTo(InstagramFeedViewModel other)
        {
            return -1;
            //return String.Compare(other.Id, Id, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return (((InstagramFeedViewModel)obj).Id == Id);
        }

        public Uri LikeImageUri
        {
            get
            {
                if (UserHasLiked.HasValue && UserHasLiked.Value)
                    return new Uri("/Images/unlike.png", UriKind.Relative);
                else
                    return new Uri("/Images/like.png", UriKind.Relative);
            }
        }

        public int LikeCount
        {
            get { return _likeCount; }
            set
            {
                if (value != _likeCount)
                {
                    _likeCount = value;
                    NotifyPropertyChanged("LikeCount");
                    NotifyPropertyChanged("LikeCountDisplay");
                    NotifyPropertyChanged("LikeImageUri");
                }
            }
        }

        public string LikeCountDisplay
        {
            get
            {
                return string.Format("\u2665 {0}", LikeCount);
            }
        }

        // id
        public string Id { get; set; }

        public string Type { get; set; }

        public Uri VideoUri { get; set; }

        public double VideoButtonOpacity
        {
            get
            {
                if (Type == "video" && VideoUri != null)
                {
                    return 1;
                }
                return 0;
            }
        }

        // image
        // set this in code
        public Uri ImageUri
        {
            get;
            set;
        }

        public bool CacheImage { get; set; }


        public bool IsUpdatingImageSource { get; set; }

        // read this in XAML
        public Uri _imageSource;
        public Uri ImageSource
        {
            get
            {

                if (_imageSource != null)
                {
                    IsUpdatingImageSource = false;
                    return _imageSource;
                }

                if (IsUpdatingImageSource)
                    return _imageSource;

                if (!CacheImage)
                    return ImageUri;

                UpdateImageSource();

                return _imageSource;
            }
        }

        private void UpdateImageSource()
        {

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                var model = state as InstagramFeedViewModel;
                if (model == null)
                    return;

                var cacheHelper = new ThumbnailCacheHelper();

                var cachedUri = cacheHelper.GetCachedUri(model.ImageUri);
                if (cachedUri != null)
                {
                    model._imageSource = cachedUri;
                    UiHelper.SafeDispatch(() => model.NotifyPropertyChanged("ImageSource"));
                    return;
                }

                if (model.ImageUri != null && !model.IsUpdatingImageSource)
                {
                    model.IsUpdatingImageSource = true;
                    cacheHelper.CacheImage(model.ImageUri.ToString(), () => UiHelper.SafeDispatch(() =>
                    {
                        model.IsUpdatingImageSource = false;
                        model.NotifyPropertyChanged("ImageSource");
                    }));
                }
            }, this);
        }

        public Uri ImageSourceFull { get; set; }

        // date time
        public DateTime CreatedAt { get; set; }

        public string CreatedAtDisplay
        {
            get
            {
                var age = DateTime.UtcNow.Subtract(CreatedAt);

                if (age.TotalDays > 1)
                {
                    return string.Format("\u25F7 {0}d", age.Days);
                }

                if (age.TotalHours > 1)
                {
                    return string.Format("\u25F7 {0}h", age.Hours);
                }

                if (age.TotalMinutes > 1)
                    return string.Format("\u25F7 {0}m", age.Minutes);

                return string.Format("\u25F7 {0}s", age.Seconds < 0 ? 0 : age.Seconds);

            }
        }

        // comments
        public string Description { get; set; }

        public string UserName { get; set; }

        public string FullName { get; set; }

        public string UserId { get; set; }

        // Location name
        private string _locationName;
        private int _likeCount;
        private bool? _userHasLiked;

        public string LocationName
        {
            get
            {
                return _locationName;
            }
            set
            {
                if (value != _locationName)
                {
                    _locationName = value;
                    NotifyPropertyChanged("LocationName");
                }
            }
        }

        public double? LocationLatitude { get; set; }

        public double? LocationLongitude { get; set; }

        public string Filter { get; set; }

        // http link for this. Useful for tweeting
        public string Link { get; set; }

        public long? LocationId { get; set; }

        // Used for tagging the item 
        public long AccountId { get; set; }

        public string UserImageUrl { get; set; }

        public long? LongId
        {
            get
            {

                try
                {
                    var newId = Id.Replace("_" + UserId, ""); // ends with _ACCOUNTID;
                    return long.Parse(newId);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public bool? UserHasLiked
        {
            get { return _userHasLiked; }
            set
            {
                if (value != _userHasLiked)
                {
                    _userHasLiked = value;
                    NotifyPropertyChanged("UserHasLiked");
                    NotifyPropertyChanged("LikeCount");
                    NotifyPropertyChanged("LikeImageUri");
                }
            }
        }

        

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }

}