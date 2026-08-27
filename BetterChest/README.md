# 💎 BetterChest (v1.7.0)

**BetterChest** is a comprehensive, progression-balanced loot overhaul for **Stardew Valley 1.6+**, transforming **Skull Cavern Treasure Rooms** with dynamic gameplay loot engines, decaying multi-rolls, critical stack multipliers, depth scaling, linear legendary scaling, and intelligent progression gatekeeping. *(Note: Fishing treasure chest enhancements have been migrated to BetterFishing).*

---

## 📖 Table of Contents
1. [Core Philosophy](#-core-philosophy)
2. [Module 1: Skull Cavern Loot Overhaul](#-module-1-skull-cavern-loot-overhaul)
   - [7 Gameplay Categories & Item Pool](#7-gameplay-categories--item-pool)
   - [Decaying Multi-Roll Engine & Minimum Guarantees](#decaying-multi-roll-engine--minimum-guarantees)
   - [Critical Stack Multipliers](#critical-stack-multipliers)
   - [Linear Legendary Depth Scaling](#linear-legendary-depth-scaling)
   - [Depth-Based Tier Scaling](#depth-based-tier-scaling)
   - [Supercharged Floor 100+ Special Chests](#supercharged-floor-100-special-chests)
3. [🛡️ Master Gatekeeping & Unlock Matrix](#️-master-gatekeeping--unlock-matrix)
4. [📊 Complete Drop Rate & Probability Table](#-complete-drop-rate--probability-table)
5. [⚙️ Configuration (`config.json`)](#️-configuration-configjson)
6. [🛠️ Building & Installation](#️-building--installation)

---

## 🎯 Core Philosophy

Vanilla treasure chests often reward players with frustrating low-tier clutter (e.g. 1x Stone, 1x Wood, decorative clothing) or feel static. While buffing rewards makes dives rewarding, unrestrained loot can break early-game progression (e.g., obtaining Qi attachments, Ginger Island volcano materials, or 1.6 Masteries in Spring Year 1).

**BetterChest** solves this with a two-part approach:
1. **Meaningful Rewards:** Generous multi-rolls, critical jackpot procs, high-utility resources (Hardwood & Coal), and no dud warp totems.
2. **Context-Aware Progression & Clean Fishing:** Gatekeeping late-game materials, scaling rolls by mine depth, and guaranteeing bonus rolls for any trash found in fishing chests.

---

## 🏔️ Module 1: Skull Cavern Loot Overhaul

### 7 Gameplay Categories & Item Pool
Every treasure chest in Skull Cavern selects items across 7 dedicated utility pools:

1. **Legendary:** Prismatic Shard, Magic Rock Candy, Golden Animal Cracker, Auto-Petter, Galaxy Soul, Stardrop Tea, Prize Ticket.
2. **Agriculture:** Hyper Speed-Gro, Deluxe Fertilizer, Deluxe Retaining Soil, Rare Seed, Starfruit Seeds, Iridium Sprinkler, Tree Fertilizer, Pressure Nozzle, Enricher.
3. **Mining:** Iridium Ore, Mega Bomb, Radioactive Ore, Iridium Bar, Radioactive Bar, Coal (35–90), Cinder Shard, Jade, Diamond.
4. **Fishing:** Challenge Bait, Deluxe Bait, Magic Bait, Trap Bobber, Curiosity Lure, Sea Jelly, River Jelly, Cave Jelly, Seafoam Pudding, Dish O' The Sea.
5. **Combat:** Life Elixir, Triple Shot Espresso, Dragon Tooth, Fairy Dust, Monster Musk, Tiger Slime Egg, Purple Slime Egg.
6. **Foraging:** Hardwood (30–80), Mystic Tree Seed, Golden Coconut, Magma Cap, Purple Mushroom.
7. **Lootboxes:** Omni Geode, Mystery Box, Golden Mystery Box, Artifact Trove, Calico Egg, Treasure Totem (2–5).

### Decaying Multi-Roll Engine & Minimum Guarantees
Instead of giving a flat single item, chests attempt multiple sequential rolls with guaranteed minimums:
- **Standard Chests (Floors 50–99 / Regular):** **Min 2 guaranteed rolls**, up to **8 max rolls**.
  - Roll 1: 100% (Guaranteed)
  - Roll 2: 100% (Guaranteed)
  - Roll 3: 80% chance
  - Roll 4: 65% chance
  - Roll 5: 50% chance
  - Roll 6: 35% chance
  - Roll 7: 20% chance
  - Roll 8: 10% chance
- **Shallow / Gatekeep Floors (1–49):** **Min 1 guaranteed roll**, up to **4 max rolls** (Roll 2: 60%, Roll 3: 35%, Roll 4: 20%).
- **Super Chests (Floor 100+ Special):** **Min 3 guaranteed rolls**, up to **12 max rolls**.

### Critical Stack Multipliers
When a stackable item (ores, coal, bombs, seeds, fertilizers, geodes) rolls, it has a chance to proc a critical jackpot stack multiplier:
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
  - Min 1 roll, maximum rolls capped at **4 items**.
  - Stack jackpot multiplier capped at **2x max**.
  - Legendary weight reduced according to linear depth scaling.
- **Deep Floors (50–99):**
  - Standard **min 2 guaranteed rolls** and up to **8 rolls** with full **4x jackpot multipliers**.
- **Floor 100+ Special Chests:**
  - Supercharged ceiling up to **12 rolls** (min 3 guaranteed) and **5x Mega Jackpots**.

---

## 🛡️ Master Gatekeeping & Unlock Matrix

| Feature / Setting | Description | Default |
| :--- | :--- | :---: |
| `ScaleLegendaryByDepth` | Linearly scales Legendary drop rate from Floor 1 (10%) to Floor 100 (100%) | `true` |
| `EnableDepthScaling` | Caps shallow floors (1–49) to 4 rolls and 2x stack limit | `true` |
| `GatekeepMasteryItems` | Gated until respective 1.6 Mastery is claimed | `true` |
| `GatekeepIslandItems` | Gated until Ginger Island is visited | `true` |
| `GatekeepQiItems` | Gated until Qi's Walnut Room is unlocked (100 Walnuts) | `true` |
| `GatekeepRadioactiveItems` | Gated until Qi's Room unlocked or Dangerous Mines active | `true` |
| `GatekeepMysteryBoxes` | Gated until Mr. Qi Mystery Box event occurs | `true` |
| `GatekeepCalicoEggs` | Gated to Desert Festival season (Spring 15–17) or Year 2+ | `true` |
| `GatekeepAutoPetter` | Gated until Community Center / Joja completion | `false` |

---

## 📊 Complete Drop Rate & Probability Table

### Regular Chests (Floor $\ge 100$ Baseline)

| Category | Item Name | Qualified ID | Base Stack | Item Weight | Multipliers | Per-Roll Chance |
| :--- | :--- | :--- | :---: | :---: | :---: | :---: |
| **Legendary (~14.285%)** | **Prismatic Shard** | `(O)74` | 1 – 2 | 25.0 | ❌ No | **2.747%** |
| | **Magic Rock Candy** | `(O)279` | 1 – 2 | 20.0 | ❌ No | **2.198%** |
| | **Golden Animal Cracker** | `(O)GoldenAnimalCracker` | 1 – 2 | 20.0 | ❌ No | **2.198%** |
| | **Auto-Petter** | `(BC)272` | 1 | 20.0 | ❌ No | **2.198%** |
| | **Galaxy Soul** | `(O)896` | 1 – 2 | 15.0 | ❌ No | **1.648%** |
| | **Stardrop Tea** | `(O)StardropTea` | 1 – 3 | 15.0 | ❌ No | **1.648%** |
| | **Prize Ticket** | `(O)PrizeTicket` | 2 – 4 | 15.0 | ❌ No | **1.648%** |
| **Agriculture (~14.285%)**| **Hyper Speed-Gro** | `(O)918` | 10 – 25 | 20.0 | ✅ Yes | **1.867%** |
| | **Deluxe Fertilizer** | `(O)919` | 10 – 25 | 20.0 | ✅ Yes | **1.867%** |
| | **Rare Seed** | `(O)347` | 2 – 6 | 20.0 | ✅ Yes | **1.867%** |
| | **Starfruit Seeds** | `(O)486` | 5 – 15 | 18.0 | ✅ Yes | **1.681%** |
| | **Deluxe Retaining Soil** | `(O)920` | 10 – 25 | 18.0 | ✅ Yes | **1.681%** |
| | **Iridium Sprinkler** | `(O)645` | 1 – 2 | 18.0 | ❌ No | **1.681%** |
| | **Tree Fertilizer** | `(O)805` | 10 – 20 | 15.0 | ✅ Yes | **1.401%** |
| | **Pressure Nozzle** | `(O)915` | 1 – 2 | 12.0 | ✅ Yes | **1.120%** |
| | **Enricher** | `(O)913` | 1 – 2 | 12.0 | ✅ Yes | **1.120%** |
| **Mining (~14.285%)** | **Coal** | `(O)382` | 35 – 90 | 24.0 | ✅ Yes | **1.758%** |
| | **Iridium Ore** | `(O)386` | 10 – 25 | 25.0 | ✅ Yes | **1.831%** |
| | **Mega Bomb** | `(O)288` | 5 – 15 | 25.0 | ✅ Yes | **1.831%** |
| | **Radioactive Ore** | `(O)909` | 5 – 15 | 22.0 | ✅ Yes | **1.612%** |
| | **Iridium Bar** | `(O)337` | 2 – 6 | 22.0 | ✅ Yes | **1.612%** |
| | **Radioactive Bar** | `(O)910` | 2 – 4 | 20.0 | ✅ Yes | **1.465%** |
| | **Cinder Shard** | `(O)848` | 6 – 16 | 20.0 | ✅ Yes | **1.465%** |
| | **Jade** | `(O)70` | 3 – 8 | 20.0 | ✅ Yes | **1.465%** |
| | **Diamond** | `(O)72` | 3 – 8 | 18.0 | ✅ Yes | **1.319%** |
| **Fishing (~14.285%)** | **Challenge Bait** | `(O)ChallengeBait` | 15 – 35 | 22.0 | ✅ Yes | **1.786%** |
| | **Deluxe Bait** | `(O)DeluxeBait` | 20 – 40 | 22.0 | ✅ Yes | **1.786%** |
| | **Magic Bait** | `(O)908` | 10 – 25 | 20.0 | ✅ Yes | **1.623%** |
| | **Trap Bobber** | `(O)694` | 1 – 3 | 18.0 | ✅ Yes | **1.461%** |
| | **Curiosity Lure** | `(O)856` | 1 – 2 | 16.0 | ✅ Yes | **1.299%** |
| | **Sea Jelly** | `(O)SeaJelly` | 1 – 3 | 16.0 | ✅ Yes | **1.299%** |
| | **River Jelly** | `(O)RiverJelly` | 1 – 3 | 16.0 | ✅ Yes | **1.299%** |
| | **Cave Jelly** | `(O)CaveJelly` | 1 – 3 | 16.0 | ✅ Yes | **1.299%** |
| | **Seafoam Pudding** | `(O)265` | 2 – 5 | 15.0 | ✅ Yes | **1.218%** |
| | **Dish O' The Sea** | `(O)242` | 2 – 6 | 15.0 | ✅ Yes | **1.218%** |
| **Combat (~14.285%)** | **Life Elixir** | `(O)773` | 3 – 8 | 22.0 | ✅ Yes | **2.381%** |
| | **Triple Shot Espresso** | `(O)253` | 3 – 10 | 22.0 | ✅ Yes | **2.381%** |
| | **Dragon Tooth** | `(O)852` | 2 – 5 | 20.0 | ✅ Yes | **2.165%** |
| | **Fairy Dust** | `(O)872` | 2 – 5 | 20.0 | ✅ Yes | **2.165%** |
| | **Monster Musk** | `(O)879` | 2 – 5 | 18.0 | ✅ Yes | **1.948%** |
| | **Tiger Slime Egg** | `(O)857` | 1 – 2 | 15.0 | ❌ No | **1.623%** |
| | **Purple Slime Egg** | `(O)439` | 1 – 2 | 15.0 | ❌ No | **1.623%** |
| **Foraging (~14.285%)** | **Hardwood** | `(O)709` | 30 – 80 | 24.0 | ✅ Yes | **3.670%** |
| | **Mystic Tree Seed** | `(O)MysticTreeSeed` | 2 – 6 | 22.0 | ✅ Yes | **3.364%** |
| | **Golden Coconut** | `(O)791` | 2 – 6 | 22.0 | ✅ Yes | **3.364%** |
| | **Magma Cap** | `(O)851` | 3 – 8 | 20.0 | ✅ Yes | **3.058%** |
| | **Purple Mushroom** | `(O)422` | 5 – 12 | 20.0 | ✅ Yes | **3.058%** |
| **Lootboxes (~14.285%)**| **Omni Geode** | `(O)749` | 10 – 25 | 28.0 | ✅ Yes | **2.857%** |
| | **Mystery Box** | `(O)MysteryBox` | 3 – 10 | 25.0 | ✅ Yes | **2.551%** |
| | **Artifact Trove** | `(O)275` | 3 – 10 | 25.0 | ✅ Yes | **2.551%** |
| | **Golden Mystery Box** | `(O)GoldenMysteryBox` | 2 – 5 | 22.0 | ✅ Yes | **2.245%** |
| | **Calico Egg** | `(O)CalicoEgg` | 15 – 40 | 22.0 | ✅ Yes | **2.245%** |
| | **Treasure Totem** | `(O)TreasureTotem` | 2 – 5 | 18.0 | ❌ No | **1.837%** |

---

## ⚙️ Configuration (`config.json`)

```json
{
  "EnableCustomRewards": true,
  "ExcludeCosmetics": true,
  "EnableDepthScaling": true,
  "ScaleLegendaryByDepth": true,
  "MaxRolls": 8,
  "Roll2Chance": 1.0,
  "Roll3Chance": 0.8,
  "Roll4Chance": 0.65,
  "Roll5Chance": 0.5,
  "Roll6Chance": 0.35,
  "Roll7Chance": 0.2,
  "Roll8Chance": 0.1,
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
  "EnableCoal": true,
  "EnableHardwood": true,
  "EnableBombs": true,
  "EnableFishingTackle": true,
  "EnableSlimeEggs": true,
  "EnableCombatConsumables": true,
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
  "GatekeepAutoPetter": false
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
