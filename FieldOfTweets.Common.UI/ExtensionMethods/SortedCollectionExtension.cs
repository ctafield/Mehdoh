using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FieldOfTweets.Common.UI.ExtensionMethods
{
    public static class SortedCollectionExtension 
    {

        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> newItems)
        {
            if (collection == null)
                collection = new ObservableCollection<T>(newItems);
            else
            {
                foreach (var item in newItems)
                    collection.Add(item);
            }
        }
    }
}
