namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class ResponseEntityUrl
    {
        public string display_url { get; set; }
        public string expanded_url { get; set; }
        public int[] indices { get; set; }
        public string url { get; set; }
    }

}