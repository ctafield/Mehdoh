namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class TrendLocation
    {        
        public string url { get; set; }        
        public string name { get; set; }        
        public string countryCode { get; set; }        
        public int woeid { get; set; }        
        public int? parentid { get; set; }        
        public PlaceType placeType { get; set; }        
        public string country { get; set; }
    }

}