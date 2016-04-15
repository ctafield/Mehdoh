namespace FieldOfTweets.Common.Api.Twitter.Responses
{
    public class ResponseAccountSettings
    {
        
        public string language { get; set; }
        
        public bool discoverable_by_email { get; set; }
        
        public TrendLocation[] trend_location { get; set; }
        
        public SleepTime sleep_time { get; set; }
        
        public bool geo_enabled { get; set; }
        
        public bool always_use_https { get; set; }
        
        public TimeZone time_zone { get; set; }
    }

}
