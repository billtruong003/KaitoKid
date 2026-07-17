using System;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using UnityEngine;

namespace Teabag.Player
{
/// <summary>
/// Reads the locally saved cosmetics from IDataPersistenceService.
/// Mirrors the core logic of AuthenticationUtils.Cosmetics so that Rig/ scripts
/// (Player.asmdef) don't need to reference Assembly-CSharp.
/// </summary>
public static class PlayerCosmeticsHelper
{
    public static Dictionary<CosmeticSlot, string> GetSavedCosmetics()
    {
        string[] enumNames = Enum.GetNames(typeof(CosmeticSlot));
        Dictionary<CosmeticSlot, string> cos = new Dictionary<CosmeticSlot, string>();
        for (int i = 0; i < enumNames.Length; i++)
        {
            string cosmeticSlotToName = ServiceLocator.Get<IDataPersistenceService>()?.LoadData<string>($"Cosmetic{enumNames[i]}", "") ?? "";
            cos.Add((CosmeticSlot)i, cosmeticSlotToName);
        }
        return cos;
    }
}
}
