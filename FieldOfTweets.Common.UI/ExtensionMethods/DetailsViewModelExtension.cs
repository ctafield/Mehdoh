// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.UI.ViewModels;

namespace FieldOfTweets.Common.UI.ExtensionMethods
{
    public static class DetailsPageViewModelExtensions
    {
        public static DetailsPageViewModel AsDetailsViewModel(this ResponseTweet status, long accountId)
        {
            var item = new DetailsPageViewModel
            {
                AccountId = accountId,
                ScreenName = status.user.screen_name,
                DisplayName = status.user.name,
                Description = status.text,
                ProfileImageUrl = status.user.profile_image_url,
                Id = status.id,
                IsRetweet = false,
                InReplyToId = status.in_reply_to_status_id,
                Client = status.source,
                LanguageCode = status.lang
            };

            const string format = "ddd MMM dd HH:mm:ss zzzz yyyy";

            try
            {
                var createdAtDate = DateTime.ParseExact(status.created_at, format, CultureInfo.InvariantCulture);
                item.CreatedAt = createdAtDate;
            }
            catch (Exception)
            {
                // ignore
            }

            try
            {
                if (status.retweeted_status != null && status.retweeted_status.user != null && !string.IsNullOrEmpty(status.retweeted_status.text))
                {
                    item.IsRetweet = true;
                    item.DisplayName = status.retweeted_status.user.name;
                    item.ScreenName = status.retweeted_status.user.screen_name;
                    item.ProfileImageUrl = status.retweeted_status.user.profile_image_url;
                    item.RetweetDescription = status.retweeted_status.text;
                    item.OriginalRetweetId = status.retweeted_status.id;

                    try
                    {
                        var createdAtDate = DateTime.ParseExact(status.retweeted_status.created_at, format, CultureInfo.InvariantCulture);
                        item.CreatedAt = createdAtDate;
                    }
                    catch (Exception)
                    {
                        // ignore
                    }

                    item.Verified = (status.retweeted_status.user.verified.HasValue &&
                                     status.retweeted_status.user.verified.Value);

                    // These are the person that retweeted
                    item.RetweetDisplayName = status.user.name;
                    item.RetweetScreenName = status.user.screen_name;

                    item.Client = status.retweeted_status.source;
                }
            }
            catch (Exception)
            {
                // ignore                
            }

            item.Assets = new List<AssetViewModel>();

            try
            {
                if (item.IsRetweet)
                {
                    if (status.retweeted_status != null)
                    {
                        #region retweet entities

                        if (status.retweeted_status.entities != null)
                        {
                            if (status.retweeted_status.entities.hashtags != null && status.retweeted_status.entities.hashtags.Length > 0)
                            {
                                foreach (var entity in status.retweeted_status.entities.hashtags.Select(asset => new AssetViewModel
                                {
                                    ShortValue = asset.text,
                                    LongValue = asset.text,
                                    StartOffset = asset.indices[0],
                                    EndOffset = asset.indices[1],
                                    Type = AssetTypeEnum.Hashtag
                                }))
                                {
                                    item.Assets.Add(entity);
                                }
                            }

                            if (status.retweeted_status.entities.user_mentions != null &&
                                status.retweeted_status.entities.user_mentions.Length > 0)
                            {
                                foreach (var entity in status.retweeted_status.entities.user_mentions.Select(
                                            asset => new AssetViewModel
                                            {
                                                Type = AssetTypeEnum.Mention,
                                                ShortValue = asset.screen_name,
                                                LongValue = asset.name,
                                                StartOffset = asset.indices[0],
                                                EndOffset = asset.indices[1],
                                            }))
                                {
                                    item.Assets.Add(entity);
                                }
                            }


                            if (status.retweeted_status.entities.media != null &&
                                status.retweeted_status.entities.media.Length > 0)
                            {
                                foreach (var entity in status.retweeted_status.entities.media.Select(asset => new AssetViewModel
                                {
                                    ShortValue = asset.display_url,
                                    LongValue = (string.IsNullOrEmpty(asset.media_url) ? asset.expanded_url : asset.media_url),
                                    StartOffset = asset.indices[0],
                                    EndOffset = asset.indices[1],
                                    Type = AssetTypeEnum.Media
                                }))
                                {
                                    item.Assets.Add(entity);
                                }
                            }

                            if (status.retweeted_status.entities.urls != null &&
                                status.retweeted_status.entities.urls.Length > 0)
                            {
                                foreach (var model in status.retweeted_status.entities.urls.Select(asset => new AssetViewModel
                                        {
                                            ShortValue = asset.url,
                                            LongValue = asset.expanded_url,
                                            StartOffset = asset.indices[0],
                                            EndOffset = asset.indices[1],
                                            Type = AssetTypeEnum.Url
                                        }))
                                {
                                    item.Assets.Add(model);
                                }
                            }
                        }

                        #endregion
                    }
                }
                else
                {

                    #region extended entities

                    if (status.extended_entities != null)
                    {
                        if (status.extended_entities.media != null && status.extended_entities.media.Length > 0)
                        {
                            foreach (var entity in status.extended_entities.media.Select(asset => new AssetViewModel
                            {
                                ShortValue = asset.display_url,
                                LongValue = (string.IsNullOrEmpty(asset.media_url) ? asset.expanded_url : asset.media_url),
                                StartOffset = asset.indices[0],
                                EndOffset = asset.indices[1],
                                Type = AssetTypeEnum.Media
                            }))
                            {
                                item.Assets.Add(entity);
                            }
                        }
                    }

                    #endregion

                    #region entities

                    if (status.entities != null)
                    {
                        if (status.entities.hashtags != null && status.entities.hashtags.Length > 0)
                        {
                            foreach (var entity in status.entities.hashtags.Select(asset => new AssetViewModel
                            {
                                ShortValue = asset.text,
                                LongValue = asset.text,
                                StartOffset = asset.indices[0],
                                EndOffset = asset.indices[1],
                                Type = AssetTypeEnum.Hashtag
                            }))
                            {
                                item.Assets.Add(entity);
                            }
                        }

                        if (status.entities.user_mentions != null && status.entities.user_mentions.Length > 0)
                        {
                            foreach (var entity in status.entities.user_mentions.Select(asset => new AssetViewModel
                            {
                                Type = AssetTypeEnum.Mention,
                                ShortValue = asset.screen_name,
                                LongValue = asset.name,
                                StartOffset = asset.indices[0],
                                EndOffset = asset.indices[1],
                            }))
                            {
                                item.Assets.Add(entity);
                            }
                        }

                        if (status.entities.media != null && status.entities.media.Length > 0)
                        {
                            foreach (var entity in status.entities.media.Select(asset => new AssetViewModel
                            {
                                ShortValue = asset.display_url,
                                LongValue = (string.IsNullOrEmpty(asset.media_url) ? asset.expanded_url : asset.media_url),
                                StartOffset = asset.indices[0],
                                EndOffset = asset.indices[1],
                                Type = AssetTypeEnum.Media
                            }))
                            {
                                if (item.Assets.All(x => x.ShortValue != entity.ShortValue))
                                    item.Assets.Add(entity);
                            }
                        }

                        if (status.entities.urls != null && status.entities.urls.Length > 0)
                        {
                            foreach (var model in status.entities.urls.Select(asset => new AssetViewModel
                            {
                                ShortValue = asset.url,
                                LongValue = asset.expanded_url,
                                StartOffset = asset.indices[0],
                                EndOffset = asset.indices[1],
                                Type = AssetTypeEnum.Url
                            }))
                            {
                                item.Assets.Add(model);
                            }
                        }
                    }

                    #endregion
                }
            }
            catch (Exception)
            {
                // ignore
            }


            // Geo stuff
            try
            {
                if (status.coordinates != null && status.coordinates.coordinates != null)
                {
                    item.LocationFull = status.coordinates.coordinates[1].ToString(CultureInfo.CurrentUICulture) + ", " +
                                        status.coordinates.coordinates[0].ToString(CultureInfo.CurrentUICulture);

                    item.Location1X = status.coordinates.coordinates[1];
                    item.Location1Y = status.coordinates.coordinates[0];
                    item.Location2X = status.coordinates.coordinates[1];
                    item.Location2Y = status.coordinates.coordinates[0];
                    item.Location3X = status.coordinates.coordinates[1];
                    item.Location3Y = status.coordinates.coordinates[0];
                    item.Location4X = status.coordinates.coordinates[1];
                    item.Location4Y = status.coordinates.coordinates[0];
                }
                else if (status.geo != null && status.geo.coordinates != null)
                {
                    item.LocationFull = status.geo.coordinates[1].ToString(CultureInfo.CurrentUICulture) + ", " +
                                        status.geo.coordinates[0].ToString(CultureInfo.CurrentUICulture);

                    item.Location1X = status.geo.coordinates[1];
                    item.Location1Y = status.geo.coordinates[0];
                    item.Location2X = status.geo.coordinates[1];
                    item.Location2Y = status.geo.coordinates[0];
                    item.Location3X = status.geo.coordinates[1];
                    item.Location3Y = status.geo.coordinates[0];
                    item.Location4X = status.geo.coordinates[1];
                    item.Location4Y = status.geo.coordinates[0];
                }
                else if (status.place != null)
                {
                    item.LocationFull = status.place.full_name;

                    if (status.place.bounding_box != null && status.place.bounding_box.coordinates != null)
                    {
                        item.Location1X = status.place.bounding_box.coordinates[0][0][0];
                        item.Location1Y = status.place.bounding_box.coordinates[0][0][1];
                        item.Location2X = status.place.bounding_box.coordinates[0][1][0];
                        item.Location2Y = status.place.bounding_box.coordinates[0][1][1];
                        item.Location3X = status.place.bounding_box.coordinates[0][2][0];
                        item.Location3Y = status.place.bounding_box.coordinates[0][2][1];
                        item.Location4X = status.place.bounding_box.coordinates[0][3][0];
                        item.Location4Y = status.place.bounding_box.coordinates[0][3][1];
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }


            return item;
        }


        public static DetailsPageViewModel AsDetailsViewModel(this ITweetTable thisTweet, bool? isSearchTweet = false)
        {
            var details = new DetailsPageViewModel
            {
                Id = thisTweet.Id,
                AccountId = thisTweet.ProfileId,
                DisplayName = thisTweet.DisplayName,
                ScreenName = thisTweet.ScreenName,
                ProfileImageUrl = thisTweet.ProfileImageUrl,
                Description = thisTweet.Description,
                CreatedAt = thisTweet.CreatedAtFormatted,
                Client = FormatHtml(thisTweet.Client),
                Verified = thisTweet.Verified,
                InReplyToId = thisTweet.InReplyToId,
                LocationFull = thisTweet.LocationFullName + " " + thisTweet.LocationCountry,
                Location1X = thisTweet.Location1X,
                Location1Y = thisTweet.Location1Y,
                Location2X = thisTweet.Location2X,
                Location2Y = thisTweet.Location2Y,
                Location3X = thisTweet.Location3X,
                Location3Y = thisTweet.Location3Y,
                Location4X = thisTweet.Location4X,
                Location4Y = thisTweet.Location4Y,
                LanguageCode = thisTweet.LanguageCode
            };

            if (thisTweet.IsRetweet)
            {
                details.IsRetweet = true;
                details.DisplayName = thisTweet.RetweetUserDisplayName;
                details.ScreenName = thisTweet.RetweetUserScreenName;
                details.ProfileImageUrl = thisTweet.RetweetUserImageUrl;
                details.RetweetDescription = thisTweet.RetweetDescription;

                details.OriginalRetweetId = thisTweet.RetweetOriginalId;

                // These are the person that retweeted
                details.RetweetDisplayName = thisTweet.DisplayName;
                details.RetweetScreenName = thisTweet.ScreenName;
                details.Verified = thisTweet.RetweetUserVerified;
            }

            IEnumerable<IAssetTable> assets = null;

            // Assets
            if (thisTweet is TimelineTable)
            {
                details.TweetType = TweetTypeEnum.Timeline;
                var table = (TimelineTable)thisTweet;
                table.Assets.Load();
                if (table.Assets.HasLoadedOrAssignedValues && table.Assets != null)
                {
                    assets = table.Assets.Cast<IAssetTable>();
                }
            }
            else if (thisTweet is MentionTable)
            {
                details.TweetType = TweetTypeEnum.Mention;
                var table = (MentionTable)thisTweet;
                table.Assets.Load();
                if (table.Assets.HasLoadedOrAssignedValues && table.Assets != null)
                {
                    assets = table.Assets.Cast<IAssetTable>();
                }
            }
            else if (thisTweet is MessageTable)
            {
                details.TweetType = TweetTypeEnum.Message;
                var table = (MessageTable)thisTweet;
                table.Assets.Load();
                if (table.Assets.HasLoadedOrAssignedValues && table.Assets != null)
                {
                    assets = table.Assets.Cast<IAssetTable>();
                }
            }
            else if (thisTweet is FavouriteTable)
            {
                details.TweetType = TweetTypeEnum.Favourite;
                var table = (FavouriteTable)thisTweet;
                table.Assets.Load();
                if (table.Assets.HasLoadedOrAssignedValues && table.Assets != null)
                {
                    assets = table.Assets.Cast<IAssetTable>();
                }
            }
            else if (thisTweet is RetweetsByMeTable)
            {
                details.TweetType = TweetTypeEnum.RetweetByMe;
                var table = (RetweetsByMeTable)thisTweet;
                table.Assets.Load();
                if (table.Assets.HasLoadedOrAssignedValues && table.Assets != null)
                {
                    assets = table.Assets.Cast<IAssetTable>();
                }
            }
            else if (thisTweet is RetweetsOfMeTable)
            {
                details.TweetType = TweetTypeEnum.RetweetOfMe;
                var table = (RetweetsOfMeTable)thisTweet;
                table.Assets.Load();
                if (table.Assets.HasLoadedOrAssignedValues && table.Assets != null)
                {
                    assets = table.Assets;
                }
            }
            else if (thisTweet is RetweetsToMeTable)
            {
                details.TweetType = TweetTypeEnum.RetweetToMe;
                var table = (RetweetsToMeTable)thisTweet;
                table.Assets.Load();
                if (table.Assets.HasLoadedOrAssignedValues && table.Assets != null)
                {
                    assets = table.Assets;
                }
            }
            else if (thisTweet is TwitterListTable)
            {
                details.TweetType = TweetTypeEnum.TwitterList;
                var table = (TwitterListTable)thisTweet;
                table.Assets.Load();
                if (table.Assets.HasLoadedOrAssignedValues && table.Assets != null)
                {
                    assets = table.Assets;
                }
            }
            else if (thisTweet is TwitterSearchTable)
            {
                details.TweetType = TweetTypeEnum.TwitterList;
                var table = (TwitterSearchTable)thisTweet;
                table.Assets.Load();
                if (table.Assets.HasLoadedOrAssignedValues && table.Assets != null)
                {
                    assets = table.Assets;
                }
            }

            if (assets != null)
            {
                var assetTables = assets as IAssetTable[] ?? assets.ToArray();
                if (assetTables.Any())
                {
                    var assetViewModel = assetTables.AsViewModel();
                    details.Assets = assetViewModel;
                }
                else
                {
                    details.Assets = new List<AssetViewModel>();
                }
            }

            return details;
        }

        private static string FormatHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Replace("&lt;", "<").Replace("&gt;", ">");
        }

        private static IList<AssetViewModel> AsViewModel(this IEnumerable<IAssetTable> assets)
        {
            if (assets == null)
                return null;

            return assets.Select(asset => new AssetViewModel
            {
                EndOffset = asset.EndOffset,
                EntityId = asset.EntityId,
                Id = asset.Id,
                LongValue = asset.LongValue,
                ShortValue = asset.ShortValue,
                StartOffset = asset.StartOffset,
                Type = asset.Type
            }).ToList();
        }
    }
}