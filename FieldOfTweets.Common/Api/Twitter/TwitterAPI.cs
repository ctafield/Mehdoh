using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using FieldOfTweets.Common.ErrorLogging;
using Hammock;
using Hammock.Authentication.OAuth;
using Hammock.Caching;
using Hammock.Retries;
using Hammock.Silverlight.Compat;
using Hammock.Streaming;
using Hammock.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using HttpUtility = System.Web.HttpUtility;

using FieldOfTweets.Common.Api.Responses.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.Responses;

namespace FieldOfTweets.Common.Api.Twitter
{

    public class TwitterApi : MehdohApi
    {

        private OAuthCredentials Credentials { get; set; }
        private RestClient Client { get; set; }

        private int _refreshCount;

        public object State { get; set; }

        public int ApiFetchCount
        {
            get
            {
                try
                {
                    if (_refreshCount == 0)
                    {
                        var sh = new SettingsHelper();
                        _refreshCount = sh.GetRefreshCount();
                    }

                }
                catch (Exception)
                {
                    _refreshCount = 50;
                }

                return _refreshCount;
            }
        }

        #region Check the Rate Limit

        public class TwitterError
        {
            public string message { get; set; }
            public int code { get; set; }
        }

        public class TwitterErrorResponseSimple
        {
            public string errors { get; set; }
        }

        public class TwitterErrorResponse
        {
            public List<TwitterError> errors { get; set; }
        }

        public bool IsRateLimited { get; set; }

        private bool CheckIsRateLimited(RestResponse response)
        {

            if (response == null)
            {
                return false;
            }

            return CheckIsRateLimited(response.Content);
        }

        private bool CheckIsRateLimited(string content)
        {

            if (string.IsNullOrEmpty(content) || content.Length < 10)
                return false;

            if (!content.StartsWith("{\"errors\""))
                return false;

            var errors = new List<string>();

            try
            {
                var res = JsonConvert.DeserializeObject<TwitterErrorResponse>(content,
                                                                            new JsonSerializerSettings
                                                                            {
                                                                                Error = delegate(object sender, ErrorEventArgs args)
                                                                                {
                                                                                    errors.Add(args.ErrorContext.Error.Message);
                                                                                    args.ErrorContext.Handled = true;
                                                                                }
                                                                            });

                if (res != null && res.errors != null && res.errors.Any())
                {

                    try
                    {

                        var firstError = res.errors.FirstOrDefault();
                        if (firstError != null)
                        {
                            if (firstError.code == 88)
                                ErrorMessage = "Twitter's API rate limit exceeded";
                            else if (firstError.code == 135)
                                ErrorMessage = "Are the time & date right on your phone?";
                            else
                                ErrorMessage = firstError.message;
                        }
                    }
                    catch (Exception)
                    {
                        ErrorMessage = "twitter error";
                    }
                    return true;
                }

                // failed to deserialise that, try again
                if (errors.Count > 0)
                {
                    var simpleError = JsonConvert.DeserializeObject<TwitterErrorResponseSimple>(content, new JsonSerializerSettings
                                                                                                {
                                                                                                    Error = delegate(object sender, ErrorEventArgs args)
                                                                                                    {
                                                                                                        errors.Add(args.ErrorContext.Error.Message);
                                                                                                        args.ErrorContext.Handled = true;
                                                                                                    }
                                                                                                });

                    if (simpleError != null && !string.IsNullOrEmpty(simpleError.errors))
                    {
                        ErrorMessage = simpleError.errors;
                        return true;
                    }

                }

            }
            catch
            {
                return true;
            }


            return false;
        }


        protected new T DeserialiseResponse<T>(string responseContent)
        {
            if (CheckIsRateLimited(responseContent))
            {
                HasError = true;
                return default(T);
            }

            return base.DeserialiseResponse<T>(responseContent);
        }

        protected new T DeserialiseResponse<T>(RestResponse response)
        {
            return DeserialiseResponse<T>(response.Content);
        }

        private async Task CallClient(RestRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();

            Client.BeginRequest(request, new RestCallback(
                delegate(RestRequest req, RestResponse resp, object userState)
                {
                    tcs.SetResult(true);
                }));

            // wait here until the stuff above has finished.
            await tcs.Task;
        }

        private async Task<T> CallClient<T>(RestRequest request)
        {

            var tcs = new TaskCompletionSource<T>();

            Client.BeginRequest(request, new RestCallback(delegate(RestRequest req, RestResponse resp, object userState)
            {
                try
                {
                    T result = DeserialiseResponse<T>(resp);
                    tcs.SetResult(result);
                }
                catch (Exception)
                {
                    // Dont care
                    tcs.SetResult(default(T));
                }
            }));

            // wait here until the stuff above has finished.
            await tcs.Task;

            return tcs.Task.Result;

        }

        private void SafeRaiseEvent(EventHandler eventCompletedHandler)
        {
            if (eventCompletedHandler != null)
                eventCompletedHandler(this, null);
        }

        #endregion

        public long AccountId { get; private set; }

        private string CurrentScreenName { get; set; }

        public TwitterApi(long id, ExternalSettings settings)
        {

            string tokenSecret = settings.User.AccessTokenSecret;
            string token = settings.User.AccessToken;
            CurrentScreenName = settings.User.ScreenName;

            Credentials = new OAuthCredentials
            {
                Type = OAuthType.ProtectedResource,
                SignatureMethod = OAuthSignatureMethod.HmacSha1,
                ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                ConsumerKey = settings.ConsumerKey,
                ConsumerSecret = settings.ConsumerKeySecret,
                Token = token,
                TokenSecret = tokenSecret
            };

            Client = new RestClient
            {
                Authority = TwitterConstants.BaseApiUrl,
                HasElevatedPermissions = true,
                UserAgent = "mehdoh",
                DecompressionMethods = DecompressionMethods.GZip,
                RetryPolicy = new RetryPolicy() { RetryCount = 3 }
            };

            AccountId = id;

        }

        public TwitterApi(long? id = default(long?))
        {

            HasError = false;

            string tokenSecret = string.Empty;
            string token = string.Empty;

            if (id != null)
            {

                AccountId = id.Value;

                // ignore the caching for external apps

                if (OAuthCache.Cache.ContainsKey(AccountId))
                {
                    var item = OAuthCache.Cache[AccountId];
                    tokenSecret = item.TokenSecret;
                    token = item.Token;
                    CurrentScreenName = item.ScreenName;
                }
                else
                {
                    using (var sh = new StorageHelper())
                    {
                        var user = sh.GetTwitterUser(AccountId);
                        if (user != null)
                        {
                            tokenSecret = user.AccessTokenSecret;
                            token = user.AccessToken;
                            CurrentScreenName = user.ScreenName;
                        }
                    }
                    try
                    {
                        var item = new OAuthCacheTokens()
                                   {
                                       Token = token,
                                       TokenSecret = tokenSecret,
                                       ScreenName = CurrentScreenName
                                   };
                        OAuthCache.Add(AccountId, item);
                    }
                    catch (Exception)
                    {
                    }
                }


            }
            else
            {
#if WP7
                throw new Exception("invalid account id");
#else
                throw new ApplicationException("invalid account id");
#endif
            }

            Credentials = new OAuthCredentials
                              {
                                  Type = OAuthType.ProtectedResource,
                                  SignatureMethod = OAuthSignatureMethod.HmacSha1,
                                  ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                                  ConsumerKey = TwitterConstants.ConsumerKey,
                                  ConsumerSecret = TwitterConstants.ConsumerKeySecret,
                                  Token = token,
                                  TokenSecret = tokenSecret,
                                  Version = TwitterConstants.OAuthVersion
                              };

            Client = new RestClient
                         {
                             Authority = TwitterConstants.BaseApiUrl,
                             HasElevatedPermissions = true,
                             UserAgent = "mehdoh",
                             DecompressionMethods = DecompressionMethods.GZip,
                             RetryPolicy = new RetryPolicy() { RetryCount = 3 },
                             VersionPath = TwitterConstants.OAuthVersion
                         };

        }

