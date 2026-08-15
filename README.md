# 🌟 feiluvnana's Stardew Valley Mod Collection (1.6+)

A suite of high-performance, modular quality-of-life and progression enhancement mods for **Stardew Valley 1.6+**, built with SMAPI and Harmony.

---

## 📦 Mod Suite Overview

| Mod | Version | Description | Key Features |
| :--- | :---: | :--- | :--- |
| [**BetterSkullCavernChest**](./BetterSkullCavernChest) | `1.1.0` | Complete overhaul of Skull Cavern treasure room loot mechanics. | 7 usage categories, Floor 100 special chest buffs (equal category shares, 7 decaying rolls, 2x-5x stack multipliers), GMCM menu. |
| [**BetterProduct**](./BetterProduct) | `1.0.0` | Comprehensive artisan and cooking value / buff rebalancer. | Cooking profit margins, honey-type flower mead preservation, juice/pickle/roe/caviar buffs, energy & buff duration scaling. |
| [**ExtendedDesertFestival**](./ExtendedDesertFestival) | `1.1.0` | Expands the Calico Desert Festival across all seasons. | Enables Desert Festival in Summer, Fall, and Winter (15th–17th), preserves Calico Eggs between festivals. |
| [**ExtendedStackable**](./ExtendedStackable) | `1.0.0` | Stack size overhaul for normally unstackable items. | Stacks fishing tackle, 1.6 trinkets, rings, furniture, boots, clothing, and hats up to 999. |
| [**BetterGeodeCracking**](./BetterGeodeCracking) | `1.0.0` | Bulk and instant geode/trove cracking without fees. | Free 0g geode cracking at Clint's, Shift+Click / button bulk stack cracking, instant opening, GMCM menu. |
| [**BetterTrinket**](./BetterTrinket) | `1.0.0` | Smart trinket reforging overhaul, bad-luck protection, and stat tooltips. | "Never downgrade" reforge guarantee, bad-luck pity counter, stat range tooltips, configurable Iridium Bar cost, GMCM menu. |

---

## 🚀 Installation

1. Install the latest version of [SMAPI](https://smapi.io/).
2. Download or build the mod folder(s).
3. Place the mod folders directly into your `Stardew Valley/Mods/` directory:
   ```text
   Stardew Valley/Mods/
   └── [feiluvnana Mods]/
       ├── BetterSkullCavernChest/
       ├── BetterProduct/
       ├── ExtendedDesertFestival/
       ├── ExtendedStackable/
       ├── BetterGeodeCracking/
       └── BetterTrinket/
   ```
4. Run the game using **StardewModdingAPI.exe**.

---

## ⚙️ Generic Mod Config Menu (GMCM) Support

All mods in this collection feature native integration with [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098). Configure drop rates, multipliers, toggles, stack limits, and festival seasons directly in-game from the title screen or in-game pause menu.

---

## 🛠️ Development & Building

All mods are built targeting **.NET 6.0 / .NET 10.0** and the **Stardew Valley 1.6+** SMAPI environment.

### Build All Mods
Run the following command from the root folder:
```powershell
dotnet build BetterSkullCavernChest/BetterSkullCavernChest.csproj
dotnet build BetterProduct/BetterProduct.csproj
dotnet build ExtendedDesertFestival/ExtendedDesertFestival.csproj
dotnet build ExtendedStackable/ExtendedStackable.csproj
dotnet build BetterGeodeCracking/BetterGeodeCracking.csproj
dotnet build BetterTrinket/BetterTrinket.csproj
```

---

## 📄 License & Credits
Created by **feiluvnana** for Stardew Valley 1.6. Built with SMAPI and Harmony.
