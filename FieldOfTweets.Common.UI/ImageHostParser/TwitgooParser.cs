using System.Linq;

namespace FieldOfTweets.Common.ImageHostParser
{
    public class TwitgooParser : IImageHostParser
    {
        public string GetImageUrl(string contents)
        {
            var splitter = contents.Split('<');

            foreach (var item in splitter.Where(item => item.ToLower().Contains("id=\"fullsize\"")))
            {
                var newItems = item.Split(' ');
                {
                    var srcItem = newItems.SingleOrDefault(x => x.ToLower().StartsWith("src"));
                    if (!string.IsNullOrEmpty(srcItem))
                    {
                        srcItem = srcItem.Replace("src=", "");
                        srcItem = srcItem.Replace("\"", "");
                        var newUrl = srcItem;
                        return newUrl;
                    }
                }
            }

            return string.Empty;

        }
    }
}
