using System;
using System.Collections.Generic;
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

    public class StorifyNewStoryRequest
    {
        public StorifyNewStoryRequest()
        {
            story = new StorifyNewStoryRequestStory();
        }

        public string publish { get; set; }
        public StorifyNewStoryRequestStory story { get; set; }
    }

    public class StorifyNewStoryRequestStory
    {
        public string title { get; set; }
        public string slug { get; set; }
        public string description { get; set; }
        public string thumbnail { get; set; }
        public List<string> elements { get; set; }
    }

}
