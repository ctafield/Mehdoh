using System;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{


    [Index(Columns = "UserId", IsUnique = true, Name = "idxUserLookupTableId")]
    [Table]
    public class UserLookupTable
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = false)]       
        public long UserId { get; set; }

        [Column]
        public string ScreenName { get; set; }

        [Column]
        public string DisplayName { get; set; }

        [Column]
        public string ProfileImageUrl { get; set; }

        [Column]
        public DateTime DateAdded { get; set; }

    }


}
