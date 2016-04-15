using System.Collections.Generic;

namespace FieldOfTweets.Common.Api
{

    public static class OAuthCache
    {
        public static Dictionary<long, OAuthCacheTokens> Cache;

        static OAuthCache()
        {
            Cache = new Dictionary<long, OAuthCacheTokens>();
        }

        private static readonly object locker = new object();
        public static void Add(long accountId, OAuthCacheTokens item)
        {
            lock (locker)
            {
                if (!Cache.ContainsKey(accountId))
                    Cache.Add(accountId, item);
            }            
        }

    }

}
