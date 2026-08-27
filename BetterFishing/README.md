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

### 4. 👑 Legendary Prize Multiplier & Signature
* **$+100.0\%$ ($2\times$) Dedicated Legendary Multiplier**: Legendary fish feel like monumental trophies.
* **Deterministic Species Signature (0% to +8%)**: Predictable species trait variance derived from Item ID.
* **100% Native Compatibility**: Overwrites `Data/Objects` at `Late` priority, meaning the **Price Catalogue** power book, tooltips, Fish Smokers ($2\times$), Roe ($30 + P/2$), and Aged Roe ($2\times \text{Roe}$) update seamlessly with zero conflicts.

### 5. 🎁 Decaying Multi-Roll Fishing Treasure Chests
* Migrated from *BetterChest*: replaces vanilla hardcoded decay multipliers (`0.40f` $\rightarrow$ `0.60f` for standard chests; `0.60f` $\rightarrow$ `0.80f` for 1.6 golden chests) for rewarding, vanilla-faithful multi-rolls.

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
| `ApexFactor` | `0.91252` | Apex scaling factor for D > 50. |
| `ApexExponent` | `4.34` | Exponential power for D > 50. |
| `LegendaryFishMultiplierBonus` | `1.00` | Legendary prize multiplier (+100%). |
| `EnablePredictableHashBonus` | `true` | Enables deterministic 0%–8% species hash bonus. |
| `EnableFishingChestBuff` | `true` | Enable decaying-roll enhancement for treasure chests. |
| `FishingChestDecayRate` | `0.60` | Roll probability decay for standard chests (vanilla 0.40). |
| `GoldenChestDecayRate` | `0.80` | Roll probability decay for golden chests (vanilla 0.60). |
