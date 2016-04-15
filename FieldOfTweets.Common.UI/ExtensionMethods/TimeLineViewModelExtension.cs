// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.ImageHostParser;
using FieldOfTweets.Common.UI.ImageHostParser;
using FieldOfTweets.Common.UI.Interfaces;
using FieldOfTweets.Common.UI.ThirdPartyApi;
using FieldOfTweets.Common.UI.ViewModels;

namespace FieldOfTweets.Common.UI.ExtensionMethods
{
    public static class TimeLineViewModelExtension
    {

        private static IMehdohApp App
        {
            get { return ((IMehdohApp) Application.Current); }
        }

        public static TimelineViewModel AsViewModel(this TwitterSearchTable status, long accountId)
        {
            var tweet = new TimelineViewModel
                            {
                                ScreenName = status.ScreenName,
                                DisplayName = status.DisplayName,
                                CreatedAt = status.CreatedAt,
                                Description = string.IsNullOrEmpty(status.RetweetDescription) ? HttpUtility.HtmlDecode(status.Description) :
                                                                                                HttpUtility.HtmlDecode(status.RetweetDescription),
                                ImageUrl = status.ProfileImageUrl,
                                Id = status.Id,
                                RetweetUserDisplayName = status.RetweetUserDisplayName,
                                RetweetUserImageUrl = status.RetweetUserImageUrl,
                                RetweetUserScreenName = status.RetweetUserScreenName,
                                IsRetweet = status.IsRetweet,
                                IsReply = (status.InReplyToId.HasValue && status.InReplyToId.Value > 0),
                                AccountId = accountId,
                                LanguageCode = status.LanguageCode
                            };

            const string format = "ddd MMM dd HH:mm:ss zzzz yyyy";

            DateTime createdAtDate;
            DateTime.TryParseExact(status.CreatedAt, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out createdAtDate);

            tweet.CreatedAtDate = createdAtDate;

            var otherAuthors = new List<string>();

            if (status.Assets != null)
            {
                var screenName = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(accountId);

                foreach (var asset in status.Assets)
                {
                    switch (asset.Type)
                    {
                        case AssetTypeEnum.Mention:
                            try
                            {
                                if (String.Compare(asset.ShortValue.Replace("@", ""), screenName, StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    tweet.IsUserMentioned = true;
                                }
                                else
                                {
                                    var thisVal = (asset.ShortValue.StartsWith("@") ? asset.ShortValue : "@" + asset.ShortValue);
                                    otherAuthors.Add(thisVal);
                                }
                            }
                            catch (Exception)
                            {
                            }
                            break;

                        case AssetTypeEnum.Media:
                        case AssetTypeEnum.Url:
                            {
                                try
                                {
                                    string newUrl;
                                    var urlToParse = asset.LongValue;
                                    if (urlToParse.Length > 40)
                                        newUrl = urlToParse.Substring(0, 40) + "...";
                                    else
                                        newUrl = urlToParse;
                                    tweet.Description = tweet.Description.Replace(asset.ShortValue, newUrl);
                                }
                                catch (Exception)
                                {
                                }

                                if (tweet.MediaUrl == null)
                                    tweet.MediaUrl = GetMediaUrlFromAsset(asset.LongValue);
                            }
                            break;
                    }
                }
            }

            tweet.OtherAuthors = string.Join(",", otherAuthors);

            return tweet;
        }

        public static TimelineViewModel AsViewModel(this RetweetsToMeTable status, long accountId)
        {
            var tweet = new TimelineViewModel
                            {
                                ScreenName = status.ScreenName,
                                DisplayName = status.DisplayName,
                                CreatedAt = status.CreatedAt,
                                Description =
                                    string.IsNullOrEmpty(status.RetweetDescription)
                                        ? HttpUtility.HtmlDecode(status.Description)
                                        : HttpUtility.HtmlDecode(status.RetweetDescription),
                                ImageUrl = status.ProfileImageUrl,
                                Id = status.Id,
                                RetweetUserDisplayName = status.RetweetUserDisplayName,
                                RetweetUserImageUrl = status.RetweetUserImageUrl,
                                RetweetUserScreenName = status.RetweetUserScreenName,
                                IsRetweet = status.IsRetweet,
                                AccountId = accountId,
                                LanguageCode = status.LanguageCode                                
                            };

            return tweet;
        }


        public static TimelineViewModel AsViewModel(this RetweetsByMeTable status, long accountId)
        {
            var tweet = new TimelineViewModel
                            {
                                ScreenName = status.ScreenName,
                                DisplayName = status.DisplayName,
                                CreatedAt = status.CreatedAt,
                                Description =
                                    string.IsNullOrEmpty(status.RetweetDescription)
                                        ? HttpUtility.HtmlDecode(status.Description)
                                        : HttpUtility.HtmlDecode(status.RetweetDescription),
                                ImageUrl = status.ProfileImageUrl,
                                Id = status.Id,
                                RetweetUserDisplayName = status.RetweetUserDisplayName,
                                RetweetUserImageUrl = status.RetweetUserImageUrl,
                                RetweetUserScreenName = status.RetweetUserScreenName,
                                IsRetweet = status.IsRetweet,
                                AccountId = accountId,
                                LanguageCode = status.LanguageCode
                            };

            return tweet;
        }

        public static TimelineViewModel AsViewModel(this RetweetsOfMeTable status, long accountId)
        {
            var tweet = new TimelineViewModel
                            {
                                ScreenName = status.ScreenName,
                                DisplayName = status.DisplayName,
                                CreatedAt = status.CreatedAt,
                                Description = string.IsNullOrEmpty(status.RetweetDescription)
                                                  ? HttpUtility.HtmlDecode(status.Description)
                                                  : HttpUtility.HtmlDecode(status.RetweetDescription),
                                ImageUrl = status.ProfileImageUrl,
                                Id = status.Id,
                                RetweetUserDisplayName = status.RetweetUserDisplayName,
                                RetweetUserImageUrl = status.RetweetUserImageUrl,
                                RetweetUserScreenName = status.RetweetUserScreenName,
                                IsRetweet = status.IsRetweet,
                                AccountId = accountId,
                                LanguageCode = status.LanguageCode
                            };

            return tweet;
        }

        public static FavouritesViewModel AsFavouriesViewModel(this FavouriteTable status, long accountId)
        {
            var tweet = new FavouritesViewModel
                            {
                                ScreenName = status.ScreenName,
                                DisplayName = status.DisplayName,
                                CreatedAt = status.CreatedAt,
                                Description = HttpUtility.HtmlDecode(status.Description),
                                ImageUrl = status.ProfileImageUrl,
                                Id = status.Id,
                                RetweetUserDisplayName = status.RetweetUserDisplayName,
                                RetweetUserImageUrl = status.RetweetUserImageUrl,
                                RetweetUserScreenName = status.RetweetUserScreenName,
                                IsRetweet = status.IsRetweet,
                                IsReply = (status.InReplyToId.HasValue && status.InReplyToId.Value > 0),
                                AccountId = status.ProfileId,
                                LanguageCode = status.LanguageCode
                            };

            if (status.Assets != null)
            {
                var screenName = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(accountId);

                foreach (var asset in status.Assets)
                {
                    try
                    {
                        switch (asset.Type)
                        {
                            case AssetTypeEnum.Mention:
                                if (!string.IsNullOrWhiteSpace(asset.ShortValue))
                                    if (string.Compare(asset.ShortValue.Replace("@", ""), screenName, StringComparison.CurrentCultureIgnoreCase) == 0)
                                        tweet.IsUserMentioned = true;
                                break;

                            case AssetTypeEnum.Media:
                            case AssetTypeEnum.Url:
                                string newUrl;
                                if (!string.IsNullOrWhiteSpace(asset.LongValue) && !string.IsNullOrWhiteSpace(asset.ShortValue))
                                {
                                    var urlToParse = asset.LongValue;
                                    if (urlToParse.Length > 40)
                                        newUrl = urlToParse.Substring(0, 40) + "...";
                                    else
                                        newUrl = urlToParse;
                                    tweet.Description = tweet.Description.Replace(asset.ShortValue, newUrl);
                                }
                                break;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return tweet;
        }

        public static MentionsViewModel AsViewModel(this MentionTable t, long accountId)
        {
            var tweet = new MentionsViewModel
                            {
                                ScreenName = t.ScreenName,
                                DisplayName = t.DisplayName,
                                CreatedAt = t.CreatedAt,
                                Description = HttpUtility.HtmlDecode(t.Description),
                                ImageUrl = t.ProfileImageUrl,
                                Id = t.Id,
                                RetweetUserDisplayName = t.RetweetUserDisplayName,
                                RetweetUserImageUrl = t.RetweetUserImageUrl,
                                RetweetUserScreenName = t.RetweetUserScreenName,
                                IsRetweet = t.IsRetweet,
                                IsReply = (t.InReplyToId.HasValue && t.InReplyToId.Value > 0),
                                AccountId = accountId,
                                LanguageCode = t.LanguageCode
                            };

            var otherAuthors = new List<string>();

            if (t.Assets != null)
            {
                var screenName = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(accountId);

                foreach (var asset in t.Assets)
                {
                    switch (asset.Type)
                    {
                        case AssetTypeEnum.Media:

                            try
                            {
                                string newMediaUrl;
                                var urlToParse = asset.LongValue;
                                if (urlToParse.Length > 40)
                                    newMediaUrl = urlToParse.Substring(0, 40) + "...";
                                else
                                    newMediaUrl = urlToParse;
                                tweet.Description = tweet.Description.Replace(asset.ShortValue, newMediaUrl);
                            }
                            catch (Exception)
                            {
                            }

                            tweet.MediaUrl = new Uri(asset.LongValue, UriKind.Absolute);
                            break;

                        case AssetTypeEnum.Mention:
                            try
                            {
                                if (string.Compare(asset.ShortValue, screenName, StringComparison.InvariantCultureIgnoreCase) != 0)
                                {
                                    var thisVal = (asset.ShortValue.StartsWith("@") ? asset.ShortValue : "@" + asset.ShortValue);
                                    otherAuthors.Add(thisVal);
                                }
                            }
                            catch (Exception)
                            {
                            }
                            break;

                        case AssetTypeEnum.Url:
                            string newUrl;
                            if (!string.IsNullOrWhiteSpace(asset.LongValue) && !string.IsNullOrWhiteSpace(asset.ShortValue))
                            {
                                var urlToParse = asset.LongValue;
                                if (urlToParse.Length > 40)
                                    newUrl = urlToParse.Substring(0, 40) + "...";
                                else
                                    newUrl = urlToParse;
                                tweet.Description = tweet.Description.Replace(asset.ShortValue, newUrl);
                            }

                            try
                            {
                                if (tweet.MediaUrl == null)
                                {
                                    var targetUrl = string.IsNullOrEmpty(asset.LongValue) ? asset.ShortValue : asset.LongValue;
                                    tweet.MediaUrl = GetMediaUrlFromAsset(targetUrl);
                                }
                            }
                            catch (Exception)
                            {
                            }


                            break;
                    }

                    if (tweet.MediaUrl != null)
                        break;

                }
            }

            tweet.OtherAuthors = string.Join(",", otherAuthors);

            return tweet;
        }

        public static MentionsViewModel AsMentionViewModel(this ResponseTweet status, long accountId)
        {
            var tweet = new MentionsViewModel
                            {
                                ResponseTweet = status,
                                ScreenName = status.user.screen_name,
                                DisplayName = status.user.name,
                                Description = HttpUtility.HtmlDecode(status.text),
                                CreatedAt = status.created_at,
                                ImageUrl = status.user.profile_image_url,
                                Id = status.id,
                                //Client = status.source,                
                                IsRetweet = false,
                                IsReply = (status.in_reply_to_status_id.HasValue && status.in_reply_to_status_id.Value > 0),
                                AccountId = accountId,
                                LanguageCode = status.lang
                            };

            if (status.retweeted_status != null && status.retweeted_status.user != null)
            {
                tweet.RetweetUserDisplayName = status.retweeted_status.user.name;
                tweet.RetweetUserScreenName = status.retweeted_status.user.screen_name;
                tweet.RetweetUserImageUrl = status.retweeted_status.user.profile_image_url;
                tweet.Description = HttpUtility.HtmlDecode(status.retweeted_status.text);
                tweet.CreatedAt = status.retweeted_status.created_at;
                tweet.RetweetOriginalId = status.retweeted_status.id; 
                tweet.IsRetweet = true;
            }

            var otherAuthors = new List<string>();

            if (status.entities != null)
            {
                if (status.entities.user_mentions != null && status.entities.user_mentions.Length > 0)
                {
                    var screenName = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(accountId);

                    foreach (var url in status.entities.user_mentions)
                    {
                        if (string.Compare(url.screen_name, screenName, StringComparison.InvariantCultureIgnoreCase) != 0)
                        {
                            var thisVal = (url.screen_name.StartsWith("@") ? url.screen_name : "@" + url.screen_name);
                            otherAuthors.Add(thisVal);
                        }
                    }
                }

                if (status.entities.media != null && status.entities.media.Length > 0)
                {
                    foreach (var url in status.entities.media)
                    {
                        if (!string.IsNullOrWhiteSpace(url.expanded_url) && !string.IsNullOrWhiteSpace(url.url))
                        {
                            try
                            {
                                string newUrl;
                                var urlToParse = url.display_url;
                                if (urlToParse.Length > 40)
                                    newUrl = url.display_url.Substring(0, 40) + "...";
                                else
                                    newUrl = url.display_url;
                                tweet.Description = tweet.Description.Replace(url.url, newUrl);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        if (tweet.MediaUrl == null)
                        {
                            tweet.MediaUrl = new Uri(url.media_url, UriKind.Absolute);
                        }
                    }
                }

                if (status.entities.urls != null && status.entities.urls.Length > 0)
                {
                    foreach (var url in status.entities.urls)
                    {
                        if (!string.IsNullOrWhiteSpace(url.expanded_url) && !string.IsNullOrWhiteSpace(url.url))
                        {
                            try
                            {
                                string newUrl;
                                var urlToParse = url.expanded_url;
                                if (urlToParse.Length > 40)
                                    newUrl = url.expanded_url.Substring(0, 40) + "...";
                                else
                                    newUrl = url.expanded_url;
                                tweet.Description = tweet.Description.Replace(url.url, newUrl);
                            }
                            catch (Exception)
                            {
                            }
                        }


                        try
                        {
                            if (tweet.MediaUrl == null)
                            {
                                var targetUrl = string.IsNullOrEmpty(url.expanded_url) ? url.url : url.expanded_url;
                                tweet.MediaUrl = GetMediaUrlFromAsset(targetUrl);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            tweet.OtherAuthors = string.Join(",", otherAuthors);

            return tweet;
        }


        public static MessagesViewModel AsViewModel(this ResponseDirectMessage status, long accountId)
        {
            var tweet = new MessagesViewModel
                       {
                           ScreenName = status.sender.screen_name,
                           DisplayName = status.sender.name,
                           Description = System.Web.HttpUtility.HtmlDecode(status.text),
                           CreatedAt = status.created_at,
                           ImageUrl = status.sender.profile_image_url,
                           Id = status.id,
                           IsRetweet = false,
                           AccountId = accountId,
                           LanguageCode = status.lang
                       };

            if (status.entities != null)
            {
                if (status.entities.media != null && status.entities.media.Length > 0)
                {
                    foreach (var url in status.entities.media)
                    {
                        if (!string.IsNullOrWhiteSpace(url.expanded_url) && !string.IsNullOrWhiteSpace(url.url))
                        {
                            try
                            {
                                string newUrl;
                                var urlToParse = url.display_url;
                                if (urlToParse.Length > 40)
                                    newUrl = url.display_url.Substring(0, 40) + "...";
                                else
                                    newUrl = url.display_url;
                                tweet.Description = tweet.Description.Replace(url.url, newUrl);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        if (tweet.MediaUrl == null)
                        {
                            tweet.MediaUrl = new Uri(url.media_url, UriKind.Absolute);
                        }
                    }
                }


                if (status.entities.urls != null && status.entities.urls.Length > 0)
                {
                    foreach (var url in status.entities.urls)
                    {
                        if (!string.IsNullOrWhiteSpace(url.expanded_url) && !string.IsNullOrWhiteSpace(url.url))
                        {
                            try
                            {
                                string newUrl;
                                var urlToParse = url.expanded_url;
                                if (urlToParse.Length > 40)
                                    newUrl = url.expanded_url.Substring(0, 40) + "...";
                                else
                                    newUrl = url.expanded_url;
                                tweet.Description = tweet.Description.Replace(url.url, newUrl);
                            }
                            catch (Exception)
                            {
                            }
                        }


                        try
                        {
                            if (tweet.MediaUrl == null)
                            {
                                var targetUrl = string.IsNullOrEmpty(url.expanded_url) ? url.url : url.expanded_url;
                                tweet.MediaUrl = GetMediaUrlFromAsset(targetUrl);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            return tweet;

        }

        public static List<PhotoViewModel> AsPhotoViewModel(this TimelineTable status, long accountId)
        {
            var res = new List<PhotoViewModel>();

            if (status == null || status.Assets == null)
                return res;

            foreach (var ass in status.Assets)
            {
                // media, easy, add it
                if (ass.Type == AssetTypeEnum.Media)
                {
                    var url = string.IsNullOrEmpty(ass.LongValue) ? ass.ShortValue : ass.LongValue;

                    var model = new PhotoViewModel
                                    {
                                        Id = status.Id,
                                        AccountId = accountId,
                                        ImageUri = new Uri(url, UriKind.Absolute),
                                        CreatedAt = status.CreatedAt,
                                        HasImage = true
                                    };
                    res.Add(model);
                }

                    // link... lets re-use the code from details page
                else if (ass.Type == AssetTypeEnum.Url)
                {
                    var model = new PhotoViewModel
                                    {
                                        AccountId = accountId,
                                        Id = status.Id,
                                        CreatedAt = status.CreatedAt
                                    };

                    var targetUrl = string.IsNullOrEmpty(ass.LongValue) ? ass.ShortValue : ass.LongValue;

                    if (targetUrl.ToLower().EndsWith(".jpg") || targetUrl.ToLower().EndsWith(".png") || targetUrl.ToLower().EndsWith(".jpeg"))
                    {
                        model.HasImage = true;
                        model.ImageUri = new Uri(targetUrl, UriKind.Absolute);
                    }
                    else if (targetUrl.ToLower().Contains("imgur.com"))
                    {
                        model.HasImage = true;
                        SetParsedImageContainer(model, targetUrl, (sender, e) => SetParsedImage(e, new ImgurParser()));
                    }
                    else if (targetUrl.ToLower().Contains("img.ly"))
                    {
                        //SetParsedImageContainer(model, targetUrl, (sender, e) => SetParsedImage(e, new ImglyParser()));
                        try
                        {
                            var splitUrl = targetUrl.Split('/');
                            var id = splitUrl.Last();
                            var newUrl = string.Format("http://img.ly/show/large/{0}", id);
                            model.ImageUri = new Uri(newUrl, UriKind.Absolute);
                            model.HasImage = true;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else if (targetUrl.ToLower().Contains("instagr.am/p/") || targetUrl.ToLower().Contains("instagram.com/p/"))
                    {
                        model.HasImage = true;
                        if (!targetUrl.EndsWith("/"))
                            targetUrl += "/";

                        model.ImageUri = new Uri(targetUrl + "media?size=l", UriKind.Absolute);
                    }
                    else if (targetUrl.ToLower().Contains("d.pr/i/"))
                    {
                        var newUrl = string.Format("{0}+", targetUrl);
                        model.ImageUri = new Uri(newUrl, UriKind.Absolute);
                    }
                    else if (targetUrl.ToLower().Contains("picplz.com"))
                    {
                        model.HasImage = true;
                        SetParsedImageContainer(model, targetUrl, (sender, e) => SetParsedImage(e, new PicPlzParser()));
                    }
                    else if (targetUrl.ToLower().Contains("flic.kr/p/") || targetUrl.ToLower().Contains("flickr.com/photos/"))
                    {
                        model.HasImage = true;
                        SetParsedImageContainer(model, targetUrl, (sender, e) => SetParsedImage(e, new FlickrPhotoParser()));
                    }
                    else if (targetUrl.ToLower().Contains("lockerz.com"))
                    {
                        model.HasImage = true;
                        var newUrl = "http://api.plixi.com/api/tpapi.svc/imagefromurl?size=medium&url=" + targetUrl;
                        model.ImageUri = new Uri(newUrl, UriKind.Absolute);
                    }
                    else if (targetUrl.ToLower().Contains("twitgoo.com"))
                    {
                        model.HasImage = true;
                        SetParsedImageContainer(model, targetUrl, (sender, e) => SetParsedImage(e, new TwitgooParser()));
                    }
                    else if (targetUrl.ToLower().Contains("mobypicture.com") || targetUrl.ToLower().Contains("moby.to"))
                    {
                        model.HasImage = true;
                        SetParsedImageContainer(model, targetUrl, (sender, e) => SetParsedImage(e, new MobyPictureParser()));
                    }
                    else if (targetUrl.ToLower().Contains("500px.com/photo"))
                    {
                        model.HasImage = true;
                        SetParsedImageContainer(model, targetUrl, (sender, e) => SetParsedImage(e, new FiveHundredPxPictureParser()));
                    }
                    else if (targetUrl.ToLower().Contains("eyeem.com/p"))
                    {
                        model.HasImage = true;
                        SetParsedImageContainer(model, targetUrl, (sender, e) => SetParsedImage(e, new EyeEmParser()));
                    }
                    else if (targetUrl.ToLower().Contains("2instawithlove.com/p/"))
                    {
                        model.HasImage = true;
                        SetParsedImageContainer(model, targetUrl, (sender, e) => SetParsedImage(e, new ToInstaWithLoveParser()));
                    }
                    else if (targetUrl.ToLower().Contains("photoplay.net/photos/"))
                    {
                        model.HasImage = true;
                        SetParsedImageContainer(model, targetUrl, (sender, e) => SetParsedImage(e, new PhotoplayParser()));
                    }
                    else if (targetUrl.ToLower().Contains("//sdrv.ms/") || targetUrl.ToLower().Contains("//1drv.ms/"))
                    {
                        var newUrl = "https://apis.live.net/v5.0/skydrive/get_item_preview?url=" + targetUrl;
                        model.ImageUri = new Uri(newUrl, UriKind.Absolute);
                        model.HasImage = true;
                    }
                    else if (targetUrl.ToLower().Contains("molo.me") || targetUrl.ToLower().Contains("molome.com"))
                    {
                        try
                        {
                            var splitUrl = targetUrl.Split('/');
                            var id = splitUrl.Last();
                            var newUrl = string.Format("http://p480x480.molo.me/{0}_480x480", id);
                            model.ImageUri = new Uri(newUrl, UriKind.Absolute);
                            model.HasImage = true;
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else if (targetUrl.ToLower().Contains("twitpic.com"))
                    {
                        var imageUri = new Uri(targetUrl, UriKind.Absolute);
                        string filePath = imageUri.AbsolutePath;

                        if (!filePath.StartsWith("/"))
                            filePath = "/" + filePath;

                        model.ImageUri = new Uri(string.Format("http://twitpic.com/show/full/{0}", filePath), UriKind.Absolute);
                        model.HasImage = true;
                    }
                    else if (targetUrl.ToLower().Contains("yfrog.com"))
                    {
                        model.ImageUri = new Uri(targetUrl + ":iphone", UriKind.Absolute);
                        model.HasImage = true;
                    }

                    res.Add(model);
                }
            }

            return res;
        }

        private static void SetParsedImage(OpenReadCompletedEventArgs e, IImageHostParser parser)
        {
            if (e.Error != null)
                return;

            var resInfo = new StreamResourceInfo(e.Result, null);
            using (var reader = new StreamReader(resInfo.Stream))
            {
                using (var bReader = new BinaryReader(reader.BaseStream))
                {
                    var contents = bReader.ReadBytes((int)reader.BaseStream.Length);
                    var fileContents = System.Text.Encoding.UTF8.GetString(contents, 0, contents.Length);
                    var newUrl = parser.GetImageUrl(fileContents);
                    if (!string.IsNullOrWhiteSpace(newUrl))
                    {
                        var model = e.UserState as PhotoViewModel;
                        if (model != null)
                            model.ImageUri = new Uri(newUrl, UriKind.Absolute);
                    }
                }
            }
        }

        private static void SetParsedImageContainer(PhotoViewModel model, string targetUrl, OpenReadCompletedEventHandler eventHandler)
        {
            var client = new WebClient();
            client.OpenReadCompleted += eventHandler;
            var imageUri = new Uri(targetUrl, UriKind.Absolute);
            client.OpenReadAsync(imageUri, model);
        }

        public static MessagesViewModel AsViewModel(this MessageTable status, long accountId)
        {

            var model = new MessagesViewModel
                       {
                           ScreenName = status.ScreenName,
                           DisplayName = status.DisplayName,
                           CreatedAt = status.CreatedAt,
                           Description = HttpUtility.HtmlDecode(status.Description),
                           ImageUrl = status.ProfileImageUrl,
                           Id = status.Id,
                           AccountId = accountId
                       };

            if (status.Assets != null)
            {
                foreach (var asset in status.Assets)
                {
                    switch (asset.Type)
                    {
                        case AssetTypeEnum.Url:
                            {
                                try
                                {
                                    string newUrl;
                                    var urlToParse = asset.LongValue;
                                    if (urlToParse.Length > 40)
                                        newUrl = urlToParse.Substring(0, 40) + "...";
                                    else
                                        newUrl = urlToParse;
                                    model.Description = model.Description.Replace(asset.ShortValue, newUrl);
                                }
                                catch (Exception)
                                {
                                }
                            }
                            break;
                    }
                }
            }

            return model;

        }


        public static TimelineViewModel AsViewModel(this TimelineTable status, long accountId)
        {

            var tweet = new TimelineViewModel
                            {
                                ScreenName = status.ScreenName,
                                DisplayName = status.DisplayName,
                                CreatedAt = status.CreatedAt,
                                Description = string.IsNullOrEmpty(status.RetweetDescripton)
                                                    ? HttpUtility.HtmlDecode(status.Description)
                                                    : HttpUtility.HtmlDecode(status.RetweetDescripton),
                                ImageUrl = status.ProfileImageUrl,
                                Id = status.Id,
                                RetweetUserDisplayName = status.RetweetUserDisplayName,
                                RetweetUserImageUrl = status.RetweetUserImageUrl,
                                RetweetOriginalId = status.RetweetOriginalId,
                                RetweetUserScreenName = status.RetweetUserScreenName,
                                IsRetweet = status.IsRetweet,
                                IsReply = (status.InReplyToId.HasValue && status.InReplyToId.Value > 0),
                                AccountId = accountId,
                                LanguageCode = status.LanguageCode
                            };

            var otherAuthors = new List<string>();

            if (status.Assets != null)
            {
                var screenName = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(accountId);

                foreach (var asset in status.Assets)
                {
                    switch (asset.Type)
                    {
                        case AssetTypeEnum.Mention:
                            try
                            {
                                if (string.Compare(asset.ShortValue.Replace("@", ""), screenName, StringComparison.CurrentCultureIgnoreCase) == 0)
                                {
                                    tweet.IsUserMentioned = true;
                                }
                                else
                                {
                                    var thisVal = (asset.ShortValue.StartsWith("@") ? asset.ShortValue : "@" + asset.ShortValue);
                                    otherAuthors.Add(thisVal);
                                }
                            }
                            catch (Exception)
                            {
                            }
                            break;

                        case AssetTypeEnum.Media:
                        case AssetTypeEnum.Url:

                            try
                            {
                                string newUrl;
                                var urlToParse = asset.LongValue;
                                if (urlToParse.Length > 40)
                                    newUrl = urlToParse.Substring(0, 40) + "...";
                                else
                                    newUrl = urlToParse;
                                tweet.Description = tweet.Description.Replace(asset.ShortValue, newUrl);
                            }
                            catch (Exception)
                            {
                            }

                            if (asset.Type == AssetTypeEnum.Media)
                            {
                                tweet.MediaUrl = new Uri(asset.LongValue, UriKind.Absolute);
                            }
                            else
                            {
                                var targetUrl = string.IsNullOrEmpty(asset.LongValue) ? asset.ShortValue : asset.LongValue;
                                tweet.MediaUrl = GetMediaUrlFromAsset(targetUrl);
                            }

                            break;
                    }

                    if (tweet.MediaUrl != null)
                        break;

                }

                if (status.Assets.Count(x => x.Type == AssetTypeEnum.Media) > 1)
                    tweet.HasMultipleImages = true;

            }

            tweet.OtherAuthors = string.Join(",", otherAuthors);

            return tweet;
        }

        public static TimelineViewModel AsViewModel(this TwitterListTable status, long accountId)
        {
            var tweet = new TimelineViewModel
                            {
                                ScreenName = status.ScreenName,
                                DisplayName = status.DisplayName,
                                CreatedAt = status.CreatedAt,
                                Description =
                                    string.IsNullOrEmpty(status.RetweetDescription)
                                        ? HttpUtility.HtmlDecode(status.Description)
                                        : HttpUtility.HtmlDecode(status.RetweetDescription),
                                ImageUrl = status.ProfileImageUrl,
                                Id = status.Id,
                                RetweetUserDisplayName = status.RetweetUserDisplayName,
                                RetweetUserImageUrl = status.RetweetUserImageUrl,
                                RetweetUserScreenName = status.RetweetUserScreenName,
                                RetweetOriginalId = status.RetweetOriginalId,
                                IsRetweet = status.IsRetweet,
                                IsReply = (status.InReplyToId.HasValue && status.InReplyToId.Value > 0),
                                AccountId = accountId,
                                LanguageCode = status.LanguageCode
                            };

            var otherAuthors = new List<string>();

            if (status.Assets != null)
            {
                var screenName = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(accountId);

                foreach (var asset in status.Assets)
                {
                    switch (asset.Type)
                    {
                        case AssetTypeEnum.Mention:
                            try
                            {
                                if (string.Compare(asset.ShortValue.Replace("@", ""), screenName, StringComparison.CurrentCultureIgnoreCase) == 0)
                                {
                                    tweet.IsUserMentioned = true;
                                }
                                else
                                {
                                    var thisVal = (asset.ShortValue.StartsWith("@") ? asset.ShortValue : "@" + asset.ShortValue);
                                    otherAuthors.Add(thisVal);
                                }
                            }
                            catch (Exception)
                            {
                            }
                            break;
                            
                        case AssetTypeEnum.Media:
                        case AssetTypeEnum.Url:

                            try
                            {
                                string newUrl;
                                var urlToParse = asset.LongValue;
                                if (urlToParse.Length > 40)
                                    newUrl = urlToParse.Substring(0, 40) + "...";
                                else
                                    newUrl = urlToParse;
                                tweet.Description = tweet.Description.Replace(asset.ShortValue, newUrl);
                            }
                            catch (Exception)
                            {
                            }

                            if (asset.Type == AssetTypeEnum.Media)
                            {
                                tweet.MediaUrl = new Uri(asset.LongValue, UriKind.Absolute);
                            }
                            else
                            {
                                var targetUrl = string.IsNullOrEmpty(asset.LongValue) ? asset.ShortValue : asset.LongValue;
                                tweet.MediaUrl = GetMediaUrlFromAsset(targetUrl);
                            }

                            break;
                    }

                    if (tweet.MediaUrl != null)
                        break;

                }
            }

            tweet.OtherAuthors = string.Join(",", otherAuthors);

            return tweet;
        }

        public static TimelineViewModel AsViewModel(this ResponseTweet status, long accountId)
        {

            var tweet = new TimelineViewModel
                            {
                                ResponseTweet = status,
                                ScreenName = status.user.screen_name,
                                DisplayName = status.user.name,
                                Description = HttpUtility.HtmlDecode(status.text),
                                CreatedAt = status.created_at,
                                ImageUrl = status.user.profile_image_url,
                                Id = status.id,
                                IsRetweet = false,
                                IsReply = (status.in_reply_to_status_id.HasValue && status.in_reply_to_status_id.Value > 0),
                                AccountId = accountId,
                                LanguageCode = status.lang
                            };

            if (status.retweeted_status != null && status.retweeted_status.user != null && !string.IsNullOrEmpty(status.retweeted_status.text))
            {
                tweet.RetweetUserDisplayName = status.retweeted_status.user.name;
                tweet.RetweetUserScreenName = status.retweeted_status.user.screen_name;
                tweet.RetweetUserImageUrl = status.retweeted_status.user.profile_image_url;
                tweet.Description = HttpUtility.HtmlDecode(status.retweeted_status.text);
                tweet.IsRetweet = true;
                tweet.RetweetOriginalId = status.retweeted_status.id;

                tweet.CreatedAt = status.retweeted_status.created_at;
            }

            var otherAuthors = new List<string>();

            if (status.extended_entities != null && status.extended_entities.media != null && status.extended_entities.media.Length > 0)
            {
                foreach (var url in status.extended_entities.media)
                {
                    if (!string.IsNullOrWhiteSpace(url.expanded_url) && !string.IsNullOrWhiteSpace(url.url))
                    {
                        try
                        {
                            string newUrl;
                            var urlToParse = url.display_url;
                            if (urlToParse.Length > 40)
                                newUrl = url.display_url.Substring(0, 40) + "...";
                            else
                                newUrl = url.display_url;
                            tweet.Description = tweet.Description.Replace(url.url, newUrl);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    try
                    {
                        if (tweet.MediaUrl == null)
                            tweet.MediaUrl = new Uri(url.media_url, UriKind.Absolute);
                    }
                    catch (Exception)
                    {
                    }
                }

                if (status.extended_entities.media.Length > 1)
                    tweet.HasMultipleImages = true;

            }
            else if (status.entities != null)
            {
                if (status.entities.media != null && status.entities.media.Length > 0)
                {
                    foreach (var url in status.entities.media)
                    {
                        if (!string.IsNullOrWhiteSpace(url.expanded_url) && !string.IsNullOrWhiteSpace(url.url))
                        {
                            try
                            {
                                string newUrl;
                                var urlToParse = url.display_url;
                                if (urlToParse.Length > 40)
                                    newUrl = url.display_url.Substring(0, 40) + "...";
                                else
                                    newUrl = url.display_url;
                                tweet.Description = tweet.Description.Replace(url.url, newUrl);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        try
                        {
                            if (tweet.MediaUrl == null)
                                tweet.MediaUrl = new Uri(url.media_url, UriKind.Absolute);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                if (status.entities.urls != null && status.entities.urls.Length > 0)
                {
                    foreach (var url in status.entities.urls)
                    {
                        if (!string.IsNullOrWhiteSpace(url.expanded_url) && !string.IsNullOrWhiteSpace(url.url))
                        {
                            try
                            {
                                string newUrl;
                                var urlToParse = url.expanded_url;
                                if (urlToParse.Length > 40)
                                    newUrl = url.expanded_url.Substring(0, 40) + "...";
                                else
                                    newUrl = url.expanded_url;
                                tweet.Description = tweet.Description.Replace(url.url, newUrl);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        var targetUrl = string.IsNullOrEmpty(url.expanded_url) ? url.url : url.expanded_url;

                        tweet.MediaUrl = GetMediaUrlFromAsset(targetUrl);
                    }
                }

                if (status.entities.user_mentions != null && status.entities.user_mentions.Length > 0)
                {
                    var screenName = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(accountId);

                    foreach (var url in status.entities.user_mentions)
                    {
                        if (string.Compare(url.screen_name, screenName, StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            tweet.IsUserMentioned = true;
                        }
                        else
                        {
                            var thisVal = (url.screen_name.StartsWith("@") ? url.screen_name : "@" + url.screen_name);
                            otherAuthors.Add(thisVal);
                        }
                    }
                }
            }
            tweet.OtherAuthors = string.Join(",", otherAuthors);

            return tweet;
        }

        public static FavouritesViewModel AsFavouritesViewModel(this ResponseTweet status, long accountId)
        {
            var item = new FavouritesViewModel
                           {
                               ResponseTweet = status,
                               DisplayName = status.user.name,
                               Description = HttpUtility.HtmlDecode(status.text),
                               CreatedAt = status.created_at,
                               ImageUrl = status.user.profile_image_url,
                               Id = status.id,
                               //Client = status.source,
                               IsRetweet = false,
                               ScreenName = status.user.screen_name,
                               AccountId = accountId,
                               IsReply = (status.in_reply_to_status_id.HasValue && status.in_reply_to_status_id.Value > 0),
                               LanguageCode = status.lang
                           };

            if (status.retweeted_status != null && status.retweeted_status.user != null)
            {
                item.RetweetUserDisplayName = status.retweeted_status.user.name;
                item.RetweetUserScreenName = status.retweeted_status.user.screen_name;
                item.RetweetUserImageUrl = status.retweeted_status.user.profile_image_url;
                item.RetweetOriginalId = status.retweeted_status.id;
                item.Description = status.retweeted_status.text;
                item.CreatedAt = status.retweeted_status.created_at;
                item.IsRetweet = true;
            }

            if (status.entities != null)
            {
                if (status.entities.media != null && status.entities.media.Length > 0)
                {
                    foreach (var url in status.entities.media)
                    {
                        if (!string.IsNullOrWhiteSpace(url.expanded_url) && !string.IsNullOrWhiteSpace(url.url))
                        {
                            string newUrl;
                            var urlToParse = url.expanded_url;
                            if (urlToParse.Length > 40)
                                newUrl = url.expanded_url.Substring(0, 40) + "...";
                            else
                                newUrl = url.expanded_url;
                            item.Description = item.Description.Replace(url.url, newUrl);
                        }
                    }
                }
                if (status.entities.urls != null && status.entities.urls.Length > 0)
                {
                    foreach (var url in status.entities.urls)
                    {
                        if (!string.IsNullOrWhiteSpace(url.expanded_url) && !string.IsNullOrWhiteSpace(url.url))
                        {
                            string newUrl;
                            var urlToParse = url.expanded_url;
                            if (urlToParse.Length > 40)
                                newUrl = url.expanded_url.Substring(0, 40) + "...";
                            else
                                newUrl = url.expanded_url;
                            item.Description = item.Description.Replace(url.url, newUrl);
                        }
                    }
                }
                if (status.entities.user_mentions != null && status.entities.user_mentions.Length > 0)
                {
                    var screenName = ((IMehdohApp)(Application.Current)).ViewModel.GetScreenNameForAccountId(accountId);

                    foreach (var url in status.entities.user_mentions)
                    {
                        if (String.Compare(url.screen_name, screenName, StringComparison.OrdinalIgnoreCase) == 0)
                            item.IsUserMentioned = true;
                    }
                }
            }

            return item;
        }

        private static Uri GetMediaUrlFromAsset(string targetUrl)
        {

            if (targetUrl.ToLower().EndsWith(".jpg") || targetUrl.ToLower().EndsWith(".png") || targetUrl.ToLower().EndsWith(".jpeg"))
            {
                return new Uri(targetUrl, UriKind.Absolute);
            }

            if (targetUrl.ToLower().Contains("youtube.com") || targetUrl.ToLower().Contains("youtu.be") && !targetUrl.ToLower().Contains("user/"))
            {
                try
                {
                    string newUrl = targetUrl.GetYoutubeVideoIdFromUrl();

                    if (!string.IsNullOrEmpty(newUrl))
                    {
                        return new Uri(string.Format("http://img.youtube.com/vi/{0}/0.jpg", newUrl), UriKind.Absolute);
                    }
                }
                catch (Exception)
                {
                }
            }
            else if (targetUrl.ToLower().Contains("d.pr/i/"))
            {
                var newUrl = string.Format("{0}+", targetUrl);
                return new Uri(newUrl, UriKind.Absolute);
            }
            else if (targetUrl.ToLower().Contains("img.ly"))
            {
                try
                {
                    var splitUrl = targetUrl.Split('/');
                    var id = splitUrl.Last();
                    var newUrl = string.Format("http://img.ly/show/large/{0}", id);
                    return new Uri(newUrl, UriKind.Absolute);
                }
                catch (Exception)
                {
                }
            }
            else if (targetUrl.ToLower().Contains("instagr.am/p/") || targetUrl.ToLower().Contains("instagram.com/p/"))
            {
                if (!targetUrl.EndsWith("/"))
                    targetUrl += "/";
                return new Uri(targetUrl + "media?size=l", UriKind.Absolute);
            }
            else if (targetUrl.ToLower().Contains("molo.me") || targetUrl.ToLower().Contains("molome.com"))
            {
                try
                {
                    var splitUrl = targetUrl.Split('/');
                    var id = splitUrl.Last();
                    var newUrl = string.Format("http://p480x480.molo.me/{0}_480x480", id);
                    return new Uri(newUrl, UriKind.Absolute);
                }
                catch (Exception)
                {
                }
            }
            else if (targetUrl.ToLower().Contains("twitpic.com/"))
            {
                try
                {
                    var imageUri = new Uri(targetUrl, UriKind.Absolute);
                    string filePath = imageUri.AbsolutePath;
                    if (!filePath.StartsWith("/"))
                        filePath = "/" + filePath;
                    return new Uri(string.Format("http://twitpic.com/show/full/{0}", filePath), UriKind.Absolute);
                }
                catch (Exception)
                {
                }
            }
            else if (targetUrl.ToLower().Contains("yfrog.com/"))
            {
                return new Uri(targetUrl + ":iphone", UriKind.Absolute);
            }
            else if (targetUrl.ToLower().Contains("//sdrv.ms/") || targetUrl.ToLower().Contains("//1drv.ms/"))
            {
                var newUrl = "https://apis.live.net/v5.0/skydrive/get_item_preview?url=" + targetUrl;
                return new Uri(newUrl, UriKind.Absolute);
            }
            else if (targetUrl.ToLower().Contains("2instawithlove.com/p/"))
            {
                return new Uri(targetUrl, UriKind.Absolute);
            }
            else if (targetUrl.ToLower().Contains("photoplay.net/photos/"))
            {
                return new Uri(targetUrl, UriKind.Absolute);
            }
            else if (targetUrl.ToLower().Contains("/flic.kr/p/"))
            {

                var newTargetUrl = string.Empty;

                var t = Task.Run(async () =>
                {

                    var api = new FlickrApi();

                    var photoId = targetUrl.Substring(targetUrl.LastIndexOf('/') + 1);
                    long newPhotoId = api.DecodeBase58(photoId);

                    var result = await api.GetPhotoDetails(newPhotoId.ToString(CultureInfo.InvariantCulture));

                    if (result != null)
                    {
                        var thumb = result.sizes.size.FirstOrDefault(x => x.label == "Medium");
                        if (thumb != null)
                        {
                            newTargetUrl = thumb.source;
                        }
                    }
                });

                t.Wait(3000);

                if (!string.IsNullOrEmpty(newTargetUrl))
                {

                    return new Uri(newTargetUrl, UriKind.Absolute);

                }

            }

            return null;
        }
    }
}