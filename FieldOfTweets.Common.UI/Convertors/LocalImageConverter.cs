using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FieldOfTweets.Common.UI.Convertors
{
    public class LocalImageConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value == null) return null;

            string stringUri;

            var uri = value as Uri;
            if (uri != null)
                stringUri = uri.ToString();
            else if (value is string)
                stringUri = (string)value;
            else
                return null;

            if (stringUri.StartsWith("http"))
                return value;

            if (stringUri.EndsWith("/"))
                return value;

            try
            {
                var newImage = new BitmapImage()
                               {
                                   CreateOptions = BitmapCreateOptions.BackgroundCreation
                               };

                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = myStore.OpenFile(stringUri, FileMode.Open))
                    {
                        newImage.SetSource(stream);
                    }
                }

                return newImage;

            }
            catch (Exception)
            {
                return value;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }
}
