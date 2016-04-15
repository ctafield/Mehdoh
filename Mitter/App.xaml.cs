// *********************************************************************************************************
// <copyright file="App.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Navigation;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Chat;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.UI;
using FieldOfTweets.Common.UI.Classes;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.ViewModels;

#if WP8
using CrittercismSDK;
#endif

#if WP7
using BugSense;
#endif

using FieldOfTweets.Common;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.ErrorLogging;
using FieldOfTweets.Common.Exceptions;
using FieldOfTweets.Common.UI.Friends;

using Microsoft.Phone.Controls;
using Microsoft.Phone.Info;
using Microsoft.Phone.Shell;
using Mitter.Classes;
using Telerik.Windows.Controls;
using StorageHelper = FieldOfTweets.Common.StorageHelper;

//using BugSense;

#endregion

namespace Mitter
{

    public partial class App : Application, IMehdohApp
    {

        public ShareLink ShareLink { get; set; }

        #region Constants

        private static MainViewModel _viewModel;
        private static string _fontSize;
        private static bool? _displayLinks;
        private static bool? _displayMaps;
        private static bool? _showTimelineImages;
        private static SupportedPageOrientation? _supportedOrientations;

        #endregion

        #region Fields

#if WP8
        private bool reset;
#endif

        #endregion

        #region Properties

        public bool JustAddedFirstUser { get; set; }

        public static bool AttempedLink { get; set; }
        public static bool SucceededLink { get; set; }

        public string FontSize
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_fontSize))
                {
                    var sh = new SettingsHelper();
                    _fontSize = sh.GetFontSize();
                }
                return _fontSize;
            }
            set
            {
                _fontSize = value;

                // Rebind
                RebindViewModel();
            }
        }

        public bool DisplayLinks
        {
            get
            {
                if (!_displayLinks.HasValue)
                {
                    var sh = new SettingsHelper();
                    _displayLinks = sh.GetDisplayLinks();
                }
                return _displayLinks.Value;
            }
            set { _displayLinks = value; }
        }


        public bool DisplayMaps
        {
            get
            {
                if (!_displayMaps.HasValue)
                {
                    var sh = new SettingsHelper();
                    _displayMaps = sh.GetDisplayMaps();
                }
                return _displayMaps.Value;
            }
            set { _displayMaps = value; }
        }

        public bool ShowTimelineImages
        {
            get
            {
                if (!_showTimelineImages.HasValue)
                {
                    var sh = new SettingsHelper();
                    _showTimelineImages = sh.GetShowTimelineImages();
                }
                return _showTimelineImages.Value;
            }
        }

        /// <summary>
        ///     A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public MainViewModel ViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (_viewModel == null)
                    _viewModel = new MainViewModel();

                return _viewModel;
            }
            set { _viewModel = value; }
        }

        /// <summary>
        ///     Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        public SupportedPageOrientation SupportedOrientations
        {
            get
            {
                if (_supportedOrientations == null)
                {
                    var sh = new SettingsHelper();
                    var value = sh.GetOrientationLock();
                    switch (value)
                    {
                        case ApplicationConstants.OrientationLockEnum.Off:
                            _supportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
                            break;
                        case ApplicationConstants.OrientationLockEnum.Portrait:
                            _supportedOrientations = SupportedPageOrientation.Portrait;
                            break;
                        case ApplicationConstants.OrientationLockEnum.Landscape:
                            _supportedOrientations = SupportedPageOrientation.Landscape;
                            break;
                        default:
                            _supportedOrientations = SupportedPageOrientation.PortraitOrLandscape;
                            break;
                    }
                }

                return _supportedOrientations.Value;
            }
        }


        #endregion

        #region Constructor

        /// <summary>
        ///     Constructor for the Application object.
        /// </summary>
        public App()
        {


#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Utilities.BeginRecording();
            }
#endif

#if WP8
            try
            {
                Crittercism.Init("5174e8b15f721646d9000005");
            }
            catch
            {
                UnhandledException += Application_UnhandledException;
            }

#endif

#if WP7
            // Global handler for uncaught exceptions.             
            try
            {
                var options = new NotificationOptions
                                  {
                                      Type = enNotificationType.MessageBoxConfirm
                                  };
                BugSenseHandler.Instance.Init(this, "c72d4e0d", options);
            }
            catch (Exception)
            {
                UnhandledException += Application_UnhandledException;
            }
