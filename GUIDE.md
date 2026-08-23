# 🧑‍💻 The Complete Stardew Valley Modding Guide

> **Audience:** Developers with programming experience who are new to the Stardew Valley modding world.
> This guide covers the **entire modding ecosystem** — from game architecture to frameworks, Content Patcher to Harmony, map editing to releasing on Nexus. Code examples reference this repository where applicable but the knowledge is universal.

---

## Table of Contents

**Part I — The Modding World**

1. [How Stardew Valley Works Under the Hood](#1-how-stardew-valley-works-under-the-hood)
2. [The Modding Ecosystem Overview](#2-the-modding-ecosystem)
3. [Types of Mods](#3-types-of-mods)
4. [Key Framework Mods You Should Know](#4-key-framework-mods)

**Part II — Setting Up**

5. [Environment Setup](#5-environment-setup)
6. [Project Configuration (.csproj)](#6-project-configuration)
7. [The Manifest (manifest.json)](#7-the-manifest)

**Part III — SMAPI C# Mod Development**

8. [ModEntry — The Lifecycle Entry Point](#8-modentry)
9. [SMAPI Events — The Full Reference](#9-smapi-events)
10. [Configuration (ModConfig + config.json)](#10-configuration)
11. [Localization (i18n)](#11-localization)
12. [Generic Mod Config Menu (GMCM) Integration](#12-gmcm-integration)
13. [Harmony Patching — Rewriting Game Methods](#13-harmony-patching)
14. [Editing Game Assets (Content API)](#14-editing-game-assets)
15. [SMAPI Reflection & Private Access](#15-smapi-reflection)
16. [Multiplayer Considerations](#16-multiplayer)
17. [Console Commands](#17-console-commands)

**Part IV — Content Patcher (No C# Required)**

18. [Content Patcher Fundamentals](#18-content-patcher)
19. [Trigger Actions & Game State Queries (1.6)](#19-trigger-actions)

**Part V — Game Data & Assets**

20. [Stardew Valley 1.6 Data Architecture](#20-data-architecture)
21. [Qualified Item IDs](#21-qualified-item-ids)
22. [Map Editing with Tiled](#22-map-editing)
23. [Sprites, Textures & Spritesheets](#23-sprites-and-textures)

**Part VI — Key Game APIs You'll Use Daily**

24. [Essential Types & Patterns](#24-essential-types)
25. [Common Game Interactions](#25-common-interactions)

**Part VII — Ship It**

26. [Decompiling the Game Code](#26-decompiling)
27. [Building & Testing](#27-building-and-testing)
28. [Debugging](#28-debugging)
29. [Releasing Your Mod](#29-releasing)
30. [Common Pitfalls & Best Practices](#30-common-pitfalls)
31. [External Resources — The Complete Link Collection](#31-external-resources)

---

# Part I — The Modding World

## 1. How Stardew Valley Works Under the Hood

### Tech Stack

| Layer | Technology | Role |
|:---|:---|:---|
| **Game Logic** | C# / .NET 6.0 | All gameplay code — NPCs, farming, combat, menus |
| **Rendering** | MonoGame (XNA successor) | 2D rendering, input, audio via `SpriteBatch` |
| **Maps** | xTile engine | Tile-based maps loaded from `.tbin`/`.tmx` files |
| **Data** | JSON / dictionaries | Items, NPCs, machines, shops — data-driven since 1.6 |
| **Mod Loader** | SMAPI | Injects mods, provides APIs, catches errors |

### The Game Loop

Stardew Valley uses the standard MonoGame game loop:

```
┌─────────────────────────────────────────────────────┐
│                    Game Loop (~60 FPS)              │
│                                                     │
│   ┌─────────────┐    ┌──────────────────────┐       │
│   │  Update()   │───▶│      Draw()          │       │
│   │  (Logic)    │    │    (Rendering)       │       │
│   │             │    │                      │       │
│   │ • Input     │    │ • SpriteBatch.Begin()│       │
│   │ • NPC AI    │    │ • Draw world tiles   │       │
│   │ • Farming   │    │ • Draw objects/NPCs  │       │
│   │ • Time      │    │ • Draw HUD/menus     │       │
│   │ • Physics   │    │ • SpriteBatch.End()  │       │
│   └─────────────┘    └──────────────────────┘       │
│         ▲                                           │
│         │  SMAPI injects events here:               │
│         │  • UpdateTicked (after Update)            │
│         │  • Rendered (after Draw)                  │
│         │  • RenderingHud (before HUD Draw)         │
│         │  • RenderedHud (after HUD Draw)           │
│         │  • etc.                                   │
└─────────────────────────────────────────────────────┘
```

**Key principle for modders:**
- **Logic** goes in `GameLoop` events (`UpdateTicked`, `DayStarted`, etc.)
- **Rendering** goes in `Display` events (`Rendered`, `RenderingHud`, etc.)
- Never put heavy computation in rendering events — it causes stutter.

### SpriteBatch — How Drawing Works

All 2D rendering uses MonoGame's `SpriteBatch`:
1. `SpriteBatch.Begin()` — sets blend mode, shader, sort mode.
2. `SpriteBatch.Draw(texture, position, sourceRect, color, ...)` — buffers a sprite.
3. `SpriteBatch.End()` — flushes the buffer to the GPU in one batch.

The game calls Begin/End in specific phases. SMAPI's Display events give you access to draw at the right time.

---

## 2. The Modding Ecosystem

```
┌───────────────────────────────────────────────────────┐
│                    STARDEW VALLEY                     │
│                (Vanilla Game, .NET 6)                 │
├───────────────────────────────────────────────────────┤
│                      SMAPI 4.0+                       │
│          (Mod loader, event bus, content API)         │
├──────────┬──────────────┬─────────────────────────────┤
│Framework │  C# Mods     │ Content Packs               │
│  Mods    │  (DLLs)      │ (JSON + images, no code)    │
│          │              │                             │
│ Content  │ Your custom  │ Portraits, recolors,        │
│ Patcher  │ gameplay     │ new items, map edits,       │
│ GMCM     │ changes      │ dialogue changes            │
│ SpaceCore│              │                             │
│ FTM      │              │                             │
└──────────┴──────────────┴─────────────────────────────┘
```

### The Two Approaches to Modding

| Approach | How | When to Use |
|:---|:---|:---|
| **Content Packs (JSON)** | Write JSON config files + provide images. No C# code needed. | Portraits, sprites, data edits, map patches, new items, dialogue |
| **C# Mods (SMAPI DLLs)** | Write compiled C# code loaded by SMAPI. Full game access. | Custom logic, new mechanics, UI overlays, Harmony patches |

> [!TIP]
> **Start with Content Patcher** if your mod only needs to add/edit items, sprites, or data. Only write C# if you need custom logic, UI, or Harmony patches.

---

## 3. Types of Mods

### By Function

| Category | Examples | Approach |
|:---|:---|:---|
| **Visual/Aesthetic** | Portraits, building recolors, seasonal outfits | Content Pack |
| **New Content** | Custom crops, furniture, recipes, NPCs | Content Pack (CP) or C# + CP |
| **Gameplay Tweaks** | Chest loot changes, enchantment rebalance | C# Mod + Harmony |
| **Quality of Life** | Hover tooltips, auto-stacking, lookup overlays | C# Mod |
| **Map Edits** | Wider doorways, new locations, removed obstacles | Content Pack (CP) or C# |
| **Framework** | Provides APIs for other mods (GMCM, SpaceCore) | C# Mod |
| **Expansion** | Stardew Valley Expanded, Ridgeside Village | C# + Content Pack hybrid |

### By Technical Architecture

| Type | Entry Point | Requires C#? |
|:---|:---|:---|
| **SMAPI Mod** | `manifest.json` → `EntryDll` → `ModEntry : Mod` | Yes |
| **Content Pack** | `manifest.json` → `ContentPackFor` → `content.json` | No |
| **XNB Replacement** | Direct file replacement (legacy, avoid) | No |

---

## 4. Key Framework Mods

These are mods other mods depend on. As a developer, you'll interact with many of them:

| Framework | Author | Purpose | Status |
|:---|:---|:---|:---|
| **[Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915)** | Pathoschild | JSON-based asset editing (data, images, maps) | ✅ Essential |
| **[GMCM](https://www.nexusmods.com/stardewvalley/mods/5098)** | spacechase0 | In-game config menu UI for any mod | ✅ Essential |
| **[SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348)** | spacechase0 | Extended APIs (custom skills, crafting, events) | ✅ Active |
| **[Farm Type Manager](https://www.nexusmods.com/stardewvalley/mods/3231)** | Esca | Custom spawning on any map (forage, ore, monsters) | ✅ Active |
| **[Alternative Textures](https://www.nexusmods.com/stardewvalley/mods/9246)** | PeacefulEnd | Multiple texture variants for buildings/objects | ✅ Active |
| **[Fashion Sense](https://www.nexusmods.com/stardewvalley/mods/9969)** | PeacefulEnd | Custom clothing, hairstyles, accessories | ✅ Active |
| **[Json Assets](https://www.nexusmods.com/stardewvalley/mods/1720)** | spacechase0 | Custom items/crops (legacy) | ⚠️ Legacy — prefer CP in 1.6 |
| **[BFAV](https://www.nexusmods.com/stardewvalley/mods/3296)** | Paritee | Custom farm animals (legacy) | ❌ Obsolete — use CP |
| **[Custom NPC Fixes](https://www.nexusmods.com/stardewvalley/mods/3849)** | spacechase0 | Fixes bugs with custom NPCs | ✅ Active |
| **[Expanded Preconditions Utility](https://www.nexusmods.com/stardewvalley/mods/6529)** | ChroniclerCherry | Advanced conditions for shop/event data | ✅ Active |

---

# Part II — Setting Up

## 5. Environment Setup

### Prerequisites

| Requirement | Notes |
|:---|:---|
| **Stardew Valley** | Steam, GOG, or any platform |
| **SMAPI** | Download from [smapi.io](https://smapi.io/) and run the installer |
| **.NET 6.0 SDK** | [Download](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) — the game targets net6.0 |
| **IDE** | Visual Studio Community, JetBrains Rider (both free), or VS Code |
| **ILSpy** | For decompiling game code (see [§26](#26-decompiling)) |
| **Tiled** | For map editing (see [§22](#22-map-editing)) |

### Installing SMAPI

1. Download the latest installer from [smapi.io](https://smapi.io/).
2. Run the installer — it auto-detects your game folder.
3. Launch via `StardewModdingAPI.exe` (or through Steam if configured).
4. SMAPI creates a `Mods/` folder — this is where all mods live.

### Folder Structure

```
Stardew Valley/
├── StardewModdingAPI.exe           ← Launch through this
├── Stardew Valley.dll              ← The game assembly
├── Content/                        ← Vanilla game assets (DO NOT edit)
│   ├── Data/                       ← Game data (JSON dictionaries)
│   ├── Maps/                       ← Tile maps (.tbin / .tmx)
│   ├── Characters/                 ← NPC sprites
│   ├── TileSheets/                 ← Tileset images
│   └── ...
├── smapi-internal/                 ← SMAPI runtime
└── Mods/                           ← All mods go here
    ├── ContentPatcher/             ← Framework mod
    ├── GenericModConfigMenu/       ← Framework mod
    ├── YourMod/                    ← Your mod folder
    │   ├── manifest.json
    │   ├── ModEntry.cs
    │   └── ...
    └── ...
```

### Anatomy of a C# Mod Folder

```
MyMod/
├── MyMod.csproj            ← .NET project file
├── manifest.json           ← SMAPI mod descriptor (REQUIRED)
├── ModEntry.cs             ← Entry point class (REQUIRED)
├── ModConfig.cs            ← Configuration POCO
├── config.json             ← Auto-generated by SMAPI from ModConfig defaults
├── i18n/                   ← Localization
│   ├── default.json        ← English (fallback)
│   └── vi.json             ← Other languages
├── IGenericModConfigMenuApi.cs  ← Duck-typed GMCM interface
├── *Patches.cs             ← Harmony patch classes
├── *Manager.cs / *Logic.cs ← Business logic
├── assets/                 ← Textures, sprites (optional)
├── content.json            ← Content Patcher definitions (optional)
└── README.md               ← Documentation
```

---

## 6. Project Configuration

Every C# mod starts with a `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>MyMod</AssemblyName>
    <RootNamespace>MyMod</RootNamespace>
    <Version>1.0.0</Version>
    <!-- MUST be net6.0 — the game's runtime -->
    <TargetFramework>net6.0</TargetFramework>
    <!-- Set true if using HarmonyLib -->
    <EnableHarmony>true</EnableHarmony>
    <Platforms>AnyCPU</Platforms>
    <!-- false = don't auto-copy DLL to Mods/ (useful when project IS in Mods/) -->
    <EnableModDeploy>false</EnableModDeploy>
    <!-- Output DLL directly to project folder -->
    <OutputPath>.</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- THE magic package: auto-references game DLLs, MonoGame, SMAPI, Harmony -->
    <PackageReference Include="Pathoschild.Stardew.ModBuildConfig" Version="4.1.1" />
  </ItemGroup>
</Project>
```

### What `Pathoschild.Stardew.ModBuildConfig` Does

- Auto-detects your Stardew Valley installation path.
- Adds references to `StardewValley.dll`, `StardewModdingAPI.dll`, MonoGame assemblies, `HarmonyLib`.
- On build, packages your mod into a release `.zip`.
- Sets `GamePath` automatically (override with `<GamePath>` if detection fails).

---

## 7. The Manifest

`manifest.json` tells SMAPI everything about your mod:

```json
{
  "Name": "My Cool Mod",
  "Author": "myname",
  "Version": "1.0.0",
  "Description": "Does something cool.",
  "UniqueID": "myname.MyCoolMod",
  "MinimumApiVersion": "4.0.0",
  "EntryDll": "MyCoolMod.dll",
  "Dependencies": [
    { "UniqueID": "spacechase0.GenericModConfigMenu", "IsRequired": false }
  ],
  "UpdateKeys": ["Nexus:12345"]
}
```

| Field | Purpose |
|:---|:---|
| `Name` | Human-readable name in SMAPI console |
| `Author` | Your name/handle |
| `Version` | Semver `X.Y.Z` — update every release |
| `UniqueID` | Globally unique, convention: `Author.ModName` |
| `MinimumApiVersion` | Minimum SMAPI version needed |
| `EntryDll` | Your compiled DLL filename |
| `Dependencies` | Other mods needed (soft or hard) |
| `UpdateKeys` | Where SMAPI checks for updates: `Nexus:ID`, `GitHub:user/repo` |
| `ContentPackFor` | For content packs: which framework mod this pack is for |

---

# Part III — SMAPI C# Mod Development

## 8. ModEntry

Your mod must contain exactly **one** class extending `StardewModdingAPI.Mod`:

```csharp
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MyMod
{
    public class ModEntry : Mod
    {
        public static ModConfig Config { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static ITranslationHelper I18n { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            // 1. Load config
            Config = helper.ReadConfig<ModConfig>();

            // 2. Store shared services for other classes
            ModMonitor = Monitor;
            I18n = helper.Translation;

            // 3. Apply Harmony patches (if needed)
            var harmony = new HarmonyLib.Harmony(ModManifest.UniqueID);
            MyPatches.Apply(harmony);

            // 4. Subscribe to SMAPI events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.Content.AssetRequested += OnAssetRequested;
        }
    }
}
```

### Your Toolkit (Properties from `Mod` Base Class)

| Property | Type | Purpose |
|:---|:---|:---|
| `Helper` | `IModHelper` | Events, content, data, reflection, mod registry |
| `Monitor` | `IMonitor` | Logging to SMAPI console |
| `ModManifest` | `IManifest` | Your manifest.json as a C# object |

### `IModHelper` Sub-APIs

| API | Access | Purpose |
|:---|:---|:---|
| Events | `Helper.Events.*` | Subscribe to game lifecycle events |
| Config | `Helper.ReadConfig<T>()` | Deserialize `config.json` |
| Translation | `Helper.Translation` | Read i18n strings |
| Game Content | `Helper.GameContent` | Load/invalidate game assets |
| Mod Content | `Helper.ModContent` | Load assets from your mod folder |
| Reflection | `Helper.Reflection` | Access private fields/methods safely |
| ModRegistry | `Helper.ModRegistry` | Check other mods, get their APIs |
| Data | `Helper.Data` | Read/write JSON per save or globally |

---

## 9. SMAPI Events

```
Helper.Events.
├── GameLoop                ← Game lifecycle
│   ├── GameLaunched              Title screen ready (register GMCM here)
│   ├── SaveLoaded                A save file was loaded
│   ├── DayStarted                New in-game day begins
│   ├── DayEnding                 Day is ending (before save)
│   ├── TimeChanged               In-game clock ticked (10-minute increments)
│   ├── UpdateTicked              Every game tick (~60/sec)
│   ├── OneSecondUpdateTicked     Once per real second
│   ├── Saving                    About to write save
│   ├── Saved                     Save completed
│   └── ReturnedToTitle           Player quit to title
│
├── Player                  ← Player actions
│   ├── Warped                    Player changed location
│   └── InventoryChanged          Items added/removed from inventory
│
├── Input                   ← Keyboard / mouse / controller
│   ├── ButtonPressed             Any button down
│   ├── ButtonReleased            Any button up
│   ├── CursorMoved               Mouse moved
│   └── MouseWheelScrolled        Scroll wheel
│
├── World                   ← World mutations
│   ├── ObjectListChanged         Objects placed/removed on map
│   ├── TerrainFeatureListChanged Trees/grass/etc changed
│   ├── NpcListChanged            NPCs added/removed
│   ├── BuildingListChanged       Buildings changed
│   └── LocationListChanged       Locations added/removed
│
├── Display                 ← Rendering hooks
│   ├── RenderingWorld            Before world is drawn
│   ├── RenderedWorld             After world is drawn
│   ├── RenderingHud              Before HUD is drawn (draw under HUD)
│   ├── RenderedHud               After HUD is drawn (draw over HUD)
│   ├── Rendering                 Before everything
│   ├── Rendered                  After everything
│   ├── MenuChanged               Active menu opened/closed/swapped
│   └── WindowResized             Game window resized
│
├── Content                 ← Asset pipeline
│   ├── AssetRequested            Game needs an asset (edit/replace here)
│   ├── AssetReady                Asset finished loading
│   └── AssetsInvalidated         Cached assets were invalidated
│
└── Multiplayer             ← Network
    ├── ModMessageReceived        Message from another mod
    ├── PeerConnected             Player joined
    └── PeerDisconnected          Player left
```

### Context Checks (Always Use These!)

```csharp
Context.IsWorldReady     // A save is loaded and the world exists
Context.IsPlayerFree     // Player can move (not in menu/cutscene)
Context.IsMainPlayer     // This is the host (not a farmhand)
Context.IsMultiplayer    // Multiple players connected
```

### Common Event Patterns

```csharp
// Run code once when the game reaches the title screen
helper.Events.GameLoop.GameLaunched += (s, e) => {
    // Register GMCM, check for other mods
};

// Run code when a day starts
helper.Events.GameLoop.DayStarted += (s, e) => {
    if (Game1.dayOfMonth == 1)
        Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
};

// Run periodic logic (sparingly!)
helper.Events.GameLoop.UpdateTicked += (s, e) => {
    if (e.IsMultipleOf(30) && Context.IsWorldReady)  // every 0.5 seconds
        DoPeriodicCheck();
};

// React to player changing location
helper.Events.Player.Warped += (s, e) => {
    if (e.NewLocation is MineShaft shaft && shaft.mineLevel > 120)
        ProcessSkullCavernFloor(shaft);
};
```

---

## 10. Configuration

```csharp
// ModConfig.cs — properties with defaults become config.json entries
public class ModConfig
{
    public bool EnableFeature { get; set; } = true;
    public int MaxItems { get; set; } = 8;
    public float DropChance { get; set; } = 0.5f;
}

// In ModEntry.cs:
Config = helper.ReadConfig<ModConfig>();   // load
Helper.WriteConfig(Config);                // save
```

SMAPI auto-generates `config.json` from your defaults. Players edit it manually or via GMCM.

---

## 11. Localization

Place translation files in `i18n/`:

| File | Language |
|:---|:---|
| `default.json` | English (fallback) |
| `de.json` | German |
| `es.json` | Spanish |
| `fr.json` | French |
| `ja.json` | Japanese |
| `ko.json` | Korean |
| `pt.json` | Portuguese |
| `ru.json` | Russian |
| `vi.json` | Vietnamese |
| `zh.json` | Chinese |

```json
// i18n/default.json
{
  "config.enable.name": "Enable Feature",
  "config.enable.tooltip": "Turn this feature on or off.",
  "message.greeting": "Hello, {{name}}! You have {{count}} items."
}
```

```csharp
// Usage in code
string label = Helper.Translation.Get("config.enable.name");
string msg = Helper.Translation.Get("message.greeting",
    new { name = Game1.player.Name, count = 42 });
```

---

## 12. GMCM Integration

Integrate with [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) via a **duck-typed interface** (no DLL dependency):

```csharp
// IGenericModConfigMenuApi.cs — local mirror of GMCM's API
public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue,
                       Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue,
                         Func<string> name, Func<string>? tooltip = null,
                         int? min = null, int? max = null, int? interval = null,
                         Func<int, string>? formatValue = null, string? fieldId = null);
    void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue,
                         Func<string> name, Func<string>? tooltip = null,
                         float? min = null, float? max = null, float? interval = null,
                         Func<float, string>? formatValue = null, string? fieldId = null);
}
```

```csharp
// In OnGameLaunched:
var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
if (gmcm is null) return;  // GMCM not installed — graceful fallback

gmcm.Register(mod: ModManifest,
    reset: () => Config = new ModConfig(),
    save: () => Helper.WriteConfig(Config));

gmcm.AddBoolOption(mod: ModManifest,
    name:    () => I18n.Get("config.enable.name"),
    tooltip: () => I18n.Get("config.enable.tooltip"),
    getValue: () => Config.EnableFeature,
    setValue: v => Config.EnableFeature = v);
```

**How it works:** `GetApi<T>` asks SMAPI to create a proxy object matching your interface using the actual GMCM mod's public methods. If GMCM isn't installed, it returns `null`.

---

## 13. Harmony Patching

When SMAPI events don't expose what you need, **Harmony** lets you inject code into game methods at runtime.

> [!WARNING]
> Harmony is powerful but fragile. Game updates can break patches. Prefer SMAPI events when possible. Always wrap patches in `try/catch`.

### Prefix (runs BEFORE original, can skip it)

```csharp
public static class EnchantmentPatches
{
    public static void Apply(Harmony harmony)
    {
        try
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(BaseEnchantment),
                    nameof(BaseEnchantment.GetEnchantmentFromItem),
                    new[] { typeof(Item), typeof(Item) }),
                prefix: new HarmonyMethod(typeof(EnchantmentPatches),
                    nameof(GetEnchantment_Prefix))
            );
        }
        catch (Exception ex) { Monitor.Log($"Patch failed: {ex}", LogLevel.Error); }
    }

    // Return false → skip original method. __result (ref) → set return value.
    public static bool GetEnchantment_Prefix(Item base_item, Item item,
        ref BaseEnchantment? __result)
    {
        if (!Config.UniformChances) return true;  // true = run original

        var candidates = BaseEnchantment.GetAvailableEnchantmentsForItem(base_item as Tool);
        __result = Game1.random.ChooseFrom(candidates);
        return false;  // false = SKIP original
    }
}
```

### Postfix (runs AFTER original)

```csharp
public static void Apply(Harmony harmony)
{
    harmony.Patch(
        original: AccessTools.Method(typeof(FishingRod),
            nameof(FishingRod.openTreasureMenuEndFunction)),
        postfix: new HarmonyMethod(typeof(FishingPatches),
            nameof(TreasureMenu_Postfix))
    );
}

// __instance = the object whose method was called (auto-injected)
public static void TreasureMenu_Postfix(FishingRod __instance)
{
    if (Game1.activeClickableMenu is ItemGrabMenu menu)
        EnhanceFishingChest(__instance, menu);
}
```

### Harmony Special Parameters

| Parameter | Purpose |
|:---|:---|
| `__instance` | The object whose method was called |
| `__result` | The method's return value (`ref` to modify it) |
| `__state` | Pass data from prefix to postfix |
| `___fieldName` | Access a private field (triple underscore + name) |

---

## 14. Editing Game Assets

Subscribe to `Content.AssetRequested` to edit/replace any game asset:

### Edit Data Dictionaries

```csharp
if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
{
    e.Edit(asset => {
        var data = asset.AsDictionary<string, BigCraftableData>().Data;
        if (data.TryGetValue("Anvil", out var anvil))
            anvil.Description = I18n.Get("anvil.description");
    });
}
```

### Edit Maps

```csharp
if (e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse2"))
{
    e.Edit(asset => {
        var map = asset.AsMap().Data;
        var buildings = map.GetLayer("Buildings");
        buildings.Tiles[x, y] = null;  // Remove a tile
    });
}
```

### Load Custom Textures

```csharp
if (e.NameWithoutLocale.IsEquivalentTo("Mods/mymod/CustomSprite"))
{
    e.LoadFromModFile<Texture2D>("assets/sprite.png", AssetLoadPriority.Medium);
}
```

### Patch Images (Spritesheet Overlays)

```csharp
if (e.NameWithoutLocale.IsEquivalentTo("Maps/walls_and_floors"))
{
    e.Edit(asset => {
        var editor = asset.AsImage();
        var tex = Helper.ModContent.Load<Texture2D>("assets/wallpaper.png");
        editor.PatchImage(tex,
            sourceArea: new Rectangle(0, 0, 16, 48),
            targetArea: new Rectangle(176, 0, 16, 48));
    });
}
```

### Invalidate Cached Assets

Force the game to reload an asset (e.g., after config change):

```csharp
Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
```

---

## 15. SMAPI Reflection

Access private fields/methods safely (survives minor game updates):

```csharp
// Read a private field
var netField = Helper.Reflection
    .GetField<NetBool>(shaft, "netIsTreasureRoom", required: false);
bool isTreasure = netField?.GetValue()?.Value ?? false;

// Call a private method
var method = Helper.Reflection
    .GetMethod(Game1.player, "SomePrivateMethod");
method.Invoke(arg1, arg2);
```

---

## 16. Multiplayer

```csharp
// Send a message to all connected players
Helper.Multiplayer.SendMessage(
    message: new MyData { Value = 42 },
    messageType: "MyMod.DataSync",
    modIDs: new[] { ModManifest.UniqueID }
);

// Receive messages
Helper.Events.Multiplayer.ModMessageReceived += (s, e) => {
    if (e.FromModID == ModManifest.UniqueID && e.Type == "MyMod.DataSync")
    {
        var data = e.ReadAs<MyData>();
    }
};
```

Key rules:
- Use `Context.IsMainPlayer` to restrict host-only logic.
- Game state (`Game1.player`) is per-player — don't assume singleplayer.
- `modData` on objects is synced in save files but not live.

---

## 17. Console Commands

Register custom SMAPI console commands:

```csharp
Helper.ConsoleCommands.Add("mymod_debug", "Prints debug info", (cmd, args) => {
    Monitor.Log($"Player: {Game1.player.Name}, Money: {Game1.player.Money}", LogLevel.Info);
});
```

---

# Part IV — Content Patcher (No C# Required)

## 18. Content Patcher

[Content Patcher](https://github.com/Pathoschild/StardewMods/blob/stable/ContentPatcher/docs/author-guide.md) lets you create mods with **JSON only** — no C# code needed.

### Structure

```
MyContentPack/
├── manifest.json
├── content.json          ← Edit instructions
├── assets/
│   └── my_sprite.png
└── i18n/
    └── default.json
```

### manifest.json

```json
{
  "Name": "My Content Pack",
  "Author": "me",
  "Version": "1.0.0",
  "UniqueID": "me.MyContentPack",
  "ContentPackFor": { "UniqueID": "Pathoschild.ContentPatcher" }
}
```

### content.json Actions

| Action | Purpose | Example |
|:---|:---|:---|
| `Load` | Provide a completely new asset | Custom sprite, new map |
| `EditData` | Add/edit entries in data dictionaries | New items, shop entries |
| `EditImage` | Overlay patches on spritesheets | Recolor, replace sprites |
| `EditMap` | Patch map tiles/properties | Add warps, modify terrain |

### Example content.json

```json
{
  "Format": "2.4.0",
  "Changes": [
    {
      "Action": "Load",
      "Target": "Mods/me.MyPack/CustomSprite",
      "FromFile": "assets/my_sprite.png"
    },
    {
      "Action": "EditData",
      "Target": "Data/Furniture",
      "Entries": {
        "me.MyPack.Chair": "My Chair/chair/1 2/1 1/1/500/-1/My Chair"
      }
    },
    {
      "Action": "EditImage",
      "Target": "Maps/walls_and_floors",
      "FromFile": "assets/wallpaper.png",
      "ToArea": { "X": 176, "Y": 0, "Width": 16, "Height": 48 },
      "When": { "Season": "Spring" }
    },
    {
      "Action": "EditData",
      "Target": "Data/Shops",
      "TargetField": ["SeedShop", "Items"],
      "Entries": {
        "me.MyPack.Chair": {
          "Id": "me.MyPack.Chair",
          "ItemId": "(F)me.MyPack.Chair",
          "Price": 500
        }
      }
    }
  ]
}
```

### Conditions (`When`)

Content Patcher supports powerful conditions:

```json
{
  "Action": "EditImage",
  "Target": "Characters/Abigail",
  "FromFile": "assets/abigail_winter.png",
  "When": {
    "Season": "Winter",
    "Hearts:Abigail": "{{Range: 8, 14}}",
    "HasMod": "author.AnotherMod"
  }
}
```

---

## 19. Trigger Actions & Game State Queries (1.6)

### Trigger Actions

1.6 introduced a system to perform actions from data (dialogue, events, mail) without C#:

```
# In dialogue
"Mon": "Here's a gift!#$action AddItem (O)74"

# In events
/action AddItem (O)74 5

# In mail
%action AddItem (O)74%%
```

C# mods can register custom triggers:
```csharp
TriggerActionManager.RegisterTrigger("MyMod.CustomTrigger");
TriggerActionManager.Raise("MyMod.CustomTrigger");
```

### Game State Queries (GSQ)

Condition strings usable in data fields:

```
SEASON Spring                     → is it Spring?
!SEASON Spring                    → is it NOT Spring?
WEATHER Here Sun                  → is it sunny at current location?
PLAYER_HAS_MAIL Current Visited_Island → has the player visited the island?
PLAYER_STAT Current timesEnchanted 5   → enchanted 5+ times?
```

Multiple conditions are comma-separated (AND logic):
```
"Condition": "!SEASON Spring, WEATHER Here Sun, PLAYER_COMBAT_LEVEL Current 8"
```

---

# Part V — Game Data & Assets

## 20. Data Architecture (1.6)

Stardew Valley 1.6 moved to a **data-driven** architecture. Most game content is defined in JSON dictionaries under `Content/Data/`:

| Data Asset | Contents | Format |
|:---|:---|:---|
| `Data/Objects` | All standard items | `Dict<string, ObjectData>` |
| `Data/Weapons` | All weapons | `Dict<string, WeaponData>` |
| `Data/BigCraftables` | Machines, stations | `Dict<string, BigCraftableData>` |
| `Data/Characters` | NPC definitions (unified in 1.6) | `Dict<string, CharacterData>` |
| `Data/Furniture` | Furniture items | `Dict<string, string>` (delimited) |
| `Data/Crops` | Crop growth rules | `Dict<string, CropData>` |
| `Data/Machines` | Machine processing rules | `Dict<string, MachineData>` |
| `Data/Shops` | Shop inventories | `Dict<string, ShopData>` |
| `Data/FarmAnimals` | Farm animal types | `Dict<string, FarmAnimalData>` |
| `Data/PassiveFestivals` | Festival schedules | `Dict<string, PassiveFestivalData>` |
| `Data/Buffs` | Buff definitions | `Dict<string, BuffData>` |
| `Data/Locations` | Location metadata | `Dict<string, LocationData>` |

### 1.6 Key Changes

- **String IDs** replaced numeric IDs (no more item ID conflicts between mods).
- **`Data/Characters`** unified `NPCDispositions`, `spousePatios`, `spouseRooms`.
- **`Data/Weapons`** is now fully data-driven (was hardcoded).
- **XNB editing is obsolete** — use Content Patcher or SMAPI Content API.

---

## 21. Qualified Item IDs

Items in 1.6 use **qualified IDs** — a type prefix + string ID:

| Prefix | Item Type | Example |
|:---|:---|:---|
| `(O)` | Object | `(O)74` (Prismatic Shard), `(O)mymod_Crop` |
| `(BC)` | Big Craftable | `(BC)Anvil`, `(BC)21` (Keg) |
| `(F)` | Furniture | `(F)mymod.Chair` |
| `(W)` | Weapon | `(W)4` (Galaxy Sword) |
| `(B)` | Boots | `(B)504` |
| `(H)` | Hat | `(H)6` |
| `(S)` | Shirt | `(S)1000` |
| `(P)` | Pants | `(P)0` |
| `(T)` | Tool | `(T)IridiumAxe` |
| `(TR)` | Trinket | `(TR)FrogEgg` |

```csharp
// Creating items by qualified ID
Item shard = ItemRegistry.Create("(O)74", amount: 1);
Item sword = ItemRegistry.Create("(W)4");

// Checking an item's qualified ID
if (item.QualifiedItemId == "(O)74") { /* Prismatic Shard */ }
```

---

## 22. Map Editing with Tiled

Stardew Valley maps use the **xTile** engine, but you edit them with **[Tiled](https://www.mapeditor.org/)**.

### Setup

1. Download [Tiled](https://www.mapeditor.org/download.html).
2. Enable the `.tbin` plugin: **Edit → Preferences → Plugins → tbin**.
3. Copy vanilla maps from `Content/Maps/` to a working folder (never edit originals).
4. Keep tilesheet `.png` files in the same folder as your map.

### Map Layers

| Layer | Purpose |
|:---|:---|
| `Back` | Ground terrain (grass, water, paths) |
| `Buildings` | Collision objects (walls, fences, bridges) |
| `Paths` | Flooring, removable debris |
| `Front` | Drawn above the player (tree tops) |
| `AlwaysFront` | Always on top (foliage overlays) |

### Tile Properties

Set on individual tiles or objects to control game behavior:

| Property | Layer | Effect |
|:---|:---|:---|
| `Passable T` | Buildings | Player can walk through this tile |
| `Water T` | Back | Tile is water (fishing works here) |
| `Action Warp X Y MapName` | Buildings | Teleports player |
| `Action Door` | Buildings | Shows door animation |
| `TouchAction MagicWarp X Y MapName` | Back | Teleports on step |
| `NoFurniture T` | Back | Can't place furniture here |

### Deploying Map Edits

Use **Content Patcher** to patch maps without replacing files:

```json
{
  "Action": "EditMap",
  "Target": "Maps/FarmHouse2",
  "FromFile": "assets/my_farmhouse_patch.tmx",
  "ToArea": { "X": 0, "Y": 0, "Width": 10, "Height": 10 }
}
```

Or use the SMAPI C# Content API (see [§14](#14-editing-game-assets)).

---

## 23. Sprites & Textures

### How Spritesheets Work

The game uses spritesheets (texture atlases) — many sprites packed into one image:

```
┌──────────────────────────────────────────────┐
│ [0,0]──16px──[16,0]──16px──[32,0]           │
│ │ Sprite 0 │ │ Sprite 1 │ │ Sprite 2 │ ... │
│ [0,16]      [16,16]      [32,16]            │
│ │ Sprite 3 │ │ Sprite 4 │ ...               │
└──────────────────────────────────────────────┘
```

A `sourceRectangle` selects which sprite to draw:
```csharp
var sourceRect = new Rectangle(x: 32, y: 0, width: 16, height: 16); // Sprite 2
spriteBatch.Draw(spritesheet, destinationPosition, sourceRect, Color.White);
```

### Adding Custom Sprites

1. Create a `.png` spritesheet in your `assets/` folder.
2. Load it via Content API or Content Patcher.
3. Reference it by asset name from game data.

### Image Formats

- **Sprites**: 16×16 or 16×32 px per frame (characters are 16×32).
- **Furniture**: Variable sizes (defined in `Data/Furniture` dimensions).
- **Big Craftables**: 16×32 px.
- **Tile sheets**: 16×16 px per tile, arbitrary sheet size.

---

# Part VI — Key Game APIs

## 24. Essential Types

| Type | Namespace | Purpose |
|:---|:---|:---|
| `Game1` | `StardewValley` | **The** singleton — entire game state |
| `Game1.player` | | Local `Farmer` object |
| `Game1.currentLocation` | | Current `GameLocation` |
| `Game1.activeClickableMenu` | | Currently open menu (or null) |
| `Game1.random` | | Shared `Random` instance |
| `Game1.content` | | XNA `ContentManager` for loading assets |
| `Farmer` | `StardewValley` | Player (inventory, stats, mail flags, skills) |
| `GameLocation` | `StardewValley` | A map/area — has `Objects`, `Characters`, `terrainFeatures` |
| `StardewValley.Object` | `StardewValley` | In-world object (machine, forage, crafted item) |
| `Item` | `StardewValley` | Base class for all items |
| `Tool` | `StardewValley` | Base for Axe, Pickaxe, Hoe, FishingRod, etc. |
| `Chest` | `StardewValley.Objects` | Container with items |
| `MineShaft` | `StardewValley.Locations` | A mine/skull cavern floor |
| `NPC` | `StardewValley` | Non-player character |
| `Vector2` | `Microsoft.Xna.Framework` | 2D position (also used as tile coordinate key) |
| `Rectangle` | `Microsoft.Xna.Framework` | Sprite source/target area |
| `Texture2D` | `Microsoft.Xna.Framework.Graphics` | A texture/image |
| `Color` | `Microsoft.Xna.Framework` | RGBA color |
| `Context` | `StardewModdingAPI` | Game state flags |
| `ItemRegistry` | `StardewValley.Internal` | Create items by qualified ID |
| `PathUtilities` | `StardewModdingAPI.Utilities` | Cross-platform path/asset normalization |

---

## 25. Common Interactions

```csharp
// === Items ===
Item shard = ItemRegistry.Create("(O)74", amount: 1);      // Create an item
Game1.player.addItemToInventory(shard);                     // Give to player
Game1.player.removeItemsFromInventory("(O)74", 5);          // Remove items

// === Checking progress ===
bool hasIsland = Game1.MasterPlayer.hasOrWillReceiveMail("Visited_Island");
bool hasMastery = Game1.player.stats.Get("masteryLevelsSpent") > 0;
int combatLevel = Game1.player.CombatLevel;

// === Objects on the map ===
foreach (var pair in location.Objects.Pairs)
{
    Vector2 tile = pair.Key;
    StardewValley.Object obj = pair.Value;
}

// === Custom mod data (persists in save) ===
chest.modData["mymod.UniqueID/IsProcessed"] = "true";
bool processed = chest.modData.ContainsKey("mymod.UniqueID/IsProcessed");

// === HUD messages ===
Game1.addHUDMessage(new HUDMessage("You found treasure!", HUDMessage.achievement_type));

// === Warping ===
Game1.warpFarmer("Town", 30, 30, false);

// === Opening menus ===
Game1.activeClickableMenu = new DialogueBox("Hello, world!");

// === Playing sound ===
Game1.playSound("coin");

// === Invalidating assets ===
Helper.GameContent.InvalidateCache("Data/Objects");
```

---

# Part VII — Ship It

## 26. Decompiling

Use [ILSpy](https://github.com/icsharpcode/ILSpy/releases) to read `Stardew Valley.dll`:

1. Open `Stardew Valley.dll` from your game folder.
2. Set language to **C#** (not IL).
3. Navigate class tree → find the method you want to understand/patch.
4. Right-click → **Save Code** to export entire decompiled project.

```powershell
# Command-line alternative:
dotnet tool install --global ilspycmd
ilspycmd -p --nested-directories -r "Stardew Valley.dll" -o ./decompiled
```

---

## 27. Building & Testing

```powershell
# Build from repo root
dotnet build MyMod/MyMod.csproj

# DLL outputs directly to mod folder → launch game → SMAPI loads it
```

Workflow: **Edit → Build → Close game → Relaunch** (no hot reload).

---

## 28. Debugging

### Logging

```csharp
Monitor.Log("Info message", LogLevel.Info);
Monitor.Log($"Player has {Game1.player.Money}g", LogLevel.Debug);
Monitor.Log($"Critical error: {ex}", LogLevel.Error);
```

| Level | Visibility |
|:---|:---|
| `Trace` | Hidden by default (verbose) |
| `Debug` | Dev messages |
| `Info` | Normal |
| `Warn` | Non-critical issues |
| `Error` | Something broke |
| `Alert` | Critical |

### SMAPI Console Commands

```
help                        List commands
debug warp Town 30 30       Teleport
debug item (O)74            Give Prismatic Shard
debug money 999999          Set gold
debug speed 5               Set speed
debug time 600              Set to 6:00 AM
```

### Visual Studio Attach

**Debug → Attach to Process → StardewModdingAPI.exe** → set breakpoints.

### Log Files

SMAPI log: `%appdata%\StardewValley\ErrorLogs\SMAPI-latest.txt`
Share via: [smapi.io/log](https://smapi.io/log)

---

## 29. Releasing

### Version Sync

Update in **three** places:
1. `manifest.json` → `"Version": "X.Y.Z"`
2. `.csproj` → `<Version>X.Y.Z</Version>`
3. `README.md` version table (if applicable)

### Build Produces a ZIP

`Pathoschild.Stardew.ModBuildConfig` auto-creates `MyMod X.Y.Z.zip` on build.

### Publish To

| Platform | URL |
|:---|:---|
| **Nexus Mods** | [nexusmods.com/stardewvalley](https://www.nexusmods.com/stardewvalley/) |
| **ModDrop** | [moddrop.com/stardew-valley](https://www.moddrop.com/stardew-valley/) |
| **GitHub** | Your repo's Releases page |

Add to `manifest.json`:
```json
"UpdateKeys": ["Nexus:12345", "GitHub:username/repo"]
```

SMAPI auto-checks for updates and alerts players.

---

## 30. Common Pitfalls

| ❌ Mistake | ✅ Fix |
|:---|:---|
| Hardcoding English strings in GMCM | Use `I18n.Get("key")` |
| Creating Console App instead of Class Library | Use **Class Library** project template |
| Accessing `Game1.player` in `Entry()` | Wait for `Context.IsWorldReady` in an event handler |
| Hardcoding `\\` path separators | Use `Path.Combine()` |
| Unprotected Harmony patches | Wrap in `try/catch` |
| Comparing asset names with `Path.Combine` | Use `PathUtilities.NormalizeAssetName()` |
| Editing files in `Content/` directly | Use Content Patcher or SMAPI Content API |
| Using XNB replacement | Migrate to Content Patcher |
| Heavy logic in `Display.Rendered` events | Move logic to `GameLoop.UpdateTicked` |
| Forgetting `ref` on `__result` in Harmony | Declare `ref ReturnType __result` |
| Not null-checking game fields | Always null-check: `location?.Objects` |
| Putting game logic in multiplayer without host check | Use `Context.IsMainPlayer` for host-only code |

---

## 31. External Resources

### Official Documentation

| Resource | URL |
|:---|:---|
| **SMAPI Modder Guide** | [stardewvalleywiki.com/Modding:Modder_Guide](https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started) |
| **SMAPI API Reference** | [stardewvalleywiki.com/.../APIs](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs) |
| **Events Reference** | [stardewvalleywiki.com/.../Events](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Events) |
| **Harmony Patching** | [stardewvalleywiki.com/.../Harmony](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony) |
| **Content API** | [stardewvalleywiki.com/.../Content](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Content) |
| **Config API** | [stardewvalleywiki.com/.../Config](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Config) |
| **Translation API** | [stardewvalleywiki.com/.../Translation](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Translation) |
| **Game State Queries** | [stardewvalleywiki.com/Modding:Game_state_queries](https://stardewvalleywiki.com/Modding:Game_state_queries) |
| **Trigger Actions** | [stardewvalleywiki.com/Modding:Trigger_actions](https://stardewvalleywiki.com/Modding:Trigger_actions) |
| **Item Data (1.6)** | [stardewvalleywiki.com/Modding:Items](https://stardewvalleywiki.com/Modding:Items) |
| **NPC Data (1.6)** | [stardewvalleywiki.com/Modding:NPC_data](https://stardewvalleywiki.com/Modding:NPC_data) |
| **Map Modding** | [stardewvalleywiki.com/Modding:Maps](https://stardewvalleywiki.com/Modding:Maps) |
| **1.6 Migration Guide** | [stardewvalleywiki.com/Modding:Migrate_to_1.6](https://stardewvalleywiki.com/Modding:Migrate_to_Stardew_Valley_1.6) |
| **Content Patcher Docs** | [github.com/.../ContentPatcher](https://github.com/Pathoschild/StardewMods/blob/stable/ContentPatcher/docs/author-guide.md) |
| **Harmony Library Wiki** | [harmony.pardeike.net](https://harmony.pardeike.net/articles/intro.html) |
| **SMAPI Source Code** | [github.com/Pathoschild/SMAPI](https://github.com/Pathoschild/SMAPI) |

### Community

| Resource | URL |
|:---|:---|
| **Stardew Valley Discord** | `#making-mods` channel |
| **r/SMAPI** | [reddit.com/r/SMAPI](https://www.reddit.com/r/SMAPI/) |
| **Mod Compatibility List** | [smapi.io/mods](https://smapi.io/mods/) |
| **SMAPI Log Parser** | [smapi.io/log](https://smapi.io/log) |
| **Nexus Mods** | [nexusmods.com/stardewvalley](https://www.nexusmods.com/stardewvalley/) |

### Tools

| Tool | Purpose |
|:---|:---|
| [ILSpy](https://github.com/icsharpcode/ILSpy/releases) | Decompile `Stardew Valley.dll` |
| [Tiled](https://www.mapeditor.org/) | Edit tile maps (.tbin/.tmx) |
| [GMCM](https://www.nexusmods.com/stardewvalley/mods/5098) | In-game config menu |
| [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) | JSON-based asset editing |
| [Lookup Anything](https://www.nexusmods.com/stardewvalley/mods/541) | In-game debug inspector |
| [Debug Mode](https://www.nexusmods.com/stardewvalley/mods/679) | Tile coordinates, cursor info |

---

> **Happy modding! 🌟** Start with Content Patcher for simple changes, graduate to SMAPI events for custom logic, and reach for Harmony only when the game doesn't expose what you need. Decompile early and often — reading the game's code is the fastest way to learn.
