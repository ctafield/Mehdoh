using System;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.CompilerServices;

namespace FieldOfTweets.Common.ErrorLogging
{

    public class ErrorLogger
    {

        public static string GetFileName()
        {
            return string.Format("ErrorLog_{0}.log", DateTime.Now.ToString("yyyy_MM_dd"));
        }

        public static void LogException(string methodName, Exception ex)
        {

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var file = myStore.OpenFile(GetFileName(), FileMode.Append))
                {
                    using (var stream = new StreamWriter(file))
                    {
                        stream.WriteLine(new string('*', 80));
                        stream.WriteLine("Time : " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
                        stream.WriteLine("App Version : " + VersionInfo.FullVersion());
                        stream.WriteLine("Method : " + methodName);
                        stream.WriteLine("Message : " + ex.Message);
                        stream.WriteLine("Exception Type : " + ex.GetType());
                        stream.WriteLine("Stack Trace : " + ex.StackTrace + Environment.NewLine);
                    }
                }
            }

            if (ex.InnerException != null)
            {
                LogException(methodName, ex.InnerException);
            }

        }

        public static void LogException(Exception ex, [CallerMemberName] string methodName = "")
        {
            LogException(methodName, ex);
        }

    }

}
