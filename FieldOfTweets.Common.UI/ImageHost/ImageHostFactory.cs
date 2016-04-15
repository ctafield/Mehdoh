using FieldOfTweets.Common.ImageHost;

namespace FieldOfTweets.Common.UI.ImageHost
{

    public static class ImageHostFactory
    {

        private static IImageHost _host;

        public static void ClearHost()
        {
            _host = null;
        }

        public static IImageHost Host
        {
            get
            {
                if (_host == null)
                {                    
                    var val = new SettingsHelper().GetImageHost();
                    switch (val)
                    {
                        case ApplicationConstants.ImageHostEnum.Imgly:
                            _host = new ImglyApi();
                            break;

                        case ApplicationConstants.ImageHostEnum.TwitPic:
                            _host = new TwitPicAPI();
                            break;

                        case ApplicationConstants.ImageHostEnum.yFrog:
                            _host = new YfrogAPI();
                            break;

                        case ApplicationConstants.ImageHostEnum.TwitVid: // now defunct, return twitter here
                            _host = new TwitterImageHostApi();
                            break;

                        case  ApplicationConstants.ImageHostEnum.Twitter:
                            _host = new TwitterImageHostApi();
                            break;

                        case ApplicationConstants.ImageHostEnum.MobyPicture:
                            _host = new MobyPictureImageHostApi();
                            break;

                        case ApplicationConstants.ImageHostEnum.SkyDrive:
                            _host = new SkyDriveHostApi();
                            break;

                        default:
                            _host = new TwitterImageHostApi(); // default to twitter now
                            break;                            
                    }                    
                }

                return _host;
            }
        }

    }

}
