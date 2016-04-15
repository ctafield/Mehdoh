using System;
using System.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    /// <summary>
    /// Added in DB_SCHEMA version 3
    /// </summary>
    [Table]
    public class RecentTextSearchTable
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int Id { get; set; }
        
        [Column]
        public string Description { get; set; }

        [Column]
        public DateTime Timestamp { get; set; }

    }
}
