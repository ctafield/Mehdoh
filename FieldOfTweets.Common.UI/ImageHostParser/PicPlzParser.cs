using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace FieldOfTweets.Common.ImageHostParser
{
    public class PicPlzParser : IImageHostParser
    {

        public string GetImageUrl(string contents)
        {
            var splitter = contents.Split('<');

            foreach (var item in splitter.Where(item => item.ToLower().Contains("id=\"mainimage\"")))
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
