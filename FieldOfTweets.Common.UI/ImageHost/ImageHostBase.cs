using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using FieldOfTweets.Common.Api.Twitter;

namespace FieldOfTweets.Common.UI.ImageHost
{

    public abstract class ImageHostBase : IDisposable
    {

        public int ReserveSize
        {
            get { return GetPlaceHolder().Length; }
        }

        public long MaximumImages
        {
            get
            {
                return 1;
            }
        }

        public bool IsTwitter
        {
            get
            {
                return false;
            }
        }

        public abstract string GetPlaceHolder();

        private const string TempImageFile = "temp_twitter_image.jpg";

        public int RetryCount { get; protected set; }

        public bool HasError { get; protected set; }

        public string UploadedUrl { get; protected set; }

        protected TwitterAccess GetTwitterUser(long accountId)
        {
            using (var sh = new StorageHelper())
            {
                var user = sh.GetTwitterUser(accountId);
                return user;
            }
        }

        public void UpdateImage(WriteableBitmap imageBitmap, int width, int height)
        {

            Debug.WriteLine(string.Format("Entering UpdateImage with image of {0} x {1}", width, height));

            double fileSize = double.MaxValue;

            while (fileSize > 2145728)
            {

                // Save the image to a temporary file
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    if (myStore.FileExists(TempImageFile))
                        myStore.DeleteFile(TempImageFile);

                    using (var myStream = new IsolatedStorageFileStream(TempImageFile, FileMode.OpenOrCreate, myStore))
                    {
                        imageBitmap.SaveJpeg(myStream, width, height, 0, 95);
                        myStream.Flush(true);
                    }
                }

                using (var myStore2 = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var myStream2 = new IsolatedStorageFileStream(TempImageFile, FileMode.Open, FileAccess.Read, myStore2))
                    {
                        fileSize = myStream2.Length;

                        Debug.WriteLine(string.Format("New filesize is {0}, image resolution is {1} x {2}", fileSize, width, height));
                    }
                }

                // shrink em
                width = (int)(width * 0.95);
                height = (int)(height * 0.95);
            
            }

        }

        private static byte[] StreamToBytes(Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.Position = 0;
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public void StoreImage(Stream imageStream)
        {

            var contents = StreamToBytes(imageStream);

            imageStream.Position = 0;

            // Save the image to a temporary file
            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var myStream = new IsolatedStorageFileStream(TempImageFile, FileMode.OpenOrCreate, myStore))
                {
                    myStream.Write(contents, 0, contents.Length);
                }
            }

        }

        public bool CurrentlyHasImage()
        {
            try
            {
                return FileExists(TempImageFile);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Stream _imageStream;

        public Stream ImageStream
        {
            get
            {

                if (_imageStream != null && _imageStream.CanRead && _imageStream.Position == 0)
                    return _imageStream;

                if (_imageStream != null)
                {
                    _imageStream.Close();
                    _imageStream.Dispose();
                }

                var myStore = IsolatedStorageFile.GetUserStoreForApplication();
                if (!myStore.FileExists(TempImageFile))
                    return null;

                try
                {                    
                    var myStream = new IsolatedStorageFileStream(TempImageFile, FileMode.Open, myStore)
                                   {
                                       Position = 0
                                   };
                    _imageStream = myStream;

                    return _imageStream;
                }
                catch (Exception)
                {
                }

                return null;
            }
            set
            {
                _imageStream = value;
            }
        }

        public void ClearStream()
        {
            //ImageStream = null;
        }

        public void Dispose()
        {

            if (_imageStream != null)
            {
                _imageStream.Dispose();
                _imageStream = null;
            }

            if (!FileExists(TempImageFile))
                return;

            try
            {                
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {                    
                    myStore.DeleteFile(TempImageFile);
                    
                    var stillExists = myStore.FileExists(TempImageFile);
                    Console.WriteLine("Dispose still exists: " + stillExists);
                }
            }
            catch (Exception)
            {
            }
        }

        private bool FileExists(string fileName)
        {
            try
            {
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    return myStore.GetFileNames(fileName).Length > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}