using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.ViewModels;

namespace FieldOfTweets.Common.UI.UserControls
{
    public partial class SelectTwitterAccount : UserControl
    {

        
        public SelectTwitterAccount()
        {
            InitializeComponent();

            Items = new ObservableCollection<SelectAccountViewModel>();
            lstAccounts.DataContext = Items;

            imgTick.Source = UiHelper.GetTickImage();

            Loaded += new RoutedEventHandler(SelectTwitterAccount_Loaded);
            // now get the accounts
            LoadAccounts();

        }

        void SelectTwitterAccount_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        public ObservableCollection<SelectAccountViewModel> Items { get; set; }

        private List<AccountFriendViewModel> _existingValues;
        public List<AccountFriendViewModel> ExistingValues
        {
            get
            {
                return _existingValues;
            }
            set
            {
                _existingValues = value;
                UpdateChecked();
            }
        }

        public List<long> AllowedIds { get; set; }

        public void UpdateAllowed()
        {
            if (AllowedIds == null || !AllowedIds.Any())
                return;

            var allowed = Items.Where(x => AllowedIds.Contains(x.Id)).ToList();

            Items.Clear();

            foreach (var item in allowed)
                Items.Add(item);

        }

        private void UpdateChecked()
        {

            if (ExistingValues == null)
                return;

            foreach (var profile in ExistingValues)
            {
                var otherItem = Items.SingleOrDefault(x => x.Id == profile.Id);
                if (otherItem != null)
                {
                    otherItem.IsSelected = profile.IsFriend;
                }
            }

        }


        private void LoadAccounts()
        {

            using (var dh = new MainDataContext())
            {

                //var accounts = dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter);
                var accounts = dh.Profiles.Where(x => x.UseToPost == true);

                foreach (var account in accounts)
                {
                    var model = account.AsViewModel();
                    var newModel = SelectAccountViewModel.FromAccountViewModel(model);
                    Items.Add(newModel);
                }

            }

        }

        public event EventHandler CheckPressed;

        private void Image_Tap(object sender, GestureEventArgs e)
        {
            if (CheckPressed != null)
                CheckPressed(this, null);
        }

        private void lstAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var listbox = sender as ListBox;

            if (listbox.SelectedIndex == -1)
                return;

            var listItem = (listbox.SelectedItem) as SelectAccountViewModel;

            listItem.IsSelected = !(listItem.IsSelected);

            listbox.SelectedIndex = -1;
        }

    }
}
