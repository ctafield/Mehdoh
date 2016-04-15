using System.Collections.Generic;

namespace FieldOfTweets.Common.UI.ThirdPartyApi.Classes.Storify
{
    public class StorifyMeta
    {
        public List<object> quoted { get; set; }
        public List<object> hashtags { get; set; }
        public StorifyCreatedWith created_with { get; set; }
    }
}