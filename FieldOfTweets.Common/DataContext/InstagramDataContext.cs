using System.Data.Linq;
using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.DataContext
{
    public class InstagramDataContext : System.Data.Linq.DataContext
    {

        public InstagramDataContext() : base(ApplicationSettings.ConnectionString)
        {

        }

        public Table<InstagramLocationsTable> InstagramLocations
        {
            get
            {
                return GetTable<InstagramLocationsTable>();
            }
        }


    }
}
