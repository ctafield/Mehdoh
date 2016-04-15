using System;
using System.Linq;
using FieldOfTweets.Common;
using Microsoft.Phone.Shell;

namespace Mitter
{
    public class ShellHelperUi
    {

        public static void PinNewTweet()
        {

            var tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains("NewTweet.xaml"));

            if (tile != null)
            {
                tile.Delete();
            }

            var newTile = new StandardTileData()
            {
                Title =  ApplicationConstants.ApplicationName,
                BackgroundImage = new Uri("/Background-NewTweet.png", UriKind.RelativeOrAbsolute),
            };

            ShellTile.Create(new Uri("/NewTweet.xaml?link=external", UriKind.Relative), newTile);

        }

    }

}
