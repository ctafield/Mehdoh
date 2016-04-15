using System;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Table]
    [Index(Columns = "Id,ProfileId", IsUnique = true, Name = "idxInstagramNewsId")]
    public class InstagramNewsTable    
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public Guid TableId { get; set; }

        [Column]
        public string Id { get; set; }

        [Column]
        public long ProfileId { get; set; }

        [Column]
        public string ThumbnailUrl { get; set; }

        [Column]
        public string StandardResolutionUrl { get; set; }

        [Column]
        public string Caption { get; set; }

        [Column]
        public DateTime CreatedAt { get; set; }

        [Column]
        public string UserName { get; set; }

        [Column]
        public string FullName { get; set; }

        [Column]
        public string UserId { get; set; }

        [Column]
        public int LikeCount { get; set; }

        [Column]
        public string Filter { get; set; }

        [Column]
        public string Link { get; set; }

        [Column]
        public string LocationName { get; set; }

        [Column]
        public double? LocationLatitude { get; set; }

        [Column]
        public double? LocationLongitude { get; set; }

        [Column]
        public long? LocationId { get; set; }

        [Column]
        public string UserImageUrl { get; set; }

        [Column]
        public string Type{ get; set; }

        [Column]
        public string VideoUrl { get; set; }

    }

}
