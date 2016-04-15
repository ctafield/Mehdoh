using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FieldOfTweets.Common.ImageHost
{

    public interface IImageHost : IDisposable
    {

        Task<string> UploadImage(long accountId, string filePath);

        void UpdateImage(WriteableBitmap imageBitmap, int width, int height);

        Stream ImageStream { get; }

        bool IsTwitter { get; }

        long MaximumImages { get;  }

        void StoreImage(Stream imageStream);
        
        string GetPlaceHolder();

        string UploadedUrl { get; }

        int ReserveSize { get; }

        void ClearStream();

        bool CurrentlyHasImage();
    }

}
