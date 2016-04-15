using System;
using System.Collections.Generic;
using System.Linq;
using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.UI.Friends
{

    internal static class FriendsHelperCache
    {

        private static List<UserLookupTable> Users { get; set; }

        public static UserLookupTable GetUser(long id)
        {
            
            if (Users == null)
                return null;

            try
            {
                return Users.SingleOrDefault(x => x.UserId == id);
            }
            catch (Exception)
            {

                return null;
            }
            
        }

        public static void AddUser(UserLookupTable user)
        {

            if (Users == null)
                Users = new List<UserLookupTable>();

            Users.Add(user);            
        }


    }

}