using System;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class InstagramSearchUserResultViewModel
    {
        public string UserId { get; set; }

        public string ScreenName { get; set; }

        public string FullName { get; set; }

        public string ImageUrl { get; set; }

        public Uri ImageSource
        {
            get
            {
                return new Uri(ImageUrl, UriKind.Absolute);
            }
        }

    }
}
