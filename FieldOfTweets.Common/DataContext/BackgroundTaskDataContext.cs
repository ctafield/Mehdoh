using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.DataContext
{
    public class BackgroundTaskDataContext : System.Data.Linq.DataContext
    {

        public BackgroundTaskDataContext()
            : base(ApplicationSettings.ConnectionStringForBackgroundTask)
        {
        }

        protected BackgroundTaskDataContext(string connectionString)
            : base(connectionString)
        {
        }

        public Table<ShellStatusTable> ShellStatus
        {
            get
            {
                return GetTable<ShellStatusTable>();
            }
        }

        public Table<MentionTable> Mentions
        {
            get
            {
                return this.GetTable<MentionTable>();
            }
        }

        public Table<MessageTable> Messages
        {
            get
            {
                return this.GetTable<MessageTable>();
            }
        }

    }
}
