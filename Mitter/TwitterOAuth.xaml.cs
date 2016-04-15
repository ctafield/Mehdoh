using System;
using System.Linq;
using System.Net;
using System.Windows;
using FieldOfTweets.Common;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.ColumnConfig;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Animations.Page;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.Resources;
using Hammock.Silverlight.Compat;
using Hammock.Web;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using Hammock;
using Hammock.Authentication.OAuth;

namespace Mitter
{

    public partial class TwitterOAuth : AnimatedBasePage
    {

        private string _oAuthTokenSecret;
        private string _oAuthToken;

        public TwitterOAuth()
        {
            InitializeComponent();

            AnimationContext = LayoutRoot;

            Loaded += new RoutedEventHandler(TwitterOAuth_Loaded);
        }

        void TwitterOAuth_Loaded(object sender, RoutedEventArgs e)
        {

        }

        protected override AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {
            if (animationType == AnimationType.NavigateForwardIn)
                return new SlideUpAnimator() { RootElement = LayoutRoot };
            else if (animationType == AnimationType.NavigateBackwardOut)
                return new SlideDownAnimator() { RootElement = LayoutRoot };

            return null;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            GetTwitterToken();

            SystemTray.ProgressIndicator = new ProgressIndicator
                                               {
                                                   IsVisible = true,
                                                   IsIndeterminate = true,
                                                   Text = "connecting to twitter"
                                               };
        }

        private void GetTwitterToken()
        {

            var credentials = new OAuthCredentials
            {
                Type = OAuthType.RequestToken,
                SignatureMethod = OAuthSignatureMethod.HmacSha1,
                ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                ConsumerKey = TwitterConstants.ConsumerKey,
                ConsumerSecret = TwitterConstants.ConsumerKeySecret,
                Version = TwitterConstants.OAuthVersion,
                CallbackUrl = TwitterConstants.CallbackUri
            };

            var client = new RestClient
            {
                Authority = "https://api.twitter.com/oauth",
                Credentials = credentials,
                HasElevatedPermissions = true
            };

            var request = new RestRequest
            {
                Path = "/request_token"
            };

            client.BeginRequest(request, new RestCallback(TwitterRequestTokenCompleted));

        }

        private int RetryCount;

        private void TwitterRequestTokenCompleted(RestRequest request, RestResponse response, object userstate)
        {


            if (response == null  || string.IsNullOrEmpty(response.Content))
            {
                // nothing. Let's try again
                UiHelper.SafeDispatch(() =>
                {
                    var res = MessageBox.Show("Mehdoh is having problems connecting to Twitter.\n\nWould you like to try again?\n\nTip: Make sure the time is correct on your phone as this can often cause problems.",
                            "Connection problems", MessageBoxButton.OKCancel);
                    if (res == MessageBoxResult.OK)
                    {
                        GetTwitterToken();
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(() => NavigationService.GoBack());
                    }
                });
                return;
            }


            _oAuthToken = GetQueryParameter(response.Content, "oauth_token");
            _oAuthTokenSecret = GetQueryParameter(response.Content, "oauth_token_secret");

            var authorizeUrl = TwitterConstants.AuthoriseUrl + "?oauth_token=" + _oAuthToken;

            if (String.IsNullOrEmpty(_oAuthToken) || String.IsNullOrEmpty(_oAuthTokenSecret))
            {
                RetryCount++;
                if (RetryCount < 3)
                {
                    GetTwitterToken();
                }
                else
                {
                    UiHelper.SafeDispatch(() =>
                                               {
                                                   var res = MessageBox.Show("Mehdoh is having problems connecting to Twitter.\n\nWould you like to try again?\n\nTip: Make sure the time is correct on your phone as this can often cause problems.",
                                                           "Connection problems", MessageBoxButton.OKCancel);
                                                   if (res == MessageBoxResult.OK)
                                                   {
                                                       GetTwitterToken();
                                                   }
                                                   else
                                                   {
                                                       Dispatcher.BeginInvoke(() => NavigationService.GoBack());
                                                   }
                                               });
                }
                return;
            }

            UiHelper.SafeDispatch(() => webBrowser.Navigate(new Uri(authorizeUrl)));
        }

        private static string GetQueryParameter(string input, string parameterName)
        {
            foreach (string item in input.Split('&'))
            {
                var parts = item.Split('=');
                if (parts[0] == parameterName)
                {
                    return parts[1];
                }
            }
            return String.Empty;
        }

        private void webBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            UiHelper.HideProgressBar();
        }

        private void webBrowser_Navigating(object sender, NavigatingEventArgs e)
        {
            SystemTray.ProgressIndicator = new ProgressIndicator
                                               {
                                                   IsVisible = true,
                                                   IsIndeterminate = true,
                                                   Text = "connecting to twitter"
                                               };

            if (e.Uri.AbsoluteUri.CompareTo("https://api.twitter.com/oauth/authorize") == 0)
            {
                UiHelper.HideProgressBar();
            }

            if (!e.Uri.AbsoluteUri.Contains(TwitterConstants.CallbackUri))
                return;

            e.Cancel = true;

            var arguments = e.Uri.AbsoluteUri.Split('?');
            if (arguments.Length < 1)
                return;

            GetAccessToken(arguments[1]);
        }

