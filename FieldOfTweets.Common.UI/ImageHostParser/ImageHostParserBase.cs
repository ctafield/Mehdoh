using System.Linq;

namespace FieldOfTweets.Common.UI.ImageHostParser
{
    public abstract class ImageHostParserBase
    {

        protected string ExtractValue(string input, string attributeName)
        {

            var newItems = input.Split(' ');
            var srcItem = newItems.SingleOrDefault(x => x.ToLower().StartsWith(attributeName));
            if (string.IsNullOrEmpty(srcItem))
                return string.Empty;

            var firstIndex = srcItem.IndexOf("\"", System.StringComparison.Ordinal);
            var secondIndex = srcItem.LastIndexOf("\"", System.StringComparison.Ordinal);

            var res= srcItem.Substring(firstIndex + 1, secondIndex - firstIndex - 1).Trim();
            return res;

        }

    }
}