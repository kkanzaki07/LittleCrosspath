using System;
using System.Collections.Generic;
using System.Linq;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Filters;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace Crosspath533;

/// <summary>
/// Lazily constructs and caches merged TowerModels for non-vanilla 5-3-3 combinations.
/// </summary>
public static class TowerMerger
{
    private static readonly HashSet<string> MergingInProgress = new();

    /// <summary>
    /// Builds a merged TowerModel for <paramref name="baseId"/> at <paramref name="tiers"/>
    /// by starting from the highest vanilla tower and layering the extra crosspath upgrades.
    /// </summary>
    public static TowerModel? GetOrCreateMergedTower(GameModel gameModel, string baseId, int[] tiers)
    {
        string targetId = CrosspathRules.FormatTowerId(baseId, tiers);

        if (MergingInProgress.Contains(targetId))
            return null;

        MergingInProgress.Add(targetId);
        try
        {
            var existing = gameModel.towers.FirstOrDefault(t => t.name == targetId);
            if (existing != null)
                return existing;

            int dominant = CrosspathRules.GetDominantPath(tiers);

            // Pick the strongest secondary path to seed from a vanilla 5-2-0-like base model
            int[] otherPaths = Enumerable.Range(0, CrosspathRules.PathCount)
                .Where(p => p != dominant)
                .OrderByDescending(p => tiers[p])
                .ToArray();

            int[] seedTiers = new int[CrosspathRules.PathCount];
            seedTiers[dominant] = tiers[dominant];
            seedTiers[otherPaths[0]] = Math.Min(2, tiers[otherPaths[0]]);
            seedTiers[otherPaths[1]] = 0;

            var seedModel = gameModel.GetTower(baseId, seedTiers[0], seedTiers[1], seedTiers[2]);
            var base000 = gameModel.GetTower(baseId, 0, 0, 0);
            if (seedModel == null || base000 == null)
                return null;

            var merged = seedModel.Duplicate();
            merged.name = targetId;
            merged.tiers = new Il2CppStructArray<int>(tiers);

            // Merge each non-dominant path that exceeds what was already in seedTiers
            foreach (int path in otherPaths)
            {
                if (tiers[path] <= seedTiers[path])
                    continue;

                int[] soloTiers = new int[CrosspathRules.PathCount];
                soloTiers[path] = tiers[path];

                var pathModel = gameModel.GetTower(baseId, soloTiers[0], soloTiers[1], soloTiers[2]);
                if (pathModel == null)
                    continue;

                MergePathIntoTower(merged, pathModel, base000);
            }

            // Rebuild appliedUpgrades from all 3 paths
            var applied = new HashSet<string>(merged.appliedUpgrades);
            for (int path = 0; path < CrosspathRules.PathCount; path++)
            {
                if (tiers[path] <= 0)
                    continue;

                int[] soloTiers = new int[CrosspathRules.PathCount];
                soloTiers[path] = tiers[path];
                var solo = gameModel.GetTower(baseId, soloTiers[0], soloTiers[1], soloTiers[2]);
                if (solo == null)
                    continue;

                foreach (string upg in solo.appliedUpgrades)
                    applied.Add(upg);
            }
            merged.appliedUpgrades = new Il2CppStringArray(applied.ToArray());

            // Wire up next valid upgrade paths for the UI
            RebuildNextUpgrades(merged, gameModel, baseId, tiers);

            gameModel.AddTowerToGame(merged);

            if (Main.DebugLogging.GetValue())
                ModHelper.Msg<Main>($"Generated merged TowerModel: {targetId}");

            return merged;
        }
        finally
        {
            MergingInProgress.Remove(targetId);
        }
    }

    private static void MergePathIntoTower(TowerModel target, TowerModel source, TowerModel base000)
    {
        // 1. Range: keep the highest range (or add delta above base000)
        float rangeDelta = Math.Max(0f, source.range - base000.range);
        target.range = Math.Max(target.range, source.range);
        if (rangeDelta > 0f && target.range < source.range + rangeDelta * 0.5f)
            target.range = Math.Max(target.range, target.range + rangeDelta * 0.5f);

        // 2. Camo detection: if source can see camo, grant it to target
        if (source.HasBehavior<OverrideCamoDetectionModel>() &&
            !target.HasBehavior<OverrideCamoDetectionModel>())
        {
            var camo = source.GetBehavior<OverrideCamoDetectionModel>().Duplicate();
            target.AddBehavior(camo);
        }

        // 3. Merge AttackModels: apply numeric weapon/projectile bonuses & new attacks
        var targetAttacks = target.GetAttackModels();
        var sourceAttacks = source.GetAttackModels();
        var baseAttacks = base000.GetAttackModels();

        for (int i = 0; i < sourceAttacks.Count; i++)
        {
            var srcAttack = sourceAttacks[i];
            if (i >= targetAttacks.Count)
            {
                // Extra attack unique to this crosspath (e.g., secondary weapon / aura)
                target.AddBehavior(srcAttack.Duplicate());
                continue;
            }

            var dstAttack = targetAttacks[i];
            var baseAttack = i < baseAttacks.Count ? baseAttacks[i] : null;
            dstAttack.range = Math.Max(dstAttack.range, srcAttack.range);

            MergeWeapons(dstAttack, srcAttack, baseAttack);
        }

        // 4. Copy any new abilities from the crosspath
        var existingAbilityNames = target.GetAbilities().Select(a => a.name).ToHashSet();
        foreach (var ability in source.GetAbilities())
        {
            if (!existingAbilityNames.Contains(ability.name))
                target.AddBehavior(ability.Duplicate());
        }
    }

