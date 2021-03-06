﻿using System;
using System.Windows;

namespace FieldOfTweets.Common.UI.ViewModels
{
    public class InstagramUserProfileViewModel
    {
        private string _fullName;

        public string ScreenName { get; set; }

        public string FullName
        {
            get
            {
                if (!string.IsNullOrEmpty(_fullName))                
                    return _fullName;
                return ScreenName;
            }
            set { _fullName = value; }
        }

        public int? Followers { get; set; }

        public int? Following { get; set; }

        public string WebSite { get; set; }

        public string Biography { get; set; }

        public string ProfileImageUrl { get; set; }

        public Uri ImageSource
        {
            get
            {
                return new Uri(ProfileImageUrl, UriKind.Absolute);
            }
        }


        // todo: this
        public Visibility IsProtectedVisibility { get { return Visibility.Collapsed; } }

        public string Id { get; set; }
    }
}
