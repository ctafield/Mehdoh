namespace FieldOfTweets.Common.Api.Twitter.Responses
{
    public class Place
    {

        public string place_type { get; set; }
        public string country_code { get; set; }
        public string name { get; set; }
        public PlaceAttributes attributes { get; set; }
        public string full_name { get; set; }
        public string country { get; set; }
        public PlaceBoundingBox bounding_box { get; set; }
        public Place[] contained_within { get; set; }
        public string id { get; set; }
        public string url { get; set; }
    }
}