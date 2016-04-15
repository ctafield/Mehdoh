using System.Runtime.Serialization;

namespace FieldOfTweets.Common.Responses.ImageHosts
{

    [DataContract()]
    public class ResponseYFrogInfo
    {
        [DataMember()]
        public string stat { get; set; }
        [DataMember()]
        public string mediaid { get; set; }
        [DataMember()]
        public string mediaurl { get; set; }
    }

    [DataContract()]
    public class ResponseYFrog
    {
        [DataMember()]
        public ResponseYFrogInfo rsp { get; set; }
    }

}
