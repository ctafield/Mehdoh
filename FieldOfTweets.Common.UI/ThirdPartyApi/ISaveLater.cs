namespace FieldOfTweets.Common.UI.ThirdPartyApi
{
    public interface ISaveLater
    {
        void AddUrl(string username, string password, string urlToAdd, string desc, string id);
    }
}
