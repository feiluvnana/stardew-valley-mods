# 🗺️ BetterMap

**BetterMap** is a high-performance map quality-of-life mod for **Stardew Valley 1.6+**, featuring a widened 3x1 Farmhouse exit doorway and seamless Ginger Island Farm barrier clearing.

---

## 📖 Table of Contents
1. [🚪 3x1 Farmhouse Exit Doorway](#-3x1-farmhouse-exit-doorway)
2. [🪵 Ginger Island Farm Barrier Removal](#-ginger-island-farm-barrier-removal)
3. [⚙️ Configuration (GMCM & config.json)](#️-configuration-gmcm--configjson)
4. [🛠️ Building & Installation](#️-building--installation)

---

## 🚪 3x1 Farmhouse Exit Doorway

* **Wide Doorway Exits:** Widens the exit doorway to **3 tiles wide** across all Farmhouse upgrade levels (`FarmHouse`, `FarmHouse1`, `FarmHouse2`) and the Ginger Island Farmhouse (`IslandFarmHouse`).
* **No More Bottlenecks:** Eliminates the narrow 1x1 tile exit doorway, allowing smooth, comfortable exits even when riding horses or passing by pets and spouses.
* **Warp & Tile Synchronization:** Synchronizes warp triggers and collision tiles across all 3 exit doorway tiles.

---

## 🪵 Ginger Island Farm Barrier Removal

* **Removes Clutter Barriers:** Completely removes the long horizontal driftwood fence and bleached log piles across Ginger Island Farm (`Island_West`).
* **Unlocks Farm Space:** Clears the divider separating the upper grassy path from the farmable fields, turning the island into an expansive, seamless open landscape.
* **Collision Cleanup:** Cleans up underlying collision layers so you can plant crops, construct buildings, and walk freely across former barrier zones.

---

## ⚙️ Configuration (GMCM & config.json)

```json
{
  "WidenHouseExit": true,
  "RemoveFarmDriftwoodBarrier": true
}
```

| Setting | Default | Description |
| :--- | :---: | :--- |
| `WidenHouseExit` | `true` | Widens the exit doorway of all Farmhouses and Island Farmhouse to 3x1 tiles. |
| `RemoveFarmDriftwoodBarrier` | `true` | Removes the driftwood fence and bleached log piles across the Ginger Island Farm. |

---

## 🛠️ Building & Installation

### Requirements
* **Stardew Valley 1.6+**
* **SMAPI 4.0+**
* *(Optional)* **Generic Mod Config Menu**

### Building from Source
```powershell
dotnet build BetterMap.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
