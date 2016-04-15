using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.UI.ImageCaching
{
    public class ThumbnailCacheHelper
    {

        public Uri GetCachedUri(Uri imageUri)
        {

            if (imageUri == null)
                return null;

            using (var dh = new MainDataContext())
            {
                var res = dh.ThumbnailCache.FirstOrDefault(x => x.LongUrl == imageUri.ToString());
                if (res != null)
                    return new Uri(res.LocalUri, UriKind.Relative);
            }

            return null;
        }

        public void CacheImage(string imageUri, Action action)
        {

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!myStore.DirectoryExists(ApplicationConstants.ThumbnailCacheStorageFolder))
                    myStore.CreateDirectory(ApplicationConstants.ThumbnailCacheStorageFolder);
            }

            var cacher = new ImageCacher();

            var targetFile = Path.Combine(ApplicationConstants.ThumbnailCacheStorageFolder, Guid.NewGuid().ToString() + ".jpg");

            cacher.GetCachedImageCompletedEvent += (sender, e) => ThreadPool.QueueUserWorkItem(x =>
                                                                                                   {

                                                                                                       try
                                                                                                       {
                                                                                                           var imageCacher = sender as ImageCacher;

                                                                                                           if (imageCacher == null)
                                                                                                           {
                                                                                                               return;
                                                                                                           }

                                                                                                           var req = imageCacher.Request;

                                                                                                           var originalUri = req.TargetUri;

                                                                                                           // insert into table
                                                                                                           using (var dh = new MainDataContext())
                                                                                                           {
                                                                                                               var t = new ThumbnailCacheTable
                                                                                                                       {
                                                                                                                           LocalUri = targetFile,
                                                                                                                           LongUrl = originalUri
                                                                                                                       };
                                                                                                               dh.ThumbnailCache.InsertOnSubmit(t);
                                                                                                               dh.SubmitChanges();
                                                                                                           }
                                                                                                       }
                                                                                                       catch (Exception)
                                                                                                       {
                                                                                                       }
                                                                                                       finally
                                                                                                       {
                                                                                                           if (action != null)
                                                                                                           {
                                                                                                               action();
                                                                                                           }
                                                                                                       }
                                                                                                   });

            var request = new ImageCacheRequest()
                          {
                              ResizeImage = true,
                              TargetUri = imageUri,
                              TargetFile = targetFile
                          };

            cacher.CacheImageToUserCache(request);

        }

    }
}
