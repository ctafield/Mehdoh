using System.Runtime.Serialization;

namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class MediaLarge
    {
        public int h { get; set; }

        public int w { get; set; }

        public string resize { get; set; }
    }
}