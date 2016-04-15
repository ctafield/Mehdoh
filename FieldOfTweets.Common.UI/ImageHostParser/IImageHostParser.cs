using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FieldOfTweets.Common.ImageHostParser
{

    public interface IImageHostParser
    {

        string GetImageUrl(string contents);

    }
}
