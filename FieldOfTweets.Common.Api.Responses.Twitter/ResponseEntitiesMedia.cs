namespace FieldOfTweets.Common.Api.Twitter.Responses
{
    
    public class ResponseEntitiesMedia
    {

        public long id { get; set; }

        public string id_str { get; set; }
        
        public string type { get; set; } // generally only "photo" for now
                
        public string display_url { get; set; }
        
        public string expanded_url { get; set; }
        
        public string media_url_https { get; set; }
        
        public string url { get; set; }
        
        public int[] indices { get; set; }
        
        public MediaSizes sizes { get; set; }
               
        public string media_url { get; set; }
    }

}