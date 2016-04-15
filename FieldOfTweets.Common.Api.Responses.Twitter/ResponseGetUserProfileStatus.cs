using FieldOfTweets.Common.Responses;

namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class ResponseGetUserProfileStatus
    {

        public Place place { get; set; }
        public long? in_reply_to_user_id { get; set; }
        public long? in_reply_to_status_id { get; set; }
        public string text { get; set; }
        public string id_str { get; set; }
        public bool favorited { get; set; }
        public string created_at { get; set; }
        public string in_reply_to_status_id_str { get; set; }
        public Coordinates geo { get; set; }
        public string in_reply_to_screen_name { get; set; }
        public long id { get; set; }
        public string in_reply_to_user_id_str { get; set; }
        public string source { get; set; }
        public string[] contributors { get; set; }
        public Coordinates coordinates { get; set; }
        public bool retweeted { get; set; }
        public string retweet_count { get; set; }
        public bool truncated { get; set; }
    }

}