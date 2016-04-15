using System;
using System.ComponentModel;
using System.Web;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class TrendViewModel : INotifyPropertyChanged
    {

        public long AccountId { get; set; }

        public Uri NavigateUri
        {
            get
            {
                return new Uri("/SearchResults.xaml?accountId=" + AccountId + "&term=" + HttpUtility.UrlEncodeUnicode(Query), UriKind.Relative);
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                }

            }
        }

        private string _query;
        public string Query
        {
            get { return _query; }
            set
            {
                if (value != _query)
                {
                    _query = value;
                    NotifyPropertyChanged("Query");
                }

            }
        }

        private string _promoted;
        public string Promoted
        {
            get { return _promoted; }
            set
            {
                if (value != _promoted)
                {
                    _promoted = value;
                    NotifyPropertyChanged("Promoted");
                }

            }
        }

        private string _events;
        public string Events
        {
            get { return _events; }
            set
            {
                if (value != _events)
                {
                    _events = value;
                    NotifyPropertyChanged("Events");
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
