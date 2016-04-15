using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.ErrorLogging;
using Microsoft.Phone.Shell;
using NotificationsExtensions.BadgeContent;
using NotificationsExtensions.TileContent;
#if WP8
using Windows.Phone.System.UserProfile;
#endif

namespace FieldOfTweets.Common
{

    public class ShellHelper
    {

        public enum TileBackgroundColourEnum
        {
            System = 0,
            Mehdoh = 1
        }

        public enum FrontTileStyleEnum
        {
            MehdohLogo,
            TwitterAvatarStyle,
            PeopleHubStyle,
            Hybrid,
            MehdohGreen
        }

        public enum LiveTileStyleEnum
        {
            TwitterAvatarStyle,
            PeopleHubStyle,
            Hybrid,
            MehdohLogo,
            NoStyle,
            MehdohGreen
        }

        public void ResetLiveTile()
        {
            ResetLiveTile(0, false);
        }

        public void ResetLiveTile(long accountId, bool isForDemo)
        {

#if WP8

            // check the folders exist
            StorageHelper.CreateUserFolders();

            ToastNotificationManager.History.Clear();

            UpdateTiles(null, null);
            
            // this sets the number
            var badgeContent = new BadgeNumericNotificationContent(0);
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeContent.CreateNotification());

#endif

#if WP7

                        // only one tile for this app atm.
            var appTile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == "/");

            if (appTile != null)
            {

                var sh = new SettingsHelper();
                var liveTileSettings = sh.GetSettingsLiveTileSettings();
                
                var liveTypeStyle = liveTileSettings.LiveTileStyle;
                var frontTypeStyle = liveTileSettings.FrontTileStyle;

                string userProfileImage = StorageHelper.SharedCachedImageUri(accountId);

                if (string.IsNullOrEmpty(userProfileImage))
                    return;

                StandardTileData newTile;

                if (liveTypeStyle != LiveTileStyleEnum.NoStyle)
                {
                    var backTitle = string.Empty;

                    newTile = new StandardTileData
                                  {
                                      Title = ApplicationConstants.ApplicationName,
                                      BackgroundImage = GetTileImage(accountId, userProfileImage, frontTypeStyle),
                                      BackTitle = backTitle,
                                      BackBackgroundImage = GetTileImage(accountId, userProfileImage, liveTypeStyle),
                                      BackContent = string.Empty
                                  };
                }
                else
                {
                    newTile = new StandardTileData
                    {
                        Title = ApplicationConstants.ApplicationName,
                        BackgroundImage = GetTileImage(accountId, userProfileImage, frontTypeStyle),
                        BackBackgroundImage = new Uri("", UriKind.Relative),
                        BackContent = string.Empty,
                        BackTitle = string.Empty
                    };
                }

                appTile.Update(newTile);

            }

#endif

