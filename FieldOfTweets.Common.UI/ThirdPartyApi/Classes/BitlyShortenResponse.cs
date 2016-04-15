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

    public class BitlyShortenResponseData
    {
        public string long_url { get; set; }
        public string url { get; set; }
        public string hash { get; set; }
        public string global_hash { get; set; }
        public int? new_hash { get; set; }
    }

    public class BitlyShortenResponse
    {
        public int? status_code { get; set; }
        public string status_txt { get; set; }
        public BitlyShortenResponseData data { get; set; }
    }

}
