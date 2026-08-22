# 🛠️ BetterTool (4-Directional Omni-Hopper & Machine Automation)

A powerful automation overhaul for **Hoppers** and artisan machines in **Stardew Valley 1.6+**, built with SMAPI and Harmony.

---

## 🌟 Key Features: 4-Directional Omni-Hopper

A single Hopper can now act as a high-efficiency automation hub for all **4 adjacent cardinal tiles** (`North`, `South`, `West`, `East`):

1. **📥 4-Directional Input / Feeding:**
   * Automatically feeds raw ingredients into up to **4 machines** placed around the Hopper.
2. **📤 4-Directional Output / Auto-Harvest:**
   * Automatically collects finished products from all **4 adjacent machines** as soon as they finish processing.
3. **📦 Smart Output Routing to Adjacent Chests:**
   * If any adjacent tile is a standard **Chest** or **Mini-Shipping Bin**, collected products are automatically routed into it, keeping the Hopper inventory dedicated to raw materials.
4. **🔄 Plus-Shaped Factory Hubs:**
   * Place one Hopper in the center surrounded by 4 Furnaces, Kegs, Preserves Jars, Dehydrators, Fish Smokers, Crab Pots, or Casks to automate them all with zero wasted space!

---

## ⚙️ Configuration Options (Non-Vanilla Feature Toggles)

Every non-vanilla capability can be individually customized in **Generic Mod Config Menu (GMCM)** or `config.json`:

| Category | Option | Default | Vanilla Behavior | BetterTool Behavior |
| :--- | :--- | :---: | :--- | :--- |
| **Automation** | `EnableAutoHarvest` | `true` | Cannot harvest finished products. | Automatically pulls finished goods from all 4 adjacent machines. |
| **Automation** | `EnableChestOutputTransfer` | `true` | Cannot transfer items into chests. | Transfers collected items into any adjacent regular chests or mini-shipping bins. |
| **Automation** | `EnablePeriodicProcessing` | `true` | Only checks when player interacts/closes menu. | Periodically checks and processes machines in the background. |
| **Automation** | `ProcessIntervalTicks` | `60` | N/A | Check frequency in game ticks (60 ticks = 1 real second). |
| **Machines** | `EnableCrabPotService` | `true` | Cannot bait or harvest crab pots. | Automatically baits adjacent crab pots and harvests catches. |
| **Machines** | `EnableCaskService` | `true` | Cannot harvest aged cask products. | Loads aging items and harvests finished aged products from adjacent casks. |
| **Customization** | `HopperCapacity` | `36` | Fixed at 36 slots. | Configurable capacity (36 slots vanilla, or 70 slots expanded). |
| **Customization** | `PlaySoundEffects` | `true` | Silent. | Plays subtle audio feedback on load/harvest/transfer. |

---

## 🚀 Building

Build the mod using .NET SDK:
```powershell
dotnet build BetterTool/BetterTool.csproj
```
