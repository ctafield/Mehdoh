using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    [Index(Columns = "OwnerScreenName", IsUnique = false, Name = "idxOwnerScreenName")]
    [Table]
    public class FriendsSnapshot
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public long Id { get; set; }

        [Column()]
        public string OwnerScreenName { get; set; }

        [Column()]
        public long FriendId { get; set; }
    }

}
