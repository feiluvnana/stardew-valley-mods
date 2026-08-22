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

### 🛒 4. Pierre's General Store Integration
- All BetterFurniture items are stocked directly at **Pierre's General Store** (`SeedShop`) at the very end of his store inventory.
- Price: **0g** for convenient early and mid-game decorating.

### 🌐 5. Multi-Language & Vietnamese Localization
- Built-in internationalization (`i18n`) support for English (`default.json`) and Vietnamese (`vi.json`).
- Dynamic real-time translation switching supported.

### 🛠️ 6. Robust Engine & Sync Patches
- **Auto-Syncing**: Automatically recalculates furniture bounding boxes and types upon save load and day start to ensure custom furniture coordinates remain persistent and glitch-free.
- **Wall Decor & Canopy Drawing**: Patches `Furniture.draw` to provide custom sprite offsets, flame particle rendering, and layering priorities.

---

## 📦 Furniture List & Data IDs

| Item Name (EN) | Tên Vật Phẩm (VI) | Type | Tile Size | Pierre Shop Price | Item ID |
| :--- | :--- | :---: | :---: | :---: | :--- |
| **Princess Double Bed** | **Giường Đôi Công Chúa** | Bed (Double) | `4x4` | `0g` | `feiluvnana.BetterFurniture.PrincessDoubleBed` |
| **Princess Pastel Window** | **Cửa Sổ Pastel Công Chúa** | Window | `2x2` | `0g` | `feiluvnana.BetterFurniture.PrincessPastelWindow` |
| **Princess Wall Sconce** | **Đèn Treo Tường Công Chúa** | Wall Torch / Light | `1x2` | `0g` | `feiluvnana.BetterFurniture.PrincessWallSconce` |
| **Princess Nightstand** | **Tủ Đầu Giường Công Chúa** | Lamp / Table | `1x2` | `0g` | `feiluvnana.BetterFurniture.PrincessNightstand` |
| **Princess Grand Rug** | **Thảm Lớn Công Chúa** | Rug | `4x3` | `0g` | `feiluvnana.BetterFurniture.PrincessGrandRug` |
| **Princess Rococo Mirror** | **Gương Rococo Công Chúa** | Wall Decor | `2x2` | `0g` | `feiluvnana.BetterFurniture.PrincessRococoMirror` |
| **Princess Bed Canopy** | **Màn Trướng Giường Công Chúa** | Wall Canopy | `4x3` | `0g` | `feiluvnana.BetterFurniture.PrincessBedCanopy` |

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
