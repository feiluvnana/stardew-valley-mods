# 🌵 ExtendedDesertFestival

**ExtendedDesertFestival** transforms the 1.6 Calico Desert Festival from a Spring-only event into a year-round festival across all four seasons in **Stardew Valley 1.6+**, complete with Calico Egg inventory persistence between festivals.

---

## 📖 Table of Contents
1. [🌸 Four-Season Desert Festival](#-four-season-desert-festival)
2. [🥚 Calico Egg Inventory Persistence](#-calico-egg-inventory-persistence)
3. [⚙️ Configuration (GMCM & config.json)](#️-configuration-gmcm--configjson)
4. [🛠️ Building & Installation](#️-building--installation)

---

## 🌸 Four-Season Desert Festival

In vanilla Stardew Valley 1.6, the Calico Desert Festival only occurs once per year on Spring 15–17.

### Features
* **All Four Seasons Supported:** Celebrates the 3-day Calico Desert Festival on the **15th, 16th, and 17th** of **Summer**, **Fall**, and **Winter** (in addition to Spring).
* **Individual Season Toggles:** Each season can be toggled on or off independently via GMCM or `config.json`.
* **Seamless Vanilla Integration:**
  * Billboard festival notifications appear in Pelican Town on the 14th of each active festival season.
  * Town calendar displays the 3-day festival icons.
  * Bus driver (Pam) and festival warp schedules operate correctly across all seasons.
  * Vendor shops, chef stations, race betting, and desert festival mini-games load seamlessly.

---

## 🥚 Calico Egg Inventory Persistence

In vanilla Stardew Valley, any unused Calico Eggs are automatically deleted from your inventory and chests when the festival ends.

### Features
* **Preserve Unused Eggs:** Prevents Calico Eggs from being wiped after festivals conclude.
* **Save Up for Rewards:** Collect eggs over multiple festivals and seasons to afford expensive high-tier rewards, prize tickets, and rare decorative items.

---

## ⚙️ Configuration (GMCM & config.json)

```json
{
  "EnableSummer": true,
  "EnableFall": true,
  "EnableWinter": true,
  "KeepEggs": true
}
```

| Setting | Default | Description |
| :--- | :---: | :--- |
| `EnableSummer` | `true` | Enables the Desert Festival on Summer 15–17. |
| `EnableFall` | `true` | Enables the Desert Festival on Fall 15–17. |
| `EnableWinter` | `true` | Enables the Desert Festival on Winter 15–17. |
| `KeepEggs` | `true` | Prevents Calico Eggs from being deleted when the festival concludes. |

---

## 🛠️ Building & Installation

### Requirements
* **Stardew Valley 1.6+**
* **SMAPI 4.0+**
* *(Optional)* **Generic Mod Config Menu**

### Building from Source
```powershell
dotnet build ExtendedDesertFestival.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
