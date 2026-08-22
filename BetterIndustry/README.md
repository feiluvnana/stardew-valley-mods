# 🏭 BetterIndustry

**BetterIndustry** is a high-efficiency artisan goods, cooking rebalance, and machine automation suite for **Stardew Valley 1.6+**, featuring flower mead value retention, profitable cooking price scaling, and 4-directional **Omni-Hopper** automation with crab pot and cask support.

---

## 📖 Table of Contents
1. [Module 1: Flower Honey Mead Fix](#-module-1-flower-honey-mead-fix)
2. [Module 2: Cooking Profit Balancing](#-module-2-cooking-profit-balancing)
3. [Module 3: 4-Directional Omni-Hopper Automation](#-module-3-4-directional-omni-hopper-automation)
4. [⚙️ Configuration (GMCM & config.json)](#️-configuration-gmcm--configjson)
5. [🛠️ Building & Installation](#️-building--installation)

---

## 🌸 Module 1: Flower Honey Mead Fix

In vanilla Stardew Valley, brewing any high-value flower honey (such as Fairy Rose Honey) in a Keg converts it into basic, generic Mead (200g base value), causing players to lose massive profits.

### BetterIndustry Formula
* **Value & Type Retention:** Preserves the underlying flower type, display name, and price scaling:
  $$\text{Mead Price} = \text{Base Flower Honey Price} \times 2.0$$
* **Examples:**
  * *Fairy Rose Honey (680g base)* $\rightarrow$ **Fairy Rose Mead (1,360g base / 1,904g Artisan)**.
  * *Poppy Honey (380g base)* $\rightarrow$ **Poppy Mead (760g base / 1,064g Artisan)**.
  * *Wild Honey (100g base)* $\rightarrow$ **Mead (200g base / 280g Artisan)**.

---

## 🍳 Module 2: Cooking Profit Balancing

In vanilla, many cooked recipes sell for less than the raw ingredients required to cook them.

### Profit Margin Guarantee
* **Dynamic Price Floor:** Ensures every cooked meal sells for at least its raw ingredient sum multiplied by `CookingProfitMargin` (default **1.25x** / **+25% profit**):
  $$\text{Cooked Meal Price} = \max\left(\text{Vanilla Price},\; \sum \text{Raw Ingredient Values} \times \text{CookingProfitMargin}\right)$$
* Eliminates the penalty of cooking your farm's produce and makes cooking a viable economic choice.

---

## 🛠️ Module 3: 4-Directional Omni-Hopper Automation

Hoppers in vanilla Stardew Valley only push items in a single direction into machines placed directly in front of them, and cannot harvest finished goods. BetterIndustry transforms the Hopper into a complete bidirectional production hub.

### Omni-Hopper Capabilities
1. **📥 4-Directional Auto-Loading:** Automatically loads raw input materials into adjacent machines across all 4 cardinal directions (`North`, `South`, `West`, `East`).
2. **📤 4-Directional Auto-Harvesting:** Collects finished artisan products as soon as processing completes.
3. **📦 Smart Chest Output Routing:** Passes collected items into adjacent standard Chests, Big Chests, or Mini-Shipping Bins.
4. **🦀 Crab Pot Automation:** Automatically loads bait and harvests catches from adjacent Crab Pots placed on shorelines.
5. **🍷 Cask Automation:** Automatically loads wine, cheese, and beer into cellar Casks, and harvests them when fully aged.
6. **🎒 Configurable Capacity:** Switch between 36 slots (vanilla) and 70 slots (expanded storage).
7. **🔊 Audio Feedback:** Plays subtle, satisfying clicks and thuds when loading or collecting goods.

---

## ⚙️ Configuration (GMCM & config.json)

```json
{
  "EnableCookingBalancing": true,
  "CookingProfitMargin": 1.25,
  "EnableMeadFix": true,
  "EnableAutoHarvest": true,
  "EnableChestOutputTransfer": true,
  "EnablePeriodicProcessing": true,
  "ProcessIntervalTicks": 60,
  "EnableCrabPotService": true,
  "EnableCaskService": true,
  "HopperCapacity": 36,
  "PlaySoundEffects": true
}
```

| Category | Setting | Default | Description |
| :--- | :--- | :---: | :--- |
| **Cooking** | `EnableCookingBalancing` | `true` | Enables ingredient-based cooking price scaling. |
| **Cooking** | `CookingProfitMargin` | `1.25` | Minimum profit multiplier over raw ingredients (1.25 = +25%). |
| **Artisan** | `EnableMeadFix` | `true` | Retains flower honey flavor and 2.0x value scaling when brewed into mead. |
| **Automation** | `EnableAutoHarvest` | `true` | Automatically harvests finished goods from 4 adjacent machines. |
| **Automation** | `EnableChestOutputTransfer`| `true` | Routes harvested items into adjacent chests or mini-shipping bins. |
| **Automation** | `EnablePeriodicProcessing` | `true` | Processes machine logic periodically in the background. |
| **Automation** | `ProcessIntervalTicks` | `60` | Automation scan interval in game ticks (60 ticks = 1 second). |
| **Machines** | `EnableCrabPotService` | `true` | Auto-baits and harvests adjacent crab pots. |
| **Machines** | `EnableCaskService` | `true` | Loads and harvests aging casks. |
| **Customization**| `HopperCapacity` | `36` | Hopper storage capacity (36 slots or 70 slots). |
| **Customization**| `PlaySoundEffects` | `true` | Plays audio cues upon item transfers. |

---

## 🛠️ Building & Installation

### Requirements
* **Stardew Valley 1.6+**
* **SMAPI 4.0+**
* *(Optional)* **Generic Mod Config Menu**

### Building from Source
```powershell
dotnet build BetterIndustry.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
