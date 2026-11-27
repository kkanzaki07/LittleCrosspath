# Crosspath 5-3-3 (BTD6 Mod)

A lightweight **Bloons TD 6** crosspathing mod (inspired by *Ultimate Crosspathing*) that lets towers upgrade up to **Tier 5** on one path and up to **Tier 3** on both remaining paths (**5-3-3**, **3-5-3**, or **3-3-5**).

## Features

- **5-3-3 Crosspathing**: Buy Tier 5 on your primary path plus up to Tier 3 on both secondary paths.
- **Lazy TowerModel Merging**: Non-vanilla tower models (like `DartMonkey-533`) are generated on demand and cached, avoiding long startup times.
- **Configurable in Mod Settings**: Toggle the mod on/off or adjust `MaxPrimaryTier` and `MaxSecondaryTier` right inside BTD6's Mod Settings menu.

## Project Structure

| File | Purpose |
| --- | --- |
| `Main.cs` | MelonLoader / BTD6 Mod Helper entry point and in-game Mod Settings. |
| `CrosspathRules.cs` | Pure C# crosspath validation and tower ID parsing (no BTD6 dependencies). |
| `TowerMerger.cs` | Merges stats, range, camo/lead, weapons, projectiles, and abilities for 5-3-3 towers. |
| `Patches/CrosspathPatches.cs` | Harmony patches for UI path unblocking (`UpgradeObject.CheckBlockedPath`) and lazy model lookup (`GameModel.GetTowerWithName`). |
| `ModHelperData.cs` | Mod Helper metadata (`WorksOnVersion`, version, repo info). |
| `Crosspath533.csproj` | `.NET 6.0` project configured for `btd6.targets`. |

## Building at Home (with BTD6 Installed)

1. Install **MelonLoader** and **BTD6 Mod Helper** into your Bloons TD 6 directory.
2. Install the **.NET 6 SDK** (or newer).
3. Copy `btd6.targets` from the [BTD6 Mod Helper template](https://github.com/gurrenm3/BTD-Mod-Helper) into this folder (or parent folder) and set your `BloonsTD6` install path.
4. Build the mod:
   ```powershell
   dotnet build
   ```
   `btd6.targets` will automatically compile `Crosspath533.dll` and copy it into `BloonsTD6/Mods/`.
