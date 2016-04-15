using System.Windows;
using FieldOfTweets.Common.UI.Interfaces;
using Microsoft.Phone.Controls;

namespace FieldOfTweets.Common.UI.LockBase
{
    public class LockBasePage : PhoneApplicationPage
    {
        public LockBasePage() : base()
        {           
            SupportedOrientations = ((IMehdohApp)Application.Current).SupportedOrientations;
        }
    }
}
