using System;
using HarmonyLib;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.Upgrade;

namespace Crosspath533.Patches;

/// <summary>
/// Matches doombubbles/UltimateCrosspathing: UpgradeObject.CheckBlockedPath returns an int
/// representing the highest tier currently allowed on __instance.path (e.g. 0, 2, 3, or 5).
/// </summary>
[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.CheckBlockedPath))]
internal static class UpgradeObject_CheckBlockedPath
{
    [HarmonyPostfix]
    internal static void Postfix(UpgradeObject __instance, ref int __result)
    {
        if (!Main.Enabled.GetValue())
            return;

        var tower = __instance.tts?.tower;
        var towerModel = tower?.towerModel;
        if (towerModel == null || towerModel.tiers == null || towerModel.tiers.Count < CrosspathRules.PathCount)
            return;

        if (towerModel.isHero || towerModel.isParagon)
            return;

        int[] currentTiers =
        {
            towerModel.tiers[0],
            towerModel.tiers[1],
            towerModel.tiers[2]
        };

        int maxPrimary = Main.MaxPrimaryTier.GetValue();
        int maxSecondary = Main.MaxSecondaryTier.GetValue();
        int allowedTier = CrosspathRules.GetMaxAllowedTierForPath(
            currentTiers,
            __instance.path,
            maxPrimary,
            maxSecondary
        );

        __result = Math.Max(__result, allowedTier);
    }
}

/// <summary>
/// Lazily generates and returns merged 5-3-3 TowerModels when BTD6 looks up a tower by name
/// (e.g. "DartMonkey-533").
/// </summary>
[HarmonyPatch(typeof(GameModel), nameof(GameModel.GetTowerWithName))]
internal static class GameModel_GetTowerWithName
{
    [HarmonyPostfix]
    internal static void Postfix(GameModel __instance, string name, ref TowerModel? __result)
    {
        if (__result != null || !Main.Enabled.GetValue())
            return;

        if (!CrosspathRules.TryParseTowerId(name, out string baseId, out int[] tiers))
            return;

        if (CrosspathRules.IsVanillaTiers(tiers))
            return;

        int maxPrimary = Main.MaxPrimaryTier.GetValue();
        int maxSecondary = Main.MaxSecondaryTier.GetValue();

        if (!CrosspathRules.IsValidTiers(tiers, maxPrimary, maxSecondary))
            return;

        __result = TowerMerger.GetOrCreateMergedTower(__instance, baseId, tiers);
    }
}

/// <summary>
/// Also intercepts the 4-argument GameModel.GetTower(baseId, t0, t1, t2) overload so
/// programmatic lookups for 5-3-3 towers succeed as well.
/// </summary>
[HarmonyPatch(
    typeof(GameModel),
    nameof(GameModel.GetTower),
    typeof(string),
    typeof(int),
    typeof(int),
    typeof(int)
)]
internal static class GameModel_GetTower
{
    [HarmonyPostfix]
    internal static void Postfix(
        GameModel __instance,
        string towerId,
        int pathOneTier,
        int pathTwoTier,
        int pathThreeTier,
        ref TowerModel? __result)
    {
        if (__result != null || !Main.Enabled.GetValue())
            return;

        int[] tiers = { pathOneTier, pathTwoTier, pathThreeTier };
        if (CrosspathRules.IsVanillaTiers(tiers))
            return;

        int maxPrimary = Main.MaxPrimaryTier.GetValue();
        int maxSecondary = Main.MaxSecondaryTier.GetValue();

        if (!CrosspathRules.IsValidTiers(tiers, maxPrimary, maxSecondary))
            return;

        __result = TowerMerger.GetOrCreateMergedTower(__instance, towerId, tiers);
    }
}
