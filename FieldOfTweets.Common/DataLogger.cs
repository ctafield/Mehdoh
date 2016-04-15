using System;
using System.IO;
using System.Text;

namespace FieldOfTweets.Common
{
    public class DataLogger : TextWriter
    {

        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        public override void WriteLine(string format, object arg0)
        {
            System.Diagnostics.Debug.WriteLine(format, arg0);
        }
        public override void WriteLine(string format, params object[] arg)
        {
            System.Diagnostics.Debug.WriteLine(format, arg);
        }

        public override void Write(char[] buffer)
        {
            System.Diagnostics.Debug.WriteLine(buffer);
        }

        public override void Write(char value)
        {
            System.Diagnostics.Debug.WriteLine(value);
        }

        public override void Write(bool value)
        {
            System.Diagnostics.Debug.WriteLine(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            // this one
            var newString = new string(buffer, index, count);
            System.Diagnostics.Debug.WriteLine(newString);
        }

        public override void WriteLine(string format, object arg0, object arg1)
        {
            System.Diagnostics.Debug.WriteLine(format, arg0, arg1);
        }

        public override void Write(object value)
        {
            System.Diagnostics.Debug.WriteLine(value);
        }

        public override void WriteLine()
        {
            System.Diagnostics.Debug.WriteLine(Environment.NewLine);
        }

        public override void Write(string value)
        {
            System.Diagnostics.Debug.WriteLine(value);
        }

    }

}
