using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{
    
    [Table]
    [Index(Columns = "Id", IsUnique = true, Name = "idxInstagramLocationsId")]
    public class InstagramLocationsTable
    {

        [Column(IsPrimaryKey = true)]
        public long Id { get; set; }

        [Column(CanBeNull = true)]
        public double? Latitude { get; set; }

        [Column(CanBeNull = true)]
        public double? Longitude { get; set; }

        [Column]
        public string Name { get; set; }

    }

}
