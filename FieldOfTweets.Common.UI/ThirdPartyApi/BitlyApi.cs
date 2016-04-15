using System;
using System.IO;
using System.Net;
using FieldOfTweets.Common.UI.ThirdPartyApi.Classes;
using Newtonsoft.Json;

namespace FieldOfTweets.Common.UI.ThirdPartyApi
{
    public class BitlyApi
    {

        private const string BitlyApiUsername = "ctafield";
        private const string BitlyApiKey = "R_c4aa1f23bd7d617c4578e0f56348bf2f";

        private string BaseUrl
        {
            get
            {
                return "https://api-ssl.bitly.com";
            }
        }

        public string LongUrl { get; private set; }
        public string ShortUrl { get; private set; }

        public event EventHandler GetShortUrlCompletedEvent;

        public void GetShortUrl(string longUrl)
        {

            LongUrl = longUrl;

            if (string.IsNullOrWhiteSpace(longUrl))
            {
                if (GetShortUrlCompletedEvent != null)
                    GetShortUrlCompletedEvent(this, null);                
            }

            if (!longUrl.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) && 
                !longUrl.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase) && 
                !longUrl.StartsWith("ftp://", StringComparison.InvariantCultureIgnoreCase))
            {
                longUrl = "http://" + longUrl;
            }
            
            var url = string.Format(BaseUrl + "/v3/shorten/?login={0}&apiKey={1}&longUrl={2}", BitlyApiUsername, BitlyApiKey, HttpUtility.UrlEncode(longUrl));

            WebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.BeginGetResponse(new AsyncCallback(GetShortUrlCompleted), request);

            
        }

        private void GetShortUrlCompleted(IAsyncResult ar)
        {
            try
            {
                var request = ar.AsyncState as WebRequest;
                var response = request.EndGetResponse(ar) as HttpWebResponse;

                HttpStatusCode responseCode = response.StatusCode;

                if (responseCode == HttpStatusCode.OK)
                {
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {
                        var content = sr.ReadToEnd();

                        var res = JsonConvert.DeserializeObject<BitlyShortenResponse>(content);

                        ShortUrl = res.data.url;
                    }                    
                }

            }
            catch (Exception)
            {
                ShortUrl = string.Empty;
            }

            if (GetShortUrlCompletedEvent != null)
                GetShortUrlCompletedEvent(this, null);
        }

    }
}
