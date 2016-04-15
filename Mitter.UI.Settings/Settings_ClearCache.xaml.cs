using System;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ImageCaching;

namespace Mitter.UI.Settings
{

    public partial class Settings_ClearCache : AnimatedBasePage
    {

        private int RetryCount { get; set; }

        public Settings_ClearCache()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            RetryCount = 0;

            ThreadPool.QueueUserWorkItem(GetImageSpace);
            ThreadPool.QueueUserWorkItem(GetDatabaseSize);
        }

        private void GetDatabaseSize(object state)
        {

            UiHelper.SafeDispatchSync(() =>
            {
                btnClear.IsEnabled = false;
            });

            try
            {
                var dbAdmin = new DatabaseAdministration();
                double size = (double)dbAdmin.GetDatabaseSize() / 1024 / 1024;

#if WP8
            double x = Math.Truncate((double) size*100)/100;
#else
                double x = Math.Floor((double)size * 100) / 100;
#endif

                var s = string.Format("{0:N2}mb", x);

                UiHelper.SafeDispatch(() =>
                {
                    txtDatabaseSize.Text = s;
                    btnClear.IsEnabled = true;
                });

            }
            catch (Exception)
            {
                if (++RetryCount < 50)
                    ThreadPool.QueueUserWorkItem(GetDatabaseSize);
            }

        }

        private void GetImageSpace(object state)
        {
            UiHelper.SafeDispatchSync(() =>
            {
                btnClearImages.IsEnabled = false;
            });

            double size = (double)ImageCacheHelper.GetSizeOfCachedImages(string.Empty) / 1024 / 1024;

#if WP8
            double x = Math.Truncate((double) size*100)/100;
#else
            double x = Math.Floor((double)size * 100) / 100;
#endif


            var s = string.Format("{0:N2}mb", x);

            UiHelper.SafeDispatch(() =>
                                      {
                                          txtImageCache.Text = s;
                                          btnClearImages.IsEnabled = true;
                                      });
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {

            UiHelper.SafeDispatchSync(() =>
                                          {
                                              txtDatabaseSize.Text = "Clearing...";
                                              btnClear.IsEnabled = false;
                                          });

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {

                try
                {
                    var dbAdmin = new DatabaseAdministration();
                    dbAdmin.ClearDatabase();

                    UiHelper.SafeDispatchSync(() => txtDatabaseSize.Text = "Calculating...");

                    // now refresh
                    ThreadPool.QueueUserWorkItem(GetDatabaseSize);
                }
                catch (Exception)
                {
                    UiHelper.SafeDispatch(() => MessageBox.Show("Something else is accessing the database at the moment. Please try again in a short while."));
                }
            });

        }

        private void btnClearImages_Click(object sender, RoutedEventArgs e)
        {

            UiHelper.SafeDispatchSync(() =>
                                          {
                                              txtImageCache.Text = "Clearing...";
                                              btnClearImages.IsEnabled = false;
                                          });

            ThreadPool.QueueUserWorkItem(delegate(object state)
                                             {
                                                 ImageCacheHelper.ClearCache();
                                                 UiHelper.SafeDispatchSync(() => txtImageCache.Text = "Calculating...");
                                                 ThreadPool.QueueUserWorkItem(GetImageSpace);
                                             });

        }


    }

}