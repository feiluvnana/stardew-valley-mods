// ============================================================================
// ModEntry.cs — the mod's FRONT DOOR.
//
// HOW SMAPI LOADS A MOD: SMAPI is the "mod loader" that launches Stardew
// Valley with mod support. At boot it reads each mod's manifest.json (an
// IManifest: name, author, version, which .dll to load), then finds the class
// inheriting from Mod and calls its Entry(...) method EXACTLY ONCE. All the
// other methods in this file are wired up from Entry.
//
// C# concept — "using" directives: they import other libraries' NAMESPACES
// (named groups of classes) so we can write short names like "Chest" instead
// of the fully-qualified "StardewValley.Objects.Chest".
//   HarmonyLib              -> Harmony: injects extra code into game methods
//                              at RUNTIME (game files themselves stay untouched)
//   Microsoft.Xna.Framework -> MonoGame, the engine Stardew Valley is built on
//   StardewModdingAPI       -> SMAPI itself: Mod base class, logging, events
//   StardewValley           -> the game's own code (Game1, Farmer, Item, ...)
//   StardewValley.Locations -> map classes like MineShaft (Skull Cavern)
//   StardewValley.Objects   -> placeable objects like Chest
//   Common                  -> Generic Mod Config Menu (GMCM) API interface
// ============================================================================
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Locations;
using StardewValley.Objects;
using Common;

namespace BetterChest
{
    // C# concept — inheritance, written "class Child : Parent": ModEntry derives
    // from SMAPI's Mod base class, inheriting useful members like Monitor (log
    // writer) and Helper (toolbox). "public" = visible to every other assembly.
    //
    // C# concept — "override" on Entry below: Entry replaces a VIRTUAL method
    // declared in the base class; SMAPI looks for it and calls it at startup.
    /// <summary>
    /// The mod's main entry point. Sets up shared services, Harmony patches, and GMCM configuration.
    /// </summary>
    public class ModEntry : Mod
    {
        // C# concept — "const": a value frozen at compile time; it can never
        // change. PascalCase naming is the convention for constants.
        //
        // WHY IT EXISTS: every game object carries a persistent string-to-string
        // dictionary called modData. Writing this key into a chest's modData
        // "tags" it as a Skull Cavern treasure chest our mod should re-roll
        // (tagging happens in ProcessMineShaftChests below; the tag is consumed
        // by ChestPatches.CheckForAction_Prefix when a player opens the chest).
        /// <summary>
        /// modData flag stamped onto eligible Skull Cavern treasure chests so
        /// <see cref="ChestPatches"/> can recognize and re-roll them.
        /// </summary>
        public const string GeneratedModDataKey = "feiluvnana.BetterChest/Generated";

        // ---------------------------------------------------------------------
        // SHARED SERVICES — assigned once in Entry(), readable from anywhere.
        // C# concepts used by these four declarations:
        //   * "static"  -> belongs to the CLASS itself, not to one instance, so
        //                  any file can write "ModEntry.Config" directly.
        //   * "{ get; private set; }" -> AUTO-PROPERTY: the compiler generates a
        //                  hidden backing field plus accessor methods. Everyone
        //                  may READ; only this class may WRITE.
        //   * "= null!" -> starts as null, but "!" silences the nullable checker
        //                  by promising "assigned before anyone reads it" —
        //                  Entry() fills all four before gameplay begins.
        // ---------------------------------------------------------------------
        /// <summary>The live user settings deserialized from config.json (shape defined in ModConfig.cs).</summary>
        public static ModConfig Config { get; private set; } = null!;
        /// <summary>SMAPI's logger; writes colored messages to the SMAPI console and log file.</summary>
        public static IMonitor ModMonitor { get; private set; } = null!;
        /// <summary>Translator pulling strings from the mod's i18n/*.json language files.</summary>
        public static ITranslationHelper I18n { get; private set; } = null!;
        /// <summary>SMAPI's toolbox: events, config read/write, reflection, mod registry.</summary>
        public static IModHelper ModHelper { get; private set; } = null!;

