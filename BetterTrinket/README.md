# 🔮 BetterTrinket

**BetterTrinket** overhauls Stardew Valley 1.6's trinket system with smart Anvil reforging protections, bad-luck pity progression, dynamic in-game tooltips, and customizable costs.

---

## ✨ Features

- **🛡️ "Never Downgrade" Protection:**
  Re-rolling at the Anvil will never lower your trinket's level or stats. You only keep or improve what you currently have.

- **🎲 Bad-Luck Pity Counter:**
  Every reforge roll on a trinket is tracked. After reaching the configurable threshold (`RollsForGuaranteedUpgrade: 3`), your next roll is guaranteed to be a higher tier. Rolling a max-tier roll resets the counter.

- **📜 In-Game Stat Range Tooltips:**
  Trinket tooltips dynamically display tier stars (`★★★★☆`), stat bounds (e.g. firing delays, freeze durations, buff timers, frog variant & cooldowns), and pity progress.

- **🪙 Configurable Anvil Cost:**
  Configure the number of Iridium Bars required per roll (`IridiumBarCost: 3`, range 1–10) via Generic Mod Config Menu.

- **🌈 1-Time Prismatic Ascension (1 Prismatic Shard at Anvil):**
  Permanently ascend any trinket to unlock its unique signature skill:
  - **🐸 Frog Egg:** *Full Harvest Feast / Bữa Tiệc No Say* — Swallowing monsters drops all of their normal loot and rewards.
  - **🧚 Fairy Box:** *Sanctuary Bloom / Hào Quang Hộ Thể* — Heal pulses also restore health to nearby allies, farmhands, horses, and pets.
  - **🦜 Parrot Egg:** *Treasure Hunter / Thợ Săn Kho Báu* — Doubles the amount and drop frequency of gold coins from monsters.
  - **⚡ Golden Spur:** *Battle Frenzy / Cuồng Chiến Thần Tốc* — Critical strike speed buff also grants +Attack damage.
  - **🏹 Magic Quiver:** *Spectral Piercer / Xuyên Phá Thần Tiễn* — Spectral arrows pierce through all enemies in their path.
  - **❄️ Ice Rod:** *Shatter Strike / Phá Băng Bạo Kích* — Striking frozen enemies shatters the ice for bonus critical damage.
  - **🛡️ Basilisk Paw:** *Gorgon's Retribution / Phản Đòn Xà Vương* — Reflects a portion of incoming damage back to attackers.

- **🌐 Multi-Language Support:**
  Full native localization support for English and Vietnamese (`vi.json`).
---

## ⚙️ Configuration (`config.json`)

```json
{
  "PreventDowngrades": true,
  "EnablePitySystem": true,
  "RollsForGuaranteedUpgrade": 3,
  "IridiumBarCost": 3,
  "ShowStatRangesInTooltips": true,
  "ShowReforgeSuccessMessage": true
}
```

Configurable in-game via **Generic Mod Config Menu**.
