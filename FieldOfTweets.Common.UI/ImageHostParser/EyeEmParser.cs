using System.Linq;
using FieldOfTweets.Common.ImageHostParser;

namespace FieldOfTweets.Common.UI.ImageHostParser
{

    public class EyeEmParser : ImageHostParserBase, IImageHostParser
    {

        public string GetImageUrl(string contents)
        {


            if (string.IsNullOrEmpty(contents))
                return string.Empty;

            try
            {


                var index = contents.IndexOf("div class=\"viewport-pic\"", System.StringComparison.Ordinal);
                var newContents = contents.Substring(index);

                var splitter = newContents.Split('<');

                var thisVal = splitter.FirstOrDefault(item => item.ToLower().Contains("img"));
                if (thisVal == null)
                    return null;

                return ExtractValue(thisVal, "src");

            }
            catch 
            {
                return string.Empty;
            }

        }

    }
}