        // =====================================================================
        // ENTRY POINT — SMAPI calls this exactly once while the game boots.
        // Setup order matters: shared services first, Harmony patches second,
        // event subscriptions last.
        // =====================================================================
        /// <summary>
        /// Loads the saved config, publishes the shared service objects, installs
        /// both Harmony patches, and subscribes to the SMAPI events we need.
        /// </summary>
        /// <param name="helper">SMAPI's per-mod toolbox (events, registry, reflection, config).</param>
        public override void Entry(IModHelper helper)
        {
            // ReadConfig<ModConfig>() loads config.json (stored next to the mod's
            // dll) into a ModConfig object; any missing field falls back to the
            // default written in ModConfig.cs. The <T> makes ReadConfig a
            // GENERIC method — one implementation reused for any config class.
            Config = helper.ReadConfig<ModConfig>();
            // "Monitor" was inherited from the base Mod class; publishing it in
            // our static property lets the patch classes log without owning a
            // ModEntry instance.
            ModMonitor = Monitor;
            I18n = helper.Translation;
            ModHelper = helper;

            // Create the mod's single shared Harmony instance. Passing the mod's
            // UniqueID (from ModManifest — the IManifest object SMAPI built out
            // of manifest.json) namespaces every patch for later identification.
            var harmony = new Harmony(ModManifest.UniqueID);
            // Install both runtime hooks; see each class's Apply method.
            ChestPatches.Apply(harmony);
            FishingPatches.Apply(harmony);

            // C# concept — EVENTS with "+=": an event is a broadcast channel.
            // "+=" SUBSCRIBES our method as a listener, so SMAPI invokes
            // OnWarped every time a player changes map, and OnGameLaunched once
            // the title screen is ready. ("-=" would unsubscribe.)
            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        // =====================================================================
        // EVENT HANDLER — runs EVERY time the local player warps to a new map.
        // The parameter list is fixed by SMAPI's WarpedEventArgs delegate:
        //   sender = whoever raised the event (rarely used)
        //   e      = event data: which player moved, and to where
        // =====================================================================
        /// <summary>
        /// Warped listener: entering a Skull Cavern floor (any MineShaft deeper
        /// than level 120) tags that floor's treasure chests for custom loot.
        /// </summary>
        /// <param name="sender">Event source (unused here).</param>
        /// <param name="e">Carries e.Player and e.NewLocation.</param>
        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            // C# concept — NULLABLE REFERENCE TYPE: the "?" after "object" says
            // the parameter may be null (SMAPI passes null for some events).
            // "try/catch" guards risky code: on any thrown Exception, execution
            // jumps to the catch block instead of crashing the game.
            try
            {
                // C# concept — pattern matching: "x is Type name" tests the
                // object's actual runtime type AND yields a properly-typed
                // variable in one step. MineShaft = Skull Cavern maps; the
                // regular Mines occupy levels 1-120, so "> 120" selects only
                // Skull Cavern floors.
                if (e.NewLocation is MineShaft shaft && shaft.mineLevel > 120)
                {
                    ProcessMineShaftChests(shaft);
                }
            }
            catch (Exception ex)
            {
                // "$" enables STRING INTERPOLATION: {ex} splices the exception's
                // message and stack trace into the text. LogLevel.Error paints
                // the line red in the SMAPI console.
                ModMonitor.Log($"Error tagging Skull Cavern chests on level warp: {ex}", LogLevel.Error);
            }
        }

