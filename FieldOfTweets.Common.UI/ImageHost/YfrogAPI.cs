using System;
using System.IO;
using System.Threading.Tasks;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.ImageHost;
using FieldOfTweets.Common.Responses.ImageHosts;
using Hammock;
using Hammock.Authentication.OAuth;
using Hammock.Web;
using Newtonsoft.Json;

namespace FieldOfTweets.Common.UI.ImageHost
{

    /// <summary>    
    /// http://yfrog.com/api/upload
    /// </summary>
    public class YfrogAPI : ImageHostBase, IImageHost
    {

        private string YFrogApiKey
        {
            get { return "58HLOQRXa2245504f90fcfcb483c38ab3f649e68"; }
        }

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
                Authority = "http://yfrog.com/api",
                HasElevatedPermissions = true
            };

            picReq.Path = "xauth_upload";

            picReq.AddField("key", YFrogApiKey);

            string fileName = Path.GetFileName(filePath);

            picReq.AddFile("media", fileName, ImageStream);

            var taskCompletion = new TaskCompletionSource<string>();

            client.BeginRequest(picReq,
                delegate(RestRequest request, RestResponse response, object userState)
                {
                    try
                    {
                        var res = JsonConvert.DeserializeObject<ResponseYFrog>(response.Content);
                        if (res != null)
                            UploadedUrl = res.rsp.mediaurl;
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
            return "http://yfrog.com/xxxxxx";
        }

    }

}