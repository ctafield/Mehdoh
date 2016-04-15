using System.Data.Linq;
using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.DataContext
{
    public class SettingsDataContext : System.Data.Linq.DataContext
    {

        public SettingsDataContext()
            : base(ApplicationSettings.ConnectionString)
        {

        }

        public Table<SettingsTable> Settings
        {
            get
            {
                return this.GetTable<SettingsTable>();
            }
        }

    }

}
