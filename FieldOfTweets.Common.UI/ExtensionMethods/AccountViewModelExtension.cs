// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

using FieldOfTweets.Common.DataStorage;
using FieldOfTweets.Common.UI.ViewModels;

namespace FieldOfTweets.Common.UI.ExtensionMethods
{
    public static class AccountViewModelExtension
    {
        public static AccountViewModel AsViewModel(this ProfileTable item)
        {
            var account = new AccountViewModel
                              {
                                  DisplayName = item.DisplayName,
                                  ScreenName =
                                      (item.ProfileType == ApplicationConstants.AccountTypeEnum.Facebook ||
                                       item.ProfileType == ApplicationConstants.AccountTypeEnum.Soundcloud)
                                          ? item.ScreenName
                                          : "@" + item.ScreenName,
                                  UseToPost = item.UseToPost ?? false,
                                  Id = item.Id,
                                  ProfileType = item.ProfileType
                              };

            account.ImageUrl = item.CachedImageUri;

            return account;
        }
    }
}