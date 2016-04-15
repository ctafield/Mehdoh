namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class ResponseTweet
    {
        
        // dont care about these yet
        //public long[] contributors { get; set; }
        //public string in_reply_to_user_id_str { get; set; }

        public Coordinates coordinates { get; set; }
        public string created_at { get; set; }
        public ResponseEntities entities { get; set; }
        public ResponseEntities extended_entities { get; set; }
        public bool? favorited { get; set; }
        public Coordinates geo { get; set; }
        public long id { get; set; }
        public string id_str { get; set; }
        public string in_reply_to_screen_name { get; set; }
        public long? in_reply_to_status_id { get; set; }
        public string in_reply_to_status_id_str { get; set; }
        public long? in_reply_to_user_id { get; set; }
        public Place place { get; set; }
        public string retweet_count { get; set; }
        public bool? retweeted { get; set; }
        public ResponseTweet retweeted_status { get; set; }
        public string source { get; set; }
        public string text { get; set; }
        //public bool? truncated { get; set; }
        public UserClass user { get; set; }
        public bool? possibly_sensitive { get; set; }
        public string lang { get; set; }
        
        // This is set by the streaming tweets
        public bool? IsTimelineTweet { get; set; }
        public bool? IsMentionTweet { get; set; }

    }

}