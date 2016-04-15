namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class ResponseGetUsersList
    {
        public string id_str { get; set; }
        public string name { get; set; }
        public bool following { get; set; }
        public string created_at { get; set; }
        public int subscriber_count { get; set; }
        public string slug { get; set; }
        public string full_name { get; set; }
        public string description { get; set; }
        public string mode { get; set; }
        public UserClass user { get; set; }
        public string uri { get; set; }
        public long id { get; set; }
        public int member_count { get; set; }
    }

}