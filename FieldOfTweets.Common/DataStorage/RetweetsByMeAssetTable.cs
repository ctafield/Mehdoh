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

    [Index(Columns = "ParentId", IsUnique = false, Name = "idxRetweetsByMeAssetRetweetsByMeId")]
    [Index(Columns = "Id", IsUnique = true, Name = "idxRetweetsByMeAssetId")]
    [Table]
    public class RetweetsByMeAssetTable : IAssetTable
    {

        private EntityRef<RetweetsByMeTable> _RetweetsByMeRef;

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

        [Association(Name = "FK_RetweetsByMe_Assets", IsForeignKey = true, ThisKey = "ParentId", OtherKey = "TableId", Storage = "_RetweetsByMeRef")]
        public RetweetsByMeTable RetweetsByMe
        {
            get { return _RetweetsByMeRef.Entity; }
            set
            {
                RetweetsByMeTable previousValue = this._RetweetsByMeRef.Entity;
                if (((previousValue != value) || (this._RetweetsByMeRef.HasLoadedOrAssignedValue == false)))
                {
                    if ((previousValue != null))
                    {
                        this._RetweetsByMeRef.Entity = null;
                        previousValue.Assets.Remove(this);
                    }
                    this._RetweetsByMeRef.Entity = value;
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
