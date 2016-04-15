using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using FieldOfTweets.Common.Api.Twitter;
using FieldOfTweets.Common.Api.Twitter.Responses;
using FieldOfTweets.Common.DataContext;
using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.UI.Friends
{

    public class FriendsHelper
    {

        public event EventHandler<GetFriendEventArgs> GetFriendsCompletedEvent;

        public List<long> UnknownFriends { get; set; }

        public long AccountId { get; private set; }

        public FriendsHelper(long accountId)
        {
            UnknownFriends = new List<long>();
            AccountId = accountId;
        }


        private static object GetUserLock = new object();

        private UserLookupTable GetUser(long id)
        {

            UserLookupTable user;

            user = FriendsHelperCache.GetUser(id);
            if (user != null)
                return user;

            user = new UserLookupTable();

            // Obtain the virtual store for the application.
            using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
            {

                lock (GetUserLock)
                {

                    var filePath = ApplicationConstants.UserCacheStorageFolder + "\\" + id.ToString() + ".user";

                    if (!myStore.FileExists(filePath))
                        return null;

                    try
                    {
                        using (var stream = new StreamReader(myStore.OpenFile(filePath, FileMode.Open, FileAccess.Read)))
                        {
                            var res = stream.ReadLine();

                            if (res == null)
                                return null;

                            user.UserId = long.Parse(res);

                            user.ScreenName = stream.ReadLine();
                            user.DisplayName = stream.ReadLine();
                            user.ProfileImageUrl = stream.ReadLine();
                        }

                        FriendsHelperCache.AddUser(user);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogging.ErrorLogger.LogException("FriendsHelper.GetUser", ex);
                        return null;
                    }
                }

                return user;
            }

        }

        public void AddFriendToSearch(long id)
        {

            var cachedUser = GetUser(id);

            if (cachedUser == null)
            {
                UnknownFriends.Add(id);
            }
            else
            {
                var user = new FriendUser()
                               {
                                   DisplayName = cachedUser.DisplayName,
                                   Id = cachedUser.UserId,
                                   ProfileImageUrl = cachedUser.ProfileImageUrl,
                                   ScreenName = cachedUser.ScreenName
                               };

                var args = new GetFriendEventArgs()
                {
                    FriendUser = user
                };

                if (GetFriendsCompletedEvent != null)
                    GetFriendsCompletedEvent(this, args);

            }

        }

        private List<List<long>> ChunkedLookup { get; set; }
        private int ChunkCounter { get; set; }

        public void GetFriends()
        {

            ChunkedLookup = new List<List<long>>();
            ChunkCounter = 0;

            var chunkSize = 100;

            // Now somehow get the unknown ones
            for (int i = 0; i < UnknownFriends.Count; i += chunkSize)
            {
                var range = chunkSize;

                if (i + range > UnknownFriends.Count)
                    range = UnknownFriends.Count - i;

                var res = UnknownFriends.GetRange(i, range);
                ChunkedLookup.Add(res);
            }

            // nothing to do?
            if (ChunkedLookup.Count == 0)
                return;

            var api = new TwitterApi(AccountId);

            api.UsersLookupCompletedEvent += new EventHandler<UserLookupEventArgs>(api_UsersLookupCompletedEvent);
            api.UsersLookup(ChunkedLookup[ChunkCounter++]);

        }

        public void SaveUser(FriendUser user)
        {

            lock (GetUserLock)
            {
                // Obtain the virtual store for the application.
                using (var myStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    // Create a new folder and call it "MyFolder".

                    if (!myStore.DirectoryExists(ApplicationConstants.UserCacheStorageFolder))
                        myStore.CreateDirectory(ApplicationConstants.UserCacheStorageFolder);

                    var filePath = ApplicationConstants.UserCacheStorageFolder + "\\" + user.Id.ToString() + ".user";

                    string outPutInfo = string.Format("{0}\n{1}\n{2}\n{3}", user.Id, user.ScreenName, user.DisplayName,
                                                      user.ProfileImageUrl);

                    using (var writer = new StreamWriter(myStore.OpenFile(filePath, FileMode.Create)))
                    {
                        writer.WriteLine(outPutInfo);
                    }
                }
            }

        }


        void api_UsersLookupCompletedEvent(object sender, UserLookupEventArgs e)
        {

            if (e == null)
                return;

            try
            {

                using (var dh = new MainDataContext())
                {

                    foreach (var user in e.UserProfiles)
                    {
                        var newUser = new FriendUser()
                                          {
                                              DisplayName = user.name,
                                              Id = user.id,
                                              ProfileImageUrl = user.profile_image_url,
                                              ScreenName = user.screen_name
                                          };

                        var args = new GetFriendEventArgs()
                        {
                            FriendUser = newUser
                        };

                        if (GetFriendsCompletedEvent != null)
                            GetFriendsCompletedEvent(this, args);

                        SaveUser(newUser);

                    }

                    dh.SubmitChanges();

                }

            }
            catch (Exception)
            {
            }

            e.UserProfiles = null;

            var oldApi = sender as TwitterApi;

            if (ChunkCounter < ChunkedLookup.Count)
            {
                var api = new TwitterApi(oldApi.AccountId);
                api.UsersLookupCompletedEvent += api_UsersLookupCompletedEvent;                
                api.UsersLookup(ChunkedLookup[ChunkCounter++]);                
            }


        }



    }

}
