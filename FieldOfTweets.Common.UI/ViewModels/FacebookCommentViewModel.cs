using System;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class FacebookCommentViewModel
    {
        public string PhotoUrl { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        
        private string _createdAt;
        public string CreatedAt
        {
            get
            {
                // ISO-8601
                // 2012-02-09T21:35:54+0000
                var properDate = DateTime.Parse(_createdAt);

                var age = DateTime.Now.Subtract(properDate);

                if (age.TotalDays > 1)
                {
                    return string.Format("{0}d", age.Days);
                }

                if (age.TotalHours > 1)
                {
                    return string.Format("{0}h", age.Hours);
                }

                if (age.TotalMinutes > 1)
                    return string.Format("{0}m", age.Minutes);

                return string.Format("{0}s", age.Seconds < 0 ? 0 : age.Seconds);


            }
            set
            {
                _createdAt = value;
            }
        }

    }

}