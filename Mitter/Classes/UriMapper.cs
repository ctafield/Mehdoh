using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using FieldOfTweets.Common;
using FieldOfTweets.Common.UI.Friends;
using FieldOfTweets.Common.UI.Interfaces;

namespace Mitter.Classes
{

    public class MehdohUriMapper : UriMapperBase
    {

        public override Uri MapUri(Uri uri)
        {


            if (((IMehdohApp)Application.Current).ShareLink != null)
            {

                var shareLink = ((IMehdohApp)Application.Current).ShareLink;

                if (!string.IsNullOrEmpty(shareLink.Link))
                {
                    var desc = shareLink.Title + " - " + shareLink.Link;
                    return new Uri("/NewTweet.xaml?terminate=1&desc=" + System.Net.HttpUtility.UrlEncode(desc), UriKind.Relative);
                }

                if (!string.IsNullOrEmpty(shareLink.FilePath))
                {
                    var url = shareLink.FilePath;
                    return new Uri("/NewTweet.xaml?terminate=1&shareImage=" + System.Net.HttpUtility.UrlEncode(url), UriKind.Relative);                    
                }

            }

            string _tempUri = System.Net.HttpUtility.UrlDecode(uri.ToString());

            try
            {

                if (_tempUri.Contains("action=Post_Update"))
                {

                    if (!AnyValidTwitterAccounts())
                    {
                        return new Uri("/AccountManagement.xaml", UriKind.Relative);
                    }

                    ThreadPool.QueueUserWorkItem(delegate { FriendsCache.LoadFriendsCache(); });

                    
                    // Map the show products request to NewTweet.xaml
                    return new Uri("/NewTweet.xaml?terminate=1", UriKind.Relative);

                }
                
                if (_tempUri.Contains("mehdoh:TwitterPost?Text="))
                {

                    if (!AnyValidTwitterAccounts())
                    {
                        return new Uri("/AccountManagement.xaml", UriKind.Relative);
                    }

                    ThreadPool.QueueUserWorkItem(delegate { FriendsCache.LoadFriendsCache(); });

                    // Get the category ID (after "CategoryID=").
                    int textIndex = _tempUri.IndexOf("Text=", System.StringComparison.Ordinal) + 5;
                    string searchTerm = _tempUri.Substring(textIndex);

                    // Map the show products request to ShowProducts.xaml
                    return new Uri("/NewTweet.xaml?terminate=1&desc=" + System.Net.HttpUtility.UrlEncode(searchTerm), UriKind.Relative);

                }
                
                if (_tempUri.Contains("mehdoh:TweetDetails?Id="))
                {
                    if (!AnyValidTwitterAccounts())
                    {
                        return new Uri("/AccountManagement.xaml", UriKind.Relative);
                    }

                    // Get the category ID (after "CategoryID=").
                    int termIndex = _tempUri.IndexOf("Id=", System.StringComparison.Ordinal) + 3;
                    string searchTerm = _tempUri.Substring(termIndex);

                    // Map the show products request to ShowProducts.xaml
                    string firstAccountId = GetFirstAccountId().ToString();
                    return new Uri("/DetailsPage.xaml?accountId=" + firstAccountId + "&id=" + System.Net.HttpUtility.UrlEncode(searchTerm), UriKind.Relative);

                }
                
                if (_tempUri.Contains("mehdoh:TwitterSearch?Term="))
                {

                    if (!AnyValidTwitterAccounts())
                    {
                        return new Uri("/AccountManagement.xaml", UriKind.Relative);
                    }

                    ThreadPool.QueueUserWorkItem(delegate { FriendsCache.LoadFriendsCache(); });

                    // Get the category ID (after "CategoryID=").
                    int termIndex = _tempUri.IndexOf("Term=", System.StringComparison.Ordinal) + 5;
                    string searchTerm = _tempUri.Substring(termIndex);

                    // Map the show products request to ShowProducts.xaml
                    string firstAccountId = GetFirstAccountId().ToString();
                    return new Uri("/SearchResults.xaml?accountId=" + firstAccountId + "&term=" + System.Net.HttpUtility.UrlEncode(searchTerm), UriKind.Relative);

                }
                else if (_tempUri.Contains("mehdoh:TwitterProfile?Screen="))
                {

                    if (!AnyValidTwitterAccounts())
                    {
                        return new Uri("/AccountManagement.xaml", UriKind.Relative);
                    }

                    ThreadPool.QueueUserWorkItem(delegate { FriendsCache.LoadFriendsCache(); });

                    // Get the category ID (after "CategoryID=").
                    int termIndex = _tempUri.IndexOf("Screen=", System.StringComparison.Ordinal) + 7;
                    string searchTerm = _tempUri.Substring(termIndex);

                    // Map the show products request to ShowProducts.xaml
                    string firstAccountId = GetFirstAccountId().ToString();
                    return new Uri("/UserProfile.xaml?accountId=" + firstAccountId + "&screen=" + System.Net.HttpUtility.UrlEncode(searchTerm), UriKind.Relative);

                }
                
                if (_tempUri.Contains("mehdoh:"))
                {
                    if (!AnyValidTwitterAccounts())
                    {
                        return new Uri("/AccountManagement.xaml", UriKind.Relative);
                    }

                    return new Uri("/MainPage.xaml", UriKind.Relative);
                }            

                // Check for lock screen launching
                if (_tempUri.Contains("/MainPage.xaml?WallpaperSettings=1"))
                {

                    var newUri = new Uri("/Mitter.UI.Settings;component/Settings_TileWP8.xaml", UriKind.Relative);
                    return newUri;
                }

                // check for voice commands
                if (_tempUri.Contains("voicecommandname"))
                {

                    var stringUri = _tempUri.ToLower();

                    // NewTweet / Compose / Reply -> NewTweet.xaml
                    if (stringUri.Contains("newtweet"))
                    {
                        var newUri = new Uri("/NewTweet.xaml?voiceCommandName=NewTweet", UriKind.Relative);
                        return newUri;
                    }
                    if (stringUri.Contains("compose"))
                    {
                        var newUri = new Uri("/NewTweet.xaml?voiceCommandName=Compose", UriKind.Relative);
                        return newUri;
                    }
                    if (stringUri.Contains("reply"))
                    {
                        var newUri = new Uri("/NewTweet.xaml?voiceCommandName=Reply", UriKind.Relative);
                        return newUri;
                    }
                    // Trends -> trends.xaml
                    if (stringUri.Contains("trends"))
                    {
                        var newUri = new Uri("/Trends.xaml", UriKind.Relative);
                        return newUri;
                    }
                    // WhoToFollow -> FollowSuggestions.xaml
                    if (stringUri.Contains("WhoToFollow"))
                    {
                        var newUri = new Uri("/FollowSuggestions.xaml", UriKind.Relative);
                        return newUri;
                    }
                }

            }
            catch (Exception)
            {
                // ignore and just fall through to return the normal URI
            }

            // Otherwise perform normal launch.
            return uri;
        }

        private bool AnyValidTwitterAccounts()
        {
            return GetFirstAccountId() != 0;
        }

        private long GetFirstAccountId()
        {
            using (var storage = new StorageHelper())
            {
                var users = storage.GetAuthorisedTwitterUsers();
                if (users == null)
                    return 0;

                var firstUser = users.FirstOrDefault();
                if (firstUser == null)
                    return 0;

                return firstUser.UserId;
            }
        }

    }
}
