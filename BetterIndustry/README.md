# 🏭 BetterIndustry (Stardew Valley 1.6+)

A comprehensive artisan goods, cooking rebalance, and machine automation suite for **Stardew Valley 1.6+**, combining fair economic scaling with high-efficiency 4-directional Omni-Hopper automation. Built with SMAPI and Harmony.

---

## 🌟 Key Features

### 🌸 1. Flower Honey Mead Fix
* **Flower Mead Retention:** Preserves the flower type and price scaling when brewing honey into mead in Kegs with the default 2.0x multiple (e.g. Fairy Rose Honey turns into Fairy Rose Mead scaling with its value instead of reverting to generic base mead).

### 🍳 2. Cooking Profit Balancing
* **Guaranteed Profit Margins (`CookingProfitMargin: 1.25x` / +25%):** Dynamically ensures cooked meals sell for at least the configured profit margin (default +25%) over their raw ingredient costs.

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
| **Artisan** | `EnableMeadFix` | `true` | Retains flower honey value and type when brewed into mead in kegs. |
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
