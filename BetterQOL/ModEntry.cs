using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Machines;
using Common;

// ModEntry is the "front door" every SMAPI mod must have. When Stardew Valley
// starts, the SMAPI mod loader finds this class, creates one instance of it, and
// calls its Entry() method exactly once. Everything else in the mod gets wired up
// here: loading the user's settings, installing Harmony patches into the game's
// code, initializing feature modules, and subscribing to SMAPI events.
namespace BetterQOL
{
    /// <summary>
    /// Main mod class, inheriting from SMAPI's Mod base class (that's what
    /// ": Mod" means - ModEntry gets all of the base class's members "for free").
    /// SMAPI discovers this class automatically because it derives from Mod.
    /// </summary>
    public class ModEntry : Mod
    {
        // The four properties below are STATIC, meaning they belong to the class
        // itself instead of to one particular object - so any other file in the mod
        // can simply write "ModEntry.Config" or "ModEntry.I18n" to reach them.
        // They act as the mod's shared service hub.
        // "= null!" tells the compiler "these will definitely be assigned in Entry(),
        // before anything reads them"; the ! suppresses the null warning until then.

        /// <summary>The user's saved settings, deserialized from this mod's config.json.</summary>
        public static ModConfig Config { get; private set; } = null!;
        /// <summary>SMAPI's logging service (writes to console + log file).</summary>
        public static IMonitor ModMonitor { get; private set; } = null!;
        /// <summary>Translation reader pulling strings from the mod's i18n folder.</summary>
        public static ITranslationHelper I18n { get; private set; } = null!;
        /// <summary>Toolkit for SMAPI services: events, input, content edits, mod registry.</summary>
        public static IModHelper ModHelper { get; private set; } = null!;

        /// <summary>
        /// SMAPI's required startup hook, called ONCE when mods finish loading.
        /// "override" means we're replacing the virtual Entry() declared on the Mod
        /// base class with our own version. Order matters here: config first (later
        /// steps read it), then Harmony patches, then modules, then event hookup.
        /// </summary>
        /// <param name="helper">SMAPI's helper object granting access to all mod APIs.</param>
        public override void Entry(IModHelper helper)
        {
            // Read config.json into a fresh ModConfig object (SMAPI creates the file
            // with class defaults on first run). "<ModConfig>" is a GENERIC type
            // argument: it tells the reusable ReadConfig method which class to build.
            Config = helper.ReadConfig<ModConfig>();
            // Copy our inherited instances into the static properties so every other
            // class can use them without needing a reference to this ModEntry object.
            ModMonitor = Monitor;
            I18n = helper.Translation;
            ModHelper = helper;

            // Harmony is a "runtime patching" library. It lets a mod inject extra code
            // INTO the game's own methods while the game runs - without modifying any
            // game files. One shared instance (named after our mod's unique id) will
            // collect every patch this mod applies.
            var harmony = new Harmony(ModManifest.UniqueID);
            // Each feature file below registers its own Harmony patches through the
            // shared instance (stack-size overrides, in-menu tooltip additions, ...).
            StackablePatches.Apply(harmony, Monitor);
            MenuTooltipPatch.Apply(harmony, Monitor);

            // Features that don't need Harmony are initialized directly with SMAPI
            // services instead (an event-driven overlay and a custom menu button).
            GeodeMenuHandler.Initialize(helper, Monitor);
            HoverInfoOverlay.Initialize(helper, Monitor);

            // EVENTS: in C#, "+=" SUBSCRIBES a method to an event - like plugging it
            // into a doorbell. Whenever SMAPI "rings" (a button is pressed, an asset
            // loads, the launch completes), every subscribed method gets invoked.
            // This is the main way mods react to gameplay without patching dozens of
            // individual game methods.
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Monitor.Log("BetterQOL initialized successfully: Extended Stackable limits, Geode Cracking overhaul, Hover Information, and Lookup Anything are active.", LogLevel.Debug);
        }

