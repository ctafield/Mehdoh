using System.Data.Linq;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "ParentId", IsUnique = false, Name = "idxTimelineAssetTimelineId")]
    [Index(Columns = "Id", IsUnique = true, Name = "idxTimelineAssetId")]
    [Table]
    public class TimelineAssetTable : IAssetTable
    {

        private EntityRef<TimelineTable> _timelineRef;

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

        [Association(Name = "FK_Timeline_Assets", IsForeignKey = true, ThisKey = "ParentId", OtherKey = "TableId", Storage = "_timelineRef")]
        public TimelineTable Timeline
        {
            get { return _timelineRef.Entity; }
            set
            {
                TimelineTable previousValue = this._timelineRef.Entity;
                if (((previousValue != value) || (this._timelineRef.HasLoadedOrAssignedValue == false)))
                {
                    if ((previousValue != null))
                    {
                        this._timelineRef.Entity = null;
                        previousValue.Assets.Remove(this);
                    }
                    this._timelineRef.Entity = value;
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
