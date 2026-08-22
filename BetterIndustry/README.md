# 🏭 BetterIndustry (Stardew Valley 1.6+)

A comprehensive artisan goods, cooking rebalance, and machine automation suite for **Stardew Valley 1.6+**, combining fair economic scaling with high-efficiency 4-directional Omni-Hopper automation. Built with SMAPI and Harmony.

---

## 🌟 Key Features

### 🌸 1. Artisan Goods & Flower Mead Scaling
* **Flower Mead Retention (`MeadMultiplier: 1.5x`):** Preserves the flower type and value when brewing honey into mead in Kegs (e.g. Fairy Rose Mead scales properly instead of reverting to generic base mead).
* **Vegetable Juice Buff (`JuiceMultiplier: 3.0x`):** Scales vegetable juices to make them competitive with fruit wines.
* **Preserves Jars & Aged Roe (`PickleMultiplier: 2.5x`, `AgedRoeMultiplier: 2.5x`):** Buffs pickles and aged roe margins.
* **Caviar Pricing (`CaviarPrice: 750g`):** Ensures sturgeon caviar is appropriately valued.

### 🍳 2. Cooking Margin & Buff Duration Overhaul
* **Guaranteed Profit Margins (`CookingProfitMargin: 1.25x` / +25%):** Dynamically ensures cooked meals sell for at least 25% more than their raw ingredient costs.
* **Energy & Health Boost (`EnergyMultiplier: 1.25x`):** Multiplies stamina/health recovery from cooked food by +25%.
* **Extended Buff Durations (`BuffDurationMultiplier: 1.5x`):** Increases food buff durations by +50%.

### 🛠️ 3. 4-Directional Omni-Hopper Automation
* **📥 4-Directional Feeding:** Automatically loads raw ingredients into adjacent machines in all 4 cardinal directions (`North`, `South`, `West`, `East`).
* **📤 4-Directional Auto-Harvest:** Automatically collects finished goods as soon as machines complete processing.
* **📦 Smart Chest Output Routing:** Passes collected products into adjacent regular Chests or Mini-Shipping Bins.
* **🦀 Special Machine Support:** Auto-baits and harvests Crab Pots; loads and harvests Casks.
* **🎒 Hopper Capacity Upgrade:** Configurable hopper storage (36 slots vanilla or 70 slots expanded).

---

## ⚙️ Configuration (Generic Mod Config Menu)

All features can be customized in-game via **Generic Mod Config Menu (GMCM)** or through `config.json`:

| Category | Option | Default | Description |
| :--- | :--- | :---: | :--- |
| **Cooking** | `EnableCookingBalancing` | `true` | Enables ingredient-based cooking price scaling. |
| **Cooking** | `CookingProfitMargin` | `1.25` | Profit multiplier over raw ingredients (1.25 = +25%). |
| **Cooking** | `EnableEnergyBuff` | `true` | Multiplies energy/health gained from cooked food. |
| **Cooking** | `EnergyMultiplier` | `1.25` | Energy multiplier factor. |
| **Cooking** | `EnableBuffDurationBoost` | `true` | Extends buff durations granted by cooked dishes. |
| **Cooking** | `BuffDurationMultiplier` | `1.5` | Multiplier for food buff durations (+50%). |
| **Artisan** | `EnableMeadFix` | `true` | Retains flower honey value when brewed in kegs. |
| **Artisan** | `MeadMultiplier` | `1.5` | Multiplier for flower mead value. |
| **Artisan** | `EnableJuiceBuff` | `true` | Boosts vegetable juice multiplier. |
| **Artisan** | `JuiceMultiplier` | `3.0` | Value multiplier for vegetable juice. |
| **Artisan** | `EnablePickleBuff` | `true` | Boosts preserves jar pickle multiplier. |
| **Artisan** | `PickleMultiplier` | `2.5` | Multiplier for pickled crops. |
| **Artisan** | `EnableRoeBuff` | `true` | Boosts aged roe and caviar prices. |
| **Artisan** | `AgedRoeMultiplier` | `2.5` | Multiplier for aged roe in preserves jars. |
| **Artisan** | `CaviarPrice` | `750` | Flat gold price for Caviar. |
| **Automation** | `EnableAutoHarvest` | `true` | Auto-harvests finished goods from 4 adjacent machines. |
| **Automation** | `EnableChestOutputTransfer` | `true` | Transfers harvested items into adjacent chests/shipping bins. |
| **Automation** | `EnablePeriodicProcessing` | `true` | Periodically processes machines in the background. |
| **Automation** | `ProcessIntervalTicks` | `60` | Background automation interval in game ticks. |
| **Machines** | `EnableCrabPotService` | `true` | Auto-baits and harvests crab pots. |
| **Machines** | `EnableCaskService` | `true` | Loads and harvests aging casks. |
| **Customization** | `HopperCapacity` | `36` | Hopper storage capacity (36 or 70 slots). |
| **Customization** | `PlaySoundEffects` | `true` | Plays subtle audio feedback on load/harvest/transfer. |

---

## 🚀 Building

```powershell
dotnet build BetterIndustry/BetterIndustry.csproj
```

---

## 📄 License & Credits
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
