namespace FieldOfTweets.Common.Api.Twitter
{

    public static class TwitterConstants
    {

        public static string ConsumerKey
        {
            get
            {
#if MEHDOH_FREE
                return "LSFH2Gb415CGLN73peZjw"; // this is mehdoh free
#else
                return "qaIzOD8mFkdm3grgvJCv7o4Ze"; // this is mehdoh
#endif
            }
        }

        public static string ConsumerKeySecret
        {
            get
            {
#if MEHDOH_FREE
                return "aomrKf67YYCwpy0iAR11jm3fIzDEtnSQqMQK2NJWstk"; // this is mehdoh free
#else
                return "9jt9y73vkcLXWkN3daSyJZaklGEP3XoQoTSCjGXUfgMGfqUjDv"; // this is mehdoh
#endif
            }
        }

        public static string OAuthVersion
        {
            get
            {
                return "1.0";
            }
        }

        public static string ApiVersion { get { return "1.1"; } }

        public static string CallbackUri
        {
            get
            {
                return "http://www.myownltd.co.uk";
            }
        }

        public static string BaseApiUrl
        {
            get
            {
                // always use SSL
                return "https://api.twitter.com/";

            }
        }

        public static string BaseAuthUrl
        {
            get
            {
                return "https://api.twitter.com/oauth";
            }
        }

        public static string AuthoriseUrl
        {
            get
            {
                return "https://api.twitter.com/oauth/authorize";
            }
        }

        public static string AccessTokenUrl
        {
            get
            {
                return "https://api.twitter.com/oauth/access_token";
            }
        }

        public static string CallbackUrl
        {
            get
            {
                return "http://www.myownltd.co.uk";
            }
        }

    }
}
