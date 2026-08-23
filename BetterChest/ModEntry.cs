using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

// ============================================================================
// ModEntry is SMAPI's launchpad: its Entry method runs ONCE when Stardew
// loads mods. From here the mod reads config.json, applies the Harmony
// patches (FishingPatches / ChestPatches), and subscribes to SMAPI events —
// Warped (player changed map, used to tag Skull Cavern chests) and
// GameLaunched (title screen ready, used to build the Generic Mod Config
// Menu UI). It also exposes shared static services (Config, ModMonitor...)
// that the other classes use without needing a reference to this object.
// Key concepts demonstrated: SMAPI mod lifecycle, events, static properties,
// and talking to another mod through an interface "API mirror".
// ============================================================================
namespace BetterChest
{
    /// <summary>
    /// Local copy of the Generic Mod Config Menu API surface this mod uses.
    /// The method signatures match spacechase0's mod exactly, so SMAPI's
    /// ModRegistry.GetApi&lt;T&gt; can hand us a live implementation WITHOUT a hard
    /// DLL reference — if that mod isn't installed, GetApi just returns null.
    /// </summary>
    public interface IGenericModConfigMenuApi
    {
        /// <summary>Registers this mod with the config menu, providing reset/save callbacks.</summary>
        /// <param name="mod">This mod's manifest (id, name, version).</param>
        /// <param name="reset">Delegate that restores default settings.</param>
        /// <param name="save">Delegate that persists current settings to disk.</param>
        /// <param name="titleScreenOnly">True to allow edits only from the title screen.</param>
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        /// <summary>Adds a bold section heading between groups of options.</summary>
        /// <param name="mod">This mod's manifest.</param>
        /// <param name="text">Delegate returning the heading text (so translations stay live).</param>
        /// <param name="tooltip">Optional delegate returning hover text.</param>
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

        /// <summary>Adds a slider/number field bound to an int property.</summary>
        /// <param name="mod">This mod's manifest.</param>
        /// <param name="getValue">Delegate reading the current value (called live each frame).</param>
        /// <param name="setValue">Delegate writing an edited value back to config.</param>
        /// <param name="name">Delegate returning the option's label.</param>
        /// <param name="tooltip">Optional hover-text delegate.</param>
        /// <param name="min">Optional minimum slider value.</param>
        /// <param name="max">Optional maximum slider value.</param>
        /// <param name="interval">Optional step size between values.</param>
        /// <param name="formatValue">Optional delegate formatting the displayed number.</param>
        /// <param name="fieldId">Optional unique id for GMCM keybinds/save slots.</param>
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);

