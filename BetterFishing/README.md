# 🎣 BetterFishing

**BetterFishing** is a comprehensive fishing rebalance mod for **Stardew Valley 1.6+ (SMAPI 4.0+)**. It scales fish base sell prices proportionally with their catch difficulty, movement behaviors, and environmental traits, while preserving iconic vanilla anchors and enhancing fishing treasure chest multi-rolls.

---

## ✨ Features

### 1. ⚖️ Dual-Anchored Difficulty Curve
Fish base prices are evaluated dynamically through a calibrated difficulty curve anchored to two vanilla standards:
* **Catfish (Difficulty 75)** $\rightarrow$ **200g** (Exact vanilla reference point).
* **Legend (Difficulty 110)** $\rightarrow$ **5,000g** (Exact vanilla reference point).

$$P_{\text{base}}(D) = F + c_1 \cdot D + c_2 \cdot \left(\frac{D}{50}\right)^2 + c_3 \cdot \max\left(0,\, \frac{D - 50}{10}\right)^{4.34}$$

### 2. 🏃 Movement Behavior Bonuses
* **`smooth`**: **$+2.0\%$** (e.g. Bream, Eel)
* **`mixed`**: **$+3.0\%$** (e.g. Carp, Salmon, Legend)
* **`floater`**: **$+4.0\%$** (e.g. Pufferfish, Blobfish)
* **`sinker`**: **$+5.0\%$** (e.g. Squid, Octopus, Super Cucumber)
* **`dart`**: **$+6.0\%$** (e.g. Catfish, Pike, Scorpion Carp)

