using System;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "Order", IsUnique = true, Name = "idxColumnConfigOrder")]
    [Table]
    [Obsolete("Use the column helper now instead")]
    public class ColumnConfigTable
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int Id { get; set; }

        // 1, 2, 3, etc...
        [Column]
        public int Order { get; set; }

        // Enum saying home, mentions, messages, favourites, list
        [Column]
        public int ColumnType { get; set; }

        // What to display
        [Column]
        public string DisplayName { get; set; }

        // Path to the list etc...
        [Column]
        public string Value { get; set; }

        [Column]
        public bool RefreshOnStartup { get; set; }

        [Column]
        public long AccountId { get; set; }

    }

}
