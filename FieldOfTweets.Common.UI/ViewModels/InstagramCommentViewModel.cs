using System;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class InstagramCommentViewModel : IComparable<InstagramCommentViewModel>
    {

        public string ScreenName { get; set; }
        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public Uri ImageSource
        {
            get
            {
             return new Uri(ImageUrl, UriKind.Absolute);   
            }
        }

        public string Author
        {
            get
            {
                return "@" + ScreenName;
            }
        }


        private string _createdAt;
        public DateTime CreatedAtDate { get; set; }

        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
        public string CreatedAt
        {
            get
            {
                var properDate = CreatedAtDate;

                var age = DateTime.UtcNow.Subtract(properDate);

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
                if (value != _createdAt)
                {
                    _createdAt = value;                    
                }
            }
        }

        public string UserId { get; set; }

        #region Implementation of IComparable<InstagramCommentViewModel>

        public int CompareTo(InstagramCommentViewModel other)
        {
            return DateTime.Compare(other.CreatedAtDate, this.CreatedAtDate);
        }

        #endregion
    }
}
