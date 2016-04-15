using System.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Table]
    public class TrendLocationTable
    {
        [Column(IsDbGenerated = false, IsPrimaryKey = true)]
        public int WoeId { get; set; }

        [Column(IsDbGenerated = false)]
        public string Name { get; set; }
    }

}
