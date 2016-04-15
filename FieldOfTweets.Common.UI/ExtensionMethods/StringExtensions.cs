using System;
using System.Text.RegularExpressions;
using System.Web;

namespace FieldOfTweets.Common.UI.ExtensionMethods
{
    public static class StringExtensions
    {

        public static string GetYoutubeVideoIdFromUrl(this string targetUrl)
        {

            try
            {

                if (targetUrl.Contains("m.youtube.com") && targetUrl.Contains("desktop_uri"))
                {
                    targetUrl = "http://www.youtube.com" + targetUrl.Substring(targetUrl.IndexOf("desktop_uri", System.StringComparison.Ordinal) + 12);
                    targetUrl = HttpUtility.UrlDecode(targetUrl);
                }

                var firstRegex = new Regex(@"((you(tu.be\/()()(?<VideoId2>[^#\&\?]*)|tube.com(\/|\/\/|\/#\/)(v\/|u\/\w\/|embed\/|watch\?feature=(.*)&v=|watch\?v=|\&v=)(?<VideoId>[^#\&\?]*))))", RegexOptions.IgnoreCase);

                var results = firstRegex.Match(targetUrl);

                var newUrl = results.Groups["VideoId"].Value;

                if (string.IsNullOrEmpty(newUrl))
                {
                    newUrl = results.Groups["VideoId2"].Value;
                }

                return newUrl;

            }
            catch (Exception)
            {
                return string.Empty;
            }

        }

    }

}
