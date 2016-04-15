using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace FieldOfTweets.Common.UI.ThirdPartyApi.Classes
{
 
    public class Timeline
    {
        public long id { get; set; }
        public string marked_at { get; set; }
    }

    public class Mentions
    {
        public long id { get; set; }
        public string marked_at { get; set; }
    }

    public class TweetMarkerPost
    {
        public Timeline timeline { get; set; }
        public Mentions mentions { get; set; }
    }

}
