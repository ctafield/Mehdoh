using System;
using System.Threading.Tasks;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.ImageHost;
using FieldOfTweets.Common.UI.ImageHost.Responses;
using Hammock;
using Hammock.Authentication.OAuth;
using Hammock.Web;
using Newtonsoft.Json;
using Path = System.IO.Path;

namespace FieldOfTweets.Common.UI.ImageHost
{
    public class MobyPictureImageHostApi : ImageHostBase, IImageHost
    {

        private const string ApiKey = "GT2heokeFF8k5tqs";

        public Task<string> UploadImage(long accountId, string filePath)
        {
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
                Authority = "https://api.mobypicture.com/2.0",
                HasElevatedPermissions = true
            };

            picReq.Path = "upload.json";

            picReq.AddField("key", ApiKey);

            string fileName = Path.GetFileName(filePath);
            picReq.AddFile("media", fileName, ImageStream);

            var taskCompletion = new TaskCompletionSource<string>();

            client.BeginRequest(picReq, delegate(RestRequest request, RestResponse response, object userState)
            {
                try
                {
                    var res = JsonConvert.DeserializeObject<ResponseMobyPicture>(response.Content);
                    if (res != null)
                        UploadedUrl = res.media.mediaurl;
                    else
                        HasError = true;

                    taskCompletion.SetResult(UploadedUrl);
                }
                catch (Exception ex)
                {
                    taskCompletion.TrySetException(ex);
                    HasError = true;
                }
            });

            return taskCompletion.Task;
        }
        
        public override string GetPlaceHolder()
        {
            return "http://moby.to/xxxxxx";
        }

    }
}
