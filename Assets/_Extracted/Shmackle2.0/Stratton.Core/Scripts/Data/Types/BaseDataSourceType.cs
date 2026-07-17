namespace Stratton.Data
{
    public interface IDataSourceTypeList
    {
    }

    public sealed class BaseDataSourceType : IDataSourceTypeList
    {
        public const string None = "None";
        public const string GoogleSheets = "GoogleSheets";
    }
}