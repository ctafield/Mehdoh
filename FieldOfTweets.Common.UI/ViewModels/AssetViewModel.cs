using FieldOfTweets.Common.DataStorage;

namespace FieldOfTweets.Common.UI.ViewModels
{

    public class AssetViewModel
    {
        
        public long Id { get; set; }

        public AssetTypeEnum Type { get; set; }

        public string ShortValue { get; set; }

        public string LongValue { get; set; }

        public int StartOffset { get; set; }

        public int EndOffset { get; set; }

        public long EntityId { get; set; }

    }

}
