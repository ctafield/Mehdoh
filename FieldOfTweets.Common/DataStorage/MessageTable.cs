using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Globalization;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "ProfileId", IsUnique = false, Name = "idxMessageProfileId")]
    [Index(Columns = "Id", IsUnique = true, Name = "idxMessageId")]
    [Table]
    public class MessageTable : ITweetTable
    {

        private readonly EntitySet<MessageAssetTable> _assetsRef;

        public MessageTable()
        {
            _assetsRef = new EntitySet<MessageAssetTable>(OnAssetsTableAdded, OnAssetsTableRemoved);
        }

        [Association(Name = "FK_Message_Assets", DeleteRule = "Cascade", ThisKey = "TableId", OtherKey = "ParentId", Storage = "_assetsRef")]
        public EntitySet<MessageAssetTable> Assets
        {
            get { return _assetsRef; }
        }

        private void OnAssetsTableAdded(MessageAssetTable asset)
        {
            asset.Message = this;
        }

        private void OnAssetsTableRemoved(MessageAssetTable asset)
        {
            asset.Message = null;
        }

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public long TableId { get; set; }

        [Column]
        public long Id { get; set; }

        [Column]
        public long ProfileId { get; set; }

        [Column]
        public string IdStr { get; set; }

        [Column]
        public string Description { get; set; }

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

        public string Client { get; set; }

        [Column]
        public bool Verified { get; set; }

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


        // Never used in messages but here to comform to the interface
        public string RetweetUserDisplayName { get; set; }
        public string RetweetUserImageUrl { get; set; }
        public bool RetweetUserVerified { get; set; }
        public string RetweetUserScreenName { get; set; }
        public string RetweetDescription { get; set; }
        public long? InReplyToId { get
        {
            return 0;
        } 
        }

        public bool IsRetweet
        {
            get
            {
                return false;
            }
        }

        [Column]
        public string LanguageCode { get; set; }

        [Column]
        public long? RetweetOriginalId { get; set; }
    }

}
