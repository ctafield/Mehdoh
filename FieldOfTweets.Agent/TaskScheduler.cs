using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.ErrorLogging;
using FieldOfTweets.Common.Settings;
using Microsoft.Phone.Scheduler;

namespace FieldOfTweets.Agent
{

    public class ScheduledAgent : ScheduledTaskAgent
    {

        private string MessageUser { get; set; }
        private string MessageText { get; set; }

        private string MentionUser { get; set; }
        private string MentionText { get; set; }

        private int NewMentions { get; set; }
        private int NewMessages { get; set; }

        private int MoreNewMentions { get; set; }
        private int MoreNewMessages { get; set; }

        private List<ToastNotificationTag> ToastNotifications { get; set; } 

        private BackgroundSettings BackgroundTaskSettings { get; set; }

#if LOGGING
        private DateTime StartDateTime { get; set; }
        private long StartDeviceTotalMemory { get; set; }
        private long StartApplicationCurrentMemoryUsage { get; set; }
#endif

        private string _userProfileUrl;
        private long _messagesSinceId = 0;
        private long _mentionsSinceId = 0;

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override async void OnInvoke(ScheduledTask task)
        {

#if LOGGING
            StartDateTime = DateTime.Now;
            StartDeviceTotalMemory = DeviceStatus.DeviceTotalMemory;
            StartApplicationCurrentMemoryUsage = DeviceStatus.ApplicationCurrentMemoryUsage;

            WriteMemoryUsage("before get all settings");
#endif
            WriteLastStarted();

            var sh = new SettingsHelper();
            BackgroundTaskSettings = sh.GetSettingsBackground();

            if (BackgroundTaskSettings.OnlyUpdateOnWifi && !ConnectionHelper.IsOnWifi())
            {
                DatabaseMaintenance();
                // notify that we've finished
                Finished();
                return;
            }

#if LOGGING
            WriteMemoryUsage("after get all settings");
#endif

            NewMentions = 0;
            NewMessages = 0;
            ToastNotifications = new List<ToastNotificationTag>();

            long accountId;

            // Load the user data
            using (var storage = new StorageHelper())
            {
                var users = storage.GetAuthorisedTwitterUsers();
                var firstUser = users.FirstOrDefault();

                if (firstUser == null)
                {
                    // notify that we've finished
                    Finished();
                    return;
                }

                accountId = firstUser.UserId;                
            }

            if (accountId == 0)
            {
                // notify that we've finished
                Finished();
                return;
            }

            DatabaseMaintenance();


#if LOGGING
            WriteMemoryUsage("after GetAuthorisedTwitterUsers");
#endif

            GetIds(accountId);

#if LOGGING
            WriteMemoryUsage("after get since ids");
#endif


            //
            // Create the deferral by requesting it from the task instance.
            //
            await CheckForMentions(accountId);            
            await CheckForDirectMessages(accountId);

            UpdateTileAndToast(accountId);

            // notify that we've finished
            Finished();

        }

