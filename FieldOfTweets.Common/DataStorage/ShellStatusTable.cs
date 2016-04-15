using System.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Table]
    public class ShellStatusTable
    {

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int Id { get; set; }

        [Column]
        public int MentionCount { get; set; }

        [Column]
        public int MessageCount { get; set; }

        [Column]
        public long LastMentionId { get; set; }

        [Column]
        public long LastMessageId { get; set; }
 
    }

}
