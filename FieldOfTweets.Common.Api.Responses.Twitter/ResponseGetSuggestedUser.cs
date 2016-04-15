namespace FieldOfTweets.Common.Api.Twitter.Responses
{
    public class ResponseGetSuggestedUser
    {
        public int size { get; set; }
        public string name { get; set; }
        public UserClass[] users { get; set; }
        public string slug { get; set; }
    }
}
