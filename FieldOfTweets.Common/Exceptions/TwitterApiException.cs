using System;

namespace FieldOfTweets.Common.Exceptions
{

    public class TwitterApiException : Exception
    {

        public string ApiError { get; set; }

        public TwitterApiException(string error)
        {
            ApiError = error;
        }

    }

}
