using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;
using Common;

namespace BetterForge
{
    /// <summary>
    /// The entry point of the mod. SMAPI finds this class automatically because it
    /// inherits from <see cref="Mod"/> and calls its <see cref="Entry"/> method once
    /// when the game starts. Everything that needs setting up (config, Harmony hooks,
    /// event subscriptions, the config menu) is wired together here.
    /// </summary>
    public class ModEntry : Mod
    {
        // Static properties so OTHER files in this mod can access shared services
        // without needing a reference to this class instance.
        // "= null!" silences the compiler's "may be null" warning: we know Entry()
        // assigns them before anything else uses them.

        /// <summary>The mod's user settings loaded from config.json.</summary>
        public static ModConfig Config { get; private set; } = null!;

        /// <summary>SMAPI's console/log writer, for printing debug or error messages.</summary>
        public static IMonitor ModMonitor { get; private set; } = null!;

        /// <summary>Translation helper that reads text from the i18n folder
        /// (default.json / vi.json) based on the player's language.</summary>
        public static ITranslationHelper I18n { get; private set; } = null!;

        /// <summary>General-purpose SMAPI helper (events, content loading, mod registry).</summary>
        public static IModHelper ModHelper { get; private set; } = null!;

        /// <summary>
        /// Called by SMAPI once at game launch. Order matters here:
        /// 1) load settings, 2) share them with the patch classes,
        /// 3) install Harmony hooks into the game code, 4) subscribe to events.
        /// </summary>
        /// <param name="helper">SMAPI passes this in; it is the gateway to all mod APIs.</param>
        public override void Entry(IModHelper helper)
        {
            // Read config.json from the mod folder into a typed C# object.
            // If the file doesn't exist yet, SMAPI creates it with default values.
            Config = helper.ReadConfig<ModConfig>();

            // Expose SMAPI's built-in Monitor through our own static property too.
            ModMonitor = Monitor;

            // Shortcuts for translations and the helper, stored for other classes.
            I18n = helper.Translation;
            ModHelper = helper;

            // Give both patch classes access to config + logger before any hook fires.
            TrinketPatches.Initialize(Config, Monitor);
            EnchantmentPatches.Initialize(Config, Monitor);

            // Create a Harmony instance named after this mod, then apply every patch.
            // Harmony lets us inject our own code into the game's methods at runtime.
            var harmony = new Harmony(ModManifest.UniqueID);
            TrinketPatches.Apply(harmony);
            EnchantmentPatches.Apply(harmony);

            // Subscribe ("+=" adds our handler to SMAPI's event list) so these
            // methods run whenever the corresponding event happens in-game.
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            Helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        /// <summary>
        /// Runs ~60 times per second while the game is open. We only act once every
        /// 30 ticks (twice per second) to keep the passive ascension luck buff in sync
        /// with how many ascended trinkets the player has equipped.
        /// </summary>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            // IsMultipleOf(30) throttles us to twice a second; the world-ready checks
            // prevent touching the player object before a save file is actually loaded.
            if (e.IsMultipleOf(30) && Context.IsWorldReady && Game1.player != null)
            {
                TrinketAscensionLogic.UpdateAscensionLuckBuff(Game1.player);
            }
        }

        /// <summary>
        /// Fires whenever the game loads any data asset. When it asks for the list of
        /// big craftables we replace the Anvil's description with our translated text,
        /// because vanilla doesn't mention trinket reforging on it.
        /// </summary>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // Only react to one specific asset: the big-craftable definition table.
            if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            {
                // e.Edit registers a callback that runs when the asset data is built.
                e.Edit(asset =>
                {
                    // Treat the asset as a dictionary keyed by craftable ID.
                    var data = asset.AsDictionary<string, BigCraftableData>().Data;

                    // Fetch the localized description once, then apply it to both IDs
                    // under which the Anvil can exist ("Anvil" is the 1.6 ID, "289" the legacy numeric ID).
                    string desc = I18n.Get("anvil.description");

                    if (data.TryGetValue("Anvil", out var anvilData))
                    {
                        anvilData.Description = desc;
                    }
                    if (data.TryGetValue("289", out var anvilData289))
                    {
                        anvilData289.Description = desc;
                    }
                });
            }
        }

        /// <summary>
        /// Runs after the title screen finishes loading — the safe moment to connect
        /// to Generic Mod Config Menu (GMCM) and register every config option as a
        /// clickable control in its in-game settings UI.
        /// </summary>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Ask SMAPI for GMCM's API. Returns null if that mod isn't installed,
            // in which case we simply skip the whole menu (the mod still works).
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // Register this mod with GMCM and tell it how to reset / save settings.
            // The lambdas ( () => ... ) are little inline functions GMCM calls later.
            configMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    // "Reset to defaults": swap in a brand-new config object, then
                    // push it to the patch classes so they use the fresh values.
                    Config = new ModConfig();
                    TrinketPatches.Config = Config;
                    EnchantmentPatches.Config = Config;
                },
                save: () =>
                {
                    // Write the current values back to config.json on disk and
                    // re-sync the patch classes in case anything changed.
                    Helper.WriteConfig(Config);
                    TrinketPatches.Config = Config;
                    EnchantmentPatches.Config = Config;
                }
            );

            // Section 1: Weapon & Tool Enchanting Options
            // Each AddBoolOption/AddNumberOption binds one config property to a
            // menu control. "getValue"/"setValue" are read/written live by GMCM,
            // and name/tooltip lambdas fetch translated text at display time.
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.enchanting")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.uniform-chances.name"),
                tooltip: () => I18n.Get("config.uniform-chances.tooltip"),
                getValue: () => Config.UniformEnchantmentChances,
                setValue: value => Config.UniformEnchantmentChances = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.randomize-seed.name"),
                tooltip: () => I18n.Get("config.randomize-seed.tooltip"),
                getValue: () => Config.RandomizeEnchantmentSeed,
                setValue: value => Config.RandomizeEnchantmentSeed = value
            );

            // Section 2: Trinket Reforging & Anvil Options
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.reforging")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.prevent-downgrades.name"),
                tooltip: () => I18n.Get("config.prevent-downgrades.tooltip"),
                getValue: () => Config.PreventDowngrades,
                setValue: value => Config.PreventDowngrades = value
            );

            // Number option with min/max/interval = a slider in the GMCM UI.
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.iridium-cost.name"),
                tooltip: () => I18n.Get("config.iridium-cost.tooltip"),
                getValue: () => Config.IridiumBarCost,
                setValue: value => Config.IridiumBarCost = value,
                min: 1,
                max: 10,
                interval: 1
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-messages.name"),
                tooltip: () => I18n.Get("config.show-messages.tooltip"),
                getValue: () => Config.ShowReforgeSuccessMessage,
                setValue: value => Config.ShowReforgeSuccessMessage = value
            );
        }
    }
}
