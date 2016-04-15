using System;
using System.ComponentModel;
using System.Windows;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class CustomiseItemCoreViewModel : INotifyPropertyChanged
    {

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

        private bool? _isChecked;

        public bool? IsChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    NotifyPropertyChanged("IsChecked");
                }
            }
        }

        private Visibility _checkVisible;

        public Visibility CheckVisible
        {
            get
            {
                return _checkVisible;
            }
            set
            {
                if (value != _checkVisible)
                {
                    _checkVisible = value;
                    NotifyPropertyChanged("CheckVisible");
                }
            }
        }

        public string Title { get; set; }
        
        public string SubTitle { get; set; }
        
        public string Value { get; set; }
        
        public string Description { get; set; }
        
        public int Type { get; set; }
        
        public int Order { get; set; }

        public long AccountId { get; set; }

        public bool RefreshOnStartUp { get; set; }

        public string ProfileImageUrl { get; set; }

    }
}
