# 💎 BetterChest (v1.8.0)

**BetterChest** is a comprehensive, progression-balanced loot overhaul for **Stardew Valley 1.6+**, transforming **Skull Cavern Treasure Rooms** with dynamic gameplay loot engines, decaying multi-rolls, critical stack multipliers, depth tier scaling, and intelligent progression gatekeeping. *(Note: Fishing treasure chest enhancements have been migrated to BetterFishing).*

---

## 📖 Table of Contents
1. [Core Philosophy](#-core-philosophy)
2. [Module 1: Skull Cavern Loot Overhaul](#-module-1-skull-cavern-loot-overhaul)
   - [7 Gameplay Categories & Item Pool](#7-gameplay-categories--item-pool)
   - [Decaying Multi-Roll Engine & Minimum Guarantees](#decaying-multi-roll-engine--minimum-guarantees)
   - [Critical Stack Multipliers](#critical-stack-multipliers)
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
1. **Meaningful Rewards:** Generous multi-rolls, critical jackpot procs, high-utility resources (Hardwood, Coal, Machines), and no dud warp totems.
2. **Context-Aware Progression:** Gatekeeping late-game materials (Volcano Caldera shortcut for Cinder Shards, Masteries/skills for respective goods), scaling rolls by mine depth, and guaranteeing bonus rolls.

---

## 🏔️ Module 1: Skull Cavern Loot Overhaul

### 7 Gameplay Categories & Item Pool
Every treasure chest in Skull Cavern selects items across 7 dedicated utility pools:

1. **Legendary:** Prismatic Shard, Magic Rock Candy, Golden Animal Cracker, Auto-Petter, Galaxy Soul, Stardrop Tea, Prize Ticket, Book of Stars.
2. **Agriculture:** Hyper Speed-Gro, Deluxe Fertilizer, Deluxe Retaining Soil, Rare Seed, Starfruit Seeds, Iridium Sprinkler, Auto-Grabber, Seed Maker, Tree Fertilizer, Pressure Nozzle, Enricher, Stardew Valley Almanac, Animal Catalogue.
3. **Mining:** Iridium Ore, Mega Bomb, Radioactive Ore, Iridium Bar, Radioactive Bar, Coal (35–90), Crystalarium, Cinder Shard, Jade, Diamond, Mining Monthly, Dwarvish Safety Manual, The Diamond Hunter.
4. **Fishing:** Challenge Bait, Deluxe Bait, Magic Bait, Trap Bobber, Curiosity Lure, Sea Jelly, River Jelly, Cave Jelly, Seafoam Pudding, Dish O' The Sea, Bait And Bobber, The Art O' Crabbing, Jewels Of The Sea.
5. **Combat:** Life Elixir, Triple Shot Espresso, Dragon Tooth, Fairy Dust, Monster Musk, Tiger Slime Egg, Purple Slime Egg, Combat Quarterly, Monster Compendium, Jack Be Nimble Jack Be Thick.
6. **Foraging:** Hardwood (30–80), Mystic Tree Seed, Golden Coconut, Magma Cap, Purple Mushroom, Woodcutter's Weekly, Woody's Secret, Ol' Slitherlegs.
7. **Lootboxes:** Omni Geode, Mystery Box, Golden Mystery Box, Artifact Trove, Calico Egg, Treasure Totem, Book of Mysteries, Treasure Appraisal Guide, Price Catalogue.

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

### Depth-Based Tier Scaling
- **Shallow Floors (1–49):**
  - Min 1 roll, maximum rolls capped at **4 items**.
  - Stack jackpot multiplier capped at **2x max**.
- **Deep Floors (50–99):**
  - Standard **min 2 guaranteed rolls** and up to **8 rolls** with full **4x jackpot multipliers**.
- **Floor 100+ Special Chests:**
  - Supercharged ceiling up to **12 rolls** (mi## 🛡️ Master Gatekeeping & Unlock Matrix

| Feature / Setting | Description | Default |
| :--- | :--- | :---: |
| `EnableDepthScaling` | Caps shallow floors (1–49) to 4 rolls and 2x stack limit | `true` |
| `GatekeepMasteryItems` | Gated until respective 1.6 Mastery (or Skill $\ge 9$) is claimed | `true` |
| `GatekeepIslandItems` | Gated until Ginger Island is visited (and Volcano Caldera shortcut for Cinder Shards) | `true` |
| `GatekeepQiItems` | Gated until Qi's Walnut Room is unlocked (100 Walnuts) | `true` |
| `GatekeepRadioactiveItems` | Gated until Qi's Room unlocked or Dangerous Mines active | `true` |
| `GatekeepMysteryBoxes` | Gated until Mr. Qi Mystery Box event occurs | `true` |
| `GatekeepCalicoEggs` | Gated to Desert Festival season (Spring 15–17) or Year 2+ | `true` |
| `GatekeepAutoPetter` | Gated until Community Center / Joja completion | `false` |

---

## 📊 Complete Drop Rate & Probability Table

### Regular Chests (Equal 15.0 Category Weights Baseline)

| Category | Item Name | Qualified ID | Base Stack | Item Weight | Multipliers | Gatekeeping Condition |
| :--- | :--- | :--- | :---: | :---: | :---: | :--- |
| **Legendary (~14.285%)** | **Prismatic Shard** | `(O)74` | 1 – 2 | 25.0 | ❌ No | None |
| | **Magic Rock Candy** | `(O)279` | 1 – 2 | 20.0 | ❌ No | None |
| | **Golden Animal Cracker** | `(O)GoldenAnimalCracker` | 1 – 2 | 20.0 | ❌ No | Farming Mastery |
| | **Auto-Petter** | `(BC)272` | 1 | 20.0 | ❌ No | CC / Joja Completion (Optional) |
| | **Galaxy Soul** | `(O)896` | 1 – 2 | 15.0 | ❌ No | Qi's Walnut Room (100 Walnuts) |
| | **Stardrop Tea** | `(O)StardropTea` | 1 – 3 | 15.0 | ❌ No | None |
| | **Prize Ticket** | `(O)PrizeTicket` | 2 – 4 | 15.0 | ❌ No | None |
| | **Book of Stars** | `(O)Book_Stars` | 1 | 10.0 | ❌ No | Any Mastery Claimed |
| **Agriculture (~14.285%)**| **Hyper Speed-Gro** | `(O)918` | 10 – 25 | 20.0 | ✅ Yes | Qi's Walnut Room (100 Walnuts) |
| | **Deluxe Fertilizer** | `(O)919` | 10 – 25 | 20.0 | ✅ Yes | Qi's Walnut Room (100 Walnuts) |
| | **Rare Seed** | `(O)347` | 2 – 6 | 20.0 | ✅ Yes | None |
| | **Starfruit Seeds** | `(O)486` | 5 – 15 | 18.0 | ✅ Yes | None |
| | **Deluxe Retaining Soil** | `(O)920` | 10 – 25 | 18.0 | ✅ Yes | Qi's Walnut Room (100 Walnuts) |
| | **Iridium Sprinkler** | `(O)645` | 1 – 2 | 18.0 | ❌ No | None |
| | **Auto-Grabber** | `(BC)165` | 1 | 12.0 | ❌ No | Farming Level $\ge 9$ |
| | **Seed Maker** | `(BC)25` | 1 | 10.0 | ❌ No | Farming Level $\ge 9$ |
| | **Tree Fertilizer** | `(O)805` | 10 – 20 | 15.0 | ✅ Yes | None |
| | **Pressure Nozzle** | `(O)915` | 1 – 2 | 12.0 | ✅ Yes | Qi's Walnut Room (100 Walnuts) |
| | **Enricher** | `(O)913` | 1 – 2 | 12.0 | ✅ Yes | Qi's Walnut Room (100 Walnuts) |
| | **Stardew Valley Almanac** | `(O)Book_Farming` | 1 | 8.0 | ❌ No | None |
| | **Animal Catalogue** | `(O)Book_Animal` | 1 | 6.0 | ❌ No | None |
| **Mining (~14.285%)** | **Iridium Ore** | `(O)386` | 10 – 25 | 25.0 | ✅ Yes | None |
| | **Mega Bomb** | `(O)288` | 5 – 15 | 25.0 | ✅ Yes | None |
| | **Radioactive Ore** | `(O)909` | 5 – 15 | 22.0 | ✅ Yes | Qi's Walnut Room |
| | **Iridium Bar** | `(O)337` | 2 – 4 | 22.0 | ❌ No | None |
| | **Radioactive Bar** | `(O)910` | 1 – 3 | 20.0 | ❌ No | Qi's Walnut Room |
| | **Coal** | `(O)382` | 35 – 90 | 24.0 | ✅ Yes | None |
| | **Crystalarium** | `(BC)21` | 1 | 12.0 | ❌ No | Mining Level $\ge 9$ |
| | **Cinder Shard** | `(O)848` | 6 – 16 | 20.0 | ✅ Yes | Volcano Caldera Shortcut |
| | **Jade** | `(O)70` | 3 – 8 | 20.0 | ✅ Yes | None |
| | **Diamond** | `(O)72` | 3 – 8 | 18.0 | ✅ Yes | None |
| | **Mining Monthly** | `(O)Book_Mining` | 1 | 8.0 | ❌ No | None |
| | **Dwarvish Safety Manual**| `(O)Book_Bombs` | 1 | 6.0 | ❌ No | None |
| | **The Diamond Hunter** | `(O)Book_Diamonds` | 1 | 6.0 | ❌ No | None |
| **Fishing (~14.285%)** | **Challenge Bait** | `(O)ChallengeBait` | 15 – 35 | 22.0 | ✅ Yes | Fishing Mastery |
| | **Deluxe Bait** | `(O)DeluxeBait` | 20 – 40 | 22.0 | ✅ Yes | None |
| | **Magic Bait** | `(O)908` | 10 – 25 | 20.0 | ✅ Yes | Qi's Walnut Room |
| | **Trap Bobber** | `(O)694` | 1 – 3 | 18.0 | ✅ Yes | None |
| | **Curiosity Lure** | `(O)856` | 1 – 2 | 16.0 | ✅ Yes | None |
| | **Sea Jelly** | `(O)SeaJelly` | 1 – 3 | 16.0 | ✅ Yes | None |
| | **River Jelly** | `(O)RiverJelly` | 1 – 3 | 16.0 | ✅ Yes | None |
| | **Cave Jelly** | `(O)CaveJelly` | 1 – 3 | 16.0 | ✅ Yes | None |
| | **Seafoam Pudding** | `(O)265` | 2 – 5 | 15.0 | ✅ Yes | None |
| | **Dish O' The Sea** | `(O)242` | 2 – 6 | 15.0 | ✅ Yes | None |
| | **Bait And Bobber** | `(O)Book_Fishing` | 1 | 8.0 | ❌ No | None |
| | **The Art O' Crabbing** | `(O)Book_Crabbing` | 1 | 6.0 | ❌ No | None |
| | **Jewels Of The Sea** | `(O)Book_Roe` | 1 | 6.0 | ❌ No | None |
| **Combat (~14.285%)** | **Life Elixir** | `(O)773` | 3 – 8 | 22.0 | ✅ Yes | None |
| | **Triple Shot Espresso** | `(O)253` | 3 – 10 | 22.0 | ✅ Yes | None |
| | **Dragon Tooth** | `(O)852` | 2 – 5 | 20.0 | ✅ Yes | Ginger Island Visited |
| | **Fairy Dust** | `(O)872` | 2 – 5 | 20.0 | ✅ Yes | Ginger Island Visited |
| | **Monster Musk** | `(O)879` | 2 – 5 | 18.0 | ✅ Yes | None |
| | **Tiger Slime Egg** | `(O)857` | 1 – 2 | 15.0 | ❌ No | Ginger Island Visited |
| | **Purple Slime Egg** | `(O)439` | 1 – 2 | 15.0 | ❌ No | None |
| | **Combat Quarterly** | `(O)Book_Combat` | 1 | 8.0 | ❌ No | None |
| | **Monster Compendium** | `(O)Book_Void` | 1 | 6.0 | ❌ No | None |
| | **Jack Be Nimble...** | `(O)Book_Defense` | 1 | 6.0 | ❌ No | None |
| **Foraging (~14.285%)** | **Hardwood** | `(O)709` | 30 – 80 | 24.0 | ✅ Yes | None |
| | **Mystic Tree Seed** | `(O)MysticTreeSeed` | 2 – 6 | 22.0 | ✅ Yes | Foraging Mastery |
| | **Golden Coconut** | `(O)791` | 2 – 6 | 22.0 | ✅ Yes | Ginger Island Visited |
| | **Magma Cap** | `(O)851` | 3 – 8 | 20.0 | ✅ Yes | Ginger Island Visited |
| | **Purple Mushroom** | `(O)422` | 5 – 12 | 20.0 | ✅ Yes | None |
| | **Woodcutter's Weekly** | `(O)Book_Foraging` | 1 | 8.0 | ❌ No | None |
| | **Woody's Secret** | `(O)Book_Woodcutting`| 1 | 6.0 | ❌ No | None |
| | **Ol' Slitherlegs** | `(O)Book_Grass` | 1 | 6.0 | ❌ No | None |
| **Lootboxes (~14.285%)**| **Omni Geode** | `(O)749` | 10 – 25 | 28.0 | ✅ Yes | None |
| | **Mystery Box** | `(O)MysteryBox` | 3 – 10 | 25.0 | ✅ Yes | Qi Mystery Box Event |
| | **Artifact Trove** | `(O)275` | 3 – 10 | 25.0 | ✅ Yes | None |
| | **Golden Mystery Box** | `(O)GoldenMysteryBox` | 2 – 5 | 22.0 | ✅ Yes | Foraging Mastery & Qi Mystery Box |
| | **Calico Egg** | `(O)CalicoEgg` | 15 – 40 | 22.0 | ✅ Yes | Desert Festival Active |
| | **Treasure Totem** | `(O)TreasureTotem` | 2 – 5 | 18.0 | ❌ No | Foraging Mastery |
| | **Book of Mysteries** | `(O)Book_Mystery` | 1 | 6.0 | ❌ No | Qi Mystery Box Event |
| | **Treasure Appraisal Guide**| `(O)Book_Artifact`| 1 | 6.0 | ❌ No | None |
| | **Price Catalogue** | `(O)Book_PriceCatalogue`| 1 | 6.0 | ❌ No | None |

---

## ⚙️ Configuration (`config.json`)

```json
{
  "EnableCustomRewards": true,
  "ExcludeCosmetics": true,
  "EnableDepthScaling": true,
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
  "EnableAutoPetter": true,
  "EnableGalaxySoul": true,
  "EnablePrizeTicket": true,
  "EnableStardropTea": true,
  "EnableFertilizers": true,
  "EnableMachines": true,
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
