using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Globalization;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "Id,ProfileId", IsUnique = true, Name = "idxMentionId")]
    [Index(Columns = "ProfileId", IsUnique = false, Name = "idxMentionProfileId")]
    [Table]
    public class MentionTable : ITweetTable
    {

        private readonly EntitySet<MentionAssetTable> _assetsRef;

        public MentionTable()
        {
            _assetsRef = new EntitySet<MentionAssetTable>(OnAssetsTableAdded, OnAssetsTableRemoved);
        }

        [Association(Name = "FK_Mention_Assets", DeleteRule = "Cascade", ThisKey = "TableId", OtherKey = "ParentId", Storage = "_assetsRef")]
        public EntitySet<MentionAssetTable> Assets
        {
            get { return _assetsRef; }
        }

        private void OnAssetsTableAdded(MentionAssetTable asset)
        {
            asset.Mention = this;
        }

        private void OnAssetsTableRemoved(MentionAssetTable asset)
        {
            asset.Mention = null;
        }

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public long TableId { get; set; }

        [Column]
        public long Id { get; set; }

        [Column]
        public long ProfileId { get; set; }

        [Column]
        public bool IsRetweet { get; set; }

        [Column]
        public string IdStr { get; set; }

        [Column]
        public string Description { get; set; }

        [Column]
        public string Client { get; set; }

        [Column]
        public string DisplayName { get; set; }

        [Column]
        public string ScreenName { get; set; }

        [Column]
        public string CreatedAt { get; set; }

        public DateTime CreatedAtFormatted
        {
            get
            {
                const string format = "ddd MMM dd HH:mm:ss zzzz yyyy";
                return DateTime.ParseExact(CreatedAt, format, CultureInfo.InvariantCulture);
            }
        }

        [Column]
        public long CreatedById { get; set; }

        [Column]
        public string ProfileImageUrl { get; set; }

        [Column]
        public string RetweetDescription { get; set; }

        [Column]
        public string RetweetUserScreenName { get; set; }

        [Column]
        public string RetweetUserDisplayName { get; set; }

        [Column]
        public string RetweetUserImageUrl { get; set; }

        [Column]
        public bool Verified { get; set; }

        [Column]
        public bool RetweetUserVerified { get; set; }

        [Column]
        public string LocationFullName { get; set; }

        [Column]
        public string LocationCountry { get; set; }

        [Column]
        public double Location1X { get; set; }

        [Column]
        public double Location1Y { get; set; }

        [Column]
        public double Location2X { get; set; }

        [Column]
        public double Location2Y { get; set; }

        [Column]
        public double Location3X { get; set; }

        [Column]
        public double Location3Y { get; set; }

        [Column]
        public double Location4X { get; set; }

        [Column]
        public double Location4Y { get; set; }

        [Column]
        public long? InReplyToId { get; set; }

        [Column]
        public string LanguageCode { get; set; }

        [Column]
        public long? RetweetOriginalId { get; set; }
    }
}
