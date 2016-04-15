using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FieldOfTweets.Common.UI.Friends
{

    public class HashtagCache
    {

        private static List<string> _hashtags;

        public static void AddHashtag(string hashTag)
        {

            if (string.IsNullOrEmpty(hashTag))
                return;

            var newName = hashTag.Replace("#", "");
            if (string.IsNullOrEmpty(newName))
                return;

            if (_hashtags == null)
                _hashtags = new List<string>();

            if (_hashtags.SingleOrDefault(x => string.Compare(x, newName, StringComparison.InvariantCultureIgnoreCase) == 0) == null)
                _hashtags.Add(newName);

        }

        public static List<string> SearchHashtag(string screenName)
        {

            if (screenName.Length < 1 || _hashtags == null)
                return null;

            return _hashtags.Where(x => x.ToLower(CultureInfo.InvariantCulture).Contains(screenName.ToLower(CultureInfo.InvariantCulture)))
                .Select(x => "#" + x)
                .OrderBy(x => x)
                .Take(30)
                .ToList();
        }

    }

}
