# 📦 BetterQOL (Stardew Valley 1.6+)

A comprehensive quality-of-life suite for **Stardew Valley 1.6+**, combining unstackable item stack size overrides with fast and free geode/mystery box processing. Built with SMAPI and Harmony.

---

## ✨ Key Features

### 💎 1. Blacksmith & Geode Processing Overhaul
* **Free 0g Cracking:** Cracking geodes, mystery boxes, artifact troves, and golden coconuts at Clint's can be made completely free (0g).
* **⚡ Instant Cracking:** Skip the 2.7-second delay to crack open items immediately.
* **🔨 "Crack All" & Shift+Click Bulk Processing:** Crack entire stacks at once either by clicking the dedicated "Crack All" button or holding Shift while clicking the anvil.
* **🚜 Expanded Geode Crusher:** Crush Mystery Boxes, Golden Mystery Boxes, Artifact Troves, and Golden Coconuts directly in on-farm Geode Crushers with optional instant processing and no-coal mode.

### 🎒 2. Extended Stack Size Overhaul
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
| **Blacksmith** | `FreeCracking` | `true` | Makes cracking completely free at Clint's (0g). |
| **Blacksmith** | `CrackingPrice` | `0` | Gold fee per geode when free cracking is disabled. |
| **Blacksmith** | `InstantCracking` | `false` | Skips single geode cracking animations. |
| **Blacksmith** | `ShowCrackAllButton` | `true` | Displays the 'Crack All' button in Clint's menu. |
| **Blacksmith** | `BulkBatchSize` | `999` | Max geodes cracked per bulk batch. |
| **Blacksmith** | `ShowSummaryToast` | `true` | Displays HUD notification after bulk cracking. |
| **Machines** | `AllowSpecialGeodesInCrusher` | `true` | Allows mystery boxes/troves in Geode Crushers. |
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
dotnet build BetterQOL/BetterQOL.csproj
```

---

## 📄 License & Credits
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