            var dsh = new DataStorageHelper();
            dsh.ResetShellStatus();

        }

        private void UpdateTiles(string[] wideContents, string userProfileUrl)
        {

            var smallTileXml = GetSmallTile();
            var tileXml = GetNormalTile();
            var wideTileXml = GetWideTile(wideContents, userProfileUrl);

            // Add the wide tile to the square tile's payload, so they are sibling elements under visual 
            IXmlNode node = tileXml.ImportNode(wideTileXml.GetElementsByTagName("binding").Item(0), true);
            tileXml.GetElementsByTagName("visual").Item(0).AppendChild(node);

            IXmlNode smallNode = tileXml.ImportNode(smallTileXml.GetElementsByTagName("binding").Item(0), true);
            tileXml.GetElementsByTagName("visual").Item(0).AppendChild(smallNode);

            var tile = new TileNotification(tileXml);

            TileUpdateManager.CreateTileUpdaterForApplication().Update(tile);

        }

        private XmlDocument GetSmallTile()
        {
            const TileTemplateType template = TileTemplateType.TileSquare71x71IconWithBadge;

            var tileXml = TileUpdateManager.GetTemplateContent(template);

            var imageElements = tileXml.GetElementsByTagName("image");
            for (int i = 0; i < imageElements.Length; i++)
            {
                var imageElement = (XmlElement)imageElements.Item((uint)i);
                const string imageSource = "ms-appx:///Assets/BlankTile.png";
                imageElement.SetAttribute("src", imageSource);
            }

            return tileXml;
        }

        private XmlDocument GetWideTile(string[] wideContents, string userProfileUrl)
        {

            if (wideContents != null)
            {
                const TileTemplateType template = TileTemplateType.TileWide310x150IconWithBadgeAndText;

                var tileXml = TileUpdateManager.GetTemplateContent(template);

                var textElements = tileXml.GetElementsByTagName("text");
                for (int i = 0; i < Math.Min(textElements.Length, wideContents.Length); i++)
                {
                    textElements.Item((uint)i).AppendChild(tileXml.CreateTextNode(wideContents[i]));
                }

                if (!string.IsNullOrEmpty(userProfileUrl))
                {
                    var imageElements = tileXml.GetElementsByTagName("image");
                    for (int i = 0; i < imageElements.Length; i++)
                    {
                        var imageElement = (XmlElement)imageElements.Item((uint)i);
                        imageElement.SetAttribute("src", "ms-appx:///Assets/SquareTile71x71.png");                        
                    }
                }

                XmlElement bindingElement = (XmlElement)tileXml.GetElementsByTagName("binding").Item(0);
                bindingElement.SetAttribute("branding", "Logo");


                return tileXml;

            }
            else
            {

                const TileTemplateType template = TileTemplateType.TileWide310x150ImageCollection;

                var tileXml = TileUpdateManager.GetTemplateContent(template);

                var fileNames = GetPeopleFileNames();

                var imageElements = tileXml.GetElementsByTagName("image");
                for (int i = 0; i < imageElements.Length; i++)
                {
                    var imageElement = (XmlElement)imageElements.Item((uint)i);
                    imageElement.SetAttribute("src", "ms-appdata:///local/" + fileNames[i]);
                }

                return tileXml;
            }


        }

        private List<string> GetPeopleFileNames()
        {

            var fileNames = new List<string>();

            const int numFiles = 5;

            try
            {

                // Enumerate the cached images
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    var directories = myStore.GetDirectoryNames(ApplicationConstants.UserCacheStorageFolder + "\\*.*");

                    if (directories.Length < numFiles)
                    {
                        // some duplicates
                        var z = 0;

                        for (var x = 0; x < numFiles; x++)
                        {
                            var newDir = directories[z];

                            var rootFolder = ApplicationConstants.UserCacheStorageFolder + @"/" + newDir + @"/";
                            var fileName = myStore.GetFileNames(rootFolder + "*.*").FirstOrDefault();
                            var fullFileName = rootFolder + fileName;

                            if (fileName != null && !fileNames.Contains(fullFileName))
                            {
                                fileNames.Add(fullFileName);
                            }

                            z++;
                            // wrap around
                            if (z > directories.Length)
                                z = 0;
                        }
                    }
                    else
                    {
                        var rand = new Random();

                        // random ones
                        while (fileNames.Count < numFiles)
                        {
                            var randPos = rand.Next(0, directories.Length);
                            var newDir = directories[randPos];

                            var rootFolder = ApplicationConstants.UserCacheStorageFolder + @"/" + newDir + @"/";
                            var fileName = myStore.GetFileNames(rootFolder + "*.*").FirstOrDefault();
                            var fullFileName = rootFolder + fileName;

                            if (fileName != null && !fileNames.Contains(fullFileName))
                            {
                                fileNames.Add(fullFileName);
                            }
                        }
                    }

                }

            }
            catch (Exception)
            {
                
            }

            return fileNames;

        }

        private XmlDocument GetNormalTile()
        {

            const TileTemplateType template = TileTemplateType.TileSquare150x150IconWithBadge;

            var tileXml = TileUpdateManager.GetTemplateContent(template);
            
            var imageElements = tileXml.GetElementsByTagName("image");
            for (int i = 0; i < imageElements.Length; i++)
            {
                var imageElement = (XmlElement)imageElements.Item((uint)i);
                const string imageSource = "ms-appx:///Assets/BlankTile.png";
                imageElement.SetAttribute("src", imageSource);
            }

            return tileXml;

        }

        private Color GetTileColour()
        {
            var settingsHelper = new SettingsHelper();
            var allSettings = settingsHelper.GetSettingsLiveTileSettings();


            if (allSettings.TileBackgroundColour == TileBackgroundColourEnum.Mehdoh)
                return Color.FromArgb(255, 0, 98, 25);

            return Color.FromArgb(0, 0, 0, 0); // 0 Alpha = transparent
        }

        public Uri GetMehdohLogoGreen()
        {

            var existingUri = new Uri("/Mitter;component/BlankTileGreen.png", UriKind.Relative);
            var newFile = ApplicationConstants.ShellContentFolder + "/fronttilegreen.png";

            using (var sri = Application.GetResourceStream(existingUri).Stream)
            {
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var file = myStore.OpenFile(newFile, FileMode.Create))
                    {
                        var byteBuffer = new byte[4096];
                        int bytesRead = -1;
                        while ((bytesRead = sri.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                        {
                            file.Write(byteBuffer, 0, bytesRead);
                        }
                    }
                }
            }

            return new Uri("isostore:/" + newFile, UriKind.RelativeOrAbsolute);

        }

        public Uri GetMehdohLogo()
        {

            var existingUri = new Uri("/Mitter;component/BlankTile.png", UriKind.Relative);
            var newFile = ApplicationConstants.ShellContentFolder + "/fronttile.png";

            using (var sri = Application.GetResourceStream(existingUri).Stream)
            {
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var file = myStore.OpenFile(newFile, FileMode.Create))
                    {
                        var byteBuffer = new byte[4096];
                        int bytesRead = -1;
                        while ((bytesRead = sri.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                        {
                            file.Write(byteBuffer, 0, bytesRead);
                        }
                    }
                }
            }

            return new Uri("isostore:/" + newFile, UriKind.RelativeOrAbsolute);

        }

        private Uri GetTileImage(long accountId, string userProfileImage, FrontTileStyleEnum liveTypeStyle)
        {

            switch (liveTypeStyle)
            {
                case FrontTileStyleEnum.TwitterAvatarStyle:
                    return GetAvatarStyle(accountId, userProfileImage);

                case FrontTileStyleEnum.MehdohLogo:
                    return GetMehdohLogo();

                case FrontTileStyleEnum.PeopleHubStyle:
                    try
                    {
                        return GetPeopleHubStyle();
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException("GetTileImage", ex);
                        return new Uri(userProfileImage, UriKind.Absolute);
                    }
                case FrontTileStyleEnum.Hybrid:
                    try
                    {
                        return GetHybridHubStyle(accountId);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException("GetTileImage", ex);
                        return new Uri(userProfileImage, UriKind.Absolute);
                    }
                case FrontTileStyleEnum.MehdohGreen:
                    try
                    {
                        return GetMehdohLogoGreen();
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException("GetTileImage", ex);
                        return new Uri(userProfileImage, UriKind.Absolute);
                    }

            }

            return new Uri(userProfileImage, UriKind.Absolute);

        }

        private Uri GetTileImage(long accountId, string userProfileImage, LiveTileStyleEnum liveTypeStyle)
        {

            switch (liveTypeStyle)
            {

                case LiveTileStyleEnum.NoStyle:
                    return GetNullStyle();

                case LiveTileStyleEnum.TwitterAvatarStyle:
                    return GetAvatarStyle(accountId, userProfileImage);

                case LiveTileStyleEnum.MehdohLogo:
                    return GetMehdohLogo();

                case LiveTileStyleEnum.PeopleHubStyle:
                    try
                    {
                        return GetPeopleHubStyle();
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException("GetTileImage", ex);
                        return new Uri(userProfileImage, UriKind.Absolute);
                    }
                case LiveTileStyleEnum.Hybrid:
                    try
                    {
                        return GetHybridHubStyle(accountId);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException("GetTileImage", ex);
                        return new Uri(userProfileImage, UriKind.Absolute);
                    }
                case LiveTileStyleEnum.MehdohGreen:
                    try
                    {
                        return GetMehdohLogoGreen();
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException("GetTileImage", ex);
                        return new Uri(userProfileImage, UriKind.Absolute);
                    }

            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return new Uri(userProfileImage, UriKind.Absolute);

        }

        public Uri GetAvatarStyle(long accountId, string userProfileImage)
        {
            if (string.IsNullOrWhiteSpace(userProfileImage))
                userProfileImage = StorageHelper.SharedCachedImageUri(accountId);

            return new Uri(userProfileImage, UriKind.Absolute);
        }

        public Uri GetNullStyle()
        {

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var can = new Canvas
                {
                    Width = 173,
                    Height = 173,
                    Background = (SolidColorBrush)Application.Current.Resources["PhoneAccentBrush"]
                };

                var writeableBitmap = new WriteableBitmap(173, 173);
                writeableBitmap.Render(can, null);
                writeableBitmap.Invalidate();

                // Save it to the shell folder
                var newFile = ApplicationConstants.ShellContentFolder + "/blanktile.png";

                using (var stream = myStore.OpenFile(newFile, FileMode.Create))
                {
                    writeableBitmap.SaveJpeg(stream, 173, 173, 0, 100);
                }

            }

            var uri = "isostore:/" + ApplicationConstants.ShellContentFolder + "/blanktile.png";

            return new Uri(uri, UriKind.Absolute);

        }

        public Uri GetHybridHubStyle(long accountId, bool isSample = false)
        {

            string newFile;

            // Enumerate the cached images
            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                var randomUsers = new List<string>();

                var directories = myStore.GetDirectoryNames(ApplicationConstants.UserCacheStorageFolder + "\\*.*");

                // TODO: Make sure the folders have users in them. Perhaps move the file generation up here.

                // Create an image
                if (directories.Length < 5)
                {
                    // some duplicates
                    var z = 0;

                    for (var x = 0; x < 9; x++)
                    {
                        randomUsers.Add(directories[z]);
                        z++;
                        // wrap around
                        if (z > directories.Length)
                            z = 0;
                    }
                }
                else
                {
                    var rand = new Random();

                    // random ones
                    while (randomUsers.Count < 5)
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
                    Width = 173,
                    Height = 173,
                    Background = new SolidColorBrush(Colors.Transparent)
                };

                const int smallImageSize = 173 / 3;

                var xPositions = new List<int>()
                                  {
                                      115,
                                      115,
                                      0,
                                      57,
                                      115
                                  };

                var yPositions = new List<int>()
                                  {
                                      0,
                                      57,
                                      115,
                                      115,
                                      115
                                  };

                // Now lets render the images
                for (var x = 0; x < 5; x++)
                {
                    var image = new Image
                    {
                        Width = smallImageSize,
                        Height = smallImageSize,
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

                        Canvas.SetLeft(image, xPositions[x]);
                        Canvas.SetTop(image, yPositions[x]);

                        can.Children.Add(image);
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                }

                var mainImage = new Image()
                                    {
                                        Width = 115,
                                        Height = 115,
                                        Stretch = Stretch.UniformToFill
                                    };

                // Add the main avatar
                using (var stream = myStore.OpenFile(ApplicationConstants.ShellContentFolder + "/" + StorageHelper.SharedCachedImageString(accountId), FileMode.Open))
                {
                    var bitmap = new BitmapImage();
                    bitmap.SetSource(stream);
                    mainImage.Source = bitmap;
                }

                Canvas.SetLeft(mainImage, 0);
                Canvas.SetTop(mainImage, 0);

                can.Children.Add(mainImage);

                GC.Collect();
                GC.WaitForPendingFinalizers();

                var writeableBitmap = new WriteableBitmap(173, 173);
                writeableBitmap.Render(can, null);
                writeableBitmap.Invalidate();

                // Save it to the shell folder

                newFile = ApplicationConstants.ShellContentFolder + "/" + ((isSample) ? "sample_" : "") + "hybridtile.png";

                using (var stream = myStore.OpenFile(newFile, FileMode.Create))
                {
                    writeableBitmap.SaveJpeg(stream, 173, 173, 0, 100);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

            }

            // Output it
            return new Uri("isostore:/" + newFile, UriKind.Absolute);

        }

        public Uri GetPeopleHubStyle(bool isSample = false)
        {

            string newFile;

            // Enumerate the cached images
            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                var randomUsers = new List<string>();

                var directories = myStore.GetDirectoryNames(ApplicationConstants.UserCacheStorageFolder + "\\*.*");

                // TODO: Make sure the folders have users in them. Perhaps move the file generation up here.

                // Create an image
                if (directories.Length < 9)
                {
                    // some duplicates
                    var z = 0;

                    for (var x = 0; x < 9; x++)
                    {
                        randomUsers.Add(directories[z]);
                        z++;
                        // wrap around
                        if (z > directories.Length)
                            z = 0;
                    }
                }
                else
                {
                    var rand = new Random();

                    // random ones
                    while (randomUsers.Count < 9)
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
                                  Width = 173,
                                  Height = 173,
                                  Background = new SolidColorBrush(Colors.Transparent)
                              };

                const int size = 173 / 3;

                int i = 0, j = 0;

                // Now lets render the images
                for (var x = 0; x < 9; x++)
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

                    if (i > 2)
                    {
                        i = 0;
                        j++;
                    }
                }

                var writeableBitmap = new WriteableBitmap(173, 173);
                writeableBitmap.Render(can, null);
                writeableBitmap.Invalidate();

                // Save it to the shell folder
                newFile = ApplicationConstants.ShellContentFolder + "/" + ((isSample) ? "sample_" : "") + "peopletile.png";

                using (var stream = myStore.OpenFile(newFile, FileMode.Create))
                {
                    writeableBitmap.SaveJpeg(stream, 173, 173, 0, 100);
                }

            }

            // Output it
            return new Uri("isostore:/" + newFile, UriKind.Absolute);

        }


