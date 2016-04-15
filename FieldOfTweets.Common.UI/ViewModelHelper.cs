// *********************************************************************************************************
// <copyright file="ViewModelHelper.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System.Collections.Generic;
using System.Linq;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.UI.ExtensionMethods;
using FieldOfTweets.Common.UI.ViewModels;

namespace FieldOfTweets.Common.UI
{
    public class ViewModelHelper
    {
        public static List<MentionsViewModel> MentionsResponseToView(long accountId, List<ResponseTweet> statuses)
        {
            return statuses.Select<ResponseTweet, MentionsViewModel>(status => status.AsMentionViewModel(accountId)).ToList();
        }

        public static List<TimelineViewModel> TimelineResponseToView(long accountId, List<ResponseTweet> statuses)
        {
            return statuses.Select<ResponseTweet, TimelineViewModel>(status => status.AsViewModel(accountId)).ToList();
        }

        public static List<MessagesViewModel> MessagesResponseToView(long accountId, List<ResponseDirectMessage> statuses)
        {
            return statuses.Select<ResponseDirectMessage, MessagesViewModel>(status => status.AsViewModel(accountId)).ToList();
        }

        public static List<FavouritesViewModel> FavouritesResponseToView(long accountId, List<ResponseTweet> favourites)
        {
            return favourites.Select<ResponseTweet, FavouritesViewModel>(status => status.AsFavouritesViewModel(accountId)).ToList();
        }
    }
}