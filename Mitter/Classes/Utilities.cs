namespace Mitter.Classes
{

#if DEBUG

    using System.Diagnostics;
    using System.Windows;
    using System;
    using System.Threading;
    using Microsoft.Phone.Info;

    public class Utilities
    {

        private static Timer _timer;

        public static void BeginRecording()
        {

            // start a timer to report memory conditions every 3 seconds
            //
            _timer = new Timer(state =>
            {
                string c = "unassigned";
                try
                {
                    // 
                }
                catch (ArgumentOutOfRangeException ar)
                {
                    var c1 = ar.Message;

                }
                catch
                {
                    c = "unassigned";
                }


                string report = "";
                report += Environment.NewLine +
                  "Current: " + (DeviceStatus.ApplicationCurrentMemoryUsage / 1000000).ToString() + "MB\n" +
                   "Peak: " + (DeviceStatus.ApplicationPeakMemoryUsage / 1000000).ToString() + "MB\n" +
                   "Memory Usage Limit: " + (DeviceStatus.ApplicationMemoryUsageLimit / 1000000).ToString() + "MB\n\n" +
                   "Device Total Memory: " + (DeviceStatus.DeviceTotalMemory / 1000000).ToString() + "MB\n" +
                   "Working Set Limit: " + Convert.ToInt32((Convert.ToDouble(DeviceExtendedProperties.GetValue("ApplicationWorkingSetLimit")) / 1000000)).ToString() + "MB";

                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Debug.WriteLine(report);
                });

            },
                null,
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(3));
        }
    }

#endif

}
