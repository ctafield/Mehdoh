using System.Data.Linq;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "ParentId", IsUnique = false, Name = "idxFavouriteAssetFavouriteId")]
    [Index(Columns = "Id", IsUnique = true, Name = "idxFavouriteAssetId")]
    [Table]
    public class FavouritesAssetTable : IAssetTable
    {

        private EntityRef<FavouriteTable> _favouriteRef;

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

        [Association(Name = "FK_Favourite_Assets", IsForeignKey = true, ThisKey = "ParentId", OtherKey = "TableId", Storage = "_favouriteRef")]
        public FavouriteTable Favourite
        {
            get { return _favouriteRef.Entity; }
            set
            {
                FavouriteTable previousValue = this._favouriteRef.Entity;
                if (((previousValue != value) || (this._favouriteRef.HasLoadedOrAssignedValue == false)))
                {
                    if ((previousValue != null))
                    {
                        this._favouriteRef.Entity = null;
                        previousValue.Assets.Remove(this);
                    }
                    this._favouriteRef.Entity = value;
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