        /// <summary>Adds a slider/number field bound to a float property (chances use 0.0-1.0 sliders).</summary>
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);

        /// <summary>Adds a checkbox bound to a bool property.</summary>
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }

    /// <summary>
    /// The mod's main entry point. SMAPI discovers this class (it extends
    /// <see cref="StardewModdingAPI.Mod"/>) and calls <see cref="Entry"/> once at startup.
    /// It also owns chest tagging for Skull Cavern floors via the Warped event.
    /// </summary>
    public class ModEntry : Mod
    {
        /// <summary>
        /// modData key used to TAG chests this mod manages ("generate per-player loot here").
        /// modData is saved with the game, so tags survive save/reload.
        /// </summary>
        public const string GeneratedModDataKey = "feiluvnana.BetterChest/Generated";

        /// <summary>
        /// The parsed config.json, shared statically so every class can read settings.
        /// "= null!" uses the null-forgiving operator: it tells the compiler "this will
        /// be assigned before anyone reads it" (in Entry) without a warning.
        /// </summary>
        public static ModConfig Config { get; private set; } = null!;
        /// <summary>Shared SMAPI logger — writes to the console and SMAPI log.</summary>
        public static IMonitor ModMonitor { get; private set; } = null!;
        /// <summary>Translation helper for the mod's i18n folder (.json language files).</summary>
        public static ITranslationHelper I18n { get; private set; } = null!;
        /// <summary>SMAPI's helper for file I/O, mod registry, reflection and events.</summary>
        public static IModHelper ModHelper { get; private set; } = null!;

        /// <summary>
        /// Called ONCE by SMAPI when the mod loads. Sets up shared services,
        /// installs Harmony patches, and subscribes to game events.
        /// </summary>
        /// <param name="helper">SMAPI's helper API for this mod (provided by the framework).</param>
        public override void Entry(IModHelper helper)
        {
            // Generic method call: ReadConfig<ModConfig> deserializes config.json
            // into a fresh ModConfig object (<T> is a type parameter — a placeholder
            // for the actual type decided at the call site).
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor; // Monitor is a property inherited from the Mod base class
            I18n = helper.Translation;
            ModHelper = helper;

            // One Harmony instance per mod, keyed by the mod's unique id.
            var harmony = new Harmony(ModManifest.UniqueID);
            FishingPatches.Apply(harmony);
            ChestPatches.Apply(harmony);

            // "+=" SUBSCRIBES a handler method to an event: whenever the player warps
            // / the game launches, SMAPI calls our methods below.
            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        /// <summary>
        /// Event handler for <see cref="StardewModdingAPI.Events.IPlayerEvents.Warped"/>.
        /// Tags Skull Cavern chests for per-player rolling whenever the player arrives.
        /// </summary>
        /// <param name="sender">The event source (SMAPI) — standard .NET event pattern.</param>
        /// <param name="e">Details of the warp (old and new location).</param>
        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            // Pattern matching: only MineShaft locations qualify, and level > 120
            // means the Skull Cavern (levels 1-120 are the regular mines).
            if (e.NewLocation is MineShaft shaft && shaft.mineLevel > 120)
            {
                ProcessMineShaftChests(shaft);
            }
        }

        /// <summary>
        /// Ensures a mine floor's treasure chests exist and tags them with
        /// <see cref="GeneratedModDataKey"/> so ChestPatches will roll loot for each player.
        /// Actual loot is NOT generated here — that happens at open time, per player.
        /// </summary>
        /// <param name="shaft">The Skull Cavern floor the player just entered.</param>
        public static void ProcessMineShaftChests(MineShaft shaft)
        {
            if (shaft == null || shaft.mineLevel <= 120)
                return;

            bool isTreasureRoom = false;
            try
            {
                // Reflection: reach into the game's PRIVATE field "netIsTreasureRoom".
                // SMAPI's Reflection wrapper makes this mod-update-safe; required: false
                // means "return null if missing" instead of throwing.
                var netIsTreasureRoomField = ModHelper?.Reflection.GetField<Netcode.NetBool>(shaft, "netIsTreasureRoom", required: false);
                if (netIsTreasureRoomField != null)
                {
                    // Unwrap twice: GetValue() may be null, and NetBool stores its bool in .Value.
                    isTreasureRoom = netIsTreasureRoomField.GetValue()?.Value ?? false;
                }
            }
            catch
            {
                // Fallback
            }

            bool isForcedSpecialChest = shaft.mineLevel == 220 || shaft.mineLevel == 320 || shaft.mineLevel == 420 || shaft.mineLevel == 520;

            // Regular floors: only proceed if the game flagged this as a treasure room.
            if (!isTreasureRoom && !isForcedSpecialChest)
                return;

            if (shaft.Objects == null)
                return;

            // Ensure special chest exists on repeatable runs for Floor 100/200/300/400 even if marked consumed by vanilla
            if (isForcedSpecialChest)
            {
                // Vector2 doubles as a TILE COORDINATE and as the dictionary key for
                // objects on the map — tile (9, 9) is where milestone chests spawn.
                Vector2 vector = new Vector2(9f, 9f);
                if (shaft.mineLevel == 320)
                    vector.X += 1f;

                // Check both dictionaries before spawning so repeat visits don't
                // create duplicate chests.
                if (!shaft.overlayObjects.ContainsKey(vector) && !shaft.Objects.ContainsKey(vector))
                {
                    Chest chest = new Chest(new List<Item>(), vector);
                    chest.SetBigCraftableSpriteIndex(344); // sprite 344 = treasure chest look
                    shaft.overlayObjects[vector] = chest;
                }

                if (shaft.mineLevel == 320 || shaft.mineLevel == 420 || shaft.mineLevel == 520)
                {
                    // Vector arithmetic offsets the tile position (2 tiles left).
                    Vector2 secVector = vector + new Vector2(-2f, 0f);
                    if (!shaft.overlayObjects.ContainsKey(secVector) && !shaft.Objects.ContainsKey(secVector))
                    {
                        // Object initializer syntax "{ Tint = ... }" sets a property right
                        // after construction without extra lines.
                        Chest secChest = new Chest(new List<Item>(), secVector)
                        {
                            Tint = new Color(255, 210, 200)
                        };
                        secChest.SetBigCraftableSpriteIndex(344);
                        shaft.overlayObjects[secVector] = secChest;
                    }
                }

                if (shaft.mineLevel == 420 || shaft.mineLevel == 520)
                {
                    Vector2 tertVector = vector + new Vector2(2f, 0f);
                    if (!shaft.overlayObjects.ContainsKey(tertVector) && !shaft.Objects.ContainsKey(tertVector))
                    {
                        Chest tertChest = new Chest(new List<Item>(), tertVector)
                        {
                            Tint = new Color(216, 255, 240)
                        };
                        tertChest.SetBigCraftableSpriteIndex(344);
                        shaft.overlayObjects[tertVector] = tertChest;
                    }
                }
            }

            // Collect every chest on this floor from both object dictionaries,
            // skipping duplicates ("is Chest c" is a type-test + cast in one step).
            var allChests = new List<Chest>();
            foreach (var obj in shaft.Objects.Values)
            {
                if (obj is Chest c) allChests.Add(c);
            }
            foreach (var obj in shaft.overlayObjects.Values)
            {
                if (obj is Chest c && !allChests.Contains(c)) allChests.Add(c);
            }

            foreach (var chest in allChests)
            {
                // Loot is NOT rolled here: rewards are rolled per-player at open time (see ChestPatches)
                // so that every player gets their own independent roll from the same chest.
                if (!chest.modData.ContainsKey(GeneratedModDataKey))
                    chest.modData[GeneratedModDataKey] = "true";
            }
        }

        /// <summary>
        /// Event handler for <see cref="StardewModdingAPI.Events.IGameLoopEvents.GameLaunched"/>.
        /// Builds this mod's page in Generic Mod Config Menu, if that mod is installed.
        /// </summary>
        /// <param name="sender">The event source (SMAPI).</param>
        /// <param name="e">Event arguments (unused here).</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Ask SMAPI for another mod's API. The generic type <IGenericModConfigMenuApi>
            // tells SMAPI which local interface to wrap; result is null if GMCM is absent.
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // Register with callbacks for "reset to defaults" and "save to disk".
            // The "() => ..." lambdas are tiny inline functions passed as delegates;
            // named arguments (mod:, reset:, save:) make the call self-documenting.
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            // General Section
            // Every option follows the same shape:
            //   name/tooltip -> delegates returning translated strings (I18n.Get looks
            //                   up keys in the i18n folder's language files);
            //   getValue     -> delegate GMCM calls to DISPLAY the current setting;
            //   setValue     -> delegate GMCM calls to STORE an edited value.
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.general"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-custom-rewards.name"),
                tooltip: () => I18n.Get("config.enable-custom-rewards.tooltip"),
                getValue: () => Config.EnableCustomRewards,
                setValue: value => Config.EnableCustomRewards = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.exclude-cosmetics.name"),
                tooltip: () => I18n.Get("config.exclude-cosmetics.tooltip"),
                getValue: () => Config.ExcludeCosmetics,
                setValue: value => Config.ExcludeCosmetics = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-depth-scaling.name"),
                tooltip: () => I18n.Get("config.enable-depth-scaling.tooltip"),
                getValue: () => Config.EnableDepthScaling,
                setValue: value => Config.EnableDepthScaling = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.scale-legendary-by-depth.name"),
                tooltip: () => I18n.Get("config.scale-legendary-by-depth.tooltip"),
                getValue: () => Config.ScaleLegendaryByDepth,
                setValue: value => Config.ScaleLegendaryByDepth = value
            );

            // Progression & Gatekeeping Section
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.progression-gatekeeping"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.gatekeep-mastery-items.name"),
                tooltip: () => I18n.Get("config.gatekeep-mastery-items.tooltip"),
                getValue: () => Config.GatekeepMasteryItems,
                setValue: value => Config.GatekeepMasteryItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.gatekeep-island-items.name"),
                tooltip: () => I18n.Get("config.gatekeep-island-items.tooltip"),
                getValue: () => Config.GatekeepIslandItems,
                setValue: value => Config.GatekeepIslandItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.gatekeep-qi-items.name"),
                tooltip: () => I18n.Get("config.gatekeep-qi-items.tooltip"),
                getValue: () => Config.GatekeepQiItems,
                setValue: value => Config.GatekeepQiItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.gatekeep-mystery-boxes.name"),
                tooltip: () => I18n.Get("config.gatekeep-mystery-boxes.tooltip"),
                getValue: () => Config.GatekeepMysteryBoxes,
                setValue: value => Config.GatekeepMysteryBoxes = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.gatekeep-calico-eggs.name"),
                tooltip: () => I18n.Get("config.gatekeep-calico-eggs.tooltip"),
                getValue: () => Config.GatekeepCalicoEggs,
                setValue: value => Config.GatekeepCalicoEggs = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.gatekeep-radioactive-items.name"),
                tooltip: () => I18n.Get("config.gatekeep-radioactive-items.tooltip"),
                getValue: () => Config.GatekeepRadioactiveItems,
                setValue: value => Config.GatekeepRadioactiveItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.gatekeep-auto-petter.name"),
                tooltip: () => I18n.Get("config.gatekeep-auto-petter.tooltip"),
                getValue: () => Config.GatekeepAutoPetter,
                setValue: value => Config.GatekeepAutoPetter = value
            );

            // Decaying Multi-Roll Section (Regular Chests)
            // Number options add min/max/interval so GMCM renders a bounded slider.
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.decaying-rolls"));
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.max-rolls.name"),
                tooltip: () => I18n.Get("config.max-rolls.tooltip"),
                getValue: () => Config.MaxRolls,
                setValue: value => Config.MaxRolls = value,
                min: 1,
                max: 8
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-2-chance.name"),
                tooltip: () => I18n.Get("config.roll-2-chance.tooltip"),
                getValue: () => Config.Roll2Chance,
                setValue: value => Config.Roll2Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-3-chance.name"),
                tooltip: () => I18n.Get("config.roll-3-chance.tooltip"),
                getValue: () => Config.Roll3Chance,
                setValue: value => Config.Roll3Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-4-chance.name"),
                tooltip: () => I18n.Get("config.roll-4-chance.tooltip"),
                getValue: () => Config.Roll4Chance,
                setValue: value => Config.Roll4Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-5-chance.name"),
                tooltip: () => I18n.Get("config.roll-5-chance.tooltip"),
                getValue: () => Config.Roll5Chance,
                setValue: value => Config.Roll5Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-6-chance.name"),
                tooltip: () => I18n.Get("config.roll-6-chance.tooltip"),
                getValue: () => Config.Roll6Chance,
                setValue: value => Config.Roll6Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-7-chance.name"),
                tooltip: () => I18n.Get("config.roll-7-chance.tooltip"),
                getValue: () => Config.Roll7Chance,
                setValue: value => Config.Roll7Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-8-chance.name"),
                tooltip: () => I18n.Get("config.roll-8-chance.tooltip"),
                getValue: () => Config.Roll8Chance,
                setValue: value => Config.Roll8Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            // Stack Multipliers Section (Regular Chests)
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.stack-multipliers"));
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.double-stack-chance.name"),
                tooltip: () => I18n.Get("config.double-stack-chance.tooltip"),
                getValue: () => Config.DoubleStackChance,
                setValue: value => Config.DoubleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.triple-stack-chance.name"),
                tooltip: () => I18n.Get("config.triple-stack-chance.tooltip"),
                getValue: () => Config.TripleStackChance,
                setValue: value => Config.TripleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.quadruple-stack-chance.name"),
                tooltip: () => I18n.Get("config.quadruple-stack-chance.tooltip"),
                getValue: () => Config.QuadrupleStackChance,
                setValue: value => Config.QuadrupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.quintuple-stack-chance.name"),
                tooltip: () => I18n.Get("config.quintuple-stack-chance.tooltip"),
                getValue: () => Config.QuintupleStackChance,
                setValue: value => Config.QuintupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            // Floor 100 Special Chest Buff Section
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.floor-100-buffs"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-floor-100-buff.name"),
                tooltip: () => I18n.Get("config.enable-floor-100-buff.tooltip"),
                getValue: () => Config.EnableFloor100Buff,
                setValue: value => Config.EnableFloor100Buff = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-all-categories-equal.name"),
                tooltip: () => I18n.Get("config.floor-100-all-categories-equal.tooltip"),
                getValue: () => Config.Floor100AllCategoriesEqual,
                setValue: value => Config.Floor100AllCategoriesEqual = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-max-rolls.name"),
                tooltip: () => I18n.Get("config.floor-100-max-rolls.tooltip"),
                getValue: () => Config.Floor100MaxRolls,
                setValue: value => Config.Floor100MaxRolls = value,
                min: 1,
                max: 12
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-2-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-2-chance.tooltip"),
                getValue: () => Config.Floor100Roll2Chance,
                setValue: value => Config.Floor100Roll2Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-3-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-3-chance.tooltip"),
                getValue: () => Config.Floor100Roll3Chance,
                setValue: value => Config.Floor100Roll3Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-4-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-4-chance.tooltip"),
                getValue: () => Config.Floor100Roll4Chance,
                setValue: value => Config.Floor100Roll4Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-5-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-5-chance.tooltip"),
                getValue: () => Config.Floor100Roll5Chance,
                setValue: value => Config.Floor100Roll5Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-6-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-6-chance.tooltip"),
                getValue: () => Config.Floor100Roll6Chance,
                setValue: value => Config.Floor100Roll6Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-7-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-7-chance.tooltip"),
                getValue: () => Config.Floor100Roll7Chance,
                setValue: value => Config.Floor100Roll7Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-8-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-8-chance.tooltip"),
                getValue: () => Config.Floor100Roll8Chance,
                setValue: value => Config.Floor100Roll8Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-9-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-9-chance.tooltip"),
                getValue: () => Config.Floor100Roll9Chance,
                setValue: value => Config.Floor100Roll9Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-10-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-10-chance.tooltip"),
                getValue: () => Config.Floor100Roll10Chance,
                setValue: value => Config.Floor100Roll10Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-11-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-11-chance.tooltip"),
                getValue: () => Config.Floor100Roll11Chance,
                setValue: value => Config.Floor100Roll11Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-12-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-12-chance.tooltip"),
                getValue: () => Config.Floor100Roll12Chance,
                setValue: value => Config.Floor100Roll12Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-double-stack.name"),
                tooltip: () => I18n.Get("config.floor-100-double-stack.tooltip"),
                getValue: () => Config.Floor100DoubleStackChance,
                setValue: value => Config.Floor100DoubleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-triple-stack.name"),
                tooltip: () => I18n.Get("config.floor-100-triple-stack.tooltip"),
                getValue: () => Config.Floor100TripleStackChance,
                setValue: value => Config.Floor100TripleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-quadruple-stack.name"),
                tooltip: () => I18n.Get("config.floor-100-quadruple-stack.tooltip"),
                getValue: () => Config.Floor100QuadrupleStackChance,
                setValue: value => Config.Floor100QuadrupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-quintuple-stack.name"),
                tooltip: () => I18n.Get("config.floor-100-quintuple-stack.tooltip"),
                getValue: () => Config.Floor100QuintupleStackChance,
                setValue: value => Config.Floor100QuintupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            // Category Weights Section
            // The config stores these weights as double, but GMCM sliders use float,
            // so the getters CAST: "(float)Config.LegendaryWeight" converts the type.
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.category-weights"));
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.legendary-weight.name"),
                tooltip: () => I18n.Get("config.legendary-weight.tooltip"),
                getValue: () => (float)Config.LegendaryWeight,
                setValue: value => Config.LegendaryWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.agriculture-weight.name"),
                tooltip: () => I18n.Get("config.agriculture-weight.tooltip"),
                getValue: () => (float)Config.AgricultureWeight,
                setValue: value => Config.AgricultureWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.mining-weight.name"),
                tooltip: () => I18n.Get("config.mining-weight.tooltip"),
                getValue: () => (float)Config.MiningWeight,
                setValue: value => Config.MiningWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.fishing-weight.name"),
                tooltip: () => I18n.Get("config.fishing-weight.tooltip"),
                getValue: () => (float)Config.FishingWeight,
                setValue: value => Config.FishingWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.combat-weight.name"),
                tooltip: () => I18n.Get("config.combat-weight.tooltip"),
                getValue: () => (float)Config.CombatWeight,
                setValue: value => Config.CombatWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.foraging-weight.name"),
                tooltip: () => I18n.Get("config.foraging-weight.tooltip"),
                getValue: () => (float)Config.ForagingWeight,
                setValue: value => Config.ForagingWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.lootbox-weight.name"),
                tooltip: () => I18n.Get("config.lootbox-weight.tooltip"),
                getValue: () => (float)Config.LootboxWeight,
                setValue: value => Config.LootboxWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );

            // Category Toggles
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.category-toggles"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-legendary-category.name"),
                tooltip: () => I18n.Get("config.enable-legendary-category.tooltip"),
                getValue: () => Config.EnableLegendaryCategory,
                setValue: value => Config.EnableLegendaryCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-agriculture-category.name"),
                tooltip: () => I18n.Get("config.enable-agriculture-category.tooltip"),
                getValue: () => Config.EnableAgricultureCategory,
                setValue: value => Config.EnableAgricultureCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-mining-category.name"),
                tooltip: () => I18n.Get("config.enable-mining-category.tooltip"),
                getValue: () => Config.EnableMiningCategory,
                setValue: value => Config.EnableMiningCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-fishing-category.name"),
                tooltip: () => I18n.Get("config.enable-fishing-category.tooltip"),
                getValue: () => Config.EnableFishingCategory,
                setValue: value => Config.EnableFishingCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-combat-category.name"),
                tooltip: () => I18n.Get("config.enable-combat-category.tooltip"),
                getValue: () => Config.EnableCombatCategory,
                setValue: value => Config.EnableCombatCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-foraging-category.name"),
                tooltip: () => I18n.Get("config.enable-foraging-category.tooltip"),
                getValue: () => Config.EnableForagingCategory,
                setValue: value => Config.EnableForagingCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-lootbox-category.name"),
                tooltip: () => I18n.Get("config.enable-lootbox-category.tooltip"),
                getValue: () => Config.EnableLootboxCategory,
                setValue: value => Config.EnableLootboxCategory = value
            );

            // Detailed Item Feature Toggles
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.item-toggles"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-fertilizers.name"),
                tooltip: () => I18n.Get("config.enable-fertilizers.tooltip"),
                getValue: () => Config.EnableFertilizers,
                setValue: value => Config.EnableFertilizers = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-auto-petter.name"),
                tooltip: () => I18n.Get("config.enable-auto-petter.tooltip"),
                getValue: () => Config.EnableAutoPetter,
                setValue: value => Config.EnableAutoPetter = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-radioactive-items.name"),
                tooltip: () => I18n.Get("config.enable-radioactive-items.tooltip"),
                getValue: () => Config.EnableRadioactiveItems,
                setValue: value => Config.EnableRadioactiveItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-iridium-items.name"),
                tooltip: () => I18n.Get("config.enable-iridium-items.tooltip"),
                getValue: () => Config.EnableIridiumItems,
                setValue: value => Config.EnableIridiumItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-bombs.name"),
                tooltip: () => I18n.Get("config.enable-bombs.tooltip"),
                getValue: () => Config.EnableBombs,
                setValue: value => Config.EnableBombs = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-fishing-tackle.name"),
                tooltip: () => I18n.Get("config.enable-fishing-tackle.tooltip"),
                getValue: () => Config.EnableFishingTackle,
                setValue: value => Config.EnableFishingTackle = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-slime-eggs.name"),
                tooltip: () => I18n.Get("config.enable-slime-eggs.tooltip"),
                getValue: () => Config.EnableSlimeEggs,
                setValue: value => Config.EnableSlimeEggs = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-combat-consumables.name"),
                tooltip: () => I18n.Get("config.enable-combat-consumables.tooltip"),
                getValue: () => Config.EnableCombatConsumables,
                setValue: value => Config.EnableCombatConsumables = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-rare-seeds.name"),
                tooltip: () => I18n.Get("config.enable-rare-seeds.tooltip"),
                getValue: () => Config.EnableRareSeeds,
                setValue: value => Config.EnableRareSeeds = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-coal.name"),
                tooltip: () => I18n.Get("config.enable-coal.tooltip"),
                getValue: () => Config.EnableCoal,
                setValue: value => Config.EnableCoal = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-hardwood.name"),
                tooltip: () => I18n.Get("config.enable-hardwood.tooltip"),
                getValue: () => Config.EnableHardwood,
                setValue: value => Config.EnableHardwood = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-mystery-boxes.name"),
                tooltip: () => I18n.Get("config.enable-mystery-boxes.tooltip"),
                getValue: () => Config.EnableMysteryBoxes,
                setValue: value => Config.EnableMysteryBoxes = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-omni-geodes.name"),
                tooltip: () => I18n.Get("config.enable-omni-geodes.tooltip"),
                getValue: () => Config.EnableOmniGeodes,
                setValue: value => Config.EnableOmniGeodes = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-calico-eggs.name"),
                tooltip: () => I18n.Get("config.enable-calico-eggs.tooltip"),
                getValue: () => Config.EnableCalicoEggs,
                setValue: value => Config.EnableCalicoEggs = value
            );

            // =========================================================================
            // === FISHING TREASURE CHESTS GMCM SECTION                              ===
            // =========================================================================
            // Same registration pattern as above, but bound to the fishing-chest
            // settings (roll ranges, golden chest rolls, trash reroll bonus).
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.fishing-chests")
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-fishing-chest-buff.name"),
                tooltip: () => I18n.Get("config.enable-fishing-chest-buff.tooltip"),
                getValue: () => Config.EnableFishingChestBuff,
                setValue: value => Config.EnableFishingChestBuff = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.fishing-chest-min-rolls.name"),
                tooltip: () => I18n.Get("config.fishing-chest-min-rolls.tooltip"),
                getValue: () => Config.FishingChestMinRolls,
                setValue: value => Config.FishingChestMinRolls = value,
                min: 1,
                max: 10
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.fishing-chest-max-rolls.name"),
                tooltip: () => I18n.Get("config.fishing-chest-max-rolls.tooltip"),
                getValue: () => Config.FishingChestMaxRolls,
                setValue: value => Config.FishingChestMaxRolls = value,
                min: 1,
                max: 12
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.golden-chest-min-rolls.name"),
                tooltip: () => I18n.Get("config.golden-chest-min-rolls.tooltip"),
                getValue: () => Config.GoldenChestMinRolls,
                setValue: value => Config.GoldenChestMinRolls = value,
                min: 1,
                max: 10
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.golden-chest-max-rolls.name"),
                tooltip: () => I18n.Get("config.golden-chest-max-rolls.tooltip"),
                getValue: () => Config.GoldenChestMaxRolls,
                setValue: value => Config.GoldenChestMaxRolls = value,
                min: 1,
                max: 12
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-fishing-trash-reroll-bonus.name"),
                tooltip: () => I18n.Get("config.enable-fishing-trash-reroll-bonus.tooltip"),
                getValue: () => Config.EnableFishingTrashRerollBonus,
                setValue: value => Config.EnableFishingTrashRerollBonus = value
            );
        }
    }
}