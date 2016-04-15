using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ViewModels;
using Telerik.Windows.Controls;

namespace Mitter.UI.Settings
{
    public partial class Settings_Mutes : AnimatedBasePage
    {

        private ObservableCollection<SelectAccountViewModel> ListOfAccounts { get; set; }
        private ObservableCollection<SelectAccountViewModel> MutedUsers { get; set; }

        private long AccountId { get; set; }

        public Settings_Mutes()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            ApplicationBar.MatchOverriddenTheme();

            if (e.NavigationMode == NavigationMode.New)
            {
                LoadSettings();
            }

        }

        private async void LoadSettings()
        {

            ListOfAccounts = new ObservableCollection<SelectAccountViewModel>();
            MutedUsers = new ObservableCollection<SelectAccountViewModel>();

            lstAccounts.DataContext = ListOfAccounts;
            lstMutedUsers.DataContext = MutedUsers;

            using (var dh = new MainDataContext())
            {
                foreach (var profile in dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter))
                {

                    var model = new SelectAccountViewModel()
                    {
                        DisplayName = profile.DisplayName,
                        Id = profile.Id,
                        ScreenName = profile.ScreenName,
                        ImageUrl = profile.ImageUri
                    };

                    ListOfAccounts.Add(model);
                }
            }

        }

        private async Task RefreshMutesForUser(long accountId)
        {

            AccountId = accountId;

            MutedUsers.Clear();

            var api = new TwitterApi(accountId);
            var mutedUsers = await api.GetMutedUsersAsync();

            if (mutedUsers != null && mutedUsers.users != null)
            {
                foreach (var user in mutedUsers.users)
                {
                    var model = new SelectAccountViewModel()
                    {
                        DisplayName = user.name,
                        ScreenName = user.screen_name,
                        Id = user.id,
                        ImageUrl = user.profile_image_url
                    };

                    MutedUsers.Add(model);
                }
            }

        }

        private async void LstAccounts_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = lstAccounts.SelectedItem;
            if (selectedItem == null)
                return;

            var model = selectedItem as SelectAccountViewModel;
            if (model == null)
                return;

            await RefreshMutesForUser(model.Id);
        }

        private async void mnuAddUser_Click(object sender, EventArgs e)
        {

            UiHelper.EnableDisableAppBar(ApplicationBar, false);

            var result = await RadInputPrompt.ShowAsync("mute user", MessageBoxButtons.OKCancel, "Please enter the user name to mute:");
            if (result.ButtonIndex == 0 & !string.IsNullOrEmpty(result.Text.Trim()))
            {
                var screenName = result.Text.Trim().Replace("@", "");
                UiHelper.ShowProgressBar("muting user");
                var api = new TwitterApi(AccountId);
                var muteResult = await api.MuteUserAsync(screenName);
                UiHelper.HideProgressBar();

                if (muteResult == null)
                {
                    UiHelper.ShowToast("Unable to mute " + screenName);
                }
                else
                {
                    UiHelper.ShowToast(screenName + " has been muted");
                    await RefreshMutesForUser(AccountId);                    
                }
            }

            UiHelper.EnableDisableAppBar(ApplicationBar, true);

        }

        private async void mnuUnMute_Click(object sender, EventArgs e)
        {

            UiHelper.EnableDisableAppBar(ApplicationBar, false);

            foreach (var account in lstMutedUsers.CheckedItems)
            {
                var model = account as SelectAccountViewModel;
                if (model == null)
                    continue;

                var api = new TwitterApi(AccountId);
                await api.UnmuteUser(model.ScreenName);
            }

            UiHelper.EnableDisableAppBar(ApplicationBar, true);

            await RefreshMutesForUser(AccountId);

        }

    }

}