using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows;
using FieldOfTweets.Common.UI.ImageCaching;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class PhotoViewModel : INotifyPropertyChanged, IComparable<PhotoViewModel>
    {

        private long _id;

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

        public int CompareTo(PhotoViewModel obj)
        {
            if (obj == null)
                return 1;

            return obj.Id.CompareTo(Id);

        }

        // set this in code
        private Uri _imageUri;

        public Uri ImageUri
        {
            get
            {
                return _imageUri;
            }
            set
            {
                _imageUri = value;
                if (_imageUri != null)
                {
                    UpdateImageSource();
                }
            }
        }

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

                UpdateImageSource();

                return _imageSource;
            }
        }

        private void UpdateImageSource()
        {            

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                var model = state as PhotoViewModel;
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

        // date time
        private DateTime? _createdAtDate;

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
        }


        private string _createdAt;

        public string CreatedAt
        {
            get
            {
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

        private bool _isGap;

        private Visibility _gapVisibility = Visibility.Collapsed;


        public Visibility GapVisibility
        {
            get
            {
                return _gapVisibility;
            }
        }

        public bool IsGap
        {
            get
            {
                return _isGap;
            }
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

        public bool HasImage { get; set; }

        public long AccountId { get; set; }

    }

}