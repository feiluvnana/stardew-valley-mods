# 🛠️ BetterTool (Directional Hopper & Machine Automation)

A balanced, directional automation overhaul for **Hoppers** and artisan machines in **Stardew Valley 1.6+**, built with SMAPI and Harmony.

---

## 🌟 Key Mechanics: Downward Gravity Flow

BetterTool follows a clean, intuitive, physical top-to-bottom pipeline flow:

1. **📥 Input / Feed (Top-to-Bottom):**
   * A Hopper placed **above** a machine (`Y - 1`) pushes raw ingredients downward into the machine (`Y`).
2. **📤 Output / Auto-Harvest (Top-to-Bottom):**
   * A Hopper placed **below** a machine (`Y + 1`) pulls finished products downward from the machine (`Y`).
3. **📦 Output Chest / Shipping Bin Transfer (Top-to-Bottom):**
   * A Hopper placed **above** a regular Chest or Mini-Shipping Bin (`Y + 1`) automatically passes collected products downward into the container.
4. **🔄 Multi-Stage Factory Pipelines (Chainable Hoppers):**
   * An intermediate Hopper between two machines simultaneously harvests from the machine above and feeds into the machine below, enabling multi-stage production lines (e.g. Unmilled Rice ➔ Rice ➔ Vinegar ➔ Chest).

---

## ⚙️ Configuration Options (Non-Vanilla Feature Toggles)

Every non-vanilla capability can be individually enabled, customized, or turned off via **Generic Mod Config Menu (GMCM)** or `config.json`:

| Category | Option | Default | Vanilla Behavior | BetterTool Behavior |
| :--- | :--- | :---: | :--- | :--- |
| **Automation** | `EnableAutoHarvest` | `true` | Cannot harvest from machines above. | Automatically pulls finished goods from machines directly above. |
| **Automation** | `EnableChestOutputTransfer` | `true` | Cannot transfer items into chests. | Transfers collected items into chests/shipping bins directly below. |
| **Automation** | `EnablePeriodicProcessing` | `true` | Only checks when player interacts/closes menu. | Periodically checks and processes machines in the background. |
| **Automation** | `ProcessIntervalTicks` | `60` | N/A | Check frequency in game ticks (60 ticks = 1 real second). |
| **Machines** | `EnableCrabPotService` | `true` | Cannot bait or harvest crab pots. | Automatically baits crab pots below and harvests catches from above. |
| **Machines** | `EnableCaskService` | `true` | Cannot harvest aged cask products. | Loads aging items below and harvests finished aged products from above. |
| **Customization** | `HopperCapacity` | `36` | Fixed at 36 slots. | Configurable capacity (36 slots vanilla, or 70 slots expanded). |
| **Customization** | `PlaySoundEffects` | `true` | Silent. | Plays subtle audio feedback on load/harvest/transfer. |

---

## 🚀 Building

Build the mod using .NET SDK:
```powershell
dotnet build BetterTool/BetterTool.csproj
```
