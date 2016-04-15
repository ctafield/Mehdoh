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

namespace FieldOfTweets.Common.UI.ImageHost.Responses
{

    public class MobyPictureMedia
    {
        public int mediaid { get; set; }
        public string mediaurl { get; set; }
    }

    public class ResponseMobyPicture
    {
        public MobyPictureMedia media { get; set; }
    }

}
