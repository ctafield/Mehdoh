using System.Collections.Generic;
using System.Linq;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.ViewModels;

namespace FieldOfTweets.Common.UI.Helpers
{
    public class AccountPostingHelper
    {

        public IEnumerable<AccountViewModel> GetAllValidAccountsForPosting()
        {

            using (var dh = new MainDataContext())
            {
                dh.ObjectTrackingEnabled = false;
                var results = dh.Profiles.Where(x => x.UseToPost.HasValue && x.UseToPost.Value).Select(x => x.AsViewModel()).ToList();
                return results;
            }
           
        }

        public IEnumerable<AccountViewModel> GetReplyAccount(long accountId)
        {
            using (var dh = new MainDataContext())
            {
                dh.ObjectTrackingEnabled = false;
                var results = dh.Profiles.Where(x => x.Id == accountId).Select(x => x.AsViewModel()).ToList();
                return results;
            }
        }

    }
}
