namespace FieldOfTweets.Common
{

    public class VersionInfo
    {

	    public static int Major
        {
#if WP8
            get { return 8; }
#elif WP7
            get { return 7; }
#endif

        }

		private static int TargetVersion
		{
#if WP8
            get { return 8; }
#elif WP7
			get { return 7; }
#endif
		}

        public static int Minor
        {
            get { return 25; }
        }

        public static long BuildNumber
        {
			get
			{
				return 2629;
			}
        }

		public static string FullVersion()
        {
			return string.Format("Version {0}.{1}.{2}", Major, Minor, BuildNumber);        
		}

    }

}
