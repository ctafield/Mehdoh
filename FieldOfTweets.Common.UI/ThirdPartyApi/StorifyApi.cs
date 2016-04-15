using System;
using System.Collections.Generic;
using System.Text;
using FieldOfTweets.Common.Api;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.UI.ThirdPartyApi.Classes;
using FieldOfTweets.Common.UI.ThirdPartyApi.Classes.Storify;
using Hammock;
using Hammock.Authentication.OAuth;
using Hammock.Web;
using Newtonsoft.Json;

namespace FieldOfTweets.Common.UI.ThirdPartyApi
{
    public class StorifyApi : MehdohApi
    {

        private const string StorifyApiKey = "4ff62aa25ec1810000428832";

        private RestClient Client { get; set; }

        protected TwitterAccess GetTwitterUser(long accountId)
        {
            using (var sh = new StorageHelper())
            {
                var user = sh.GetTwitterUser(accountId);
                return user;
            }
        }

        public void CreateStory(long accountId, string userName, List<string> screenName, IEnumerable<string> urls)
        {

            // http://dev.storify.com/api/stories#save
            

            var twitterUser = GetTwitterUser(accountId);

            var credentials = new OAuthCredentials
            {
                Type = OAuthType.ProtectedResource,
                SignatureMethod = OAuthSignatureMethod.HmacSha1,
                ParameterHandling = OAuthParameterHandling.HttpAuthorizationHeader,
                ConsumerKey = TwitterConstants.ConsumerKey,
                ConsumerSecret = TwitterConstants.ConsumerKeySecret,
                Token = twitterUser.AccessToken,
                TokenSecret = twitterUser.AccessTokenSecret,
                Version = TwitterConstants.OAuthVersion
            };

            var authClient = new RestClient()
            {
                Authority = TwitterConstants.BaseApiUrl + TwitterConstants.ApiVersion,
                UserAgent = "mehdoh"
            };

            var authReq = new RestRequest
            {
                Credentials = credentials,
                Path = "account/verify_credentials.json",
                Method = WebMethod.Get
            };

            var picReq = OAuthCredentials.DelegateWith(authClient, authReq);

            var client = new RestClient()
            {
                Authority = "https://api.storify.com/v1",
                HasElevatedPermissions = true,
                Method = WebMethod.Post
            };

            picReq.Path = "/stories/" + userName + "/create";

            var request = new StorifyNewStoryRequest
                              {
                                  publish = "true",
                                  story = new StorifyNewStoryRequestStory
                                          {
                                              slug = GetSlug(screenName),
                                              title = GetTitle(screenName),
                                              elements = new List<string>()
                                          }
                              };

            request.story.elements.AddRange(urls);

            picReq.AddParameter("story", JsonConvert.SerializeObject(request.story));
            picReq.AddParameter("publish", "true");

            picReq.AddParameter("username", userName);
            picReq.AddParameter("api_key", StorifyApiKey);

            client.BeginRequest(picReq, CreateStoryCompleted);

        }

        private string GetTitle(List<string> screenName)
        {

            if (screenName.Count == 1)
            {
                return "Tweet From " + screenName[0].ToLower();
            }

            var sb = new StringBuilder("Conversation with ");

            for (int i = 0; i < screenName.Count - 1; i++)
            {
                sb.Append("@");
                sb.Append(screenName[i]);
                sb.Append(" ");
            }

            sb.Append("and ");
            sb.Append("@");
            sb.Append(screenName[screenName.Count - 1]);

            return sb.ToString();        
        }

        private string GetSlug(List<string> screenName)
        {
            if (screenName.Count == 1)
            {
                return "tweet-from-" + screenName[0].ToLower();
            }

            var sb = new StringBuilder("conversation-with-");

            for (int i = 0; i < screenName.Count - 1; i++)
            {
                sb.Append(screenName[i].ToLower());
                sb.Append("-");
            }

            sb.Append("and-");
            sb.Append(screenName[screenName.Count - 1]);

            return sb.ToString();
        }

        public event EventHandler CreateStoryCompletedEvent;

        public CreateStorifyStoryResponse Story;

        private void CreateStoryCompleted(RestRequest request, RestResponse response, object userstate)
        {
            try
            {
                Story = DeserialiseResponse<CreateStorifyStoryResponse>(response.Content);
            }
            finally
            {
                if (CreateStoryCompletedEvent != null)
                    CreateStoryCompletedEvent(this, null);
            }

        }

    }

}
