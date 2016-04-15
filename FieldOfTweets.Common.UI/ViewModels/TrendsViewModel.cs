using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class TrendsViewModel : INotifyPropertyChanged
    {
        
        public ObservableCollection<TrendViewModel> WorldwideTrends { get; set; }
        public ObservableCollection<TrendViewModel> LocalTrends { get; set; }
        public ObservableCollection<TrendViewModel> CountryTrends { get; set; }
                
        public TrendsViewModel()
        {
            WorldwideTrends = new ObservableCollection<TrendViewModel>();
            LocalTrends = new ObservableCollection<TrendViewModel>();
            CountryTrends = new ObservableCollection<TrendViewModel>();
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
