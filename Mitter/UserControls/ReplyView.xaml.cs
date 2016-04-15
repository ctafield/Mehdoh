using System.Windows.Controls;

namespace Mitter.UserControls
{
    public partial class ReplyView : UserControl
    {
        public ReplyView()
        {
            InitializeComponent();
        }

        public void SetValue(object value)
        {
            this.DataContext = value;
        }

    }
}
