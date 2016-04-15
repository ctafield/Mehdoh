using System.Text.RegularExpressions;
using FieldOfTweets.Common.ImageHostParser;

namespace FieldOfTweets.Common.UI.ImageHostParser
{
    public class FlickrPhotoParser : IImageHostParser
    {

        public string GetImageUrl(string contents)
        {

            // <meta property="twitter:image" content="https://vines.s3.amazonaws.com/thumbs/775B3EFF-861F-4409-B081-BFA76AAA2106-342-0000001371C39371_1.0.5.mp4.jpg?versionId=TDsyW1Jb9bvav9j_95QdSrqFMBnbvNY.">
            // <meta property="twitter:player:stream" content="https://vines.s3.amazonaws.com/videos/775B3EFF-861F-4409-B081-BFA76AAA2106-342-0000001371C39371_1.0.5.mp4?versionId=Xe_eWtkbXvpOlD7bGRuBvhlKiIqdtQRi">

            var match = Regex.Match(contents, "meta property=\"og:image\" content=\"(?<ImageUrl>.*)\"");

            if (match.Success)
            {
                return match.Groups["ImageUrl"].Value;
            }

            return string.Empty;

        }


    }
}
