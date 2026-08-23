# 🏭 BetterIndustry

**BetterIndustry** is a high-efficiency artisan goods and cooking rebalance suite for **Stardew Valley 1.6+**, featuring flower mead value retention and profitable cooking price scaling.

---

## 📖 Table of Contents
1. [Module 1: Flower Honey Mead Fix](#-module-1-flower-honey-mead-fix)
2. [Module 2: Cooking Profit Balancing](#-module-2-cooking-profit-balancing)
3. [⚙️ Configuration (GMCM & config.json)](#️-configuration-gmcm--configjson)
4. [🛠️ Building & Installation](#️-building--installation)

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

## ⚙️ Configuration (GMCM & config.json)

```json
{
  "EnableCookingBalancing": true,
  "CookingProfitMargin": 1.25,
  "EnableMeadFix": true
}
```

| Category | Setting | Default | Description |
| :--- | :--- | :---: | :--- |
| **Cooking** | `EnableCookingBalancing` | `true` | Enables ingredient-based cooking price scaling. |
| **Cooking** | `CookingProfitMargin` | `1.25` | Minimum profit multiplier over raw ingredients (1.25 = +25%). |
| **Artisan** | `EnableMeadFix` | `true` | Retains flower honey flavor and 2.0x value scaling when brewed into mead. |

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
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI.
