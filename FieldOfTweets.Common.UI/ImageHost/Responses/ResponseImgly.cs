using System.Runtime.Serialization;

namespace FieldOfTweets.Common.Responses.ImageHosts
{

    [DataContract]
    public class ResponseImglyUser
    {
        [DataMember]
        public string screen_name { get; set; }
        [DataMember]
        public int id { get; set; }
    }

    [DataContract]
    public class ResponseImgly
    {
        [DataMember]
        public string timestamp { get; set; }
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public int height { get; set; }
        [DataMember]
        public int width { get; set; }
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public ResponseImglyUser user { get; set; }
        [DataMember]
        public int size { get; set; }
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public object text { get; set; }
    }

}
