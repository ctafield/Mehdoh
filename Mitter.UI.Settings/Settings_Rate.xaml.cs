// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Responses.Twitter;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Resources;
using FieldOfTweets.Common.UI.ViewModels;
using Microsoft.Phone.Controls;

namespace Mitter.UI.Settings
{
    public partial class Settings_Rate : AnimatedBasePage
    {
        #region Constructor

        public Settings_Rate()
        {
            InitializeComponent();
            AnimationContext = LayoutRoot;
        }

        #endregion

        #region Properties

        private Dictionary<long, ObservableCollection<RatesViewModel>> Rates { get; set; }

        protected bool DataLoaded { get; set; }

        private short RateLimitsToFetch { get; set; }

        protected Pivot PivotAccounts { get; set; }

        #endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (DataLoaded)
                return;

            DataLoaded = true;

            Rates = new Dictionary<long, ObservableCollection<RatesViewModel>>();

            ThreadPool.QueueUserWorkItem(CreatePivotsThread);
            
        }

        private List<AccountViewModel> listOfAccounts { get; set; }

        private void CreatePivotsThread(object state)
        {
            using (var dh = new MainDataContext())
            {
                listOfAccounts = dh.Profiles.Where(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter).Select(x => x.AsViewModel()).ToList();
            }

            RateLimitsToFetch = (short)listOfAccounts.Count();

            UiHelper.SafeDispatch(CreatePivots);
        }

        private async void CreatePivots()
        {


            PivotAccounts = new Pivot
                                {
                                    Margin = new Thickness(-12, 0, 0, 0)
                                };
            PivotAccounts.SetValue(Grid.RowProperty, 0);

            PivotAccounts.Items.Clear();

            if (listOfAccounts.Any())
            {
                UiHelper.ShowProgressBar("getting rate limits");
                txtNoTwitter.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtNoTwitter.Visibility = Visibility.Visible;
                return;
            }

            foreach (var account in listOfAccounts)
            {
                // set up the new list to bind to
                Rates.Add(account.Id, new ObservableCollection<RatesViewModel>());

                var lstRates = new ListBox
                                   {
                                       ItemsSource = Rates[account.Id],
                                       ItemTemplate = Resources["rateLimitTemplate"] as DataTemplate,
                                       Name = "list_" + account.Id,
                                       Margin = new Thickness(20, 0, 0, 0)
                                   };

                var pivot = new PivotItem
                                {
                                    Content = lstRates,
                                    Header = GetColumnHeader(account.ScreenName, account.Id),
                                    Name = "pivot_" + account.Id
                                };

                if (PivotAccounts.Items.OfType<PivotItem>().All(x => x.Name != pivot.Name))
                    PivotAccounts.Items.Add(pivot);

                // Now get the rates
                var api = new TwitterApi(account.Id);
                var results = await api.GetRateLimits();
                ApiOnGetRateLimitsCompletedEvent(account.Id, results);
            }

            ContentPanel.Children.Clear();
            ContentPanel.Children.Add(PivotAccounts);

            UiHelper.HideProgressBar();

        }

        private void ApiOnGetRateLimitsCompletedEvent(long accountId, IEnumerable<ResponseRateLimitItem> rateLimits)
        {

            var safeItems = new Dictionary<string, string>
                                {
                                    {"/statuses/home_timeline", "Timeline"},
                                    {"/statuses/mentions_timeline", "Mentions"},
                                    {"/direct_messages", "Direct Messages"},
                                    {"/favorites/list", ApplicationResources.favouriteslist },
                                    {"/search/tweets", "Search Tweets"},
                                    {"/lists/statuses", "List Statuses"},
                                    {"/trends/place", "Trends"},
                                    {"/application/rate_limit_status", "Rate Limits"},
                                    {"/users/show/:id", "Show User Profile"}
                                };

            UiHelper.SafeDispatchSync(() =>
                                      {
                                          foreach (var item in rateLimits)
                                          {
                                              if (safeItems.ContainsKey(item.title))
                                              {
                                                  Rates[accountId].Add(new RatesViewModel
                                                                           {
                                                                               ItemTitle = safeItems[item.title],
                                                                               Limit = item.limit,
                                                                               Remaining = item.remaining,
                                                                               TimeToReset = item.reset.ToString()
                                                                           });
                                              }
                                          }
                                      });

        }

        private object GetColumnHeader(string title, long accountId)
        {
            var outerStackPanel = new StackPanel
                                      {
                                          Orientation = System.Windows.Controls.Orientation.Horizontal,
                                      };

            if (accountId > 0)
            {
                var profileImage = new Image
                                       {
                                           Width = 40,
                                           Height = 40,
                                           Margin = new Thickness(0, 10, 10, 0)
                                       };

                var profileImageUrl = StorageHelper.GetProfileImageForUser(accountId);

                if (!string.IsNullOrWhiteSpace(profileImageUrl))
                {
                    try
                    {
                        Delay.LowProfileImageLoader.SetUriSource(profileImage, new Uri(profileImageUrl, UriKind.Relative));
                    }
                    catch
                    {
                    }
                }

                var innerStackPanel = new StackPanel
                                          {
                                              Margin = new Thickness(0, 10, 10, 0),
                                              Height = 40,
                                              Background = Resources["PhoneAccentBrush"] as Brush,
                                              Orientation = System.Windows.Controls.Orientation.Horizontal
                                          };

                outerStackPanel.Children.Add(innerStackPanel);

                outerStackPanel.Children.Add(profileImage);
            }

            var textBlock = new TextBlock
                                {
                                    Text = title.ToLower(),
                                    FontSize = 48
                                };

            outerStackPanel.Children.Add(textBlock);

            return outerStackPanel;
        }
    }
}