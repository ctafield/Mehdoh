namespace FieldOfTweets.Common.DataContext
{
    public class TempDataContext : MainDataContext
    {

        public TempDataContext()
            : base(ApplicationSettings.ConnectionStringForCopying)
        {
        }

    }
}
