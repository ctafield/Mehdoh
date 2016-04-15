using System.Collections;
using System.Collections.Generic;
using FieldOfTweets.Common.UI.Classes;
using Telerik.Windows.Controls;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class HierarchicalDataViewModel : ViewModelBase
    {
        private List<CollectionDataItemViewModel> items;
        private List<KeyedList<CollectionDataItemViewModel, CollectionDataItemViewModel>> _groupedItems;

        public List<KeyedList<CollectionDataItemViewModel, CollectionDataItemViewModel>> GroupedItems
        {
            get { return _groupedItems; }
            set { _groupedItems = value; }
        }


        /// <summary>
        /// Initializes the items.
        /// </summary>
        private void InitializeItems()
        {
            this.items = new List<CollectionDataItemViewModel>();
            this._groupedItems = new List<KeyedList<CollectionDataItemViewModel, CollectionDataItemViewModel>>();

            //for (int i = 1; i <= 4; i++)
            //{
            //    CollectionDataItemViewModel newItem = new CollectionDataItemViewModel(string.Format("Collection {0}", i),
            //        string.Format("Detailed information for collection {0}", i));
            //    this.items.Add(newItem);
            //}
        }

        /// <summary>
        /// A collection for <see cref="DataItemViewModel"/> objects.
        /// </summary>
        public List<CollectionDataItemViewModel> Items
        {
            get
            {
                if (this.items == null)
                {
                    this.InitializeItems();
                }
                return this.items;
            }
            private set
            {
                this.items = value;
            }
        }


    }

    public class CollectionDataItemViewModel : ViewModelBase, ICollection<DataItemViewModel>
    {
        private List<DataItemViewModel> items;
        private string title;
        private int woeid;
        private int? parentId;

        public CollectionDataItemViewModel(string title, int woeId, int? parentid)
        {
            this.title = title;
            this.woeid = woeId;
            this.parentId = parentid;
            this.InitializeItems();
        }


        /// <summary>
        /// Gets or sets the title of the collection.
        /// </summary>
        public string Title
        {
            get
            {
                return this.title;
            }
            private set
            {
                if (this.title != value)
                {
                    this.title = value;
                    this.OnPropertyChanged("Title");
                }
            }
        }


        public int WoeId
        {
            get
            {
                return this.woeid;
            }
            private set
            {
                if (this.woeid != value)
                {
                    this.woeid = value;
                    this.OnPropertyChanged("WoeId");
                }
            }

        }


        public int? ParentId
        {
            get
            {
                return this.parentId;
            }
            private set
            {
                if (this.parentId != value)
                {
                    this.parentId = value;
                    this.OnPropertyChanged("ParentId");
                }
            }

        }

        /// <summary>
        /// A collection for <see cref="DataItemViewModel"/> objects.
        /// </summary>
        public List<DataItemViewModel> Items
        {
            get
            {
                return this.items;
            }
            private set
            {
                this.items = value;
            }
        }

        /// <summary>
        /// Initializes the items.
        /// </summary>
        private void InitializeItems()
        {
            this.items = new List<DataItemViewModel>();
        }

        public IEnumerator<DataItemViewModel> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(DataItemViewModel item)
        {
            items.Add(item);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Contains(DataItemViewModel item)
        {
            if (Items == null)
                Items = new List<DataItemViewModel>();
            return Items.Contains(item);
        }

        public void CopyTo(DataItemViewModel[] array, int arrayIndex)
        {
            Items.CopyTo(array, arrayIndex);
        }

        public bool Remove(DataItemViewModel item)
        {
            return Items.Remove(item);
        }

        public int Count
        {
            get
            {
                return Items.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
    }

}
