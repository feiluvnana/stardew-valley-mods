# 🎒 ExtendedStackable (Stardew Valley 1.6+)

**ExtendedStackable** unlocks full inventory stackability for items that are normally restricted to single inventory slots in **Stardew Valley 1.6+**, including tackle, trinkets, rings, furniture, boots, clothing, and hats.

---

## ✨ Key Features

### 🎣 1. Fishing Tackle & Lures (Up to 999)
- Stacks full-durability fishing tackle (Trap Bobbers, Curiosity Lures, Cork Bobbers, Dressed Spinners, Lead Bobbers, Quality Bobbers, Treasure Hunter Bobbers, etc.) up to 999.
- Used/damaged bobbers maintain separate slots to prevent accidental loss of durability data.

### 🔮 2. 1.6 Trinkets (Up to 999)
- Stacks identical 1.6 Trinkets (Fairy Boxes, Frog Eggs, Magic Quivers, Basilisk Paws, Ice Rods, Golden Spurs, Magic Hair Gel).
- **Smart Stat & Ascension Matching**: Fully compatible with [**BetterForge**](../BetterForge). Trinkets only stack if they share identical stats and Ascension levels, keeping your optimized and Ascended trinkets distinct and protected.

### 💍 3. Rings & Accessories (Up to 999)
- Cleanly stack duplicate rings (Iridium Bands, Lucky Rings, Crabshell Rings, Burglar's Rings, Napalm Rings, Phoenix Rings, etc.) to free up chest space.

### 🪑 4. Furniture & Decorations (Up to 999)
- Store large quantities of chairs, tables, lamps, rugs, windows, and decorative pieces in a single inventory slot.

### 🥾 5. Boots, Footwear & Clothing (Up to 999)
- Consolidate identical shirts, pants, skirts, hats, Space Boots, Cinderclown Boots, and Mermaid Boots.

---

## ⚙️ Configuration (Generic Mod Config Menu)

| Setting | Default | Description |
| :--- | :---: | :--- |
| `MaxStackSize` | `999` | Global maximum stack size limit for modified items (1–999). |
| `EnableTackleStacking` | `true` | Allows fishing tackle/bobbers to stack. |
| `EnableTrinketStacking` | `true` | Allows 1.6 trinkets to stack. |
| `EnableFurnitureStacking` | `true` | Allows furniture items to stack. |
| `EnableRingStacking` | `true` | Allows rings and accessories to stack. |
| `EnableClothingAndHatStacking` | `true` | Allows shirts, pants, and hats to stack. |
| `EnableBootsStacking` | `true` | Allows boots and footwear to stack. |

---

## 🛠️ Building from Source

```powershell
dotnet build ExtendedStackable.csproj
```

---

## 📄 License
Created by **feiluvnana** for Stardew Valley 1.6+. Built with SMAPI and Harmony.
