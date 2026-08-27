// =====================================================================================
// ModEntry.cs - the mod's ENTRY POINT (its "main" file).
//
// Every SMAPI mod needs exactly one class that INHERITS from StardewModdingAPI.Mod.
// When the game boots, SMAPI finds that class, constructs it, and calls its Entry()
// method once. Everything this mod does gets wired up from there: config loading,
// event subscriptions, and the Generic Mod Config Menu registration.
//
// The "using" lines at the top import namespaces so code below can use short names:
//   StardewModdingAPI         -> SMAPI core types: Mod, IMonitor, IModHelper, LogLevel
//   StardewModdingAPI.Events  -> event argument types such as GameLaunchedEventArgs
//   Common                    -> local shared types (the GenericModConfigMenu API wrapper)
// =====================================================================================
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Common;

// A "namespace" is like a folder that groups related classes; every class in this mod
// lives inside "BetterIndustry", which prevents clashes with same-named classes from
// other mods or the game itself.
namespace BetterIndustry
{
    /// <summary>
    /// SMAPI entry point for BetterIndustry: loads the config, subscribes game events,
    /// and registers all options with Generic Mod Config Menu when that mod is present.
    /// </summary>
    // ": Mod" means this class INHERITS SMAPI's Mod base class, gaining ready-made
    // members like Monitor (logger), Helper (toolkit facade) and ModManifest (mod info).
    // "override" replaces the base class's virtual Entry() with OUR implementation;
    // SMAPI calls this exact method once per game launch.
    public class ModEntry : Mod
    {
        // ---------------- Shared state ----------------
        // "static" = these belong to the CLASS ITSELF, not to one object instance, so any
        // other file can simply write "ModEntry.Config" without a ModEntry reference.
        //
        // "{ get; private set; }" is an auto-property: everyone may READ it, but only code
        // inside ModEntry may WRITE it (private setter).
        //
        // "= null!" uses C#'s null-forgiving operator: the field truly is null until
        // Entry() runs, but "!" promises the compiler "it will be set before anyone reads
        // it", silencing nullable-reference warnings.
        //
        // I18n reads translated strings from the mod's i18n folder (ITranslationHelper).

        /// <summary>User settings loaded from config.json; shared with every balancer class.</summary>
        public static ModConfig Config { get; private set; } = null!;
        /// <summary>SMAPI's console/file logger, cached here so static helper classes can log too.</summary>
        public static IMonitor ModMonitor { get; private set; } = null!;
        /// <summary>Translation accessor for the mod's i18n language files.</summary>
        public static ITranslationHelper I18n { get; private set; } = null!;
        /// <summary>General SMAPI helper: events, asset APIs, config read/write, mod registry.</summary>
        public static IModHelper ModHelper { get; private set; } = null!;

        /// <summary>
        /// The mod's equivalent of "Main()": SMAPI calls this ONCE when the game starts.
        /// Loads settings, publishes shared objects for other files, and hooks events.
        /// </summary>
        /// <param name="helper">SMAPI toolkit providing config I/O, events and asset access.</param>
        public override void Entry(IModHelper helper)
        {
            // Deserialize config.json (in the mod folder) into a fresh ModConfig; the file
            // is auto-created with defaults on first launch. The <ModConfig> in angle
            // brackets selects WHICH class to fill - that syntax is called a "generic
            // type argument".
            Config = helper.ReadConfig<ModConfig>();
            // Cache the inherited logger and helper objects into the static properties
            // above, because the static balancer classes have no other way to reach them.
            ModMonitor = Monitor;
            I18n = helper.Translation;
            ModHelper = helper;

            // Asset Requested Events (Artisan & Cooking)
            // "+=" SUBSCRIBES a method to an event (C#'s observer pattern): whenever the
            // game loads any data asset, SMAPI will ALSO invoke both handlers so they can
            // inject their edits into the loading pipeline.
            helper.Events.Content.AssetRequested += CookingBalancer.OnAssetRequested;
            helper.Events.Content.AssetRequested += ArtisanBalancer.OnAssetRequested;

            // Apply Harmony patches
            var harmony = new HarmonyLib.Harmony(ModManifest.UniqueID);
            CookingPatches.Apply(harmony);
            MachineQualityPatches.Apply(harmony);

            // Game Loop Events
            // GameLaunched: fires once the game finished booting (right moment to query
            // other mods' APIs). DayStarted: fires every in-game morning at 6:00am.
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += FruitTreeDropper.OnDayStarted;

            // Announce successful startup. "$" marks an interpolated string (text with
            // embedded {values}), and LogLevel.Debug prints to the SMAPI console/log.
            Monitor.Log("BetterIndustry loaded successfully: Artisan Goods and Cooking Balance are active.", LogLevel.Debug);
        }

