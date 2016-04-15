using System;
using System.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Table]
    public class ThumbnailCacheTable
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public Guid ThumbnailId { get; set; }

        [Column]
        public string LongUrl { get; set; }

        [Column]
        public string LocalUri { get; set; }

    }

}
