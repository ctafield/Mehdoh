using System.Collections.Generic;
using System.Linq;
using FieldOfTweets.Common.POCO;

namespace FieldOfTweets.Common.ColumnConfig
{

    public static class ColumnHelper
    {

        private const string ColumnConfigFile = "column_config.json";

        private static List<ColumnModel> _columnConfig;

        public static List<ColumnModel> ColumnConfig
        {
            get
            {
                if (_columnConfig == null)
                {
                    LoadConfig();
                }

                return _columnConfig;
            }
            set
            {
                _columnConfig = value;
                ResetSortOrder();
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            var contents = DataStorageHelper.SerialiseResponseObject(_columnConfig);
            DataStorageHelper.SaveContentsToFile(ColumnConfigFile, contents);
        }

        private static void LoadConfig()
        {
            var dsh = new DataStorageHelper();
            _columnConfig = dsh.LoadExistingState<List<ColumnModel>>(ColumnConfigFile) ?? new List<ColumnModel>();
        
        }

        public static void AddNewColumn(ColumnModel newItem)
        {

            if (_columnConfig == null)
                LoadConfig();

            if (_columnConfig == null)
                _columnConfig = new List<ColumnModel>();

            if (_columnConfig.Any(x => x.Value == newItem.Value && x.ColumnType == newItem.ColumnType && x.AccountId == newItem.AccountId))
                return;

            if (!_columnConfig.Any())
                newItem.Order = 0;
            else
                newItem.Order = _columnConfig.Max(x => x.Order) + 1;

            _columnConfig.Add(newItem);

            SaveConfig();
        }

        public static void RefreshConfig()
        {
            _columnConfig = null;
            LoadConfig();
        }

        public static void RemoveColumns(IList<ColumnModel> sourceItems)
        {
            _columnConfig.RemoveAll(x => sourceItems.Any(y => x.AccountId == y.AccountId && x.ColumnType == y.ColumnType && x.Value == y.Value));
            SaveConfig();
            RefreshConfig();
        }

        public static void ResetSortOrder()
        {
            var currentSort = 0;

            foreach (var item in ColumnConfig)
            {
                item.Order = currentSort;
                currentSort++;
            }

            SaveConfig();
        }

        public static void UpdateColumn(ColumnModel dataItem)
        {
            // Nothing to update
            if (!ColumnConfig.Any(x => x.Value == dataItem.Value && x.ColumnType == dataItem.ColumnType && x.AccountId == dataItem.AccountId))
                return;

            var existingItem = ColumnConfig.First(x => x.Value == dataItem.Value && x.ColumnType == dataItem.ColumnType && x.AccountId == dataItem.AccountId);

            existingItem.Order = dataItem.Order;
            existingItem.RefreshOnStartup = dataItem.RefreshOnStartup;

            SaveConfig();
        }

        public static void RemoveColumnsForUser(long accountId)
        {
            ColumnConfig.RemoveAll(x => x.AccountId == accountId);
            SaveConfig();

            RefreshConfig();
        }

        public static void CheckValidColumns()
        {

            var invalidCols = ColumnConfig.Where(x => x.ColumnType == ApplicationConstants.ColumnTypeFacebook ||
                                       x.ColumnType == ApplicationConstants.ColumnTypeInstagram ||
                                       x.ColumnType == ApplicationConstants.ColumnTypeSoundcloud).ToList();
            if (invalidCols.Count > 0)
            {
                RemoveColumns(invalidCols);
            }

        }

    }

}