        /// <summary>
        /// Opens/closes the Lookup Anything window when the configured key is
        /// pressed. Runs on EVERY button press while the game runs (it's subscribed
        /// to SMAPI's ButtonPressed event), so it exits fast unless the pressed
        /// button matches the user's lookup hotkey.
        /// </summary>
        /// <param name="sender">The event source (SMAPI); conventionally unused.</param>
        /// <param name="e">Details of the press: which button, cursor position...</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // The "?" in "object?" marks a nullable reference: sender may be null,
            // and the compiler makes us acknowledge that. Here we simply ignore it.
            // Two ways to trigger lookup: a keyboard key OR a controller button.
            // Each side checks "!= None" (None means "disabled") AND matches what was
            // pressed; "||" combines them so either input works.
            bool isLookupTriggered = (Config.LookupKey != SButton.None && e.Button == Config.LookupKey)
                                  || (Config.ControllerLookupKey != SButton.None && e.Button == Config.ControllerLookupKey);

            // Feature turned off, or wrong button pressed: leave immediately.
            // A bare "return" just ends a void method early (a "guard clause").
            if (!Config.EnableLookupAnything || !isLookupTriggered)
                return;

            // Game1 is the game's giant central static class holding world/menu
            // state. activeClickableMenu is whatever menu is open right now
            // (null = none). "is LookupMenu" is a TYPE TEST against our own window.
            if (Game1.activeClickableMenu is LookupMenu)
            {
                // Already open -> pressing the key again closes it (toggle behaviour).
                Game1.exitActiveMenu();
                // Suppress() swallows the input so the game itself never sees this key.
                Helper.Input.Suppress(e.Button);
                return;
            }

            // Ask the finder (another file) what object/tile/NPC is under the cursor.
            var subject = LookupTargetFinder.FindTargetSubject();
            // "!= null" guards against hovering over nothing lookup-worthy.
            if (subject != null)
            {
                // Assigning the field both opens our menu and replaces any prior one.
                Game1.activeClickableMenu = new LookupMenu(subject);
                Helper.Input.Suppress(e.Button);
            }
        }

