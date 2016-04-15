using System.Collections.Generic;

namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    //public class TwitterList
    //{
    //    public string id_str { get; set; }
    //    public string uri { get; set; }
    //    public string name { get; set; }
    //    public string full_name { get; set; }
    //    public int member_count { get; set; }
    //    public string description { get; set; }
    //    public string slug { get; set; }
    //    public string mode { get; set; }
    //    public string created_at { get; set; }
    //    public UserClass user { get; set; }
    //    public bool following { get; set; }
    //    public int subscriber_count { get; set; }
    //    public int id { get; set; }
    //}

    public class ResponseGetListMembership
    {
        public List<ResponseGetUsersList> lists { get; set; }
        public long next_cursor { get; set; }
        public long previous_cursor { get; set; }
        public string next_cursor_str { get; set; }
        public string previous_cursor_str { get; set; }
    }

}
