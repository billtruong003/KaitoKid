using System.Text;
using UnityEngine;
using BillGameCore;
using Mythfall.Localization;

namespace Mythfall.DebugTools
{
    /// <summary>
    /// TEMP — drag onto any GameObject in MenuScene to dump every known localization
    /// key on Start, switch language vi → en → vi, and verify missing-key behavior.
    /// Remove once Sprint 0 is verified.
    /// </summary>
    public class LocalizationTester : MonoBehaviour
    {
        // Mirrors current keys in Resources/Localization/lang_vi.json + lang_en.json.
        // Update this list when you add new keys in subsequent sprints.
        static readonly string[] TestKeys =
        {
            "ui.menu.play",
            "ui.menu.settings",
            "ui.menu.characters",
            "ui.menu.coming_soon",
            "ui.common.ok",
            "ui.common.cancel",
            "ui.common.back",
            "ui.common.loading",
        };

        void Start()
        {
            var loc = ServiceLocator.Get<LocalizationService>();
            if (loc == null)
            {
                Debug.LogError("[LocTest] LocalizationService not registered — did GameBootstrap run?");
                return;
            }

            Dump(loc, "INITIAL");

            loc.SetLanguage("en");
            Dump(loc, "AFTER SetLanguage(en)");

            loc.SetLanguage("vi");
            Dump(loc, "AFTER SetLanguage(vi)");

            // Edge cases
            Debug.Log($"[LocTest] missing.key → \"{loc.Get("missing.key")}\"  (expect [missing.key] + warning)");
            Debug.Log($"[LocTest] HasKey(\"ui.menu.play\") → {loc.HasKey("ui.menu.play")}  (expect True)");
            Debug.Log($"[LocTest] HasKey(\"missing.key\") → {loc.HasKey("missing.key")}  (expect False)");

            // Format placeholder example (no key currently uses {0} but verify the path is alive)
            Debug.Log($"[LocTest] GetFormatted(\"ui.menu.play\", 99) → \"{loc.GetFormatted("ui.menu.play", 99)}\"  (expect raw value, no formatting since no {{0}})");
        }

        static void Dump(LocalizationService loc, string label)
        {
            var sb = new StringBuilder(512);
            sb.AppendLine($"[LocTest] === {label} | language={loc.CurrentLanguage} ===");
            foreach (var k in TestKeys)
                sb.AppendLine($"  {k} = \"{loc.Get(k)}\"");
            Debug.Log(sb.ToString());
        }
    }
}
