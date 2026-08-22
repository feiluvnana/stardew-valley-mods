# 💎 BetterChest

**BetterChest** is a comprehensive loot overhaul for **Stardew Valley 1.6+**, enhancing both **Skull Cavern Treasure Rooms** and **Fishing Treasure Chests** (Standard and 1.6 Golden Chests) with dynamic gameplay loot engines, decaying multi-rolls, critical stack multipliers, and vanilla-faithful quality improvements.

---

## ✨ Key Modules

### 🏔️ Module 1: Skull Cavern Treasure Chests
- **7 Usage-Based Gameplay Categories (54 Validated Items):** Legendary, Agriculture, Mining, Fishing, Combat, Foraging, and Lootboxes & Troves.
- **Decaying Multi-Rolls:** Regular chests roll up to 6 items with decreasing roll probabilities (~2.5 items expected).
- **Critical Jackpot Stack Multipliers:** 2x (15%), 3x (10%), 4x (5%) jackpot procs on stackable items.
- **Supercharged Floor 100 Special Chests:** 12-roll ceiling with boosted stack crits up to 5x Mega Jackpot and equal category distribution.
- **Zero Cosmetic Junk:** Filters out hats, shirts, and decorative items.

### 🎣 Module 2: Vanilla-Faithful Fishing Treasure Chests
- **Authentic Vanilla Loot Pools:** Preserves all classic rewards (*Neptune's Glaive*, *Broken Trident*, *Dinosaur Egg*, *Ancient Seed*, *Lost Books*, *Gems*, *Tackle*, and *Power Books*).
- **Meaningful Resource Floors:** Boosts min stacks (8–25 Coal/Ores instead of 1–3, 15–40 Bait, 3–8 Geodes).
- **Trash & Dud Removal:** Automatically filters frustrating 1x stone, 1x wood, and broken trash items.
- **1.6 Golden Chest Enhancements:** Boosted Pearl rates (15–25%), bonus marine jellies (*Sea/River/Cave Jelly*), skill books, and high-tier fishing food (*Seafoam Pudding*, *Dish O' The Sea*).
- **Artifact Protection:** Bad-luck mitigation for museum completion (*Dinosaur Egg*, *Ancient Seed*).

---

## 📊 Complete Drop Rate & Probability Table (Regular Chests)

| Category | Item Name | Qualified ID | Base Stack | Item Weight | Category Share | Per-Roll Chance |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: |
| **Legendary (~14.285%)** | **Prismatic Shard** | `(O)74` | 1 – 2 | 25.0 | 19.23% | **2.747%** |
| | **Magic Rock Candy** | `(O)279` | 1 – 2 | 20.0 | 15.38% | **2.198%** |
| | **Golden Animal Cracker** | `(O)GoldenAnimalCracker` | 1 – 2 | 20.0 | 15.38% | **2.198%** |
| | **Auto-Petter** | `(BC)272` | 1 | 20.0 | 15.38% | **2.198%** |
| | **Galaxy Soul** | `(O)896` | 1 – 2 | 15.0 | 11.54% | **1.648%** |
| | **Stardrop Tea** | `(O)StardropTea` | 1 – 3 | 15.0 | 11.54% | **1.648%** |
| | **Prize Ticket** | `(O)PrizeTicket` | 2 – 5 | 15.0 | 11.54% | **1.648%** |
| **Agriculture (~14.285%)**| **Hyper Speed-Gro** | `(O)918` | 10 – 25 | 20.0 | 13.07% | **1.867%** |
| | **Deluxe Fertilizer** | `(O)919` | 10 – 25 | 20.0 | 13.07% | **1.867%** |
| | **Rare Seed** | `(O)347` | 2 – 6 | 20.0 | 13.07% | **1.867%** |
| | **Starfruit Seeds** | `(O)486` | 5 – 15 | 18.0 | 11.76% | **1.681%** |
| | **Deluxe Retaining Soil** | `(O)920` | 10 – 25 | 18.0 | 11.76% | **1.681%** |
| | **Iridium Sprinkler** | `(O)645` | 1 – 3 | 18.0 | 11.76% | **1.681%** |
| | **Tree Fertilizer** | `(O)805` | 10 – 20 | 15.0 | 9.80% | **1.401%** |
| | **Pressure Nozzle** | `(O)915` | 1 – 2 | 12.0 | 7.84% | **1.120%** |
| | **Enricher** | `(O)913` | 1 – 2 | 12.0 | 7.84% | **1.120%** |
| **Mining (~14.285%)** | **Iridium Ore** | `(O)386` | 10 – 25 | 25.0 | 14.53% | **2.076%** |
| | **Mega Bomb** | `(O)288` | 5 – 15 | 25.0 | 14.53% | **2.076%** |
| | **Radioactive Ore** | `(O)909` | 5 – 15 | 22.0 | 12.79% | **1.827%** |
| | **Iridium Bar** | `(O)337` | 3 – 10 | 22.0 | 12.79% | **1.827%** |
| | **Radioactive Bar** | `(O)910` | 2 – 6 | 20.0 | 11.63% | **1.661%** |
| | **Cinder Shard** | `(O)848` | 6 – 16 | 20.0 | 11.63% | **1.744%** |
| | **Jade** | `(O)70` | 3 – 8 | 20.0 | 11.63% | **1.661%** |
| | **Diamond** | `(O)72` | 3 – 8 | 18.0 | 10.47% | **1.495%** |
| **Fishing (~14.285%)** | **Challenge Bait** | `(O)ChallengeBait` | 15 – 35 | 22.0 | 12.50% | **1.786%** |
| | **Deluxe Bait** | `(O)DeluxeBait` | 20 – 40 | 22.0 | 12.50% | **1.786%** |
| | **Magic Bait** | `(O)908` | 10 – 25 | 20.0 | 11.36% | **1.623%** |
| | **Trap Bobber** | `(O)694` | 1 – 3 | 18.0 | 10.23% | **1.461%** |
| | **Curiosity Lure** | `(O)856` | 1 – 2 | 16.0 | 9.09% | **1.299%** |
| | **Sea Jelly** | `(O)SeaJelly` | 1 – 3 | 16.0 | 9.09% | **1.299%** |
| | **River Jelly** | `(O)RiverJelly` | 1 – 3 | 16.0 | 9.09% | **1.299%** |
| | **Cave Jelly** | `(O)CaveJelly` | 1 – 3 | 16.0 | 9.09% | **1.299%** |
| | **Seafoam Pudding** | `(O)265` | 2 – 5 | 15.0 | 8.52% | **1.218%** |
| | **Dish O' The Sea** | `(O)242` | 2 – 6 | 15.0 | 8.52% | **1.218%** |
| **Combat (~14.285%)** | **Life Elixir** | `(O)773` | 3 – 8 | 22.0 | 16.67% | **2.381%** |
| | **Triple Shot Espresso** | `(O)253` | 3 – 10 | 22.0 | 16.67% | **2.381%** |
| | **Dragon Tooth** | `(O)852` | 2 – 5 | 20.0 | 15.15% | **2.165%** |
| | **Fairy Dust** | `(O)872` | 2 – 5 | 20.0 | 15.15% | **2.165%** |
| | **Monster Musk** | `(O)879` | 2 – 5 | 18.0 | 13.64% | **1.948%** |
| | **Tiger Slime Egg** | `(O)857` | 1 – 2 | 15.0 | 11.36% | **1.623%** |
| | **Purple Slime Egg** | `(O)439` | 1 – 2 | 15.0 | 11.36% | **1.623%** |
| **Foraging (~14.285%)** | **Hardwood** | `(O)709` | 15 – 40 | 22.0 | 15.07% | **2.153%** |
| | **Mystic Tree Seed** | `(O)MysticTreeSeed` | 2 – 6 | 22.0 | 15.07% | **2.153%** |
| | **Golden Coconut** | `(O)791` | 2 – 6 | 22.0 | 15.07% | **2.153%** |
| | **Magma Cap** | `(O)851` | 3 – 8 | 20.0 | 13.70% | **1.957%** |
| | **Purple Mushroom** | `(O)422` | 5 – 12 | 20.0 | 13.70% | **1.957%** |
| | **Warp Totem: Desert** | `(O)261` | 3 – 8 | 20.0 | 13.70% | **1.957%** |
| | **Warp Totem: Farm** | `(O)688` | 3 – 8 | 20.0 | 13.70% | **1.957%** |
| **Lootboxes (~14.285%)**| **Omni Geode** | `(O)749` | 10 – 25 | 28.0 | 20.00% | **2.857%** |
| | **Mystery Box** | `(O)MysteryBox` | 3 – 10 | 25.0 | 17.86% | **2.551%** |
| | **Artifact Trove** | `(O)275` | 3 – 10 | 25.0 | 17.86% | **2.551%** |
| | **Golden Mystery Box** | `(O)GoldenMysteryBox` | 2 – 5 | 22.0 | 15.71% | **2.245%** |
| | **Calico Egg** | `(O)CalicoEgg` | 15 – 40 | 22.0 | 15.71% | **2.245%** |
| | **Treasure Totem** | `(O)TreasureTotem` | 1 – 3 | 18.0 | 12.86% | **1.837%** |

---

## ⚙️ Configuration (`config.json`)

```json
{
  "EnableCustomRewards": true,
  "ExcludeCosmetics": true,
  "MaxRolls": 6,
  "Roll2Chance": 0.8,
  "Roll3Chance": 0.58,
  "Roll4Chance": 0.4,
  "Roll5Chance": 0.25,
  "Roll6Chance": 0.1,
  "DoubleStackChance": 0.15,
  "TripleStackChance": 0.1,
  "QuadrupleStackChance": 0.05,
  "QuintupleStackChance": 0.0,
  "EnableFloor100Buff": true,
  "Floor100AllCategoriesEqual": true,
  "Floor100MaxRolls": 12,
  "Floor100Roll2Chance": 0.94,
  "Floor100Roll3Chance": 0.91,
  "Floor100Roll4Chance": 0.86,
  "Floor100Roll5Chance": 0.79,
  "Floor100Roll6Chance": 0.71,
  "Floor100Roll7Chance": 0.63,
  "Floor100Roll8Chance": 0.53,
  "Floor100Roll9Chance": 0.42,
  "Floor100Roll10Chance": 0.3,
  "Floor100Roll11Chance": 0.18,
  "Floor100Roll12Chance": 0.05,
  "Floor100DoubleStackChance": 0.2,
  "Floor100TripleStackChance": 0.25,
  "Floor100QuadrupleStackChance": 0.1,
  "Floor100QuintupleStackChance": 0.05,
  "LegendaryWeight": 15.0,
  "AgricultureWeight": 15.0,
  "MiningWeight": 15.0,
  "FishingWeight": 15.0,
  "CombatWeight": 15.0,
  "ForagingWeight": 15.0,
  "LootboxWeight": 15.0,
  "EnableLegendaryCategory": true,
  "EnableAgricultureCategory": true,
  "EnableMiningCategory": true,
  "EnableFishingCategory": true,
  "EnableCombatCategory": true,
  "EnableForagingCategory": true,
  "EnableLootboxCategory": true,
  "EnablePrismaticShard": true,
  "EnableMagicRockCandy": true,
  "EnableGoldenAnimalCracker": true,
  "EnableGalaxySoul": true,
  "EnablePrizeTicket": true,
  "EnableStardropTea": true,
  "EnableFertilizers": true,
  "EnableSprinklers": true,
  "EnableRareSeeds": true,
  "EnableRadioactiveItems": true,
  "GatekeepRadioactiveItems": true,
  "EnableIridiumItems": true,
  "EnableBombs": true,
  "EnableFishingTackle": true,
  "EnableSlimeEggs": true,
  "EnableCombatConsumables": true,
  "EnableWarpTotems": true,
  "EnableMysteryBoxes": true,
  "EnableArtifactTroves": true,
  "EnableOmniGeodes": true,
  "EnableCalicoEggs": true,
  "EnableFishingChestBuff": true,
  "FilterFishingChestJunk": true,
  "BoostFishingResourceStacks": true,
  "FishingResourceStackMultiplier": 1.5,
  "EnableGoldenChestBuff": true,
  "GoldenChestStackMultiplier": 2.0,
  "GoldenChestPearlBonus": true,
  "EnableFishingArtifactProtection": true
}
```

Configurable in-game via **Generic Mod Config Menu**.

---

## 🛠️ Building from Source

```powershell
dotnet build BetterChest.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
