using System.Threading.Tasks;
using FieldOfTweets.Common.ImageHost;

namespace FieldOfTweets.Common.UI.ImageHost
{

    public class TwitterImageHostApi : ImageHostBase, IImageHost
    {
        public new bool IsTwitter
        {
            get { return true; }
        }

        public override string GetPlaceHolder()
        {
            // This may be irrelevant as it needs to be at the end of the tweet
            return "https://t.co/xxxxxxxxxx";
        }

        /// <summary>
        /// Not used by Twitter image host. Part of the twitter posting.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="filePath"></param>
        public Task<string> UploadImage(long accountId, string filePath)
        {
            // do nothing
            var taskCompletion = new TaskCompletionSource<string>();
            taskCompletion.SetResult(string.Empty);
            return taskCompletion.Task;
        }

    }
}
