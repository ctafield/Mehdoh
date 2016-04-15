using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace FieldOfTweets.Common.UI.ImageCaching
{

    public class ImageCacher
    {

        public ImageCacheRequest Request { get; private set; }

        public void CacheImageToUserCache(ImageCacheRequest request)
        {

            Request = request;

            if (request == null || request.TargetUri == null)
            {
                if (GetCachedImageCompletedEvent != null)
                    GetCachedImageCompletedEvent(this, null);
                return;
            }

            var client = new WebClient();
            client.OpenReadCompleted += new OpenReadCompletedEventHandler(GetCachedImageCompleted);

            var remoteUri = new Uri(request.TargetUri, UriKind.Absolute);
            client.OpenReadAsync(remoteUri, request);
        }

        public event EventHandler GetCachedImageCompletedEvent;

        private void GetCachedImageCompleted(object sender, OpenReadCompletedEventArgs e)
        {

            if (e.Error != null)
            {
                return;
            }

            try
            {

                var originalUri = e.UserState as ImageCacheRequest;
                string targetFile = originalUri.TargetFile;

                var resInfo = new StreamResourceInfo(e.Result, null);

                byte[] contents;

                using (var reader = new StreamReader(resInfo.Stream))
                {
                    using (var bReader = new BinaryReader(reader.BaseStream))
                    {
                        contents = bReader.ReadBytes((int)reader.BaseStream.Length);
                    }
                }

                if (originalUri.ResizeImage)
                {

                    var largeImage = System.IO.Path.ChangeExtension(targetFile, ".large" + System.IO.Path.GetExtension(targetFile));

                    SaveImageFromJpegOrPng(contents, largeImage);

                    EventWaitHandle wait = new AutoResetEvent(false);

                    Deployment.Current.Dispatcher.BeginInvoke(delegate()
                                                                  {
                                                                      try
                                                                      {
                                                                          var wb = new WriteableBitmap(130, 130);

                                                                          using (var reader = new MemoryStream(contents))
                                                                          {
                                                                              wb.SetSource(reader);
                                                                          }

                                                                          using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                                                                          {
                                                                              using (var myStream = new IsolatedStorageFileStream(targetFile, FileMode.Create, myStore))
                                                                              {
                                                                                  wb.SaveJpeg(myStream, 130, 130, 0, 90);
                                                                              }
                                                                          }

                                                                      }
                                                                      catch (Exception ex)
                                                                      {
                                                                          ErrorLogging.ErrorLogger.LogException(ex);
                                                                      }
                                                                      finally
                                                                      {
                                                                          wait.Set();
                                                                      }

                                                                  });

                    wait.WaitOne();

                }
                else
                {
                    // all else fails
                    SaveImageFromJpegOrPng(contents, targetFile);
                }

            }
            catch (Exception)
            {
            }
            finally
            {
                if (GetCachedImageCompletedEvent != null)
                    GetCachedImageCompletedEvent(this, null);
            }

        }

        private void SaveImageFromJpegOrPng(byte[] contents, string targetFile)
        {

            try
            {
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var myStream = new IsolatedStorageFileStream(targetFile, FileMode.Create, myStore))
                    {
                        myStream.Write(contents, 0, contents.Length);
                    }
                }
            }
            catch
            {
            }

        }

    }

}
