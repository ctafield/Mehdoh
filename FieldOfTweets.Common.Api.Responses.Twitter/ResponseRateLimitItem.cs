namespace FieldOfTweets.Common.Api.Responses.Twitter
{

    public class ResponseRateLimitItem
    {
        public string title { get; set; } // populated manually
        public int limit { get; set; }
        public int remaining { get; set; }
        public int reset { get; set; }
    }

}
