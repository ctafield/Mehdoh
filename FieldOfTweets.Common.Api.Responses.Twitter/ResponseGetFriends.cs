namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class ResponseGetFriends
    {

        public string next_cursor_str { get; set; }

        public long previous_cursor { get; set; }

        public string previous_cursor_str { get; set; }

        public long[] ids { get; set; }

        public long next_cursor { get; set; }

    }

}
