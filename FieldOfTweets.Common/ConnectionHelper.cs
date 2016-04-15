using Microsoft.Phone.Net.NetworkInformation;

namespace FieldOfTweets.Common
{

    public class ConnectionHelper
    {

        public static bool IsOnWifi()
        {

            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            var currentInteface = NetworkInterface.NetworkInterfaceType;
            return (currentInteface == NetworkInterfaceType.Wireless80211 || currentInteface == NetworkInterfaceType.Ethernet);
        }

    }

}
