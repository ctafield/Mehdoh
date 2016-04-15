using FieldOfTweets.Common.Responses;

namespace FieldOfTweets.Common.Api.Twitter.Responses
{    
    public class ResponseEntities
    {
        public ResponseEntitiesHashtag[] hashtags { get; set; }

        public ResponseEntitiesMedia[] media { get; set; }

        public ResponseEntityUrl[] urls { get; set; }

        public ResponseEntitiesMention[] user_mentions { get; set; }
    }
}