using System;
using System.Text.RegularExpressions;
using FieldOfTweets.Common.ImageHostParser;

namespace FieldOfTweets.Common.UI.ImageHostParser
{

    public class VineParser : IImageHostParser
    {

        public Uri VineVideoUri { get; set; }

        public string GetImageUrl(string contents)
        {

            // e.g. 
            // <meta property="twitter:image" content="https://vines.s3.amazonaws.com/thumbs/775B3EFF-861F-4409-B081-BFA76AAA2106-342-0000001371C39371_1.0.5.mp4.jpg?versionId=TDsyW1Jb9bvav9j_95QdSrqFMBnbvNY.">
            // <meta property="twitter:player:stream" content="https://vines.s3.amazonaws.com/videos/775B3EFF-861F-4409-B081-BFA76AAA2106-342-0000001371C39371_1.0.5.mp4?versionId=Xe_eWtkbXvpOlD7bGRuBvhlKiIqdtQRi">

            var match = Regex.Match(contents, "meta property=\"twitter:image\" content=\"(?<ImageUrl>.*)\"");

            if (match.Success)
            {

                var videoMatch = Regex.Match(contents, "meta property=\"twitter:player:stream\" content=\"(?<VideoUrl>.*)\"");
                if (videoMatch.Success)
                {
                    VineVideoUri = new Uri(videoMatch.Groups["VideoUrl"].Value, UriKind.Absolute);
                }
                
                return match.Groups["ImageUrl"].Value;
            }

            return string.Empty;

        }

    }

}
