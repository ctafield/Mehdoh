#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.POCO;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

#endregion

namespace Mitter
{
    public partial class Customise : AnimatedBasePage
    {
        private readonly object RefreshColumnsLock = new object();

        public Customise()
        {

            InitializeComponent();

            AnimationContext = LayoutRoot;

            CollectionLoaded = false;

            CurrentItems = new ObservableCollection<CustomiseItemCoreViewModel>();

            CurrentItems.CollectionChanged += CurrentItems_CollectionChanged;

            ShowingDelete = false;
            ApplicationBar = Resources["GeneralAppBar"] as ApplicationBar;
            ApplicationBar.MatchOverriddenTheme();

            OriginalState = new List<ColumnModel>();
            OriginalState.AddRange(ColumnHelper.ColumnConfig);

        }

        private ObservableCollection<CustomiseItemCoreViewModel> CurrentItems { get; set; }

        private bool ShowingDelete { get; set; }

        private List<ColumnModel> OriginalState { get; set; }

        protected bool CollectionLoaded { get; set; }

        void CurrentItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            if (!CollectionLoaded)
                return;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // ok its moved
                var newList = CurrentItems.Select(item => item.AsDomainModel()).ToList();
                ColumnHelper.ColumnConfig = newList;                
            }

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            RefreshColumns();

            base.OnNavigatedTo(e);

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CollectionLoaded = false;

            base.OnNavigatedFrom(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {

            if (ShowingDelete)
            {
                HideDelete();
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {

            switch (animationType)
            {
                case AnimationType.NavigateForwardIn:
                    return new SlideUpAnimator { RootElement = LayoutRoot };

                case AnimationType.NavigateBackwardOut:
                    return new SlideDownAnimator { RootElement = LayoutRoot };

                case AnimationType.NavigateBackwardIn:
                    return new SlideUpAnimator { RootElement = LayoutRoot };

                case AnimationType.NavigateForwardOut:
                    return new SlideDownAnimator { RootElement = LayoutRoot };
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        private void RefreshColumns()
        {

            lock (RefreshColumnsLock)
            {
                ColumnHelper.RefreshConfig();

                CurrentItems.Clear();

                var totalItems = ColumnHelper.ColumnConfig.Count;

                for (int i = 0; i < totalItems; i++)
                {
                    var viewModel = ColumnHelper.ColumnConfig[i].AsViewModel();
                    CurrentItems.Add(viewModel);
                }

                lstCurrentItems.DataContext = CurrentItems;

                CollectionLoaded = true;
            }

            //lstCurrentItems.ItemsSource = currentItems;

        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                if (NeedsRebuild())
                {
                    // any change?
                    ((IMehdohApp)(Application.Current)).RebindColumns();
                }
            }
        }

        private bool NeedsRebuild()
        {

            if (ColumnHelper.ColumnConfig == null)
                return true;

            // any difference?
            if (ColumnHelper.ColumnConfig.Count != OriginalState.Count)
                return true;

            for (int i = 0; i < OriginalState.Count; i++)
            {
                var original = OriginalState[i];
                var newitem = ColumnHelper.ColumnConfig[i];
                if (original.CompareTo(newitem) != 0)
                    return true;
            }

            return false;

        }


        private void mnuAddItem_Click(object sender, EventArgs e)
        {

#if MEHDOH_FREE
            if (ColumnHelper.ColumnConfig.Count >= 4)
            {

                const string upgradeMessage = "The free version of Mehdoh is limited to a maximum of 4 items.\n\nWould you ike to buy the full version so that you can add more?";
                UiHelper.ShowUpgrade(upgradeMessage);
                return;
            }            
#else
            if (LicenceInfo.IsTrial())
            {
                if (ColumnHelper.ColumnConfig.Count >= 4)
                {
                    MessageBox.Show("sorry, but the trial of mehdoh only allows a maximum of 4 items. buy the full version to be able to add more!", "trial mode", MessageBoxButton.OK);
                    return;
                }
            }            
#endif

            NavigationService.Navigate(new Uri("/CustomiseItems.xaml", UriKind.Relative));

        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Mitter.UI.Settings;component/Settings_Startup.xaml", UriKind.Relative));
        }

        private void lstCurrentItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // todo: blank the selection
        }

        private void mnuSelect_Click(object sender, EventArgs e)
        {

            if (ShowingDelete)
            {
                HideDelete();
            }
            else
            {
                ShowDelete();
            }
        }

        private void ShowDelete()
        {

            foreach (var item in CurrentItems)
            {
                item.CheckVisible = Visibility.Visible;
                item.IsChecked = false;
            }

            lstCurrentItems.UpdateLayout();

            ApplicationBar = Resources["DeleteAppBar"] as ApplicationBar;
            ApplicationBar.MatchOverriddenTheme();

            ShowingDelete = true;
        }

        private void HideDelete()
        {

            foreach (var item in CurrentItems)
            {
                item.CheckVisible = Visibility.Collapsed;
            }

            lstCurrentItems.UpdateLayout();

            ApplicationBar = Resources["GeneralAppBar"] as ApplicationBar;
            ApplicationBar.MatchOverriddenTheme();

            ShowingDelete = false;
        }

        private void mnuRemove_Click(object sender, EventArgs e)
        {

            var res = MessageBox.Show("are you sure you want to remove these items?", "remove item", MessageBoxButton.OKCancel);

            if (res != MessageBoxResult.OK)
                return;

            var selectedItems = CurrentItems.Where(x => x.IsChecked.HasValue && x.IsChecked.Value).Select(x => x.AsDomainModel()).ToList();
            ColumnHelper.RemoveColumns(selectedItems);

            HideDelete();

            CollectionLoaded = false;
            RefreshColumns();

        }

    }
}