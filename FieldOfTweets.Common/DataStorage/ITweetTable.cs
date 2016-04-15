using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace FieldOfTweets.Common.DataStorage
{

    public interface ITweetTable   
    {

        long TableId { get; set; }
        long Id { get; set; }
        long? RetweetOriginalId { get; set; }

        long ProfileId { get; set; }

        string ScreenName { get; set; }

        string RetweetUserScreenName { get; set; }

        bool IsRetweet { get;}

        DateTime CreatedAtFormatted { get; }

        string Description { get; set; }

        string RetweetDescription { get; set; }
        string DisplayName { get; set; }
        string ProfileImageUrl { get; set; }
        string Client { get; set; }
        bool Verified { get; set; }
        string LocationFullName { get; set; }
        string LocationCountry { get; set; }
        string RetweetUserDisplayName { get; set; }
        string RetweetUserImageUrl { get; set; }
        bool RetweetUserVerified { get; set; }

        [Column(CanBeNull = true)]       
        long? InReplyToId { get; }

        double Location1X { get; set; }
        double Location1Y { get; set; }
        double Location2X { get; set; }
        double Location2Y { get; set; }
        double Location3X { get; set; }
        double Location3Y { get; set; }
        double Location4X { get; set; }
        double Location4Y { get; set; }

        string LanguageCode { get; set; }

    }

}