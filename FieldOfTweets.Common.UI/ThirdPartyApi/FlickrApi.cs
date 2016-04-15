using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using Newtonsoft.Json;

namespace FieldOfTweets.Common.UI.ThirdPartyApi
{
    public class FlickrApi
    {

        private string Key
        {
            get { return "edfb7237a9086f7d00d1db118f5b553d"; }
        }

        private string Secret
        {
            get { return "935293fecd707ab5"; }
        }

        private string EndPoint
        {
            get { return "https://api.flickr.com/services/rest/?method={0}&api_key=" + Key + "&format=json&nojsoncallback=1&{1}"; }
        }

        /// <summary>
        /// flickr.photos.getSizes
        /// </summary>
        /// <param name="photoId"></param>
        public async Task<FlickrPhotoDetails> GetPhotoDetails(string photoId)
        {

            string url = string.Format(EndPoint, "flickr.photos.getSizes", "photo_id=" + photoId);

            // api_key = key
            // photo_id = photoId

            var t = new HttpClient();
            var result = await t.GetStringAsync(new Uri(url, UriKind.Absolute));

            return JsonConvert.DeserializeObject<FlickrPhotoDetails>(result);
        }


        private const string Base58Alphabet = "123456789abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";

        public long DecodeBase58(String base58StringToExpand)
        {
            long lConverted = 0;
            long lTemporaryNumberConverter = 1;

            while (base58StringToExpand.Length > 0)
            {
                String sCurrentCharacter = base58StringToExpand.Substring(base58StringToExpand.Length - 1);
                lConverted = lConverted + (lTemporaryNumberConverter * Base58Alphabet.IndexOf(sCurrentCharacter));
                lTemporaryNumberConverter = lTemporaryNumberConverter * Base58Alphabet.Length;
                base58StringToExpand = base58StringToExpand.Substring(0, base58StringToExpand.Length - 1);
            }

            return lConverted;
        }

    }

    public class FlickrPhotoSize
    {
        public string label { get; set; }
        public object width { get; set; }
        public object height { get; set; }
        public string source { get; set; }
        public string url { get; set; }
        public string media { get; set; }
    }

    public class FlickrPhotoSizes
    {
        public int canblog { get; set; }
        public int canprint { get; set; }
        public int candownload { get; set; }
        public List<FlickrPhotoSize> size { get; set; }
    }

    public class FlickrPhotoDetails
    {
        public FlickrPhotoSizes sizes { get; set; }
        public string stat { get; set; }
    }
}
