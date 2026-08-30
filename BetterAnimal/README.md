# 🦆 BetterAnimal

**BetterAnimal** is a comprehensive animal husbandry, duck rebalance, and small livestock productivity mod for **Stardew Valley 1.6+ (SMAPI 4.0+)**. It eliminates the frustrating "Duck Feather Penalty", halves rabbit production cooldowns, enables multi-drop yields for high-friendship animals, and allows luxury down cloth weaving in the Loom.

---

## ✨ Features

### 1. 🦆 Duck Dual-Drop & Feather Rebalance
* **🚫 No More "Duck Feather Penalty":** In vanilla, when a duck drops a Feather (250g), the player loses the Duck Egg $\rightarrow$ Duck Mayo (525g–875g) conversion.
* **✨ Dual-Harvest Mechanic:** High-friendship ducks ($\ge 4$ hearts) that roll a Duck Feather will **also drop their standard Duck Egg** (100% guarantee at 5 hearts), turning Duck Feathers into purely bonus profit!
* **🧵 Loom Down Cloth Weaving:** Duck Feathers can now be placed into a Loom (`(BC)17`) to produce **Down Cloth** ($625\text{g}\text{--}875\text{g}$ Artisan).

### 2. 🐇 Rabbit Productivity & Lucky Multi-Drops
* **⚡ Halved Harvest Cooldown:** Reduces Rabbit produce interval from vanilla 4 days down to **2 days** (matching Ducks and Goats), drastically accelerating the return on investment for the 8,000g Deluxe Coop rabbit.
* **🎁 Multi-Drop Yields:** High-friendship rabbits ($\ge 3$ hearts) have a 35% chance to yield bonus items (additional Wool or bonus Lucky Feet).
* **🍀 Rebalanced Rabbit's Foot:** Rebalanced base sell price from 565g to **850g base (1,020g Rancher / 1,700g Iridium)**.

### 3. 🐑 Sheep Shearing Progression
* **✂️ Daily Shearing at 5 Hearts:** Fully happy sheep can be sheared every single day at 5 friendship hearts, making Wool and Cloth competitive with dairy ranching.

---

## ⚙️ Configuration (GMCM & config.json)

All options can be configured in-game via **Generic Mod Config Menu** or in `config.json`:

| Key | Default | Description |
| :--- | :---: | :--- |
| `EnableDuckDualDrop` | `true` | Ducks drop an egg alongside feathers at high friendship. |
| `DuckDualDropMinHearts` | `4` | Minimum hearts for duck dual-drop (1–5). |
| `DuckDualDropChance` | `1.00` | Chance of duck dual-drop at 5 hearts (100% guarantee). |
| `EnableDuckFeatherLoom` | `true` | Allows spinning Duck Feathers into Down Cloth in Looms. |
| `EnableRabbitCooldownReduction` | `true` | Reduces rabbit production cooldown. |
| `RabbitDaysToProduce` | `2` | Days between rabbit harvests (default: 2 days). |
| `EnableRabbitMultiDrop` | `true` | Enables bonus wool / foot drops for happy rabbits. |
| `RabbitMultiDropChance` | `0.35` | Probability of bonus drop at 3+ hearts (default: 35%). |
| `EnableRabbitFootRebalance` | `true` | Enables rebalanced sell price for Rabbit's Foot. |
| `RabbitFootBasePrice` | `850` | Base sell price for Rabbit's Foot (850g base / 1,700g Iridium). |
| `EnableSheepDailyShearAtMaxHearts` | `true` | Enables daily shearing for 5-heart sheep. |

---

## 🛠️ Building & Installation

### Requirements
* **Stardew Valley 1.6+**
* **SMAPI 4.0+**
* *(Optional)* **Generic Mod Config Menu**

### Building from Source
```powershell
Set-Location -LiteralPath "d:\dev\winget\Valve.Steam\steamapps\common\Stardew Valley\Mods\`[feiluvnana Mods`]"
dotnet build "BetterAnimal/BetterAnimal.csproj"
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
