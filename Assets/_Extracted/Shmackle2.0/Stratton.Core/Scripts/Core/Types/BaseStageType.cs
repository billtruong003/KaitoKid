namespace Stratton.Core.Types
{
    public interface IStageTypeList : IBaseTypeList
    {
    }

    public sealed class BaseStageType : IStageTypeList
    {
        public static readonly StageType Dev = new StageType(nameof(Dev));
        public static readonly StageType Production = new StageType(nameof(Production));
        public static readonly StageType Stage = new StageType(nameof(Stage));
        public static readonly StageType Hotfix = new StageType(nameof(Hotfix));
        public static readonly StageType Demo = new StageType(nameof(Demo));
    }
}