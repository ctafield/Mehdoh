using System;
using System.ComponentModel;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class SelectAccountViewModel : AccountViewModel, INotifyPropertyChanged
    {

        private bool _isSelected;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }

        public static SelectAccountViewModel FromAccountViewModel(AccountViewModel model)
        {

            return new SelectAccountViewModel()
                       {
                           DisplayName = model.DisplayName,
                           Id = model.Id,
                           ImageUrl = model.ImageUrl,
                           IsSelected = false,
                           ProfileType = model.ProfileType,
                           ScreenName = model.ScreenName,                           
                           UseToPost = model.UseToPost
                       };
        }

        #region Implementation of INotifyPropertyChanged

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


        #endregion
    }

}