        // =====================================================================
        // TAGGING PASS — stamps every chest on this floor with
        // GeneratedModDataKey and optionally strips its vanilla loot. Actual
        // ROLLING happens later, inside the ChestPatches prefix, the first
        // time each individual player opens a chest.
        // =====================================================================
        /// <summary>
        /// Tags each chest on the floor with <see cref="GeneratedModDataKey"/>
        /// (skipping already-tagged chests) and clears vanilla contents when
        /// custom rewards are enabled.
        /// </summary>
        /// <param name="shaft">The Skull Cavern floor whose chests to prepare.</param>
        private void ProcessMineShaftChests(MineShaft shaft)
        {
            // C# concept — REFLECTION: reaching a class's members at runtime BY
            // NAME. SMAPI's Helper.Reflection grabs the game's PRIVATE field
            // "netIsTreasureRoom"; required:false yields null rather than an
            // exception if a game update renames it. Netcode.NetBool is a
            // network-synchronized bool wrapper (safe in multiplayer).
            var netIsTreasureRoom = Helper.Reflection.GetField<Netcode.NetBool>(shaft, "netIsTreasureRoom", required: false);
            // Null-conditional chain: "?." only proceeds when the left side is
            // non-null; "?? false" provides the fallback. Together they unwrap
            // three nullable layers without nested if-statements.
            bool isTreasureRoom = netIsTreasureRoom?.GetValue()?.Value ?? false;

            // Treasure rooms hold the reward chests; floor 220 is a milestone
            // floor whose guaranteed chest we want even without a treasure room.
            if (!isTreasureRoom && shaft.mineLevel != 220)
                return;

            // Objects is the map's tile-object table; .Pairs walks it like a
            // normal Dictionary (key = tile position, value = the object there).
            foreach (var pair in shaft.Objects.Pairs)
            {
                // Pattern match again: only tiles holding a Chest qualify.
                if (pair.Value is Chest chest)
                {
                    // Tagged on an earlier warp? Skip — keeps this pass
                    // IDEMPOTENT (running it repeatedly changes nothing).
                    if (chest.modData.ContainsKey(GeneratedModDataKey))
                        continue;

                    // Stamp the tag; modData values are always strings.
                    chest.modData[GeneratedModDataKey] = "true";

                    if (Config.EnableCustomRewards)
                    {
                        // Empty the vanilla loot now; ChestPatches refills the
                        // chest with freshly rolled items on first open.
                        chest.Items.Clear();
                    }
                }
            }
        }

        // =====================================================================
        // EVENT HANDLER — fires once the title screen is ready: a safe moment
        // to talk to OTHER mods. We integrate Generic Mod Config Menu (GMCM),
        // the community-standard in-game settings UI. Every Add* call below
        // registers ONE settings row bound to a ModConfig property.
        // =====================================================================
        /// <summary>
        /// GameLaunched listener: attaches Generic Mod Config Menu (when that mod
        /// is installed) and registers all sections/options/sliders for this mod.
        /// </summary>
        /// <param name="sender">Event source (unused here).</param>
        /// <param name="e">Standard SMAPI event payload (no extra data for this event).</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Ask SMAPI's mod REGISTRY for another mod's public API. The GENERIC
            // type names the interface to cast to; the string is the other mod's
            // UniqueID. Result is null when it isn't installed — then we simply
            // skip building the settings page (graceful degradation).
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // Register this mod with GMCM, handing over two LAMBDA callbacks:
            //   reset -> runs when the player clicks "Reset to Defaults"
            //            (replace Config with a fresh, default-filled object)
            //   save  -> runs on "Save" (serialize Config back to config.json)
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            // From here on, every Add* call adds one menu row. Note they receive
            // GETTER/SETTER DELEGATES (Func<...>/Action<...>) instead of plain
            // values — GMCM evaluates them live whenever the page is drawn or
            // saved. The wrapper helpers at the bottom of this file explain
            // that pattern in detail.

