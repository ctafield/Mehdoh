using System.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Table]
    public class SentDirectMessageTable
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public long InternalId { get; set; }

        [Column]
        public long Id { get; set; }

        [Column]
        public string RecipientScreenName { get; set; }

        [Column]
        public string RecipientDisplayName { get; set; }

        [Column]
        public string Text { get; set; }

        [Column]
        public string CreatedAt { get; set; }

        [Column]
        public string SenderScreenName { get; set; }

        [Column]
        public string SenderDisplayName { get; set; }

        [Column]
        public string ProfileImageUrl { get; set; }
    }

}
