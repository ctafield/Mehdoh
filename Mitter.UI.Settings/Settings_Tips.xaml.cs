using System;
using System.Windows;
using FieldOfTweets.Common.UI.Animations.Page;
using Microsoft.Phone.Tasks;

namespace Mitter.UI.Settings
{
    public partial class Settings_Tips : AnimatedBasePage
    {
        public Settings_Tips()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;
        }

        private void lnkUriInfo_Click(object sender, RoutedEventArgs e)
        {
            var task = new WebBrowserTask
                           {
                               Uri = new Uri("http://www.mehdoh.com/uri.htm", UriKind.Absolute)
                           };
            task.Show();            
        }
    }
}