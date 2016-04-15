using System.Collections.ObjectModel;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class SearchResultsViewModel
    {

        public ObservableCollection<TimelineViewModel> Results { get; set; }

        public SearchResultsViewModel()
        {
            Results = new ObservableCollection<TimelineViewModel>();
        }

    }
}
