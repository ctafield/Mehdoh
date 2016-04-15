using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Globalization;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "Id,ProfileId", IsUnique = true, Name = "idxTimelineId")]
    [Index(Columns = "ProfileId", IsUnique = false, Name = "idxTimelineProfileId")]
    [Table]
    public class TimelineTable : ITweetTable
    {

        private readonly EntitySet<TimelineAssetTable> _assetsRef;

        public TimelineTable()
        {
            _assetsRef = new EntitySet<TimelineAssetTable>(OnAssetsTableAdded, OnAssetsTableRemoved);
        }

        [Association(Name = "FK_Timeline_Assets", DeleteRule = "Cascade", ThisKey = "TableId", OtherKey = "ParentId", Storage = "_assetsRef")]
        public EntitySet<TimelineAssetTable> Assets
        {
            get { return _assetsRef; }
        }

        private void OnAssetsTableAdded(TimelineAssetTable asset)
        {
            asset.Timeline = this;
        }

        private void OnAssetsTableRemoved(TimelineAssetTable asset)
        {
            asset.Timeline = null;
        }

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public long TableId { get; set; }

        [Column]
        public long Id { get; set; }

        [Column]
        public long ProfileId { get; set; }

        [Column()]
        public bool IsRetweet { get; set; }

        [Column()]
        public string IdStr { get; set; }

        [Column]
        public string Description { get; set; }

        [Column]
        public string RetweetDescripton { get; set; }

        public string RetweetDescription
        {
            get
            {
                return RetweetDescripton;
            }
            set
            {
                RetweetDescripton = value;
            }
        }

        [Column]
        public string Client { get; set; }

        [Column]
        public string DisplayName { get; set; }

        [Column]
        public string ScreenName { get; set; }

        [Column]
        public string CreatedAt { get; set; }

        public string DateFormat
        {
            get
            {
                const string format = "ddd MMM dd HH:mm:ss zzzz yyyy";
                return format;
            }
        }

        public DateTime CreatedAtFormatted
        {
            get
            {
                return DateTime.ParseExact(CreatedAt, DateFormat, CultureInfo.InvariantCulture);
            }
        }

        [Column]
        public long CreatedById { get; set; }

        [Column]
        public string ProfileImageUrl { get; set; }


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
