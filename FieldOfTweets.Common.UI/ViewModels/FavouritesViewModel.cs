using System;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class FavouritesViewModel : TimelineViewModel, IComparable<FavouritesViewModel>
    {
        public int CompareTo(FavouritesViewModel other)
        {
            if (other == null)
                return 1;

            return other.Id.CompareTo(Id);
        }
    }
}
