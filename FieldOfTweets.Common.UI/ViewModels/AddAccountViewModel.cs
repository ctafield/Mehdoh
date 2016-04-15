using System.Windows;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class AddAccountViewModel
    {

        public ApplicationConstants.AccountTypeEnum AccountType { get; set; }
        public string AccountName { get; set; }
        public string ImageUri { get; set; }
        public string Subtitle{ get; set; }

        public Visibility SubtitleVisiblity
        {
            get { return string.IsNullOrEmpty(Subtitle) ? Visibility.Collapsed : Visibility.Visible; }
        }

    }

}
