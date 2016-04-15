using System;
using System.ComponentModel;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class FriendViewModel : INotifyPropertyChanged, IComparable
    {

        private const string Letters = "abcdefghijklmnopqrstuvwxyz";

        public string GroupHeader
        {
            get
            {
                var first = ScreenName.Replace("@", "").Substring(0, 1).ToLower();
                return !Letters.Contains(first) ? "#" : first;
            }
        }

        private long _id;
        public long Id
        {
            get { return _id; }
            set
            {
                if (value != Id)
                {
                    _id = value;
                    NotifyPropertyChanged("Id");
                }
            }
        }

        private long _accountId;
        public long AccountId
        {
            get { return _accountId; }
            set
            {
                if (value != AccountId)
                {
                    _accountId = value;
                    NotifyPropertyChanged("AccountId");
                }
            }
        }

        private string _screenName;
        public string ScreenName
        {
            get
            {
                if (!_screenName.StartsWith("@"))
                    return "@" + _screenName;
                return _screenName;
            }
            set
            {
                if (value != _screenName)
                {
                    _screenName = value;
                    NotifyPropertyChanged("ScreenName");
                }
            }
        }

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

        private string _profileImageUrl;

        public string ProfileImageUrl
        {
            get { return _profileImageUrl; }
            set
            {
                if (value != _profileImageUrl)
                {
                    _profileImageUrl = value;
                    NotifyPropertyChanged("ProfileTypeImageUrl");
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


        public int CompareTo(object obj)
        {
            //return (obj as TimelineViewModel).CreatedAtDate.CompareTo(CreatedAtDate);
            var otherObj = obj as FriendViewModel;

            if (obj == null)
                return 1;

            if (otherObj == null)
                return -1;

            return string.Compare(ScreenName, otherObj.ScreenName, StringComparison.InvariantCultureIgnoreCase);

        }
    }

}
