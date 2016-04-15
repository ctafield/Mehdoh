using Telerik.Windows.Controls;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class DataItemViewModel : ViewModelBase
    {

        private string title;
        private string information;
        private int _woeId;
        private bool _isSelected;

        public bool IsSelected
        {
            get
            {
                return this._isSelected;
            }
            set
            {
                if (this._isSelected != value)
                {
                    this._isSelected = value;
                    this.OnPropertyChanged("IsSelected");
                }
            }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title
        {
            get
            {
                return this.title;
            }
            set
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
            get { return this._woeId; }
            set
            {
                if (this._woeId != value)
                {
                    this._woeId = value;
                    this.OnPropertyChanged("WoeId");
                }
            }
        }
        
        public CollectionDataItemViewModel ParentObject { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.title;
        }

        /// <summary> 
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance. 
        /// </summary> 
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param> 
        /// <returns> 
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.  

        /// </returns> 
        public override bool Equals(object obj)
        {
            var typedObject = obj as DataItemViewModel;
            if (typedObject == null)
            {
                return false;
            }
            return this.Title == typedObject.Title && this.WoeId == typedObject.WoeId;
        }

        /// <summary> 
        /// Returns a hash code for this instance. 
        /// </summary> 
        /// <returns> 
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.   

        /// </returns> 
        public override int GetHashCode()
        {
            return this.Title.GetHashCode() ^ this.WoeId.GetHashCode();
        }
    }
}
