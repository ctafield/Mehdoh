namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class ResponseEntitiesMention
    {
        public long id { get; set; }

        public int[] indices { get; set; }

        public string name { get; set; }

        public string screen_name { get; set; }    
    }

}