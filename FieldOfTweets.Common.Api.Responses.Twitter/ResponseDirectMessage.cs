﻿namespace FieldOfTweets.Common.Api.Twitter.Responses
{

    public class ResponseDirectMessage
    {
        
        public ResponseEntities entities { get; set; }
        
        public UserClass recipient { get; set; }
        
        public string created_at { get; set; }
        
        public long recipient_id { get; set; }
        
        public string sender_screen_name { get; set; }
        
        public string id_str { get; set; }
        
        public UserClass sender { get; set; }
        
        public string recipient_screen_name { get; set; }
        
        public string text { get; set; }
        
        public long sender_id { get; set; }
        
        public long id { get; set; }

        public string lang { get; set; }

    }

}