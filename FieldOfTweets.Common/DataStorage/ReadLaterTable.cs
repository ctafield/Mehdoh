using System.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Table]
    public class ReadLaterTable
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int Id { get; set; }

        [Column()]
        public string InstapaperUsername { get; set; }

        [Column()]
        public string InstapaperPassword { get; set; }

        [Column()]
        public bool UseInstapaper { get; set; }

        [Column()]
        public string ReadItLaterUsername { get; set; }

        [Column()]
        public string ReadItLaterPassword { get; set; }

        [Column()]
        public bool UseReadItLater { get; set; }

    }

}
