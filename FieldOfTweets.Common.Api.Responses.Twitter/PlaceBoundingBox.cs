namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class PlaceBoundingBox
    {
        public string type { get; set; }
        public double[][][] coordinates { get; set; }
    }

}