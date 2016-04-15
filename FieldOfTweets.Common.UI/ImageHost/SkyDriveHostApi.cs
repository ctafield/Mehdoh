using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FieldOfTweets.Common.ImageHost;
using FieldOfTweets.Common.UI.ThirdPartyApi;
using Microsoft.Live;

namespace FieldOfTweets.Common.UI.ImageHost
{
    public class SkyDriveHostApi : ImageHostBase, IImageHost
    {

        // Application name:	Mehdoh
        // Client ID:	000000004C0B48B7
        // Client secret:	e0OJOFdtDwfHlk09VtaO2DHYHTKQ1whd

        private TaskCompletionSource<string> taskCompletion;

        private static readonly string[] scopes = new[] { 
                "wl.signin", 
                "wl.skydrive",
                "wl.skydrive_update", 
                "wl.offline_access"
                };

        private LiveAuthClient authClient;
        private LiveConnectClient liveClient;

        protected string MehdohUploadFolder
        {
            get { return "Mehdoh uploads"; }
        }

        private string Id { get; set; }
       
        public Task<string> UploadImage(long accountId, string filePath)
        {

            FilePath = filePath;

            taskCompletion = new TaskCompletionSource<string>();

            this.authClient = new LiveAuthClient("000000004C0B48B7");
            this.authClient.InitializeCompleted += authClient_InitializeCompleted;
            this.authClient.InitializeAsync(scopes);

            return taskCompletion.Task;

        }

        private string FilePath { get; set; }

        private void authClient_InitializeCompleted(object sender, LoginCompletedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                ContinueUpload(e.Session);
            }
            else
            {
                this.authClient.LoginCompleted += authClient_LoginCompleted;
                this.authClient.LoginAsync(scopes);
            }
        }

        private void ContinueUpload(LiveConnectSession session)
        {
            this.liveClient = new LiveConnectClient(session);
            UploadFile();
        }

        private void authClient_LoginCompleted(object sender, LoginCompletedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                ContinueUpload(e.Session);
            }
            else
            {
                taskCompletion.SetCanceled();
            }
        }

        private void UploadFile()
        {

            // get the folder            
            liveClient.GetCompleted += OnGetExistingFolder;

            liveClient.GetAsync("me/skydrive/files?filter=folders");

        }

        private void OnGetExistingFolder(object sender, LiveOperationCompletedEventArgs e)
        {

            liveClient.GetCompleted -= OnGetExistingFolder;

            try
            {

                var results = ((Dictionary<string, object>)e.Result)["data"] as List<object>;

                // exists?    
                string folderId = null;

                foreach (var folderResult in results)
                {
                    var folderInfo = folderResult as Dictionary<string, object>;

                    if ((string)folderInfo["name"] == MehdohUploadFolder)
                    {
                        folderId = (string)folderInfo["id"];
                        break;
                    }
                }

                if (folderId == null)
                {
                    // create the folder
                    // doesnt exist    
                    var folderData = new Dictionary<string, object>
                                     {
                                         {"name", MehdohUploadFolder}
                                     };

                    liveClient.PostCompleted += OnCreateFolderCompletedEvent;
                    liveClient.PostAsync("me/skydrive", folderData);
                }
                else
                {
                    UploadToFolder(folderId);
                }

            }
            catch (Exception)
            {
                // if all else fails, upload to the root
                UploadToFolder(null);
            }
        }

        private void OnCreateFolderCompletedEvent(object sender2, LiveOperationCompletedEventArgs e2)
        {
            liveClient.PostCompleted -= OnCreateFolderCompletedEvent;

            try
            {
                var folderId = e2.Result["id"] as string;
                UploadToFolder(folderId);
            }
            catch (Exception)
            {
                UploadToFolder(null);
            }
        }

        private void UploadToFolder(string folderId)
        {
            liveClient.UploadCompleted += liveClient_UploadCompleted;

            // the extra null is to work around a bug where the overwrite doesnt work
            liveClient.UploadAsync(folderId ?? "me/skydrive", FilePath.Substring(FilePath.LastIndexOf('\\') + 1), ImageStream, OverwriteOption.Overwrite, null);                
        }

        void liveClient_UploadCompleted(object sender, LiveOperationCompletedEventArgs e)
        {

            liveClient.UploadCompleted -= liveClient_UploadCompleted;

            try
            {
                var res = (string)e.Result["id"];
                if (!string.IsNullOrWhiteSpace(res))
                {
                    Id = res;

                    // convert this to a short one
                    liveClient.GetCompleted += new EventHandler<LiveOperationCompletedEventArgs>(liveClient_GetCompleted);
                    liveClient.GetAsync(Id + "/shared_read_link");

                }
            }
            catch (Exception ex)
            {
                HasError = true;
                taskCompletion.SetException(ex);
            }

        }

        void liveClient_GetCompleted(object sender, LiveOperationCompletedEventArgs e)
        {

            liveClient.GetCompleted -= liveClient_GetCompleted;

            try
            {
                var link = (string)e.Result["link"];
                ConvertToBitly(link);
            }
            catch (Exception ex)
            {
                HasError = true;
                taskCompletion.SetException(ex);
            }

        }

        private void ConvertToBitly(string link)
        {
            var api = new BitlyApi();
            api.GetShortUrlCompletedEvent += new EventHandler(api_GetShortUrlCompletedEvent);
            api.GetShortUrl(link);
        }

        void api_GetShortUrlCompletedEvent(object sender, EventArgs e)
        {

            try
            {
                var api = sender as BitlyApi;
                UploadedUrl = api.ShortUrl;
            }
            finally
            {
                taskCompletion.SetResult(UploadedUrl);
            }

        }

        public override string GetPlaceHolder()
        {
            return "http://onedrive.ms/xxxxxx";
        }
       
    }

}