            // General Section
            AddSection(configMenu, "general");
            AddBool(configMenu, "enable-custom-rewards", () => Config.EnableCustomRewards, v => Config.EnableCustomRewards = v);
            AddBool(configMenu, "exclude-cosmetics", () => Config.ExcludeCosmetics, v => Config.ExcludeCosmetics = v);
            AddBool(configMenu, "enable-depth-scaling", () => Config.EnableDepthScaling, v => Config.EnableDepthScaling = v);
            AddBool(configMenu, "scale-legendary-by-depth", () => Config.ScaleLegendaryByDepth, v => Config.ScaleLegendaryByDepth = v);

            // Progression & Gatekeeping Section
            AddSection(configMenu, "progression-gatekeeping");
            AddBool(configMenu, "gatekeep-mastery-items", () => Config.GatekeepMasteryItems, v => Config.GatekeepMasteryItems = v);
            AddBool(configMenu, "gatekeep-island-items", () => Config.GatekeepIslandItems, v => Config.GatekeepIslandItems = v);
            AddBool(configMenu, "gatekeep-qi-items", () => Config.GatekeepQiItems, v => Config.GatekeepQiItems = v);
            AddBool(configMenu, "gatekeep-mystery-boxes", () => Config.GatekeepMysteryBoxes, v => Config.GatekeepMysteryBoxes = v);
            AddBool(configMenu, "gatekeep-calico-eggs", () => Config.GatekeepCalicoEggs, v => Config.GatekeepCalicoEggs = v);
            AddBool(configMenu, "gatekeep-radioactive-items", () => Config.GatekeepRadioactiveItems, v => Config.GatekeepRadioactiveItems = v);
            AddBool(configMenu, "gatekeep-auto-petter", () => Config.GatekeepAutoPetter, v => Config.GatekeepAutoPetter = v);

            // Decaying Multi-Roll Section (Regular Chests)
            AddSection(configMenu, "decaying-rolls");
            AddInt(configMenu, "max-rolls", () => Config.MaxRolls, v => Config.MaxRolls = v, 1, 8);
            AddFloat(configMenu, "roll-2-chance", () => Config.Roll2Chance, v => Config.Roll2Chance = v);
            AddFloat(configMenu, "roll-3-chance", () => Config.Roll3Chance, v => Config.Roll3Chance = v);
            AddFloat(configMenu, "roll-4-chance", () => Config.Roll4Chance, v => Config.Roll4Chance = v);
            AddFloat(configMenu, "roll-5-chance", () => Config.Roll5Chance, v => Config.Roll5Chance = v);
            AddFloat(configMenu, "roll-6-chance", () => Config.Roll6Chance, v => Config.Roll6Chance = v);
            AddFloat(configMenu, "roll-7-chance", () => Config.Roll7Chance, v => Config.Roll7Chance = v);
            AddFloat(configMenu, "roll-8-chance", () => Config.Roll8Chance, v => Config.Roll8Chance = v);

            // Stack Multipliers Section (Regular Chests)
            AddSection(configMenu, "stack-multipliers");
            AddFloat(configMenu, "double-stack-chance", () => Config.DoubleStackChance, v => Config.DoubleStackChance = v);
            AddFloat(configMenu, "triple-stack-chance", () => Config.TripleStackChance, v => Config.TripleStackChance = v);
            AddFloat(configMenu, "quadruple-stack-chance", () => Config.QuadrupleStackChance, v => Config.QuadrupleStackChance = v);
            AddFloat(configMenu, "quintuple-stack-chance", () => Config.QuintupleStackChance, v => Config.QuintupleStackChance = v);

