namespace FieldOfTweets.Common.Responses
{

    public class ResponseGetSavedSearch
    {
        public string query { get; set; }
        public string name { get; set; }
        public string position { get; set; }
        public long id { get; set; }
        public string created_at { get; set; }
        public string id_str { get; set; }        
    }

}
