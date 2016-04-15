// *********************************************************************************************************
// <copyright file="PeopleHubExamples.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FieldOfTweets.Common;
using FieldOfTweets.Common.DataContext;

namespace Mitter.UI.Settings.Convertors
{
    public class PeopleHubExamples
    {
        private static long? _accountId;

        private static long AccountId
        {
            get
            {
                if (!_accountId.HasValue)
                {
                    using (var dh = new MainDataContext())
                    {
                        var firstProfile = dh.Profiles.FirstOrDefault(x => x.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter);
                        if (firstProfile != null)
                        {
                            _accountId = firstProfile.Id;
                        }
                    }
                }
                if (_accountId.HasValue)
                    return _accountId.Value;
                return default(long);
            }
        }

        public ImageSource MehdohLogo
        {
            get
            {
                var sh = new ShellHelper();

                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var uri = sh.GetMehdohLogo().ToString().Replace("isostore:/", "");
                    var bi = new BitmapImage();

                    using (var stream = store.OpenFile(uri, FileMode.Open))
                    {
                        bi.SetSource(stream);
                    }

                    return bi;
                }
            }
        }

#if WP7

        public ImageSource MehdohLogoGreen
        {
            get
            {
                var sh = new ShellHelper();

                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var uri = sh.GetMehdohLogoGreen().ToString().Replace("isostore:/", "");
                    var bi = new BitmapImage();

                    using (var stream = store.OpenFile(uri, FileMode.Open))
                    {
                        bi.SetSource(stream);
                    }

                    return bi;
                }
            }
        }

        public ImageSource Twitter
        {
            get
            {
                string userProfileImage = StorageHelper.SharedCachedImageUri(AccountId);
                var sh = new ShellHelper();
                var uri = sh.GetAvatarStyle(AccountId, userProfileImage).ToString().Replace("isostore:/", "");

                var bi = new BitmapImage();

                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (var stream = store.OpenFile(uri, FileMode.Open))
                    {
                        bi.SetSource(stream);
                    }
                }

                return bi;
            }
        }

        public ImageSource PeopleHub
        {
            get
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var samplePath = ApplicationConstants.ShellContentFolder + "/sample_peopletile.png";
                    if (!store.FileExists(samplePath))
                    {
                        var sh = new ShellHelper();
                        samplePath = sh.GetPeopleHubStyle(true).ToString().Replace("isostore:/", "");
                    }

                    var bi = new BitmapImage();

                    using (var stream = store.OpenFile(samplePath, FileMode.Open))
                    {
                        bi.SetSource(stream);
                    }

                    return bi;
                }
            }
        }

        public ImageSource Hybrid
        {
            get
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    var samplePath = ApplicationConstants.ShellContentFolder + "/sample_hybridtile.png";
                    if (!store.FileExists(samplePath))
                    {
                        var sh = new ShellHelper();
                        samplePath = sh.GetHybridHubStyle(AccountId, true).ToString().Replace("isostore:/", "");
                    }

                    var bi = new BitmapImage();

                    using (var stream = store.OpenFile(samplePath, FileMode.Open))
                    {
                        bi.SetSource(stream);
                    }

                    return bi;
                }
            }
        }

#endif
    }
}