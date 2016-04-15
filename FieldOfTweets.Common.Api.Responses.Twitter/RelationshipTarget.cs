namespace FieldOfTweets.Common.Api.Twitter.Responses
{
    
    public class RelationshipTarget
    {
        public string id_str { get; set; }
        public bool followed_by { get; set; }
        public bool following { get; set; }
        public string screen_name { get; set; }
        public long id { get; set; }
    }

}