### 3. 🌧️ Environmental & Location Traits
* **Rain-only catches**: **$+2.0\%$** (e.g. Catfish, Eel, Red Snapper)
* **Night / Tight time window ($\le 6$ hours)**: **$+2.0\%$** (e.g. Pufferfish, Squid, Midnight Carp)
* **Single season exclusive**: **$+2.0\%$** (e.g. Salmon, Lingcod)
* **Small / Isolated location**: **$+2.0\%$** (Secret Woods, Mines, Witch's Swamp, Sewers, Desert, Submarine, Pirate Cove, Caldera)

### 4. 👑 Legendary Fish Vanilla Anchor & Signature
* **5,000g Exact Legend Base Anchor**: Legend (`(O)163`) base price is strictly preserved at vanilla **5,000g** (Smoked Iridium Legend = 28,000g Artisan).
* **Deterministic Species Signature (0% to +8%)**: Predictable species trait variance derived from Item ID.
* **100% Native Compatibility**: Overwrites `Data/Objects` at `Late` priority, meaning the **Price Catalogue** power book, tooltips, Fish Smokers ($2\times$), Roe ($30 + P/2$), and Aged Roe ($2\times \text{Roe}$) update seamlessly with zero conflicts.

### 5. 🎁 Decaying Multi-Roll Fishing Treasure Chests
* Migrated from *BetterChest*: replaces vanilla hardcoded decay multipliers (`0.40f` $\rightarrow$ `0.45f` for standard chests; `0.60f` $\rightarrow$ `0.60f` for 1.6 golden chests) for rewarding, vanilla-faithful multi-rolls.

### 6. 🧠 Targeted Fishing Experience (EXP) Balancing
* **100% Vanilla EXP for Standard Fish ($D < 85$)**: Preserves vanilla experience progression exactly for common and mid-tier fish (Carp 8 EXP, Catfish 28 EXP, etc.).
* **Targeted Buff for Underwhelming Apex Fish ($D \ge 85$)**: Adds a modest **$+15\text{ EXP}$** bonus to brutally difficult non-legendary catches (Lingcod $31 \rightarrow 46\text{ EXP}$, Octopus $34 \rightarrow 49\text{ EXP}$, Lava Eel $33 \rightarrow 48\text{ EXP}$).
* **Rewarding Legendary Catches**: Adds **$+60\text{ EXP}$** to all 10 Legendary and Extended Family catches (Legend $39 \rightarrow 99\text{ EXP}$).
* **Vanilla Multipliers Preserved**: Perfect catches ($\times 2.4$) and Treasure chest catches ($\times 2.2$) continue to multiply on top of the calculated base EXP.

### 7. 🦀 Crab Pot Overhaul (Trash Nerf, Prices & Tiered EXP)
* **🗑️ Trash Chance Nerfed**: Applies a 65% trash reroll check, converting trash rolls into valid shellfish and dropping the effective trash rate from ~38% down to ~13%.
* **💰 Rebalanced Base Sell Prices**: Lobster (200g), Crab (150g), Crayfish (110g), Snail & Oyster (95g), Shrimp (90g), Cockle & Clam (75g), Mussel (55g), Periwinkle (45g).
* **🧠 Tiered Harvest Experience**: Lobster (24 EXP), Crab (16 EXP), Delicacies (12 EXP), Shellfish (8 EXP), Trash (5 EXP).

### 8. 🐟 Fish Pond Aquaculture & Luxury Caviar
* **⭐ Star Quality Inheritance**: Fish Ponds dynamically award **Silver, Gold, or Iridium quality** to Roe based on pond population tier (up to 40% Gold / 25% Iridium at capacity 10) and Daily Luck.
* **👑 Caviar Rebalance**: Sturgeon Caviar base price rebalanced from 500g to **800g base** (**1,120g Artisan / 2,240g Iridium Artisan**), turning Fish Ponds into genuine high-tier luxury estates.

---

## ⚙️ Configuration (GMCM)

All options can be configured in-game via **Generic Mod Config Menu** or in `config.json`:

| Key | Default | Description |
| :--- | :---: | :--- |
| `EnableFishPriceBalancing` | `true` | Enable dynamic fish price scaling. |
| `PreventNerf` | `true` | Guarantees no vanilla fish price drops below vanilla value. |
| `BaseFloor` | `20.0` | Base price floor before difficulty scaling. |
| `LinearFactor` | `0.80` | Linear difficulty multiplier. |
| `MidTierFactor` | `25.0` | Mid-tier quadratic factor ((D/50)^2). |
| `ApexFactor` | `0.9293` | Apex scaling factor for D > 50. |
| `ApexExponent` | `4.34` | Exponential power for D > 50. |
| `LegendaryFishMultiplierBonus` | `0.00` | Legendary prize multiplier bonus (Legend anchored strictly at 5,000g). |
| `EnablePredictableHashBonus` | `true` | Enables deterministic 0%–8% species hash bonus. |
| `EnableFishingChestBuff` | `true` | Enable decaying-roll enhancement for treasure chests. |
| `FishingChestDecayRate` | `0.45` | Roll probability decay for standard chests. |
| `GoldenChestDecayRate` | `0.60` | Roll probability decay for golden chests. |
| `EnableFishingExpBalancing` | `true` | Enables targeted fishing experience balancing. |
| `ApexFishExpBonus` | `15` | Bonus EXP for hard non-legendary fish (D >= 85). |
| `LegendaryFishExpBonus` | `60` | Bonus EXP for Legendary fish catches. |
| `EnableCrabPotPriceBalancing` | `true` | Enables rebalanced sell prices for crab pot catches. |
| `EnableCrabPotExpBalancing` | `true` | Enables tiered harvest experience for crab pots. |
| `EnableCrabPotTrashReduction` | `true` | Enables crab pot trash chance reduction. |
| `CrabPotTrashRerollChance` | `0.65` | Probability of converting trash into genuine shellfish (default: 65%). |
| `EnableFishPondQuality` | `true` | Enables star quality for Fish Pond roe outputs based on population. |
| `EnableCaviarRebalance` | `true` | Enables base price rebalancing for Sturgeon Caviar. |
| `CaviarBasePrice` | `800` | Base sell price for Sturgeon Caviar (800g base / 1,120g Artisan). |
