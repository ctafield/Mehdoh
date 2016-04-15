using System;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class MessagesViewModel : TimelineViewModel, IComparable<MessagesViewModel>
    {
        public int CompareTo(MessagesViewModel other)
        {
            if (other == null)
                return 1;

            return other.Id.CompareTo(Id);
        }
    }

}
