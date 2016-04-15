using System;
using System.Linq;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.UI.ThirdPartyApi;

namespace FieldOfTweets.Common.UI
{

    public class SaveLaterSettings
    {
        public bool ReadItLaterEnabled { get; set; }
        public string ReadItLaterUsername { get; set; }
        public string ReadItLaterPassword { get; set; }

        public bool InstapaperEnabled { get; set; }
        public string InstapaperUsername { get; set; }
        public string InstapaperPassword { get; set; }
    }

    public class SaveLaterHelper
    {

        public event EventHandler SaveFinishedEvent;

        public int RefreshingCountRemaining { get; set; }

        public SaveLaterSettings GetSettings()
        {
            var settings = new SaveLaterSettings();

            using (var dh = new MainDataContext())
            {
                if (!dh.ReadLaterSettings.Any())
                {
                    var newTable = new ReadLaterTable()
                                       {
                                           UseInstapaper = false,
                                           UseReadItLater = false
                                       };
                    dh.ReadLaterSettings.InsertOnSubmit(newTable);
                    dh.SubmitChanges();

                    settings.InstapaperEnabled = false;
                    settings.ReadItLaterEnabled = false;
                }
                else
                {
                    var res = dh.ReadLaterSettings.First();
                    settings.InstapaperEnabled = res.UseInstapaper;
                    settings.ReadItLaterEnabled = res.UseReadItLater;
                    
                    settings.InstapaperUsername = res.InstapaperUsername;
                    settings.InstapaperPassword = res.InstapaperPassword;

                    settings.ReadItLaterUsername = res.ReadItLaterUsername;
                    settings.ReadItLaterPassword = res.ReadItLaterPassword;
                }
            }

            return settings;
        }

        public bool SaveUrl(string url, string description, long id)
        {
            var settings = GetSettings();

            RefreshingCountRemaining = 0;

            if (settings.InstapaperEnabled && !string.IsNullOrWhiteSpace(settings.InstapaperUsername) && !string.IsNullOrWhiteSpace(settings.InstapaperPassword))
                RefreshingCountRemaining++;

            if (settings.ReadItLaterEnabled && !string.IsNullOrWhiteSpace(settings.ReadItLaterUsername) && !string.IsNullOrWhiteSpace(settings.ReadItLaterPassword))
                RefreshingCountRemaining++;

            // Nothing to do, so return false
            if (RefreshingCountRemaining == 0)
                return false;

            if (settings.ReadItLaterEnabled)
            {
                var readItLaterApi = new ReadItLaterApi();
                readItLaterApi.AddUrlCompleted += new EventHandler<EventArgs>(readItLaterApi_AddUrlCompleted);
                readItLaterApi.AddUrl(settings.ReadItLaterUsername, settings.ReadItLaterPassword, url, description, id.ToString());
            }

            if (settings.InstapaperEnabled)
            {
                var instaApi = new InstapaperApi();
                instaApi.AddUrlCompleted += new EventHandler<EventArgs>(instaApi_AddUrlCompleted);
                instaApi.AddUrl(settings.InstapaperUsername, settings.InstapaperPassword, url, description, id.ToString());
            }

            return true;
        }

        public bool ErrorInstapaper = false;

        private void instaApi_AddUrlCompleted(object sender, EventArgs e)
        {
            RefreshingCountRemaining--;

            var api = sender as InstapaperApi;

            ErrorInstapaper = !api.AddUrlSuccess;

            if (SaveFinishedEvent != null)
                SaveFinishedEvent(this, null);
        
        }

        public bool ErrorReadLater = false;

        private void readItLaterApi_AddUrlCompleted(object sender, EventArgs e)
        {
            RefreshingCountRemaining--;

            var api = sender as ReadItLaterApi;

            ErrorReadLater = !api.AddUrlSuccess;

            if (SaveFinishedEvent != null)
                SaveFinishedEvent(this, null);
        }

    }
}
