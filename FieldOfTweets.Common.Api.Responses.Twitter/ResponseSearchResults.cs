using FieldOfTweets.Common.Responses;

namespace FieldOfTweets.Common.Api.Twitter.Responses
{
    public class ResponseSearchResult
    {
        public ResponseEntities entities { get; set; }

        public string created_at { get; set; }
        
        public string from_user { get; set; }
        
        public long from_user_id { get; set; }

        public string from_user_name { get; set; }
        
        public string from_user_id_str { get; set; }

        public Coordinates geo { get; set; }
        
        public long id { get; set; }
        
        public string id_str { get; set; }
        
        public string iso_language_code { get; set; }
        
        public SearchResultMetadata metadata { get; set; }
        
        public string profile_image_url { get; set; }

        public string source { get; set; }
        
        public string text { get; set; }
        
        public long? to_user_id { get; set; }
        
        public string to_user_id_str { get; set; }
        
        public string to_user { get; set; }

        public string to_user_name { get; set; }

    }
}
