# 🤖 Development Guidelines for feiluvnana's Stardew Valley Mod Suite

This document serves as the persistent knowledge base and rule set for AI coding assistants working in this repository across all sessions.

---

## 🧭 1. Repository & Shell Environment

* **Repository Root**: `d:\dev\winget\Valve.Steam\steamapps\common\Stardew Valley\Mods\[feiluvnana Mods]`
* **Target Runtime**: `.NET 6.0` (`net6.0`)
* **Core APIs**: **Stardew Valley 1.6+**, **SMAPI 4.0+**, **HarmonyLib**, `Pathoschild.Stardew.ModBuildConfig`
* **Shell & Path Escaping (CRITICAL)**:
  * The workspace path contains square brackets: `[feiluvnana Mods]`.
  * In Windows PowerShell, square brackets are wildcard characters.
  * Always use `-LiteralPath` when changing directories or executing commands:
    ```powershell
    Set-Location -LiteralPath "d:\dev\winget\Valve.Steam\steamapps\common\Stardew Valley\Mods\`[feiluvnana Mods`]"
    ```

---

## 📦 2. Mod Suite Architecture

| Directory | Mod | Description |
| :--- | :--- | :--- |
| `BetterChest` | **💎 BetterChest** | Skull Cavern 7-category dynamic loot pool, decaying multi-rolls (1–8 rolls, min guarantees), jackpot stack multipliers (up to 5x), linear legendary depth scaling, and milestone gatekeeping. |
| `BetterFishing` | **🎣 BetterFishing** | Balanced dynamic fish price scaling, movement behavior bonuses, environmental & isolated location traits, +100% legendary multiplier, dual anchors (Catfish 200g, Legend 5,000g), and decaying treasure chest rolls. |
| `BetterForge` | **🌋 BetterForge** | 100% fair uniform weapon/tool enchantments ($1/N$), "Never Downgrade" trinket reforging, "Perfect" tier prefixes, and permanent Prismatic Ascension powers (+0.5 Luck, enhanced abilities). |
| `BetterIndustry` | **🏭 BetterIndustry** | Artisan goods and cooking rebalance: Quality-preserving machines, Flower mead 2.0x value scaling, Truffle Oil scaling fix, Vegetable Juice buffs, Expanded Cask aging, and profitable cooking (+25%). |
| `BetterQOL` | **📦 BetterQOL** | Live hover overlays (crop/machine timers, tree maturation, animal hearts), rich Lookup Anything (`F1` key with search & almanac), bulk/instant geode cracking, and unstackable item stack size overrides up to 999. |
| `BetterFurniture` | **👑 BetterFurniture** | 4x4 Princess King Bed with free placement anywhere, animated sconce flames, light sources, canopy layering, and farmhouse tile restorations. |
| `BetterMap` | **🗺️ BetterMap** | Widens all Farmhouse exit doorways to 3x1 tiles, removes Ginger Island Farm driftwood fence and bleached log piles. |
| `BetterEvent` | **🌵 BetterEvent** | Calico Desert Festival in Summer, Fall, and Winter (15th–17th), preserves Calico Eggs across seasons. |

---

## 📐 3. Development Workflow & Rules

### A. Version Bumping
Whenever updating or making non-trivial changes to a mod:
1. Update `"Version": "X.Y.Z"` in `<ModFolder>/manifest.json`.
2. Update `<Version>X.Y.Z</Version>` in `<ModFolder>/<ModFolder>.csproj`.
3. Update the version column in the root `README.md` table.
4. When built, `Pathoschild.Stardew.ModBuildConfig` will automatically package `<ModFolder> X.Y.Z.zip`.

### B. Building & Compilation
Always build using `dotnet build` from the repo root or specifying the exact `.csproj`:
```powershell
Set-Location -LiteralPath "d:\dev\winget\Valve.Steam\steamapps\common\Stardew Valley\Mods\`[feiluvnana Mods`]"
dotnet build "<ModFolder>/<ModFolder>.csproj"
```
* Ensure **0 Warnings** and **0 Errors** before committing.

### C. Localization (i18n) Standards
* **Never hardcode English UI strings** in GMCM configuration or rendering menus.
* Always update both translation dictionaries:
  * `<ModFolder>/i18n/default.json` (English)
  * `<ModFolder>/i18n/vi.json` (Vietnamese)
* Keep key naming consistent: `config.<section>.<option>.name` / `.tooltip`.

### D. Generic Mod Config Menu (GMCM) Integration
* Every configuration option defined in `<ModFolder>/ModConfig.cs` **must** be:
  1. Registered in `ModEntry.cs` with `spacechase0.GenericModConfigMenu`.
  2. Mirrored in `<ModFolder>/config.json` with appropriate default values.
  3. Documented in the respective mod's `README.md`.

### E. Git Commit & Push Standards
* Follow standard conventional commit format:
  * `feat(<ModName>): description`
  * `fix(<ModName>): description`
  * `build(<ModName>): description`
  * `chore: description`
* Always check `git status` and test builds before committing and pushing to remote.
