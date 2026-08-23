# 🌟 feiluvnana's Stardew Valley Mod Collection (1.6+)

A suite of high-performance, modular quality-of-life, progression enhancement, and aesthetic mods for **Stardew Valley 1.6+**, built with SMAPI and Harmony.

---

## 📦 Mod Suite Overview

| Mod | Version | Description | Key Features |
| :--- | :---: | :--- | :--- |
| [**💎 BetterChest**](./BetterChest) | `1.4.0` | Dynamic loot overhaul for Skull Cavern & Fishing treasure chests. | 7 gameplay categories (54 items), decaying multi-rolls, critical stack multipliers (up to 5x), linear legendary depth scaling, milestone gatekeeping, and skill-scaled fishing floors with 1.6 Golden Chest rewards. |
| [**🌋 BetterForge**](./BetterForge) | `1.0.0` | Volcano Forge, Mini-Forge, and Anvil overhauls. | 100% fair uniform weapon/tool enchantments ($1/N$), "Never Downgrade" trinket reforging, "Perfect" tier prefixes, and permanent Prismatic Ascension powers (+0.5 Luck, enhanced abilities). |
| [**🏭 BetterIndustry**](./BetterIndustry) | `2.0.2` | Artisan goods and cooking rebalance suite. | Quality-preserving machines, flower mead 2.0x value scaling, Truffle Oil scaling fix, vegetable juice buffs, expanded cask aging, and profitable cooking (+25%). |

| [**📦 BetterQOL**](./BetterQOL) | `1.4.2` | Quality-of-life suite for UI hover overlays, lookup, item stacking, and geodes. | UI Info Suite 2 style hover tooltips (crop timers, machine countdowns, tree stages, animal hearts), Lookup Anything (`F1`), unstackable item overrides up to 999, and instant "Crack All" geode processing. |
| [**👑 BetterFurniture**](./BetterFurniture) | `1.0.0` | Interior decorating and luxury furniture expansion. | 4x4 spacious Princess King Bed with free placement anywhere, animated sconce flames, light sources, canopy layering, and farmhouse tile restorations. |
| [**🗺️ BetterMap**](./BetterMap) | `1.0.0` | Farmhouse doorway and Ginger Island farm clutter cleaner. | Widens all Farmhouse exit doorways to 3x1 tiles, removes Ginger Island Farm driftwood fence and bleached log piles. |
| [**🌵 ExtendedDesertFestival**](./ExtendedDesertFestival) | `1.1.1` | Expands the Calico Desert Festival across all seasons. | Enables Desert Festival in Summer, Fall, and Winter (15th–17th), preserves Calico Eggs across seasons. |

---

## 🚀 Installation

1. Install the latest version of [SMAPI](https://smapi.io/).
2. Download or build the mod folder(s).
3. Place the mod folders directly into your `Stardew Valley/Mods/` directory:
   ```text
   Stardew Valley/Mods/
   └── [feiluvnana Mods]/
       ├── BetterChest/
       ├── BetterForge/
       ├── BetterFurniture/
       ├── BetterIndustry/
       ├── BetterMap/
       ├── BetterQOL/
       └── ExtendedDesertFestival/
   ```
4. Run the game using **StardewModdingAPI.exe**.

---

## ⚙️ Generic Mod Config Menu (GMCM) Support

All mods in this collection feature native integration with [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098). Configure drop rates, multipliers, toggles, stack limits, and festival seasons directly in-game from the title screen or in-game pause menu.

---

## 🛠️ Development & Building

All mods are built targeting **.NET 6.0** and the **Stardew Valley 1.6+** SMAPI environment.

### Build All Mods
Run the following commands from the workspace root:
```powershell
dotnet build BetterChest/BetterChest.csproj
dotnet build BetterForge/BetterForge.csproj
dotnet build BetterFurniture/BetterFurniture.csproj
dotnet build BetterIndustry/BetterIndustry.csproj
dotnet build BetterMap/BetterMap.csproj
dotnet build BetterQOL/BetterQOL.csproj
dotnet build ExtendedDesertFestival/ExtendedDesertFestival.csproj
```

---

## 📄 License & Credits
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
