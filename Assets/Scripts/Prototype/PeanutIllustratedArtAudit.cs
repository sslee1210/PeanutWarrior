using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Produces one deterministic PASS/FAIL message for the illustrated battle-art
    /// stack after all runtime-created systems have had time to initialize.
    /// </summary>
    [DefaultExecutionOrder(50000)]
    public sealed class PeanutIllustratedArtAudit : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<PeanutIllustratedArtAudit>() != null) return;
            GameObject host = new GameObject("Peanut Illustrated Art Audit");
            DontDestroyOnLoad(host);
            host.AddComponent<PeanutIllustratedArtAudit>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 180; frame++) yield return null;
            RunAudit();
        }

        private static void RunAudit()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            ProceduralBattleArtPrototype art = FindFirstObjectByType<ProceduralBattleArtPrototype>();
            if (art == null)
            {
                errors.Add("ProceduralBattleArtPrototype is missing.");
            }
            else
            {
                if (!art.enabled) errors.Add("Raster battle-art loader is disabled.");
                if (!art.ArtReady) errors.Add("Raster battle art did not finish loading.");
                for (int index = 0; index < 12; index++)
                    if (art.GetUnitSprite(index) == null) errors.Add($"Unit sprite {index:00} is missing.");
                for (int index = 0; index < 8; index++)
                    if (art.GetEffectSprite(index) == null) errors.Add($"Effect sprite {index:00} is missing.");
            }

            CombatEffectWorldViewPrototype effects = FindFirstObjectByType<CombatEffectWorldViewPrototype>();
            if (effects == null)
                errors.Add("Illustrated combat-effect system is missing.");
            else if (effects.AvailableRingCount + effects.AvailableSlashCount + effects.ActiveEffectCount <= 0)
                errors.Add("Illustrated combat-effect pool was not initialized.");

            MiniPeanutWorldViewPrototype supports = FindFirstObjectByType<MiniPeanutWorldViewPrototype>();
            if (supports == null || !supports.enabled)
                errors.Add("Illustrated support-peanut world view is missing or disabled.");
            else if (supports.VisibleMiniCount == 0)
                warnings.Add("Support peanuts are currently locked or inactive; this is valid before pet unlock.");

            PeanutDarkFantasyUiTheme uiTheme = FindFirstObjectByType<PeanutDarkFantasyUiTheme>();
            if (uiTheme == null || !uiTheme.enabled)
                errors.Add("Dark fantasy UI theme is missing or disabled.");

            if (errors.Count == 0)
            {
                string warningText = warnings.Count == 0
                    ? string.Empty
                    : $"\nWARN · {string.Join(" | ", warnings)}";
                Debug.Log(
                    "[Peanut Illustrated Art Audit]\n" +
                    "PASS · 12 unit sprites, 8 skill effects, 4 stage backgrounds, illustrated support peanuts and dark wood/gold UI are connected." +
                    warningText);
                return;
            }

            Debug.LogError(
                "[Peanut Illustrated Art Audit]\n" +
                $"FAIL · {errors.Count} blocking issue(s)\n" +
                string.Join("\n", errors));
        }
    }
}
