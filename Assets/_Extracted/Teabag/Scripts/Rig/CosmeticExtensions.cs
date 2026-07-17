using PlayFab.ClientModels;

namespace Teabag.Player
{
/// <summary>
/// Extension methods for CatalogItem used by Player.asmdef scripts.
/// Mirrors Teabag.Authentication.AuthenticationUtils.GetCosmetic so that
/// Rig/ scripts don't need to reference Assembly-CSharp.
/// </summary>
public static class CosmeticExtensions
{
    public static Cosmetic? GetCosmetic(this CatalogItem item)
    {
        if (item == null) return null;
        return new Cosmetic(item);
    }
}
}
