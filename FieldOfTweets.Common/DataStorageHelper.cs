using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.ErrorLogging;
using Newtonsoft.Json;

namespace FieldOfTweets.Common
{
    public class DataStorageHelper
    {

        private static readonly object SaveMentionUpdatesLock = new object();

        public void SaveMentionUpdates(IList<ResponseTweet> mentions, long accountId)
        {

            if (mentions == null || !mentions.Any())
                return;

            lock (SaveMentionUpdatesLock)
            {

                try
                {

                    var newItems = new List<MentionTable>();

                    using (var dh = new SaveMentionsContext())
                    {

                        foreach (var s in mentions)
                        {

                            var currentId = s.id;

                            if (dh.Mentions.Any(x => x.Id == currentId && x.ProfileId == accountId))
                                continue;

                            if (newItems.Any(x => x.Id == s.id && x.ProfileId == accountId))
                                continue;

                            var tt = MentionToTable(accountId, s);
                            newItems.Add(tt);

                            //newItems.Add(tt);

                        }

                        if (newItems.Any())
                        {
                            dh.Mentions.InsertAllOnSubmit(newItems);
                            dh.SubmitChanges();
                        }

                    }

                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException("SaveMentionUpdates : ", ex);
                }

            }

        }

        private MentionTable MentionToTable(long accountId, ResponseTweet s)
        {
            var tt = new MentionTable
            {
                Id = s.id,
                IdStr = s.id_str,
                CreatedAt = s.created_at,
                Description = s.text,
                ScreenName = s.user.screen_name,
                DisplayName = s.user.name,
                CreatedById = s.user.id,
                ProfileImageUrl = s.user.profile_image_url,
                ProfileId = accountId,
                Client = s.source,
                IsRetweet = false,
                Verified = (s.user.verified.HasValue) && s.user.verified.Value,
                InReplyToId = (s.in_reply_to_status_id.HasValue) ? s.in_reply_to_status_id.Value : 0,
                LanguageCode = s.lang
            };

            var res = ResponseEntitiesToTable<MentionAssetTable>(s.entities);
            if (res != null && res.Any())
            {
                tt.Assets.AddRange(res);
            }

            try
            {
                // Geo stuff
                if (s.coordinates != null && s.coordinates.coordinates != null)
                {
                    tt.LocationFullName = s.coordinates.coordinates[1].ToString(CultureInfo.CurrentUICulture) + ", " + s.coordinates.coordinates[0].ToString(CultureInfo.CurrentUICulture);
                    tt.LocationCountry = string.Empty;

                    tt.Location1X = s.coordinates.coordinates[1];
                    tt.Location1Y = s.coordinates.coordinates[0];
                    tt.Location2X = s.coordinates.coordinates[1];
                    tt.Location2Y = s.coordinates.coordinates[0];
                    tt.Location3X = s.coordinates.coordinates[1];
                    tt.Location3Y = s.coordinates.coordinates[0];
                    tt.Location4X = s.coordinates.coordinates[1];
                    tt.Location4Y = s.coordinates.coordinates[0];
                }
                else if (s.geo != null && s.geo.coordinates != null)
                {
                    tt.LocationFullName = s.geo.coordinates[1].ToString(CultureInfo.CurrentUICulture) + ", " + s.geo.coordinates[0].ToString(CultureInfo.CurrentUICulture);
                    tt.LocationCountry = string.Empty;

                    tt.Location1X = s.geo.coordinates[1];
                    tt.Location1Y = s.geo.coordinates[0];
                    tt.Location2X = s.geo.coordinates[1];
                    tt.Location2Y = s.geo.coordinates[0];
                    tt.Location3X = s.geo.coordinates[1];
                    tt.Location3Y = s.geo.coordinates[0];
                    tt.Location4X = s.geo.coordinates[1];
                    tt.Location4Y = s.geo.coordinates[0];
                }
                else if (s.place != null)
                {
                    tt.LocationFullName = s.place.full_name;
                    tt.LocationCountry = s.place.country;
                    if (s.place.bounding_box != null && s.place.bounding_box.coordinates != null)
                    {
                        tt.Location1X = s.place.bounding_box.coordinates[0][0][0];
                        tt.Location1Y = s.place.bounding_box.coordinates[0][0][1];
                        tt.Location2X = s.place.bounding_box.coordinates[0][1][0];
                        tt.Location2Y = s.place.bounding_box.coordinates[0][1][1];
                        tt.Location3X = s.place.bounding_box.coordinates[0][2][0];
                        tt.Location3Y = s.place.bounding_box.coordinates[0][2][1];
                        tt.Location4X = s.place.bounding_box.coordinates[0][3][0];
                        tt.Location4Y = s.place.bounding_box.coordinates[0][3][1];
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("SaveMentionUpdates", ex);
            }
            return tt;
        }

        public void SaveFavouritesUpdates(List<ResponseTweet> favourites, long accountId)
        {

            if (favourites == null || favourites.Count == 0)
                return;

            using (var dh = new MainDataContext())
            {

                foreach (var s in favourites)
                {

                    var currentId = s.id;

                    if (dh.Favourites.Any(x => x.Id == currentId && x.ProfileId == accountId))
                        continue;

                    var tt = new FavouriteTable
                    {
                        Id = s.id,
                        IdStr = s.id_str,
                        CreatedAt = s.created_at,
                        Description = s.text,
                        ScreenName = s.user.screen_name,
                        DisplayName = s.user.name,
                        CreatedById = s.user.id,
                        ProfileImageUrl = s.user.profile_image_url,
                        ProfileId = accountId,
                        Client = s.source,
                        IsRetweet = false,
                        Verified = (s.user.verified.HasValue) && s.user.verified.Value,
                        InReplyToId = (s.in_reply_to_status_id.HasValue) ? s.in_reply_to_status_id.Value : 0,
                        LanguageCode = s.lang
                    };

                    var res = ResponseEntitiesToTable<FavouritesAssetTable>(s.entities);
                    if (res != null && res.Any())
                        tt.Assets.AddRange(res);

                    if (s.retweeted_status != null && s.retweeted_status.user != null)
                    {
                        // Override the text
                        tt.Description = s.retweeted_status.text;

                        tt.RetweetUserDisplayName = s.retweeted_status.user.name;
                        tt.RetweetUserScreenName = s.retweeted_status.user.screen_name;
                        tt.RetweetUserImageUrl = s.retweeted_status.user.profile_image_url;
                        tt.RetweetUserVerified = (s.retweeted_status.user.verified.HasValue) && s.retweeted_status.user.verified.Value;

                        tt.IsRetweet = true;
                    }

                    // Geo stuff
                    if (s.coordinates != null && s.coordinates.coordinates != null)
                    {
                        tt.LocationFullName = s.coordinates.coordinates[1].ToString(CultureInfo.CurrentUICulture) + ", " + s.coordinates.coordinates[0].ToString(CultureInfo.CurrentUICulture);
                        tt.LocationCountry = string.Empty;

                        tt.Location1X = s.coordinates.coordinates[1];
                        tt.Location1Y = s.coordinates.coordinates[0];
                        tt.Location2X = s.coordinates.coordinates[1];
                        tt.Location2Y = s.coordinates.coordinates[0];
                        tt.Location3X = s.coordinates.coordinates[1];
                        tt.Location3Y = s.coordinates.coordinates[0];
                        tt.Location4X = s.coordinates.coordinates[1];
                        tt.Location4Y = s.coordinates.coordinates[0];
                    }
                    else if (s.geo != null && s.geo.coordinates != null)
                    {
                        tt.LocationFullName = s.geo.coordinates[1].ToString(CultureInfo.CurrentUICulture) + ", " + s.geo.coordinates[0].ToString(CultureInfo.CurrentUICulture);
                        tt.LocationCountry = string.Empty;

                        tt.Location1X = s.geo.coordinates[1];
                        tt.Location1Y = s.geo.coordinates[0];
                        tt.Location2X = s.geo.coordinates[1];
                        tt.Location2Y = s.geo.coordinates[0];
                        tt.Location3X = s.geo.coordinates[1];
                        tt.Location3Y = s.geo.coordinates[0];
                        tt.Location4X = s.geo.coordinates[1];
                        tt.Location4Y = s.geo.coordinates[0];
                    }
                    else if (s.place != null)
                    {
                        tt.LocationFullName = s.place.full_name;
                        tt.LocationCountry = s.place.country;
                        if (s.place.bounding_box != null && s.place.bounding_box.coordinates != null)
                        {
                            tt.Location1X = s.place.bounding_box.coordinates[0][0][0];
                            tt.Location1Y = s.place.bounding_box.coordinates[0][0][1];
                            tt.Location2X = s.place.bounding_box.coordinates[0][1][0];
                            tt.Location2Y = s.place.bounding_box.coordinates[0][1][1];
                            tt.Location3X = s.place.bounding_box.coordinates[0][2][0];
                            tt.Location3Y = s.place.bounding_box.coordinates[0][2][1];
                            tt.Location4X = s.place.bounding_box.coordinates[0][3][0];
                            tt.Location4Y = s.place.bounding_box.coordinates[0][3][1];
                        }
                    }


                    dh.Favourites.InsertOnSubmit(tt);

                }

                dh.SubmitChanges();

            }

        }

        public TimelineTable TweetResponseToTable(long accountId, ResponseTweet s)
        {

            if (s == null)
                return null;

            var tt = new TimelineTable
                         {
                             Id = s.id,
                             IdStr = s.id_str,
                             CreatedAt = s.created_at,
                             Description = s.text,
                             ScreenName = s.user.screen_name,
                             DisplayName = s.user.name,
                             CreatedById = s.user.id,
                             ProfileImageUrl = s.user.profile_image_url,
                             ProfileId = accountId,
                             Client = s.source,
                             IsRetweet = false,
                             Verified = (s.user.verified.HasValue) && s.user.verified.Value,
                             InReplyToId = s.in_reply_to_status_id,
                             LanguageCode = s.lang
                         };


            if (s.retweeted_status != null)
            {
                var res = ResponseEntitiesToTable<TimelineAssetTable>(s.retweeted_status.entities);
                if (res != null && res.Any())
                    tt.Assets.AddRange(res);
            }
            else
            {
                var res = ResponseEntitiesToTable<TimelineAssetTable>(s.entities);
                if (res != null && res.Any())
                    tt.Assets.AddRange(res);
            }

            if (s.retweeted_status != null && s.retweeted_status.user != null)
            {
                // Override the text
                tt.RetweetDescripton = s.retweeted_status.text;

                tt.RetweetUserDisplayName = s.retweeted_status.user.name;
                tt.RetweetUserScreenName = s.retweeted_status.user.screen_name;
                tt.RetweetUserImageUrl = s.retweeted_status.user.profile_image_url;
                tt.RetweetUserVerified = s.retweeted_status.user.verified.HasValue && s.retweeted_status.user.verified.Value;

                tt.IsRetweet = true;
            }


            // Geo stuff
            if (s.place != null)
            {
                tt.LocationFullName = s.place.full_name;
                tt.LocationCountry = s.place.country;

                if (s.place.bounding_box != null && s.place.bounding_box.coordinates != null)
                {
                    tt.Location1X = s.place.bounding_box.coordinates[0][0][0];
                    tt.Location1Y = s.place.bounding_box.coordinates[0][0][1];
                    tt.Location2X = s.place.bounding_box.coordinates[0][1][0];
                    tt.Location2Y = s.place.bounding_box.coordinates[0][1][1];
                    tt.Location3X = s.place.bounding_box.coordinates[0][2][0];
                    tt.Location3Y = s.place.bounding_box.coordinates[0][2][1];
                    tt.Location4X = s.place.bounding_box.coordinates[0][3][0];
                    tt.Location4Y = s.place.bounding_box.coordinates[0][3][1];
                }
            }

            return tt;

        }

        private TwitterSearchTable SearchResponseToTable(ResponseTweet s, long accountId, string searchQuery)
        {

            var tt = new TwitterSearchTable()
            {
                Id = s.id,
                IdStr = s.id_str,
                CreatedAt = s.created_at,
                Description = s.text,
                ScreenName = s.user.screen_name,
                DisplayName = s.user.name,
                CreatedById = s.user.id,
                ProfileImageUrl = s.user.profile_image_url,
                ProfileId = accountId,
                Client = s.source,
                IsRetweet = false,
                SearchQuery = searchQuery,
                LanguageCode = s.lang
            };

            if (s.retweeted_status != null)
            {
                var entities = ResponseEntitiesToTable<TwitterSearchAssetTable>(s.retweeted_status.entities);
                if (entities != null && entities.Any())
                    tt.Assets.AddRange(entities);
            }
            else
            {
                var entities = ResponseEntitiesToTable<TwitterSearchAssetTable>(s.entities);
                if (entities != null && entities.Any())
                    tt.Assets.AddRange(entities);
            }

            if (s.retweeted_status != null && s.retweeted_status.user != null)
            {
                // Override the text
                tt.RetweetDescription = s.retweeted_status.text;

                // Override the created date
                tt.CreatedAt = s.retweeted_status.created_at;

                tt.RetweetUserDisplayName = s.retweeted_status.user.name;
                tt.RetweetUserScreenName = s.retweeted_status.user.screen_name;
                tt.RetweetUserImageUrl = s.retweeted_status.user.profile_image_url;
                tt.RetweetUserVerified = s.retweeted_status.user.verified.HasValue && s.retweeted_status.user.verified.Value;

                tt.IsRetweet = true;
            }

            // Geo stuff
            if (s.coordinates != null && s.coordinates.coordinates != null)
            {
                tt.LocationFullName = s.coordinates.coordinates[1].ToString(CultureInfo.CurrentUICulture) + ", " + s.coordinates.coordinates[0].ToString(CultureInfo.CurrentUICulture);
                tt.LocationCountry = string.Empty;

                tt.Location1X = s.coordinates.coordinates[1];
                tt.Location1Y = s.coordinates.coordinates[0];
                tt.Location2X = s.coordinates.coordinates[1];
                tt.Location2Y = s.coordinates.coordinates[0];
                tt.Location3X = s.coordinates.coordinates[1];
                tt.Location3Y = s.coordinates.coordinates[0];
                tt.Location4X = s.coordinates.coordinates[1];
                tt.Location4Y = s.coordinates.coordinates[0];
            }
            else if (s.geo != null && s.geo.coordinates != null)
            {
                tt.LocationFullName = s.geo.coordinates[1].ToString(CultureInfo.CurrentUICulture) + ", " + s.geo.coordinates[0].ToString(CultureInfo.CurrentUICulture);
                tt.LocationCountry = string.Empty;

                tt.Location1X = s.geo.coordinates[1];
                tt.Location1Y = s.geo.coordinates[0];
                tt.Location2X = s.geo.coordinates[1];
                tt.Location2Y = s.geo.coordinates[0];
                tt.Location3X = s.geo.coordinates[1];
                tt.Location3Y = s.geo.coordinates[0];
                tt.Location4X = s.geo.coordinates[1];
                tt.Location4Y = s.geo.coordinates[0];
            }
            else if (s.place != null)
            {
                tt.LocationFullName = s.place.full_name;
                tt.LocationCountry = s.place.country;
                if (s.place.bounding_box != null && s.place.bounding_box.coordinates != null)
                {
                    tt.Location1X = s.place.bounding_box.coordinates[0][0][0];
                    tt.Location1Y = s.place.bounding_box.coordinates[0][0][1];
                    tt.Location2X = s.place.bounding_box.coordinates[0][1][0];
                    tt.Location2Y = s.place.bounding_box.coordinates[0][1][1];
                    tt.Location3X = s.place.bounding_box.coordinates[0][2][0];
                    tt.Location3Y = s.place.bounding_box.coordinates[0][2][1];
                    tt.Location4X = s.place.bounding_box.coordinates[0][3][0];
                    tt.Location4Y = s.place.bounding_box.coordinates[0][3][1];
                }
            }

            return tt;

        }

        public TimelineTable TimelineResponseToTable(ResponseTweet s, long accountId)
        {

            var tt = new TimelineTable
                         {
                             ProfileId = accountId,
                             Id = s.id,
                             IdStr = s.id_str,
                             CreatedAt = s.created_at,
                             Description = s.text,
                             ScreenName = s.user.screen_name,
                             DisplayName = s.user.name,
                             CreatedById = s.user.id,
                             ProfileImageUrl = s.user.profile_image_url,
                             Client = s.source,
                             IsRetweet = false,
                             Verified = (s.user.verified.HasValue) && s.user.verified.Value,
                             InReplyToId = (s.in_reply_to_status_id.HasValue) ? s.in_reply_to_status_id.Value : 0,
                             LanguageCode = s.lang
                         };

            if (s.retweeted_status != null)
            {

                // Update the source to be that of the original user
                tt.Client = s.retweeted_status.source;

                tt.CreatedAt = s.retweeted_status.created_at;

                if (s.retweeted_status.in_reply_to_status_id.HasValue)
                    tt.InReplyToId = s.retweeted_status.in_reply_to_status_id.Value;

                tt.RetweetOriginalId = s.retweeted_status.id;

                var entities = ResponseEntitiesToTable<TimelineAssetTable>(s.retweeted_status.entities);
                if (entities != null && entities.Any())
                    tt.Assets.AddRange(entities);

            }
            else
            {

                var extendedEntities = ResponseEntitiesToTable<TimelineAssetTable>(s.extended_entities);
                if (extendedEntities != null && extendedEntities.Any())
                    tt.Assets.AddRange(extendedEntities);

                var entities = ResponseEntitiesToTable<TimelineAssetTable>(s.entities);
                if (entities != null && entities.Any())
                {
                    foreach (var entity in entities)
                        if (tt.Assets.All(x => x.ShortValue != entity.ShortValue))
                            tt.Assets.Add(entity);
                }

            }

            if (s.retweeted_status != null && s.retweeted_status.user != null)
            {
                // Override the text
                tt.RetweetDescripton = s.retweeted_status.text;

                tt.RetweetUserDisplayName = s.retweeted_status.user.name;
                tt.RetweetUserScreenName = s.retweeted_status.user.screen_name;
                tt.RetweetUserImageUrl = s.retweeted_status.user.profile_image_url;
                tt.RetweetUserVerified = s.retweeted_status.user.verified.HasValue && s.retweeted_status.user.verified.Value;

                tt.IsRetweet = true;
            }

            // Geo stuff            
            if (s.coordinates != null && s.coordinates.coordinates != null)
            {
                tt.LocationFullName = s.coordinates.coordinates[1].ToString(CultureInfo.CurrentUICulture) + ", " + s.coordinates.coordinates[0].ToString(CultureInfo.CurrentUICulture);
                tt.LocationCountry = string.Empty;

                tt.Location1X = s.coordinates.coordinates[1];
                tt.Location1Y = s.coordinates.coordinates[0];
                tt.Location2X = s.coordinates.coordinates[1];
                tt.Location2Y = s.coordinates.coordinates[0];
                tt.Location3X = s.coordinates.coordinates[1];
                tt.Location3Y = s.coordinates.coordinates[0];
                tt.Location4X = s.coordinates.coordinates[1];
                tt.Location4Y = s.coordinates.coordinates[0];
            }
            else if (s.geo != null && s.geo.coordinates != null && s.geo.coordinates.Length > 2)
            {
                tt.LocationFullName = s.geo.coordinates[1].ToString(CultureInfo.CurrentUICulture) + ", " + s.geo.coordinates[0].ToString(CultureInfo.CurrentUICulture);
                tt.LocationCountry = string.Empty;

                tt.Location1X = s.geo.coordinates[1];
                tt.Location1Y = s.geo.coordinates[0];
                tt.Location2X = s.geo.coordinates[1];
                tt.Location2Y = s.geo.coordinates[0];
                tt.Location3X = s.geo.coordinates[1];
                tt.Location3Y = s.geo.coordinates[0];
                tt.Location4X = s.geo.coordinates[1];
                tt.Location4Y = s.geo.coordinates[0];
            }
            else if (s.place != null)
            {
                tt.LocationFullName = s.place.full_name;
                tt.LocationCountry = s.place.country;
                if (s.place.bounding_box != null)
                {
                    tt.Location1X = s.place.bounding_box.coordinates[0][0][0];
                    tt.Location1Y = s.place.bounding_box.coordinates[0][0][1];
                    tt.Location2X = s.place.bounding_box.coordinates[0][1][0];
                    tt.Location2Y = s.place.bounding_box.coordinates[0][1][1];
                    tt.Location3X = s.place.bounding_box.coordinates[0][2][0];
                    tt.Location3Y = s.place.bounding_box.coordinates[0][2][1];
                    tt.Location4X = s.place.bounding_box.coordinates[0][3][0];
                    tt.Location4Y = s.place.bounding_box.coordinates[0][3][1];
                }
            }

            return tt;
        }

        private RetweetsByMeTable RetweetedByMeResponseToTable(ResponseTweet s, long accountId)
        {

            var tt = new RetweetsByMeTable()
            {
                Id = s.id,
                IdStr = s.id_str,
                CreatedAt = s.created_at,
                Description = s.text,
                ScreenName = s.user.screen_name,
                DisplayName = s.user.name,
                CreatedById = s.user.id,
                ProfileImageUrl = s.user.profile_image_url,
                ProfileId = accountId,
                Client = s.source,
                IsRetweet = false,
                Verified = (s.user.verified.HasValue) && s.user.verified.Value,
                InReplyToId = (s.in_reply_to_status_id.HasValue) ? s.in_reply_to_status_id.Value : 0,
                LanguageCode = s.lang
            };

            if (s.retweeted_status != null && s.retweeted_status.user != null)
            {

                // Override the text
                tt.RetweetDescription = s.retweeted_status.text;
                tt.RetweetUserDisplayName = s.retweeted_status.user.name;
                tt.RetweetUserScreenName = s.retweeted_status.user.screen_name;
                tt.RetweetUserImageUrl = s.retweeted_status.user.profile_image_url;
                tt.RetweetUserVerified = s.retweeted_status.user.verified.HasValue && s.retweeted_status.user.verified.Value;

                tt.IsRetweet = true;

                // Update the source to be that of the original user
                tt.Client = s.retweeted_status.source;

                var res = ResponseEntitiesToTable<RetweetsByMeAssetTable>(s.retweeted_status.entities);
                if (res != null && res.Any())
                    tt.Assets.AddRange(res);

            }
            else
            {

                var res = ResponseEntitiesToTable<RetweetsByMeAssetTable>(s.entities);
                if (res != null && res.Any())
                    tt.Assets.AddRange(res);

            }

            // Geo stuff
            if (s.place != null)
            {
                tt.LocationFullName = s.place.full_name;
                tt.LocationCountry = s.place.country;
                if (s.place.bounding_box != null && s.place.bounding_box.coordinates != null)
                {
                    tt.Location1X = s.place.bounding_box.coordinates[0][0][0];
                    tt.Location1Y = s.place.bounding_box.coordinates[0][0][1];
                    tt.Location2X = s.place.bounding_box.coordinates[0][1][0];
                    tt.Location2Y = s.place.bounding_box.coordinates[0][1][1];
                    tt.Location3X = s.place.bounding_box.coordinates[0][2][0];
                    tt.Location3Y = s.place.bounding_box.coordinates[0][2][1];
                    tt.Location4X = s.place.bounding_box.coordinates[0][3][0];
                    tt.Location4Y = s.place.bounding_box.coordinates[0][3][1];
                }
            }
            else if (s.geo != null && s.geo.coordinates != null)
            {
                tt.LocationFullName = s.geo.coordinates[0].ToString(CultureInfo.CurrentUICulture) + " " + s.geo.coordinates[1].ToString(CultureInfo.CurrentUICulture);
                tt.LocationCountry = string.Empty;

                tt.Location1X = s.geo.coordinates[1];
                tt.Location1Y = s.geo.coordinates[0];
                tt.Location2X = s.geo.coordinates[1];
                tt.Location2Y = s.geo.coordinates[0];
                tt.Location3X = s.geo.coordinates[1];
                tt.Location3Y = s.geo.coordinates[0];
                tt.Location4X = s.geo.coordinates[1];
                tt.Location4Y = s.geo.coordinates[0];
            }

            return tt;

        }

        private RetweetsToMeTable RetweetsToMeResponseToTable(ResponseTweet s, long accountId)
        {

            var tt = new RetweetsToMeTable()
            {
                Id = s.id,
                IdStr = s.id_str,
                CreatedAt = s.created_at,
                Description = s.text,
                ScreenName = s.user.screen_name,
                DisplayName = s.user.name,
                CreatedById = s.user.id,
                ProfileImageUrl = s.user.profile_image_url,
                ProfileId = accountId,
                Client = s.source,
                IsRetweet = false,
                Verified = (s.user.verified.HasValue) && s.user.verified.Value,
                InReplyToId = (s.in_reply_to_status_id.HasValue) ? s.in_reply_to_status_id.Value : 0,
                LanguageCode = s.lang
            };

            if (s.retweeted_status != null && s.retweeted_status.user != null)
            {

                // Override the text
                tt.RetweetDescription = s.retweeted_status.text;
                tt.RetweetUserDisplayName = s.retweeted_status.user.name;
                tt.RetweetUserScreenName = s.retweeted_status.user.screen_name;
                tt.RetweetUserImageUrl = s.retweeted_status.user.profile_image_url;
                tt.RetweetUserVerified = s.retweeted_status.user.verified.HasValue && s.retweeted_status.user.verified.Value;

                tt.IsRetweet = true;

                // Update the source to be that of the original user
                tt.Client = s.retweeted_status.source;

                var res = ResponseEntitiesToTable<RetweetsToMeAssetTable>(s.retweeted_status.entities);
                if (res != null && res.Any())
                    tt.Assets.AddRange(res);

            }
            else
            {
                var res = ResponseEntitiesToTable<RetweetsToMeAssetTable>(s.entities);
                if (res != null && res.Any())
                    tt.Assets.AddRange(res);

            }

            // Geo stuff
            if (s.place != null)
            {
                tt.LocationFullName = s.place.full_name;
                tt.LocationCountry = s.place.country;
                if (s.place.bounding_box != null && s.place.bounding_box.coordinates != null)
                {
                    tt.Location1X = s.place.bounding_box.coordinates[0][0][0];
                    tt.Location1Y = s.place.bounding_box.coordinates[0][0][1];
                    tt.Location2X = s.place.bounding_box.coordinates[0][1][0];
                    tt.Location2Y = s.place.bounding_box.coordinates[0][1][1];
                    tt.Location3X = s.place.bounding_box.coordinates[0][2][0];
                    tt.Location3Y = s.place.bounding_box.coordinates[0][2][1];
                    tt.Location4X = s.place.bounding_box.coordinates[0][3][0];
                    tt.Location4Y = s.place.bounding_box.coordinates[0][3][1];
                }
            }
            else if (s.geo != null && s.geo.coordinates != null)
            {
                tt.LocationFullName = s.geo.coordinates[0].ToString(CultureInfo.CurrentUICulture) + " " + s.geo.coordinates[1].ToString(CultureInfo.CurrentUICulture);
                tt.LocationCountry = string.Empty;

                tt.Location1X = s.geo.coordinates[1];
                tt.Location1Y = s.geo.coordinates[0];
                tt.Location2X = s.geo.coordinates[1];
                tt.Location2Y = s.geo.coordinates[0];
                tt.Location3X = s.geo.coordinates[1];
                tt.Location3Y = s.geo.coordinates[0];
                tt.Location4X = s.geo.coordinates[1];
                tt.Location4Y = s.geo.coordinates[0];
            }

            return tt;

        }


        public RetweetsOfMeTable RetweetsOfMeResponseToTable(ResponseTweet s, long accountId)
        {


            var tt = new RetweetsOfMeTable()
            {
                Id = s.id,
                IdStr = s.id_str,
                CreatedAt = s.created_at,
                Description = s.text,
                ScreenName = s.user.screen_name,
                DisplayName = s.user.name,
                CreatedById = s.user.id,
                ProfileImageUrl = s.user.profile_image_url,
                ProfileId = accountId,
                Client = s.source,
                IsRetweet = false,
                Verified = (s.user.verified.HasValue) && s.user.verified.Value,
                InReplyToId = (s.in_reply_to_status_id.HasValue) ? s.in_reply_to_status_id.Value : 0,
                LanguageCode = s.lang
            };

            if (s.retweeted_status != null && s.retweeted_status.user != null)
            {

                // Override the text
                tt.RetweetDescription = s.retweeted_status.text;
                tt.RetweetUserDisplayName = s.retweeted_status.user.name;
                tt.RetweetUserScreenName = s.retweeted_status.user.screen_name;
                tt.RetweetUserImageUrl = s.retweeted_status.user.profile_image_url;
                tt.RetweetUserVerified = s.retweeted_status.user.verified.HasValue && s.retweeted_status.user.verified.Value;

                tt.IsRetweet = true;

                // Update the source to be that of the original user
                tt.Client = s.retweeted_status.source;

                var res = ResponseEntitiesToTable<RetweetsOfMeAssetTable>(s.retweeted_status.entities);
                if (res != null && res.Any())
                    tt.Assets.AddRange(res);

            }
            else
            {

                var res = ResponseEntitiesToTable<RetweetsOfMeAssetTable>(s.entities);
                if (res != null && res.Any())
                    tt.Assets.AddRange(res);

            }


            // Geo stuff
            if (s.place != null)
            {
                tt.LocationFullName = s.place.full_name;
                tt.LocationCountry = s.place.country;
                if (s.place.bounding_box != null && s.place.bounding_box.coordinates != null)
                {
                    tt.Location1X = s.place.bounding_box.coordinates[0][0][0];
                    tt.Location1Y = s.place.bounding_box.coordinates[0][0][1];
                    tt.Location2X = s.place.bounding_box.coordinates[0][1][0];
                    tt.Location2Y = s.place.bounding_box.coordinates[0][1][1];
                    tt.Location3X = s.place.bounding_box.coordinates[0][2][0];
                    tt.Location3Y = s.place.bounding_box.coordinates[0][2][1];
                    tt.Location4X = s.place.bounding_box.coordinates[0][3][0];
                    tt.Location4Y = s.place.bounding_box.coordinates[0][3][1];
                }
            }
            else if (s.geo != null && s.geo.coordinates != null)
            {
                tt.LocationFullName = s.geo.coordinates[0].ToString(CultureInfo.CurrentUICulture) + " " + s.geo.coordinates[1].ToString(CultureInfo.CurrentUICulture);
                tt.LocationCountry = string.Empty;

                tt.Location1X = s.geo.coordinates[1];
                tt.Location1Y = s.geo.coordinates[0];
                tt.Location2X = s.geo.coordinates[1];
                tt.Location2Y = s.geo.coordinates[0];
                tt.Location3X = s.geo.coordinates[1];
                tt.Location3Y = s.geo.coordinates[0];
                tt.Location4X = s.geo.coordinates[1];
                tt.Location4Y = s.geo.coordinates[0];
            }

            return tt;
        }


        public bool SaveRetweetedByMeUpdates(long accountId, List<ResponseTweet> retweetedByMe)
        {

            if (retweetedByMe == null || !retweetedByMe.Any())
                return false;

            bool returnValue = false;

            try
            {

                using (var dh = new MainDataContext())
                {

                    var newItems = new List<RetweetsByMeTable>();

                    foreach (var s in retweetedByMe)
                    {

                        var currentId = s.id;

                        if (dh.RetweetedByMe.Any(x => currentId == x.Id && x.ProfileId == accountId))
                        {
                            returnValue = true;
                            continue;
                        }

                        if (!newItems.Any(x => x.Id == s.id && x.ProfileId == accountId))
                        {
                            newItems.Add(RetweetedByMeResponseToTable(s, accountId));
                        }

                    }

                    dh.RetweetedByMe.InsertAllOnSubmit(newItems);
                    dh.SubmitChanges();

                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("SaveRetweetedByMeUpdates", ex);
            }

            return returnValue;

        }

        public bool SaveRetweetedToMeUpdates(long accountId, List<ResponseTweet> retweetsToMe)
        {

            if (retweetsToMe == null || !retweetsToMe.Any())
                return false;

            bool returnValue = false;

            try
            {

                using (var dh = new MainDataContext())
                {

                    var newItems = new List<RetweetsToMeTable>();

                    foreach (var s in retweetsToMe)
                    {

                        var currentId = s.id;

                        if (dh.RetweetsToMe.Any(x => currentId == x.Id && x.ProfileId == accountId))
                        {
                            returnValue = true;
                            continue;
                        }

                        if (!newItems.Any(x => x.Id == s.id && x.ProfileId == accountId))
                        {
                            newItems.Add(RetweetsToMeResponseToTable(s, accountId));
                        }

                    }

                    dh.RetweetsToMe.InsertAllOnSubmit(newItems);

                    dh.SubmitChanges();

                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("SaveRetweetedToMeUpdates", ex);
            }

            return returnValue;

        }


        public bool SaveRetweetsOfMeUpdates(long accountId, List<ResponseTweet> retweetsOfMe)
        {

            if (retweetsOfMe == null || !retweetsOfMe.Any())
                return false;

            bool returnValue = false;

            try
            {

                using (var dh = new MainDataContext())
                {

                    var newItems = new List<RetweetsOfMeTable>();

                    foreach (var s in retweetsOfMe)
                    {

                        var currentId = s.id;

                        if (dh.RetweetsOfMe.Any(x => currentId == x.Id && x.ProfileId == accountId))
                        {
                            returnValue = true;
                            continue;
                        }

                        if (!newItems.Any(x => x.Id == s.id && x.ProfileId == accountId))
                        {
                            newItems.Add(RetweetsOfMeResponseToTable(s, accountId));
                        }

                    }

                    dh.RetweetsOfMe.InsertAllOnSubmit(newItems);

                    dh.SubmitChanges();

                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("SaveRetweetsOfMeUpdates", ex);
            }

            return returnValue;

        }

        public bool SaveTwitterSearches(long accountId, string searchQuery, List<ResponseTweet> results)
        {

            if (results == null || !results.Any())
                return false;

            bool returnValue = false;

            try
            {

                using (var dh = new MainDataContext())
                {

                    var newItems = new List<TwitterSearchTable>();

                    foreach (var s in results)
                    {

                        var currentId = s.id;

                        if (dh.TwitterSearch.Any(x => currentId == x.Id && x.ProfileId == accountId && x.SearchQuery == searchQuery))
                        {
                            returnValue = true;
                            continue;
                        }

                        if (!newItems.Any(x => x.Id == s.id && x.ProfileId == accountId && x.SearchQuery == searchQuery))
                        {
                            newItems.Add(SearchResponseToTable(s, accountId, searchQuery));
                        }

                    }

                    dh.TwitterSearch.InsertAllOnSubmit(newItems);

                    dh.SubmitChanges();

                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("SaveTwitterSearches", ex);
            }

            return returnValue;

        }

        public bool SaveTimelineUpdates(long accountId, List<ResponseTweet> timelines)
        {

            /*
            
             * Code to resolve URLS
                         
            // bit.ly/p9Y77u
            var r = System.Net.WebRequest.Create(new Uri("http://bit.ly/p9Y77u", UriKind.Absolute));
            r.Method = "HEAD";            
            WebResponse res = r.GetResponse();
             * 
             */

            if (timelines == null || !timelines.Any())
                return false;

            bool returnValue = false;

            //string memoryUsage1 = (DeviceStatus.ApplicationCurrentMemoryUsage / 1000000).ToString() + "MB\n";

            // try this
            //GC.Collect();

            //string memoryUsage2 = (DeviceStatus.ApplicationCurrentMemoryUsage / 1000000).ToString() + "MB\n";

            //string memory = string.Format("{0} -> {1}", memoryUsage1, memoryUsage2);

            try
            {

                using (var dh = new MainDataContext())
                {

                    //dh.Log = new DataLogger();

                    //var newItems = new List<ResponseTweet>();

                    var newItems = timelines.Where(x => dh.Timeline.All(y => y.Id != x.id && y.ProfileId != x.user.id)).ToList();

                    //foreach (ResponseTweet s in timelines)
                    //{



                    //    if (dh.Timeline.Any(x => x.Id == s.id && x.ProfileId == accountId))
                    //    {
                    //        returnValue = true;
                    //        continue;
                    //    }

                    //    if (!newItems.Any(x => x.id == s.id && x.user.id == accountId))
                    //    {
                    //        newItems.Add(s);
                    //    }

                    //}

                    var items = newItems.Select(x => TimelineResponseToTable(x, accountId));
                    dh.Timeline.InsertAllOnSubmit(items);
                    dh.SubmitChanges();

                    dh.Dispose();

                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("SaveTimelineUpdates", ex);
            }

            return returnValue;

        }

        private static readonly object SaveMessagesUpdateLock = new object();

        public void SaveMessagesUpdate(IList<ResponseDirectMessage> iList, long accountId)
        {

            if (iList == null || iList.Count == 0)
                return;

            lock (SaveMessagesUpdateLock)
            {

                try
                {

                    using (var dh = new MainDataContext())
                    {

                        var newItems = new List<MessageTable>();

                        foreach (var s in iList)
                        {

                            var currentId = s.id_str;

                            if (dh.Messages.Count(x => x.IdStr == currentId) > 0)
                                continue;

                            if (newItems.Any(x => x.IdStr == currentId))
                                continue;

                            var tt = new MessageTable()
                            {
                                CreatedAt = s.created_at,
                                CreatedById = s.sender.id,
                                Description = s.text,
                                DisplayName = s.sender.name,
                                Id = s.id,
                                IdStr = s.id_str,
                                ProfileId = accountId,
                                ProfileImageUrl = s.sender.profile_image_url,
                                ScreenName = s.sender.screen_name,
                                Verified = s.sender.verified.HasValue && s.sender.verified.Value,
                                LanguageCode = s.lang
                            };

                            var res = ResponseEntitiesToTable<MessageAssetTable>(s.entities);
                            if (res != null && res.Any())
                                tt.Assets.AddRange(res);

                            newItems.Add(tt);

                        }

                        if (newItems.Any())
                        {
                            dh.Messages.InsertAllOnSubmit(newItems);
                            dh.SubmitChanges();
                        }

                    }

                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException("SaveMesssagesUpdate", ex);
                }

            }

        }

        public void ResetShellStatus()
        {

            using (var dh = new MainDataContext())
            {
                if (!dh.ShellStatus.Any())
                    return;

                try
                {

                    var res = dh.ShellStatus.FirstOrDefault();
                    if (res != null)
                    {
                        res.MentionCount = 0;
                        res.MessageCount = 0;

                        dh.SubmitChanges();
                    }

                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException("ResetShellStatus", ex);
                }
            }

        }


        public static T GetResponseObject<T>(string cachedContent)
        {

            try
            {
                return JsonConvert.DeserializeObject<T>(cachedContent);
            }
            catch (Exception)
            {
                return default(T);
            }

            //var dcSerializer = new DataContractSerializer(typeof(T));

            //using (var sr = new StringReader(cachedContent))
            //{
            //    using (var xReader = XmlReader.Create(sr))
            //    {
            //        return (T)dcSerializer.ReadObject(xReader);
            //    }
            //}

        }

        public static string SerialiseResponseObject<T>(T settings)
        {

            return JsonConvert.SerializeObject(settings);

            //var serialXml = new StringBuilder();
            //var dcSerializer = new DataContractSerializer(typeof(T));
            //using (var xWriter = XmlWriter.Create(serialXml))
            //{
            //    dcSerializer.WriteObject(xWriter, settings);
            //    xWriter.Flush();
            //    return serialXml.ToString();
            //}

        }

        public T LoadExistingState<T>(string fileName)
        {

            var contents = LoadContentsFromFile(fileName);
            if (!string.IsNullOrWhiteSpace(contents))
            {
                var results = GetResponseObject<T>(contents);
                return results;
            }

            return default(T);
        }

        public List<ResponseTweet> LoadExistingMentionState(long accountId)
        {
            return LoadExistingState<List<ResponseTweet>>(string.Format(ApplicationConstants.StateMentions, accountId));
        }

        public List<ResponseDirectMessage> LoadExistingMessagesState(long accountId)
        {
            return LoadExistingState<List<ResponseDirectMessage>>(string.Format(ApplicationConstants.StateMessages, accountId));
        }

        public void RemoveState(string filename)
        {
            try
            {
                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (storage.FileExists(filename))
                        storage.DeleteFile(filename);
                }
            }
#if LOGGING
            catch (Exception ex)
            {
                ErrorLogger.LogException("RemoveState", ex);
            }
#endif
            finally
            {

            }

        }

        public string LoadContentsFromFile(string filePath)
        {
            string contents = string.Empty;

            try
            {

                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {

                    if (storage.FileExists(filePath))
                    {
                        using (var newFile = storage.OpenFile(filePath, FileMode.Open))
                        {
                            using (var reader = new StreamReader(newFile))
                            {
                                contents = reader.ReadToEnd();
                            }
                        }
                    }
                }

            }
            catch (Exception)
            {
                contents = string.Empty;
            }


            return contents;
        }

        public void SaveMentionUpdatesSerialised(List<ResponseTweet> mentions, long accountId)
        {
            var contents = SerialiseResponseObject(mentions);
            SaveContentsToFile(string.Format(ApplicationConstants.StateMentions, accountId), contents);
        }

        public void SaveMessagesUpdateSerialised(List<ResponseDirectMessage> directMessages, long accountId)
        {
            var contents = SerialiseResponseObject(directMessages);
            SaveContentsToFile(string.Format(ApplicationConstants.StateMessages, accountId), contents);
        }

        public static void SaveContentsToFile(string filePath, string contents)
        {

            using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var newFile = storage.OpenFile(filePath, FileMode.Create))
                {
                    using (var writer = new StreamWriter(newFile))
                    {
                        writer.Write(contents);
                        writer.Flush();
                    }
                }
            }
        }

        public void MoveMessageStateToDb(long accountId)
        {
            var res = LoadExistingMessagesState(accountId);
            if (res != null)
            {
                SaveMessagesUpdate(res, accountId);
            }

            try
            {
                RemoveState(string.Format(ApplicationConstants.StateMessages, accountId));
            }
            catch (Exception)
            {
            }

        }

        public void MoveMentionStateToDb(long accountId)
        {
            var res = LoadExistingMentionState(accountId);
            if (res != null)
            {
                SaveMentionUpdates(res, accountId);
            }

            try
            {
                RemoveState(string.Format(ApplicationConstants.StateMentions, accountId));
            }
            catch
            {
            }

        }

        #region lists

        public TwitterListTable TwitterListResponseToTable(long accountId, ResponseTweet s)
        {

            var tt = new TwitterListTable()
            {
                Id = s.id,
                IdStr = s.id_str,
                CreatedAt = s.created_at,
                Description = s.text,
                ScreenName = s.user.screen_name,
                DisplayName = s.user.name,
                CreatedById = s.user.id,
                ProfileImageUrl = s.user.profile_image_url,
                ProfileId = accountId,
                Client = s.source,
                IsRetweet = false,
                Verified = (s.user.verified.HasValue) && s.user.verified.Value,
                InReplyToId = (s.in_reply_to_status_id.HasValue) ? s.in_reply_to_status_id.Value : 0
            };

            if (s.retweeted_status != null)
            {

                // Override the text
                tt.RetweetDescription = s.retweeted_status.text;

                tt.RetweetOriginalId = s.retweeted_status.id;

                // Override the created date
                tt.CreatedAt = s.retweeted_status.created_at;

                if (s.retweeted_status.user != null)
                {
                    tt.RetweetUserDisplayName = s.retweeted_status.user.name;
                    tt.RetweetUserScreenName = s.retweeted_status.user.screen_name;
                    tt.RetweetUserImageUrl = s.retweeted_status.user.profile_image_url;
                    tt.RetweetUserVerified = s.retweeted_status.user.verified.HasValue && s.retweeted_status.user.verified.Value;

                    tt.IsRetweet = true;
                }

                var res = ResponseEntitiesToTable<TwitterListAssetTable>(s.retweeted_status.entities);
                if (res != null && res.Any())
                    tt.Assets.AddRange(res);

            }
            else
            {

                var res = ResponseEntitiesToTable<TwitterListAssetTable>(s.entities);
                if (res != null && res.Any())
                    tt.Assets.AddRange(res);

            }

            // Geo stuff
            if (s.place != null)
            {
                tt.LocationFullName = s.place.full_name;
                tt.LocationCountry = s.place.country;
                if (s.place.bounding_box != null)
                {
                    tt.Location1X = s.place.bounding_box.coordinates[0][0][0];
                    tt.Location1Y = s.place.bounding_box.coordinates[0][0][1];
                    tt.Location2X = s.place.bounding_box.coordinates[0][1][0];
                    tt.Location2Y = s.place.bounding_box.coordinates[0][1][1];
                    tt.Location3X = s.place.bounding_box.coordinates[0][2][0];
                    tt.Location3Y = s.place.bounding_box.coordinates[0][2][1];
                    tt.Location4X = s.place.bounding_box.coordinates[0][3][0];
                    tt.Location4Y = s.place.bounding_box.coordinates[0][3][1];
                }
            }
            else if (s.geo != null && s.geo.coordinates != null)
            {
                tt.LocationFullName = s.geo.coordinates[0].ToString(CultureInfo.CurrentUICulture) + " " + s.geo.coordinates[1].ToString(CultureInfo.CurrentUICulture);
                tt.LocationCountry = string.Empty;

                tt.Location1X = s.geo.coordinates[1];
                tt.Location1Y = s.geo.coordinates[0];
                tt.Location2X = s.geo.coordinates[1];
                tt.Location2Y = s.geo.coordinates[0];
                tt.Location3X = s.geo.coordinates[1];
                tt.Location3Y = s.geo.coordinates[0];
                tt.Location4X = s.geo.coordinates[1];
                tt.Location4Y = s.geo.coordinates[0];
            }

            return tt;

        }

        public bool SaveTwitterListUpdates(long accountId, List<ResponseTweet> twitterList, string slug)
        {

            if (twitterList == null || !twitterList.Any())
                return false;

            bool returnValue = false;

            try
            {

                using (var dh = new MainDataContext())
                {

                    var newItems = new List<TwitterListTable>();

                    foreach (var s in twitterList.OrderBy(x => x.id))
                    {

                        var currentId = s.id;

                        if (dh.TwitterList.Any(x => currentId == x.Id && x.ProfileId == accountId))
                        {
                            returnValue = true;
                            continue;
                        }

                        if (!newItems.Any(x => x.Id == s.id && x.ProfileId == accountId))
                        {
                            var newItem = TwitterListResponseToTable(accountId, s);
                            newItem.ListId = slug;
                            newItems.Add(newItem);
                        }
                    }

                    dh.TwitterList.InsertAllOnSubmit(newItems);

                    dh.SubmitChanges();

                }

            }
            catch (Exception ex)
            {
                ErrorLogger.LogException("SaveTwitterListUpdates", ex);
            }

            return returnValue;

        }

        #endregion

        public void DeleteTweetFromStorage(long accountId, long tweetId)
        {

            // TODO: Check if this is timeline or mention
            using (var dh = new MainDataContext())
            {

                var tweets = dh.Timeline.Where(x => x.Id == tweetId);

                if (tweets.Any())
                {
                    dh.Timeline.DeleteAllOnSubmit(tweets);
                }

                var mentions = dh.Mentions.Where(x => x.Id == tweetId);

                if (mentions.Any())
                {
                    dh.Mentions.DeleteAllOnSubmit(mentions);
                }

                var messages = dh.Messages.Where(x => x.Id == tweetId);

                if (messages.Any())
                {
                    dh.Messages.DeleteAllOnSubmit(messages);
                }

                dh.SubmitChanges();

            }


        }

        private List<T> ResponseEntitiesToTable<T>(ResponseEntities entities) where T : IAssetTable, new()
        {

            if (entities == null)
                return null;

            var res = new List<T>();

            if (entities.hashtags != null && entities.hashtags.Any())
            {
                res.AddRange(entities.hashtags.Select(asset => new T
                {
                    ShortValue = asset.text,
                    LongValue = asset.text,
                    StartOffset = asset.indices[0],
                    EndOffset = asset.indices[1],
                    Type = AssetTypeEnum.Hashtag
                }));
            }

            if (entities.media != null && entities.media.Any())
            {
                res.AddRange(entities.media.Select(asset => new T
                {
                    ShortValue = asset.display_url,
                    LongValue = (string.IsNullOrEmpty(asset.media_url) ? asset.expanded_url : asset.media_url),
                    StartOffset = asset.indices[0],
                    EndOffset = asset.indices[1],
                    Type = AssetTypeEnum.Media
                }));
            }

            if (entities.urls != null && entities.urls.Any())
            {
                foreach (var url in entities.urls)
                {
                    // 
                    if (!res.Any(x => x.Type == AssetTypeEnum.Media && x.StartOffset == url.indices[0]))
                    {

                        res.Add(new T()
                                {
                                    ShortValue = url.url,
                                    LongValue = url.expanded_url,
                                    StartOffset = url.indices[0],
                                    EndOffset = url.indices[1],
                                    Type = AssetTypeEnum.Url
                                });
                    }
                }
            }

            if (entities.user_mentions != null && entities.user_mentions.Any())
            {
                res.AddRange(entities.user_mentions.Select(asset => new T
                {
                    ShortValue = asset.screen_name,
                    LongValue = asset.name,
                    StartOffset = asset.indices[0],
                    EndOffset = asset.indices[1],
                    Type = AssetTypeEnum.Mention
                }));
            }

            return res;

        }

        private static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return origin.AddSeconds(timestamp);
        }

        public void DeleteFile(string filename)
        {
            try
            {
                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (storage.FileExists(filename))
                        storage.DeleteFile(filename);
                }
            }
            catch (Exception)
            {
            }
        }

        public void DeleteFavourites(long accountId)
        {

            try
            {
                using (var dh = new MainDataContext())
                {
                    var favourites = dh.Favourites.Where(x => x.ProfileId == accountId);
                    dh.Favourites.DeleteAllOnSubmit(favourites);
                    dh.SubmitChanges();
                }
            }
            catch (Exception)
            {
            }

        }

    }

}
