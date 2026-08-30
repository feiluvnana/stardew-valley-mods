# 📦 BetterQOL

**BetterQOL** is a comprehensive quality-of-life suite for **Stardew Valley 1.6+**, combining UI Info Suite 2 style real-time hover tooltips, Lookup Anything (`F1`), real-time crop grow times, machine processing timers, unstackable item stack size overrides (up to 999), and fast geode/mystery box processing.

---

## 📖 Table of Contents
1. [Module 1: Real-Time Hover Tooltips & Timers](#-module-1-real-time-hover-tooltips--timers)
2. [Module 2: Exact Experience Display & Skills Panel](#-module-2-exact-experience-display--skills-panel)
3. [Module 3: Lookup Anything (F1 Hotkey)](#-module-3-lookup-anything-f1-hotkey)
4. [Module 4: Blacksmith & Geode Processing Overhaul](#-module-4-blacksmith--geode-processing-overhaul)
5. [Module 5: Extended Stack Size Overhaul](#-module-5-extended-stack-size-overhaul)
6. [Module 6: Dynamic Object & Terrain Transparency](#-module-6-dynamic-object--terrain-transparency)
7. [⚙️ Configuration (GMCM & config.json)](#️-configuration-gmcm--configjson)
8. [🛠️ Building & Installation](#️-building--installation)

---

## 🔍 Module 1: Real-Time Hover Tooltips & Timers

Hover over any interactive tile, entity, or inventory item to inspect live data in a compact, native-styled tooltip overlay:

* **🌱 Crops & Soil:** Displays produce name, sprite icon, growth stage (`Stage 3/5`), days remaining until harvest (`Ready in 3 days` / `★ Ready to Harvest!`), regrow cycle info (`Regrows every 4 days`), watering status (`Watered ✓` / `Unwatered ✗`), and fertilizer applied. Supports Garden Pots and Paddy Crops.
* **⚙️ Processing Machines:** Displays output product name and icon, remaining processing time (`2h 30m` / `1d 4h`), and exact clock completion time (`Today at 4:20 PM` / `Tomorrow at 9:00 AM`). Highlights finished machines with `★ Ready to Collect!`. *(Non-machine debris like tree branches and stones are strictly filtered out).*
* **🍷 Casks:** Displays aging stage, days to next star quality (`Aging: Gold in 12 days`), and total days to Iridium quality.
* **🦀 Crab Pots:** Displays bait type, overnight readiness, and caught items.
* **🍎 Fruit Trees:** Displays maturation countdown (`Maturing in 12 days`), ready fruit count (`3/3 fruits`), fruit quality, and seasonal production status.
* **🌲 Wild Trees & Bushes:** Displays growth stage (`Stage 4/5`), moss availability (`Has Moss ✓`), tapper status, and Tea Bush harvest windows.
* **🐄 Farm Animals & Pets:** Displays daily petting status (`Petted today ✓` / `Needs petting ✗`), friendship hearts (`5.0 / 5.0 ♥`), happiness, and ready produce.
* **🎒 Inventory & Menu Items:** Displays individual and stack sell prices, clean wrapped/bulleted **Community Center bundle requirements**, and **Museum donation status**.

---

## 📊 Module 2: Exact Experience Display & Skills Panel

* **📈 On-Panel Direct Display:** Displays exact XP numbers (`5,420 / 6,900 XP` or `15,000 XP (Max)`) directly on the Skills tab menu next to each skill bar.
* **🔎 Skills Tab Hover Tooltips:** Hovering over any of the 5 skill rows (Farming, Fishing, Foraging, Mining, Combat) in the Game Menu's Skills tab displays exact current XP (`Current XP: 5,420`), next level target (`Next Level: 6,900`), remaining XP needed (`1,480 XP needed`), and level progress percentage (`60.5%`).
* **🌟 1.6 Mastery Bar Tracking:** Tracks exact Mastery experience and next goal.
* **📖 F1 Almanac & Farmer Card:** The World Overview Almanac and Farmer Lookup cards display the complete breakdown of exact XP and levels for all 5 skills.

---

## 🔎 Module 3: Lookup Anything (`F1` Hotkey)

Press **F1** anywhere in the game world or inside menus and inventories to inspect rich, real-time data:

* **👤 Villagers & NPCs:** Displays high-res portrait, birthday, talked-today status, weekly gifts given, relationship hearts with exact point progress, and complete lists of **Loved (★★★★★)** and **Liked (★★★)** gifts.
* **🎒 Items & Inventory:** Displays sell value, health/energy restoration, **Museum donation status** (donated or needed), **Community Center bundle requirements**, who loves/likes the item, and **Crafting/Cooking recipes** using the item.
* **⚔️ Monsters:** Displays combat stats (HP bar, attack damage, defense, XP gained) and full possible loot drop tables.
* **🐄 Farm Animals & Pets:** Displays friendship hearts, happiness rating (0–255), mood reason (e.g. hungry, left outside), petting status, and ready produce.
* **🐟 Fish Ponds & Buildings:** Displays fish population, max capacity, next spawn countdown, output chances, and needed quest items.

---

## 💎 Module 3: Blacksmith & Geode Processing Overhaul

* **⚡ Instant Cracking:** Skips the 2.7-second delay to crack open items immediately.
* **🔨 "Crack All" & Shift+Click Bulk Processing:** Crack entire stacks at once either by clicking the dedicated "Crack All" button or holding Shift while clicking the anvil at the standard 25g cost.
* **🚜 Geode Crusher Improvements:** On-farm Geode Crushers with optional instant processing and no-coal mode.

---

## 🎒 Module 4: Extended Stack Size Overhaul

Remove vanilla inventory stack limits and stack previously unstackable items up to **999** (or custom configured size):

* **🎣 Fishing Tackle & Bobbers:** Stacks tackles that share identical durability.
* **🔮 1.6 Trinkets:** Stacks identical trinkets (preserving stats and ascension status).
* **💍 Rings & Combined Rings:** Stacks standard and combined rings with identical effects.
* **🪑 Furniture & Decor:** Stacks rugs, lamps, windows, chairs, and beds.
* **👗 Clothing & Hats:** Stacks identical wearable clothes and hats.
* **👢 Boots:** Stacks identical boots with matching defense and immunity stats.

---

## 🌫️ Module 5: Dynamic Object & Terrain Transparency

Customizable, smooth-fading transparency for obstruction-heavy entities whenever the player walks near or behind them:

* **🏠 Farm Buildings:** Custom fade distance and minimum opacity when walking behind barns, coops, sheds, etc.
* **🌿 Interactive Bushes:** Fades tea bushes, berry bushes, and town bushes so the player and ground items remain clearly visible.
* **🌲 Trees & Fruit Trees:** Fades wild tree canopies and mature fruit tree canopies when behind or under them.
* **🌾 Tall Grass:** Dynamically makes tall grass transparent around the player to easily spot dropped items or forage.
* **🌱 Crops & Pots:** Optional transparency for tall crops (e.g. corn, hops, trellis crops) in outdoor dirt and garden pots.
* **🍄 Forage & Small Objects:** Optional transparency for small stones, weeds, twigs, and forage items.
* **📦 Big Craftables:** Optional transparency for machines, scarecrows, statues, and chests.
* **⌨️ Hotkey Toggles:** Bindable keys to instantly toggle full transparency or disable transparency to restore vanilla rendering.

---

## ⚙️ Configuration (GMCM & config.json)

```json
{
  "EnableCropHover": true,
  "EnableMachineHover": true,
  "EnableTreeHover": true,
  "EnableAnimalHover": true,
  "ShowWaterAndFertilizer": true,
  "ShowItemIconInTooltip": true,
  "ShowExactFinishTime": true,
  "ShowItemSellPriceOnHover": true,
  "ShowBundleNeedOnHover": true,
  "ShowMuseumNeedOnHover": true,
  "HoverHotkey": "None",
  "EnableLookupAnything": true,
  "LookupKey": "F1",
  "ShowGiftTastes": true,
  "ShowItemRecipes": true,
  "ShowBundleAndMuseumInfo": true,
  "InstantCracking": false,
  "ShowCrackAllButton": true,
  "BulkBatchSize": 999,
  "ShowSummaryToast": true,
  "InstantGeodeCrusher": false,
  "GeodeCrusherRequiresCoal": true,
  "MaxStackSize": 999,
  "EnableTackleStacking": true,
  "EnableTrinketStacking": true,
  "EnableFurnitureStacking": true,
  "EnableRingStacking": true,
  "EnableClothingAndHatStacking": true,
  "EnableBootsStacking": true,
  "EnableTransparency": true,
  "EnableBuildingTransparency": true,
  "BuildingBelowPlayerOnly": true,
  "BuildingTileDistance": 3,
  "BuildingMinimumOpacity": 0.4,
  "EnableBushTransparency": true,
  "BushBelowPlayerOnly": true,
  "BushTileDistance": 5,
  "BushMinimumOpacity": 0.4,
  "EnableTreeTransparency": true,
  "TreeBelowPlayerOnly": true,
  "TreeTileDistance": 5,
  "TreeMinimumOpacity": 0.1,
  "EnableGrassTransparency": true,
  "GrassBelowPlayerOnly": false,
  "GrassTileDistance": 3,
  "GrassMinimumOpacity": 0.3,
  "EnableCropTransparency": false,
  "CropBelowPlayerOnly": false,
  "CropTileDistance": 3,
  "CropMinimumOpacity": 0.4,
  "EnableObjectTransparency": false,
  "ObjectBelowPlayerOnly": true,
  "ObjectTileDistance": 3,
  "ObjectMinimumOpacity": 0.4,
  "EnableCraftableTransparency": false,
  "CraftableBelowPlayerOnly": true,
  "CraftableTileDistance": 3,
  "CraftableMinimumOpacity": 0.4,
  "DisableTransparencyKey": "",
  "FullTransparencyKey": ""
}
```

| Category | Setting | Default | Description |
| :--- | :--- | :---: | :--- |
| **Hover Info** | `EnableCropHover` | `true` | Show crop grow time, days to harvest, and soil info. |
| **Hover Info** | `EnableMachineHover` | `true` | Show machine processing countdown and finish times. |
| **Hover Info** | `EnableTreeHover` | `true` | Show fruit tree maturation and wild tree stages. |
| **Hover Info** | `EnableAnimalHover` | `true` | Show animal petting status, hearts, and produce. |
| **Hover Info** | `ShowWaterAndFertilizer` | `true` | Display soil watering and fertilizer details. |
| **Hover Info** | `ShowItemIconInTooltip` | `true` | Display item sprite previews in hover tooltips. |
| **Hover Info** | `ShowExactFinishTime` | `true` | Display estimated clock finish time (e.g. 4:20 PM). |
| **Hover Info** | `ShowItemSellPriceOnHover` | `true` | Display sell prices when hovering over items in menus. |
| **Hover Info** | `ShowBundleNeedOnHover` | `true` | Display Community Center bundle needs on menu items. |
| **Hover Info** | `ShowMuseumNeedOnHover` | `true` | Display Museum donation status on menu items. |
| **Hover Info** | `HoverHotkey` | `None` | Key to hold to show hover tooltips (`None` = always). |
| **Lookup** | `EnableLookupAnything` | `true` | Enable F1 lookup cards. |
| **Lookup** | `LookupKey` | `F1` | Hotkey to trigger lookup card. |
| **Lookup** | `ShowGiftTastes` | `true` | Display loved/liked gifts in lookup cards. |
| **Lookup** | `ShowItemRecipes` | `true` | Display crafting/cooking recipes in lookup cards. |
| **Lookup** | `ShowBundleAndMuseumInfo` | `true` | Display bundle and museum status on items. |
| **Blacksmith** | `InstantCracking` | `false` | Skips single geode cracking animations. |
| **Blacksmith** | `ShowCrackAllButton` | `true` | Displays the 'Crack All' button in Clint's menu. |
| **Blacksmith** | `BulkBatchSize` | `999` | Max geodes cracked per bulk batch. |
| **Blacksmith** | `ShowSummaryToast` | `true` | Displays HUD notification after bulk cracking. |
| **Machines** | `InstantGeodeCrusher` | `false` | Geode Crushers process instantly (0 minutes). |
| **Machines** | `GeodeCrusherRequiresCoal` | `true` | Whether Geode Crushers require coal. |
| **Stacking** | `MaxStackSize` | `999` | Stack size limit for stackable items. |
| **Stacking** | `EnableTackleStacking` | `true` | Allows matching tackles to stack. |
| **Stacking** | `EnableTrinketStacking` | `true` | Allows identical trinkets to stack. |
| **Stacking** | `EnableFurnitureStacking` | `true` | Allows furniture to stack. |
| **Stacking** | `EnableRingStacking` | `true` | Allows identical rings to stack. |
| **Stacking** | `EnableClothingAndHatStacking` | `true` | Allows matching clothes and hats to stack. |
| **Stacking** | `EnableBootsStacking` | `true` | Allows identical boots to stack. |
| **Transparency** | `EnableTransparency` | `true` | Master toggle for all custom transparency features. |
| **Transparency** | `EnableBuildingTransparency` | `true` | Custom transparency for farm buildings. |
| **Transparency** | `BuildingTileDistance` | `3` | Tile distance for building transparency. |
| **Transparency** | `BuildingMinimumOpacity` | `0.4` | Minimum opacity for transparent buildings (0.0–1.0). |
| **Transparency** | `EnableBushTransparency` | `true` | Custom transparency for bushes. |
| **Transparency** | `BushTileDistance` | `5` | Tile distance for bush transparency. |
| **Transparency** | `BushMinimumOpacity` | `0.4` | Minimum opacity for transparent bushes (0.0–1.0). |
| **Transparency** | `EnableTreeTransparency` | `true` | Custom transparency for wild trees & fruit trees. |
| **Transparency** | `TreeTileDistance` | `5` | Tile distance for tree canopy transparency. |
| **Transparency** | `TreeMinimumOpacity` | `0.1` | Minimum opacity for transparent tree canopies (0.0–1.0). |
| **Transparency** | `EnableGrassTransparency` | `true` | Custom transparency for tall grass. |
| **Transparency** | `GrassTileDistance` | `3` | Tile distance for grass transparency. |
| **Transparency** | `GrassMinimumOpacity` | `0.3` | Minimum opacity for transparent grass (0.0–1.0). |
| **Transparency** | `EnableCropTransparency` | `false` | Custom transparency for outdoor and pot crops. |
| **Transparency** | `EnableObjectTransparency` | `false` | Custom transparency for forage, stones, twigs, and weeds. |
| **Transparency** | `EnableCraftableTransparency` | `false` | Custom transparency for machines, scarecrows, and chests. |
| **Transparency** | `DisableTransparencyKey` | `""` | Keybind to toggle disable custom transparency. |
| **Transparency** | `FullTransparencyKey` | `""` | Keybind to toggle full maximum transparency. |

---

## 🛠️ Building & Installation

### Requirements
* **Stardew Valley 1.6+**
* **SMAPI 4.0+**
* *(Optional)* **Generic Mod Config Menu**

### Building from Source
```powershell
dotnet build BetterQOL.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
