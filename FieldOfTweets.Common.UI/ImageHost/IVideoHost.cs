using System.IO;
using FieldOfTweets.Common.ImageHost;

namespace FieldOfTweets.Common.UI.ImageHost
{
    interface IVideoHost : IImageHost
    {

        void UploadVideo(long accountId, Stream imageStream, string filePath);

    }
}
