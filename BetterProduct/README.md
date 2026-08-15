# 🍯 BetterProduct

**BetterProduct** rebalances artisan goods and cooked dishes in **Stardew Valley 1.6+**, ensuring that processing high-quality ingredients and cooking complex meals is always profitable and mechanically rewarding.

---

## ✨ Features

- **🌸 Honey & Flower Mead Quality Preservation:**
  In vanilla, processing valuable flower honey into mead loses the flower type and value. **BetterProduct** preserves the underlying flower value and applies a configurable multiplier (`MeadMultiplier: 1.5x`), ensuring that Fairy Rose Mead and rare flower meads remain highly profitable.

- **🍳 Cooking Profit Margins & Value Scaling:**
  Cooked dishes calculate value based on the total sale price of their constituent raw ingredients plus a configurable profit margin (`CookingProfitMargin: 1.25x` / +25%).

- **⚡ Energy, Health & Buff Duration Boosts:**
  - **Energy Scaling (`EnergyMultiplier: 1.25x`):** Increases base stamina restoration from cooked foods.
  - **Buff Duration Scaling (`BuffDurationMultiplier: 1.5x`):** Extends food stat buff timers by +50%, allowing mining/fishing/luck buffs to last significantly longer into the day.

- **🥫 Enhanced Artisan Goods:**
  - **Juice Multiplier (`JuiceMultiplier: 3.0x`):** Boosts vegetable juices to rival fruit wines.
  - **Pickle Multiplier (`PickleMultiplier: 2.5x`):** Improves preserves jar margins.
  - **Aged Roe & Caviar Scaling (`AgedRoeMultiplier: 2.5x`, `CaviarPrice: 750g`):** Makes fish pond roe production and sturgeon caviar lucrative.

- **🍄 Enhanced Dehydrator Mechanics:**
  - **Mixed-Quality Input Support:** Combines items of different star qualities (e.g. 2 Normal + 2 Silver + 1 Gold) into the 5-item batch with zero waste.
  - **Smart Inventory Auto-Pull:** Interacting with $<5$ items in hand automatically pulls remaining matching items from other inventory stacks.
  - **Weighted Output Quality:** Produces Silver, Gold, or Iridium dried goods matching the weighted average quality of the input batch.
  - **Custom Speed & Recipe Expansion:** Configurable speed multiplier and options to dry edible vegetables and flowers.

---

## ⚙️ Configuration (`config.json`)

```json
{
  "EnableCookingBalancing": true,
  "CookingProfitMargin": 1.25,
  "EnableEnergyBuff": true,
  "EnergyMultiplier": 1.25,
  "EnableBuffDurationBoost": true,
  "BuffDurationMultiplier": 1.5,
  "EnableMeadFix": true,
  "MeadMultiplier": 1.5,
  "EnableJuiceBuff": true,
  "JuiceMultiplier": 3.0,
  "EnablePickleBuff": true,
  "PickleMultiplier": 2.5,
  "EnableRoeBuff": true,
  "AgedRoeMultiplier": 2.5,
  "CaviarPrice": 750,
  "EnableDehydratorBalancing": true,
  "AllowMixedQualityDehydrating": true,
  "EnableDriedQualityScaling": true,
  "DehydratorSpeedMultiplier": 1.0,
  "AllowVegetableDehydrating": true,
  "AllowFlowerDehydrating": false
}
```

Configurable in-game via **Generic Mod Config Menu**.
