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

### "Never Downgrade" Stat Protection
* Re-rolling at the Anvil will **never lower** your trinket's level, cooldown, or primary stats.
* If a new roll produces lower or equal stats, your previous superior stats are automatically preserved.
* **Stack-Safe Reforging:** Reforging while holding a stack of trinkets processes safely without losing items.

### "Perfect" Tier Prefix & Maximum Badges
When a trinket reaches its absolute maximum possible stat roll, BetterForge dynamically updates the item:
* **Display Name:** Prepends the **"Perfect"** title (e.g. *Perfect Fairy Box*, *Perfect Magic Quiver*, *Perfect Ice Rod*).
* **Tooltip Badge:** Adds the golden **"✦ MAXIMUM TIER REACHED ✦"** badge to the tooltip.
* **HUD Notification:** Displays an on-screen toast banner announcing your perfect reforge.

---

## 🌈 Module 3: Permanent Prismatic Ascension

Bring any Trinket to the Anvil and forge it with **1 Prismatic Shard** to permanently unlock its **Prismatic Ascension**.

### 🌟 Base Passive Luck Buff
* Equipping any Ascended Trinket grants an endless **+0.5 Luck** buff per equipped ascended trinket (`Prismatic Ascension`).

### Unique Ascended Trinket Powers

| Trinket | Vanilla Stats / Effect | Prismatic Ascension Enhanced Power |
| :--- | :--- | :--- |
| **🐸 Frog Egg** | Follows player and eats nearby monsters. | Swallowing monsters drops all their loot, with a **45% chance** to immediately reset the swallow cooldown. |
| **🧚 Fairy Box** | Spawns a healing fairy (Level 1–5). | Provides continuous passive healing every pulse (even out of combat), heals nearby multiplayer allies, and grants **+1 Defense** for 15s (*Fairy Blessing*). |
| **🦜 Parrot Egg** | Spawns a parrot that finds gold coins (Level 1–4). | **Doubles gold coin value** and grants a **+35% chance** for defeated monsters to drop bonus monster loot. |
| **✨ Golden Spur** | Critical strikes grant a short speed boost (5–10s). | Increases Critical Strike Chance by **+10%**, and the critical speed boost provides **+3 Attack** (*Spur Fury*). |
| **🏹 Magic Quiver** | Fires spectral arrows every 0.9–1.6s. | Spectral arrows **pierce through all enemies** and grant **+15% Critical Strike Chance**. |
| **❄️ Ice Rod** | Shoots ice orbs freezing enemies (3–5s cooldown). | Striking frozen enemies shatters the ice into an **ice blast**, dealing 30% Attack damage and slowing nearby foes. |
| **🦎 Basilisk Paw** | Grants immunity to debuffs (Slimed, Jinxed, etc.). | **Reflects 50% incoming damage** back to attackers, and melee attacks have a **20% chance to lifesteal** (heals 3–8 HP). |

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
