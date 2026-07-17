namespace Stratton.Data
{
    public interface IDataHostTypeList
    {
    }

    public sealed class BaseDataHostType : IDataHostTypeList
    {
        public const string None = "None";
        public const string S3 = "S3";
        public const string PlayFab = "PlayFab";
        public const string PlayerPrefs = "PlayerPrefs";
        public const string SQLite = "SQLite";
    }
}