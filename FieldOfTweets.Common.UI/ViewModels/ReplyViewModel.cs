namespace FieldOfTweets.Common.UI.ViewModels
{
    public class ReplyViewModel
    {
        public string UserName { get; set; }

        public string Message
        {
            get;
            set;
        }

        public string Time
        {
            get;
            set;
        }

        public string PictureUrl
        {
            get;
            set;
        }

        public long Id { get; set; }
        public long AccountId { get; set; }
        public string DisplayName { get; set; }
    }
}
