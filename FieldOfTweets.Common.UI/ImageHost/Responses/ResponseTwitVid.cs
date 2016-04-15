namespace FieldOfTweets.Common.Responses.ImageHosts
{

    public class ResponseTwitVidInfo
    {
        public string stat { get; set; }
        public string user_id { get; set; }
        public string media_id { get; set; }
        public string media_url { get; set; }
        public string user_tags { get; set; }
        public string geo_latitude { get; set; }
        public string geo_longitude { get; set; }
        public string message { get; set; }
        public string last_byte { get; set; }
        public string status_id { get; set; }
    }

    public class ResponseTwitVid
    {
        public ResponseTwitVidInfo rsp { get; set; }
    }

}
