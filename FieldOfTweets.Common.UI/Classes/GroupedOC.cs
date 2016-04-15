using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FieldOfTweets.Common.UI.Classes
{

    public class GroupedOc<T> : ObservableCollection<T>
    {

        public GroupedOc(string name, IEnumerable<T> items)
        {
            this.Key = name;

            if (items != null)
                foreach (T item in items)
                {
                    this.Add(item);
                }
        }

        public override bool Equals(object obj)
        {
            var that = obj as GroupedOc<T>;
            return (that != null) && (this.Key.Equals(that.Key));
        }

        public string Key { get; set; }

    }

}
