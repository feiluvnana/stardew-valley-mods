// =============================================================================
//  BetterMap — main entry point.
//  Every SMAPI mod needs exactly ONE public class derived from `Mod` (this is
//  it). SMAPI finds the class via manifest.json's "EntryDll", instantiates it,
//  and calls Entry(...) once when the game boots. Everything else this mod
//  does happens through EVENTS we subscribe to below.
// =============================================================================
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Common;

namespace BetterMap
{
    /// <summary>
    /// SMAPI's hook into BetterMap: loads config, subscribes to game events,
    /// rewrites map assets as the game requests them, and wires up the Generic
    /// Mod Config Menu settings page.
    /// </summary>
    /// <remarks>
    /// INHERITANCE: writing `class ModEntry : Mod` means ModEntry EXTENDS
    /// SMAPI's Mod base class, inheriting useful members such as `Helper`
    /// (file/event/registry access), `Monitor` (console logging), and
    /// `ModManifest` (this mod's identity card). The keyword `override` marks
    /// methods that replace virtual ones from the base class.
    /// </remarks>
    public class ModEntry : Mod
    {
        /// <summary>Globally reachable pointer to the one-and-only ModEntry instance.</summary>
        /// <remarks>
        /// SINGLETON pattern: only one entry object ever exists, so a `static`
        /// field (attached to the CLASS itself, not to an object) lets other
        /// code grab it without passing references around.
        /// The `= null!` initializer uses C#'s null-forgiving operator (`!`) to
        /// silence the compiler's "might be null" warning — we promise that
        /// Entry() assigns a real value before anyone reads it.
        /// </remarks>
        public static ModEntry Instance { get; private set; } = null!;
        /// <summary>The loaded user settings (config.json) for this mod.</summary>
        /// <remarks>`private set` means outside code may READ this property,
        /// but only this class may replace the whole object. GMCM mutates the
        /// individual flags INSIDE it rather than swapping the object.</remarks>
        public ModConfig Config { get; private set; } = null!;

        /// <summary>
        /// Called once by SMAPI at game startup: initialize state and hook events.
        /// </summary>
        /// <param name="helper">SMAPI's toolkit for files, events, translations,
        /// and more; also available later as the inherited `Helper` property.</param>
        public override void Entry(IModHelper helper)
        {
            // Remember our single instance in the shared field (see Instance docs).
            Instance = this;
            // Deserialize config.json into a ModConfig object.
            // `<ModConfig>` is a GENERIC type argument telling SMAPI which
            // class to construct and fill from the JSON file.
            Config = Helper.ReadConfig<ModConfig>();

            // Subscribe handler methods to SMAPI events with += ("event subscription").
            // AssetRequested fires every time the game asks for a content asset
            // (a map, a data table...) — our chance to edit it before use.
            Helper.Events.Content.AssetRequested += OnAssetRequested;
            // GameLaunched fires once after ALL mods have finished loading —
            // the safe moment to talk to other mods' APIs (like GMCM).
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            // Info-level message shown in the SMAPI console at startup.
            Monitor.Log("BetterMap loaded successfully.", LogLevel.Info);
        }

        /// <summary>
        /// Runs whenever ANY asset is requested; registers edits for each of
        /// the farmhouse/island maps this mod cares about.
        /// </summary>
        /// <remarks>
        /// EVENT HANDLER SHAPE: SMAPI events expect methods like
        /// `(object? sender, SomeEventArgs e)`; the `?` on `sender` means it is
        /// allowed to be null. Calling e.Edit(...) here does NOT edit anything
        /// immediately — it REGISTERS a callback (the lambda in braces) that
        /// SMAPI executes at the exact moment that asset loads, keeping edits
        /// lazy, repeatable, and compatible with other editing mods.
        /// </remarks>
        /// <param name="sender">Who raised the event (usually SMAPI itself).</param>
        /// <param name="e">Which asset was requested, plus editing tools.</param>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // e.NameWithoutLocale strips language suffixes from the asset name;
            // IsEquivalentTo compares case-insensitively and ignores versions.
            // "Maps/Island_W" is Ginger Island West — the island farm area.
            if (e.NameWithoutLocale.IsEquivalentTo("Maps/Island_W"))
            {
                // `asset => { ... }` is a LAMBDA: an inline, unnamed method
                // passed as data. It "captures" Config and Monitor from the
                // surrounding scope (a CLOSURE) so the patcher can use them
                // whenever SMAPI finally runs this edit.
                e.Edit(asset =>
                {
                    // Wrap the generic asset handle in a map-specific editor:
                    // IAssetData.AsMap() exposes xTile-specific helpers.
                    var editor = asset.AsMap();
                    // editor.Data is the live xTile Map object — patch in place.
                    MapPatcher.PatchIslandWest(editor.Data, Config, Monitor);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/IslandFarmHouse"))
            {
                // Interior of the farmhouse on Ginger Island.
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchIslandFarmHouse(editor.Data, Config, Monitor);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse"))
            {
                // The starter cabin most players begin the game in.
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchFarmHouse(editor.Data, Config, Monitor);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse1") || e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse1_marriage"))
            {
                // First farmhouse upgrade — "_marriage" is the variant where a
                // spouse lives there; both share the same door position.
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchFarmHouse1(editor.Data, Config, Monitor);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse2") || e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse2_marriage"))
            {
                // Second upgrade — again patched together with its marriage twin.
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchFarmHouse2(editor.Data, Config, Monitor);
                });
            }
        }

