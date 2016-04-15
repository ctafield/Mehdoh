using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Mitter.UserControls
{
    public partial class UserBanner : UserControl
    {

        public static readonly DependencyProperty BannerUrlProperty =
            DependencyProperty.Register("BannerUrl", typeof(string), typeof(UserBanner), null);

        public Uri BannerUrl
        {
            get { return (Uri) GetValue(BannerUrlProperty); }
            set { SetValue(BannerUrlProperty, value);}
        }


        public static readonly DependencyProperty ScreenNameProperty =
            DependencyProperty.Register("ScreenName", typeof(string), typeof(UserBanner), null);

        public string ScreenName
        {
            get { return (string)GetValue(ScreenNameProperty); }
            set { SetValue(ScreenNameProperty, value); }
        }


        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(string), typeof(UserBanner), null);

        public string DisplayName
        {
            get { return (string)GetValue(DisplayNameProperty); }
            set { SetValue(DisplayNameProperty, value); }
        }

        public UserBanner()
        {
            InitializeComponent();
        }

    }

}
