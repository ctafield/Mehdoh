// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

using System.Windows;
using FieldOfTweets.Common.POCO;
using FieldOfTweets.Common.UI.ViewModels;

namespace FieldOfTweets.Common.UI.ExtensionMethods
{
    public static class CustomiseItemExtensions
    {
        public static CustomiseItemCoreViewModel AsViewModel(this ColumnModel item)
        {
            var profileImageUrl = StorageHelper.GetProfileImageForUser(item.AccountId);

            var viewModel = new CustomiseItemCoreViewModel
                                {
                                    RefreshOnStartUp = item.RefreshOnStartup,
                                    Title = item.DisplayName,
                                    Type = item.ColumnType,
                                    Value = item.Value,
                                    ProfileImageUrl = profileImageUrl,                       
                                    AccountId = item.AccountId,
                                    CheckVisible = Visibility.Collapsed
                                };

            return viewModel;
        }

        public static ColumnModel AsDomainModel(this CustomiseItemCoreViewModel model)
        {
            var col = new ColumnModel
                          {
                              RefreshOnStartup = model.RefreshOnStartUp,
                              DisplayName = model.Title,
                              ColumnType = model.Type,
                              Order = model.Order,
                              Value = model.Value,
                              AccountId = model.AccountId,
                          };

            return col;
        }
    }
}