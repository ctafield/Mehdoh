using System.Linq;

namespace FieldOfTweets.Common.ImageHostParser
{
    public class ImgurParser : IImageHostParser
    {
        public string GetImageUrl(string contents)
        {
            var splitter = contents.Split('<');

            foreach (var item in splitter.Where(item => item.ToLower().Contains("rel=\"image_src\"")))
            {
                var newItems = item.Split(' ');
                {
                    var srcItem = newItems.SingleOrDefault(x => x.ToLower().Contains("href"));
                    if (!string.IsNullOrEmpty(srcItem))
                    {
                        srcItem = srcItem.Replace("href=", "");
                        srcItem = srcItem.Replace("\"", "");
                        var newUrl = srcItem;
                        return newUrl;
                    }
                }
            }

            foreach (var item in splitter.Where(item => item.ToLower().Contains("rel=image_src")))
            {
                var newItems = item.Split(' ');
                {
                    var srcItem = newItems.SingleOrDefault(x => x.ToLower().Contains("href"));
                    if (!string.IsNullOrEmpty(srcItem))
                    {
                        srcItem = srcItem.Replace("href=", "");
                        var newUrl = srcItem;
                        return newUrl;
                    }
                }
                
            }

            return string.Empty;
        }

    }
}
