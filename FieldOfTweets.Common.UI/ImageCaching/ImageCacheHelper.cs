using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using System.IO;


namespace FieldOfTweets.Common.UI.ImageCaching
{
    public class ImageCacheHelper
    {

        private static Dictionary<string, bool> KnownCaches = new Dictionary<string, bool>();

        private static readonly object CacheImageLock = new object();

        private static readonly object IsProfileImageCachedLock = new object();

        private static string GetKeyForCache(string userId, string currentImage)
        {
            return userId + "|" + currentImage;
        }

        public static Uri GetUriForCachedImage(string userId, string currentImage)
        {

            var userFolder = Path.Combine(ApplicationConstants.UserCacheStorageFolder, userId);
            var targetFile = Path.Combine(userFolder, currentImage);

            return new Uri(targetFile, UriKind.Relative);
        }

        public static bool IsProfileImageCached(string userId, string currentImage)
        {

            lock (IsProfileImageCachedLock)
            {

                try
                {

                    var profileKey = GetKeyForCache(userId, currentImage);

                    if (KnownCaches.ContainsKey(profileKey) && KnownCaches[profileKey])
                        return true;

                    // Obtain the virtual store for the application.
                    using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {

                        // Create a new folder and call it "MyFolder".
                        if (!myStore.DirectoryExists(ApplicationConstants.UserCacheStorageFolder))
                            myStore.CreateDirectory(ApplicationConstants.UserCacheStorageFolder);

                        var userFolder = Path.Combine(ApplicationConstants.UserCacheStorageFolder, userId);

                        if (!myStore.DirectoryExists(userFolder))
                            myStore.CreateDirectory(userFolder);

                        var targetFile = Path.Combine(userFolder, currentImage);

                        var res = myStore.FileExists(targetFile);

                        KnownCaches[profileKey] = res;

                        return res;
                    }
                }
                catch (Exception)
                {
                    return false;
                }

            }

        }

        public static void CacheImage(string targetUri, string userId, string currentImage, Action completedEvent)
        {

            //lock (CacheImageLock)
            //{
            var userFolder = Path.Combine(ApplicationConstants.UserCacheStorageFolder, userId);
            var targetFile = Path.Combine(userFolder, currentImage);

            var cacher = new ImageCacher();
            cacher.GetCachedImageCompletedEvent += delegate
            {
                var key = GetKeyForCache(userId, currentImage);
                if (!KnownCaches.ContainsKey(key))
                    KnownCaches.Add(key, true);
                else if (!KnownCaches[key])
                    KnownCaches[key] = true;

            };

            cacher.GetCachedImageCompletedEvent += delegate(object sender, EventArgs e)
                                                       {
                                                           if (completedEvent != null)
                                                               completedEvent();
                                                       };

            cacher.CacheImageToUserCache(
                    new ImageCacheRequest
                    {
                        TargetUri = targetUri,
                        TargetFile = targetFile
                    }
                );

        }

        private static readonly object GetCachedImageLock = new object();

        public static BitmapImage GetCachedImage(string userId, string currentImage)
        {

            var userFolder = Path.Combine(ApplicationConstants.UserCacheStorageFolder, userId);
            var targetFile = Path.Combine(userFolder, currentImage);

            var newImage = new BitmapImage();

            lock (GetCachedImageLock)
            {
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = myStore.OpenFile(targetFile, FileMode.Open))
                    {
                        newImage.SetSource(stream);
                    }
                }
            }

            return newImage;

        }

        public static long GetSizeOfCachedImages(string folderName)
        {

            long totalSize = 0;

            if (string.IsNullOrEmpty(folderName))
                folderName = ApplicationConstants.UserCacheStorageFolder;


            // Obtain the virtual store for the application.
            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                if (folderName != ApplicationConstants.UserCacheStorageFolder)
                {
                    // parse the files
                    var theseFiles = myStore.GetFileNames(Path.Combine(folderName, "*.*"));

                    foreach (var file in theseFiles)
                    {
                        using (var fInfo = myStore.OpenFile(Path.Combine(folderName, file), FileMode.Open, FileAccess.Read))
                        {                            
                            totalSize += fInfo.Length;
                        }
                    }
                }

                // now parse the folders
                var folders = myStore.GetDirectoryNames(Path.Combine(folderName, "*.*"));

                foreach (var folder in folders)
                    totalSize += GetSizeOfCachedImages(Path.Combine(folderName, folder));

            }

            return totalSize;            
        }

        public static void ClearCache()
        {
            try
            {

                // Now clear the know cache
                KnownCaches = new Dictionary<string, bool>();

                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var folders = myStore.GetDirectoryNames(Path.Combine(ApplicationConstants.UserCacheStorageFolder, "*.*"));

                    foreach (var folder in folders)
                    {
                        var pathToDestroy = Path.Combine(ApplicationConstants.UserCacheStorageFolder, folder);

                        var files = myStore.GetFileNames(Path.Combine(pathToDestroy, "*.*"));

                        foreach (var file in files)
                        {
                            myStore.DeleteFile(Path.Combine(pathToDestroy, file));
                        }

                        // now zap the folder
                        myStore.DeleteDirectory(pathToDestroy);                        
                    }
                }
            }
            catch (Exception)
            {                           
            }
        }
    }

}
