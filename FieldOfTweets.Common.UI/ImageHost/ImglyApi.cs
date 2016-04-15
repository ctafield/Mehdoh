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
    public class ImglyApi : ImageHostBase, IImageHost
    {

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
                Authority = "http://img.ly/api/2",
                HasElevatedPermissions = true
            };

            picReq.Path = "upload.json";

            string fileName = Path.GetFileName(filePath);
            picReq.AddFile("media", fileName, ImageStream);

            var taskCompletion = new TaskCompletionSource<string>();

            client.BeginRequest(picReq,
                delegate(RestRequest request, RestResponse response, object userState)
                {
                    try
                    {
                        var res = JsonConvert.DeserializeObject<ResponseImgly>(response.Content);
                        if (res != null)
                            UploadedUrl = res.url;
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
            return "http://img.ly/xxxx";
        }
       
    }
}
