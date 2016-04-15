using System;
using System.Net;

namespace FieldOfTweets.Common.UI.ThirdPartyApi
{
    public class InstapaperApi : ISaveLater
    {

        #region Authenticate

        public void Authenticate(string username, string password)
        {

            const string baseUrl = "https://www.instapaper.com/api/authenticate?username={0}&password={1}";

            string url = string.Format(baseUrl, HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password));

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
                        AuthenticateSuccess = (response.StatusCode == HttpStatusCode.OK);                    
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

            const string baseUrl = "https://www.instapaper.com/api/add?username={0}&password={1}&url={2}&selection={3}";

            string url = string.Format(baseUrl, HttpUtility.UrlEncode(username), HttpUtility.UrlEncode(password), HttpUtility.UrlEncode(urlToAdd), HttpUtility.UrlEncode(desc));

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
                var response = request.EndGetResponse(ar) as HttpWebResponse;

                HttpStatusCode responseCode = response.StatusCode;

                if (responseCode == HttpStatusCode.Created)
                    AddUrlSuccess = true;
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
