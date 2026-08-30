# 🦆 BetterAnimal

**BetterAnimal** is a comprehensive animal husbandry and small livestock productivity mod for **Stardew Valley 1.6+ (SMAPI 4.0+)**. It eliminates the frustrating "Duck Feather Penalty", halves rabbit production cooldowns, accelerates dinosaur egg cycles to 3 days, enables multi-drop yields for high-friendship animals (Ducks, Rabbits, Goats, Dinosaurs, Void Chickens), and enhances Slime Hutch ranching capacity.

---

## ✨ Features

### 1. 🦆 Duck Dual-Drop & Feather Rebalance
* **🚫 No More "Duck Feather Penalty":** In vanilla, when a duck drops a Feather (250g), the player loses the Duck Egg $\rightarrow$ Duck Mayo (525g–875g) conversion.
* **✨ Dual-Harvest Mechanic:** High-friendship ducks ($\ge 4$ hearts) that roll a Duck Feather will **also drop their standard Duck Egg** (100% guarantee at 5 hearts), turning Duck Feathers into purely bonus profit!
* **🧵 Loom Down Cloth Weaving:** Duck Feathers can now be placed into a Loom (`(BC)17`) to produce **Down Cloth** (375g–750g base / 525g–1,050g Artisan).

### 2. 🐇 Rabbit Productivity & Lucky Multi-Drops
* **⚡ Halved Harvest Cooldown:** Reduces Rabbit produce interval from vanilla 4 days down to **2 days** (matching Ducks and Goats), accelerating return on investment.
* **🎁 Multi-Drop Yields:** High-friendship rabbits ($\ge 3$ hearts) have a 35% chance to yield bonus items (additional Wool or bonus Lucky Feet).

### 3. 🦕 Dinosaur Productivity
* **⚡ 3-Day Egg Laying Cycle:** Reduces Dinosaur egg produce cooldown from 7 days down to **3 days** (~373g/day Artisan).
* **🥚 Multi-Egg Clutches:** High-friendship dinosaurs ($\ge 4$ hearts) have a 25% chance to lay a bonus 2nd Dinosaur Egg (~467g/day Artisan).

### 4. 🐐 Goat Dairy Productivity
* **🥛 Multi-Milk Yields:** Preserves the 2-day dairy identity with high-friendship goats ($\ge 4$ hearts) having a 35% chance to yield **2x Goat Milk** on harvest (~378g/day Artisan).

### 5. 🔮 Void Chicken Productivity
* **🥚 Multi-Void Egg Drops:** High-friendship void chickens ($\ge 4$ hearts) have a 25% chance to drop a bonus 2nd Void Egg (~481g/day Artisan).

### 6. 🐑 Sheep Shearing Progression
* **✂️ Daily Shearing at 5 Hearts:** Fully happy sheep can be sheared every single day at 5 friendship hearts (~658g/day Artisan).

### 7. 🧪 Slime Hutch & Slime Ranching Scaling
* **🟢 6 Daily Slime Balls:** Enhances daily Slime Ball spawn capacity from 4 to **6 balls/day** in populated hutches.
* **💥 Pop Multipliers:** Slime balls drop **20–30 Slime** per pop.
* **🥚 2x Egg-Press Yields:** Slime Egg-Press has a **25% chance to yield 2x Slime Eggs** upon completion.

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
| `EnableSheepDailyShearAtMaxHearts` | `true` | Enables daily shearing for 5-heart sheep. |
| `EnableDinosaurCooldownReduction` | `true` | Reduces dinosaur cooldown to 3 days. |
| `DinosaurDaysToProduce` | `3` | Days between dinosaur egg lays (default: 3 days). |
| `EnableDinosaurMultiDrop` | `true` | Enables 25% chance for 2nd dinosaur egg. |
| `DinosaurMultiDropChance` | `0.25` | Probability of bonus dinosaur egg (default: 25%). |
| `EnableGoatMultiDrop` | `true` | Enables 35% chance for 2x goat milk on harvest. |
| `GoatMultiDropChance` | `0.35` | Probability of bonus goat milk (default: 35%). |
| `EnableVoidChickenMultiDrop` | `true` | Enables 25% chance for 2nd void egg. |
| `VoidChickenMultiDropChance` | `0.25` | Probability of bonus void egg (default: 25%). |
| `EnableSlimeRanchingBalancing` | `true` | Enhances Slime Hutch capacity and slime drops. |
| `SlimeHutchMaxBalls` | `6` | Maximum daily Slime Balls in Slime Hutch (default: 6). |
| `EnableSlimeEggPressMultiYield` | `true` | Enables 25% chance for 2x slime eggs in Egg-Press. |
| `SlimeEggPressDoubleChance` | `0.25` | Probability of 2x slime eggs in Egg-Press (default: 25%). |

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
