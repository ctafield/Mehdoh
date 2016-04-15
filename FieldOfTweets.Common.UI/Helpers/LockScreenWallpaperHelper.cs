using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Phone.System.UserProfile;
using FieldOfTweets.Common.UI.Classes;
using Lumia.Imaging;
using Lumia.Imaging.Artistic;
using Lumia.InteropServices.WindowsRuntime;

namespace FieldOfTweets.Common.UI.Helpers
{
    public class LockScreenWallpaperHelper
    {

#if WP8

        public async Task CheckAndUpdateLockScreenWallpaper()
        {
            try
            {
                var isProvider = Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
                if (!isProvider)
                {
                    return;
                }

                // At this stage, the app is the active lock screen background provider.

                // The following code example shows the new URI schema.
                // ms-appdata points to the root of the local app data folder.
                // ms-appx points to the Local app install folder, to reference resources bundled in the XAP package.
                var schema = "ms-appdata:///Local/";

                var filePathOfTheImage = await GenerateLockScreenWallpaper();

                var uri = new Uri(schema + filePathOfTheImage, UriKind.Absolute);

                // Set the lock screen background image.
                LockScreen.SetImageUri(uri);

                // Get the URI of the lock screen background image.
                var currentImage = Windows.Phone.System.UserProfile.LockScreen.GetImageUri();

            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private async Task<string> GenerateLockScreenWallpaper()
        {

            // 480 x 800 = 7 (73) x 11 (73) = 77 images

            int width;
            int height;
            int numImages = 77;

           int size = 120;

            switch (ResolutionHelper.CurrentResolution)
            {
                case Resolutions.HD720p:
                    height = 1280;
                    width = 720;
                    break;
                case Resolutions.WVGA:
                    height = 800;
                    width = 768;
                    break;
                case Resolutions.WXGA:
                    height = 1280;
                    width = 768;
                    break;
                case Resolutions.HD1080p:
                    height = 1920;
                    width = 1080;
                    size = 180;
                    break;
                default:
                    height = 800;
                    width = 768;
                    break;
            }
            
            string newFile;

            // Enumerate the cached images
            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                var randomUsers = new List<string>();

                var directories = myStore.GetDirectoryNames(ApplicationConstants.UserCacheStorageFolder + "\\*.*");

                // Create an image
                if (directories.Length < numImages)
                {
                    // some duplicates
                    var z = 0;

                    for (var x = 0; x < numImages; x++)
                    {
                        randomUsers.Add(directories[z]);
                        z++;
                        // wrap around
                        if (z > directories.Length - 1)
                            z = 0;
                    }
                }
                else
                {
                    var rand = new Random();

                    // random ones
                    while (randomUsers.Count < numImages)
                    {
                        var randPos = rand.Next(0, directories.Length);
                        var newDir = directories[randPos];
                        if (!randomUsers.Contains(newDir))
                        {
                            randomUsers.Add(newDir);
                        }
                    }
                }

                var can = new Canvas
                {
                    Width = width,
                    Height = height,
                    Background = new SolidColorBrush(Colors.Transparent)
                };

                int i = 0, j = 0;

                // Now lets render the images
                for (var x = 0; x < randomUsers.Count; x++)
                {
                    var image = new Image
                    {
                        Width = size,
                        Height = size,
                        Stretch = Stretch.UniformToFill
                    };

                    var rootFolder = ApplicationConstants.UserCacheStorageFolder + "\\" + randomUsers[x] + "\\";

                    var fileName = myStore.GetFileNames(rootFolder + "*.*").FirstOrDefault();

                    if (fileName != null)
                    {
                        using (var stream = myStore.OpenFile(rootFolder + fileName, FileMode.Open))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.SetSource(stream);
                            image.Source = bitmap;
                        }

                        Canvas.SetLeft(image, i * size);
                        Canvas.SetTop(image, j * size);

                        can.Children.Add(image);
                    }

                    i += 1;

                    if (i > 6)
                    {
                        i = 0;
                        j++;
                    }
                }

                var writeableBitmap = new WriteableBitmap(width, height);
                writeableBitmap.Render(can, null);
                writeableBitmap.Invalidate();

                IReadableBitmap bitty = writeableBitmap.AsBitmap();

                // Create a target where the filtered image will be rendered to
                var target = new WriteableBitmap(width, height);

                // Create a source to read the image from PhotoResult stream
                var source = new BitmapImageSource(bitty);

                // Create effect collection with the source stream
                using (var filters = new FilterEffect(source))
                {
                    // Initialize the filter
                    var sampleFilter = new LomoFilter(0.8, 0, LomoVignetting.High, LomoStyle.Neutral);

                    // Add the filter to the FilterEffect collection
                    filters.Filters = new IFilter[] { sampleFilter };

                    // Create a new renderer which outputs WriteableBitmaps
                    using (var renderer = new WriteableBitmapRenderer(filters, target))
                    {
                        // Render the image with the filter(s)
                        await renderer.RenderAsync();
                    }
                }


                // Save it to the shell folder
                try
                {
                    var currentImage = LockScreen.GetImageUri();

                    if (currentImage.ToString().EndsWith("_A.jpg"))
                    {
                        newFile = "LiveLockBackground_B.jpg";
                    }
                    else
                    {
                        newFile = "LiveLockBackground_A.jpg";
                    }

                }
                catch (Exception)
                {
                    newFile = "LiveLockBackground_A.jpg";
                }


                using (var stream = myStore.OpenFile(newFile, FileMode.Create))
                {
                    target.SaveJpeg(stream, width, height, 0, 100); // reduce memory usage
                }

            }

            return newFile;

        }

#endif
    }
}
