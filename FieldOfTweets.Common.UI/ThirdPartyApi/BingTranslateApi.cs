using System;
using System.IO;
using System.Net;

namespace FieldOfTweets.Common.UI.ThirdPartyApi
{
    public class BingTranslateApi
    {

        private string ApiKey
        {
            get
            {
                return "92D41D10D75FB36A058CFCCB7F5C522E2FFAA9FC";
            }
        }

        public event EventHandler TranslateTextCompleted;

        public string TranslatedText { get; set; }

        public void TranslateText(string inputText, string language)
        {
            var uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?to=" + language + "&appId=" + ApiKey + "&text=" + Uri.EscapeDataString(inputText);
            var req = (HttpWebRequest)WebRequest.Create(uri);
            req.BeginGetResponse(CallBackMethod, req);
        }

        private void CallBackMethod(IAsyncResult ar)
        {
            try
            {
                var request = (HttpWebRequest) ar.AsyncState;
                var response = (HttpWebResponse) request.EndGetResponse(ar);

                using (var responseStream = response.GetResponseStream())
                {
                    var sr = new StreamReader(responseStream);
                    TranslatedText = sr.ReadToEnd();
                    TranslatedText = TranslatedText.Substring(TranslatedText.IndexOf(">", StringComparison.InvariantCultureIgnoreCase) + 1);
                    TranslatedText = TranslatedText.Substring(0, TranslatedText.IndexOf("<", StringComparison.InvariantCultureIgnoreCase));

                    TranslatedText = HttpUtility.HtmlDecode(TranslatedText);
                }
            }
            catch (Exception ex)
            {
                ErrorLogging.ErrorLogger.LogException("CallBackMethod", ex);
            }
            finally
            {
                if (TranslateTextCompleted != null)
                    TranslateTextCompleted(this, null);
            }
        }

    }
}
