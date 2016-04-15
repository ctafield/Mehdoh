using System.Data.Linq;
using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.DataContext
{

    public class SaveMentionsContext : System.Data.Linq.DataContext
    {


        public SaveMentionsContext() : base(ApplicationSettings.ConnectionString)
        {

        }

        public Table<ProfileTable> Profiles
        {
            get
            {
                return this.GetTable<ProfileTable>();
            }
        }


        public Table<MentionTable> Mentions
        {
            get
            {
                return this.GetTable<MentionTable>();
            }
        }

        public Table<MentionAssetTable> MentionAsset
        {
            get
            {
                return this.GetTable<MentionAssetTable>();
            }
        }


    }
}
