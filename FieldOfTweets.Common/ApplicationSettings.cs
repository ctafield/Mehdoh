using System;

namespace FieldOfTweets.Common
{

    public static class ApplicationSettings
    {
        [Obsolete("no longer used")]
        public const string ConnectionString_v1 = @"isostore:/fot.sdf";


        public const string DatabaseFileName = @"fot_v2.sdf";        
        public const string ConnectionString = @"data source=isostore:/fot_v2.sdf;max buffer size=1024;max database size=128";
        public const string ConnectionStringForBackgroundTask = @"data source=isostore:/fot_v2.sdf;max buffer size=1024;max database size=128";

        public const string DatabaseFileNameTemp = @"fot_v2_temp.sdf";
        public const string ConnectionStringForCopying = @"data source=isostore:/fot_v2_temp.sdf;max buffer size=1024;max database size=128";

    }

}
