# 🛠️ BetterTool (Better Hopper & Machine Automation)

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

## ⚙️ Configuration Options

| Option | Default | Description |
| :--- | :---: | :--- |
| `EnableAutoHarvest` | `true` | Automatically collects finished products from machines above the hopper. |
| `EnableAdjacentChestOutput` | `true` | Transfers items downward into a chest or mini-shipping bin placed directly below the hopper. |
| `ProcessIntervalTicks` | `60` | Check frequency in game ticks (60 ticks = ~1 real second). |
| `PlaySoundEffects` | `true` | Plays subtle sound effects when items are loaded or collected. |
| `HopperCapacity` | `36` | Hopper storage capacity (`36` or `70` slots). |
| `ServiceCrabPots` | `true` | Enables auto-baiting (below) and harvesting (above) of crab pots. |
| `ServiceCasks` | `true` | Enables auto-loading (below) and harvesting (above) for cellar casks. |

---

## 🚀 Building

Build the mod using .NET SDK:
```powershell
dotnet build BetterTool/BetterTool.csproj
```
