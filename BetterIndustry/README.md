# 🏭 BetterIndustry

**BetterIndustry** is a high-efficiency artisan goods and cooking rebalance suite for **Stardew Valley 1.6+**, eliminating the most frustrating pain points of the artisan and culinary systems.

---

## 📖 Table of Contents
1. [Module 1: Flower Honey Mead Fix](#-module-1-flower-honey-mead-fix)
2. [Module 2: Cooking Profit Balancing & Food Star Levels](#-module-2-cooking-profit-balancing--food-star-levels)
3. [Module 3: Quality-Preserving Machines](#-module-3-quality-preserving-machines)
4. [Module 4: Truffle Oil Scaling Fix](#-module-4-truffle-oil-scaling-fix)
5. [Module 5: Vegetable Juice Buff](#-module-5-vegetable-juice-buff)
6. [Module 6: Expanded Cask Aging](#-module-6-expanded-cask-aging)
7. [Module 7: Fruit Tree Automation](#-module-7-fruit-tree-automation)
8. [⚙️ Configuration (GMCM & config.json)](#️-configuration-gmcm--configjson)
9. [🛠️ Building & Installation](#️-building--installation)

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

## 🍳 Module 2: Cooking Profit Balancing & Food Star Levels

In vanilla, cooking always creates normal (0-star) dishes regardless of whether top-tier Iridium ingredients were used, and many recipes sell for less than their raw ingredients.

### 1. Dynamic Profit Margin Guarantee
* Ensures every cooked meal sells for at least its raw ingredient sum multiplied by `CookingProfitMargin` (default **1.25x** / **+25% profit**):
  $$\text{Cooked Meal Price} = \max\left(\text{Vanilla Price},\; \sum \text{Raw Ingredient Values} \times \text{CookingProfitMargin}\right)$$

### 2. Ingredient Quality Inheritance & Star Levels
* Dynamically calculates the cooked meal's star rating from the average quality of consumed produce ingredients:
  $$Q_{avg} = \frac{\sum (\text{Quality}_i \times \text{Count}_i)}{\text{Quality-Eligible Ingredients Count}}$$
* **Smart Produce Protection:** Non-quality store-bought staples (Wheat Flour, Sugar, Oil, Vinegar, Rice) are automatically filtered out from the denominator so they **never dilute** the star rating of your farm's crops, fish, and artisan ingredients!
  * *Example:* 1 Iridium Tomato (4) + 1 Wheat Flour (staple) $\rightarrow$ **Iridium Spaghetti (4)**!
  * *Example:* 1 Iridium Egg (4) + 1 Sugar (staple) + 1 Flour (staple) $\rightarrow$ **Iridium Chocolate Cake (4)**!

| Average Ingredient Quality ($Q_{avg}$) | Dish Quality | Sell Value | Energy & Health Scaling | Active Buff Bonus | Duration Multiplier |
| :---: | :---: | :---: | :---: | :---: | :---: |
| $Q_{avg} < 0.75$ | **Regular (0)** | $1.00\times$ | $1.0\times$ ($2.5\times$ Edibility) | Base | $1.0\times$ |
| $0.75 \le Q_{avg} < 2.0$ | **Silver (⭐)** | $1.25\times$ | $1.4\times$ ($3.5\times$ Edibility) | $+1$ to active stats | $1.5\times$ |
| $2.0 \le Q_{avg} < 3.5$ | **Gold (⭐⭐)** | $1.50\times$ | $1.8\times$ ($4.5\times$ Edibility) | $+1$ to active stats | $1.5\times$ |
| $Q_{avg} \ge 3.5$ | **Iridium (⭐⭐⭐⭐)** | $2.00\times$ | $2.6\times$ ($6.5\times$ Edibility) | $+2$ to active stats | $2.0\times$ |

### 3. Enhanced Qi Seasoning Synergy
* **Regular (0) / Silver (1) $\rightarrow$ Gold (2)** (Vanilla standard guaranteed)
* **Gold (2) $\rightarrow$ Iridium (4)** (Allows players to reach master-tier Iridium dishes when seasoning Gold-quality meals!)
* **Iridium (4) $\rightarrow$ Iridium (4)**

### 4. Smart Ingredient Priority
* Configure `IngredientQualityPriority` in GMCM to automatically select:
  * `HighestQualityFirst` (Default): Uses your highest-quality crops to create premium food.
  * `LowestQualityFirst`: Preserves top-quality crops and consumes lower-tier ingredients first.
  * `InventoryOrder`: Follows vanilla bottom-up inventory order.

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

## 🌳 Module 7: Fruit Tree Automation

Mature fruit trees will automatically drop their ripe fruit onto the ground overnight once they reach the configured threshold (default 3 fruit), streamlining daily farm harvesting.

---

## ⚙️ Configuration (GMCM & config.json)

```json
{
  "EnableCookingBalancing": true,
  "CookingProfitMargin": 1.25,
  "EnableFoodQuality": true,
  "IngredientQualityPriority": "HighestQuality",
  "EnableEnhancedFoodBuffs": true,
  "IridiumBuffDurationMultiplier": 2.0,
  "EnableMeadFix": true,
  "EnableQualityPreserving": true,
  "EnableTruffleOilFix": true,
  "TruffleOilMultiplier": 1.5,
  "EnableJuiceBuff": true,
  "JuiceMultiplier": 2.75,
  "EnableExpandedAging": true,
  "EnableAutoFruitDrop": true,
  "MaxFruitsBeforeDrop": 3
}
```

| Category | Setting | Default | Range / Step | Description |
| :--- | :--- | :---: | :---: | :--- |
| **Cooking** | `EnableCookingBalancing` | `true` | bool | Enables ingredient-based cooking price scaling. |
| **Cooking** | `CookingProfitMargin` | `1.25` | `1.0` – `5.0` (`0.05`) | Minimum profit multiplier over raw ingredients (+25%). |
| **Cooking** | `EnableFoodQuality` | `true` | bool | Enables food quality calculation (Silver, Gold, Iridium) and enhanced Qi Seasoning. |
| **Cooking** | `IngredientQualityPriority` | `"HighestQuality"` | Dropdown | Ingredient selection order (`HighestQuality`, `LowestQuality`, `InventoryOrder`). |
| **Cooking** | `EnableEnhancedFoodBuffs` | `true` | bool | Grants +2 to active stat buffs for Iridium dishes and scales buff duration. |
| **Cooking** | `IridiumBuffDurationMultiplier` | `2.0` | `1.0` – `3.0` (`0.05`) | Buff duration multiplier for Iridium-quality meals. |
| **Artisan** | `EnableMeadFix` | `true` | bool | Retains flower honey flavor and 2.0x value scaling when brewed into mead. |
| **Artisan** | `EnableQualityPreserving` | `true` | bool | Preserves input star quality across all artisan machines. |
| **Artisan** | `EnableTruffleOilFix` | `true` | bool | Truffle Oil scales value and quality based on the input Truffle. |
| **Artisan** | `TruffleOilMultiplier` | `1.5` | `1.0` – `3.0` (`0.05`) | Multiplier for Truffle Oil relative to input truffle price. |
| **Artisan** | `EnableJuiceBuff` | `true` | bool | Buffs the price multiplier for Vegetable Juice brewed in Kegs. |
| **Artisan** | `JuiceMultiplier` | `2.75` | `1.0` – `5.0` (`0.05`) | Multiplier for Vegetable Juice relative to raw vegetable price. |
| **Artisan** | `EnableExpandedAging` | `true` | bool | Allows Casks to age additional artisan goods (Vegetable Juice). |
| **Fruit Tree** | `EnableAutoFruitDrop` | `true` | bool | Auto-drops fruit overnight when tree reaches threshold. |
| **Fruit Tree** | `MaxFruitsBeforeDrop` | `3` | `1` – `10` (`1`) | Number of fruit on tree that triggers auto-drop. |

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

