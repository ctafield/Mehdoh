namespace FieldOfTweets.Common.Api.Twitter.Responses
{
    public class StreamResponseDeleteDelete
    {
        public Status status { get; set; }
        public StreamDeleteDirectMessage direct_message { get; set; }
    }

    public class StreamDeleteDirectMessage
    {
        public string id_str { get; set; }
        public long id { get; set; }
        public long user_id { get; set; }
    }
}