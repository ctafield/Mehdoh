using System;
using System.Linq;
using FieldOfTweets.Common.DataContext;

namespace FieldOfTweets.Common.POCO
{

    public class ColumnModel : IComparable<ColumnModel>
    {

        private string _fullDisplayName;

        public string FullDisplayName
        {
            get
            {
                if (_fullDisplayName == null)
                {
                    using (var dh = new MainDataContext())
                    {
                        var prof = dh.Profiles.SingleOrDefault(x => x.Id == AccountId);
                        if (prof == null)
                        {
                            _fullDisplayName = DisplayName;                            
                        }
                        else
                        {
                            if (prof.ProfileType == ApplicationConstants.AccountTypeEnum.Twitter || prof.ProfileType == ApplicationConstants.AccountTypeEnum.Instagram)
                                _fullDisplayName = string.Format(@"@{0}\{1} ({2})", prof.ScreenName, DisplayName, ColumnTypeDisplay);
                            else
                                _fullDisplayName = string.Format(@"{0}\{1} ({2})", prof.ScreenName, DisplayName, ColumnTypeDisplay);
                        }

                    }
                }
                return _fullDisplayName;
            }
        }

        protected string ColumnTypeDisplay
        {
            get
            {
                switch (ColumnType)
                {
                    case ApplicationConstants.ColumnTypeTwitter:
                        return "twitter";
                    case ApplicationConstants.ColumnTypeTwitterList:
                        return "list";
                    case ApplicationConstants.ColumnTypeInstagram:
                        return "instagram";
                    case ApplicationConstants.ColumnTypeFacebook:
                        return "facebook";
                    case ApplicationConstants.ColumnTypeTwitterSearch:
                        return "search";
                    case ApplicationConstants.ColumnTypeSoundcloud:
                        return "soundcloud";
                    default:
                        return string.Empty;
                }
            }
        }

        public string DisplayName { get; set; }
        public string Value { get; set; }
        public int ColumnType { get; set; }
        public int Order { get; set; }


        private bool _refreshOnStartup;
        public bool RefreshOnStartup
        {
            get
            {
                if (ColumnType == ApplicationConstants.ColumnTypeSoundcloud || 
                    ColumnType == ApplicationConstants.ColumnTypeFacebook)
                    return true;

                return _refreshOnStartup;
            }
            set
            {
                _refreshOnStartup = value;
            }
        }

        public long AccountId { get; set; }

        public int CompareTo(ColumnModel newitem)
        {

            if (newitem == null)
                return -1;

            if (AccountId != newitem.AccountId)
                return -1;

            if (Value != newitem.Value)
                return -1;

            if (ColumnType != newitem.ColumnType)
                return -1;

            if (DisplayName != newitem.DisplayName)
                return -1;

            return 0;
        }

        public bool IsAllowedToChangeStartup
        {
            get
            {
                if (ColumnType == ApplicationConstants.ColumnTypeSoundcloud ||
                    ColumnType == ApplicationConstants.ColumnTypeFacebook)
                    return false;

                return true;
            }
        }

    }

}
