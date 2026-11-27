using System;
using System.Linq;

namespace Crosspath533;

/// <summary>
/// Pure crosspath rule evaluation (no BTD6 / Il2Cpp dependencies).
/// Default rule: at most 1 path up to Tier 5, and the other 2 paths up to Tier 3 (5-3-3).
/// </summary>
public static class CrosspathRules
{
    public const int PathCount = 3;

    /// <summary>
    /// Returns true if the given 3-path tier combination satisfies the configured limits.
    /// </summary>
    public static bool IsValidTiers(int[] tiers, int maxPrimary = 5, int maxSecondary = 3)
    {
        if (tiers.Length != PathCount)
            return false;

        int pathsAboveSecondary = 0;

        for (int i = 0; i < PathCount; i++)
        {
            int tier = tiers[i];
            if (tier < 0 || tier > maxPrimary)
                return false;

            if (tier > maxSecondary)
                pathsAboveSecondary++;
        }

        // Only one path may exceed maxSecondary (e.g. only one path can be Tier 4 or 5).
        return pathsAboveSecondary <= 1;
    }

    /// <summary>
    /// Returns true if upgrading <paramref name="pathIndex"/> by 1 tier from
    /// <paramref name="currentTiers"/> is allowed under the configured limits.
    /// </summary>
    public static bool CanUpgradePath(
        int[] currentTiers,
        int pathIndex,
        int maxPrimary = 5,
        int maxSecondary = 3)
    {
        if (currentTiers.Length != PathCount || pathIndex < 0 || pathIndex >= PathCount)
            return false;

        var next = (int[])currentTiers.Clone();
        next[pathIndex]++;
        return IsValidTiers(next, maxPrimary, maxSecondary);
    }

    /// <summary>
    /// Returns the maximum tier allowed for <paramref name="pathIndex"/> given
    /// <paramref name="currentTiers"/> (matches UpgradeObject.CheckBlockedPath semantics).
    /// </summary>
    public static int GetMaxAllowedTierForPath(
        int[] currentTiers,
        int pathIndex,
        int maxPrimary = 5,
        int maxSecondary = 3)
    {
        if (currentTiers.Length != PathCount || pathIndex < 0 || pathIndex >= PathCount)
            return 0;

        for (int i = 0; i < PathCount; i++)
        {
            if (i != pathIndex && currentTiers[i] > maxSecondary)
                return maxSecondary;
        }

        return maxPrimary;
    }

    /// <summary>
    /// Returns true if this tier combination is already part of vanilla BTD6 (max 5-2-0).
    /// </summary>
    public static bool IsVanillaTiers(int[] tiers)
    {
        if (tiers.Length != PathCount)
            return false;

        var sorted = tiers.OrderByDescending(t => t).ToArray();
        return sorted[0] <= 5 && sorted[1] <= 2 && sorted[2] == 0;
    }

    /// <summary>
    /// Returns the index (0, 1, or 2) of the highest-tier path.
    /// </summary>
    public static int GetDominantPath(int[] tiers)
    {
        int best = 0;
        for (int i = 1; i < tiers.Length; i++)
        {
            if (tiers[i] > tiers[best])
                best = i;
        }
        return best;
    }

    /// <summary>
    /// Parses a BTD6 tower ID like "DartMonkey-533" into ("DartMonkey", [5, 3, 3]).
    /// </summary>
    public static bool TryParseTowerId(string? towerId, out string baseId, out int[] tiers)
    {
        baseId = string.Empty;
        tiers = Array.Empty<int>();

        if (string.IsNullOrWhiteSpace(towerId))
            return false;

        int dash = towerId.LastIndexOf('-');
        if (dash <= 0 || dash + 4 != towerId.Length)
            return false;

        char c0 = towerId[dash + 1];
        char c1 = towerId[dash + 2];
        char c2 = towerId[dash + 3];

        if (!char.IsDigit(c0) || !char.IsDigit(c1) || !char.IsDigit(c2))
            return false;

        baseId = towerId[..dash];
        tiers = new[] { c0 - '0', c1 - '0', c2 - '0' };
        return true;
    }

    /// <summary>
    /// Formats a tower ID from base ID and tiers, e.g. ("DartMonkey", [5, 3, 3]) -> "DartMonkey-533".
    /// </summary>
    public static string FormatTowerId(string baseId, int[] tiers) =>
        $"{baseId}-{tiers[0]}{tiers[1]}{tiers[2]}";
}
