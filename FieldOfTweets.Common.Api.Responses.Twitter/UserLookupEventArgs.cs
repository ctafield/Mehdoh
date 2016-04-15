using System;
using System.Collections.Generic;

namespace FieldOfTweets.Common.Api.Twitter.Responses
{
    public class UserLookupEventArgs : EventArgs
    {

        public List<ResponseGetUserProfile> UserProfiles { get; set; }

    }

}
