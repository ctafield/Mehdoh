namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class RelationshipSource
    {
        public bool? want_retweets { get; set; }
        public bool blocking { get; set; }
        public string id_str { get; set; }
        public bool marked_spam { get; set; }
        public bool all_replies { get; set; }
        public bool followed_by { get; set; }
        public bool notifications_enabled { get; set; }
        public bool following { get; set; }
        public string screen_name { get; set; }
        public long id { get; set; }
        public bool can_dm { get; set; }
    }

}