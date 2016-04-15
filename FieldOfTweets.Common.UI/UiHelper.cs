using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Coding4Fun.Toolkit.Controls;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.ErrorLogging;
using FieldOfTweets.Common.UI.Classes;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Devices;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Telerik.Windows.Controls;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace FieldOfTweets.Common.UI
{

    public enum ThemeEnum { Light, Dark }

    public static class UiHelper
    {


        private static IMehdohApp App
        {
            get { return((IMehdohApp) (Application.Current)); }
        }

        public static bool ImageHasBeenShared { get; set; }

        public static string SelectedUser
        {
            get;
            set;
        }

        public static void SafeDispatchSync(Action action)
        {
            if (Deployment.Current.Dispatcher.CheckAccess())
            { // do it now on this thread 
                action.Invoke();
            }
            else
            {
                EventWaitHandle wait = new AutoResetEvent(false);

                // do it on the UI thread 
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                                                              {
                                                                  action();
                                                                  wait.Set();
                                                              });
                wait.WaitOne();
            }
        }

        public static void SafeDispatch(Action action)
        {
            if (Deployment.Current.Dispatcher.CheckAccess())
            { // do it now on this thread 
                action.Invoke();
            }
            else
            {
                // do it on the UI thread 
                Deployment.Current.Dispatcher.BeginInvoke(action);
            }
        }

        public static void ShowProgressBar()
        {

            ShowProgressBar(string.Empty);

        }

        public static void ShowProgressBar(string text)
        {
            SafeDispatchSync(() =>
                             {
                                 try
                                 {
                                     if (SystemTray.ProgressIndicator == null)
                                     {
                                         SystemTray.ProgressIndicator = new ProgressIndicator()
                                             {
                                                 IsIndeterminate = true,
                                                 IsVisible = true,
                                                 Text = text
                                             }; 
                                     }
                                     else
                                     {
                                         SystemTray.ProgressIndicator.IsIndeterminate = true;
                                         SystemTray.ProgressIndicator.IsVisible = true;
                                         SystemTray.ProgressIndicator.Text = text;    
                                     }
                                 }
                                 catch (Exception ex)
                                 {
                                     ErrorLogger.LogException("ShowProgressBar", ex);
                                 }
                                 
                             });
        }

        public static void HideProgressBar()
        {
            SafeDispatchSync(() =>
                {
                    try
                    {

                        if (SystemTray.ProgressIndicator != null)
                        {
                            SystemTray.ProgressIndicator.IsIndeterminate = false;
                            SystemTray.ProgressIndicator.IsVisible = false;
                            SystemTray.ProgressIndicator.Text = string.Empty;
                        }
                        else
                        {
                            SystemTray.ProgressIndicator = new ProgressIndicator
                                {
                                    IsVisible = false,
                                    IsIndeterminate = false
                                };
                        }

                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException("HideProgressBar", ex);
                    }

                });
        }

        public static bool AttemptedValid { get; set; }

        public static bool ValidateUser()
        {

            AttemptedValid = true;

            // Load the user data
            using (var storage = new StorageHelper())
            {

                // check for twitter users
                var twitterUsers = storage.GetAuthorisedTwitterUsers();
                if (twitterUsers != null && twitterUsers.Any())
                {
                    return true;
                }

            }

            return false;

        }

        public static ThemeEnum GetCurrentTheme()
        {

            //var bgc = Application.Current.Resources["PhoneBackgroundColor"].ToString();
            //if (bgc == "#FF000000")
            //return ThemeEnum.Dark;

            //return ThemeEnum.Light;
            if ((Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"] == Visibility.Visible)
                return ThemeEnum.Dark;

            return ThemeEnum.Light;

        }

        public static string GetTwitterSearchCount(Pivot mainPivot, string searchQuery, long accountId)
        {
            var listItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == searchQuery && x.AccountId == accountId);
            if (listItem != null)
            {
                var listItemIndex = ColumnHelper.ColumnConfig.IndexOf(listItem);
                return GetColumnCount(mainPivot, listItemIndex);
            }
            return "0";
        }


        public static string GetListCount(Pivot mainPivot, string slugId, long accountId)
        {
            var listItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == slugId && x.AccountId == accountId);
            if (listItem != null)
            {
                var listItemIndex = ColumnHelper.ColumnConfig.IndexOf(listItem);
                return GetColumnCount(mainPivot, listItemIndex);
            }
            return "0";
        }

        public static FrameworkElement GetTimelineList(Pivot mainPivot, long accountId)
        {
            try
            {
                var timelineItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterTimeline && x.AccountId == accountId);
                if (timelineItem != null)
                {
                    var listItemIndex = ColumnHelper.ColumnConfig.IndexOf(timelineItem);
                    return ((PivotItem)mainPivot.Items[listItemIndex]).Content as FrameworkElement;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public static string GetFavouritesCount(Pivot mainPivot, long accountId)
        {
            var timelineItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterFavourites && x.AccountId == accountId);
            if (timelineItem != null)
            {
                var timelineIndex = ColumnHelper.ColumnConfig.IndexOf(timelineItem);
                return GetColumnCount(mainPivot, timelineIndex);
            }
            return "0";
        }

        public static string GetTimelineCount(Pivot mainPivot, long accountId)
        {
            var timelineItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterTimeline && x.AccountId == accountId);
            if (timelineItem != null)
            {
                var timelineIndex = ColumnHelper.ColumnConfig.IndexOf(timelineItem);
                return GetColumnCount(mainPivot, timelineIndex);
            }
            return "0";
        }

        public static string GetMessagesCount(Pivot mainPivot, long accountId)
        {
            var timelineItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterMessages && x.AccountId == accountId);
            if (timelineItem != null)
            {
                var timelineIndex = ColumnHelper.ColumnConfig.IndexOf(timelineItem);
                return GetColumnCount(mainPivot, timelineIndex);
            }
            return "0";
        }

        public static string GetMentionsCount(Pivot mainPivot, long accountId)
        {
            var timelineItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterMentions && x.AccountId == accountId);
            if (timelineItem != null)
            {
                var timelineIndex = ColumnHelper.ColumnConfig.IndexOf(timelineItem);
                return GetColumnCount(mainPivot, timelineIndex);
            }
            return "0";
        }

        public static string GetColumnCount(Pivot mainPivot, int selectedIndex)
        {
            try
            {

                var sh = new SettingsHelper();
                if (!sh.GetShowPivotHeaderCounts())
                    return "";

                var control = mainPivot.Items[selectedIndex] as PivotItem;
                if (control != null)
                {
                    var stackPanel = control.Header as StackPanel;
                    if (stackPanel != null)
                    {
                        var textStack = stackPanel.Children[0] as StackPanel;
                        if (textStack != null)
                        {
                            var textBlock = textStack.Children[0] as TextBlock;
                            if (textBlock != null)
                            {
                                return textBlock.Text;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return "0";
            }

            return "0";

        }

        public static void SetColumnCount(Pivot mainPivot, int selectedIndex, string newValue)
        {

            var sh = new SettingsHelper();
            if (!sh.GetShowPivotHeaderCounts())
                return;

            UiHelper.SafeDispatch(() =>
            {
                try
                {
                    var control = mainPivot.Items[selectedIndex] as PivotItem;
                    if (control != null)
                    {
                        var stackPanel = control.Header as StackPanel;
                        if (stackPanel != null)
                        {
                            var textStack = stackPanel.Children[0] as StackPanel;
                            if (textStack != null)
                            {
                                var textBlock = textStack.Children[0] as TextBlock;
                                if (textBlock != null)
                                {
                                    textBlock.Text = newValue;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            });
        }

        public static void SetTwitterSearchCount(Pivot mainPivot, string countValue, long accountId, string searchQuery)
        {
            var listItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == searchQuery && x.AccountId == accountId && x.ColumnType == ApplicationConstants.ColumnTypeTwitterSearch);
            if (listItem != null)
            {
                var listIndex = ColumnHelper.ColumnConfig.IndexOf(listItem);
                SetColumnCount(mainPivot, listIndex, countValue);
            }
        }

        public static void SetListCount(Pivot mainPivot, string slug, string countValue, long accountId)
        {
            var listItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == slug && x.AccountId == accountId && x.ColumnType == ApplicationConstants.ColumnTypeTwitterList);
            if (listItem != null)
            {
                var listIndex = ColumnHelper.ColumnConfig.IndexOf(listItem);
                SetColumnCount(mainPivot, listIndex, countValue);
            }
        }

        public static void SetFavouritesCount(Pivot mainPivot, string countValue, long accountId)
        {
            var favouritesItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterFavourites && x.AccountId == accountId);
            if (favouritesItem != null)
            {
                var favouritesIndex = ColumnHelper.ColumnConfig.IndexOf(favouritesItem);
                SetColumnCount(mainPivot, favouritesIndex, countValue);
            }
        }

        public static void SetRetweetedToMeCount(Pivot mainPivot, string countValue, long accountId)
        {
            var retweetedToMeItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterRetweetedToMe && x.AccountId == accountId);
            if (retweetedToMeItem != null)
            {
                var retweetsOfMeIndex = ColumnHelper.ColumnConfig.IndexOf(retweetedToMeItem);
                SetColumnCount(mainPivot, retweetsOfMeIndex, countValue);
            }
        }


        public static void SetRetweetedByMeCount(Pivot mainPivot, string countValue, long accountId)
        {
            var retweetedByMeItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterRetweetedByMe && x.AccountId == accountId);
            if (retweetedByMeItem != null)
            {
                var retweetsOfMeIndex = ColumnHelper.ColumnConfig.IndexOf(retweetedByMeItem);
                SetColumnCount(mainPivot, retweetsOfMeIndex, countValue);
            }
        }

        public static void SetRetweetsOfMeCount(Pivot mainPivot, string countValue, long accountId)
        {
            var retweetsOfMeItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterRetweetsOfMe && x.AccountId == accountId);
            if (retweetsOfMeItem != null)
            {
                var retweetsOfMeIndex = ColumnHelper.ColumnConfig.IndexOf(retweetsOfMeItem);
                SetColumnCount(mainPivot, retweetsOfMeIndex, countValue);
            }
        }

        public static void SetTimelineCount(Pivot mainPivot, string countValue, long accountId)
        {
            var timelineItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterTimeline && x.AccountId == accountId);
            if (timelineItem != null)
            {
                var timelineIndex = ColumnHelper.ColumnConfig.IndexOf(timelineItem);
                SetColumnCount(mainPivot, timelineIndex, countValue);
            }
        }

        public static void SetMentionsCount(Pivot mainPivot, string countValue, long accountId)
        {
            var mentionsItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterMentions && x.AccountId == accountId);
            if (mentionsItem != null)
            {
                var mentionIndex = ColumnHelper.ColumnConfig.IndexOf(mentionsItem);
                SetColumnCount(mainPivot, mentionIndex, countValue);
            }
        }

        public static void SetMessagesCount(Pivot mainPivot, string countValue, long accountId)
        {
            var messagesItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.Value == ApplicationConstants.ColumnTwitterMessages && x.AccountId == accountId);
            if (messagesItem != null)
            {
                var messageIndex = ColumnHelper.ColumnConfig.IndexOf(messagesItem);
                SetColumnCount(mainPivot, messageIndex, countValue);
            }
        }


        private static BitmapImage _retweetImage;


        public static BitmapImage GetRetweetImage()
        {
            return _retweetImage ??
                   (_retweetImage =
                    UiHelper.GetCurrentTheme() == ThemeEnum.Dark
                        ? new BitmapImage(new Uri("/Images/retweet-dark.png", UriKind.Relative))
                        : new BitmapImage(new Uri("/Images/retweet-light.png", UriKind.Relative)));
        }

        private static BitmapImage _addMentionPng;

        public static BitmapImage GetAddMentionPng()
        {
            if (_addMentionPng == null)
            {
                if (UiHelper.GetCurrentTheme() == ThemeEnum.Dark)
                {
                    _addMentionPng = new BitmapImage(new Uri("/Images/addmention-dark.png", UriKind.Relative));
                }
                else
                {
                    _addMentionPng = new BitmapImage(new Uri("/Images/addmention-light.png", UriKind.Relative));
                }
            }

            return _addMentionPng;
        }

        private static BitmapImage _soundcloudStreamPng;

        public static BitmapImage GetSoundcloudStreamPng()
        {
            if (_soundcloudStreamPng == null)
            {
                if (UiHelper.GetCurrentTheme() == ThemeEnum.Dark)
                {
                    _soundcloudStreamPng = new BitmapImage(new Uri("/Images/soundcloud-stream-dark.png", UriKind.Relative));
                }
                else
                {
                    _soundcloudStreamPng = new BitmapImage(new Uri("/Images/soundcloud-stream-light.png", UriKind.Relative));
                }
            }

            return _soundcloudStreamPng;
        }

        public static Uri GetUnFavouriteImage()
        {
            //return UiHelper.GetCurrentTheme() == ThemeEnum.Dark ? new Uri("/Images/dark/appbar.favs.del.rest.png", UriKind.Relative) : new Uri("/Images/light/appbar.favs.del.rest.png", UriKind.Relative);
            return new Uri("/Images/76x76/dark/appbar.star.minus.png", UriKind.Relative);
        }

        public static Uri GetFavouriteImage()
        {
            //return UiHelper.GetCurrentTheme() == ThemeEnum.Dark ? new Uri("/Images/dark/appbar.favs.addto.rest.png", UriKind.Relative) : new Uri("/Images/light/appbar.favs.addto.rest.png", UriKind.Relative);
            return new Uri("/Images/76x76/dark/appbar.star.add.png", UriKind.Relative);
        }

        /// <summary>
        /// Groups a passed Contacts ObservableCollection
        /// </summary>
        /// <returns>Grouped Observable Collection of Contacts suitable for the LongListSelector</returns>
        public static ObservableCollection<GroupedOc<FriendViewModel>> CreateGroupedOC()
        {

            //Initialise the Grouped OC to populate and return
            var groupedFriends = new ObservableCollection<GroupedOc<FriendViewModel>>();

            //Now enumerate throw the alphabet creating empty groups objects
            //This ensure that the whole alphabet exists even if we never populate them
            const string alpha = "#abcdefghijklmnopqrstuvwxyz";

            for (var i = 0; i < alpha.Length; i++)
            {
                var c = alpha[i];

                //Create GroupedOC for given letter
                var thisGoc = new GroupedOc<FriendViewModel>(c.ToString(), null);

                //Add this GroupedOC to the observable collection that is being returned
                // and the LongListSelector can be bound to.
                groupedFriends.Add(thisGoc);
            }

            return groupedFriends;
        }

        public static void ShowToast(string title, string message)
        {
            UiHelper.SafeDispatch(() =>
            {
                var newToast = new ToastPrompt()
                {
                    Message = message,
                    Title = title,
                    Foreground = new SolidColorBrush(Colors.White)
                };
                newToast.Show();
            });
        }

        public static void ShowToast(string message)
        {
#if MEHDOH_PRO
            ShowToast("mehdoh unity", message);
#elif MEHDOH_FREE
            ShowToast("little mehdoh", message);
#else
            ShowToast("mehdoh", message);
#endif
        }

        public static Stream PrepareImageForUpload(PhotoResult e, int maxSizeKb, bool enforceSmallSize)
        {

            var imageSize = e.ChosenPhoto.Length;

            // At this point, check the size?
            if (imageSize > maxSizeKb * 1024)
            {

                bool resizeImage = false;

                if (!enforceSmallSize)
                {
                    // Prompt if they want to continue or resize.
                    // var friendlySize = string.Format("{0}kb", imageSize / 1024);

                    // TODO Prompt

                    resizeImage = true;
                }
                else
                {
                    resizeImage = true;
                }

                if (resizeImage)
                {
                    // Perhaps prompt?

                    var isf = IsolatedStorageFile.GetUserStoreForApplication();
                    var tempFileName = Guid.NewGuid().ToString();

                    var pm = new BitmapImage
                    {
                        CreateOptions = BitmapCreateOptions.IgnoreImageCache
                    };

                    pm.SetSource(e.ChosenPhoto);

                    var newFileName = ResizeImage(pm, tempFileName, 1024, 1024);
                    var oldstream = isf.OpenFile(newFileName, FileMode.Open, FileAccess.Read);
                    return oldstream;
                }
            }

            // Already the right size, so return that.
            return e.ChosenPhoto;

        }
        private static string ResizeImage(BitmapImage imageToResize, string fileName, double newWidth, double newHeight)
        {

            var resizedImage = new WriteableBitmap(imageToResize); //imageToResize is BitmapImage

            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var isfs = new IsolatedStorageFileStream(fileName, FileMode.Create, isf))
                {

                    double pixHt = imageToResize.PixelHeight;
                    double pixWt = imageToResize.PixelWidth;

                    double maxHeight = newWidth;
                    double maxWidth = newHeight;
                    double scaleX = 1;
                    double scaleY = 1;

                    if (pixHt > maxHeight)
                        scaleY = maxHeight / pixHt;
                    if (pixWt > maxWidth)
                        scaleX = maxWidth / pixWt;


                    double scale = Math.Min(scaleY, scaleX);
                    int newWidth1 = Convert.ToInt32(pixWt * scale);
                    int newHeight1 = Convert.ToInt32(pixHt * scale);

                    resizedImage.SaveJpeg(isfs, newWidth1, newHeight1, 0, 70);
                }
            }

            return fileName;
        }

        //private static BitmapImage _maximizeButton;

        //public static ImageSource GetMaximizeButton()
        //{
        //    return _maximizeButton ??
        //           (_maximizeButton =
        //            UiHelper.GetCurrentTheme() == ThemeEnum.Dark
        //                ? new BitmapImage(new Uri("/Mitter;component/Images/up-dark.png", UriKind.Relative))
        //                : new BitmapImage(new Uri("/Mitter;component/Images/up-light.png", UriKind.Relative)));
        //}

        //private static BitmapImage _deleteButton;

        //public static BitmapImage GetDeleteImage()
        //{
        //    return _deleteButton ??
        //           (_deleteButton =
        //            UiHelper.GetCurrentTheme() == ThemeEnum.Dark
        //                ? new BitmapImage(new Uri("/Mitter;component/Images/delete-dark.png", UriKind.Relative))
        //                : new BitmapImage(new Uri("/Mitter;component/Images/delete-light.png", UriKind.Relative)));
        //}

        //private static BitmapImage _upArrowImage;

        //public static BitmapImage GetUpArrowImage()
        //{
        //    return _upArrowImage ??
        //           (_upArrowImage =
        //            UiHelper.GetCurrentTheme() == ThemeEnum.Dark
        //                ? new BitmapImage(new Uri("/Mitter;component/Images/up-dark.png", UriKind.Relative))
        //                : new BitmapImage(new Uri("/Mitter;component/Images/up-light.png", UriKind.Relative)));
        //}

        //private static BitmapImage _downArrowImage;

        //public static BitmapImage GetDownArrowImage()
        //{
        //    return _downArrowImage ??
        //           (_downArrowImage =
        //            UiHelper.GetCurrentTheme() == ThemeEnum.Dark
        //                ? new BitmapImage(new Uri("/Mitter;component/Images/down-dark.png", UriKind.Relative))
        //                : new BitmapImage(new Uri("/Mitter;component/Images/down-light.png", UriKind.Relative)));
        //}


        private static BitmapImage _tipImage;

        public static BitmapImage GetTipImage()
        {
            return _tipImage ??
                   (_tipImage =
                    UiHelper.GetCurrentTheme() == ThemeEnum.Dark
                        ? new BitmapImage(new Uri("/Images/settings/tip-small-dark.png", UriKind.Relative))
                        : new BitmapImage(new Uri("/Images/settings/tip-small-light.png", UriKind.Relative)));

        }

        private static BitmapImage _tickImage;

        public static BitmapImage GetTickImage()
        {
            return _tickImage ??
                   (_tickImage =
                    UiHelper.GetCurrentTheme() == ThemeEnum.Dark
                        ? new BitmapImage(new Uri("/Images/check-dark.png", UriKind.Relative))
                        : new BitmapImage(new Uri("/Images/check-light.png", UriKind.Relative)));
        }


        public static void EnablePullToRefresh(Pivot mainPivot)
        {


            UiHelper.SafeDispatch(() =>
            {

                foreach (var col in ColumnHelper.ColumnConfig.Where(x => x.ColumnType == ApplicationConstants.ColumnTypeTwitter &&
                                                                          (x.Value == ApplicationConstants.ColumnTwitterTimeline ||
                                                                           x.Value == ApplicationConstants.ColumnTwitterMentions ||
                                                                           x.Value == ApplicationConstants.ColumnTwitterMessages)))
                {
                    var columnIndex = ColumnHelper.ColumnConfig.IndexOf(col);
                    var control = mainPivot.Items[columnIndex] as PivotItem;
                    if (control == null)
                    {
                        return;
                    }
                    var thisList = control.Content as RadDataBoundListBox;
                    if (thisList == null)
                    {
                        return;
                    }
                    thisList.IsPullToRefreshEnabled = true;
                }
            });
        }

        public static void DisablePullToRefresh(Pivot mainPivot)
        {


            UiHelper.SafeDispatch(() =>
                                  {

                                      try
                                      {
                                          foreach (var col in ColumnHelper.ColumnConfig.Where(x => x.ColumnType == ApplicationConstants.ColumnTypeTwitter &&
                                                                                                   (x.Value == ApplicationConstants.ColumnTwitterTimeline || 
                                                                                                    x.Value == ApplicationConstants.ColumnTwitterMentions || 
                                                                                                    x.Value == ApplicationConstants.ColumnTwitterMessages)))
                                          {
                                              var columnIndex = ColumnHelper.ColumnConfig.IndexOf(col);
                                              var control = mainPivot.Items[columnIndex] as PivotItem;
                                              if (control == null)
                                              {
                                                  return;
                                              }

                                              var thisList = control.Content as RadDataBoundListBox;
                                              if (thisList == null)
                                              {
                                                  return;
                                              }
                                              thisList.IsPullToRefreshEnabled = false;
                                          }
                                      }
                                      catch (Exception e)
                                      {
                                          Console.WriteLine(e);
                                      }
                                  });
        }


        public static void HidePullToRefresh(Pivot mainPivot, int columnType, string columnName, long accountId)
        {

            UiHelper.SafeDispatch(() =>
            {
                try
                {
                    var columnItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.ColumnType == columnType && x.Value == columnName && x.AccountId == accountId);
                    if (columnItem == null)
                    {
                        return;
                    }
                    var columnIndex = ColumnHelper.ColumnConfig.IndexOf(columnItem);
                    var control = mainPivot.Items[columnIndex] as PivotItem;
                    if (control == null)
                    {
                        return;
                    }
                    var thisList = control.Content as RadDataBoundListBox;
                    if (thisList == null)
                    {
                        return;
                    }
                    thisList.StopPullToRefreshLoading(true, true);
                }
                catch
                {
                    // dont care, shouldn't be here yet??!
                }
            });

        }

        public static void ScrollIntoView(Pivot mainPivot, int columnType, string columnName, long accountId, object lastItem)
        {

            if (lastItem == null)
                return;

            UiHelper.SafeDispatch(() =>
            {

                try
                {
                var columnItem = ColumnHelper.ColumnConfig.FirstOrDefault(x => x.ColumnType == columnType && x.Value == columnName && x.AccountId == accountId);
                if (columnItem == null)
                {
                    return;
                }
                var columnIndex = ColumnHelper.ColumnConfig.IndexOf(columnItem);
                var control = mainPivot.Items[columnIndex] as PivotItem;
                if (control == null)
                {
                    return;
                }
                var thisList = control.Content as ListBox;
                if (thisList != null)
                {
                    thisList.UpdateLayout();
                    thisList.ScrollIntoView(lastItem);
                    return;
                }
                var dataBoundListBox = control.Content as RadDataBoundListBox;
                if (dataBoundListBox != null)
                {
                    dataBoundListBox.UpdateLayout();
                    dataBoundListBox.BringIntoView(lastItem);
                }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException("ScrollIntoView", ex);
                }

            });
        }

        public static long GetAccountId(NavigationContext navigationContext)
        {

            long accountId = 0;
            string temp;
            navigationContext.QueryString.TryGetValue("accountId", out temp);
            if (!string.IsNullOrEmpty(temp))
            {

                accountId = long.Parse(temp);
            }

            return accountId;

        }


        public static void VibrateNotification()
        {
            var vibrateController = VibrateController.Default;
            vibrateController.Start(TimeSpan.FromMilliseconds(800));
        }

        public static void SoundNotification()
        {

            if (MediaPlayer.IsMuted)
                return;

            const string path = @"sounds\tweet1.wav";

            using (var stream = TitleContainer.OpenStream(path))
            {
                if (stream != null)
                {
                    var effect = SoundEffect.FromStream(stream);
                    FrameworkDispatcher.Update();
                    effect.Play();
                }
            }
        }

        public static FlowDirection GetFlowDirection(string languageCode)
        {
            try
            {
                switch (languageCode)
                {
                    case "ar":
                        return FlowDirection.RightToLeft;
                    case "he":
                        return FlowDirection.RightToLeft;
                    case "iw":
                        return FlowDirection.RightToLeft;
                    default:
                        return FlowDirection.LeftToRight;
                }
            }
            catch (Exception)
            {
                return FlowDirection.LeftToRight;
            }
        }

        /// <summary>
        /// Returns an object to represent the pivot header.
        /// </summary>
        /// <param name="columnType">What type of column is it? Twitter/Soundcloud/Instagram etc...</param>
        /// <param name="title">The title, or caption, of the column.</param>
        /// <param name="accountId">The account this column belongs to.</param>
        /// <param name="lstheader_DoubleTap"></param>
        /// <param name="lstHeader_Hold"></param>
        /// <param name="showPivotHeaderCounts"></param>
        /// <param name="showPivotHeaderAvatars"></param>
        /// <returns></returns>
        public static UIElement GetColumnHeader(int columnType, string title, long accountId, EventHandler<GestureEventArgs> lstheader_DoubleTap, EventHandler<GestureEventArgs> lstheader_Tap, EventHandler<GestureEventArgs> lstHeader_Hold, bool showPivotHeaderCounts, bool showPivotHeaderAvatars, int tag)
        {

            const bool debug = false;
           
            var outerStackPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(-9, 0, 0, 0),
                Tag = tag
            };

            if (debug)
            {
                outerStackPanel.Background = new SolidColorBrush(Colors.Green);
            }

            if (accountId > 0)
            {
                var profileImage = new Image
                {
                    Width = 40,
                    Height = 40,
                    Margin = new Thickness(0, 10, 10, 0),
                };

                var profileImageUrl = StorageHelper.GetProfileImageForUser(accountId);

                if (!string.IsNullOrWhiteSpace(profileImageUrl))
                {
                    try
                    {
                        var image = new BitmapImage();

                        using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            using (var stream = myStore.OpenFile(profileImageUrl, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                image.SetSource(stream);
                            }
                        }

                        profileImage.Source = image;
                    }
                    catch (Exception)
                    {
                    }
                }

                var countOuterStackPanel = new StackPanel
                {
                    Margin = new Thickness(0, 10, 10, 0),
                    Height = 40,
                    Background = Application.Current.Resources["PhoneAccentBrush"] as Brush,
                    Orientation = System.Windows.Controls.Orientation.Horizontal
                };

                if (debug)
                {
                    countOuterStackPanel.Background = new SolidColorBrush(Colors.Red);
                }

                string newTitle = "0";

                // This is the tweet count
                var countTextBlock = new TextBlock
                {
                    Text = newTitle,
                    FontWeight = FontWeights.Bold,
                    FontSize = 22,
                    MinWidth = 40,
                    Height = 40,
                    Margin = new Thickness(3),
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.White)
                };

                if (showPivotHeaderCounts)
                {
                    countOuterStackPanel.Children.Add(countTextBlock);
                    outerStackPanel.Children.Add(countOuterStackPanel);
                }

                if (showPivotHeaderAvatars)
                    outerStackPanel.Children.Add(profileImage);
            }

            // This is the caption (timeline/mentions etc...)
            var textBlock = new TextBlock
            {
                Text = title,
                FontSize = 48,
                Margin = new Thickness(0)
            };

            outerStackPanel.Children.Add(textBlock);

            if (lstheader_DoubleTap != null)
                outerStackPanel.DoubleTap += lstheader_DoubleTap;

            if (lstHeader_Hold != null)
            {
                var listener = GestureService.GetGestureListener(outerStackPanel);
                listener.Flick += delegate(object sender, FlickGestureEventArgs e)
                {
                    if (e.Direction == System.Windows.Controls.Orientation.Vertical && (int) e.Angle >= 80 &&
                        (int) e.Angle <= 100)
                        lstHeader_Hold(null, null);
                };

                outerStackPanel.Hold += lstHeader_Hold;
            }

            if (lstheader_Tap != null)
            {
                outerStackPanel.Tap += lstheader_Tap;
            }

            return outerStackPanel;
        }


        public static void RemoveBackStackUntil(string pageToStopAt, NavigationService navigationService)
        {
            while (navigationService.BackStack.FirstOrDefault() != null &&
                   !navigationService.BackStack.First().Source.ToString().ToLowerInvariant().Contains(pageToStopAt.ToLowerInvariant()))
            {
                navigationService.RemoveBackEntry();
            }
        }

        public static void EnableDisableAppBar(IApplicationBar applicationBar, bool isEnabled)
        {
            if (applicationBar == null)
                return;

            SafeDispatchSync(() =>
            {
                foreach (var button in applicationBar.Buttons)
                {
                    var appButton = button as ApplicationBarIconButton;
                    if (appButton != null)
                        appButton.IsEnabled = isEnabled;
                }
            });
        }
    }


}
