namespace Stratton.Data
{
    public interface IDataExportTypeList
    {
    }

    public sealed class BaseDataExportType : IDataExportTypeList
    {
        public const string None = "None";
        public const string Json = "JSON";
    }
}