        private void GetAccessToken(string uri)
        {

            var requestToken = GetQueryParameter(uri, "oauth_token");

            if (requestToken != _oAuthToken)
            {
                Dispatcher.BeginInvoke(() =>
                                           {
                                               App.SucceededLink = false;

                                               MessageBox.Show(ApplicationResources.pleaseauthorise);

                                               if (NavigationService.CanGoBack)
                                               {
                                                   NavigationService.GoBack();
                                               }
                                           });

                return;
            }
                
            App.SucceededLink = true;

            var requestVerifier = GetQueryParameter(uri, "oauth_verifier");

            var credentials = new OAuthCredentials
            {
                Type = OAuthType.AccessToken,
                SignatureMethod = OAuthSignatureMethod.HmacSha1,
                ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                ConsumerKey = TwitterConstants.ConsumerKey,
                ConsumerSecret = TwitterConstants.ConsumerKeySecret,
                Token = _oAuthToken,
                TokenSecret = _oAuthTokenSecret,
                Verifier = requestVerifier
            };

            var client = new RestClient
            {
                Authority = "https://api.twitter.com/oauth",
                Credentials = credentials,
                HasElevatedPermissions = true
            };

            var request = new RestRequest
            {
                Path = "/access_token",
                Method = WebMethod.Post,
                DecompressionMethods = DecompressionMethods.GZip
            };

            client.BeginRequest(request, new RestCallback(RequestAccessTokenCompleted));
        }

        private async void RequestAccessTokenCompleted(RestRequest request, RestResponse response, object userstate)
        {

            // this doesnt do much tbh
            UiHelper.SafeDispatch(() =>
            {
                // clear the cookies
                var cookies = webBrowser.GetCookies();
                foreach (Cookie cookie in cookies)
                {
                    cookie.Expired = true;
                    cookie.Discard = true;
                }
            });


            TwitterAccess twitteruser;

            try
            {
                twitteruser = new TwitterAccess
                {
                    AccessToken = GetQueryParameter(response.Content, "oauth_token"),
                    AccessTokenSecret = GetQueryParameter(response.Content, "oauth_token_secret"),
                    UserId = long.Parse(GetQueryParameter(response.Content, "user_id")),
                    ScreenName = GetQueryParameter(response.Content, "screen_name")
                };
            }
            catch (Exception)
            {
                // retry
                GetTwitterToken();
                return;
            }

            if (String.IsNullOrEmpty(twitteruser.AccessToken) || String.IsNullOrEmpty(twitteruser.AccessTokenSecret))
            {
                Dispatcher.BeginInvoke(() => MessageBox.Show(response.Content));
                return;
            }

            using (var storageHelper = new StorageHelper())
            {
                storageHelper.SaveUser(twitteruser);
            }

            // Create new profile record            
            var settings = new ExternalSettings()
                                            {
                                                ConsumerKey = TwitterConstants.ConsumerKey,
                                                ConsumerKeySecret = TwitterConstants.ConsumerKeySecret,
                                                User = twitteruser
                                            };
            var api = new TwitterApi(twitteruser.UserId, settings);            
            var result = await api.GetUserProfile(twitteruser);
            api_GetUserProfileCompletedEvent(result);

        }

        private void api_GetUserProfileCompletedEvent(ResponseGetUserProfile userProfile)
        {

            if (userProfile == null)
            {
                UiHelper.SafeDispatch(() =>
                {
                    UiHelper.HideProgressBar();
                    MessageBox.Show("Unable to fetch profile from twitter. Something wrong on twitter's side perhaps?", "failed to get profile", MessageBoxButton.OK);
                    if (NavigationService.CanGoBack)
                    {
                        NavigationService.GoBack();
                    }
                });
                return;
            }

            Dispatcher.BeginInvoke(delegate()
            {

                var apiProfile = userProfile.AsViewModel();

                using (var dh = new MainDataContext())
                {

                    var profiles = from s in dh.Profiles
                                   where s.Id == apiProfile.Id
                                   select s;

                    var profile = profiles.FirstOrDefault();

                    if (profile == null)
                    {
                        dh.Profiles.InsertOnSubmit(new ProfileTable()
                        {
                            Id = apiProfile.Id,
                            Bio = apiProfile.Bio,
                            DisplayName = apiProfile.DisplayName,
                            ImageUri = apiProfile.ProfileImageUrl,
                            Location = apiProfile.Location,
                            ScreenName = apiProfile.ScreenName,
                            ProfileType = ApplicationConstants.AccountTypeEnum.Twitter,
                            UseToPost = true
                        });
                    }
                    else
                    {
                        profile.Id = apiProfile.Id;
                        profile.Bio = apiProfile.Bio;
                        profile.DisplayName = apiProfile.DisplayName;
                        profile.ImageUri = apiProfile.ProfileImageUrl;
                        profile.Location = apiProfile.Location;
                        profile.ScreenName = apiProfile.ScreenName;
                        profile.ProfileType = ApplicationConstants.AccountTypeEnum.Twitter;
                    }

                    dh.SubmitChanges();

                    CheckNeedsDefaultColumns(apiProfile.Id);

                    StorageHelperUI.SaveProfileImageCompletedEvent += ImageSaveCompleted;
                    StorageHelperUI.SaveProfileImage(apiProfile.ProfileImageUrl, apiProfile.ProfileImageUrl, apiProfile.Id, ApplicationConstants.AccountTypeEnum.Twitter);

                }

            });

        }

        private void ImageSaveCompleted(object sender, EventArgs e)
        {

            // remove handler
            StorageHelperUI.SaveProfileImageCompletedEvent -= ImageSaveCompleted;

            Dispatcher.BeginInvoke(() =>
            {
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            });

        }

        private void CheckNeedsDefaultColumns(long accountId)
        {

            if (ColumnHelper.ColumnConfig.Count != 0)
                return;

            var dbAdmin = new DatabaseAdministration();
            dbAdmin.AddDefaultTwitterColumns(accountId);

            ((IMehdohApp)(Application.Current)).JustAddedFirstUser = true;

            ((IMehdohApp)(Application.Current)).RebindColumns();

        }

    }

};