        public void GetDirectMessageImage(string targetUrl)
        {

            var request = new RestRequest()
                          {
                              Credentials = Credentials,
                              Method = WebMethod.Get,
                              VersionPath = TwitterConstants.ApiVersion
                          };

            Client.Authority = string.Empty;
            Client.Path = targetUrl;

            Client.BeginRequest(request, GetDirectMessageImageCompleted);

        }

        public event EventHandler GetDirectMessageImageCompletedEvent;

        public byte[] DmImageConent;

        private void GetDirectMessageImageCompleted(RestRequest request, RestResponse response, object userstate)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    response.ContentStream.Seek(0, SeekOrigin.Begin);
                    response.ContentStream.CopyTo(memoryStream);
                    DmImageConent = memoryStream.ToArray();
                }
            }
            finally
            {
                SafeRaiseEvent(GetDirectMessageImageCompletedEvent);
            }

        }

        #region New Tweet

        public async Task PostNewTweet(string tweetText, double latitude, double longitude, string placeId, Stream imageStream)
        {
            await UpdateStatus(tweetText, 0, latitude, longitude, placeId, imageStream);
        }

        public async Task PostNewTweet(string tweetText, double latitude, double longitude, string placeId)
        {
            await UpdateStatus(tweetText, 0, latitude, longitude, placeId, null);
        }

        private async Task UpdateStatus(string tweetText, long replyToId, double latitude, double longitude, string placeId, Stream imageStream)
        {

            string updatePath;

            if (imageStream == null)
                updatePath = "statuses/update.json";
            else
            {
                updatePath = "statuses/update_with_media.json";
            }

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = updatePath,
                                  Method = WebMethod.Post,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("wrap_links", "true");

            if (replyToId > 0)
                request.AddParameter("in_reply_to_status_id", replyToId.ToString(CultureInfo.InvariantCulture));

            if (!Equals(latitude, 0.0) && !Equals(longitude, 0.0))
            {
                request.AddParameter("lat", latitude.ToString(CultureInfo.InvariantCulture));
                request.AddParameter("long", longitude.ToString(CultureInfo.InvariantCulture));
                request.AddParameter("display_coordinates", "true"); // todo: check this is ok?
            }

            if (!string.IsNullOrEmpty(placeId))
                request.AddParameter("place_id", placeId);

            request.AddParameter("status", Uri.EscapeDataString(tweetText));

            // Any file?
            if (imageStream != null)
                request.AddFile("media", "image", imageStream);

            await CallClient(request);

        }

        #endregion


        public async Task<List<ResponseRateLimitItem>> GetRateLimits()
        {

            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "application/rate_limit_status.json",
                Method = WebMethod.Get,
                VersionPath = TwitterConstants.ApiVersion
            };

            var rateLimits = new List<ResponseRateLimitItem>();

            var tcs = new TaskCompletionSource<List<ResponseRateLimitItem>>();

            Client.BeginRequest(request, new RestCallback(
                delegate(RestRequest req, RestResponse resp, object userState)
                {
                    try
                    {
                        var result = JObject.Parse(resp.Content);

                        var resources = result.GetValue("resources");

                        foreach (var val in resources.Values())
                        {
                            foreach (var childVal in val.Values())
                            {
                                var thisItem = childVal.ToObject<ResponseRateLimitItem>();
                                thisItem.title = ((JProperty)childVal.Parent).Name;
                                rateLimits.Add(thisItem);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Dont care
                    }
                    finally
                    {
                        tcs.SetResult(rateLimits);
                    }

                }));

            // wait here until the stuff above has finished.
            await tcs.Task;

            return tcs.Task.Result;

        }


        #region Get Mentions

        public async Task<List<ResponseTweet>> GetMentions(long startId, int? count)
        {
            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "statuses/mentions_timeline.json",
                                  Method = WebMethod.Get,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            if (startId > 0)
                request.AddParameter("since_id", startId.ToString(CultureInfo.InvariantCulture));

            request.AddParameter("contributor_details", "false");
            request.AddParameter("include_entities", "true");

            if (count == null)
                count = ApiFetchCount;

            request.AddParameter("count", count.Value.ToString(CultureInfo.InvariantCulture));

            return await CallClient<List<ResponseTweet>>(request);
        }


        #endregion

        #region Get Old Mentions

        public async Task<List<ResponseTweet>> GetMentionsOld(long maxId)
        {
            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "statuses/mentions_timeline.json",
                                  Method = WebMethod.Get,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("max_id", maxId.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("include_entities", "true");
            request.AddParameter("count", ApiFetchCount.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("contributor_details", "false");

            return await CallClient<List<ResponseTweet>>(request);

        }

        #endregion

        #region Get Old Timeline

        public async Task<List<ResponseTweet>> GetTimelineOld(long maxId, long minId)
        {

            // http://api.twitter.com/1/statuses/home_timeline.json
            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "statuses/home_timeline.json",
                                  Method = WebMethod.Get,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("max_id", maxId.ToString(CultureInfo.InvariantCulture));

            if (minId > 0)
                request.AddParameter("since_id", minId.ToString(CultureInfo.InvariantCulture));

            request.AddParameter("count", ApiFetchCount.ToString(CultureInfo.InvariantCulture));

            request.AddParameter("contributer_details", "true");
            request.AddParameter("include_rts", "true");
            request.AddParameter("include_entities", "true");

            request.CacheOptions = new CacheOptions
                                       {
                                           Duration = new TimeSpan(0, 0, 0, 0),
                                           Mode = CacheMode.AbsoluteExpiration
                                       };

            return await CallClient<List<ResponseTweet>>(request);

        }

        #endregion

        #region Get Timeline

        public async Task<List<ResponseTweet>> GetTimeline(long startId, int? count)
        {

            // http://api.twitter.com/1/statuses/home_timeline.json
            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "statuses/home_timeline.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            if (count == null)
                count = ApiFetchCount;

            request.AddParameter("count", count.Value.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("contributer_details", "false");
            request.AddParameter("include_rts", "true");
            request.AddParameter("include_entities", "true");
            //request.AddParameter("trim_user", "true");

            request.CacheOptions = new CacheOptions
                                       {
                                           Duration = new TimeSpan(0, 0, 0, 0),
                                           Mode = CacheMode.AbsoluteExpiration
                                       };

            if (startId > 0)
                request.AddParameter("since_id", startId.ToString(CultureInfo.InvariantCulture));

            return await CallClient<List<ResponseTweet>>(request);

        }

        #endregion

        #region Get User Profile

        public Task<ResponseGetUserProfile> GetUserProfile(TwitterAccess user)
        {
            return GetUserProfile(user.ScreenName, 0);
        }

        public Task<ResponseGetUserProfile> GetUserProfile(string screenName, long userId)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "users/show.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            if (!string.IsNullOrEmpty(screenName))
                request.AddParameter("screen_name", screenName);

            if (userId > 0)
                request.AddParameter("user_id", userId.ToString(CultureInfo.InvariantCulture));

            return CallClient<ResponseGetUserProfile>(request);

        }

        #endregion

        #region Reply To Tweet

        public async Task ReplyToTweet(string tweetText, long replyToId, double latitude, double longitude, string placeId)
        {
            await UpdateStatus(tweetText, replyToId, latitude, longitude, placeId, null);
        }

        public async Task ReplyToTweet(string tweetText, long replyToId, double latitude, double longitude, string placeId, Stream imageStream)
        {
            await UpdateStatus(tweetText, replyToId, latitude, longitude, placeId, imageStream);
        }

        #endregion

        #region Retweet

        public event EventHandler RetweetCompletedEvent;

        public ResponseRetweetClass RetweetResponse { get; set; }

        public void Retweet(long statusId)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "statuses/retweet/" + statusId + ".json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            Client.BeginRequest(request, new RestCallback(RetweetCompleted));

        }

        private void RetweetCompleted(RestRequest request, RestResponse response, object userstate)
        {

            try
            {
                RetweetResponse = DeserialiseResponse<ResponseRetweetClass>(response);
            }
            finally
            {
                SafeRaiseEvent(RetweetCompletedEvent);
            }

        }

        #endregion

        #region Follow User

        public async Task FollowUser(string screenName)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "friendships/create.json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("include_entities", "true");
            request.AddParameter("screen_name", screenName);

            await CallClient(request);

        }

        #endregion

        #region Unfollow User

        public async Task UnfollowUser(string screenName)
        {


            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "friendships/destroy.json",
                Method = WebMethod.Post,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("screen_name", screenName);

            await CallClient(request);
        }


        #endregion

        #region Favourites

        public async Task<List<ResponseTweet>> GetFavourites(long sinceId, long? maxId = 0, string screenName = null)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "favorites/list.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            if (sinceId > 0)
                request.AddParameter("since_id", sinceId.ToString(CultureInfo.InvariantCulture));

            if (maxId.HasValue && maxId > 0)
                request.AddParameter("max_id", maxId.Value.ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrEmpty(screenName))
                request.AddParameter("screen_name", screenName);

            request.AddParameter("include_entities", "true");

            // Client.BeginRequest(request, new RestCallback(GetFavouritesCompleted));
            return await CallClient<List<ResponseTweet>>(request);

        }

        #endregion

        #region Favourite

        public async Task FavouriteTweet(long id)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "favorites/create.json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("id", id.ToString());

            await CallClient(request);

        }

        #endregion

        #region Unfavourite

        public async Task<ResponseUnfavourite> UnfavouriteTweet(long id)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "favorites/destroy.json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("id", id.ToString());

            return await CallClient<ResponseUnfavourite>(request);

        }

        #endregion

        #region Get Tweet

        public async Task<ResponseTweet> GetTweet(long id)
        {


            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "statuses/show.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("id", id.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("include_entities", "true");

            return await CallClient<ResponseTweet>(request);

        }

        public TimelineTable IsCachedTweet(long id)
        {

            using (var dh = new MainDataContext())
            {

                var dataLoadOptions = new DataLoadOptions();
                dataLoadOptions.LoadWith<TimelineTable>(x => x.Assets);
                dh.LoadOptions = dataLoadOptions;

                var timelineTweet = from timeline in dh.Timeline
                                    where timeline.Id == id
                                    select timeline;

                return timelineTweet.FirstOrDefault();

            }

        }

        #endregion

        #region Get Direct Message

        public event EventHandler GetDirectMessageCompletedEvent;

        public ResponseTweet DirectMessage { get; private set; }

        public void GetDirectMessage(long id)
        {
            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "direct_messages/show.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("id", id.ToString());

            Client.BeginRequest(request, new RestCallback(GetDirectMessageCompleted));

        }

        public void GetDirectMessageCompleted(RestRequest request, RestResponse response, object userstate)
        {

            try
            {
                DirectMessage = DeserialiseResponse<ResponseTweet>(response);
            }
            finally
            {
                SafeRaiseEvent(GetDirectMessageCompletedEvent);
            }

        }

        #endregion

        #region Direct Messages

        public async Task<List<ResponseDirectMessage>> GetDirectMessages(long sinceId, int? count)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "direct_messages.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            if (sinceId > 0)
                request.AddParameter("since_id", sinceId.ToString(CultureInfo.InvariantCulture));

            if (count == null)
                count = ApiFetchCount;

            request.AddParameter("count", count.Value.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("include_entities", "true");

            return await CallClient<List<ResponseDirectMessage>>(request);

        }

        #endregion

        #region Send Direct message

        public async Task SendNewMessage(string replyToAuthor, string text)
        {

            text = text.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "direct_messages/new.json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("screen_name", replyToAuthor);
            request.AddParameter("text", Uri.EscapeDataString(text));

            // Client.BeginRequest(request, new RestCallback(SendNewMessageCompleted));
            await CallClient(request);

        }

        #endregion

        #region Public Timeline

        public async Task<List<ResponseTweet>> GetPublicTimeline(string screenName, long maxId)
        {
            // statuses/public_timeline

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "statuses/user_timeline.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("screen_name", screenName);

            if (maxId > 0)
                request.AddParameter("max_id", maxId.ToString(CultureInfo.InvariantCulture));

            return await CallClient<List<ResponseTweet>>(request);

        }

        #endregion

        #region Search

        public async Task<ResponseSearch> SearchForReplies(string userName, long? sinceId = null, long? newestId = null)
        {

            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "search/tweets.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            if (!userName.StartsWith("@"))
                userName = "@" + userName;

            request.AddParameter("q", userName);

            if (sinceId.HasValue)
                request.AddParameter("since_id", sinceId.Value.ToString(CultureInfo.InvariantCulture));

            if (newestId.HasValue)
                request.AddParameter("max_id", newestId.Value.ToString(CultureInfo.InvariantCulture));

            request.AddParameter("include_entities", "false");
            request.AddParameter("count", "100");
            request.AddParameter("result_type", "recent");

            return await CallClient<ResponseSearch>(request);

        }

        public async Task<ResponseSearch> Search(string latitude, string longitude, int distance, long maxId)
        {

            if (string.IsNullOrEmpty(latitude))
            {
                return null;
            }

            if (string.IsNullOrEmpty(longitude))
            {
                return null;
            }

            // Different base uri
            // search.twitter.com/search.json

            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "search/tweets.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            string location = string.Format("{0},{1},{2}mi", latitude.ToString(CultureInfo.InvariantCulture),
                                                             longitude.ToString(CultureInfo.InvariantCulture),
                                                             distance.ToString(CultureInfo.InvariantCulture));

            request.AddParameter("q", string.Empty);
            request.AddParameter("geocode", location);

            if (maxId != 0)
                request.AddParameter("max_id", maxId.ToString(CultureInfo.InvariantCulture));

            return await CallClient<ResponseSearch>(request);

        }

        public async Task<ResponseSearch> Search(string query, long maxId, long sinceId, bool includeEntities)
        {

            // Different base uri
            // search.twitter.com/search.json

            SearchQuery = query;

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "search/tweets.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("q", Uri.EscapeDataString(query));

            // the range goes since -> max ... so if since is greater than max, then we need to swap.
            if (sinceId > 0 && maxId > 0)
            {
                if (sinceId > maxId)
                {
                    // swap em
                    var temp = maxId;
                    maxId = sinceId;
                    sinceId = temp;
                }
            }

            if (maxId > 0)
                request.AddParameter("max_id", (maxId - 1).ToString(CultureInfo.InvariantCulture));

            if (sinceId > 0)
                request.AddParameter("since_id", (sinceId - 1).ToString(CultureInfo.InvariantCulture));

            if (includeEntities)
                request.AddParameter("include_entities", "true");

            return await CallClient<ResponseSearch>(request);

        }

        #endregion

        #region Get Followers

        public void GetFollowers(string screenName, string currentCursor)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "followers/ids.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("screen_name", screenName);
            request.AddParameter("cursor", currentCursor);

            Client.BeginRequest(request, new RestCallback(GetFollowersCompleted));

        }

        private void GetFollowersCompleted(RestRequest request, RestResponse response, object userstate)
        {

            try
            {

                FollowerIds = new List<long>();

                var res = DeserialiseResponse<ResponseGetFriends>(response);

                if (res != null && res.ids != null && res.ids.Any())
                {
                    FollowerIds.AddRange(res.ids);
                }

            }
            finally
            {
                SafeRaiseEvent(GetFollowersCompletedEvent);
            }

        }

        public List<long> FollowerIds;

        public event EventHandler GetFollowersCompletedEvent;

        #endregion

        #region Get Friends

        public void GetFriends(string screenName, string cursor)
        {

            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "friends/ids.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("screen_name", screenName);
            request.AddParameter("cursor", cursor);

            Client.BeginRequest(request, new RestCallback(GetFriendsCompleted));

        }


        public void GetFriends(string screenName)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "friends/ids.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("screen_name", screenName);
            request.AddParameter("cursor", "-1");

            Client.BeginRequest(request, new RestCallback(GetFriendsCompleted));

        }

        public List<long> FriendIds;

        public event EventHandler GetFriendsCompletedEvent;

        public void GetFriendsCompleted(RestRequest request, RestResponse response, object userstate)
        {

            try
            {

                FriendIds = new List<long>();

                var res = DeserialiseResponse<ResponseGetFriends>(response);

                if (res != null && res.ids != null && res.ids.Any())
                {
                    FriendIds.AddRange(res.ids);
                }

            }
            finally
            {
                SafeRaiseEvent(GetFriendsCompletedEvent);
            }

        }

        #endregion

        #region Users Lookups

        public event EventHandler<UserLookupEventArgs> UsersLookupCompletedEvent;

        public void UsersLookup(List<long> ids)
        {

            // users/lookup
            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "users/lookup.json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            var userIds = string.Join(",", ids);

            request.AddParameter("user_id", userIds);
            request.AddParameter("include_entities", "false");

            Client.BeginRequest(request, new RestCallback(UsersLookupCompleted));

        }


        public void UsersLookupCompleted(RestRequest request, RestResponse response, object userstate)
        {


            List<ResponseGetUserProfile> res = null;

            try
            {
                res = DeserialiseResponse<List<ResponseGetUserProfile>>(response);
            }
            finally
            {
                if (UsersLookupCompletedEvent != null)
                {
                    if (res != null)
                    {
                        var args = new UserLookupEventArgs()
                                       {
                                           UserProfiles = res
                                       };
                        UsersLookupCompletedEvent(this, args);
                    }
                    else
                    {
                        UsersLookupCompletedEvent(this, null);
                    }
                }
            }

        }

        #endregion

        #region Report Spam

        public void ReportSpam(string screenName)
        {

            // report_spam
            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "report_spam.json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("screen_name", screenName);

            Client.BeginRequest(request, new RestCallback(ReportSpamCompleted));


        }

        public event EventHandler ReportSpamCompletedEvent;

        private void ReportSpamCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (ReportSpamCompletedEvent != null)
                ReportSpamCompletedEvent(this, null);

        }

        #endregion

        #region Block User

        public async Task BlockUser(string screenName)
        {

            // users/lookup
            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "blocks/create.json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("screen_name", screenName);

            await CallClient(request);
        }

        #endregion

        #region Delete Tweet

        public event EventHandler DeleteTweetedCompletedEvent;

        public void DeleteTweet(long id)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "statuses/destroy/" + id + ".json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            Client.BeginRequest(request, new RestCallback(DeleteTweetCompleted));

        }

        private void DeleteTweetCompleted(RestRequest request, RestResponse response, object userState)
        {

            if (DeleteTweetedCompletedEvent != null)
                DeleteTweetedCompletedEvent(this, null);

        }

        #endregion

        #region Delete Direct Message

        public void DeleteDirectMessage(long id)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "direct_messages/destroy.json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("id", id.ToString(CultureInfo.InvariantCulture));

            Client.BeginRequest(request, new RestCallback(DeleteDirectMessageCompleted));

        }

        public event EventHandler DeleteDirectMessageCompletedEvent;

        private void DeleteDirectMessageCompleted(RestRequest request, RestResponse response, object userstate)
        {
            if (DeleteDirectMessageCompletedEvent != null)
                DeleteDirectMessageCompletedEvent(this, null);
        }


        #endregion

        #region Get Direct Messages

        public async Task<List<ResponseGetSentDirectMessage>> GetSentDirectMessages(long sinceId)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "direct_messages/sent.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            if (sinceId > 0)
                request.AddParameter("since_id", sinceId.ToString(CultureInfo.InvariantCulture));

            return await CallClient<List<ResponseGetSentDirectMessage>>(request);

        }

        #endregion

        #region Account Settings

        public async Task<ResponseAccountSettings> GetAccountSettings()
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "account/settings.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            return await CallClient<ResponseAccountSettings>(request);

        }

        #endregion

        #region Reverse Geolookup

        public void ReverseGeocode(double latitude, double longitude)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "geo/reverse_geocode.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("lat", latitude.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("long", longitude.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("max_results", "1");

            Client.BeginRequest(request, new RestCallback(ReverseGeocodeCompleted));

        }

        public event EventHandler ReverseGeocodeCompletedEvent;

        public ResponseGeocode Geocode { get; set; }

        private void ReverseGeocodeCompleted(RestRequest request, RestResponse response, object userstate)
        {

            try
            {
                Geocode = DeserialiseResponse<ResponseGeocode>(response);
            }
            finally
            {
                SafeRaiseEvent(ReverseGeocodeCompletedEvent);
            }

        }

        #endregion

        #region Get Available Trend Locations

        public void GetAvailableTrendLocations()
        {
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "trends/available.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            Client.BeginRequest(request, new RestCallback(GetAvailableTrendLocationsCompleted));

        }

        public event EventHandler GetAvailableTrendLocationsCompletedEvent;

        public List<TrendLocation> AvailableTrendLocation { get; set; }

        private void GetAvailableTrendLocationsCompleted(RestRequest request, RestResponse response, object userstate)
        {

            try
            {
                AvailableTrendLocation = JsonConvert.DeserializeObject<List<TrendLocation>>(response.Content);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("GetAvailableTrendLocationsCompleted", ex);
            }
            finally
            {
                SafeRaiseEvent(GetAvailableTrendLocationsCompletedEvent);
            }

        }



        #endregion

        #region Get Closest Trends

        public void GetClosestTrendLocation(double longitude, double latitude)
        {
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "trends/closest.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("lat", latitude.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("long", longitude.ToString(CultureInfo.InvariantCulture));

            Client.BeginRequest(request, new RestCallback(GetClosestTrendLocationCompleted));

        }

        public event EventHandler GetClosestTrendLocationCompletedEvent;

        public List<TrendLocation> ClosestTrendLocation { get; set; }

        private void GetClosestTrendLocationCompleted(RestRequest request, RestResponse response, object userstate)
        {

            try
            {

                ClosestTrendLocation = JsonConvert.DeserializeObject<List<TrendLocation>>(response.Content);

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("GetClosestTrendLocationCompleted", ex);
            }
            finally
            {
                SafeRaiseEvent(GetClosestTrendLocationCompletedEvent);
            }

        }

        #endregion

        #region Get Trends by WOEID

        public void GetCurrentTrendsByWoeid(long woeId)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "trends/place.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("id", woeId.ToString());

            Client.BeginRequest(request, new RestCallback(GetCurrentTrendsByWoeidCompleted));

        }

        public List<ResponseTrend> CurrentTrends { get; set; }

        public event EventHandler GetCurrentTrendsByWoeidCompletedEvent;

        private void GetCurrentTrendsByWoeidCompleted(RestRequest request, RestResponse response, object userstate)
        {

            try
            {
                var content = JArray.Parse(response.Content);

                var trends = content[0]["trends"];

                var newContent = JArray.Parse(trends.ToString()).ToList();

                CurrentTrends = new List<ResponseTrend>();
                foreach (var token in newContent)
                {
                    var trend = DeserialiseResponse<ResponseTrend>(token.ToString());
                    CurrentTrends.Add(trend);
                }

            }
            catch
            {
                HasError = true;
                ErrorMessage = "Twitter's rate limit exceeded";
            }
            finally
            {
                SafeRaiseEvent(GetCurrentTrendsByWoeidCompletedEvent);
            }

        }

        #endregion

        #region Update Profile Photo

        public event EventHandler UpdateProfilePhotoCompletedEvent;

        public void UpdateProfilePhoto(Stream chosenPhoto, string filePath)
        {
            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "account/update_profile_image.json",
                                  Method = WebMethod.Post,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            var fileName = Path.GetFileName(filePath);

            chosenPhoto.Seek(0, SeekOrigin.Begin);
            request.AddFile("image", fileName, chosenPhoto);

            request.AddParameter("include_entities", "false");
            request.AddParameter("skip_status", "false");

            Client.BeginRequest(request, new RestCallback(UpdateProfilePhotoCompleted));

        }

        protected void UpdateProfilePhotoCompleted(RestRequest request, RestResponse response, object userstate)
        {

            HasError = false;

            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
            }

            if (UpdateProfilePhotoCompletedEvent != null)
                UpdateProfilePhotoCompletedEvent(this, null);
        }

        #endregion

        #region Friendship

        public async Task<ResponseGetFriendship> GetFriendship(string sourceScreeName, string targetScreenName)
        {

            // http://api.twitter.com/1/friendships/show.format

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "friendships/show.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("source_screen_name", sourceScreeName);
            request.AddParameter("target_screen_name", targetScreenName);

            //Client.BeginRequest(request, new RestCallback(GetFriendshipCompleted));
            return await CallClient<ResponseGetFriendship>(request);

        }

        #endregion

        #region Test

        public void PerformTest()
        {

            // http://api.twitter.com/1/help/test.format

            var request = new RestRequest
                              {
                                  Path = "help/test.json",
                                  Method = WebMethod.Head
                              };

            Client.BeginRequest(request, new RestCallback(PerformTestCompleted));


        }

        public event EventHandler PerformTestCompletedEvent;

        private void PerformTestCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (PerformTestCompletedEvent != null)
                PerformTestCompletedEvent(this, null);

        }

        #endregion

        #region Saved Searches - Fetch

        public async Task<List<ResponseGetSavedSearch>> GetSavedSearches()
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "saved_searches/list.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            return await CallClient<List<ResponseGetSavedSearch>>(request);

        }

        #endregion

        #region Delete Saved Search

        public event EventHandler DeleteSavedSearchCompletedEvent;

        public void DeleteSavedSearch(long savedQueryId)
        {

            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "saved_searches/destroy/" + savedQueryId.ToString(CultureInfo.InvariantCulture) + ".json",
                Method = WebMethod.Post,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            Client.BeginRequest(request, new RestCallback(DeleteSavedSearchCompleted));


        }

        private void DeleteSavedSearchCompleted(RestRequest request, RestResponse response, object userstate)
        {
            SafeRaiseEvent(DeleteSavedSearchCompletedEvent);
        }

        #endregion

        #region Save Search

        public event EventHandler SaveSearchCompletedEvent;

        public void SaveSearch(string query)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "saved_searches/create.json",
                                  Method = WebMethod.Post,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("query", HttpUtility.UrlPathEncode(query));

            Client.BeginRequest(request, new RestCallback(SaveSearchCompleted));

        }

        private void SaveSearchCompleted(RestRequest request, RestResponse response, object userstate)
        {
            SafeRaiseEvent(SaveSearchCompletedEvent);
        }

        #endregion

        #region Get Users Own Lists

        public void GetUsersOwnLists()
        {

            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "lists/ownerships.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("user_id", AccountId.ToString(CultureInfo.InvariantCulture));

            Client.BeginRequest(request, new RestCallback(GetUsersOwnListsCompleted));

        }

        public event EventHandler GetUsersOwnListsCompletedEvent;

        public ResponseGetUserOwnLists GetUsersOwnList { get; set; }

        private void GetUsersOwnListsCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
                SafeRaiseEvent(GetUsersOwnListsCompletedEvent);
                return;
            }

            try
            {
                GetUsersOwnList = DeserialiseResponse<ResponseGetUserOwnLists>(response);
            }
            finally
            {
                SafeRaiseEvent(GetUsersOwnListsCompletedEvent);
            }


        }

        #endregion

        #region Get Lists - ALL lists, even other users lists, that the user subscribes to

        public async Task<List<ResponseGetUsersList>> GetUsersLists()
        {
            return await GetUsersLists(string.Empty);
        }

        public async Task<List<ResponseGetUsersList>> GetUsersLists(string screenName)
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "lists/list.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            if (!string.IsNullOrWhiteSpace(screenName))
                request.AddParameter("screen_name", screenName);

            return await CallClient<List<ResponseGetUsersList>>(request);

        }


        #endregion

        #region GetListStatus

        public async Task<List<ResponseTweet>> GetListStatuses(string listId, long oldestId, long newestId)
        {

            // https://api.twitter.com/1/lists/statuses.format
            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "lists/statuses.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            request.AddParameter("list_id", listId);
            request.AddParameter("per_page", ApiFetchCount.ToString(CultureInfo.InvariantCulture));
            //request.AddParameter("page", currentPage.ToString());

            if (oldestId > 0)
                request.AddParameter("since_id", oldestId.ToString(CultureInfo.InvariantCulture));

            if (newestId > 0)
                request.AddParameter("max_id", newestId.ToString(CultureInfo.InvariantCulture));

            request.AddParameter("include_entities", "true");

            // listSlug = listId;

            // Client.BeginRequest(request, new RestCallback(GetListStatusesCompleted));

            return await CallClient<List<ResponseTweet>>(request);

        }

        //public event EventHandler GetListStatusesCompletedEvent;

        //public List<ResponseTweet> ListStatuses;
        //public string ListSlug;

        //private void GetListStatusesCompleted(RestRequest request, RestResponse response, object userstate)
        //{

        //    try
        //    {
        //        ListStatuses = DeserialiseResponse<List<ResponseTweet>>(response);
        //    }
        //    catch (Exception)
        //    {
        //        // Dont care
        //    }
        //    finally
        //    {
        //        SafeRaiseEvent(GetListStatusesCompletedEvent);
        //    }

        //}

        #endregion

        #region Suggested Users Categories

        public void GetSuggestedUserCategories()
        {

            var request = new RestRequest
                              {
                                  Credentials = Credentials,
                                  Path = "users/suggestions.json",
                                  Method = WebMethod.Get,
                                  DecompressionMethods = DecompressionMethods.GZip,
                                  VersionPath = TwitterConstants.ApiVersion
                              };

            Client.BeginRequest(request, new RestCallback(GetSuggestedUserCategoriesCompleted));

        }

        public event EventHandler GetSuggestedUserCategoriesCompletedEvent;


        public List<ResponseGetSuggestedUserCategory> SuggestedUserCategories;

        private void GetSuggestedUserCategoriesCompleted(RestRequest request, RestResponse response, object userstate)
        {

            try
            {

                if (response.InnerException != null || CheckIsRateLimited(response))
                {
                    HasError = true;
                    return;
                }

                SuggestedUserCategories = JsonConvert.DeserializeObject<List<ResponseGetSuggestedUserCategory>>(response.Content);

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("GetSuggestedUserCategoriesCompleted", ex);
            }
            finally
            {
                SafeRaiseEvent(GetSuggestedUserCategoriesCompletedEvent);
            }

        }

        #endregion

        #region Suggested Users

        public void GetSuggestedUsers(string slug)
        {

            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "users/suggestions/" + slug + ".json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            Client.BeginRequest(request, new RestCallback(GetSuggestedUsersCompleted));

        }

        public event EventHandler GetSuggestedUsersCompletedEvent;

        public ResponseGetSuggestedUser SuggestedUsers;

        protected void GetSuggestedUsersCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
                SafeRaiseEvent(GetSuggestedUsersCompletedEvent);
                return;
            }

            try
            {

                SuggestedUsers = DeserialiseResponse<ResponseGetSuggestedUser>(response);
            }
            catch (Exception)
            {
                // ignore                 
            }
            finally
            {
                SafeRaiseEvent(GetSuggestedUsersCompletedEvent);
            }

        }

        #endregion

        #region Retweets Of Me

        public void GetRetweetsOfMe(long sinceId, long maxId)
        {

            // http://api.twitter.com/1/statuses/retweets_of_me.json
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "statuses/retweets_of_me.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("count", ApiFetchCount.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("contributer_details", "false");
            request.AddParameter("include_rts", "true");
            request.AddParameter("include_entities", "true");
            //request.AddParameter("trim_user", "true");

            request.CacheOptions = new CacheOptions
            {
                Duration = new TimeSpan(0, 0, 0, 0),
                Mode = CacheMode.AbsoluteExpiration
            };

            if (sinceId > 0)
                request.AddParameter("since_id", sinceId.ToString(CultureInfo.InvariantCulture));

            if (maxId > 0)
                request.AddParameter("max_id", maxId.ToString(CultureInfo.InvariantCulture));

            Client.BeginRequest(request, new RestCallback(GetRetweetsOfMeCompleted));


        }

        public event EventHandler GetRetweetsOfMeCompletedEvent;

        public List<ResponseTweet> RetweetsOfMe { get; set; }

        private void GetRetweetsOfMeCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
                SafeRaiseEvent(GetRetweetsOfMeCompletedEvent);
                return;
            }

            try
            {
                RetweetsOfMe = DeserialiseResponse<List<ResponseTweet>>(response);
            }
            catch (Exception)
            {
                // Dont care
            }
            finally
            {
                SafeRaiseEvent(GetRetweetsOfMeCompletedEvent);
            }


        }

        #endregion

        #region Retweets By Me

        public void GetRetweetsByMe(long sinceId, long maxId)
        {

            // http://api.twitter.com/1/statuses/retweeted_by_me.json
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "statuses/retweeted_by_me.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("count", ApiFetchCount.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("contributer_details", "false");
            request.AddParameter("include_rts", "true");
            request.AddParameter("include_entities", "true");
            //request.AddParameter("trim_user", "true");

            request.CacheOptions = new CacheOptions
            {
                Duration = new TimeSpan(0, 0, 0, 0),
                Mode = CacheMode.AbsoluteExpiration
            };

            if (sinceId > 0)
                request.AddParameter("since_id", sinceId.ToString(CultureInfo.InvariantCulture));

            if (maxId > 0)
                request.AddParameter("max_id", maxId.ToString(CultureInfo.InvariantCulture));

            Client.BeginRequest(request, new RestCallback(GetRetweetsByMeCompleted));

        }

        public event EventHandler GetRetweetsByMeCompletedEvent;

        public List<ResponseTweet> RetweetsByMe { get; set; }

        private void GetRetweetsByMeCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
                SafeRaiseEvent(GetRetweetsByMeCompletedEvent);
                return;
            }

            try
            {
                RetweetsByMe = DeserialiseResponse<List<ResponseTweet>>(response);
            }
            catch (Exception)
            {
                // Dont care
            }
            finally
            {
                SafeRaiseEvent(GetRetweetsByMeCompletedEvent);
            }


        }

        #endregion

        #region Retweets To Me

        public void GetRetweetsToMe(long sinceId, long maxId)
        {

            // http://api.twitter.com/1/statuses/retweeted_by_me.json
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "statuses/retweeted_to_me.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("count", ApiFetchCount.ToString(CultureInfo.InvariantCulture));
            request.AddParameter("contributer_details", "false");
            request.AddParameter("include_rts", "true");
            request.AddParameter("include_entities", "true");
            //request.AddParameter("trim_user", "true");

            request.CacheOptions = new CacheOptions
            {
                Duration = new TimeSpan(0, 0, 0, 0),
                Mode = CacheMode.AbsoluteExpiration
            };

            if (sinceId > 0)
                request.AddParameter("since_id", sinceId.ToString(CultureInfo.InvariantCulture));

            if (maxId > 0)
                request.AddParameter("max_id", maxId.ToString(CultureInfo.InvariantCulture));

            Client.BeginRequest(request, new RestCallback(GetRetweetsToMeCompleted));

        }

        public event EventHandler GetRetweetsToMeCompletedEvent;

        public List<ResponseTweet> RetweetsToMe { get; set; }

        private void GetRetweetsToMeCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
                SafeRaiseEvent(GetRetweetsToMeCompletedEvent);
                return;
            }

            try
            {
                RetweetsToMe = DeserialiseResponse<List<ResponseTweet>>(response);
            }
            catch (Exception)
            {
                // Dont care
            }
            finally
            {
                SafeRaiseEvent(GetRetweetsToMeCompletedEvent);
            }


        }

        #endregion

        #region GetUsersWhoRetweetedTweet

        public void GetUsersWhoRetweetedTweet(long tweetId, long page)
        {
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = string.Format("statuses/retweets/{0}.json", tweetId),
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("count", "100");
            request.AddParameter("trim_user", "false");

            Client.BeginRequest(request, new RestCallback(GetUsersWhoRetweetedTweetCompleted));

        }

        public event EventHandler GetUsersWhoRetweetedTweetCompletedEvent;

        public List<UserClass> RetweetUsers;

        private void GetUsersWhoRetweetedTweetCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
                SafeRaiseEvent(GetUsersWhoRetweetedTweetCompletedEvent);
                return;
            }

            try
            {
                var tweets = DeserialiseResponse<List<ResponseTweet>>(response);
                RetweetUsers = tweets.Select(x => x.user).ToList();
            }
            finally
            {
                SafeRaiseEvent(GetUsersWhoRetweetedTweetCompletedEvent);
            }

        }

        #endregion

        #region lists/memberships

        public void GetListsMemberships(string screenName, long nextCursor)
        {

            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = string.Format("lists/memberships.json"),
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("screen_name", screenName);
            request.AddParameter("cursor", nextCursor.ToString(CultureInfo.InvariantCulture));

            Client.BeginRequest(request, new RestCallback(GetListsMembershipsCompleted));

        }

        public event EventHandler GetListsMembershipsCompletedEvent;


        public ResponseGetListMembership ListMembership { get; set; }

        private void GetListsMembershipsCompleted(RestRequest request, RestResponse response, object userstate)
        {

            try
            {
                ListMembership = DeserialiseResponse<ResponseGetListMembership>(response);
            }
            finally
            {
                SafeRaiseEvent(GetListsMembershipsCompletedEvent);
            }


        }

        #endregion

        #region Subscribe to List

        public void SubscribeToList(string listId)
        {

            // lists/subscribers/create
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "lists/subscribers/create.json",
                Method = WebMethod.Post,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("list_id", listId);

            Client.BeginRequest(request, new RestCallback(SubscribeToListCompleted));

        }

        public event EventHandler SubscribeToListCompletedEvent;

        private void SubscribeToListCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
            }

            SafeRaiseEvent(SubscribeToListCompletedEvent);

        }

        #endregion

        #region Check List Membership

        public void CheckListMembership(string listId, string userScreen, long ownerAccountId)
        {

            // lists/subscribers/create
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "lists/members/show.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("list_id", listId);
            request.AddParameter("screen_name", userScreen);
            request.AddParameter("include_entities", "0");
            request.AddParameter("skip_status", "1");
            request.AddParameter("owner_id", ownerAccountId.ToString());

            Client.BeginRequest(request, CheckListMembershipCompleted);

        }

        public event EventHandler CheckListMembershipCompletedEvent;

        private void CheckListMembershipCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
            }

            SafeRaiseEvent(CheckListMembershipCompletedEvent);

        }

        #endregion

        #region Add User To List

        public void AddUserToList(string listId, string screenName)
        {
            // lists/members/create
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "lists/members/create.json",
                Method = WebMethod.Post,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("list_id", listId);
            request.AddParameter("screen_name", screenName);

            Client.BeginRequest(request, new RestCallback(AddUserToListCompleted));
        }

        public event EventHandler AddUserToListCompletedEvent;

        private void AddUserToListCompleted(RestRequest request, RestResponse response, object userstate)
        {

            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
            }

            SafeRaiseEvent(AddUserToListCompletedEvent);
        }

        #endregion

        #region Remove User From List

        public void RemoveUserFromList(string listId, string screenName)
        {
            // lists/members/create
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "lists/members/destroy.json",
                Method = WebMethod.Post,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            request.AddParameter("list_id", listId);
            request.AddParameter("screen_name", screenName);

            Client.BeginRequest(request, new RestCallback(RemoveUserFromListCompleted));
        }

        public event EventHandler RemoveUserFromListCompletedEvent;

        private void RemoveUserFromListCompleted(RestRequest request, RestResponse response, object userstate)
        {
            if (response.InnerException != null || CheckIsRateLimited(response))
            {
                HasError = true;
            }

            SafeRaiseEvent(RemoveUserFromListCompletedEvent);
        }

        #endregion

        #region Streaming

        public void StartStreaming()
        {

            // https://userstream.twitter.com/2/user.json

            var request = new RestRequest
            {
                Credentials = Credentials,
                Method = WebMethod.Get,
                Path = "user.json",
            };

            var streamOptions = new StreamOptions()
            {
                Duration = new TimeSpan(7, 0, 0, 0),
                ResultsPerCallback = 1
            };

            request.StreamOptions = streamOptions;

            request.AddParameter("with", "followings");
            //request.AddParameter("replies", "all"); // this shows everything!
            request.AddHeader("X-User-Agent", "mehdoh");

            // no zip
            Client.DecompressionMethods = null;
            Client.Authority = "https://userstream.twitter.com";
            Client.VersionPath = "2";
            Client.SilverlightUserAgentHeader = "mehdoh";

            Client.BeginRequest<object>(request, StreamCompletedEvent);

            //Client.BeginRequest(request, StreamCompletedEvent);

        }

        public event EventHandler StreamingEvent;

        public object StreamingResult;

        private StreamingResponseFriends StreamingFriendIds { get; set; }

        public string SearchQuery { get; set; }

        private void StreamCompletedEvent(RestRequest request, RestResponse<object> response, object userState)
        {

            try
            {
                if (CheckIsRateLimited(response.Content))
                    return;

                if (string.IsNullOrWhiteSpace(response.Content))
                    return;

                if (response.Content.StartsWith("<html"))
                {
                    StopStreaming();
                    StartStreaming();
                    return;
                }

                // right, this could be anything

                var errors = new List<string>();

                if (response.Content.StartsWith("MEHDOH_EOF_MAGIC_123123"))
                {

                    StreamingResult = new StreamResponseEOF();
                    SafeRaiseEvent(StreamingEvent);
                    return;
                }

                // friends list?
                if (response.Content.StartsWith("{\"friends\":"))
                {
                    StreamingFriendIds = JsonConvert.DeserializeObject<StreamingResponseFriends>(response.Content,
                                                                                                new JsonSerializerSettings
                                                                                                {
                                                                                                    Error = delegate(object sender, ErrorEventArgs args)
                                                                                                    {
                                                                                                        errors.Add(args.ErrorContext.Error.Message);
                                                                                                        args.ErrorContext.Handled = true;
                                                                                                    }
                                                                                                });
                    // dont raise event yet
                    return;
                }

                // tweet create
                var timelineTweet = JsonConvert.DeserializeObject<ResponseTweet>(response.Content,
                                                                                        new JsonSerializerSettings

                                                                                        {
                                                                                            Error = delegate(object sender, ErrorEventArgs args)
                                                                                                {
                                                                                                    errors.Add(args.ErrorContext.Error.Message);
                                                                                                    args.ErrorContext.Handled = true;
                                                                                                }
                                                                                        });
                if (errors.Count == 0 && timelineTweet.id != 0)
                {

                    bool isCurrentUser = string.Compare(timelineTweet.user.screen_name, CurrentScreenName, StringComparison.InvariantCultureIgnoreCase) == 0;

                    //else
                    //{
                    //    if (timelineTweet.retweeted_status != null && timelineTweet.retweeted_status.user != null)
                    //    {
                    //        if (string.Compare(timelineTweet.retweeted_status.user.screen_name, CurrentScreenName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    //    }
                    //}

                    if (StreamingFriendIds.friends.Contains(timelineTweet.user.id_str) || isCurrentUser)
                        timelineTweet.IsTimelineTweet = true;
                    else
                        timelineTweet.IsTimelineTweet = false;

                    // mention?
                    if (timelineTweet.retweeted_status == null)
                    {
                        if (timelineTweet.entities != null && timelineTweet.entities.user_mentions != null && timelineTweet.entities.user_mentions.Any(x => string.Compare(x.screen_name, CurrentScreenName, StringComparison.InvariantCultureIgnoreCase) == 0))
                        {
                            timelineTweet.IsMentionTweet = true;
                        }
                    }

                    StreamingResult = timelineTweet;
                    SafeRaiseEvent(StreamingEvent);
                    return;
                }

                // tweet destroy
                var deleteStatus = JsonConvert.DeserializeObject<StreamResponseDelete>(response.Content,
                                                                                        new JsonSerializerSettings
                                                                                        {
                                                                                            Error = delegate(object sender, ErrorEventArgs args)
                                                                                            {
                                                                                                errors.Add(args.ErrorContext.Error.Message);
                                                                                                args.ErrorContext.Handled = true;
                                                                                            }
                                                                                        });
                if (errors.Count == 0 && deleteStatus.delete != null)
                {
                    StreamingResult = deleteStatus;
                    SafeRaiseEvent(StreamingEvent);
                    return;
                }

                // friend create
                // friend destroy

                // direct message received
                var dmReceived = JsonConvert.DeserializeObject<StreamDirectMessageReceived>(response.Content,
                                                                                            new JsonSerializerSettings
                                                                                            {
                                                                                                Error = delegate(object sender, ErrorEventArgs args)
                                                                                                {
                                                                                                    errors.Add(args.ErrorContext.Error.Message);
                                                                                                    args.ErrorContext.Handled = true;
                                                                                                }
                                                                                            });

                if (errors.Count == 0 && dmReceived.direct_message != null)
                {
                    StreamingResult = dmReceived;
                    SafeRaiseEvent(StreamingEvent);
                    return;
                }

                // direct message sent
                var dmSent = JsonConvert.DeserializeObject<StreamDirectMessageSent>(response.Content,
                                                                                    new JsonSerializerSettings
                                                                                    {
                                                                                        Error = delegate(object sender, ErrorEventArgs args)
                                                                                        {
                                                                                            errors.Add(args.ErrorContext.Error.Message);
                                                                                            args.ErrorContext.Handled = true;
                                                                                        }
                                                                                    });

                if (errors.Count == 0 && dmSent.direct_message != null)
                {
                    StreamingResult = dmSent;
                    SafeRaiseEvent(StreamingEvent);
                    return;
                }


            }
            finally
            {

            }

        }

        public void StopStreaming()
        {
            try
            {
                Client.CancelStreaming();
            }
            catch (Exception)
            {

            }

        }

        #endregion

        #region Configuration

        //public void GetConfiguration()
        //{
        //    // lists/members/create
        //    var request = new RestRequest
        //    {
        //        Credentials = Credentials,
        //        Path = "help/configuration.json",
        //        Method = WebMethod.Get,
        //        DecompressionMethods = DecompressionMethods.GZip,
        //        VersionPath = TwitterConstants.ApiVersion
        //    };

        //    Client.BeginRequest(request, new RestCallback(GetConfigurationCompleted));
        //}

        //private void GetConfigurationCompleted(RestRequest request, RestResponse response, object userstate)
        //{

        //}

        #endregion

        #region Get Muted Users

        public async Task<ResponseMutedUsers> GetMutedUsersAsync()
        {
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "mutes/users/list.json",
                Method = WebMethod.Get,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            return await CallClient<ResponseMutedUsers>(request);
        }

        #endregion

        #region Create Mute

        public async Task<ResponseGetUserProfile> MuteUserAsync(string screenName, long? userId = null)
        {
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "mutes/users/create.json",
                Method = WebMethod.Post,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            if (!string.IsNullOrEmpty(screenName))
            {
                request.AddParameter("screen_name", screenName);
            }

            if (userId.HasValue)
            {
                request.AddParameter("user_id", userId.Value.ToString(CultureInfo.InvariantCulture));                
            }

            return await CallClient<ResponseGetUserProfile>(request);
        }

        #endregion

        public async Task<ResponseGetUserProfile> UnmuteUser(string screenName, long? userId = null)
        {
            var request = new RestRequest
            {
                Credentials = Credentials,
                Path = "mutes/users/destroy.json",
                Method = WebMethod.Post,
                DecompressionMethods = DecompressionMethods.GZip,
                VersionPath = TwitterConstants.ApiVersion
            };

            if (!string.IsNullOrEmpty(screenName))
            {
                request.AddParameter("screen_name", screenName);
            }

            if (userId.HasValue)
            {
                request.AddParameter("user_id", userId.Value.ToString(CultureInfo.InvariantCulture));
            }

            return await CallClient<ResponseGetUserProfile>(request);
        }
    }
}