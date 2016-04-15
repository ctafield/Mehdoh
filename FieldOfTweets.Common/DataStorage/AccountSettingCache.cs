using System;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "ProfileId", IsUnique = true, Name = "idxSettingCacheProfileId")]
    [Table]
    public class AccountSettingCache
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public long Id { get; set; }

        [Column()]
        public long ProfileId { get; set; }

        [Column()]
        public bool UseSSL { get; set; }

        [Column()]
        public DateTime DateCached { get; set; }

        [Column()]
        public string CachedContent { get; set; }

    }
}
