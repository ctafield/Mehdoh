using System;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class InstagramLikeViewModel
    {
        public string UserId { get; set; }

        public string ScreenName { get; set; }

        public string Description { get; set; }

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
