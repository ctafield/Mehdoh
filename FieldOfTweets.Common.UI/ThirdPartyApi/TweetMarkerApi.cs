using System;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.UI.ThirdPartyApi.Classes;
using FieldOfTweets.Common.UI.ThirdPartyApi.Classes.TweetMarker;
using Hammock;
using Hammock.Authentication.OAuth;
using Hammock.Caching;
using Hammock.Retries;
using Hammock.Web;
using Newtonsoft.Json;

namespace FieldOfTweets.Common.UI.ThirdPartyApi
{

    /// <summary>
    /// Documentation can be found here: 
    /// http://tweetmarker.net/developers/
    /// </summary>
    public class TweetMarkerApi
    {

        private const string TweetMarkerApiKey = "ME-85354E4C7EF1";

        public long AccountId { get; private set; }
        public long? LastTimelineId { get; private set; }

        public event EventHandler GetLastReadCompletedEvent;

        public void GetLastRead(long accountId)
        {

            // GET https://api.tweetmarker.net/v1/lastread?collection=timeline&username=manton
            // GET /v2/lastread?api_key=yourkey&username=manton&collection=timeline&collection=mentions
            // This doesn't require echo

            AccountId = accountId;

            TwitterAccess user;

            using (var sh = new StorageHelper())
            {
                user = sh.GetTwitterUser(accountId);
            }

            var client = new RestClient
            {
                HasElevatedPermissions = true,
                UserAgent = "mehdoh",
                RetryPolicy = new RetryPolicy() { RetryCount = 3 },
                Authority = "https://api.tweetmarker.net",
                VersionPath = "v2",
                CacheOptions = new CacheOptions() { Mode = CacheMode.AbsoluteExpiration, Duration = TimeSpan.FromSeconds(0) }
            };

            var request = new RestRequest()
                          {
                              Path = "lastread",
                              Method = WebMethod.Get,
                              CacheOptions = new CacheOptions() { Mode = CacheMode.AbsoluteExpiration, Duration = TimeSpan.FromSeconds(0) }
                          };

            request.AddParameter("api_key", TweetMarkerApiKey);
            request.AddParameter("collection", "timeline");
            request.AddParameter("username", user.ScreenName);
            
            request.AddParameter("plsdontcache", DateTime.Now.ToString("ddMMyyyyHHmmss"));

            client.BeginRequest(request, GetLastReadCompleted);

        }

        private void GetLastReadCompleted(RestRequest request, RestResponse response, object userstate)
        {
            try
            {
                var res = JsonConvert.DeserializeObject<TweetMarkerResponse>(response.Content);
                if (res != null)
                {
                    if (res.timeline != null && res.timeline.id.HasValue)
                        LastTimelineId = res.timeline.id.Value;
                    else
                        LastTimelineId = null;
                }
            }
            catch
            {                
                // ignore, usually broken anyway

                // todo: maybe retry?
            }
            finally
            {
                if (GetLastReadCompletedEvent != null)
                    GetLastReadCompletedEvent(this, null);
            }

        }

        public event EventHandler SetLastReadCompletedEvent;

        public void SetLastTimelineRead(long accountId, long timelineId)
        {

            TwitterAccess user;

            using (var sh = new StorageHelper())
            {
                user = sh.GetTwitterUser(accountId);
            }

            var credentials = new OAuthCredentials
            {
                Type = OAuthType.ProtectedResource,
                SignatureMethod = OAuthSignatureMethod.HmacSha1,
                ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                ConsumerKey = TwitterConstants.ConsumerKey,
                ConsumerSecret = TwitterConstants.ConsumerKeySecret,
                Token = user.AccessToken,
                TokenSecret = user.AccessTokenSecret,
                Version = TwitterConstants.OAuthVersion
            };

            var authClient = new RestClient()
            {
                Authority = "https://api.twitter.com/1",
                UserAgent = "mehdoh"
            };

            var authReq = new RestRequest
            {
                Credentials = credentials,
                Path = "account/verify_credentials.json",
                Method = WebMethod.Get
            };

            var tmRequest = OAuthCredentials.DelegateWith(authClient, authReq);

            var client = new RestClient()
            {
                Authority = "https://api.tweetmarker.net/",
                VersionPath = "v2",
                Method = WebMethod.Post
            };

            tmRequest.Path = "lastread?api_key=" + TweetMarkerApiKey + "&username=" + user.ScreenName;

            //tmRequest.AddParameter("api_key", TweetMarkerApiKey);
            //tmRequest.AddParameter("collection", "timeline");
            //tmRequest.AddParameter("id", id.ToString());
            //tmRequest.AddParameter("username", user.ScreenName);


            var content = new TweetMarkerPost()
                          {
                              timeline = new Timeline()
                                         {
                                             id = timelineId
                                         }
                          };

            var contentString = JsonConvert.SerializeObject(content, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            var bytes = System.Text.Encoding.UTF8.GetBytes(contentString);
            tmRequest.AddPostContent(bytes);

            client.BeginRequest(tmRequest, SetLastReadCompleted);

        }

        private void SetLastReadCompleted(RestRequest request, RestResponse response, object userstate)
        {
            if (SetLastReadCompletedEvent != null)
                SetLastReadCompletedEvent(this, null);
        }

        public void SetLastMentionsRead(long accountId, long mentionsId)
        {

            TwitterAccess user;

            using (var sh = new StorageHelper())
            {
                user = sh.GetTwitterUser(accountId);

            }

            var credentials = new OAuthCredentials
            {
                Type = OAuthType.ProtectedResource,
                SignatureMethod = OAuthSignatureMethod.HmacSha1,
                ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                ConsumerKey = TwitterConstants.ConsumerKey,
                ConsumerSecret = TwitterConstants.ConsumerKeySecret,
                Token = user.AccessToken,
                TokenSecret = user.AccessTokenSecret,
                Version = TwitterConstants.OAuthVersion
            };

            var authClient = new RestClient()
            {
                Authority = "https://api.twitter.com/1",
                UserAgent = "mehdoh"
            };

            var authReq = new RestRequest
            {
                Credentials = credentials,
                Path = "account/verify_credentials.json",
                Method = WebMethod.Get
            };

            var tmRequest = OAuthCredentials.DelegateWith(authClient, authReq);

            var client = new RestClient()
            {
                Authority = "https://api.tweetmarker.net/",
                VersionPath = "v2",
                Method = WebMethod.Post
            };

            tmRequest.Path = "lastread?api_key=" + TweetMarkerApiKey + "&username=" + user.ScreenName;

            var content = new TweetMarkerPost()
            {
                mentions = new Mentions()
                {
                    id = mentionsId,
                }
            };

            var contentString = JsonConvert.SerializeObject(content, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            var bytes = System.Text.Encoding.UTF8.GetBytes(contentString);
            tmRequest.AddPostContent(bytes);

            client.BeginRequest(tmRequest, SetLastReadCompleted);

        }


    }

}
