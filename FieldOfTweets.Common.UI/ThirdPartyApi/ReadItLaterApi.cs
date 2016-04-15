using System;
using System.Net;

namespace FieldOfTweets.Common.UI.ThirdPartyApi
{
    public class ReadItLaterApi : ISaveLater
    {

        private string ApiKey
        {
            get
            {
                return "c46TlD0gg8fu3i9aN0d306aha5pdEfI6";                
            }
        }

        #region Authenticate

        public void Authenticate(string username, string password)
        {

            const string baseUrl = "https://readitlaterlist.com/v2/auth?username={0}&password={1}&apikey={2}";

            string url = string.Format(baseUrl, HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password), ApiKey);

            WebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.BeginGetResponse(new AsyncCallback(AuthenticateCallback), request);

        }

        public event EventHandler<EventArgs> AuthenticateCompleted;

        public bool AuthenticateSuccess;

        private void AuthenticateCallback(IAsyncResult ar)
        {

            try
            {
                var request = ar.AsyncState as WebRequest;
                if (request != null)
                {
                    var response = request.EndGetResponse(ar) as HttpWebResponse;

                    if (response != null)
                    {
                        HttpStatusCode responseCode = response.StatusCode;

                        if (responseCode == HttpStatusCode.OK)
                            AuthenticateSuccess = true;
                    }
                }
            }
            catch (Exception)
            {
                AuthenticateSuccess = false;
            }

            if (AuthenticateCompleted != null)
                AuthenticateCompleted(this, null);

        }

        #endregion

        #region Add Url

        public void AddUrl(string username, string password, string urlToAdd, string desc, string id)
        {

            const string baseUrl = "https://readitlaterlist.com/v2/add?apikey={0}&username={1}&password={2}&url={3}&title={4}&ref_id={5}";

            string url = string.Format(baseUrl, ApiKey, HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password), HttpUtility.UrlEncode(urlToAdd), HttpUtility.UrlEncode(desc), id);

            WebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.BeginGetResponse(new AsyncCallback(AddUrlCallback), request);

        }

        public event EventHandler<EventArgs> AddUrlCompleted;

        public bool AddUrlSuccess;

        private void AddUrlCallback(IAsyncResult ar)
        {

            try
            {
                var request = ar.AsyncState as WebRequest;
                if (request != null)
                {
                    var response = request.EndGetResponse(ar) as HttpWebResponse;
                    if (response != null)
                        AddUrlSuccess = (response.StatusCode == HttpStatusCode.OK);
                }
            }
            catch (Exception)
            {
                AddUrlSuccess = false;
            }

            if (AddUrlCompleted != null)
                AddUrlCompleted(this, null);

        }

        #endregion

    }

}
