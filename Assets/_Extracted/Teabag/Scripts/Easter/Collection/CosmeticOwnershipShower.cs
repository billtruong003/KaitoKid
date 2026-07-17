using System.Collections.Generic;
using System.Text;
using Teabag.Authentication;
using Teabag.Core;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class CosmeticOwnershipShower : MonoBehaviour
{
    public List<string> cosmeticNames = new List<string>();
    TMP_Text text;

    private void OnEnable()
    {
        Refresh();
    }

    public static void RefreshAll()
    {
        foreach (CosmeticOwnershipShower shower in FindObjectsOfType<CosmeticOwnershipShower>())
            shower.Refresh();
    }

    public void Refresh()
    {
        StringBuilder builder = new StringBuilder();

        foreach (string cosmetic in cosmeticNames)
        {
            bool owns = AuthenticationUtils.OwnsCosmetic(cosmetic);
            if (owns)
                builder.AppendLine($"<s>{cosmetic}</s>");
            else
                builder.AppendLine(cosmetic);

            builder.AppendLine();
        }

        if (text == null)
            text = GetComponent<TMP_Text>();

        text.text = builder.ToString();
    }
}
