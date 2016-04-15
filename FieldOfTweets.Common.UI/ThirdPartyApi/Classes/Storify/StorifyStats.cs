using System.Collections.Generic;

namespace FieldOfTweets.Common.UI.ThirdPartyApi.Classes.Storify
{
    public class StorifyStats
    {
        public int? popularity { get; set; }
        public int? views { get; set; }
        public int? likes { get; set; }
        public int? comments { get; set; }
        public int? elementComments { get; set; }
        public List<object> embeds { get; set; }
        public StorifyElements elements { get; set; }
    }
}