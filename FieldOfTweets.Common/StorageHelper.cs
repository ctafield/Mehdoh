using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.ErrorLogging;
using Newtonsoft.Json;

#if FACEBOOK
using FieldOfTweets.Common.Api.Facebook;
#endif

namespace FieldOfTweets.Common
{

    public class StorageHelper : IDisposable
    {

        public static void CreateUserFolders()
        {

            try
            {
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    if (!myStore.DirectoryExists(ApplicationConstants.UserStorageFolder))
                        myStore.CreateDirectory(ApplicationConstants.UserStorageFolder);

                    if (!myStore.DirectoryExists(ApplicationConstants.SoundcloudUserStorageFolder))
                        myStore.CreateDirectory(ApplicationConstants.SoundcloudUserStorageFolder);

                    if (!myStore.DirectoryExists(ApplicationConstants.InstagramUserStorageFolder))
                        myStore.CreateDirectory(ApplicationConstants.InstagramUserStorageFolder);

                    if (!myStore.DirectoryExists(ApplicationConstants.FacebookUserStorageFolder))
                        myStore.CreateDirectory(ApplicationConstants.FacebookUserStorageFolder);
                }
            }
            catch
            {

            }

        }

        public static string OriginalProfileImageUrl { get; set; }

        public static string SharedCachedImageString(long accountId)
        {

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                var path = Path.Combine(ApplicationConstants.UserStorageFolder, (accountId + ".*"));

                var res = myStore.GetFileNames(path);
                var fileName =
                    res.FirstOrDefault(
                        file =>
                        file.ToLower().EndsWith(".gif") || file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".jpeg") ||
                        file.ToLower().EndsWith(".png") || file.ToLower().EndsWith("."));

                return string.IsNullOrEmpty(fileName) ? null : fileName;
            }
        }

        public static string SharedCachedImageUri(long accountId)
        {

            // Obtain the virtual store for the application.
            var fileName = SharedCachedImageString(accountId);

            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return "isostore:/" + ApplicationConstants.ShellContentFolder + "/" + fileName;

        }

        private static readonly object CachedImageUriLock = new object();

        public static string CachedImageUri(string userId)
        {

            lock (CachedImageUriLock)
            {
                try
                {

                    // Obtain the virtual store for the application.
                    using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        var path = ApplicationConstants.UserStorageFolder + "/" + userId + ".*";

                        var res = myStore.GetFileNames(path);
                        var fileName =
                            res.FirstOrDefault(
                                file =>
                                file.ToLower().EndsWith(".gif") || file.ToLower().EndsWith(".jpg") ||
                                file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpeg") ||
                                file.ToLower().EndsWith("."));

                        if (string.IsNullOrEmpty(fileName))
                            return null;

                        return "/" + ApplicationConstants.UserStorageFolder + "/" + fileName;
                    }

                }
                catch (Exception)
                {
                    return null;
                }

            }

        }

        private static readonly object SaveUserLock = new object();

        public void SaveUser(TwitterAccess twitterUser)
        {

            lock (SaveUserLock)
            {
                // Obtain the virtual store for the application.
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    if (!myStore.DirectoryExists(ApplicationConstants.UserStorageFolder))
                        myStore.CreateDirectory(ApplicationConstants.UserStorageFolder);

                    string filePath = string.Format(@"{0}/{1}.user", ApplicationConstants.UserStorageFolder, twitterUser.UserId);

                    using (var isoFileStream = myStore.OpenFile(filePath, FileMode.OpenOrCreate))
                    {
                        var serializer = new DataContractSerializer(typeof(TwitterAccess));
                        serializer.WriteObject(isoFileStream, twitterUser);
                    }
                }
            }

        }

        
#if FACEBOOK
        private Dictionary<long, FacebookAccess> CachedFacebookUser { get; set; }

        public FacebookAccess GetFacebookUser(long accountId)
        {

            if (CachedFacebookUser == null)
                CachedFacebookUser = new Dictionary<long, FacebookAccess>();

            if (CachedFacebookUser.ContainsKey(accountId))
                return CachedFacebookUser[accountId];

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                if (!myStore.DirectoryExists(ApplicationConstants.FacebookUserStorageFolder))
                {
                    return null;
                }

                var fileNames = myStore.GetFileNames(ApplicationConstants.FacebookUserStorageFolder + @"\facebook.user");

                if (fileNames.Length == 0)
                    return null;

                var users = new List<FacebookAccess>();

                foreach (var fullPath in fileNames.Select(fileName => ApplicationConstants.FacebookUserStorageFolder + @"\" + fileName))
                {
                    using (var isoFileStream = myStore.OpenFile(fullPath, FileMode.Open))
                    {
                        var serializer = new DataContractSerializer(typeof(FacebookAccess));
                        var newUser = serializer.ReadObject(isoFileStream) as FacebookAccess;
                        if (newUser != null)
                            users.Add(newUser);
                    }
                }

                return users.FirstOrDefault();

            }

        }
#endif

#if FACEBOOK
        public void SaveUser(FacebookAccess facebookUser)
        {

            lock (SaveUserLock)
            {
                // Obtain the virtual store for the application.
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!myStore.DirectoryExists(ApplicationConstants.UserStorageFolder))
                        myStore.CreateDirectory(ApplicationConstants.UserStorageFolder);

                    if (!myStore.DirectoryExists(ApplicationConstants.FacebookUserStorageFolder))
                        myStore.CreateDirectory(ApplicationConstants.FacebookUserStorageFolder);

                    var filePath = string.Format(@"{0}/facebook.user", ApplicationConstants.FacebookUserStorageFolder);

                    using (var isoFileStream = myStore.OpenFile(filePath, FileMode.OpenOrCreate))
                    {
                        var serializer = new DataContractSerializer(typeof(FacebookAccess));
                        serializer.WriteObject(isoFileStream, facebookUser);
                    }
                }
            }

        }
