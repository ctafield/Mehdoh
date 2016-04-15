using System;
using FieldOfTweets.Common.UI.Resources;
using Microsoft.Phone.Shell;

namespace FieldOfTweets.Common.UI.ExtensionMethods
{
    public static class ApplicationBarExtension
    {

        public static void LocaliseMenu(this IApplicationBar menu)
        {
            if (menu.MenuItems == null)
                return;

            foreach (var item in menu.MenuItems)
            {
                var menuItem = item as ApplicationBarMenuItem;
                if (menuItem == null)
                    continue;

                if (string.Compare(menuItem.Text, "customise home screen", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // replace
                    menuItem.Text = ApplicationResources.customisehomescreen;
                }
            }

        }

    }

}