        /// <summary>After every mod has loaded, register our GMCM settings page.</summary>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Deferred until now so GMCM has definitely finished loading too.
            RegisterGenericModConfigMenu();
        }

        /// <summary>
        /// Connects to the Generic Mod Config Menu through our mirrored API
        /// interface (Common/IGenericModConfigMenuApi.cs) and adds BetterMap's
        /// two toggles to the in-game settings screen.
        /// </summary>
        private void RegisterGenericModConfigMenu()
        {
            // Ask SMAPI for GMCM's live object, typed as OUR mirror interface.
            // Returns null when GMCM isn't installed; the pattern check below
            // then exits early — no settings page, but also no crash.
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // Named arguments (mod:, reset:, save:) label each value for readability.
            // `mod:` passes our manifest identifying the page owner;
            // `reset:` / `save:` are statement lambdas run when the player
            // clicks the matching button in the menu.
            configMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    // Brand-new object = stock defaults...
                    Config = new ModConfig();
                    // ...persisted straight to config.json...
                    Helper.WriteConfig(Config);
                    // ...and applied instantly, no game restart needed.
                    ReloadMaps();
                },
                save: () =>
                {
                    // Flush current toggles to disk, then reapply the maps.
                    Helper.WriteConfig(Config);
                    ReloadMaps();
                }
            );

            // Mod description on Root Page
            configMenu.AddParagraph(
                mod: ModManifest,
                text: () => Helper.Translation.Get("mod.description")
            );

            // Section: Ginger Island Farm
            // A bold heading grouping the options below it. The strings come
            // from i18n/*.json via the Translation helper (localization-ready);
            // they are Func<string> lambdas so GMCM re-reads them on open.
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => Helper.Translation.Get("gmcm.section.ginger_island.title"),
                tooltip: () => Helper.Translation.Get("gmcm.section.ginger_island.description")
            );

            // A checkbox wired to Config.RemoveFarmDriftwoodBarrier:
            // getValue pulls the current value for display; setValue receives
            // the player's new value (`value => ...` is an arrow-style lambda).
            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => Config.RemoveFarmDriftwoodBarrier,
                setValue: value => Config.RemoveFarmDriftwoodBarrier = value,
                name: () => Helper.Translation.Get("gmcm.remove_farm_driftwood_barrier.name"),
                tooltip: () => Helper.Translation.Get("gmcm.remove_farm_driftwood_barrier.tooltip")
            );

            // Section: Farmhouse Doorways
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => Helper.Translation.Get("gmcm.section.farmhouse.title"),
                tooltip: () => Helper.Translation.Get("gmcm.section.farmhouse.description")
            );

            // Second checkbox, same delegate wiring pattern as above.
            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => Config.WidenHouseExit,
                setValue: value => Config.WidenHouseExit = value,
                name: () => Helper.Translation.Get("gmcm.widen_house_exit.name"),
                tooltip: () => Helper.Translation.Get("gmcm.widen_house_exit.tooltip")
            );
        }

        /// <summary>
        /// Drops every map this mod patches out of the game's asset cache so
        /// the next request reloads them from scratch — which re-triggers our
        /// AssetRequested edits immediately (used after config changes).
        /// </summary>
        /// <remarks>
        /// WHY THIS IS NEEDED: loaded assets are CACHED — the game reuses its
        /// copy instead of asking for them again. Without invalidating, changed
        /// settings would only appear after a full game restart.
        /// </remarks>
        private void ReloadMaps()
        {
            // One InvalidateCache call per edited asset name; the game will
            // re-request each one when a location using it loads.
            Helper.GameContent.InvalidateCache("Maps/Island_W");
            Helper.GameContent.InvalidateCache("Maps/IslandFarmHouse");
            Helper.GameContent.InvalidateCache("Maps/FarmHouse");
            Helper.GameContent.InvalidateCache("Maps/FarmHouse1");
            Helper.GameContent.InvalidateCache("Maps/FarmHouse1_marriage");
            Helper.GameContent.InvalidateCache("Maps/FarmHouse2");
            Helper.GameContent.InvalidateCache("Maps/FarmHouse2_marriage");
            // Debug level stays invisible unless verbose logging is enabled.
            Monitor.Log("BetterMap: Invalidated map cache and reloaded maps.", LogLevel.Debug);
        }
    }
}