        private void GetIds(long accountId)
        {

            try
            {
                using (var dh = new BackgroundTaskDataContext())
                {

                    dh.Log = new DataLogger();

                    var status = dh.ShellStatus.FirstOrDefault();
                    if (status != null)
                    {
                        NewMentions = status.MentionCount;
                        NewMessages = status.MessageCount;

                        _mentionsSinceId = status.LastMentionId;
                        _messagesSinceId = status.LastMessageId;
                    }
                    else
                    {
                        if (dh.Mentions.Any(x => x.ProfileId == accountId))
                            _mentionsSinceId = dh.Mentions.Where(x => x.ProfileId == accountId).Max(x => x.Id);

                        if (dh.Messages.Any(x => x.ProfileId == accountId))
                            _messagesSinceId = dh.Messages.Where(x => x.ProfileId == accountId).Max(x => x.Id);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ErrorLogger.LogException("OnInvoke", e);
            }

        }

        private async Task CheckForMentions(long accountId)
        {
            // Grab the latest data
            var api = new TwitterApi(accountId); // todo: check this for multi account            
            var result = await api.GetMentions(_mentionsSinceId, 20);
            api_GetMentionsCompletedEvent(accountId, result);
        }

        private async Task CheckForDirectMessages(long accountId)
        {

            var newApi = new TwitterApi(accountId);
            var result = await newApi.GetDirectMessages(_messagesSinceId, 20);
            api_GetDirectMessageCompletedEvent(accountId, result);
        }

        private void DatabaseMaintenance()
        {

            return;

            LogDatabaseFileSize("Before");

            // clear down the timeline and mentions etc...
            using (var dh = new MainDataContext())
            {
                var endResults = dh.Timeline.Where(x => x.CreatedAtFormatted < DateTime.Now.AddDays(-1));

                if (endResults.Any())
                {
                    LogDatabaseFileSize("Deleting " + endResults.Count() + " rows", false);
                    dh.Timeline.DeleteAllOnSubmit(endResults);
                    dh.SubmitChanges();
                }
                else
                {
                    LogDatabaseFileSize("Nothing timeline to delete yet", false);
                }

                var mentions = dh.Mentions.Where(x => x.CreatedAtFormatted < DateTime.Now.AddDays(-1));

                if (mentions.Any())
                {
                    LogDatabaseFileSize("Deleting " + mentions.Count() + " rows", false);
                    dh.Mentions.DeleteAllOnSubmit(mentions);
                    dh.SubmitChanges();
                }
                else
                {
                    LogDatabaseFileSize("Nothing metions to delete yet", false);
                }

            }

            LogDatabaseFileSize("After");

        }

        private void LogDatabaseFileSize(string context, bool includeSize = true)
        {

            long fileSize;

            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var file = storage.OpenFile("fot_v2.sdf", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fileSize = file.Length;
                }
            }

            try
            {
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var file = myStore.OpenFile("DBA.log", FileMode.Append))
                    {
                        using (var stream = new StreamWriter(file))
                        {
                            stream.WriteLine(DateTime.Now.ToString(CultureInfo.CurrentUICulture) + ":" + context + ((includeSize) ? ":" + fileSize.ToString() : ""));
                            stream.Flush();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("LogDatabaseFileSize", ex);
            }


        }

        private void api_GetDirectMessageCompletedEvent(long accountId, List<ResponseDirectMessage> directMessages)
        {

            try
            {

#if LOGGING
                WriteMemoryUsage("get dm's completed event - before GC");

                GC.Collect();
                GC.WaitForPendingFinalizers();

                WriteMemoryUsage("get dm's completed event - after GC");
#endif

                if (directMessages != null && directMessages.Any())
                {

                    try
                    {
                        MoreNewMessages = directMessages.Count;
                        NewMessages += MoreNewMessages;

                        var lastMessage = directMessages.OrderByDescending(x => x.id).FirstOrDefault();

                        if (lastMessage != null)
                        {
                            _userProfileUrl = lastMessage.sender.profile_image_url;
                            MessageUser = "@" + lastMessage.sender_screen_name + " (" + lastMessage.sender.name + ")";
                            MessageText = lastMessage.text;
                        }

                        foreach (var item in directMessages)
                        {
                            ToastNotifications.Add(new ToastNotificationTag
                            {
                                Message = "@" + item.sender.screen_name + " messaged you",
                                Tag = "dm:" + item.id
                            });
                        }

                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException("TaskScheduler.api_GetDirectMessageCompletedEvent", ex);
                    }
                    finally
                    {


#if LOGGING      
                        WriteMemoryUsage("get metions completed event - before save messages");
#endif

                        var dsh = new DataStorageHelper();
                        var existing = dsh.LoadExistingMessagesState(accountId) ?? new List<ResponseDirectMessage>();
                        existing.AddRange(directMessages);
                        dsh.SaveMessagesUpdateSerialised(existing, accountId);

                        _messagesSinceId = existing.Max(x => x.id);

#if LOGGING
                        WriteMemoryUsage("get dm's completed event - after save messages");
#endif
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("TaskScheduler.api_GetDirectMessageCompletedEvent", ex);
            }

        }

        private void UpdateTileAndToast(long accountId)
        {

#if LOGGING
            WriteMemoryUsage("check completion");
#endif

            MakeToast();

#if LOGGING
            WriteMemoryUsage("after making the toast");
#endif
            UpdateLiveTile(accountId);

        }

        private void api_GetMentionsCompletedEvent(long accountId, List<ResponseTweet> mentions)
        {

            try
            {

#if LOGGING
                WriteMemoryUsage("get metions completed event - before GC");
#endif

#if LOGGING
                WriteMemoryUsage("get metions completed event - after GC");
#endif

                if (mentions != null && mentions.Any())
                {

                    try
                    {
                        MoreNewMentions = mentions.Count;
                        NewMentions += MoreNewMentions;

                        var lastMention = mentions.OrderByDescending(x => x.id).FirstOrDefault();

                        if (lastMention != null)
                        {
                            _userProfileUrl = lastMention.user.profile_image_url;
                            MentionUser = "@" + lastMention.user.screen_name + " (" + lastMention.user.name + ")";
                            MentionText = lastMention.text;
                        }

                        foreach (var item in mentions)
                        {
                            ToastNotifications.Add(new ToastNotificationTag
                            {
                                Message = "@" + item.user.screen_name + " mentioned you",
                                Tag = "tweet:" + item.id
                            });
                        }

                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.LogException("TaskScheduler.api_GetMentionsCompletedEvent", ex);
                    }
                    finally
                    {
#if LOGGING
                        WriteMemoryUsage("get metions completed event - before save mentions");
#endif
                        var dsh = new DataStorageHelper();

                        // Load the existing ones
                        var existing = dsh.LoadExistingMentionState(accountId) ?? new List<ResponseTweet>();
                        existing.AddRange(mentions);
                        dsh.SaveMentionUpdatesSerialised(existing, accountId);

                        _mentionsSinceId = existing.Max(x => x.id);

#if LOGGING
                        WriteMemoryUsage("get metions completed event - after save mentions");
#endif

                    }

                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("TaskScheduler.api_GetMentionsCompletedEvent", ex);
            }

        }

        private void MakeToast()
        {

            if (!BackgroundTaskSettings.EnableToast)
                return;

            if (BackgroundTaskSettings.SleepEnabled)
            {
                var from = BackgroundTaskSettings.SleepFrom;
                var to = BackgroundTaskSettings.SleepTo;

                var now = DateTime.Now;
                var tomorrow = DateTime.Now.AddDays(1);

                DateTime startQuiet;
                DateTime endQuiet;

                if (from.TimeOfDay.CompareTo(to.TimeOfDay) > 0)
                {
                    // this means its wraps around to next day
                    startQuiet = new DateTime(now.Year, now.Month, now.Day, from.Hour, from.Minute, from.Second);
                    endQuiet = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, to.Hour, to.Minute, to.Second);
                }
                else
                {
                    startQuiet = new DateTime(now.Year, now.Month, now.Day, from.Hour, from.Minute, from.Second);
                    endQuiet = new DateTime(now.Year, now.Month, now.Day, to.Hour, to.Minute, to.Second);
                }

                // is the time in between the sleep period?
                if (DateTime.Compare(now, startQuiet) >= 0 && DateTime.Compare(now, endQuiet) <= 0)
                    return;

            }

            // None in total?
            if (NewMentions == 0 && NewMessages == 0)
                return;

            // None new this time? Don't want to keep re-toasting
            if (MoreNewMentions == 0 && MoreNewMessages == 0)
                return;

            var notifier = ToastNotificationManager.CreateToastNotifier();

            foreach (var toastMessage in ToastNotifications)
            {
                if (notifier.Setting == NotificationSetting.Enabled)
                {                    
                    var toast = ToastHelper.CreateTextOnlyToast("Mehdoh", toastMessage.Message);
                    //toast.Tag = toastMessage.Tag;
                    ToastNotificationManager.CreateToastNotifier().Show(toast);
                }
            }

        }

        private void UpdateLiveTile(long accountId)
        {
            if (NewMentions == 0 && NewMessages == 0)
            {
                return;
            }

            EventWaitHandle wait = new AutoResetEvent(false);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
                                                          {
                                                              var sh = new ShellHelper();
                                                              sh.UpdateLiveTitle(accountId, NewMentions, NewMessages,
                                                                                _mentionsSinceId, _messagesSinceId,
                                                                                MentionUser, MentionText,
                                                                                MessageUser, MessageText,
                                                                                _userProfileUrl, delegate
                                                                                                                          {
#if LOGGING
                                                                     WriteUpdateStats();
#endif
                                                                                                                              wait.Set();
                                                                                                                          });
                                                          });

            wait.WaitOne(1000);

        }

        private void Finished()
        {
            WriteLastFinished();
            NotifyComplete();
        }

        private void WriteLastStarted()
        {

            try
            {
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var file = myStore.OpenFile(ApplicationConstants.BackgroundTaskLastStarted, FileMode.Create))
                    {
                        using (var stream = new StreamWriter(file))
                        {
                            stream.WriteLine(DateTime.Now.ToString(CultureInfo.CurrentUICulture));
                            stream.Flush();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("WriteLastStarted", ex);
            }

        }



        private void WriteLastFinished()
        {

            try
            {
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var file = myStore.OpenFile(ApplicationConstants.BackgroundTaskLastRun, FileMode.Create))
                    {
                        using (var stream = new StreamWriter(file))
                        {
                            stream.WriteLine(DateTime.Now.ToString(CultureInfo.CurrentUICulture));
                            stream.Flush();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("WriteLastFinished", ex);
            }

        }

#if LOGGING

        private readonly object writingupdate = new object();

        private void WriteMemoryUsage(string stage)
        {

            const string statsFile = "BackgroundTask_MemoryProgress.log";

            try
            {
                lock (writingupdate)
                {
                    var currentApplicationCurrentMemoryUsage = DeviceStatus.ApplicationCurrentMemoryUsage;

                    using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (var file = myStore.OpenFile(statsFile, FileMode.Append))
                        {
                            using (var stream = new StreamWriter(file))
                            {
                                var val2 = string.Format(DateTime.Now.ToString(CultureInfo.CurrentUICulture) + " - " + stage + " CurrentMemoryUsage : {0:0.00}b / {1:0.00}kb / {2:0.00}mb",
                                                         currentApplicationCurrentMemoryUsage, currentApplicationCurrentMemoryUsage / 1024,
                                                         (currentApplicationCurrentMemoryUsage / 1024) / 1024);
                                stream.WriteLine(val2);
                            }

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("WriteMemoryUsage", ex);
            }

        }

#endif

#if LOGGING

        private void WriteUpdateStats()
        {


            try
            {

                var endDeviceTotalMemory = DeviceStatus.DeviceTotalMemory;
                var endApplicationCurrentMemoryUsage = DeviceStatus.ApplicationCurrentMemoryUsage;
                var memoryDifference = endApplicationCurrentMemoryUsage - StartApplicationCurrentMemoryUsage;

                const string statsFile = "BackgroundTask_Stats.log";

                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var file = myStore.OpenFile(statsFile, FileMode.Append))
                    {
                        using (var stream = new StreamWriter(file))
                        {
                            stream.WriteLine(new string('*', 80));

                            stream.WriteLine("Start Date Time : " + StartDateTime);
                            stream.WriteLine("End Date Time : " + DateTime.Now.ToString(CultureInfo.CurrentUICulture));

                            stream.WriteLine("StartDeviceTotalMemory : " + StartDeviceTotalMemory);

                            var val1 = string.Format("StartApplicationCurrentMemoryUsage : {0:0.00}b / {1:0.00}kb / {2:0.00}mb",
                             StartApplicationCurrentMemoryUsage, StartApplicationCurrentMemoryUsage / 1024,
                             (StartApplicationCurrentMemoryUsage / 1024) / 1024);

                            stream.WriteLine(val1);

                            stream.WriteLine("EndDeviceTotalMemory : " + endDeviceTotalMemory);

                            var val2 = string.Format("EndApplicationCurrentMemoryUsage : {0:0.00}b / {1:0.00}kb / {2:0.00}mb",
                                                     endApplicationCurrentMemoryUsage, endApplicationCurrentMemoryUsage / 1024,
                                                     (endApplicationCurrentMemoryUsage / 1024) / 1024);

                            stream.WriteLine(val2);

                            stream.WriteLine("Delta : " + memoryDifference);

                            stream.Flush();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Dont care. Just for my amusement
                ErrorLogger.LogException("WriteUpdateStats", ex);
            }

        }
#endif

    }

    internal class ToastNotificationTag
    {
        public string Message { get; set;  }
        public string Tag { get; set; }
    }
}