            // Floor 100 Special Chest Buff Section
            AddSection(configMenu, "floor-100-buffs");
            AddBool(configMenu, "enable-floor-100-buff", () => Config.EnableFloor100Buff, v => Config.EnableFloor100Buff = v);
            AddBool(configMenu, "floor-100-all-categories-equal", () => Config.Floor100AllCategoriesEqual, v => Config.Floor100AllCategoriesEqual = v);
            AddInt(configMenu, "floor-100-max-rolls", () => Config.Floor100MaxRolls, v => Config.Floor100MaxRolls = v, 1, 12);
            AddFloat(configMenu, "floor-100-roll-2-chance", () => Config.Floor100Roll2Chance, v => Config.Floor100Roll2Chance = v);
            AddFloat(configMenu, "floor-100-roll-3-chance", () => Config.Floor100Roll3Chance, v => Config.Floor100Roll3Chance = v);
            AddFloat(configMenu, "floor-100-roll-4-chance", () => Config.Floor100Roll4Chance, v => Config.Floor100Roll4Chance = v);
            AddFloat(configMenu, "floor-100-roll-5-chance", () => Config.Floor100Roll5Chance, v => Config.Floor100Roll5Chance = v);
            AddFloat(configMenu, "floor-100-roll-6-chance", () => Config.Floor100Roll6Chance, v => Config.Floor100Roll6Chance = v);
            AddFloat(configMenu, "floor-100-roll-7-chance", () => Config.Floor100Roll7Chance, v => Config.Floor100Roll7Chance = v);
            AddFloat(configMenu, "floor-100-roll-8-chance", () => Config.Floor100Roll8Chance, v => Config.Floor100Roll8Chance = v);
            AddFloat(configMenu, "floor-100-roll-9-chance", () => Config.Floor100Roll9Chance, v => Config.Floor100Roll9Chance = v);
            AddFloat(configMenu, "floor-100-roll-10-chance", () => Config.Floor100Roll10Chance, v => Config.Floor100Roll10Chance = v);
            AddFloat(configMenu, "floor-100-roll-11-chance", () => Config.Floor100Roll11Chance, v => Config.Floor100Roll11Chance = v);
            AddFloat(configMenu, "floor-100-roll-12-chance", () => Config.Floor100Roll12Chance, v => Config.Floor100Roll12Chance = v);
            AddFloat(configMenu, "floor-100-double-stack", () => Config.Floor100DoubleStackChance, v => Config.Floor100DoubleStackChance = v);
            AddFloat(configMenu, "floor-100-triple-stack", () => Config.Floor100TripleStackChance, v => Config.Floor100TripleStackChance = v);
            AddFloat(configMenu, "floor-100-quadruple-stack", () => Config.Floor100QuadrupleStackChance, v => Config.Floor100QuadrupleStackChance = v);
            AddFloat(configMenu, "floor-100-quintuple-stack", () => Config.Floor100QuintupleStackChance, v => Config.Floor100QuintupleStackChance = v);

            // Category Weights Section
            AddSection(configMenu, "category-weights");
            // "(float)" is a CAST: an explicit conversion of the double property
            // to float, which GMCM sliders require. Trailing numbers = the
            // slider's minimum, maximum, and step size.
            AddFloat(configMenu, "legendary-weight", () => (float)Config.LegendaryWeight, v => Config.LegendaryWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "agriculture-weight", () => (float)Config.AgricultureWeight, v => Config.AgricultureWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "mining-weight", () => (float)Config.MiningWeight, v => Config.MiningWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "fishing-weight", () => (float)Config.FishingWeight, v => Config.FishingWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "combat-weight", () => (float)Config.CombatWeight, v => Config.CombatWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "foraging-weight", () => (float)Config.ForagingWeight, v => Config.ForagingWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "lootbox-weight", () => (float)Config.LootboxWeight, v => Config.LootboxWeight = v, 0f, 100f, 1f);

