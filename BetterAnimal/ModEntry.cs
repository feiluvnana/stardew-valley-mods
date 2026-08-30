using Common;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BetterAnimal
{
    /// <summary>
    /// The main SMAPI mod entry point for BetterAnimal.
    /// Manages configuration, Harmony patches, asset data balancing, and GMCM menu integration.
    /// </summary>
    public sealed class ModEntry : Mod
    {
        public static ModConfig Config { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static IModHelper ModHelper { get; private set; } = null!;
        public static ITranslationHelper I18n { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor;
            ModHelper = helper;
            I18n = helper.Translation;

            // Apply Harmony patches
            var harmony = new Harmony(ModManifest.UniqueID);
            FarmAnimalPatches.Apply(harmony);

            // Asset requested hooks
            helper.Events.Content.AssetRequested += AnimalDataBalancer.OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Monitor.Log("BetterAnimal loaded successfully: Duck dual-drops, rabbit productivity, and small livestock balancing active.", LogLevel.Debug);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    Config = new ModConfig();
                    Helper.WriteConfig(Config);
                    InvalidateAssetCaches();
                },
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    InvalidateAssetCaches();
                }
            );

            // Sub-page navigation links on root page
            configMenu.AddPageLink(ModManifest, "ducks", () => I18n.Get("config.section.ducks"));
            configMenu.AddPageLink(ModManifest, "rabbits", () => I18n.Get("config.section.rabbits"));
            configMenu.AddPageLink(ModManifest, "sheep", () => I18n.Get("config.section.sheep"));

            // ---------------- Sub-Page 1: Duck Balance ----------------
            configMenu.AddPage(ModManifest, "ducks", () => I18n.Get("config.section.ducks"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-duck-dual-drop.name"),
                tooltip: () => I18n.Get("config.enable-duck-dual-drop.tooltip"),
                getValue: () => Config.EnableDuckDualDrop,
                setValue: value => Config.EnableDuckDualDrop = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.duck-dual-drop-min-hearts.name"),
                tooltip: () => I18n.Get("config.duck-dual-drop-min-hearts.tooltip"),
                getValue: () => Config.DuckDualDropMinHearts,
                setValue: value => Config.DuckDualDropMinHearts = value,
                min: 1,
                max: 5,
                interval: 1
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.duck-dual-drop-chance.name"),
                tooltip: () => I18n.Get("config.duck-dual-drop-chance.tooltip"),
                getValue: () => Config.DuckDualDropChance,
                setValue: value => Config.DuckDualDropChance = value,
                min: 0.1f,
                max: 1.0f,
                interval: 0.05f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-duck-feather-loom.name"),
                tooltip: () => I18n.Get("config.enable-duck-feather-loom.tooltip"),
                getValue: () => Config.EnableDuckFeatherLoom,
                setValue: value => Config.EnableDuckFeatherLoom = value
            );

            // ---------------- Sub-Page 2: Rabbit Productivity ----------------
            configMenu.AddPage(ModManifest, "rabbits", () => I18n.Get("config.section.rabbits"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-rabbit-cooldown-reduction.name"),
                tooltip: () => I18n.Get("config.enable-rabbit-cooldown-reduction.tooltip"),
                getValue: () => Config.EnableRabbitCooldownReduction,
                setValue: value => Config.EnableRabbitCooldownReduction = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.rabbit-days-to-produce.name"),
                tooltip: () => I18n.Get("config.rabbit-days-to-produce.tooltip"),
                getValue: () => Config.RabbitDaysToProduce,
                setValue: value => Config.RabbitDaysToProduce = value,
                min: 1,
                max: 4,
                interval: 1
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-rabbit-multi-drop.name"),
                tooltip: () => I18n.Get("config.enable-rabbit-multi-drop.tooltip"),
                getValue: () => Config.EnableRabbitMultiDrop,
                setValue: value => Config.EnableRabbitMultiDrop = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.rabbit-multi-drop-chance.name"),
                tooltip: () => I18n.Get("config.rabbit-multi-drop-chance.tooltip"),
                getValue: () => Config.RabbitMultiDropChance,
                setValue: value => Config.RabbitMultiDropChance = value,
                min: 0.05f,
                max: 1.0f,
                interval: 0.05f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-rabbit-foot-rebalance.name"),
                tooltip: () => I18n.Get("config.enable-rabbit-foot-rebalance.tooltip"),
                getValue: () => Config.EnableRabbitFootRebalance,
                setValue: value => Config.EnableRabbitFootRebalance = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.rabbit-foot-base-price.name"),
                tooltip: () => I18n.Get("config.rabbit-foot-base-price.tooltip"),
                getValue: () => Config.RabbitFootBasePrice,
                setValue: value => Config.RabbitFootBasePrice = value,
                min: 200,
                max: 3000,
                interval: 25
            );

            // ---------------- Sub-Page 3: Sheep & Wool ----------------
            configMenu.AddPage(ModManifest, "sheep", () => I18n.Get("config.section.sheep"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-sheep-daily-shear.name"),
                tooltip: () => I18n.Get("config.enable-sheep-daily-shear.tooltip"),
                getValue: () => Config.EnableSheepDailyShearAtMaxHearts,
                setValue: value => Config.EnableSheepDailyShearAtMaxHearts = value
            );
        }

        private void InvalidateAssetCaches()
        {
            Helper.GameContent.InvalidateCache("Data/FarmAnimals");
            Helper.GameContent.InvalidateCache("Data/Objects");
            Helper.GameContent.InvalidateCache("Data/Machines");
        }
    }
}
