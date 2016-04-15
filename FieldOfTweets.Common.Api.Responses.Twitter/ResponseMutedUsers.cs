using System.Collections.Generic;
using FieldOfTweets.Common.Api.Twitter.Responses;

namespace FieldOfTweets.Common.Api.Responses.Twitter
{

    public class ResponseMutedUsers
    {
        public List<ResponseGetUserProfile> users { get; set; }
        public int next_cursor { get; set; }
        public string next_cursor_str { get; set; }
        public int previous_cursor { get; set; }
        public string previous_cursor_str { get; set; }
    }

}
