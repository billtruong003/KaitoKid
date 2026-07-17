namespace Stratton.Assets
{
    public interface IAssetsHostTypeList
    {
    }

    public sealed class BaseAssetsHostType : IAssetsHostTypeList
    {
        public const string Local = "Local";
        public const string S3 = "S3";
    }
}