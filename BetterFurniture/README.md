# 👑 BetterFurniture

**BetterFurniture** expands Stardew Valley 1.6+'s interior decorating system with a luxury Princess Furniture Collection, a spacious 4x4 King-Size Double Bed with unrestricted placement mechanics, custom animated light sources, wall canopy layering, and remastered farmhouse wall and flooring textures.

---

## 📖 Table of Contents
1. [Spacious 4x4 Princess Double Bed](#-spacious-4x4-princess-double-bed)
2. [Princess Luxury Furniture Collection](#-princess-luxury-furniture-collection)
3. [Farmhouse Wall & Floor Restorations](#-farmhouse-wall--floor-restorations)
4. [Pierre's General Store Integration](#-pierres-general-store-integration)
5. [📦 Furniture Item IDs & Specifications](#-furniture-item-ids--specifications)
6. [🛠️ Building & Installation](#️-building--installation)

---

## 🛏️ Spacious 4x4 Princess Double Bed

* **King-Size Dimensions (4x4):** A grand, spacious master bed that enhances bedroom layouts.
* **Unrestricted Placement Engine:** Harmony patches bypass vanilla tile collision bottlenecks, allowing you to freely place, pick up, and rotate beds anywhere inside farmhouses, cabins, and ginger island houses without getting blocked by invalid tile checks.
* **Full Sleep & Spouse Triggers:** Seamless sleep confirmation dialogs and proper partner sleeping positioning.

---

## 🛋️ Princess Luxury Furniture Collection

* **Princess Double Bed (4x4):** Grand centerpiece master bed with elegant pink sheets and gold trimming.
* **Princess Pastel Window (2x2):** Custom arched window rendering natural ambient sunlight during daytime hours.
* **Princess Wall Sconce (1x2):** Wall-mounted golden sconce featuring dynamic animated flame particles and a functional light source.
* **Princess Nightstand (1x2):** Luxury bedside table equipped with a built-in interactive lamp.
* **Princess Grand Rug (4x3):** Large ornate pastel area rug.
* **Princess Rococo Mirror (2x2):** Ornate gold-framed vanity wall mirror.
* **Princess Bed Canopy (4x3):** Wall-mounted sheer fabric canopy with custom draw-layer depth to hang over beds without visual clipping.

---

## 🎨 Farmhouse Wall & Floor Restorations

* **White Kitchen Tiles:** High-resolution crisp overlay for farmhouse kitchen areas (`Maps/farmhouse_tiles`).
* **Princess Wallpaper & Warm Cream Wallpaper:** Delicate pastel floral and cream wallpaper options (`Maps/walls_and_floors`).
* **Pastel Kitchen Floor & Kid Floor:** Matching flooring patterns (`Maps/walls_and_floors`).

---

## 🛒 Pierre's General Store Integration

All BetterFurniture items are stocked directly at **Pierre's General Store** (`SeedShop`) at the very end of his store inventory:
* **Cost:** `0g` for convenient, stress-free decorating.

---

## 📦 Furniture Item IDs & Specifications

| Item Name (EN) | Tên Vật Phẩm (VI) | Type | Tile Size | Item ID |
| :--- | :--- | :---: | :---: | :--- |
| **Princess Double Bed** | **Giường Đôi Công Chúa** | Bed (Double) | `4x4` | `feiluvnana.BetterFurniture.PrincessDoubleBed` |
| **Princess Pastel Window** | **Cửa Sổ Pastel Công Chúa** | Window | `2x2` | `feiluvnana.BetterFurniture.PrincessPastelWindow` |
| **Princess Wall Sconce** | **Đèn Treo Tường Công Chúa** | Wall Torch / Light | `1x2` | `feiluvnana.BetterFurniture.PrincessWallSconce` |
| **Princess Nightstand** | **Tủ Đầu Giường Công Chúa** | Lamp / Table | `1x2` | `feiluvnana.BetterFurniture.PrincessNightstand` |
| **Princess Grand Rug** | **Thảm Lớn Công Chúa** | Rug | `4x3` | `feiluvnana.BetterFurniture.PrincessGrandRug` |
| **Princess Rococo Mirror** | **Gương Rococo Công Chúa** | Wall Decor | `2x2` | `feiluvnana.BetterFurniture.PrincessRococoMirror` |
| **Princess Bed Canopy** | **Màn Trướng Giường Công Chúa** | Wall Canopy | `4x3` | `feiluvnana.BetterFurniture.PrincessBedCanopy` |

---

## 🛠️ Building & Installation

### Requirements
* **Stardew Valley 1.6+**
* **SMAPI 4.0+**

### Building from Source
```powershell
dotnet build BetterFurniture.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