        /// <summary>
        /// Edits the game's raw DATA ASSETS while they load - here Data/Machines,
        /// the table describing every machine's inputs, outputs, and timing. SMAPI
        /// fires AssetRequested whenever the game asks for an asset, letting mods
        /// tweak it in memory without touching any game files.
        /// </summary>
        /// <param name="sender">SMAPI (unused).</param>
        /// <param name="e">Identifies the asset loading and accepts edit callbacks.</param>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // Assets are addressed like file paths. IsEquivalentTo ignores locale
            // suffixes ("Data/Machines.fr-FR"), so one check covers every language.
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
            {
                // Queue a LAMBDA ("asset => ..." is an inline anonymous function)
                // that SMAPI runs when it actually builds the asset data.
                e.Edit(asset =>
                {
                    // Reinterpret the asset as a string-keyed dictionary of
                    // MachineData rows - one entry per machine in the game.
                    var data = asset.AsDictionary<string, MachineData>().Data;
                    // "(BC)182" is the Geode Crusher big-craftable's internal id.
                    // TryGetValue avoids a crash if another mod removed that row,
                    // handing the found row back through "out var geodeCrusher".
                    if (data.TryGetValue("(BC)182", out var geodeCrusher))
                    {
                        if (Config.InstantGeodeCrusher)
                        {
                            if (geodeCrusher.OutputRules != null)
                            {
                                // Machines take real-time MINUTES to finish; that delay
                                // lives in MinutesUntilReady on each output rule. Zeroing
                                // every rule makes the crusher finish instantly.
                                foreach (var rule in geodeCrusher.OutputRules)
                                {
                                    rule.MinutesUntilReady = 0;
                                }
                            }
                        }

                        if (!Config.GeodeCrusherRequiresCoal)
                        {
                            // "?." is the null-conditional operator: call Clear() only
                            // when the list exists; a null left side yields null instead
                            // of throwing NullReferenceException.
                            geodeCrusher.AdditionalConsumedItems?.Clear();
                            // Clearing these messages removes the "needs coal" refusal.
                            geodeCrusher.InvalidCountMessage = null;
                            if (geodeCrusher.OutputRules != null)
                            {
                                foreach (var rule in geodeCrusher.OutputRules)
                                {
                                    rule.InvalidCountMessage = null;
                                }
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Runs once the game (and all mods) are fully launched. That timing matters:
        /// we ask ANOTHER MOD - Generic Mod Config Menu (GMCM) - for its programming
        /// API here, because mods load in unpredictable order and it may not exist
        /// earlier. Builds this mod's entire in-game settings screen row by row.
        /// </summary>
        /// <param name="sender">SMAPI (unused).</param>
        /// <param name="e">Event arguments (unused).</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // GetApi<IGenericModConfigMenuApi> asks the mod registry for GMCM's
            // interface. "<T>" is a generic parameter naming the interface we expect;
            // it returns null when GMCM isn't installed, in which case the guard
            // below skips building the settings UI instead of crashing.
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // Register() gives GMCM our reset/save behaviours. The arguments are
            // LAMBDAS ("() => ..." = a small function with no parameters): GMCM stores
            // them and calls them later, when the player clicks those buttons.
            configMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    // "Reset to defaults": discard current settings for a brand-new,
                    // default-filled object, write it to disk, refresh game assets.
                    Config = new ModConfig();
                    Helper.WriteConfig(Config);
                    InvalidateAssetCaches();
                },
                save: () =>
                {
                    // Save button: persist the (possibly edited) config to config.json.
                    Helper.WriteConfig(Config);
                    InvalidateAssetCaches();
                }
            );

            // ---------------- Section 1: Blacksmith Geode Cracking ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.blacksmith")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.instant-cracking.name"),
                tooltip: () => I18n.Get("config.instant-cracking.tooltip"),
                getValue: () => Config.InstantCracking,
                setValue: value => Config.InstantCracking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-crack-all-button.name"),
                tooltip: () => I18n.Get("config.show-crack-all-button.tooltip"),
                getValue: () => Config.ShowCrackAllButton,
                setValue: value => Config.ShowCrackAllButton = value
            );

            // Number option rendered as a slider: min/max clamp allowed values and
            // "interval" is the step size between clicks (1..999 step 10).
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.bulk-batch-size.name"),
                tooltip: () => I18n.Get("config.bulk-batch-size.tooltip"),
                getValue: () => Config.BulkBatchSize,
                setValue: value => Config.BulkBatchSize = value,
                min: 1,
                max: 999,
                interval: 10
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-summary-toast.name"),
                tooltip: () => I18n.Get("config.show-summary-toast.tooltip"),
                getValue: () => Config.ShowSummaryToast,
                setValue: value => Config.ShowSummaryToast = value
            );

            // ---------------- Section 2: Farm Machine Options ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.farm-machines")
            );

            // The two machine toggles below can't use the simple one-line setter:
            // they change Data/Machines content, so their setValue lambdas ALSO
            // invalidate that asset's cache to make the change apply immediately.
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.instant-geode-crusher.name"),
                tooltip: () => I18n.Get("config.instant-geode-crusher.tooltip"),
                getValue: () => Config.InstantGeodeCrusher,
                setValue: value =>
                {
                    Config.InstantGeodeCrusher = value;
                    InvalidateAssetCaches();
                }
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.geode-crusher-requires-coal.name"),
                tooltip: () => I18n.Get("config.geode-crusher-requires-coal.tooltip"),
                getValue: () => Config.GeodeCrusherRequiresCoal,
                setValue: value =>
                {
                    Config.GeodeCrusherRequiresCoal = value;
                    InvalidateAssetCaches();
                }
            );

