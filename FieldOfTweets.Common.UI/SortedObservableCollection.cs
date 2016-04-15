using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace FieldOfTweets.Common.UI
{

    public class SortedObservableCollection<T> : ObservableCollection<T>
            where T : IComparable<T>
    {

        private void SafeDispatch(Action action)
        {
            if (Deployment.Current.Dispatcher.CheckAccess())
            { // do it now on this thread 
                action.Invoke();
            }
            else
            {
                // do it on the UI thread 
                Deployment.Current.Dispatcher.BeginInvoke(action);
            }
        }

        protected override void InsertItem(int index, T item)
        {

            for (var i = 0; i < this.Count; i++)
            {
                switch (Math.Sign(this[i].CompareTo(item)))
                {
                    case 0: // same? return as we dont want duplicates
                        return;
                    case 1:
                        SafeDispatch(() => base.InsertItem(i, item));
                        return;
                    case -1:
                        break;
                }
            }

            SafeDispatch(() => base.InsertItem(this.Count, item));
            
        }

    }

}
