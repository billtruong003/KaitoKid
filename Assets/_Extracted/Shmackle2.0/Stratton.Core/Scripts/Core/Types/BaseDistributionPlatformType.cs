namespace Stratton.Core.Types
{
    public interface IDistributionPlatformTypeList : IBaseTypeList
    {
    }

    public class BaseDistributionPlatformType : IDistributionPlatformTypeList
    {
        public static readonly DistributionPlatformType None = new DistributionPlatformType(nameof(None));
        public static readonly DistributionPlatformType AppStore = new DistributionPlatformType(nameof(AppStore));
        public static readonly DistributionPlatformType GooglePlay = new DistributionPlatformType(nameof(GooglePlay));
        public static readonly DistributionPlatformType SamsungGalaxyStore = new DistributionPlatformType(nameof(SamsungGalaxyStore));
        public static readonly DistributionPlatformType HuaweiAppGallery = new DistributionPlatformType(nameof(HuaweiAppGallery));
        public static readonly DistributionPlatformType WindowsStore = new DistributionPlatformType(nameof(WindowsStore));
        public static readonly DistributionPlatformType Steam = new DistributionPlatformType(nameof(Steam));
        public static readonly DistributionPlatformType Meta = new DistributionPlatformType(nameof(Meta));
    }
}