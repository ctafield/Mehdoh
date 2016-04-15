using System;
using System.IO;
using System.IO.IsolatedStorage;

namespace FieldOfTweets.Common.UI
{

    public class ThemeHelper
    {

        public enum Theme
        {
            System,
            Dark,
            Light,
            MehdohDark,
            MehdohLight,
            GenericModernDark,
            GenericModernLight
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public Theme GetCurrentTheme()
        {
            try
            {
                
                // commented out until fixed by the silverlight toolkit
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    if (!myStore.FileExists("theme.mehdoh"))
                        return Theme.System;

                    using (var file = myStore.OpenFile("theme.mehdoh", FileMode.OpenOrCreate))
                    {
                        using (var writer = new StreamReader(file))
                        {
                            var contents = writer.ReadLine();
                            if (string.IsNullOrEmpty(contents))
                                return Theme.System;
                            var value = (Theme)Enum.Parse(typeof(Theme), contents, true);
                            return value;
                        }
                    }
                }

            }
            catch (Exception)
            {
                return Theme.System;
            }

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public void SetCurrentTheme(Theme theme)
        {
            try
            {

                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var file = myStore.OpenFile("theme.mehdoh", FileMode.OpenOrCreate))
                    {
                        using (var writer = new StreamWriter(file))
                        {
                            writer.WriteLine(theme);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

        }

    }

}
