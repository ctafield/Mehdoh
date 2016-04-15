using System.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{
    public interface IAssetTable
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        long Id { get; set; }

        long ParentId { get; set; }

        [Column(DbType = "int")]
        AssetTypeEnum Type { get; set; }

        [Column]
        string ShortValue { get; set; }

        [Column]
        string LongValue { get; set; }

        [Column]
        int StartOffset { get; set; }

        [Column]
        int EndOffset { get; set; }

        [Column]
        long EntityId { get; set; }

    }
}