// *********************************************************************************************************
// <copyright file="AccountSettingsHelper.cs" company="My Own Limited">
// Copyright (c) 2013 All Rights Reserved
// </copyright>
// <summary>Mehdoh for Windows Phone</summary>
// *********************************************************************************************************

using System;
using System.Linq;
using System.Threading.Tasks;
using FieldOfTweets.Common.Api;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;
using Newtonsoft.Json;

namespace FieldOfTweets.Common
{
    public class AccountSettingsHelper : MehdohApi
    {
        #region Constructor

        public AccountSettingsHelper(long accountId)
        {
            AccountId = accountId;
        }

        #endregion

        #region Properties

        public long AccountId { get; private set; }

        #endregion

        public async Task<ResponseAccountSettings> GetAccountSettings()
        {

            // Do whatever account settings does
            try
            {
                using (var dh = new MainDataContext())
                {
                    if (dh.AccountSettingCache.Any(x => x.ProfileId == AccountId))
                    {
                        var res = dh.AccountSettingCache.FirstOrDefault();
                        if (res != null)
                        {
                            var diff = DateTime.Now.Subtract(res.DateCached);

                            if (diff.TotalDays < 4) // number of days to cache the settings for.... lets go for 4.
                            {
                                return DeserialiseResponse<ResponseAccountSettings>(res.CachedContent); // GetResponseObject<ResponseAccountSettings>(res.CachedContent);
                            }

                            // Failed so lets grab the settings again to overwrite them
                            var updateApi = new TwitterApi(AccountId);
                            var result = await updateApi.GetAccountSettings();
                            SaveAccountSettingsToCache(AccountId, result);
                            return result;

                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            // If we're here, then we need to go get the new one.
            var api = new TwitterApi(AccountId);
            var result3 = await api.GetAccountSettings();
            SaveAccountSettingsToCache(AccountId, result3);
            return result3;
        }

        private void SaveAccountSettingsToCache(long accountId, ResponseAccountSettings accountSettings)
        {

            if (accountSettings == null)
            {
                return;
            }


            try
            {

                // Save the values
                string newCachedContent = JsonConvert.SerializeObject(accountSettings);

                using (var dh = new MainDataContext())
                {
                    if (dh.AccountSettingCache.Any(x => x.ProfileId == accountId))
                    {
                        var res = dh.AccountSettingCache.FirstOrDefault(x => x.ProfileId == accountId);
                        if (res != null)
                        {
                            res.DateCached = DateTime.Now;
                            res.CachedContent = newCachedContent;
                        }
                        dh.SubmitChanges();
                    }
                    else
                    {
                        var res = new AccountSettingCache
                        {
                            CachedContent = newCachedContent,
                            DateCached = DateTime.Now,
                            ProfileId = accountId,
                            UseSSL = false
                        };
                        dh.AccountSettingCache.InsertOnSubmit(res);
                        dh.SubmitChanges();
                    }
                }
            }
            catch (Exception)
            {
                
            }


        }

    }
}