#endif


            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            try
            {
                var themeHelper = new ThemeHelper();
                var currentTheme = themeHelper.GetCurrentTheme();

                switch (currentTheme)
                {
                    case ThemeHelper.Theme.System:
                        // nothing
                        break;

                    case ThemeHelper.Theme.Light:
                        ThemeManager.OverrideOptions = ThemeManagerOverrideOptions.SystemTrayAndApplicationBars;
                        ThemeManager.ToLightTheme();
                        break;

                    case ThemeHelper.Theme.Dark:
                        ThemeManager.OverrideOptions = ThemeManagerOverrideOptions.SystemTrayAndApplicationBars;
                        ThemeManager.ToDarkTheme();
                        break;

                    case ThemeHelper.Theme.MehdohDark:
                        ThemeManager.OverrideOptions = ThemeManagerOverrideOptions.SystemTrayAndApplicationBars;
                        ThemeManager.ToDarkTheme();
                        ThemeManager.SetAccentColor(AccentColor.Mehdoh);
                        break;

                    case ThemeHelper.Theme.MehdohLight:
                        ThemeManager.OverrideOptions = ThemeManagerOverrideOptions.SystemTrayAndApplicationBars;
                        ThemeManager.ToLightTheme();
                        ThemeManager.SetAccentColor(AccentColor.Mehdoh);
                        break;

                    case ThemeHelper.Theme.GenericModernLight:
                        ThemeManager.OverrideOptions = ThemeManagerOverrideOptions.SystemTrayAndApplicationBars;
                        ThemeManager.AppBarForegroundIsAccentColour = true;
                        ThemeManager.ToLightTheme();
                        ThemeManager.SetAccentColor(AccentColor.Twitter);
                        break;

                    case ThemeHelper.Theme.GenericModernDark:
                        ThemeManager.OverrideOptions = ThemeManagerOverrideOptions.SystemTrayAndApplicationBars;
                        ThemeManager.AppBarForegroundIsAccentColour = true;
                        ThemeManager.ToDarkTheme();
                        ThemeManager.SetAccentColor(AccentColor.Twitter);
                        break;

                }
            }
            catch (Exception)
            {
            }

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                //Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                // PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

            // Added this in 1.6 in order to get proper language encoding on the Japanese market.
            // reference: http://nanapho.jp/archives/2011/11/how-to-make-your-app-pinworthy-in-japanese-market/
            RootFrame.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.Name);

            RootFrame.Navigating += RootFrame_Navigating;
            RootFrame.Navigated += RootFrame_Navigated;

        }

        #endregion

        #region Members

        public static event EventHandler SuspendStreamingEvent;
        public static event EventHandler ReigniteStreamingEvent;
        public static event EventHandler ColumnsChangedEvent;
        public static event EventHandler FontSizeChangedEvent;

        public void SuspendStreaming()
        {
            if (SuspendStreamingEvent != null)
                SuspendStreamingEvent(null, null);
        }

        public static void ReigniteStreaming()
        {
            if (ReigniteStreamingEvent != null)
                ReigniteStreamingEvent(null, null);
        }

        public void RebindColumns()
        {
            if (ColumnsChangedEvent != null)
                ColumnsChangedEvent(null, null);
        }

        private static void RebindViewModel()
        {
            if (FontSizeChangedEvent != null)
                FontSizeChangedEvent(null, null);
        }

        public void ResetSupportedOrientations()
        {
            _supportedOrientations = null;
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
#if WP8
            reset = e.NavigationMode == NavigationMode.Reset;
#endif
        }

        private void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
#if WP8
            if (reset && e.IsCancelable && !(e.Uri.ToString().Contains("?commandMode=voice") ||
                                             e.Uri.ToString().Contains("/Protocol") ||
                                             e.Uri.ToString().Contains("action=Post_Update")))
            {
                e.Cancel = true;
                reset = false;
            }
