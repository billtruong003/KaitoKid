using Squido.JungleXRKit.Core;

namespace Teabag.Core
{
    public interface IPerkSettings : ISettings
    {
        BasePerkDataObject[] PerkDataBase { get; }
    }
}
