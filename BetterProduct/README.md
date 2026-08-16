# 🍯 BetterProduct (Stardew Valley 1.6+)

**BetterProduct** rebalances artisan goods, cooked dishes, and preserves in **Stardew Valley 1.6+**, ensuring that processing high-quality ingredients and cooking complex meals is always profitable, viable, and mechanically rewarding.

---

## ✨ Key Features

### 🌸 1. Flower Mead Quality & Value Preservation
- In vanilla Stardew Valley, processing valuable flower honey into mead loses the flower type, reducing precious Fairy Rose Honey into generic base-price mead.
- **BetterProduct** preserves the underlying flower value and applies a configurable multiplier (`MeadMultiplier: 1.5x`), ensuring that Fairy Rose Mead and rare flower meads remain exceptionally profitable.

### 🍳 2. Cooking Profit Margins & Value Scaling
- Dynamically calculates the value of cooked dishes based on the total sale price of their constituent raw ingredients plus a configurable profit margin (`CookingProfitMargin: 1.25x` / +25%).
- Makes home cooking profitable instead of a net gold loss compared to shipping raw ingredients.

### ⚡ 3. Stamina Restoration & Buff Duration Scaling
- **Stamina / Energy Scaling (`EnergyMultiplier: 1.25x`):** Boosts base energy restored by cooked dishes by +25%.
- **Buff Duration Boost (`BuffDurationMultiplier: 1.5x`):** Extends food stat buff durations by +50%, allowing mining, fishing, speed, and luck buffs to last significantly longer through the day.

### 🥫 4. Artisan Goods Rebalancing
- **Vegetable Juice Multiplier (`JuiceMultiplier: 3.0x`):** Elevates vegetable juices to rival fruit wines in value.
- **Preserves & Pickles (`PickleMultiplier: 2.5x`):** Increases preserves jar profitability.
- **Aged Roe & Caviar (`AgedRoeMultiplier: 2.5x`, `CaviarPrice: 750g`):** Scales fish pond roe production and sturgeon caviar margins.

---

## ⚙️ Configuration (Generic Mod Config Menu)

| Setting | Default | Description |
| :--- | :---: | :--- |
| `EnableCookingBalancing` | `true` | Enables ingredient-based cooking price calculations. |
| `CookingProfitMargin` | `1.25` | Multiplier applied to raw ingredient prices for cooked meals (1.0 = break-even, 1.25 = +25% profit). |
| `EnableEnergyBuff` | `true` | Multiplies energy/stamina gained from cooked food. |
| `EnergyMultiplier` | `1.25` | Energy multiplier factor. |
| `EnableBuffDurationBoost` | `true` | Extends buff durations granted by cooked meals. |
| `BuffDurationMultiplier` | `1.5` | Buff duration multiplier (1.5 = +50% duration). |
| `EnableMeadFix` | `true` | Retains flower honey value when brewed in kegs. |
| `MeadMultiplier` | `1.5` | Multiplier for flower mead value. |
| `EnableJuiceBuff` | `true` | Boosts vegetable juice multiplier. |
| `JuiceMultiplier` | `3.0` | Value multiplier for vegetable juice. |
| `EnablePickleBuff` | `true` | Boosts preserves jar pickle multiplier. |
| `PickleMultiplier` | `2.5` | Value multiplier for pickled crops. |
| `EnableRoeBuff` | `true` | Boosts aged roe and caviar prices. |
| `AgedRoeMultiplier` | `2.5` | Multiplier for aged roe in preserves jars. |
| `CaviarPrice` | `750` | Flat gold price for Caviar. |

---

## 🛠️ Building from Source

```powershell
dotnet build BetterProduct.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
