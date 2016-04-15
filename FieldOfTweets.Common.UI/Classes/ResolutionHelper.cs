using System;
using System.Windows;

namespace FieldOfTweets.Common.UI.Classes
{
    public enum Resolutions { WVGA, WXGA, HD720p,
        HD1080p
    };

    public static class ResolutionHelper
    {

#if WP8
        private static bool IsWvga
        {
            get
            {
                return Application.Current.Host.Content.ScaleFactor == 100;
            }
        }

        private static bool IsWxga
        {
            get
            {
                return Application.Current.Host.Content.ScaleFactor == 160;
            }
        }

        private static bool Is720p
        {
            get
            {
                return Application.Current.Host.Content.ScaleFactor == 150;
            }
        }

        private static bool IsHD1080p
        {
            get
            {
                return Application.Current.Host.Content.ScaleFactor == 225;
            }
        }

#endif

        public static Resolutions CurrentResolution
        {
            get
            {
#if WP7
                return Resolutions.WVGA;                
#else                
                if (IsWvga) return Resolutions.WVGA;
                else if (IsWxga) return Resolutions.WXGA;
                else if (Is720p) return Resolutions.HD720p;
                else if (IsHD1080p) return Resolutions.HD1080p;
                else return Resolutions.WXGA;
#endif
            }
        }
    }
}