            // Category Toggles
            AddSection(configMenu, "category-toggles");
            AddBool(configMenu, "enable-legendary-category", () => Config.EnableLegendaryCategory, v => Config.EnableLegendaryCategory = v);
            AddBool(configMenu, "enable-agriculture-category", () => Config.EnableAgricultureCategory, v => Config.EnableAgricultureCategory = v);
            AddBool(configMenu, "enable-mining-category", () => Config.EnableMiningCategory, v => Config.EnableMiningCategory = v);
            AddBool(configMenu, "enable-fishing-category", () => Config.EnableFishingCategory, v => Config.EnableFishingCategory = v);
            AddBool(configMenu, "enable-combat-category", () => Config.EnableCombatCategory, v => Config.EnableCombatCategory = v);
            AddBool(configMenu, "enable-foraging-category", () => Config.EnableForagingCategory, v => Config.EnableForagingCategory = v);
            AddBool(configMenu, "enable-lootbox-category", () => Config.EnableLootboxCategory, v => Config.EnableLootboxCategory = v);

            // Detailed Item Feature Toggles
            AddSection(configMenu, "item-toggles");
            AddBool(configMenu, "enable-fertilizers", () => Config.EnableFertilizers, v => Config.EnableFertilizers = v);
            AddBool(configMenu, "enable-auto-petter", () => Config.EnableAutoPetter, v => Config.EnableAutoPetter = v);
            AddBool(configMenu, "enable-radioactive-items", () => Config.EnableRadioactiveItems, v => Config.EnableRadioactiveItems = v);
            AddBool(configMenu, "enable-iridium-items", () => Config.EnableIridiumItems, v => Config.EnableIridiumItems = v);
            AddBool(configMenu, "enable-bombs", () => Config.EnableBombs, v => Config.EnableBombs = v);
            AddBool(configMenu, "enable-fishing-tackle", () => Config.EnableFishingTackle, v => Config.EnableFishingTackle = v);
            AddBool(configMenu, "enable-slime-eggs", () => Config.EnableSlimeEggs, v => Config.EnableSlimeEggs = v);
            AddBool(configMenu, "enable-combat-consumables", () => Config.EnableCombatConsumables, v => Config.EnableCombatConsumables = v);
            AddBool(configMenu, "enable-rare-seeds", () => Config.EnableRareSeeds, v => Config.EnableRareSeeds = v);
            AddBool(configMenu, "enable-coal", () => Config.EnableCoal, v => Config.EnableCoal = v);
            AddBool(configMenu, "enable-hardwood", () => Config.EnableHardwood, v => Config.EnableHardwood = v);
            AddBool(configMenu, "enable-mystery-boxes", () => Config.EnableMysteryBoxes, v => Config.EnableMysteryBoxes = v);
            AddBool(configMenu, "enable-omni-geodes", () => Config.EnableOmniGeodes, v => Config.EnableOmniGeodes = v);
            AddBool(configMenu, "enable-calico-eggs", () => Config.EnableCalicoEggs, v => Config.EnableCalicoEggs = v);

            // Fishing Treasure Chests GMCM Section
            AddSection(configMenu, "fishing-chests");
            AddBool(configMenu, "enable-fishing-chest-buff", () => Config.EnableFishingChestBuff, v => Config.EnableFishingChestBuff = v);
            AddInt(configMenu, "fishing-chest-min-rolls", () => Config.FishingChestMinRolls, v => Config.FishingChestMinRolls = v, 1, 10);
            AddInt(configMenu, "fishing-chest-max-rolls", () => Config.FishingChestMaxRolls, v => Config.FishingChestMaxRolls = v, 1, 12);
            AddInt(configMenu, "golden-chest-min-rolls", () => Config.GoldenChestMinRolls, v => Config.GoldenChestMinRolls = v, 1, 10);
            AddInt(configMenu, "golden-chest-max-rolls", () => Config.GoldenChestMaxRolls, v => Config.GoldenChestMaxRolls = v, 1, 12);
            AddBool(configMenu, "enable-fishing-trash-reroll-bonus", () => Config.EnableFishingTrashRerollBonus, v => Config.EnableFishingTrashRerollBonus = v);
        }

