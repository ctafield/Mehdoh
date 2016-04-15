using System;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class MentionsViewModel : TimelineViewModel, IComparable<MentionsViewModel>
    {
        public int CompareTo(MentionsViewModel other)
        {
            if (other == null)
                return 1;

            return other.Id.CompareTo(Id);
        }
    }

}