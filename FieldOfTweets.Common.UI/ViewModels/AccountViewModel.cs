using System;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class AccountViewModel
    {

        public double ViewOpacity
        {
            get
            {
                switch (ProfileType)
                {
                    case ApplicationConstants.AccountTypeEnum.Twitter:
                        return 1;
                    case ApplicationConstants.AccountTypeEnum.Instagram:
                        return 1;
                    case ApplicationConstants.AccountTypeEnum.Soundcloud:
                        return 1;
                    default:
                        return 0;
                }
            }
        }

        public double RefreshOpacity
        {
            get
            {
                switch (ProfileType)
                {
                    case ApplicationConstants.AccountTypeEnum.Twitter:
                        return 1;
                    case ApplicationConstants.AccountTypeEnum.Instagram:
                        return 1;
                    case ApplicationConstants.AccountTypeEnum.Soundcloud:
                        return 1;
                    default:
                        return 0;
                }
            }
        }

        public string ScreenName { get; set; }

        public string DisplayName { get; set; }

        public string ImageUrl { get; set; }

        public bool CanUseToPost
        {
            get
            {
                if (ProfileType == ApplicationConstants.AccountTypeEnum.Instagram || ProfileType == ApplicationConstants.AccountTypeEnum.Soundcloud)
                    return false;

                return true;
            }
        }

        public bool UseToPost { get; set; }

        public long Id { get; set; }

        public ApplicationConstants.AccountTypeEnum? ProfileType { get; set; }

        public Uri ProfileTypeImageUrl
        {

            get
            {
                if (ProfileType == ApplicationConstants.AccountTypeEnum.Twitter)
                {
                    return new Uri("/Images/profile_type_twitter.png", UriKind.Relative);
                }
                else if (ProfileType == ApplicationConstants.AccountTypeEnum.Soundcloud)
                {
                    return new Uri("/Images/profile_type_soundcloud.png", UriKind.Relative);
                }
                else if (ProfileType == ApplicationConstants.AccountTypeEnum.Instagram)
                {
                    return new Uri("/Images/profile_type_instagram.png", UriKind.Relative);
                }
#if WP8
                else if (ProfileType == ApplicationConstants.AccountTypeEnum.Facebook)
                {
                    return new Uri("/Images/profile_type_facebook.png", UriKind.Relative);
                }
#endif
                return new Uri("/Images/profile_type_twitter.png", UriKind.Relative);

            }
        }


    }

}
