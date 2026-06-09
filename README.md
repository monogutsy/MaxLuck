# MaxLuck

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square)
![TShock](https://img.shields.io/badge/TShock-6.1.0-blue?style=flat-square)
![Terraria](https://img.shields.io/badge/Terraria-1.4.5.6-green?style=flat-square)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)

A TShock plugin that turns Terraria's RNG-based loot system into a guaranteed drops system. When an NPC dies, it drops **everything** from its loot table no chance rolls, no luck required.

---

## What It Does

Instead of boosting drop rates or setting a luck value, MaxLuck reads each NPC's full loot table from Terraria's `ItemDropDatabase` and forces every item to drop on kill.

- A Zombie dies → you get the Zombie Arm, Shackle, and everything else it can possibly drop
- A Nymph dies → Metal Detector, guaranteed
- A boss dies → all possible loot, including expert and master mode items
- Event enemies → full loot tables, every time

---

## Features

- **Guaranteed drops** from every NPC's complete loot table
- **Boss loot** — all items from all difficulty modes (configurable)
- **Event enemies** — Pirate Invasion, Solar Eclipse, Frost Moon, Pumpkin Moon, Martian Madness, Blood Moon
- **Rare creatures** — Nymph, Tim, Doctor Bones, Rune Wizard, and others
- **Spawn rate control** — faster spawns, more active enemies
- **Fishing enhancements** — bonus fishing power, automatic buffs
- **Event triggering** — increased chance for Blood Moons, Eclipses, Invasions
- **Duplicate prevention** — won't spam the same item multiple times per kill
- **Item blacklist** — exclude specific items from guaranteed drops
- **Full configuration** — everything is toggleable and tunable

---

## Installation

1. Download the latest `MaxLuck.zip` from [Releases](../../releases)
2. Extract the contents of the ZIP file
3. Drop `MaxLuck.dll` into your TShock server's `ServerPlugins/` folder
4. Restart the server
5. The configuration file will be generated at `tshock/MaxLuckConfig.json`

---

## Commands

| Command | Permission | Description |
|---------|-----------|-------------|
| `/maxluck` | `maxluck.admin` | Show current status |
| `/maxluck on` | `maxluck.admin` | Enable the plugin |
| `/maxluck off` | `maxluck.admin` | Disable the plugin |
| `/maxluck reload` | `maxluck.admin` | Reload configuration from disk |
| `/maxluck status` | `maxluck.admin` | Detailed status of all subsystems |

---

## Configuration

Generated at `tshock/MaxLuckConfig.json` on first run.

```json
{
  "Enabled": true,
  "LuckValue": 1.0,
  "UpdateInterval": 60,
  "BroadcastOnToggle": true,
  "GuaranteedDrops": {
    "Enabled": true,
    "ApplyToBosses": true,
    "GuaranteedEventLoot": true,
    "GuaranteedRareCreatureLoot": true,
    "GuaranteedFishingLoot": true,
    "LootRollCount": 10,
    "LogDrops": false,
    "PreventDuplicates": true,
    "ExcludedItemIds": []
  },
  "EnemySpawns": {
    "Enabled": true,
    "SpawnRateMultiplier": 5.0,
    "MaxSpawnsMultiplier": 3.0,
    "RareEnemyMultiplier": 5.0,
    "EnforceInterval": 10,
    "BaseSpawnRateOverride": 0,
    "MaxSpawnsOverride": 0
  },
  "Fishing": {
    "Enabled": true,
    "FishingPowerBonus": 500,
    "ApplyFishingBuffs": true,
    "BuffDuration": 600
  },
  "Events": {
    "Enabled": true,
    "IncreaseRareEvents": true,
    "EclipseChance": 0.15,
    "BloodMoonChance": 0.15,
    "PirateInvasionChance": 0.05,
    "GoblinArmyChance": 0.05,
    "SlimeRainChance": 0.08,
    "EventCheckCooldown": 60,
    "AnnounceEvents": true
  }
}
```

### Key Config Options

| Option | What it does |
|--------|-------------|
| `ApplyToBosses` | Include boss enemies in the guaranteed drops system |
| `GuaranteedEventLoot` | Force all event enemy drops |
| `GuaranteedRareCreatureLoot` | Force drops from rare NPCs (Nymph, Tim, etc.) |
| `PreventDuplicates` | Skip items that already dropped in the same kill |
| `ExcludedItemIds` | Array of item IDs to never force-drop |
| `LogDrops` | Print every guaranteed drop to server console (noisy, for debugging) |
| `SpawnRateMultiplier` | Divides vanilla spawn timer (higher = faster spawns) |
| `MaxSpawnsMultiplier` | Multiplies max active enemies |

---

## Building From Source

Requirements:
- .NET 9 SDK
- TShock 6.1.0 NuGet package (pulled automatically)

```
git clone https://github.com/monogutsy/MaxLuck.git
cd MaxLuck
dotnet build MaxLuck/MaxLuck.csproj -c Release
```

Output: `MaxLuck/bin/Release/net9.0/MaxLuck.dll`

---

## How the Drop Resolution Works

The plugin hooks into `NpcKilled` and queries `Main.ItemDropsDB.GetRulesForNPCID()` for the killed NPC's type. It then walks the full rule tree:

- `CommonDrop` → extracts item directly
- `DropBasedOnExpertMode` / `DropBasedOnMasterMode` → resolves both branches
- `OneFromRulesRule` / `OneFromOptionsDropRule` → takes all options instead of one
- `LeadingConditionRule` → ignores condition, processes all chained rules
- `ChainedRules` (OnSuccess/OnFailure) → resolves both paths
- Unknown rule types → reflection fallback to extract `itemId` and `dropIds` fields

This means it works dynamically with any NPC that has entries in Terraria's drop database, without hardcoding individual enemy drops.

---

## Compatibility

| | Version |
|--|---------|
| Terraria | 1.4.5.6 |
| TShock | 6.1.0 |
| .NET | 9.0 |
| API | TSAPI 2.1 |

---

## License

[MIT](LICENSE)