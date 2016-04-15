using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using FieldOfTweets.Common.UI.Friends;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class SearchResultViewModel : INotifyPropertyChanged, IComparable<SearchResultViewModel>
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

        public bool IsReply { get; set; }


        public Visibility ReplyVisibility
        {
            get { return (IsReply) ? Visibility.Visible : Visibility.Collapsed; }
        }



        #region ImageUrl

        private string _imageUrl;

        public Uri ImageUri
        {
            get
            {
                if (_imageUrl.ToLower().EndsWith("gif"))
                    return new Uri(_imageUrl, UriKind.Absolute);
                else
                    return null;
            }
        }

        public string ImageUrl
        {
            get
            {
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

        private string _description;
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

        private long _userId;
        public long UserId
        {
            get { return _userId; }
            set
            {
                if (value != _userId)
                {
                    _userId = value;
                    NotifyPropertyChanged("Description");
                }
            }
        }

        #region Display Name

        private string _displayName;

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

        #region ScreenName

        public string ScreenNameDisplay
        {
            get { return "@" + ScreenName; }
        }

        private string _screenName;

        public string ScreenName
        {
            get
            {
                return _screenName;
            }
            set
            {
                if (value != _screenName)
                {
                    FriendsCache.AddFriend(value);

                    _screenName = value;
                    NotifyPropertyChanged("ScreenName");
                }
            }
        }

        #endregion

        // Other authors too
        public string OtherAuthors { get; set; }

        #region Author

        private string _author;

        public string Author
        {
            get
            {
                if (string.IsNullOrEmpty(_author))
                {
                    _author = SettingsHelper.GetNameDisplayModeCached() == ApplicationConstants.NameDisplayModeEnum.ScreenName ? ScreenNameDisplay : DisplayName;
                }
                return _author;
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

        private string _descriptionRt;

        public string DescriptionRT
        {
            get
            {
                if (string.IsNullOrEmpty(_descriptionRt))
                {
                    _descriptionRt = GetRetweetText(ScreenName, Description);
                }
                return _descriptionRt;
            }
        }
        #region Created At

        private string _createdAt;
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
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

        private DateTime? _createdAtDate;
        public DateTime CreatedAtDate
        {
            get
            {
                if (_createdAtDate != null)
                    return _createdAtDate.Value;

                //const string format = "ddd, dd MMM yyyy HH:mm:ss zzzz";
                const string format = "ddd MMM dd HH:mm:ss zzzz yyyy";

                _createdAtDate = DateTime.ParseExact(_createdAt, format, CultureInfo.InvariantCulture);
                return _createdAtDate.Value;
            }
            set
            {
                _createdAtDate = value;
            }
        }

        #endregion

        #region Client

        private string _client;

        public string Client
        {
            get { return _client; }
            set
            {
                if (value != _client)
                {
                    _client = value;
                    NotifyPropertyChanged("Client");
                }
            }
        }

        public long AccountId { get; set; }

        #endregion


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public int CompareTo(SearchResultViewModel other)
        {
            if (other == null)
                return 1;

            return other.Id.CompareTo(Id);
        }

    }
}