        /// <summary>
        /// Runs once the game has fully launched. Detects Generic Mod Config Menu (GMCM)
        /// and describes every settings option so players can edit config in-game
        /// instead of hand-editing config.json.
        /// </summary>
        /// <param name="sender">Event source supplied by SMAPI (unused).</param>
        /// <param name="e">Event payload supplied by SMAPI (unused).</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Ask SMAPI's registry for ANOTHER MOD'S API. GetApi<T> returns null when the
            // named mod ("spacechase0.GenericModConfigMenu") isn't installed. A "?" on a
            // reference type (nullable reference) declares that null is an expected,
            // explicitly-checked value rather than a bug.
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            // "is null" is a clear modern null check. No GMCM installed? Just skip the
            // whole menu setup - the mod still works through config.json alone.
            if (configMenu is null)
                return;

            // Register this mod with GMCM, handing over two CALLBACKS written as lambdas
            // (a lambda is a small inline anonymous function, "() => { ... }").
            configMenu.Register(
                // Named arguments ("mod:", "reset:", "save:") label each parameter for
                // readability in long calls. ModManifest uniquely identifies THIS mod
                // (id, name, version) to GMCM.
                mod: ModManifest,
                // reset: runs when the player clicks "Reset to Defaults" - builds a
                // brand-new ModConfig containing the coded defaults, then persists it...
                reset: () =>
                {
                    Config = new ModConfig();
                    Helper.WriteConfig(Config);
                    InvalidateAssetCaches();
                },
                // save: runs on "Save" - writes whatever values are currently shown to
                // disk. Both paths invalidate caches so new settings take effect at once.
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    InvalidateAssetCaches();
                }
            );

