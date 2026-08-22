# 📦 BetterQOL (Stardew Valley 1.6+)

A comprehensive quality-of-life suite for **Stardew Valley 1.6+**, combining real-time crop grow times, machine processing timers, tree maturation, unstackable item stack size overrides, and fast geode/mystery box processing. Built with SMAPI and Harmony.

---

## ✨ Key Features

### 🔍 1. Real-Time Hover Tooltips & Timers
Hover over any interactive tile or farm entity to inspect real-time progress:
* **🌱 Crops & Soil:** Displays produce name, item sprite icon, growth phase (`Stage 3/5`), days remaining until harvest (`Ready in 3 days` / `★ Ready to Harvest!`), regrow cycle info (`Regrows every 4 days`), watering status (`Watered ✓` / `Unwatered ✗`), and fertilizer applied. Supports Garden Pots and Paddy Crops.
* **⚙️ Processing Machines:** Displays machine output item name/icon, remaining processing time (`2h 30m` / `1d 4h`), and exact completion clock time (`Today at 4:20 PM` / `Tomorrow at 9:00 AM`). Highlights finished machines with `★ Ready to Collect!`.
* **🍷 Casks:** Displays current quality star level, days to next star quality (`Aging: Gold in 12 days`), and total days to Iridium quality.
* **🦀 Crab Pots:** Displays bait type, overnight status, and caught harvest.
* **🍎 Fruit Trees:** Displays maturation countdown (`Maturing in 12 days`), ready fruit count (`3/3 fruits`), fruit quality star level, and seasonal production status.
* **🌲 Wild Trees & Bushes:** Displays growth stage (`Stage 4/5`), moss availability (`Has Moss ✓`), tapper status, and Tea Bush maturation/harvest windows.
* **🐄 Farm Animals & Pets:** Displays daily petting status (`Petted today ✓` / `Needs petting ✗`), friendship hearts (`5.0 / 5.0 ♥`), and produce readiness.

### 💎 2. Blacksmith & Geode Processing Overhaul
* **⚡ Instant Cracking:** Skip the 2.7-second delay to crack open items immediately.
* **🔨 "Crack All" & Shift+Click Bulk Processing:** Crack entire stacks at once either by clicking the dedicated "Crack All" button or holding Shift while clicking the anvil at standard 25g cost.
* **🚜 Geode Crusher Improvements:** On-farm Geode Crushers with optional instant processing and no-coal mode.

### 🎒 3. Extended Stack Size Overhaul
Remove vanilla inventory stack limits and stack previously unstackable items up to **999** (or custom configured size):
* **🎣 Fishing Tackle & Bobbers:** Stacks tackles that share identical durability.
* **🔮 1.6 Trinkets:** Stacks identical trinkets (preserving stats and ascension status).
* **💍 Rings & Combined Rings:** Stacks standard and combined rings with identical effects.
* **🪑 Furniture & Decor:** Stacks rugs, lamps, windows, chairs, and beds.
* **👗 Clothing & Hats:** Stacks identical wearable clothes and hats.
* **👢 Boots:** Stacks identical boots with matching defense and immunity stats.

---

## ⚙️ Configuration (Generic Mod Config Menu)

All options can be configured in-game via **Generic Mod Config Menu (GMCM)** or through `config.json`:

| Category | Option | Default | Description |
| :--- | :--- | :---: | :--- |
| **Hover Info** | `EnableCropHover` | `true` | Show crop grow time, days to harvest, and soil info. |
| **Hover Info** | `EnableMachineHover` | `true` | Show machine processing countdown and finish times. |
| **Hover Info** | `EnableTreeHover` | `true` | Show fruit tree maturation and wild tree stages. |
| **Hover Info** | `EnableAnimalHover` | `true` | Show animal petting status, hearts, and produce. |
| **Hover Info** | `ShowWaterAndFertilizer` | `true` | Display soil watering and fertilizer details. |
| **Hover Info** | `ShowItemIconInTooltip` | `true` | Display item sprite previews in hover tooltips. |
| **Hover Info** | `ShowExactFinishTime` | `true` | Display estimated clock finish time (e.g. 4:20 PM). |
| **Hover Info** | `HoverHotkey` | `None` | Key to hold to show hover tooltips (`None` = always). |
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

---

## 🚀 Building

```powershell
dotnet build "BetterQOL\BetterQOL.csproj"
```

---

## 📄 License & Credits
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