#if WP8

        public void UpdateLiveTitle(long accountId, int noMentions, int noMessages, long mentionsSinceId, long messagesSinceId,
                string mentionUser, string mentionText, string messageUser, string messageText,
                string userProfileUrl, Action finishAction)
        {

            try
            {

                Debug.WriteLine("Updating live tile");

                string thisUser = string.Empty;

                using (var sh = new StorageHelper())
                {
                    var user = sh.GetTwitterUser(accountId);
                    if (user != null)
                    {
                        thisUser = user.ScreenName;
                        if (!thisUser.StartsWith("@"))
                            thisUser = "@" + thisUser;
                    }
                }

                var wideContents = new string[2];

                wideContents[0] = string.Empty;
                wideContents[1] = string.Empty;

                if (!string.IsNullOrEmpty(mentionUser))
                {
                    wideContents[0] = mentionUser;

                    if (mentionText.ToLowerInvariant().StartsWith(thisUser.ToLower()))
                        wideContents[1] = mentionText.Substring(thisUser.Length + 1);
                    else
                        wideContents[1] = mentionText;
                }
                else if (!string.IsNullOrEmpty(messageUser))
                {
                    wideContents[0] = messageUser;
                    wideContents[1] = messageText;
                }

                var totalCount = (uint)(noMentions + noMessages);
                
                UpdateTiles(wideContents, userProfileUrl);

                // this sets the number
                var badgeContent = new BadgeNumericNotificationContent(totalCount);
                BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeContent.CreateNotification());

                SetShellStatus(noMentions, noMessages, mentionsSinceId, messagesSinceId);

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("UpdateLiveTile", ex);
            }
            finally
            {
                if (finishAction != null)
                {
                    finishAction();
                }
            }

        }