            // ---------------- Section 3: Item Stacking Options ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.stackable")
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.max-stack-size.name"),
                tooltip: () => I18n.Get("config.max-stack-size.tooltip"),
                getValue: () => Config.MaxStackSize,
                setValue: value => Config.MaxStackSize = value,
                min: 1,
                max: 9999
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-tackle-stacking.name"),
                tooltip: () => I18n.Get("config.enable-tackle-stacking.tooltip"),
                getValue: () => Config.EnableTackleStacking,
                setValue: value => Config.EnableTackleStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-trinket-stacking.name"),
                tooltip: () => I18n.Get("config.enable-trinket-stacking.tooltip"),
                getValue: () => Config.EnableTrinketStacking,
                setValue: value => Config.EnableTrinketStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-furniture-stacking.name"),
                tooltip: () => I18n.Get("config.enable-furniture-stacking.tooltip"),
                getValue: () => Config.EnableFurnitureStacking,
                setValue: value => Config.EnableFurnitureStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-ring-stacking.name"),
                tooltip: () => I18n.Get("config.enable-ring-stacking.tooltip"),
                getValue: () => Config.EnableRingStacking,
                setValue: value => Config.EnableRingStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-clothing-and-hat-stacking.name"),
                tooltip: () => I18n.Get("config.enable-clothing-and-hat-stacking.tooltip"),
                getValue: () => Config.EnableClothingAndHatStacking,
                setValue: value => Config.EnableClothingAndHatStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-boots-stacking.name"),
                tooltip: () => I18n.Get("config.enable-boots-stacking.tooltip"),
                getValue: () => Config.EnableBootsStacking,
                setValue: value => Config.EnableBootsStacking = value
            );

            // ---------------- Section 4: Hover Information & Timers (UI Info Suite 2 Style) ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.hover-info")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-crop-hover.name"),
                tooltip: () => I18n.Get("config.enable-crop-hover.tooltip"),
                getValue: () => Config.EnableCropHover,
                setValue: value => Config.EnableCropHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-machine-hover.name"),
                tooltip: () => I18n.Get("config.enable-machine-hover.tooltip"),
                getValue: () => Config.EnableMachineHover,
                setValue: value => Config.EnableMachineHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-tree-hover.name"),
                tooltip: () => I18n.Get("config.enable-tree-hover.tooltip"),
                getValue: () => Config.EnableTreeHover,
                setValue: value => Config.EnableTreeHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-animal-hover.name"),
                tooltip: () => I18n.Get("config.enable-animal-hover.tooltip"),
                getValue: () => Config.EnableAnimalHover,
                setValue: value => Config.EnableAnimalHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-water-and-fertilizer.name"),
                tooltip: () => I18n.Get("config.show-water-and-fertilizer.tooltip"),
                getValue: () => Config.ShowWaterAndFertilizer,
                setValue: value => Config.ShowWaterAndFertilizer = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-item-icon-in-tooltip.name"),
                tooltip: () => I18n.Get("config.show-item-icon-in-tooltip.tooltip"),
                getValue: () => Config.ShowItemIconInTooltip,
                setValue: value => Config.ShowItemIconInTooltip = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-exact-finish-time.name"),
                tooltip: () => I18n.Get("config.show-exact-finish-time.tooltip"),
                getValue: () => Config.ShowExactFinishTime,
                setValue: value => Config.ShowExactFinishTime = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-item-sell-price-on-hover.name"),
                tooltip: () => I18n.Get("config.show-item-sell-price-on-hover.tooltip"),
                getValue: () => Config.ShowItemSellPriceOnHover,
                setValue: value => Config.ShowItemSellPriceOnHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-bundle-need-on-hover.name"),
                tooltip: () => I18n.Get("config.show-bundle-need-on-hover.tooltip"),
                getValue: () => Config.ShowBundleNeedOnHover,
                setValue: value => Config.ShowBundleNeedOnHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-museum-need-on-hover.name"),
                tooltip: () => I18n.Get("config.show-museum-need-on-hover.tooltip"),
                getValue: () => Config.ShowMuseumNeedOnHover,
                setValue: value => Config.ShowMuseumNeedOnHover = value
            );

            // Keybind option: lets the player bind any keyboard/controller button.
            configMenu.AddKeybind(
                mod: ModManifest,
                name: () => I18n.Get("config.hover-hotkey.name"),
                tooltip: () => I18n.Get("config.hover-hotkey.tooltip"),
                getValue: () => Config.HoverHotkey,
                setValue: value => Config.HoverHotkey = value
            );

            // ---------------- Section 5: Lookup Anything ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.lookup-anything")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-lookup-anything.name"),
                tooltip: () => I18n.Get("config.enable-lookup-anything.tooltip"),
                getValue: () => Config.EnableLookupAnything,
                setValue: value => Config.EnableLookupAnything = value
            );

            configMenu.AddKeybind(
                mod: ModManifest,
                name: () => I18n.Get("config.lookup-key.name"),
                tooltip: () => I18n.Get("config.lookup-key.tooltip"),
                getValue: () => Config.LookupKey,
                setValue: value => Config.LookupKey = value
            );

            configMenu.AddKeybind(
                mod: ModManifest,
                name: () => I18n.Get("config.controller-lookup-key.name"),
                tooltip: () => I18n.Get("config.controller-lookup-key.tooltip"),
                getValue: () => Config.ControllerLookupKey,
                setValue: value => Config.ControllerLookupKey = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-gift-tastes.name"),
                tooltip: () => I18n.Get("config.show-gift-tastes.tooltip"),
                getValue: () => Config.ShowGiftTastes,
                setValue: value => Config.ShowGiftTastes = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-item-recipes.name"),
                tooltip: () => I18n.Get("config.show-item-recipes.tooltip"),
                getValue: () => Config.ShowItemRecipes,
                setValue: value => Config.ShowItemRecipes = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-bundle-and-museum-info.name"),
                tooltip: () => I18n.Get("config.show-bundle-and-museum-info.tooltip"),
                getValue: () => Config.ShowBundleAndMuseumInfo,
                setValue: value => Config.ShowBundleAndMuseumInfo = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-community-center-progress.name"),
                tooltip: () => I18n.Get("config.show-community-center-progress.tooltip"),
                getValue: () => Config.ShowCommunityCenterProgress,
                setValue: value => Config.ShowCommunityCenterProgress = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-friendship-overview.name"),
                tooltip: () => I18n.Get("config.show-friendship-overview.tooltip"),
                getValue: () => Config.ShowFriendshipOverview,
                setValue: value => Config.ShowFriendshipOverview = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-progress-and-perfection.name"),
                tooltip: () => I18n.Get("config.show-progress-and-perfection.tooltip"),
                getValue: () => Config.ShowProgressAndPerfection,
                setValue: value => Config.ShowProgressAndPerfection = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-mine-and-guild-progress.name"),
                tooltip: () => I18n.Get("config.show-mine-and-guild-progress.tooltip"),
                getValue: () => Config.ShowMineAndGuildProgress,
                setValue: value => Config.ShowMineAndGuildProgress = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-museum-progress.name"),
                tooltip: () => I18n.Get("config.show-museum-progress.tooltip"),
                getValue: () => Config.ShowMuseumProgress,
                setValue: value => Config.ShowMuseumProgress = value
            );
        }

        /// <summary>
        /// Tells SMAPI to throw away its cached copy of Data/Machines, so the next
        /// time the game asks for it our AssetRequested edit re-runs using CURRENT
        /// settings. Without this, changed machine options wouldn't apply until the
        /// game was restarted.
        /// </summary>
        private void InvalidateAssetCaches()
        {
            Helper.GameContent.InvalidateCache("Data/Machines");
        }
    }
}
