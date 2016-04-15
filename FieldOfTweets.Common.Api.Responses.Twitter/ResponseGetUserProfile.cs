using System.Collections.Generic;
using Newtonsoft.Json;

namespace FieldOfTweets.Common.Api.Twitter.Responses
{
    public class ResponseGetUserProfile
    {

        [JsonProperty(PropertyName = "protected")]
        public bool is_protected { get; set; }
        public string notifications { get; set; }
        public string profile_text_color { get; set; }
        public string profile_image_url_https { get; set; }
        public bool default_profile_image { get; set; }
        public string profile_sidebar_fill_color { get; set; }
        public string location { get; set; }
        public string name { get; set; }
        public bool contributors_enabled { get; set; }
        public bool profile_background_tile { get; set; }
        public bool is_translator { get; set; }
        public int? utc_offset { get; set; }
        public string url { get; set; }
        public string id_str { get; set; }
        public bool default_profile { get; set; }
        public string follow_request_sent { get; set; }
        public bool? following { get; set; }
        public bool verified { get; set; }
        public long favourites_count { get; set; }
        public long? friends_count { get; set; }
        public string profile_link_color { get; set; }
        public string description { get; set; }
        public string created_at { get; set; }
        public string profile_sidebar_border_color { get; set; }
        public ResponseGetUserProfileStatus status { get; set; }
        public string time_zone { get; set; }
        public string profile_image_url { get; set; }
        public bool show_all_inline_media { get; set; }
        public bool geo_enabled { get; set; }
        public bool profile_use_background_image { get; set; }
        public long id { get; set; }
        public long? listed_count { get; set; }
        public string profile_background_color { get; set; }
        public long? followers_count { get; set; }
        public string screen_name { get; set; }
        public string profile_background_image_url { get; set; }
        public string lang { get; set; }
        public long? statuses_count { get; set; }
        public string profile_background_image_url_https { get; set; }
        public string profile_banner_url { get; set; }
        public ProfileEntities entities { get; set; }
    }

    public class ProfileEntities
    {
        public Url url { get; set; }
        public Description description { get; set; }
    }

    public class Url
    {
        public ResponseEntityUrl[] urls { get; set; }
    }

    public class Description
    {
        public ResponseEntityUrl[] urls { get; set; }
    }

}