            // Every option below follows the same GMCM recipe: a getter lambda feeding the
            // CURRENT value into the menu, a setter lambda writing the chosen value back
            // into Config, and (for numbers) min/max/interval slider bounds.
            // I18n.Get(...) pulls localized display text from the i18n folder's json files.
            // ---------------- Section 1: Cooking & Food Quality ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.cooking")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.cooking-balancing.name"),
                tooltip: () => I18n.Get("config.cooking-balancing.tooltip"),
                getValue: () => Config.EnableCookingBalancing,
                setValue: value => Config.EnableCookingBalancing = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.cooking-margin.name"),
                tooltip: () => I18n.Get("config.cooking-margin.tooltip"),
                getValue: () => Config.CookingProfitMargin,
                setValue: value => Config.CookingProfitMargin = value,
                min: 1.0f,
                max: 5.0f,
                interval: 0.05f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.food-quality.name"),
                tooltip: () => I18n.Get("config.food-quality.tooltip"),
                getValue: () => Config.EnableFoodQuality,
                setValue: value => Config.EnableFoodQuality = value
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => I18n.Get("config.quality-priority.name"),
                tooltip: () => I18n.Get("config.quality-priority.tooltip"),
                getValue: () => Config.IngredientQualityPriority,
                setValue: value => Config.IngredientQualityPriority = value,
                allowedValues: new[] { "HighestQuality", "LowestQuality", "InventoryOrder" },
                formatAllowedValue: val => I18n.Get($"config.quality-priority.{val.ToLowerInvariant()}")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enhanced-buffs.name"),
                tooltip: () => I18n.Get("config.enhanced-buffs.tooltip"),
                getValue: () => Config.EnableEnhancedFoodBuffs,
                setValue: value => Config.EnableEnhancedFoodBuffs = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.iridium-buff-duration.name"),
                tooltip: () => I18n.Get("config.iridium-buff-duration.tooltip"),
                getValue: () => Config.IridiumBuffDurationMultiplier,
                setValue: value => Config.IridiumBuffDurationMultiplier = value,
                min: 1.0f,
                max: 3.0f,
                interval: 0.05f
            );

            // ---------------- Section 2: Artisan Goods ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.artisan")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.mead-fix.name"),
                tooltip: () => I18n.Get("config.mead-fix.tooltip"),
                getValue: () => Config.EnableMeadFix,
                setValue: value => Config.EnableMeadFix = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.machine-quality.name"),
                tooltip: () => I18n.Get("config.machine-quality.tooltip"),
                getValue: () => Config.EnableMachineQuality,
                setValue: value => Config.EnableMachineQuality = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.machine-luck.name"),
                tooltip: () => I18n.Get("config.machine-luck.tooltip"),
                getValue: () => Config.ApplyDailyLuckToMachines,
                setValue: value => Config.ApplyDailyLuckToMachines = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.truffle-oil-fix.name"),
                tooltip: () => I18n.Get("config.truffle-oil-fix.tooltip"),
                getValue: () => Config.EnableTruffleOilFix,
                setValue: value => Config.EnableTruffleOilFix = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.truffle-oil-multiplier.name"),
                tooltip: () => I18n.Get("config.truffle-oil-multiplier.tooltip"),
                getValue: () => Config.TruffleOilMultiplier,
                setValue: value => Config.TruffleOilMultiplier = value,
                min: 1.0f,
                max: 3.0f,
                interval: 0.05f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.juice-buff.name"),
                tooltip: () => I18n.Get("config.juice-buff.tooltip"),
                getValue: () => Config.EnableJuiceBuff,
                setValue: value => Config.EnableJuiceBuff = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.juice-multiplier.name"),
                tooltip: () => I18n.Get("config.juice-multiplier.tooltip"),
                getValue: () => Config.JuiceMultiplier,
                setValue: value => Config.JuiceMultiplier = value,
                min: 1.0f,
                max: 5.0f,
                interval: 0.05f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.expanded-aging.name"),
                tooltip: () => I18n.Get("config.expanded-aging.tooltip"),
                getValue: () => Config.EnableExpandedAging,
                setValue: value => Config.EnableExpandedAging = value
            );

            // ---------------- Section 3: Fruit Tree Automation ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.fruittree")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.auto-fruit-drop.name"),
                tooltip: () => I18n.Get("config.auto-fruit-drop.tooltip"),
                getValue: () => Config.EnableAutoFruitDrop,
                setValue: value => Config.EnableAutoFruitDrop = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.fruit-drop-threshold.name"),
                tooltip: () => I18n.Get("config.fruit-drop-threshold.tooltip"),
                getValue: () => Config.MaxFruitsBeforeDrop,
                setValue: value => Config.MaxFruitsBeforeDrop = value,
                min: 1,
                max: 10,
                interval: 1
            );
        }

        /// <summary>
        /// Tells SMAPI to throw away its cached copies of the game-data assets this mod
        /// edits. The next time the game needs them it reloads from scratch, which re-runs
        /// our OnAssetRequested edits using the freshly saved config values.
        /// </summary>
        private void InvalidateAssetCaches()
        {
            // Data/Objects: every item's stats (name, sell Price, edibility, ...).
            // Data/Machines: per-machine input triggers and outputs (our artisan rules).
            // Data/CookingRecipes: packed text recipes mapping dishes to ingredients.
            // Without invalidation, stale pre-edit values would persist until restart.
            Helper.GameContent.InvalidateCache("Data/Objects");
            Helper.GameContent.InvalidateCache("Data/Machines");
            Helper.GameContent.InvalidateCache("Data/CookingRecipes");
        }
    }
}