#endif

#if WP7

        public void UpdateLiveTitle(long accountId, int noMentions, int noMessages, long mentionsSinceId, long messagesSinceId, Action finishAction)
        {

            try
            {

                string userProfileImage = StorageHelper.SharedCachedImageUri(accountId);

                if (string.IsNullOrEmpty(userProfileImage))
                    return;

                // only one tile for this app atm.
                var appTile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString() == "/");

                if (appTile != null)
                {

                    var content = string.Empty;

                    if (noMentions > 0)
                    {
                        content = noMentions + " new mention" + ((noMentions == 1) ? "" : "s");

                        if (noMessages > 0)
                        {
                            content = content + "\n";
                        }
                    }

                    if (noMessages > 0)
                    {
                        content = content + noMessages + " new message" + ((noMessages == 1) ? "" : "s");
                    }

                    var newTile = new StandardTileData
                    {
                        BackContent = content
                    };

                    appTile.Update(newTile);

                }

                SetShellStatus(noMentions, noMessages, mentionsSinceId, messagesSinceId);

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("UpdateLiveTile", ex);
            }
            finally
            {
                if (finishAction != null)
                {
                    finishAction();
                }
            }

        }

#endif


        private void SetShellStatus(int noMentions, int noMessages, long lastMentionId, long lastMessageId)
        {

            try
            {

                using (var dh = new MainDataContext())
                {
                    var existingTable = dh.ShellStatus.FirstOrDefault();
                    if (existingTable == null)
                    {
                        var newTable = new ShellStatusTable
                        {
                            MentionCount = noMentions,
                            MessageCount = noMessages,
                            LastMentionId = lastMentionId,
                            LastMessageId = lastMessageId
                        };
                        dh.ShellStatus.InsertOnSubmit(newTable);
                    }
                    else
                    {
                        existingTable.MentionCount = noMentions;
                        existingTable.MessageCount = noMessages;
                        existingTable.LastMentionId = lastMentionId;
                        existingTable.LastMessageId = lastMessageId;
                    }

                    dh.SubmitChanges();
                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("SetShellStatus", ex);
            }

        }

        public void ClearShellStatus()
        {

            using (var dh = new MainDataContext())
            {

                if (dh.ShellStatus.Any())
                {
                    dh.ShellStatus.DeleteAllOnSubmit(dh.ShellStatus);
                    dh.SubmitChanges();
                }

            }
        }


    }

}
