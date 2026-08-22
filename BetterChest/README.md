# 💎 BetterChest

**BetterChest** is a comprehensive, progression-balanced loot overhaul for **Stardew Valley 1.6+**, transforming both **Skull Cavern Treasure Rooms** and **Fishing Treasure Chests** (Standard and 1.6 Golden Chests) with dynamic gameplay loot engines, decaying multi-rolls, critical stack multipliers, depth scaling, linear legendary scaling, and intelligent progression gatekeeping.

---

## 📖 Table of Contents
1. [Core Philosophy](#-core-philosophy)
2. [Module 1: Skull Cavern Loot Overhaul](#-module-1-skull-cavern-loot-overhaul)
   - [7 Gameplay Categories & Item Pool](#7-gameplay-categories--item-pool)
   - [Decaying Multi-Roll Engine](#decaying-multi-roll-engine)
   - [Critical Stack Multipliers](#critical-stack-multipliers)
   - [Linear Legendary Depth Scaling](#linear-legendary-depth-scaling)
   - [Depth-Based Tier Scaling](#depth-based-tier-scaling)
   - [Supercharged Floor 100+ Special Chests](#supercharged-floor-100-special-chests)
3. [Module 2: Fishing Treasure Chest Overhaul](#-module-2-fishing-treasure-chest-overhaul)
   - [Skill-Scaled Resource Minimum Floors](#skill-scaled-resource-minimum-floors)
   - [Fishing Progression & Mine Depth Gatekeeping](#fishing-progression--mine-depth-gatekeeping)
   - [1.6 Golden Fishing Chest Enhancements](#16-golden-fishing-chest-enhancements)
   - [Artifact Bad-Luck Protection](#artifact-bad-luck-protection)
   - [Trash & Dud Removal](#trash--dud-removal)
4. [🛡️ Master Gatekeeping & Unlock Matrix](#️-master-gatekeeping--unlock-matrix)
5. [📊 Complete Drop Rate & Probability Table](#-complete-drop-rate--probability-table)
6. [⚙️ Configuration (`config.json`)](#️-configuration-configjson)
7. [🛠️ Building & Installation](#️-building--installation)

---

## 🎯 Core Philosophy

Vanilla treasure chests often reward players with frustrating low-tier clutter (e.g. 1x Stone, 1x Wood, decorative clothing) or feel static. While buffing rewards makes dives rewarding, unrestrained loot can break early-game progression (e.g., obtaining Qi attachments, Ginger Island volcano materials, or 1.6 Masteries in Spring Year 1).

**BetterChest** solves this with a two-part approach:
1. **Meaningful Rewards:** Generous multi-rolls, critical jackpot procs, and high-utility items.
2. **Context-Aware Progression:** Gatekeeping late-game materials, scaling rolls by mine depth, and scaling fishing resource floors by your Fishing skill level.

---

## 🏔️ Module 1: Skull Cavern Loot Overhaul

### 7 Gameplay Categories & Item Pool
Every treasure chest in Skull Cavern selects items across 7 dedicated utility pools (54 validated vanilla items):

1. **Legendary:** Prismatic Shard, Magic Rock Candy, Golden Animal Cracker, Auto-Petter, Galaxy Soul, Stardrop Tea, Prize Ticket.
2. **Agriculture:** Hyper Speed-Gro, Deluxe Fertilizer, Deluxe Retaining Soil, Rare Seed, Starfruit Seeds, Iridium Sprinkler, Tree Fertilizer, Pressure Nozzle, Enricher.
3. **Mining:** Iridium Ore, Mega Bomb, Radioactive Ore, Iridium Bar, Radioactive Bar, Cinder Shard, Jade, Diamond.
4. **Fishing:** Challenge Bait, Deluxe Bait, Magic Bait, Trap Bobber, Curiosity Lure, Sea Jelly, River Jelly, Cave Jelly, Seafoam Pudding, Dish O' The Sea.
5. **Combat:** Life Elixir, Triple Shot Espresso, Dragon Tooth, Fairy Dust, Monster Musk, Tiger Slime Egg, Purple Slime Egg.
6. **Foraging:** Hardwood, Mystic Tree Seed, Golden Coconut, Magma Cap, Purple Mushroom, Warp Totem: Desert, Warp Totem: Farm.
7. **Lootboxes:** Omni Geode, Mystery Box, Golden Mystery Box, Artifact Trove, Calico Egg, Treasure Totem.

### Decaying Multi-Roll Engine
Instead of giving a flat single item, chests attempt multiple sequential rolls with decreasing probability:
- **Roll 1:** 100% (Guaranteed)
- **Roll 2:** 80% chance
- **Roll 3:** 58% chance
- **Roll 4:** 40% chance
- **Roll 5:** 25% chance
- **Roll 6:** 10% chance
- *Expected yield on normal floors:* **~2.5 items per chest**.

### Critical Stack Multipliers
When a stackable item (ores, bombs, seeds, fertilizers, geodes) rolls, it has a chance to proc a critical jackpot stack multiplier:
- **2x Double Stack:** 15% chance
- **3x Triple Stack:** 10% chance
- **4x Quadruple Stack:** 5% chance
- **5x Quintuple Stack (Floor 100 Special):** 5% chance

### Linear Legendary Depth Scaling
To prevent players from finding endgame legendary items on shallow floors, the Legendary category weight scales **linearly with floor depth**:

$$\text{DepthFactor} = \operatorname{clamp}\left(0.10 + 0.90 \times \frac{\text{Floor} - 1}{99},\, 0.10,\, 1.00\right)$$

$$\text{Active Legendary Weight} = \text{Base Legendary Weight} \times \text{DepthFactor}$$

| Skull Cavern Floor | Depth Factor | Legendary Weight | Legendary Per-Roll Chance |
| :---: | :---: | :---: | :---: |
| **Floor 1** | **0.10 (10%)** | **1.5** | **~1.6%** |
| **Floor 25** | **0.32 (32%)** | **4.8** | **~5.0%** |
| **Floor 50** | **0.55 (55%)** | **8.2** | **~8.3%** |
| **Floor 75** | **0.77 (77%)** | **11.6** | **~11.4%** |
| **Floor 100+** | **1.00 (100%)** | **15.0** | **~14.3%** |

### Depth-Based Tier Scaling
- **Shallow Floors (1–49):**
  - Maximum rolls capped at **3 items** (Roll 2: 60%, Roll 3: 30%).
  - Stack jackpot multiplier capped at **2x max**.
  - Legendary weight reduced according to linear depth scaling.
- **Deep Floors (50–99):**
  - Standard **6-roll decaying pool** and full **4x jackpot multipliers**.
- **Floor 100+ Special Chests:**
  - Supercharged ceiling up to **12 rolls** and **5x Mega Jackpots**.

### Supercharged Floor 100+ Special Chests
- Guaranteed chest generation on Floors 100, 200, 300, and 400 (even on repeated runs).
- **12 Max Rolls:** Decaying chances from 94% down to 5%.
- **Equal Category Distribution:** All 7 categories share equal ~14.28% weight.
- **5x Mega Jackpot:** 20% 2x, 25% 3x, 10% 4x, 5% 5x multiplier chances.

---

## 🎣 Module 2: Fishing Treasure Chest Overhaul

### Skill-Scaled Resource Minimum Floors
Resource stacks in fishing chests dynamically scale with the player's `FishingLevel`:

| Resource / Item | Fishing Lv 0–4 (Standard / Golden) | Fishing Lv 5–8 (Standard / Golden) | Fishing Lv 9–10+ (Standard / Golden) |
| :--- | :---: | :---: | :---: |
| **Coal** | 3 / 10 | 5 / 15 | 8 / 20 |
| **Copper Ore** | 4 / 12 | 7 / 18 | 10 / 25 |
| **Iron Ore** | 3 / 10 | 6 / 16 | 10 / 25 |
| **Gold Ore** | 2 / 8 | 5 / 14 | 8 / 20 |
| **Iridium Ore** | 1 / 3 | 2 / 5 | 3 / 8 |
| **Bait** | 5 / 18 | 10 / 25 | 15 / 35 |
| **Deluxe Bait** | 4 / 10 | 7 / 15 | 10 / 20 |
| **Wild Bait** | 2 / 6 | 4 / 8 | 5 / 12 |
| **Challenge Bait** | 4 / 8 | 6 / 14 | 10 / 20 |
| **Magic Bait** | 2 / 5 | 3 / 8 | 5 / 12 |
| **Magnet** | 2 / 5 | 3 / 7 | 5 / 10 |
| **Geode / Frozen / Magma** | 1 / 3 | 2 / 4 | 3 / 6 |
| **Omni Geode** | 1 / 3 | 2 / 5 | 3 / 8 |
| **Mystery Box** | 1 / 2 | 1 / 3 | 2 / 4 |
| **Golden Mystery Box** | 1 / 2 | 1 / 2 | 2 / 3 |

### Fishing Progression & Mine Depth Gatekeeping
To maintain natural game balance, high-tier loot found in fishing chests is validated against the player's world and mining progression:

| Dropped Item | Gatekeeping Requirement | Fallback / Downgrade when Locked |
| :--- | :--- | :--- |
| **Prismatic Shard** | Mine Floor $\ge 120$ OR Fishing Level $\ge 7$ | Downgrades to Diamond or Omni Geode |
| **Iridium Bar** | Mine Floor $\ge 120$ OR Fishing Level $\ge 8$ | Downgrades to Gold Bar or Iron Bar |
| **Iridium Ore** | Mine Floor $\ge 120$ OR Fishing Level $\ge 9$ | Downgrades to Gold Ore or Iron Ore |
| **Gold Ore** | Mine Floor $\ge 80$ OR Fishing Level $\ge 7$ | Downgrades to Iron Ore or Copper Ore |
| **Iron Ore** | Mine Floor $\ge 40$ OR Fishing Level $\ge 4$ | Downgrades to Copper Ore |
| **Magma Geode** | Mine Floor $\ge 80$ OR Fishing Level $\ge 7$ | Downgrades to Frozen or Basic Geode |
| **Frozen Geode** | Mine Floor $\ge 40$ OR Fishing Level $\ge 4$ | Downgrades to Basic Geode |
| **Mystery Box** | Mr. Qi Mystery Box cutscene triggered | Downgrades to Omni Geode |
| **Golden Mystery Box** | Combat Mastery claimed OR 30+ boxes opened | Downgrades to Mystery Box / Omni Geode |
| **Golden Animal Cracker** | Farming Mastery claimed | Replaces with Deluxe Bait (20x) |
| **Challenge Bait** | Fishing Mastery claimed | Downgrades to Deluxe Bait |
| **Magic Bait** | Qi's Walnut Room unlocked (100 Walnuts) | Downgrades to Deluxe Bait |
| **Ginger Island Items** | Visited Ginger Island | Downgrades to Omni Geode |
| **Qi Room Items** | Qi's Walnut Room unlocked | Downgrades to Diamond |

### 1.6 Golden Fishing Chest Enhancements
- **Pearl Bonus:** 20% bonus chance to roll a Pearl (`(O)797`).
- **1.6 Marine Jellies:** 25% chance to roll 1–2 Sea Jelly, River Jelly, or Cave Jelly.
- **Skill Food:** 20% chance to roll 1–2 Seafoam Pudding or Dish O' The Sea.
- **Stack Multiplier:** Default 2.0x multiplier on stackable items.

### Artifact Bad-Luck Protection
- **Dinosaur Egg:** 5% bonus check if uncollected, requiring `FishingLevel >= 5` and `DeepestMineLevel >= 40` (or `FishingLevel >= 7`).
- **Ancient Seed:** 5% bonus check if uncollected, requiring `FishingLevel >= 3` (preventing instant Day 1 seeds).

### Trash & Dud Removal
- Filters useless trash: Trash, Driftwood, Broken Glasses, Broken CD, Soggy Newspaper.
- Filters dud clutter: 1x–3x Stone or 1x–3x Wood drops.
- **Guaranteed Safety Fallback:** Ensures no chest ever opens empty.

---

## 🛡️ Master Gatekeeping & Unlock Matrix

| Feature / Setting | Description | Default |
| :--- | :--- | :---: |
| `ScaleLegendaryByDepth` | Linearly scales Legendary drop rate from Floor 1 (10%) to Floor 100 (100%) | `true` |
| `EnableDepthScaling` | Caps shallow floors (1–49) to 3 rolls and 2x stack limit | `true` |
| `GatekeepMasteryItems` | Gated until respective 1.6 Mastery is claimed | `true` |
| `GatekeepIslandItems` | Gated until Ginger Island is visited | `true` |
| `GatekeepQiItems` | Gated until Qi's Walnut Room is unlocked (100 Walnuts) | `true` |
| `GatekeepRadioactiveItems` | Gated until Qi's Room unlocked or Dangerous Mines active | `true` |
| `GatekeepMysteryBoxes` | Gated until Mr. Qi Mystery Box event occurs | `true` |
| `GatekeepCalicoEggs` | Gated to Desert Festival season (Spring 15–17) or Year 2+ | `true` |
| `GatekeepAutoPetter` | Gated until Community Center / Joja completion | `false` |
| `ScaleFishingResourcesByLevel`| Scales fishing chest resource stack minimums by Fishing Level | `true` |
| `GatekeepFishingHighTierLoot` | Downgrades over-leveled ores and items in fishing chests | `true` |

---

## 📊 Complete Drop Rate & Probability Table

### Regular Chests (Floor $\ge 100$ Baseline)

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
  "EnableDepthScaling": true,
  "ScaleLegendaryByDepth": true,
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
  "GatekeepMasteryItems": true,
  "GatekeepIslandItems": true,
  "GatekeepQiItems": true,
  "GatekeepMysteryBoxes": true,
  "GatekeepCalicoEggs": true,
  "GatekeepRadioactiveItems": true,
  "GatekeepAutoPetter": false,
  "EnableFishingChestBuff": true,
  "FilterFishingChestJunk": true,
  "BoostFishingResourceStacks": true,
  "ScaleFishingResourcesByLevel": true,
  "GatekeepFishingHighTierLoot": true,
  "FishingResourceStackMultiplier": 1.5,
  "EnableGoldenChestBuff": true,
  "GoldenChestStackMultiplier": 2.0,
  "GoldenChestPearlBonus": true,
  "EnableFishingArtifactProtection": true
}
```

Configurable in-game via **Generic Mod Config Menu (GMCM)**.

---

## 🛠️ Building & Installation

### Requirements
- **Stardew Valley 1.6+**
- **SMAPI 4.0+**
- *(Optional)* **Generic Mod Config Menu**

### Installation
1. Place the `BetterChest` folder into your `Stardew Valley/Mods/` directory.
2. Launch the game using SMAPI.

### Building from Source
```powershell
dotnet build BetterChest.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
