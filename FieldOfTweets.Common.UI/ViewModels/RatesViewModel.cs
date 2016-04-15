using System;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class RatesViewModel
    {
        private string _timeToReset;

        public int Remaining { get; set; }
        public int Limit { get; set; }
        public string ItemTitle { get; set; }

        public string TimeToReset
        {
            get { 
                var doubleValue = double.Parse(_timeToReset);
                var timeWhenResets = ConvertFromUnixTimestamp(doubleValue);
                var difference = timeWhenResets.Subtract(DateTime.UtcNow);
                return string.Format("{0}mins {1}secs", difference.Minutes, difference.Seconds);
            }
            set { _timeToReset = value; }
        }

        private static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            return epoch.AddSeconds(timestamp);
        }

    }

}