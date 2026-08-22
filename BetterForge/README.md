# 🌋 BetterForge (Stardew Valley 1.6+)

**BetterForge** overhauls Stardew Valley 1.6's Volcano Forge, Mini-Forge, and Anvil systems with 100% fair uniform weapon/tool enchantments, an interactive Anvil reforging system with "Never Downgrade" stat protections, bad-luck mitigation, dynamic tooltip stat displays, the "Perfect" prefix for max-tier gear, and permanent **Prismatic Ascension** powers for 1.6 Trinkets.

---

## ✨ Key Features

### ⚔️ 1. Fair Uniform Weapon & Tool Enchantments
- **Equal Odds ($1/N$):** Every enchantment applicable to your weapon or tool (e.g. *Crusader*, *Vampiric*, *Generous*, *Master*, *Archaeologist*, *Bug Killer*, *Artful*, *Haymaker*, *Reaching*, *Bottomless*, etc.) has the exact same probability of rolling.
- **No Biased Daily Seeds:** Uses true non-deterministic randomness instead of vanilla's locked daily PRNG seed, allowing you to re-roll upon reloading the day.
- **No Duplicate Current Rolls:** Filters out the enchantment currently applied on your weapon or tool to guarantee a new roll.

---

### 🔨 2. Smart Anvil Trinket Reforging
- **"Never Downgrade" Protection:** Re-rolling at the Anvil will never lower your trinket's level, tier, or stats. If a roll is lower or equal, your previous stats are safely preserved.
- **Stack-Safe Reforging:** Hold a single trinket or an entire stack to reforge safely. Materials are accurately checked and consumed based on stack count.
- **"Perfect" Prefix & Stat Badges:** Reaching the absolute maximum roll on a trinket awards the **"✦ MAXIMUM TIER REACHED ✦"** badge and dynamically updates the display name to **"Perfect [Trinket Name]"** (e.g., *Perfect Fairy Box*, *Perfect Magic Quiver*).

---

### 🌈 3. Permanent Prismatic Ascension
Hold a trinket and forge it at the Anvil using **1 Prismatic Shard** to permanently unlock its unique Prismatic Ascension powers:

- **🌟 Base Passive Luck Buff:** Equipping any Ascended Trinket grants a passive **+0.5 Luck** per equipped ascended trinket (`Prismatic Ascension` endless buff).
- **🐸 Frog Egg:** Swallowing monsters drops all loot, with a **45% chance** to immediately reset the swallow cooldown.
- **🧚 Fairy Box:** Provides guaranteed baseline healing every pulse (even out of combat), heals nearby multiplayer allies, and grants **+1 Defense** for 15 seconds (*Fairy Blessing*).
- **🦜 Parrot Egg:** **Doubles gold coin drops** and grants a **+35% chance** for bonus monster loot drops upon defeat.
- **✨ Golden Spur:** Increases Critical Strike Chance by **+10%**, and the critical speed boost provides **+3 Attack** (*Spur Fury*).
- **🏹 Magic Quiver:** Spectral arrows **pierce through all enemies** and grant **+15% Critical Strike Chance**.
- **❄️ Ice Rod:** Striking frozen enemies shatters the ice and triggers an **ice blast** (deals 30% Attack damage and slows nearby enemies).
- **🦎 Basilisk Paw:** **Reflects 50% incoming damage** back to attackers, and attacks have a **20% chance to lifesteal** (heal 3–8 HP).

---

## ⚙️ Configuration (Generic Mod Config Menu)

| Setting | Default | Description |
| :--- | :---: | :--- |
| `UniformEnchantmentChances` | `true` | Gives every available enchantment an equal, fair probability of rolling ($1/N$). |
| `RandomizeEnchantmentSeed` | `true` | Uses true randomness instead of vanilla's deterministic daily PRNG sequence. |
| `PreventDowngrades` | `true` | Guarantees that re-rolling at the Anvil will never lower your trinket's stats. |
| `IridiumBarCost` | `3` | Number of Iridium Bars consumed per trinket at the Anvil (Vanilla = 3). |
| `ShowReforgeSuccessMessage` | `true` | Displays a HUD notification banner upon rolling an upgrade or perfect tier. |

---

## 🌐 Localization
Full English and Vietnamese translations included.

---

## 🛠️ Building from Source

```powershell
dotnet build BetterForge.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