#endif

            if (e.Uri.ToString().Contains("/NewTweet.xaml?link=external"))
            {
                ThreadPool.QueueUserWorkItem(delegate { FriendsCache.LoadFriendsCache(); });
            }

            // This is if it's coming from the picture sharer
            if (e.Uri.ToString().Contains("/MainPage.xaml") &&
                e.Uri.ToString().Contains("FileId=") &&
                e.Uri.ToString().Contains("Action=ShareContent"))
            {
                ThreadPool.QueueUserWorkItem(delegate { FriendsCache.LoadFriendsCache(); });

                var oldUri = e.Uri.ToString();
                var newUri = oldUri.Replace("MainPage.xaml", "NewTweet.xaml");
                e.Cancel = true;
                RootFrame.Dispatcher.BeginInvoke(() => RootFrame.Navigate(new Uri(newUri, UriKind.RelativeOrAbsolute)));
            }

        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {

            // Clear the licence cache in case they bought the application
            LicenceInfo.ClearLicenceCache();

            ApplicationUsageHelper.Init(VersionInfo.FullVersion());

            var sharingArgs = e as ShareLaunchingEventArgs;

            if (sharingArgs != null)
            {
                var shareOperation = sharingArgs.ShareTargetActivatedEventArgs.ShareOperation;

                if (shareOperation.Data.Contains(StandardDataFormats.WebLink))
                {
                    Debug.WriteLine("share contains weblink");

                    var task = Task.Run(async () =>
                    {
                        var uri = await shareOperation.Data.GetWebLinkAsync();
                        var title = shareOperation.Data.Properties.Title;

                        ShareLink = new ShareLink
                        {
                            Link = uri.ToString(),
                            Title = title
                        };

                    });

                    task.Wait();

                }
                else if (shareOperation.Data.Contains(StandardDataFormats.StorageItems))
                {

                    var task = Task.Run(async () =>
                    {

                        Debug.WriteLine("share contains storage items");
                        var storageItems = await shareOperation.Data.GetStorageItemsAsync();
                        var t = storageItems.Count;
                        Debug.WriteLine(t.ToString(CultureInfo.InvariantCulture) + " items");

                        foreach (var item in storageItems)
                        {
                            var thisFile = item as IStorageFile;
                            if (thisFile != null)
                            {

                                try
                                {

                                    var newName = await CopyFile(thisFile);

                                    ShareLink = new ShareLink
                                    {
                                        FilePath = newName
                                    };

                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                }


                            }
                        }

                    });

                    task.Wait();

                }

            }

            // check the folders exist
            StorageHelper.CreateUserFolders();


            var dba = new DatabaseAdministration();
            dba.ValidateDatabase();

        }

        private async Task<string> CopyFile(IStorageFile sourceFile)
        {

            var targetFolder = ApplicationData.Current.TemporaryFolder;

            var result = await sourceFile.CopyAsync(targetFolder, sourceFile.Name, NameCollisionOption.ReplaceExisting);

            return result.Name;

        }


        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            // Clear the licence cache in case they bought the application
            LicenceInfo.ClearLicenceCache();

            ApplicationUsageHelper.OnApplicationActivated();

            // Load the current user too
            UiHelper.ValidateUser();

            if (!e.IsApplicationInstancePreserved)
            {
                ((IMehdohApp)(Application.Current)).ViewModel.JustRestored = true;
            }
            else
            {
                ((IMehdohApp)(Application.Current)).ViewModel.JustRestored = false;
            }

            if (!e.IsApplicationInstancePreserved)
            {
                // Ensure that application state is restored appropriately
                if (!((IMehdohApp)(Application.Current)).ViewModel.IsDataLoaded)
                {
                    ((IMehdohApp)(Application.Current)).ViewModel.LoadDataAfterTombstoning();
                    ((IMehdohApp)(Application.Current)).ViewModel.LoadViewStateForTombstoning();
                }


            }

            ThreadPool.QueueUserWorkItem(delegate
                                             {
                                                 var sh = new ShellHelper();
                                                 sh.ResetLiveTile();
                                             });

            FriendsCache.LoadFriendsCache();

            ReigniteStreaming();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // Ensure that required application state is persisted here.
            SuspendStreaming();

            // Save ViewModelState
            ((IMehdohApp)(Application.Current)).ViewModel.SaveViewStateForTombstoning();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {

            try
            {
                ErrorLogger.LogException("Application_UnhandledException", e.ExceptionObject);
            }
            catch (Exception)
            {
            }


            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }

            if (e.ExceptionObject is TwitterApiException)
            {
                var error = e.ExceptionObject as TwitterApiException;

                if (error != null && error.ApiError.Contains("This account is currently suspended"))
                {
                    UiHelper.SafeDispatch(() =>
                                          MessageBox.Show("The account you are viewing has been suspended by Twitter.",
                                                          "Sorry!", MessageBoxButton.OK)
                        );
                }
            }

            e.Handled = true;

            try
            {
#if WP8
                Crittercism.LeaveBreadcrumb("Application_UnhandledException");
                Crittercism.LogHandledException(e.ExceptionObject);
#endif
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Phone application initialization

        // Avoid double-initialization

        #region Fields

        private bool phoneApplicationInitialized;

        #endregion

        // Do not add any additional code to this method

        #region Members

        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            //RootFrame = new  TransitionFrame();            

            RootFrame.Navigated += CompleteInitializePhoneApplication;

#if WP8
            // Assign the URI-mapper class to the application frame.
            RootFrame.UriMapper = new MehdohUriMapper();
#endif

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        #endregion

    }

}