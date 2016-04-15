using System.Windows;
using FieldOfTweets.Common.UI.Interfaces;

namespace FieldOfTweets.Common.UI.Resources
{

    public class FontResources
    {

        private IMehdohApp App
        {
            get { return ((IMehdohApp) Application.Current); }
        }

        public double FontSizeHeader1
        {            
            get
            {
                
                switch (App.FontSize)
                {                        
                    case SettingsHelper.FontSizeSmallest:
                        return 34;
                    case SettingsHelper.FontSizeSmaller:
                        return 35;
                    case SettingsHelper.FontSizeOriginal:                        
                        return 36;
                    case SettingsHelper.FontSizeLarger:
                        return 37;
                    case SettingsHelper.FontSizeLargest:
                        return 38;
                }
                return 26;
            }            
        }

        public double FontSizeHeader2
        {
            get
            {
                switch (App.FontSize)
                {
                    case SettingsHelper.FontSizeSmallest:
                        return 24;
                    case SettingsHelper.FontSizeSmaller:
                        return 25;
                    case SettingsHelper.FontSizeOriginal:
                        return 26;
                    case SettingsHelper.FontSizeLarger:
                        return 27;
                    case SettingsHelper.FontSizeLargest:
                        return 28;
                }
                return 26;
            }
        }

        public double FontSizeNormalDetails
        {
         
            get { return FontSizeNormal + 4; }
        }

        public double FontSizeNormal
        {
            get
            {
                switch (App.FontSize)
                {
                    case SettingsHelper.FontSizeSmallest:
                        return 16;
                    case SettingsHelper.FontSizeSmaller:
                        return 18;
                    case SettingsHelper.FontSizeOriginal:
                        return 20;
                    case SettingsHelper.FontSizeLarger:
                        return 24;
                    case SettingsHelper.FontSizeLargest:
                        return 32;
                }
                return 20;
            }
        }

    }

}
