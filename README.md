# 🌟 feiluvnana's Stardew Valley Mod Collection (1.6+)

A suite of high-performance, modular quality-of-life, progression enhancement, and aesthetic mods for **Stardew Valley 1.6+**, built with SMAPI and Harmony.

---

## 📦 Mod Suite Overview

| Mod | Version | Description | Key Features |
| :--- | :---: | :--- | :--- |
| [**BetterSkullCavernChest**](./BetterSkullCavernChest) | `1.1.0` | Dynamic loot overhaul for Skull Cavern treasure rooms. | 7 gameplay categories (54 items), decaying multi-roll chances, critical stack multipliers (up to 5x), and Floor 100 special chest buffs. |
| [**BetterProduct**](./BetterProduct) | `1.0.0` | Comprehensive artisan and cooking value / buff rebalancer. | Flower-type honey mead preservation, profitable cooking margins (+25%), +50% food buff durations, +25% energy, and juice/pickle/roe/caviar boosts. |
| [**BetterFurniture**](./BetterFurniture) | `1.0.0` | Interior decorating and luxury furniture expansion. | 4x4 spacious Princess King Bed with free placement anywhere, animated sconce flames, light sources, canopy layering, and farmhouse tile restorations. |
| [**ExtendedDesertFestival**](./ExtendedDesertFestival) | `1.1.0` | Expands the Calico Desert Festival across all seasons. | Enables Desert Festival in Summer, Fall, and Winter (15th–17th), preserves Calico Eggs across seasons. |
| [**ExtendedStackable**](./ExtendedStackable) | `1.0.0` | Stack size overhaul for normally unstackable items. | Stacks fishing tackle, 1.6 trinkets, rings, furniture, boots, clothing, and hats up to 999 with stat matching. |
| [**BetterGeodeCracking**](./BetterGeodeCracking) | `1.0.0` | Bulk and instant geode/trove cracking without fees. | Free 0g cracking at Clint's, Shift+Click / "Crack All" button bulk processing, instant cracking toggle, and faithful RNG. |
| [**BetterForge**](./BetterForge) | `1.0.0` | Volcano Forge, Mini-Forge, and Anvil overhauls. | 100% fair uniform weapon/tool enchantments, "Never Downgrade" trinket reforging, "Perfect" tier prefixes, and Prismatic Ascension powers. |
| [**BetterTool**](./BetterTool) | `1.0.0` | 4-Directional hopper and machine automation overhaul. | 4-way omnidirectional auto-feed and auto-harvest (N, S, W, E), smart routing to adjacent chests/bins, crab pot & cask support. |
| [**BetterMap**](./BetterMap) | `1.0.0` | Farmhouse doorway and Ginger Island farm clutter cleaner. | Widens all Farmhouse exit doorways to 3x1 tiles, removes Ginger Island Farm driftwood fence and bleached log piles. |

---

## 🚀 Installation

1. Install the latest version of [SMAPI](https://smapi.io/).
2. Download or build the mod folder(s).
3. Place the mod folders directly into your `Stardew Valley/Mods/` directory:
   ```text
   Stardew Valley/Mods/
   └── [feiluvnana Mods]/
       ├── BetterForge/
       ├── BetterFurniture/
       ├── BetterGeodeCracking/
       ├── BetterMap/
       ├── BetterProduct/
       ├── BetterSkullCavernChest/
       ├── BetterTool/
       ├── ExtendedDesertFestival/
       └── ExtendedStackable/
   ```
4. Run the game using **StardewModdingAPI.exe**.

---

## ⚙️ Generic Mod Config Menu (GMCM) Support

All mods in this collection feature native integration with [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098). Configure drop rates, multipliers, toggles, stack limits, and festival seasons directly in-game from the title screen or in-game pause menu.

---

## 🛠️ Development & Building

All mods are built targeting **.NET 6.0 / .NET 10.0** and the **Stardew Valley 1.6+** SMAPI environment.

### Build All Mods
Run the following commands from the root directory:
```powershell
dotnet build BetterForge/BetterForge.csproj
dotnet build BetterFurniture/BetterFurniture.csproj
dotnet build BetterGeodeCracking/BetterGeodeCracking.csproj
dotnet build BetterMap/BetterMap.csproj
dotnet build BetterProduct/BetterProduct.csproj
dotnet build BetterSkullCavernChest/BetterSkullCavernChest.csproj
dotnet build BetterTool/BetterTool.csproj
dotnet build ExtendedDesertFestival/ExtendedDesertFestival.csproj
dotnet build ExtendedStackable/ExtendedStackable.csproj
```

---

## 📄 License & Credits
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
