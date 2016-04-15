using System.Data.Linq;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "ParentId", IsUnique = false, Name = "idxMentionAssetMentionId")]
    [Index(Columns = "Id", IsUnique = true, Name = "idxMentionAssetId")]
    [Table]
    public class MentionAssetTable : IAssetTable
    {

        private EntityRef<MentionTable> _mentionRef;

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public long Id { get; set; }

        [Column(DbType = "int")]
        public AssetTypeEnum Type { get; set; }

        [Column]
        public string ShortValue { get; set; }

        [Column]
        public string LongValue { get; set; }

        [Column]
        public int StartOffset { get; set; }

        [Column]
        public int EndOffset { get; set; }

        [Column]
        public long EntityId { get; set; }

        [Column]
        public long ParentId { get; set; }

        [Association(Name = "FK_Mention_Assets", IsForeignKey = true, ThisKey = "ParentId", OtherKey = "TableId", Storage = "_mentionRef")]
        public MentionTable Mention
        {
            get { return _mentionRef.Entity; }
            set
            {
                MentionTable previousValue = this._mentionRef.Entity;
                if (((previousValue != value) || (this._mentionRef.HasLoadedOrAssignedValue == false)))
                {
                    if ((previousValue != null))
                    {
                        this._mentionRef.Entity = null;
                        previousValue.Assets.Remove(this);
                    }
                    this._mentionRef.Entity = value;
                    if ((value != null))
                    {
                        value.Assets.Add(this);
                        this.Id = value.Id;
                    }
                }
            }
        }


    }
}
