using System;
using System.Collections.Generic;
using System.Windows.Controls;
using FieldOfTweets.Common;
using FieldOfTweets.Common.UI.ViewModels;

namespace Mitter.UserControls
{
    public partial class AddAccount : UserControl
    {
        
        public AddAccount()
        {
            InitializeComponent();
            BindValues();
        }

        private void BindValues()
        {

            var model = new List<AddAccountViewModel>();

            model.Add(new AddAccountViewModel()
                      {
                          AccountName = "twitter",
                          AccountType = ApplicationConstants.AccountTypeEnum.Twitter,
                          ImageUri = "/Images/profile_type_twitter.png"
                      });

            model.Add(new AddAccountViewModel()
            {
                AccountName = "instagram",
                AccountType = ApplicationConstants.AccountTypeEnum.Instagram,
                ImageUri = "/Images/profile_type_instagram.png"
            });

            model.Add(new AddAccountViewModel()
            {
                AccountName = "soundcloud",
                AccountType = ApplicationConstants.AccountTypeEnum.Soundcloud,
                ImageUri = "/Images/profile_type_soundcloud.png"
            });

#if WP8_SPECIAL
            model.Add(new AddAccountViewModel()
            {
                AccountName = "facebook *",
                AccountType = ApplicationConstants.AccountTypeEnum.Facebook,
                ImageUri = "/Images/profile_type_facebook.png",
                Subtitle = "* requires in-app purchase"
            });
#endif

            lstAccounts.DataContext = model;

        }

        public ApplicationConstants.AccountTypeEnum SelectedAccountType { get; set; }
        public event EventHandler AccountTypeSelectedEvent;

        private void lstAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (lstAccounts.SelectedIndex == -1)
                return;

            var item = lstAccounts.SelectedItem as AddAccountViewModel;

            SelectedAccountType = item.AccountType;

            lstAccounts.SelectedIndex = -1;

            if (AccountTypeSelectedEvent != null)
                AccountTypeSelectedEvent(this, null);
        }

    }
}
