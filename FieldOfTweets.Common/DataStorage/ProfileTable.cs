using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Globalization;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "Id", IsUnique = true, Name = "idxProfileId")]
    [Table]
    public class ProfileTable
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = false)]
        public long Id { get; set; }
        
        [Column()]
        public string ScreenName { get; set; }

        [Column]
        public string DisplayName { get; set; }

        public string CachedImageUri
        {
            get
            {
                var cached = StorageHelper.CachedImageUri(Id.ToString(CultureInfo.InvariantCulture));
                return !String.IsNullOrEmpty(cached) ? cached : ImageUri;
            }
        }

        [Column]
        public string ImageUri { get; set; }

        [Column]
        public string Bio { get; set; }

        [Column]
        public string Location { get; set; }

        [Column(CanBeNull = true)]
        public bool? Verified { get; set; }

        [Column(CanBeNull = true)]
        public ApplicationConstants.AccountTypeEnum? ProfileType { get; set; }

        [Column]
        public bool? UseToPost { get; set; }
       
    }

}
