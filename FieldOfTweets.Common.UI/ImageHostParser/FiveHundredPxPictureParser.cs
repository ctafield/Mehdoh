using System.Linq;
using FieldOfTweets.Common.ImageHostParser;

namespace FieldOfTweets.Common.UI.ImageHostParser
{
    public class FiveHundredPxPictureParser : IImageHostParser
    {

        public string GetImageUrl(string contents)
        {

            var splitter = contents.Split('<');

            foreach (var item in splitter.Where(item => item.ToLower().Contains("id=\"mainphoto\"")))
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