    private static void MergeWeapons(
        AttackModel dstAttack,
        AttackModel srcAttack,
        AttackModel? baseAttack)
    {
        for (int w = 0; w < srcAttack.weapons.Count; w++)
        {
            var srcWeapon = srcAttack.weapons[w];
            if (w >= dstAttack.weapons.Count)
            {
                dstAttack.AddWeapon(srcWeapon.Duplicate());
                continue;
            }

            var dstWeapon = dstAttack.weapons[w];
            var baseWeapon = baseAttack != null && w < baseAttack.weapons.Count
                ? baseAttack.weapons[w]
                : null;

            // Attack speed multiplier (smaller rate = faster firing)
            if (baseWeapon != null && baseWeapon.rate > 0f && srcWeapon.rate > 0f && srcWeapon.rate < baseWeapon.rate)
            {
                float speedFactor = srcWeapon.rate / baseWeapon.rate;
                dstWeapon.rate *= speedFactor;
            }

            // Projectile pierce & damage deltas
            if (dstWeapon.projectile != null && srcWeapon.projectile != null)
            {
                float basePierce = baseWeapon?.projectile?.pierce ?? 0f;
                float pierceBonus = Math.Max(0f, srcWeapon.projectile.pierce - basePierce);
                dstWeapon.projectile.pierce += pierceBonus;

                var dstDmg = dstWeapon.projectile.GetDamageModel();
                var srcDmg = srcWeapon.projectile.GetDamageModel();
                var baseDmg = baseWeapon?.projectile?.GetDamageModel();

                if (dstDmg != null && srcDmg != null)
                {
                    float baseDamage = baseDmg?.damage ?? 0f;
                    float dmgBonus = Math.Max(0f, srcDmg.damage - baseDamage);
                    dstDmg.damage += dmgBonus;

                    // Combine immunities (only keep immunities that BOTH paths are still blocked by)
                    dstDmg.immuneBloonProperties &= srcDmg.immuneBloonProperties;
                }

                // If source projectile can hit camo, remove camo filter from target projectile
                bool srcHitsCamo = !srcWeapon.projectile.GetBehaviors<FilterInvisibleModel>().Any();
                if (srcHitsCamo)
                    dstWeapon.projectile.RemoveBehaviors<FilterInvisibleModel>();
            }
        }
    }

    private static void RebuildNextUpgrades(
        TowerModel merged,
        GameModel gameModel,
        string baseId,
        int[] tiers)
    {
        int maxPrimary = Main.MaxPrimaryTier.GetValue();
        int maxSecondary = Main.MaxSecondaryTier.GetValue();

        var upgradePaths = new List<UpgradePathModel>();
        for (int path = 0; path < CrosspathRules.PathCount; path++)
        {
            if (!CrosspathRules.CanUpgradePath(tiers, path, maxPrimary, maxSecondary))
                continue;

            int[] nextTiers = (int[])tiers.Clone();
            nextTiers[path]++;

            // Look up the upgrade ID from the solo path tower
            int[] soloNext = new int[CrosspathRules.PathCount];
            soloNext[path] = nextTiers[path];
            var soloTower = gameModel.GetTower(baseId, soloNext[0], soloNext[1], soloNext[2]);
            string? upgradeName = soloTower?.appliedUpgrades.LastOrDefault();
            if (string.IsNullOrEmpty(upgradeName))
                continue;

            string nextTowerId = CrosspathRules.FormatTowerId(baseId, nextTiers);
            upgradePaths.Add(new UpgradePathModel(upgradeName, nextTowerId));
        }

        merged.upgrades = new Il2CppReferenceArray<UpgradePathModel>(upgradePaths.ToArray());
    }
}
