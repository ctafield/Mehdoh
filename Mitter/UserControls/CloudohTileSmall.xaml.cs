using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Cloudoh.UserControls
{
    public partial class CloudohTileSmall : UserControl
    {
        public CloudohTileSmall()
        {
            InitializeComponent();
        }

        public void SetValues(string screenName, string fileUrl)
        {

            var newImage = new BitmapImage()
            {
                CreateOptions = BitmapCreateOptions.None,
                DecodePixelHeight = 159,
                DecodePixelWidth = 159
            };

            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var stream = myStore.OpenFile(fileUrl, FileMode.Open))
                {
                    newImage.SetSource(stream);
                }
            }

            image.Source = newImage;

            this.UpdateLayout();
        }
    }
}
