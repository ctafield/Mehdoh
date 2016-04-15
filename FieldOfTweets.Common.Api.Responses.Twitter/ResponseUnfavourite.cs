using FieldOfTweets.Common.Responses;

namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class ResponseUnfavourite
    {

        public string retweet_count { get; set; }

        public long? in_reply_to_user_id { get; set; }

        public long? in_reply_to_status_id { get; set; }

        public UserClass user { get; set; }

        public string id_str { get; set; }

        public string geo { get; set; }

        public bool favorited { get; set; }

        public string in_reply_to_status_id_str { get; set; }

        public string text { get; set; }

        public string in_reply_to_screen_name { get; set; }

        public string in_reply_to_user_id_str { get; set; }

        public string coordinates { get; set; }

        public long[] contributors { get; set; }

        public bool retweeted { get; set; }

        public string source { get; set; }

        public string place { get; set; }

        public long id { get; set; }

        public bool truncated { get; set; }

        public string created_at { get; set; }

    }


}
