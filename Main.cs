using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.ModOptions;
using Crosspath533;
using MelonLoader;

[assembly: MelonInfo(typeof(Main), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace Crosspath533;

public class Main : BloonsTD6Mod
{
    public static readonly ModSettingBool Enabled = new(true)
    {
        displayName = "Enable 5-3-3 Crosspathing",
        description = "Toggle extended crosspathing on or off."
    };

    public static readonly ModSettingInt MaxPrimaryTier = new(5)
    {
        displayName = "Max Primary Path Tier",
        description = "Highest tier allowed on a tower's main upgrade path.",
        min = 1,
        max = 5
    };

    public static readonly ModSettingInt MaxSecondaryTier = new(3)
    {
        displayName = "Max Secondary Paths Tier",
        description = "Highest tier allowed on the other two upgrade paths (default: 3 for 5-3-3).",
        min = 0,
        max = 5
    };

    public static readonly ModSettingBool DebugLogging = new(false)
    {
        displayName = "Debug Logging",
        description = "Log generated crosspath TowerModels to the MelonLoader console."
    };

    public override void OnApplicationStart()
    {
        ModHelper.Msg<Main>(
            $"{ModHelperData.Name} v{ModHelperData.Version} loaded " +
            $"(limits: {MaxPrimaryTier.GetValue()}-{MaxSecondaryTier.GetValue()}-{MaxSecondaryTier.GetValue()})."
        );
    }
}
