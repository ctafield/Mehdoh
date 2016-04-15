using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "ParentId", IsUnique = false, Name = "idxRetweetsOfMeAssetRetweetsOfMeId")]
    [Index(Columns = "Id", IsUnique = true, Name = "idxRetweetsOfMeAssetId")]
    [Table]
    public class RetweetsOfMeAssetTable : IAssetTable
    {

        private EntityRef<RetweetsOfMeTable> _retweetsOfMeRef;

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

        [Association(Name = "FK_RetweetsOfMe_Assets", IsForeignKey = true, ThisKey = "ParentId", OtherKey = "TableId", Storage = "_retweetsOfMeRef")]
        public RetweetsOfMeTable RetweetsOfMe
        {
            get { return _retweetsOfMeRef.Entity; }
            set
            {
                RetweetsOfMeTable previousValue = this._retweetsOfMeRef.Entity;
                if (((previousValue != value) || (this._retweetsOfMeRef.HasLoadedOrAssignedValue == false)))
                {
                    if ((previousValue != null))
                    {
                        this._retweetsOfMeRef.Entity = null;
                        previousValue.Assets.Remove(this);
                    }
                    this._retweetsOfMeRef.Entity = value;
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