#endif

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private static Dictionary<long, TwitterAccess> CachedTwitterUser { get; set; }

        public TwitterAccess GetTwitterUser(long accountId)
        {

            if (CachedTwitterUser == null)
                CachedTwitterUser = new Dictionary<long, TwitterAccess>();

            if (CachedTwitterUser.ContainsKey(accountId))
                return CachedTwitterUser[accountId];

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                if (!myStore.DirectoryExists(ApplicationConstants.UserStorageFolder))
                {
                    return null;
                }

                var fileNames = myStore.GetFileNames(ApplicationConstants.UserStorageFolder + @"\" + accountId + ".user");

                if (fileNames.Length == 0)
                    return null;

                var users = new List<TwitterAccess>();

                var allFiles = fileNames.Select(fileName => ApplicationConstants.UserStorageFolder + @"\" + fileName).ToList();

                try
                {
                    foreach (var fullPath in allFiles)
                    {
                        using (var isoFileStream = myStore.OpenFile(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var serializer = new DataContractSerializer(typeof(TwitterAccess));
                            var newUser = serializer.ReadObject(isoFileStream) as TwitterAccess;
                            if (newUser != null)
                            {
                                users.Add(newUser);
                                isoFileStream.Close();
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    return null;
                }

                var user = users.FirstOrDefault();

                try
                {
                    if (!CachedTwitterUser.ContainsKey(accountId))
                        CachedTwitterUser.Add(accountId, user);
                }
                catch (Exception ex)
                {
                    // ignore if we have a conflict here
                    ErrorLogger.LogException("GetTwitterUser", ex);
                }
                
                return user;
            }

        }

        public List<TwitterAccess> GetAuthorisedTwitterUsers()
        {

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                if (!myStore.DirectoryExists(ApplicationConstants.UserStorageFolder))
                {
                    return null;
                }

                try
                {

                    var fileNames = myStore.GetFileNames(ApplicationConstants.UserStorageFolder + @"\*.user");

                    if (fileNames.Length == 0)
                        return null;

                    var users = new List<TwitterAccess>();

                    foreach (var fullPath in fileNames.Select(fileName => ApplicationConstants.UserStorageFolder + @"\" + fileName))
                    {
                        using (var isoFileStream = myStore.OpenFile(fullPath, FileMode.Open))
                        {
                            var serializer = new DataContractSerializer(typeof(TwitterAccess));
                            var newUser = serializer.ReadObject(isoFileStream) as TwitterAccess;
                            if (newUser != null)
                                users.Add(newUser);
                        }
                    }

                    return users;
                }
                catch
                {

                    return null;
                }

            }

        }

        public static void RemoveUser(long accountId)
        {

            using (IsolatedStorageFile myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!myStore.DirectoryExists(ApplicationConstants.UserStorageFolder))
                {
                    return;
                }

                string pattern = string.Format("{0}/{1}.*", ApplicationConstants.UserStorageFolder, accountId);
                var res = myStore.GetFileNames(pattern);

                foreach (var o in res)
                {
                    string fileName = string.Format("{0}/{1}", ApplicationConstants.UserStorageFolder, o);
                    myStore.DeleteFile(fileName);
                }
            }

        }

        public static string GetProfileImageForUser(long accountId)
        {
            using (var dh = new MainDataContext())
            {
                var prof = dh.Profiles.FirstOrDefault(x => x.Id == accountId);
                if (prof == null)
                    return null;

                return prof.CachedImageUri;
            }
        }

        public void UpdateUserColumnsWithNewUser(long userId)
        {

            foreach (var column in ColumnHelper.ColumnConfig)
            {
                if (column.AccountId == -1)
                    column.AccountId = userId;
            }

            ColumnHelper.SaveConfig();

        }

        public static List<string> LoadFriendsCache()
        {

            try
            {

                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    const string filename = "friendsj.cache";

                    if (!myStore.FileExists(filename))
                        return null;

                    using (var isoFileStream = myStore.OpenFile(filename, FileMode.Open))
                    {
                        using (var sw = new StreamReader(isoFileStream))
                        {
                            var contents = sw.ReadToEnd();
                            return JsonConvert.DeserializeObject<List<string>>(contents);
                        }

                    }
                }

            }
            catch (Exception)
            {
                return null;
            }

        }

        public static void SaveFriendsCache(List<string> friends)
        {




            lock (((ICollection)friends).SyncRoot)
            {

                try
                {

                    int count = 0;

                    var newList = new List<string>();
                    foreach (var friend in friends.ToList())
                    {
                        if (!newList.Contains(friend.ToLower()))
                        {
                            newList.Add(friend);
                            count += 1;
                        }

                        if (count > 1000)
                            break;
                    }
                    
                    // Obtain the virtual store for the application.
                    using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        var filePath = "friendsj.cache";

                        if (myStore.FileExists(filePath))
                            myStore.DeleteFile(filePath);

                        using (var isoFileStream = myStore.OpenFile(filePath, FileMode.CreateNew))
                        {
                            var value = JsonConvert.SerializeObject(newList);
                            using (var sw = new StreamWriter(isoFileStream))
                            {
                                sw.Write(value);
                                sw.Flush();
                            }
                        }

                    }

                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException("SaveFriendsCache", ex);
                }

            }
        }
    }


}
