using System;
using System.Collections.Generic;
using System.Linq;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.UI.Search
{

    public class SearchHelper
    {

        #region People

        public void AddToPeopleSearchRecents(string description)
        {

            if (string.IsNullOrEmpty(description))
                return;

            if (description.Contains("@"))
                description = description.Replace("@", "");

            if (string.IsNullOrEmpty(description))
                return;

            using (var dh = new MainDataContext())
            {

                var newSearch = new RecentPeopleSearchTable
                                    {
                                        Description = description,
                                        Timestamp = DateTime.Now
                                    };

                var t = dh.RecentPeopleSearch.SingleOrDefault(x => string.Compare(x.Description, description, StringComparison.InvariantCultureIgnoreCase) == 0);
                
                if (t != null)
                {
                    t.Timestamp = DateTime.Now;                    
                }
                else
                {
                    if (dh.RecentPeopleSearch.Count() < 10)
                    {
                        dh.RecentPeopleSearch.InsertOnSubmit(newSearch);
                    }
                    else
                    {
                        var res = dh.RecentPeopleSearch.OrderByDescending(x => x.Timestamp).Take(10).ToList();

                        dh.RecentPeopleSearch.DeleteOnSubmit(res[9]);
                        dh.RecentPeopleSearch.InsertOnSubmit(newSearch);
                    }
                }

                dh.SubmitChanges();

            }

        }

        public IEnumerable<string> GetRecentPeopleSearches()
        {

            using (var dh = new MainDataContext())
            {
                return dh.RecentPeopleSearch.OrderByDescending(x => x.Timestamp).Select(x => "@" + x.Description).ToList();
            }

        }

        public void ClearRecentPeopleSearch()
        {
            using (var dh = new MainDataContext())
            {
                var res = dh.RecentPeopleSearch.ToList();
                dh.RecentPeopleSearch.DeleteAllOnSubmit(res);
                dh.SubmitChanges();
            }
        }

        #endregion

        #region Text

        public void AddToTextSearchRecents(string description)
        {

            if (string.IsNullOrEmpty(description))
                return;

            using (var dh = new MainDataContext())
            {

                var newSearch = new RecentTextSearchTable
                                    {
                    Description = description,
                    Timestamp = DateTime.Now
                };

                var t = dh.RecentTextSearch.SingleOrDefault(x => string.Compare(x.Description, description, StringComparison.InvariantCultureIgnoreCase) == 0);

                if (t != null)
                {
                    t.Timestamp = DateTime.Now;
                }
                else
                {
                    if (dh.RecentTextSearch.Count() < 10)
                    {
                        dh.RecentTextSearch.InsertOnSubmit(newSearch);
                    }
                    else
                    {
                        var res = dh.RecentTextSearch.OrderByDescending(x => x.Timestamp).Take(10).ToList();

                        dh.RecentTextSearch.DeleteOnSubmit(res[9]);
                        dh.RecentTextSearch.InsertOnSubmit(newSearch);
                    }
                }

                dh.SubmitChanges();

            }

        }

        public IEnumerable<string> GetRecentTextSearches()
        {

            using (var dh = new MainDataContext())
            {
                return dh.RecentTextSearch.OrderByDescending(x => x.Timestamp).Select(x => x.Description).ToList();
            }

        }

        public void ClearRecentTextSearch()
        {
            using (var dh = new MainDataContext())
            {
                var res = dh.RecentTextSearch.ToList();
                dh.RecentTextSearch.DeleteAllOnSubmit(res);
                dh.SubmitChanges();
            }
        }

        #endregion

    }

}
