using System.Data.Linq;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "ParentId", IsUnique = false, Name = "idxMessageAssetMessageId")]
    [Index(Columns = "Id", IsUnique = true, Name = "idxMessageAssetId")]
    [Table]
    public class MessageAssetTable : IAssetTable
    {

        private EntityRef<MessageTable> _messageRef;

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

        [Association(Name = "FK_Message_Assets", IsForeignKey = true, ThisKey = "ParentId", OtherKey = "TableId", Storage = "_messageRef")]
        public MessageTable Message
        {
            get { return _messageRef.Entity; }
            set
            {
                MessageTable previousValue = this._messageRef.Entity;
                if (((previousValue != value) || (this._messageRef.HasLoadedOrAssignedValue == false)))
                {
                    if ((previousValue != null))
                    {
                        this._messageRef.Entity = null;
                        previousValue.Assets.Remove(this);
                    }
                    this._messageRef.Entity = value;
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
