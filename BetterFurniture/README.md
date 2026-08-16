# 👑 BetterFurniture (Stardew Valley 1.6+)

**BetterFurniture** expands Stardew Valley 1.6+'s interior decorating system with a luxury Princess Furniture Collection, a spacious 4x4 King-Size Double Bed with unrestricted placement mechanics, custom animated light sources, wall canopy layering, and remastered farmhouse wall and flooring textures.

---

## ✨ Features

### 🛏️ 1. Spacious 4x4 Princess Double Bed
- **Expanded Dimensions (4x4)**: A large, comfortable king-size bed that offers a true master-bedroom aesthetic.
- **Sleep Triggers**: Fully functional sleep detection and partner positioning.
- **Unrestricted Placement**: Harmony patches remove vanilla placement bounding bottlenecks, allowing you to freely place and rotate beds anywhere inside farmhouses and cabins without getting blocked by invalid tile checks.

### 🛋️ 2. Princess Luxury Furniture Collection
- **Princess Double Bed (4x4)**: The centerpiece master bed.
- **Princess Pastel Window (2x2)**: Custom arched pastel window letting in natural sunlight.
- **Princess Wall Sconce (1x2)**: Wall-mounted torch featuring dynamic animated flame rendering and an ambient light source.
- **Princess Nightstand (1x2)**: Elegant matching nightstand equipped with a built-in interactive lamp.
- **Princess Grand Rug (4x3)**: Large ornate luxury area rug.
- **Princess Rococo Mirror (2x2)**: Ornate gold-trimmed vanity mirror.
- **Princess Bed Canopy (4x3)**: Elegant wall-mounted fabric canopy with custom draw-layer depth to properly hang over beds without visual clipping.

### 🎨 3. Farmhouse & Texture Restorations
- **White Kitchen Tiles**: High-resolution crisp overlay for farmhouse kitchen areas (`Maps/farmhouse_tiles`).
- **Princess Wallpaper & Warm Cream Wallpaper**: Delicate pastel floral and cream wallpaper options (`Maps/walls_and_floors`).
- **Pastel Kitchen Floor & Kid Floor**: Matching flooring patterns (`Maps/walls_and_floors`).

### 🛠️ 4. Robust Engine & Sync Patches
- **Auto-Syncing**: Automatically recalculates furniture bounding boxes and types upon save load and day start to ensure custom furniture coordinates remain persistent and glitch-free.
- **Wall Decor & Canopy Drawing**: Patches `Furniture.draw` to provide custom sprite offsets, flame particle rendering, and layering priorities.

---

## 📦 Furniture List & Data IDs

| Item Name | Type | Tile Size | Price | Item ID |
| :--- | :---: | :---: | :---: | :--- |
| **Princess Double Bed** | Bed (Double) | `4x4` | `10,000g` | `feiluvnana.BetterFurniture.PrincessDoubleBed` |
| **Princess Pastel Window** | Window | `2x2` | `2,000g` | `feiluvnana.BetterFurniture.PrincessPastelWindow` |
| **Princess Wall Sconce** | Wall Torch / Light | `1x2` | `1,000g` | `feiluvnana.BetterFurniture.PrincessWallSconce` |
| **Princess Nightstand** | Lamp / Table | `1x2` | `2,000g` | `feiluvnana.BetterFurniture.PrincessNightstand` |
| **Princess Grand Rug** | Rug | `4x3` | `3,000g` | `feiluvnana.BetterFurniture.PrincessGrandRug` |
| **Princess Rococo Mirror** | Wall Decor | `2x2` | `2,500g` | `feiluvnana.BetterFurniture.PrincessRococoMirror` |
| **Princess Bed Canopy** | Wall Canopy | `4x3` | `3,500g` | `feiluvnana.BetterFurniture.PrincessBedCanopy` |

---

## 🚀 Installation

1. Install the latest version of [SMAPI](https://smapi.io/).
2. Place the `BetterFurniture` folder into your `Stardew Valley/Mods/` directory.
3. Launch the game using **StardewModdingAPI.exe**.

---

## 🛠️ Building from Source

```powershell
dotnet build BetterFurniture.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
