using System.Collections.Generic;

namespace FieldOfTweets.Common.UI.ThirdPartyApi.Classes.Storify
{
    public class StorifyContent
    {
        public string sid { get; set; }
        public string title { get; set; }
        public string slug { get; set; }
        public string status { get; set; }
        public int? version { get; set; }
        public string permalink { get; set; }
        public object description { get; set; }
        public string thumbnail { get; set; }
        public StorifyDate date { get; set; }
        public bool @private { get; set; }
        public List<object> topics { get; set; }
        public List<object> siteposts { get; set; }
        public StorifyMeta meta { get; set; }
        public StorifyStats stats { get; set; }
        public bool modified { get; set; }
        public bool deleted { get; set; }
    }
}