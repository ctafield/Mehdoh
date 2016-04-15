#if WINDOWS_PHONE
using Microsoft.Phone.Marketplace;
#endif

namespace FieldOfTweets.Common
{
    public static class LicenceInfo
    {

        private static bool? _isTrial;       

        public static void ClearLicenceCache()
        {
            _isTrial = null;
        }

        public static bool IsTrial()
        {


#if DEBUG
            if (!_isTrial.HasValue)
            {
                var licenseInfo = new LicenseInformation();
                _isTrial = licenseInfo.IsTrial();
            }

            return _isTrial.Value;
#else

            if (!_isTrial.HasValue)
            {
                var licenseInfo = new LicenseInformation();
                _isTrial = licenseInfo.IsTrial();
            }

            return _isTrial.Value;
#endif

        }

    }

}
