using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.ViewModels;

namespace Mitter.Helpers
{
    public class DetailsViewHelper
    {
        public DetailsPageViewModel TryGetDetailsViewFromTimelineTweet(long id, long accountId)
        {

            using (var dh = new MainDataContext())
            {

                // check timeline table
                var res = from timelines
                            in dh.Timeline
                          where timelines.Id == id
                              && timelines.ProfileId == accountId
                          select timelines;

                if (res.Any())
                {
                    var firstItem = res.First();
                    return firstItem.AsDetailsViewModel();
                }

                // check lists
                var res4 = from lists
                            in dh.TwitterList
                           where lists.Id == id
                           && lists.ProfileId == accountId
                           select lists;

                if (res4.Any())
                {
                    return res4.First().AsDetailsViewModel();
                }

                // check search results
                var resSearch = from lists
                            in dh.TwitterSearch
                           where lists.Id == id
                           && lists.ProfileId == accountId
                           select lists;

                if (resSearch.Any())
                {
                    return resSearch.First().AsDetailsViewModel();
                }


                // check your retweets
                var res2 = from timelines
                            in dh.RetweetedByMe
                           where timelines.Id == id
                           && timelines.ProfileId == accountId
                           select timelines;

                if (res2.Any())
                {
                    return res2.First().AsDetailsViewModel();
                }

                // check retweets to you
                var res3 = from timelines
                            in dh.RetweetsToMe
                           where timelines.Id == id
                           && timelines.ProfileId == accountId
                           select timelines;

                if (res3.Any())
                {
                    return res3.First().AsDetailsViewModel();
                }


            }


            return null;

        }
    }
}
