# 🏭 BetterIndustry

**BetterIndustry** is a high-efficiency artisan goods and cooking rebalance suite for **Stardew Valley 1.6+**, eliminating the most frustrating pain points of the artisan and culinary systems.

---

## 📖 Table of Contents
1. [Module 1: Flower Honey Mead Fix](#-module-1-flower-honey-mead-fix)
2. [Module 2: Cooking Profit Balancing](#-module-2-cooking-profit-balancing)
3. [Module 3: Quality-Preserving Machines](#-module-3-quality-preserving-machines)
4. [Module 4: Truffle Oil Scaling Fix](#-module-4-truffle-oil-scaling-fix)
5. [Module 5: Vegetable Juice Buff](#-module-5-vegetable-juice-buff)
6. [Module 6: Expanded Cask Aging](#-module-6-expanded-cask-aging)
7. [⚙️ Configuration (GMCM & config.json)](#️-configuration-gmcm--configjson)
8. [🛠️ Building & Installation](#️-building--installation)

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

## ⭐ Module 3: Quality-Preserving Machines

In vanilla, artisan machines strip all star quality (Silver, Gold, Iridium) from input items, penalizing players for using high-quality crops and animal products.

### Features
* **Full Quality Inheritance:** Kegs, Preserves Jars, Cheese Presses, Mayonnaise Machines, Looms, Dehydrators, and Fish Smokers preserve the input item's star quality on finished goods.
* **Examples:**
  * *Iridium Starfruit* in Keg $\rightarrow$ **Iridium Starfruit Wine**.
  * *Iridium Large Milk* in Cheese Press $\rightarrow$ **Iridium Cheese**.
  * *Iridium Egg* in Mayo Machine $\rightarrow$ **Iridium Mayonnaise**.
  * *Iridium Wool* in Loom $\rightarrow$ **Iridium Cloth** (2.0x base sell value).

---

## 🍄 Module 4: Truffle Oil Scaling Fix

In vanilla, an Iridium Truffle sells for **1,250g raw**, but Truffle Oil sells for only **1,065g base**—a net **loss of 185g** without the Artisan profession.

### BetterIndustry Formula
* **Proportional Scaling:** Truffle Oil price is dynamically computed from input Truffle value using `TruffleOilMultiplier` (default **1.5x**) and retains the Truffle's quality star:
  $$\text{Truffle Oil Price} = \text{Input Truffle Value} \times \text{TruffleOilMultiplier}$$

| Truffle Quality | Raw Sale Value | Vanilla Oil Sale | BetterIndustry Oil (Base) | BetterIndustry Oil (Artisan) |
| :--- | :---: | :---: | :---: | :---: |
| **Regular** | 625g | 1,065g | **937g** | **1,311g** |
| **Silver** | 781g | 1,065g | **1,171g** | **1,639g** |
| **Gold** | 937g | 1,065g | **1,405g** | **1,967g** |
| **Iridium** | 1,250g | 1,065g | **1,875g** | **2,625g** |

---

## 🥕 Module 5: Vegetable Juice Buff

In vanilla, fruit wines receive a 3.0x multiplier and can be aged in casks, while vegetable juices receive only a 2.25x multiplier and cannot be aged, heavily disincentivizing vegetable farming.

### BetterIndustry Formula
* **Enhanced Multiplier:** Boosts Vegetable Juice price scaling from 2.25x to `JuiceMultiplier` (default **2.75x**):
  $$\text{Juice Price} = \text{Base Vegetable Price} \times \text{JuiceMultiplier}$$
* **Example (Pumpkin, 320g base):**
  * Vanilla Juice: 720g base (1,008g Artisan).
  * BetterIndustry Juice: **880g base** (**1,232g Artisan**).

---

## 🍷 Module 6: Expanded Cask Aging

In vanilla, Casks in the Farmhouse Cellar can only age Wine, Cheese, Goat Cheese, Beer, Mead, and Pale Ale.

### Features
* **Juice Aging:** Vegetable Juice can now be placed into Casks to age from normal to Silver, Gold, and Iridium quality (Aging rate: 4.0, matching Wine).
* Reaches Iridium quality in 56 days for a 2.0x value bonus!

---

## ⚙️ Configuration (GMCM & config.json)

```json
{
  "EnableCookingBalancing": true,
  "CookingProfitMargin": 1.25,
  "EnableMeadFix": true,
  "EnableQualityPreserving": true,
  "EnableTruffleOilFix": true,
  "TruffleOilMultiplier": 1.5,
  "EnableJuiceBuff": true,
  "JuiceMultiplier": 2.75,
  "EnableExpandedAging": true
}
```

| Category | Setting | Default | Range / Step | Description |
| :--- | :--- | :---: | :---: | :--- |
| **Cooking** | `EnableCookingBalancing` | `true` | bool | Enables ingredient-based cooking price scaling. |
| **Cooking** | `CookingProfitMargin` | `1.25` | `1.0` – `5.0` (`0.05`) | Minimum profit multiplier over raw ingredients (+25%). |
| **Artisan** | `EnableMeadFix` | `true` | bool | Retains flower honey flavor and 2.0x value scaling when brewed into mead. |
| **Artisan** | `EnableQualityPreserving` | `true` | bool | Preserves input star quality across all artisan machines. |
| **Artisan** | `EnableTruffleOilFix` | `true` | bool | Truffle Oil scales value and quality based on the input Truffle. |
| **Artisan** | `TruffleOilMultiplier` | `1.5` | `1.0` – `3.0` (`0.05`) | Multiplier for Truffle Oil relative to input truffle price. |
| **Artisan** | `EnableJuiceBuff` | `true` | bool | Buffs the price multiplier for Vegetable Juice brewed in Kegs. |
| **Artisan** | `JuiceMultiplier` | `2.75` | `1.0` – `5.0` (`0.05`) | Multiplier for Vegetable Juice relative to raw vegetable price. |
| **Artisan** | `EnableExpandedAging` | `true` | bool | Allows Casks to age additional artisan goods (Vegetable Juice). |

---

## 🛠️ Building & Installation

### Requirements
* **Stardew Valley 1.6+**
* **SMAPI 4.0+**
* *(Optional)* **Generic Mod Config Menu**

### Building from Source
```powershell
Set-Location -LiteralPath "d:\dev\winget\Valve.Steam\steamapps\common\Stardew Valley\Mods\`[feiluvnana Mods`]"
dotnet build "BetterIndustry/BetterIndustry.csproj"
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI.

