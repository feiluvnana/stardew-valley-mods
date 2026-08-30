# 🏭 BetterIndustry

**BetterIndustry** is a comprehensive economy, artisan goods, milling, cooking, minerals, fruit trees, and forestry rebalance suite for **Stardew Valley 1.6+ (SMAPI 4.0+)**.

---

## 📖 Table of Contents
1. [Module 1: Flower Honey Mead Rebalance](#-module-1-flower-honey-mead-rebalance)
2. [Module 2: Cooking Profit Balancing & Food Star Levels](#-module-2-cooking-profit-balancing--food-star-levels)
3. [Module 3: Quality-Preserving Artisan Machines & Cooking Oil](#-module-3-quality-preserving-artisan-machines--cooking-oil)
4. [Module 4: Truffle Oil Scaling Normalization](#-module-4-truffle-oil-scaling-normalization)
5. [Module 5: Vegetable Juice Buff & Expanded Cask Aging](#-module-5-vegetable-juice-buff--expanded-cask-aging)
6. [Module 6: Fruit Tree Year-1 Positive ROI](#-module-6-fruit-tree-year-1-positive-roi)
7. [Module 7: 41 Geode Minerals & Foraged Minerals Rebalance](#-module-7-41-geode-minerals--foraged-minerals-rebalance)
8. [Module 8: Mid/Late-Game Monster Loot Rebalance](#-module-8-midlate-game-monster-loot-rebalance)
9. [Module 9: Tree Tapper Multi-Harvest Yields](#-module-9-tree-tapper-multi-harvest-yields)
10. [Module 10: Artisanal Milling & Grain Rebalance](#-module-10-artisanal-milling--grain-rebalance)
11. [⚙️ Configuration (GMCM & config.json)](#️-configuration-gmcm--configjson)
12. [🛠️ Building & Installation](#️-building--installation)

---

## 🌸 Module 1: Flower Honey Mead Rebalance
* **Value & Type Retention:** Preserves underlying flower honey type and scales to `FlowerMeadMultiplier` (default **1.35x**):
  $$\text{Mead Price} = \text{Base Flower Honey Price} \times 1.35$$
* **Balanced Values:**
  * *Fairy Rose Honey (680g base)* $\rightarrow$ **918g base / 1,285g Artisan** (brews in 10h).
  * *Poppy Honey (380g base)* $\rightarrow$ **513g base / 718g Artisan**.
  * *Summer Spangle Honey (280g base)* $\rightarrow$ **378g base / 529g Artisan**.
  * *Blue Jazz Honey (200g base)* $\rightarrow$ **270g base / 378g Artisan**.
  * *Tulip Honey (160g base)* $\rightarrow$ **216g base / 302g Artisan**.

---

## 🍳 Module 2: Cooking Profit Balancing & Food Star Levels
* **Profit Margin Guarantee:** Cooked dishes sell for at least their raw ingredient sum multiplied by `CookingProfitMargin` (default **1.25x** / **+25% profit**).
* **4-Tier Quality System:** Ingredients roll Silver, Gold, and Iridium meals with enhanced stat buffs (+2) and doubled durations.

---

## ⭐ Module 3: Quality-Preserving Artisan Machines & Cooking Oil
* **60/25/15 Matrix:** Machines preserve star quality (Normal, Silver, Gold, 0% Iridium) across Kegs, Jars, Cheese Presses, Looms, Dehydrators, and Smokers.
* **🛢️ Cooking Oil Artisan Tag:** Assigns category **`-26` (`Artisan Goods`)** to Cooking Oil (`(O)247`) so it benefits from the **+40% Artisan profession**, scaling through star tiers (140g–280g Artisan).

---

## 🍄 Module 4: Truffle Oil Scaling Normalization
* Scaled from base raw Truffle value ($625\text{g} \times 1.5 = 937\text{g}$ base) without double-dipping:
  * Regular (0⭐): **937g base / 1,311g Artisan**
  * Silver (1⭐): **1,171g base / 1,639g Artisan**
  * Gold (2⭐): **1,405g base / 1,967g Artisan**

---

## 🥕 Module 5: Vegetable Juice Buff & Expanded Cask Aging
* **Juice Multiplier:** Vegetable Juice brews at `JuiceMultiplier` (default **2.75x**).
* **Cask Aging:** Vegetable Juice can age in Cellar Casks up to Iridium quality over 56 days.

---

## 🌳 Module 6: Fruit Tree Year-1 Positive ROI
Rebalances fruit tree fruit prices in `Data/Objects` so every orchard tree achieves a positive return on investment in Year 1:
* **Apricot (`634`)**: **75g base** (2,100g Y1 revenue vs 2,000g sapling)
* **Cherry (`638`)**: **110g base** (3,080g Y1 revenue vs 3,400g sapling)
* **Orange (`635`)**: **135g base** (3,780g Y1 revenue vs 4,000g sapling)
* **Apple (`613`)**: **135g base** (3,780g Y1 revenue vs 4,000g sapling)
* **Peach (`636`)**: **180g base** (5,040g Y1 revenue vs 6,000g sapling)
* **Pomegranate (`637`)**: **180g base** (5,040g Y1 revenue vs 6,000g sapling)
* **Banana (`91`)**: **180g base**
* **Mango (`834`)**: **160g base**

---

## 💎 Module 7: 41 Geode Minerals & Foraged Minerals Rebalance
Rebalances all 41 geode minerals with modest 2-digit profit increases and a 100g minimum floor (+75g profit over Clint's 25g cracking fee), capped well below Diamond (750g):
* **Standard Geode:** Limestone (40g), Mudstone (50g), Sandstone (90g), Calcite (110g), Granite (110g), Nekoite (120g), Orpiment (120g), Slate (125g), Malachite (145g), Thunder Egg (145g), Jagoite (165g), Petrified Slime (140g), Celestine (175g), Alamite (205g), Jamborite (205g).
* **Frozen Geode:** Esperite (145g), Fluorapatite (145g), Marble (160g), Pyrite (170g), Soapstone (170g), Aerinite (175g), Geminite (210g), Hematite (210g), Opal (210g), Ghost Crystal (265g), Lunarite (265g), Ocean Stone (290g), Fairy Stone (325g), Kyanite (325g).
* **Magma Geode:** Baryte (85g), Bixbite (165g), Jasper (210g), Basalt (240g), Lava Teardrop (245g), Lemon Stone (270g), Obsidian (270g), Tigerseye (350g), Dolomite (380g), Fire Opal (435g), Helvite (540g), Star Shards (560g).
* **Foraged Minerals:** Quartz (45g), Earth Crystal (80g), Frozen Tear (110g), Fire Quartz (145g).

---

## ⚔️ Module 8: Mid/Late-Game Monster Loot Rebalance
Strict early-game protection: Bug Meat (8g), Slime (5g), and Bat Wing (15g) are preserved at 100% vanilla.
* **Bone Fragment (`881`)**: **25g** (was 12g)
* **Solar Essence (`768`)**: **75g** (was 40g)
* **Void Essence (`769`)**: **90g** (was 50g)
* **Squid Ink (`814`)**: **175g** (was 110g)

---

## 🌲 Module 9: Tree Tapper Multi-Harvest Yields
* **Standard Tapper (`(BC)105`)**: **35% chance** to yield 2x products on harvest.
* **Heavy Tapper (`(BC)264`)**: **100% guaranteed 2x yield**, with a **20% chance for 3x yield**.

---

## 🌾 Module 10: Artisanal Milling & Grain Rebalance
* **Artisan Category (`-26`):** Wheat Flour (**90g**), Sugar (**50g**), and Rice (**140g**) benefit from the +40% Artisan bonus.
* **Quality Retention:** Preserves crop star quality into milled goods via the 60/25/15 matrix.

---

## ⚙️ Configuration (GMCM & config.json)

| Key | Default | Description |
| :--- | :---: | :--- |
| `EnableCookingBalancing` | `true` | Enables ingredient-based cooking price scaling. |
| `CookingProfitMargin` | `1.25` | Minimum profit multiplier over raw ingredients (+25%). |
| `EnableFoodQuality` | `true` | Enables food quality star ratings from ingredients. |
| `EnableEnhancedFoodBuffs` | `true` | Grants +2 to stats for Iridium meals. |
| `EnableMeadFix` | `true` | Preserves flower honey type when brewing mead. |
| `FlowerMeadMultiplier` | `1.35` | Multiplier for Flower Honey Mead relative to honey (default: 1.35x). |
| `EnableMachineQuality` | `true` | Enables 60/25/15 machine quality matrix (0% Iridium). |
| `EnableTruffleOilFix` | `true` | Truffle Oil scales to 937g base / 1,967g Gold Artisan. |
| `EnableCookingOilArtisanCategory` | `true` | Assigns Artisan category (-26) to Cooking Oil. |
| `EnableJuiceBuff` | `true` | Buffs Vegetable Juice multiplier in Kegs (2.75x). |
| `EnableExpandedAging` | `true` | Allows Vegetable Juice to age in Casks. |
| `EnableFruitTreeRebalance` | `true` | Rebalances orchard fruit prices for positive Year-1 ROI. |
| `EnableAutoFruitDrop` | `true` | Drops ripe fruit overnight when tree reaches threshold. |
| `EnableMineralPriceRebalance` | `true` | Rebalances 41 geode minerals with 2-digit profit bumps. |
| `EnableMonsterLootRebalance` | `true` | Rebalances mid/late dungeon drops (Solar/Void Essence, Squid Ink). |
| `EnableTapperMultiYield` | `true` | Enables 2x/3x multi-harvest yields on Tree Tappers. |
| `StandardTapperDoubleChance` | `0.35` | Chance of 2x yield from Standard Tappers (35%). |
| `HeavyTapperTripleChance` | `0.20` | Chance of 3x yield from Heavy Tappers (20%, 2x is guaranteed). |
| `EnableMillBalancing` | `true` | Rebalances milled goods prices. |
| `EnableMillArtisanCategory` | `true` | Assigns Artisan category (-26) to milled goods. |
| `EnableMillQualityMatrix` | `true` | Enables 60/25/15 quality matrix for the Mill. |
| `SugarBasePrice` | `50` | Base sell price for Sugar (vanilla default 50g). |

---

## 🛠️ Building & Installation
```powershell
Set-Location -LiteralPath "d:\dev\winget\Valve.Steam\steamapps\common\Stardew Valley\Mods\`[feiluvnana Mods`]"
dotnet build "BetterIndustry/BetterIndustry.csproj"
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.

