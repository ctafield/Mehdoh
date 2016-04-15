using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Microsoft.Phone.Scheduler;

namespace FieldOfTweets.Common.UI
{
    public class BackgroundHelper
    {

        private const string PeriodicTaskName = "FieldOfTweetsAgent";

        private PeriodicTask GetPeriodicTask()
        {
            return ScheduledActionService.Find(PeriodicTaskName) as PeriodicTask;
        }

        public async Task<bool> ReigniteBackgroundTask()
        {
            if (RemoveTask())
                return await AddTask();

            return true;
        }

        public bool IsDisabled()
        {
            var periodicTask = GetPeriodicTask();
            return periodicTask != null && !periodicTask.IsEnabled;
        }

        public bool RemoveTask()
        {
            // If the task already exists and background agents are enabled for the
            // application, you must remove the task and then add it again to update 
            // the schedule
            try
            {
                var res = ScheduledActionService.Find(PeriodicTaskName);
                if (res != null)
                    ScheduledActionService.Remove(PeriodicTaskName);                
            }
            catch
            {
                // Dont care
                return false;
            }

            return true;

        }

        private async Task<bool> AddTask()
        {
            try
            {

                BackgroundAccessStatus result = await BackgroundExecutionManager.RequestAccessAsync();
                
                // The description is required for periodic agents. This is the string that the user
                // will see in the background services Settings page on the device.
                var periodicTask = new PeriodicTask(PeriodicTaskName)
                {
                    Description =
                        "Mehdoh update task. Used to update mentions and direct messages, send notifications and update the live tile in the background."
                };

                ScheduledActionService.Add(periodicTask);

            }
            catch (Exception)
            {
                return false;
            }

            return true;

        }

        public async Task<bool> StartTask()
        {
            return await ReigniteBackgroundTask();
        }

    }
}
