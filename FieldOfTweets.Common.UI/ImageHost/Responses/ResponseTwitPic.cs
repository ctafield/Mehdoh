using System.Runtime.Serialization;

namespace FieldOfTweets.Common.Responses.ImageHosts
{

    public class ResponseTwitPicUser
    {
        public int? id { get; set; }
        public string screen_name { get; set; }
    }

    public class ResponseTwitPic
    {
        public string id { get; set; }
        public string text { get; set; }
        public string url { get; set; }
        public int? width { get; set; }
        public int? height { get; set; }
        public int? size { get; set; }
        public string type { get; set; }
        public string timestamp { get; set; }        
        public ResponseTwitPicUser user { get; set; }
    }

}
