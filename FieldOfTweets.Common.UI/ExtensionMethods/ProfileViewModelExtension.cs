// *********************************************************************************************************
// <copyright file="ProfileViewModelExtension.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System;
using System.Collections.Generic;
using System.Net;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.UI.Classes;
using FieldOfTweets.Common.UI.ViewModels;

namespace FieldOfTweets.Common.UI.ExtensionMethods
{
    public static class ProfileViewModelExtension
    {
        private static string _bannerSize;

        #region Properties

        private static string BannerSize
        {
            get
            {
                if (!string.IsNullOrEmpty(_bannerSize))
                {
                    return _bannerSize;
                }

                try
                {
                    _bannerSize = ResolutionHelper.CurrentResolution == Resolutions.WVGA ? "/mobile" : "/mobile_retina";
                    return _bannerSize;
                }
                catch (Exception)
                {
                    return "/mobile";
                }
            }
        }

        #endregion

        public static TwitterAccountViewModel AsViewModel(this ResponseGetUserProfile resp)
        {
            if (resp != null && (resp.id > 0 || !string.IsNullOrEmpty(resp.id_str)))
            {
                var profile = new TwitterAccountViewModel
                {
                    Id = resp.id,
                    DisplayName = resp.name,
                    Location =
                        string.IsNullOrWhiteSpace(resp.location)
                            ? string.Empty
                            : HttpUtility.HtmlDecode(resp.location),
                    ProfileImageUrl = resp.profile_image_url,
                    Bio =
                        string.IsNullOrWhiteSpace(resp.description)
                            ? string.Empty
                            : HttpUtility.HtmlDecode(resp.description),
                    ScreenName = resp.screen_name,
                    FollowersCount = (resp.followers_count.HasValue) ? resp.followers_count.Value : 0,
                    FollowingCount = (resp.friends_count.HasValue) ? resp.friends_count.Value : 0,
                    Verified = resp.verified,
                    Following = (resp.following.HasValue && resp.following.Value),
                    IsLoaded = true,
                    JoinDateString = resp.created_at,
                    Url = resp.url,
                    TweetCount = (resp.statuses_count.HasValue) ? resp.statuses_count.Value : 0,
                    BackgroundImageUrl = resp.profile_background_image_url,
                    ListedCount = resp.listed_count ?? 0,
                    IsProtected = resp.is_protected,
                    BannerUrl =
                        string.IsNullOrEmpty(resp.profile_banner_url)
                            ? string.Empty
                            : resp.profile_banner_url + BannerSize
                };


                if (resp.entities != null)
                {
                    if (resp.entities.url != null && resp.entities.url.urls != null)
                    {
                        profile.Assets = new List<AssetViewModel>();

                        foreach (var asset in resp.entities.url.urls)
                        {
                            profile.Assets.Add(new AssetViewModel()
                            {
                                Type = AssetTypeEnum.Url,
                                StartOffset = asset.indices[0],
                                EndOffset = asset.indices[1],
                                ShortValue = asset.url,
                                LongValue = asset.expanded_url                             
                            });
                        }
                    }
                }
               
                return profile;
            }

            return null;
        }
    }
}