using System;

namespace FieldOfTweets.Common.Settings
{

    public class BackgroundSettings
    {
        public bool OnlyUpdateOnWifi { get; set; }
        public bool EnableToast { get; set; }
        public bool SleepEnabled { get; set; }
        public DateTime SleepFrom { get; set; }
        public DateTime SleepTo { get; set; }
    }

}