        // =====================================================================
        // GMCM WRAPPER HELPERS — tiny methods keeping the long registration
        // list above readable (one line per option).
        //
        // C# concepts in these signatures:
        //   * Func<bool>    — a DELEGATE: a variable that HOLDS A METHOD.
        //                     Func<T> takes nothing and RETURNS a T (our getter).
        //   * Action<bool>  — delegate taking a T, returning nothing (our setter).
        //   * Lambdas like "() => Config.EnableCustomRewards" build those
        //     delegates inline; because GMCM invokes them whenever it DRAWS or
        //     SAVES the menu, the UI always shows live config values.
        //   * Named arguments ("min: min") pass parameters by name for clarity.
        //   * Optional parameters ("int interval = 1") may be omitted by callers.
        // =====================================================================

        /// <summary>Adds one translated section-heading row separating option groups.</summary>
        /// <param name="menu">The GMCM api instance obtained in OnGameLaunched.</param>
        /// <param name="sectionKey">Final segment of the heading's translation key.</param>
        private void AddSection(IGenericModConfigMenuApi menu, string sectionKey)
        {
            // String interpolation composes the key, e.g. "config.section.general",
            // resolved against the i18n/*.json translation files.
            menu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get($"config.section.{sectionKey}"));
        }

        /// <summary>Adds a checkbox row bound to one bool config property.</summary>
        /// <param name="menu">Target GMCM api instance.</param>
        /// <param name="optionKey">Translation-key stem for the row's name/tooltip.</param>
        /// <param name="getter">Delegate returning the current value (evaluated live).</param>
        /// <param name="setter">Delegate storing a new value when the box is toggled.</param>
        private void AddBool(IGenericModConfigMenuApi menu, string optionKey, Func<bool> getter, Action<bool> setter)
        {
            menu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get($"config.{optionKey}.name"),
                tooltip: () => I18n.Get($"config.{optionKey}.tooltip"),
                getValue: getter,
                setValue: setter
            );
        }

        /// <summary>Adds a whole-number slider row bound to one int config property.</summary>
        /// <param name="menu">Target GMCM api instance.</param>
        /// <param name="optionKey">Translation-key stem for the row's name/tooltip.</param>
        /// <param name="getter">Delegate returning the current value.</param>
        /// <param name="setter">Delegate storing a new value.</param>
        /// <param name="min">Slider minimum.</param>
        /// <param name="max">Slider maximum.</param>
        /// <param name="interval">Slider step size (optional; defaults to 1).</param>
        private void AddInt(IGenericModConfigMenuApi menu, string optionKey, Func<int> getter, Action<int> setter, int min, int max, int interval = 1)
        {
            menu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get($"config.{optionKey}.name"),
                tooltip: () => I18n.Get($"config.{optionKey}.tooltip"),
                getValue: getter,
                setValue: setter,
                min: min,
                max: max,
                interval: interval
            );
        }

        /// <summary>Adds a fractional slider row bound to one float config property.</summary>
        /// <param name="menu">Target GMCM api instance.</param>
        /// <param name="optionKey">Translation-key stem for the row's name/tooltip.</param>
        /// <param name="getter">Delegate returning the current value.</param>
        /// <param name="setter">Delegate storing a new value.</param>
        /// <param name="min">Slider minimum (optional; defaults to 0.0).</param>
        /// <param name="max">Slider maximum (optional; defaults to 1.0).</param>
        /// <param name="interval">Slider step size (optional; defaults to 0.01).</param>
        private void AddFloat(IGenericModConfigMenuApi menu, string optionKey, Func<float> getter, Action<float> setter, float min = 0.0f, float max = 1.0f, float interval = 0.01f)
        {
            // Identical call to AddInt's — the float types of the delegates pick
            // the floating-point overload of AddNumberOption automatically.
            menu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get($"config.{optionKey}.name"),
                tooltip: () => I18n.Get($"config.{optionKey}.tooltip"),
                getValue: getter,
                setValue: setter,
                min: min,
                max: max,
                interval: interval
            );
        }
    }
}
