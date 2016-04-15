using FieldOfTweets.Common.UI.Classes;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;

namespace FieldOfTweets.Common.UI.Interfaces
{
    
    public interface IMehdohApp
    {
        bool JustAddedFirstUser { get; set; }

        MainViewModel ViewModel { get; set; }
        bool ShowTimelineImages { get; }
        string FontSize { get; set; }
        bool DisplayLinks { get; set; }
        bool DisplayMaps { get; set; }
        SupportedPageOrientation SupportedOrientations { get; }
        void RebindColumns();
        void SuspendStreaming();
        void ResetSupportedOrientations();
        
        ShareLink ShareLink { get; set; }

    }

}
