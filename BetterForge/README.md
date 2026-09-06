# 🌋 BetterForge

**BetterForge** is a comprehensive overhaul of Stardew Valley 1.6+'s **Volcano Forge**, **Mini-Forge**, and **Anvil** systems, featuring 100% fair uniform weapon and tool enchantments, an intelligent Anvil trinket reforging system with "Never Downgrade" stat protections, the dynamic **"Perfect"** tier prefix, and permanent **Prismatic Ascension** powers for 1.6 Trinkets.

---

## 📖 Table of Contents
1. [Module 1: Fair Uniform Enchantments](#-module-1-fair-uniform-enchantments)
2. [Module 2: Smart Anvil Trinket Reforging](#-module-2-smart-anvil-trinket-reforging)
3. [Module 3: Permanent Prismatic Ascension](#-module-3-permanent-prismatic-ascension)
4. [⚙️ Configuration (GMCM & config.json)](#️-configuration-gmcm--configjson)
5. [🛠️ Building & Installation](#️-building--installation)

---

## ⚔️ Module 1: Fair Uniform Enchantments

In vanilla Stardew Valley, weapon and tool enchantments at the Volcano Forge use a deterministic pseudo-random daily seed that can cause frustrating streaks or uneven odds.

### Features
* **Equal Probability ($1/N$):** Every enchantment applicable to your weapon or tool has the exact same probability of rolling.
  * **Weapon Enchantments:** *Crusader*, *Vampiric*, *Artful*, *Bug Killer*, *Haymaker*.
  * **Tool Enchantments:** *Auto-Hook*, *Archeologist*, *Bottomless*, *Efficient*, *Generous*, *Master*, *Preserving*, *Powerful*, *Reaching*, *Shaving*, *Swift*.
* **No Duplicate Rolls:** The enchantment currently applied on your weapon or tool is excluded from the roll pool, guaranteeing a new enchantment on every forge attempt.
* **True Non-Deterministic RNG:** Randomizes rolls independently of the vanilla daily seed, allowing you to reload the day if you wish to try different forge combinations.

---

## 🔨 Module 2: Smart Anvil Trinket Reforging

The Anvil allows players to re-roll the stats and levels of 1.6 Trinkets using **3 Iridium Bars**.

### Vanilla Stage Progression & Equal Expected Cost
* **Preserves Vanilla Tiers:** Respects the authentic vanilla tier structures for each trinket (e.g. Parrot Egg = 4, Fairy Box = 5, Magic Quiver = 5, Ice Rod = 5, Golden Spur = 5, Frog Egg = 7, Basilisk Paw = 1).
* **Equal Expected Iridium Cost:** Uses a normalized success probability curve such that the mathematical expected cost to reach Max Tier from Tier 1 is identical across all trinkets (~15 rolls = 45 Iridium Bars at 3 bars/roll).
* **"Never Downgrade" Stat Protection:** Reforge rolls advance step-by-step ($i \to i + 1$). If an upgrade attempt fails, your current tier and stats are 100% protected—never lost or downgraded.
* **Stack-Safe Reforging:** Reforging while holding a stack of trinkets processes safely without losing items.

### "Perfect" Tier Prefix & Maximum Badges
When a trinket reaches its absolute maximum possible stat roll, BetterForge dynamically updates the item:
* **Display Name:** Prepends the **"Perfect"** title (e.g. *Perfect Fairy Box*, *Perfect Magic Quiver*, *Perfect Ice Rod*).
* **Tooltip Tier Badges:** Displays **"✦ TIER {tier}/{maxTier} ✦"** during progression and the golden **"✦ MAXIMUM TIER REACHED ✦"** badge at max rank.
* **HUD Notification:** Displays an on-screen toast banner announcing your tier upgrade or stats-protected roll.

---

## 🌈 Module 3: Permanent Prismatic Ascension

Bring any Trinket to the Anvil and forge it with **1 Prismatic Shard** for a **20% chance** to permanently unlock its **Prismatic Ascension** (complete with dedicated visual debris, sound effects, and success/failure HUD toast banners).

### 🌟 Base Passive Luck Buff
* Equipping any Ascended Trinket grants an endless **+0.5 Luck** buff per equipped ascended trinket (`Prismatic Ascension`).

### Balanced Ascended Trinket Powers

| Trinket | Vanilla Stats / Effect | Prismatic Ascension Enhanced Power |
| :--- | :--- | :--- |
| **🐸 Frog Egg** | Follows player and eats nearby monsters. | Swallowing monsters drops all their loot, with a **40% chance** to immediately reset the swallow cooldown. |
| **🧚 Fairy Box** | Spawns a healing fairy (Level 1–5). | Restores **2% max HP per pulse out of combat** (scaled by Power, min 2 HP), provides combat healing pulses to player and nearby co-op allies, and grants **+1 Defense** for 15s (*Fairy Blessing*). |
| **🦜 Parrot Egg** | Spawns a parrot that finds gold coins (Level 1–4). | Grants **scaled bonus gold** on monster kills based on monster max HP, and provides a **+28% chance** for defeated monsters to drop bonus monster loot. |
| **✨ Golden Spur** | Critical strikes grant a short speed boost (5–10s). | Increases Critical Strike Chance by **+5%**; during *Spur Fury*, gain **+3 Attack** and **+25% Critical Strike Damage**. |
| **🏹 Magic Quiver** | Fires spectral arrows every 0.9–1.6s. | Spectral arrows **pierce up to 2 monsters** and gain **+15% Critical Strike Chance** (1.5x damage). |
| **❄️ Ice Rod** | Shoots ice orbs freezing enemies (3–5s cooldown). | Striking frozen enemies shatters the ice into an **ice blast** (deals 30% Attack damage and inflicts a **2.5s frost chill slow** on nearby foes). |
| **🦎 Basilisk Paw** | Grants immunity to debuffs (Slimed, Jinxed, etc.). | **Reflects 75% incoming damage** back to attackers with recoil, and attacks have a **12% chance to lifesteal** (heals 3–5 HP, 0.6s cooldown). |

---

## ⚙️ Configuration (GMCM & config.json)

```json
{
  "UniformEnchantmentChances": true,
  "RandomizeEnchantmentSeed": true,
  "PreventDowngrades": true,
  "IridiumBarCost": 3,
  "ShowReforgeSuccessMessage": true
}
```

| Setting | Default | Description |
| :--- | :---: | :--- |
| `UniformEnchantmentChances` | `true` | Gives every available weapon/tool enchantment an equal, fair probability ($1/N$). |
| `RandomizeEnchantmentSeed` | `true` | Uses true non-deterministic RNG instead of vanilla's daily PRNG sequence. |
| `PreventDowngrades` | `true` | Guarantees that re-rolling at the Anvil will never lower your trinket stats. |
| `IridiumBarCost` | `3` | Number of Iridium Bars consumed per trinket reforge attempt (Vanilla = 3). |
| `ShowReforgeSuccessMessage` | `true` | Displays a HUD notification toast when rolling an upgrade or perfect tier. |

---

## 🛠️ Building & Installation

### Requirements
* **Stardew Valley 1.6+**
* **SMAPI 4.0+**
* *(Optional)* **Generic Mod Config Menu**

### Building from Source
```powershell
dotnet build BetterForge.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
