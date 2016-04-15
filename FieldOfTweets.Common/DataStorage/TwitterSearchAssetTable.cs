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

    [Index(Columns = "ParentId", IsUnique = false, Name = "idxTwitterSearchAssetId")]
    [Table]
    public class TwitterSearchAssetTable : IAssetTable
    {

        private EntityRef<TwitterSearchTable> _twitterSearchRef;

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
        public long ListId { get; set; }

        [Column]
        public string SearchQuery { get; set; }

        [Column]
        public long ParentId { get; set; }

        [Association(Name = "FK_TwitterSearch_Assets", IsForeignKey = true, ThisKey = "ParentId", OtherKey = "TableId", Storage = "_twitterSearchRef")]
        public TwitterSearchTable TwitterSearch
        {
            get { return _twitterSearchRef.Entity; }
            set
            {
                TwitterSearchTable previousValue = this._twitterSearchRef.Entity;
                if (((previousValue != value) || (this._twitterSearchRef.HasLoadedOrAssignedValue == false)))
                {
                    if ((previousValue != null))
                    {
                        this._twitterSearchRef.Entity = null;
                        previousValue.Assets.Remove(this);
                    }
                    this._twitterSearchRef.Entity = value;
                    if ((value != null))
                    {
                        value.Assets.Add(this);
                        this.ListId = value.Id;
                        this.SearchQuery = value.SearchQuery;
                    }
                }
            }
        }


    }

}
