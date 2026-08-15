# 🌋 BetterForge

**BetterForge** overhauls Stardew Valley 1.6's Volcano Forge, Mini-Forge, and Anvil systems with 100% fair uniform weapon/tool enchantments, an interactive Mini-Forge style Anvil UI, smart trinket reforging protections, bad-luck pity progression, dynamic tooltips, and Prismatic Ascension.

---

## ✨ Features

### ⚔️ 1. Fair Uniform Weapon & Tool Enchantments
- **Equal Odds ($1/N$):** Every enchantment applicable to your weapon or tool (e.g. *Crusader*, *Vampiric*, *Generous*, *Master*, *Archaeologist*, *Bug Killer*, *Artful*, *Haymaker*) has the exact same probability of rolling.
- **No Biased Daily Seeds:** Uses true non-deterministic randomness instead of vanilla's locked daily PRNG seed, allowing you to re-roll upon reloading the day.
- **No Duplicate Current Rolls:** Filters out the enchantment currently applied to guarantee a new roll.

### 🔨 2. Anvil Trinket Forge Menu
- **Interactive UI:** Interacting with the Anvil opens a dedicated Mini-Forge style interface.
- **Single Item Protection:** Only 1 trinket can enter the Anvil at a time, preventing accidental stack consumption.
- **Full Controller & Mouse Support:** Snappy menus for gamepad navigation, drag-and-drop, and shift-click quick transfer.

### 🛡️ 3. Smart Trinket Reforging & Ascension
- **"Never Downgrade" Protection:** Re-rolling at the Anvil will never lower your trinket's level or stats.
- **Bad-Luck Pity Counter:** Guarantees higher tier rolls after a set number of rolls.
- **🌈 1-Time Prismatic Ascension:** Use 1 Prismatic Shard at the Anvil to permanently unlock enhanced ascension powers.
- **ExtendedStackable Mod Compatibility:** Trinkets can only stack when sharing the exact same stats and Ascension state.

---

## ⚙️ Configuration

Configurable in-game via **Generic Mod Config Menu**:
- `UniformEnchantmentChances` (default: `true`)
- `RandomizeEnchantmentSeed` (default: `true`)
- `PreventDowngrades` (default: `true`)
- `EnablePitySystem` (default: `true`)
- `RollsForGuaranteedUpgrade` (default: `3`)
- `IridiumBarCost` (default: `3`)
- `ShowStatRangesInTooltips` (default: `true`)
- `ShowReforgeSuccessMessage` (default: `true`)
