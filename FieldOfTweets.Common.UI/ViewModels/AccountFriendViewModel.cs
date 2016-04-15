// *********************************************************************************************************
// <copyright file="SearchPage.xaml.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary> Mehdoh for Windows Phone </summary>
// *********************************************************************************************************

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class AccountFriendViewModel
    {
        #region Properties

        public bool StatusChecked { get; set; }
        public string ScreenName { get; set; }
        public long Id { get; set; }
        public bool IsFriend { get; set; }
        public bool IsFollowingYou { get; set; }

        #endregion
    }
}