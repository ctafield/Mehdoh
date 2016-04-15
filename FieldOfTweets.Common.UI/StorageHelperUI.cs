using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Resources;
using Newtonsoft.Json;

namespace FieldOfTweets.Common.UI
{

    public class StorageHelperUI
    {

        public static long UserId { get; set; }

        private class RequestState
        {
            public bool ResetLiveTile { get; set; }
            public string FullImageUrl { get; set; }
            public string ProfileImageUrl { get; set; }
            public ApplicationConstants.AccountTypeEnum AccountType { get; set; }
        }

        public static event EventHandler SaveProfileImageCompletedEvent;




        public void SaveContentsToFile<T>(string filePath, T obj) where T : new()
        {
            var contents = SerialiseResponseObject(obj);
            if (!string.IsNullOrEmpty(contents))
                SaveContentsToFile(filePath, contents);
        }

        public void SaveContentsToFile(string filePath, string contents)
        {

            try
            {
                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var newFile = storage.OpenFile(filePath, FileMode.Create))
                    {
                        using (var writer = new StreamWriter(newFile))
                        {
                            writer.Write(contents);
                            writer.Flush();
                        }
                    }
                }

            }
            catch (Exception)
            {
                // ignore?!?
            }

        }

        public T LoadContentsFromFile<T>(string filePath) where T : new()
        {
            var contents = LoadContentsFromFile(filePath);
            if (string.IsNullOrEmpty(contents))
                return new T();
            return GetResponseObject<T>(contents);
        }

        public string LoadContentsFromFile(string filePath)
        {
            string contents = string.Empty;

            try
            {

                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    if (storage.FileExists(filePath))
                    {
                        using (var newFile = storage.OpenFile(filePath, FileMode.Open))
                        {
                            using (var reader = new StreamReader(newFile))
                            {
                                contents = reader.ReadToEnd();
                            }
                        }
                    }
                }

            }
            catch (Exception)
            {
                contents = string.Empty;
            }


            return contents;
        }

        public T GetResponseObject<T>(string cachedContent)
        {

            try
            {
                return JsonConvert.DeserializeObject<T>(cachedContent);
            }
            catch (Exception)
            {
                return default(T);
            }

        }

        public string SerialiseResponseObject<T>(T settings)
        {
            return JsonConvert.SerializeObject(settings, Formatting.None);
        }







        public static void SaveProfileImage(string fullImageUrl, string actualFileName, long userId, ApplicationConstants.AccountTypeEnum accountType, bool? resetLiveTile = null)
        {

            UserId = userId;
            var state = new RequestState();

            switch (accountType)
            {

                case ApplicationConstants.AccountTypeEnum.Twitter:

                    var res = actualFileName.Split('/');
                    var profileImageUrl = res[res.Length - 1];

                    state = new RequestState
                                {
                                    FullImageUrl = fullImageUrl,
                                    ProfileImageUrl = profileImageUrl,
                                    AccountType = accountType,
                                    ResetLiveTile = resetLiveTile ?? true
                                };

                    break;

                case ApplicationConstants.AccountTypeEnum.Facebook:

                    state = new RequestState
                                {
                                    FullImageUrl = fullImageUrl,
                                    ProfileImageUrl = actualFileName,
                                    AccountType = accountType,
                                    ResetLiveTile = resetLiveTile ?? false
                                };
                    break;

                case ApplicationConstants.AccountTypeEnum.Instagram:

                    state = new RequestState
                                {
                                    FullImageUrl = fullImageUrl,
                                    ProfileImageUrl = actualFileName,
                                    AccountType = accountType,
                                    ResetLiveTile = resetLiveTile ?? false
                                };
                    break;

                case ApplicationConstants.AccountTypeEnum.Soundcloud:

                    state = new RequestState
                                {
                                    FullImageUrl = fullImageUrl,
                                    ProfileImageUrl = actualFileName,
                                    AccountType = accountType,
                                    ResetLiveTile = resetLiveTile ?? true
                                };
                    break;

            }

            var client = new WebClient();            
            client.OpenReadCompleted += client_OpenReadCompleted;
            client.OpenReadAsync(new Uri(fullImageUrl), state);

        }

        private static void client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {

            var requestState = e.UserState as RequestState;

            if (e.Error != null)
            {
                // The remote server returned an error: NotFound.
                if (!String.IsNullOrEmpty(e.Error.Message))
                {
                    if (e.Error.Message.ToLower().Contains("the remote server returned an error: notfound"))
                    {
                        if (requestState != null && (requestState.FullImageUrl != null && requestState.FullImageUrl.EndsWith("&size=original")))
                        {
                            var fullImageUrl = requestState.FullImageUrl.Replace("&size=original", "");
                            requestState.FullImageUrl = fullImageUrl;
                            var client = new WebClient();
                            client.OpenReadCompleted += client_OpenReadCompleted;
                            client.OpenReadAsync(new Uri(fullImageUrl), requestState);
                        }
                    }
                }

                return;
            }

            try
            {

                var resInfo = new StreamResourceInfo(e.Result, null);
                if (requestState != null)
                {
                    var profileImageUrl = requestState.ProfileImageUrl;

                    byte[] contents;

                    using (var reader = new StreamReader(resInfo.Stream))
                    {
                        using (var bReader = new BinaryReader(reader.BaseStream))
                        {
                            contents = bReader.ReadBytes((int)reader.BaseStream.Length);
                        }
                    }

                    SaveSharedImage(contents, profileImageUrl);

                    // not done anymore in wp7/wp8
                    //if (requestState.ResetLiveTile)
                    //{
                    //    SafeDispatch(() =>
                    //    {
                    //        var sh = new ShellHelper();
                    //        sh.ResetLiveTile(UserId, false);
                    //    });
                    //}

                }

                if (SaveProfileImageCompletedEvent != null)
                    SaveProfileImageCompletedEvent(null, null);

            }
            catch (Exception)
            {

            }

        }

        private static void SafeDispatch(Action action)
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


        private static readonly object SaveSharedImageLock = new object();

        private static void ClearImagesFromPath(IsolatedStorageFile myStore, string path, string rootPath)
        {
            var res = myStore.GetFileNames(path);
            var existingFiles = res.Where(file => file.ToLower().EndsWith(".gif") || file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".png") || file.ToLower().EndsWith(".jpeg") || file.ToLower().EndsWith("."));
            foreach (var existingFile in existingFiles)
            {
                try
                {
                    myStore.DeleteFile(rootPath + "/" + existingFile);
                }
                catch (Exception)
                {
                }
            }
        }

        private static void SaveSharedImage(byte[] contents, string profileImageUrl)
        {

            lock (SaveSharedImageLock)
            {

                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    var fileName = Path.GetExtension(profileImageUrl);
                    if (fileName != null && !fileName.StartsWith("."))
                        fileName = "." + fileName;

                    if (fileName == ".")
                        fileName = ".jpg";

                    // Delete any old images
                    var path1 = ApplicationConstants.UserStorageFolder + "/" + UserId + ".*";
                    ClearImagesFromPath(myStore, path1, ApplicationConstants.UserStorageFolder);

                    fileName = String.Format("{0}{1}", UserId, fileName);

                    string targetFile = ApplicationConstants.UserStorageFolder + "/" + fileName;

                    using (var isf = myStore.OpenFile(targetFile, FileMode.Create))
                    {
                        var writer = new BinaryWriter(isf);
                        writer.Write(contents);
                    }

                    var path2 = ApplicationConstants.ShellContentFolder + "/" + UserId + ".*";
                    ClearImagesFromPath(myStore, path2, ApplicationConstants.ShellContentFolder);

                    var otherTargetFile = ApplicationConstants.ShellContentFolder + "/" + fileName;

                    using (var writer = new BinaryWriter(new IsolatedStorageFileStream(otherTargetFile, FileMode.Create, myStore)))
                    {
                        writer.Write(contents);
                    }
                }

            }

        }

        private static readonly object UpdateCacheImageLock = new object();

        public static Stream UpdateCachedImage(long userId, Stream newImage, string newImageName, ApplicationConstants.AccountTypeEnum accountType)
        {

            lock (UpdateCacheImageLock)
            {

                // Obtain the virtual store for the application.
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    var path = Path.Combine(ApplicationConstants.UserStorageFolder, (userId + ".*"));

                    var res = myStore.GetFileNames(path);
                    var fileName = res.Where(file => file.ToLower().EndsWith(".gif") || file.ToLower().EndsWith(".jpg") || file.ToLower().EndsWith(".jpeg") || file.ToLower().EndsWith(".png") || file.ToLower().EndsWith("."));

                    foreach (var file in fileName)
                    {
                        myStore.DeleteFile(file);
                    }

                    var ext = Path.GetExtension(newImageName);
                    if (ext != null && !ext.StartsWith("."))
                        ext = "." + ext;

                    var newFileName = userId + ext;

                    var newFilePath = Path.Combine(ApplicationConstants.UserStorageFolder, newFileName);

                    newImage.Seek(0, SeekOrigin.Begin);

                    byte[] contents;

                    using (var reader = new StreamReader(newImage))
                    {
                        using (var bReader = new BinaryReader(reader.BaseStream))
                        {
                            contents = bReader.ReadBytes((int)reader.BaseStream.Length);
                        }
                    }

                    SaveSharedImage(contents, newImageName);

                    // Save the new one
                    using (var writer = new BinaryWriter(new IsolatedStorageFileStream(newFilePath, FileMode.Create, myStore)))
                    {
                        writer.Write(contents);
                    }

                    return new IsolatedStorageFileStream(newFilePath, FileMode.Open, myStore);

                }

            }

        }

    }

}
