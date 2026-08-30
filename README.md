# 🌟 feiluvnana's Stardew Valley Mod Collection (1.6+)

A suite of high-performance, modular quality-of-life, progression enhancement, and aesthetic mods for **Stardew Valley 1.6+**, built with SMAPI and Harmony.

---

## 📦 Mod Suite Overview

| Mod | Version | Description | Key Features |
| :--- | :---: | :--- | :--- |
| [**💎 BetterChest**](./BetterChest) | `1.8.3` | Dynamic loot overhaul for Skull Cavern treasure rooms. | 7 gameplay categories (57+ items including 1.6 Books & Machines), decaying multi-rolls, critical stack multipliers (up to 5x), depth tier scaling, and milestone gatekeeping (Volcano Caldera shortcut & 1.6 Masteries). |
| [**🎣 BetterFishing**](./BetterFishing) | `1.1.0` | Balanced fish price scaling, trait bonuses, aquaculture star quality, and fishing treasure chest multi-rolls. | Dynamic difficulty-based base price scaling, movement bonuses (2%–6%), environmental & isolated location traits (+2%), +100% legendary prize multiplier, dual anchors (Catfish 200g, Legend 5,000g), fish pond roe star quality, Caviar rebalance (800g base), and decaying treasure chest rolls (0.45/0.60). |
| [**🌋 BetterForge**](./BetterForge) | `1.1.2` | Volcano Forge, Mini-Forge, and Anvil overhauls. | 100% fair uniform weapon/tool enchantments ($1/N$), "Never Downgrade" trinket reforging, "Perfect" tier prefixes, and permanent Prismatic Ascension powers (+0.5 Luck, enhanced abilities). |
| [**🏭 BetterIndustry**](./BetterIndustry) | `2.4.0` | Artisan goods, artisanal milling, and cooking rebalance suite. | Food quality & star levels (Silver, Gold, Iridium), quality-preserving machine quality matrix, artisanal milling with quality retention & Artisan tag (-26), flower mead 2.0x value scaling, Truffle Oil scaling fix, vegetable juice buffs, expanded cask aging, and profitable cooking (+25%). |
| [**🦆 BetterAnimal**](./BetterAnimal) | `1.0.0` | Animal husbandry, duck rebalance, and small livestock productivity. | Eliminates the Duck Feather penalty with high-friendship dual drops (feather + egg), halves rabbit production cooldown to 2 days, multi-drop yields for happy rabbits, 850g Rabbit's Foot, and Loom down cloth processing. |
| [**📦 BetterQOL**](./BetterQOL) | `1.6.0` | Comprehensive Quality-of-Life suite: Exact XP skills panel, Lookup Anything (`F1`), UI hover overlays, dynamic transparency, item stacking, and geode cracking. | Exact XP numbers & level progress on SkillsPage, UI Info Suite 2 style hover tooltips (crops, machines, trees, animals), F1 Lookup Anything, dynamic transparency, unstackable item stack overrides, and instant geode cracking. |
| [**👑 BetterFurniture**](./BetterFurniture) | `1.0.1` | Interior decorating and luxury furniture expansion. | 4x4 spacious Princess King Bed with free placement anywhere, animated sconce flames, light sources, canopy layering, and farmhouse tile restorations. |
| [**🗺️ BetterMap**](./BetterMap) | `1.0.0` | Farmhouse doorway and Ginger Island farm clutter cleaner. | Widens all Farmhouse exit doorways to 3x1 tiles, removes Ginger Island Farm driftwood fence and bleached log piles. |
| [**🌵 BetterEvent**](./BetterEvent) | `1.2.0` | Expands the Calico Desert Festival across all seasons. | Enables Desert Festival in Summer, Fall, and Winter (15th–17th), preserves Calico Eggs across seasons. |

---

## 🚀 Installation

1. Install the latest version of [SMAPI](https://smapi.io/).
2. Download or build the mod folder(s).
3. Place the mod folders directly into your `Stardew Valley/Mods/` directory:
   ```text
   Stardew Valley/Mods/
   └── [feiluvnana Mods]/
       ├── BetterAnimal/
       ├── BetterChest/
       ├── BetterEvent/
       ├── BetterFishing/
       ├── BetterForge/
       ├── BetterFurniture/
       ├── BetterIndustry/
       ├── BetterMap/
       └── BetterQOL/
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
dotnet build BetterAnimal/BetterAnimal.csproj
dotnet build BetterChest/BetterChest.csproj
dotnet build BetterEvent/BetterEvent.csproj
dotnet build BetterFishing/BetterFishing.csproj
dotnet build BetterForge/BetterForge.csproj
dotnet build BetterFurniture/BetterFurniture.csproj
dotnet build BetterIndustry/BetterIndustry.csproj
dotnet build BetterMap/BetterMap.csproj
dotnet build BetterQOL/BetterQOL.csproj
```

---

## 📄 License & Credits
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
