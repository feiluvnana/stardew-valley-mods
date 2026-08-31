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

            // Mod description on Root Page
            configMenu.AddParagraph(ModManifest, () => I18n.Get("mod.description"));

            // Sub-page navigation links on root page
            configMenu.AddPageLink(ModManifest, "ducks", () => I18n.Get("config.section.ducks"));
            configMenu.AddPageLink(ModManifest, "rabbits", () => I18n.Get("config.section.rabbits"));
            configMenu.AddPageLink(ModManifest, "sheep", () => I18n.Get("config.section.sheep"));
            configMenu.AddPageLink(ModManifest, "dinosaurs", () => I18n.Get("config.section.dinosaurs"));
            configMenu.AddPageLink(ModManifest, "goats", () => I18n.Get("config.section.goats"));
            configMenu.AddPageLink(ModManifest, "void_chickens", () => I18n.Get("config.section.void-chickens"));
            configMenu.AddPageLink(ModManifest, "slime_hutch", () => I18n.Get("config.section.slime-hutch"));

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

            // ---------------- Sub-Page 3: Sheep & Wool ----------------
            configMenu.AddPage(ModManifest, "sheep", () => I18n.Get("config.section.sheep"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-sheep-daily-shear.name"),
                tooltip: () => I18n.Get("config.enable-sheep-daily-shear.tooltip"),
                getValue: () => Config.EnableSheepDailyShearAtMaxHearts,
                setValue: value => Config.EnableSheepDailyShearAtMaxHearts = value
            );

            // ---------------- Sub-Page 4: Dinosaur Productivity ----------------
            configMenu.AddPage(ModManifest, "dinosaurs", () => I18n.Get("config.section.dinosaurs"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-dinosaur-cooldown-reduction.name"),
                tooltip: () => I18n.Get("config.enable-dinosaur-cooldown-reduction.tooltip"),
                getValue: () => Config.EnableDinosaurCooldownReduction,
                setValue: value => Config.EnableDinosaurCooldownReduction = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.dinosaur-days-to-produce.name"),
                tooltip: () => I18n.Get("config.dinosaur-days-to-produce.tooltip"),
                getValue: () => Config.DinosaurDaysToProduce,
                setValue: value => Config.DinosaurDaysToProduce = value,
                min: 1,
                max: 7,
                interval: 1
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-dinosaur-multi-drop.name"),
                tooltip: () => I18n.Get("config.enable-dinosaur-multi-drop.tooltip"),
                getValue: () => Config.EnableDinosaurMultiDrop,
                setValue: value => Config.EnableDinosaurMultiDrop = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.dinosaur-multi-drop-chance.name"),
                tooltip: () => I18n.Get("config.dinosaur-multi-drop-chance.tooltip"),
                getValue: () => Config.DinosaurMultiDropChance,
                setValue: value => Config.DinosaurMultiDropChance = value,
                min: 0.05f,
                max: 1.0f,
                interval: 0.05f
            );

            // ---------------- Sub-Page 5: Goat Productivity ----------------
            configMenu.AddPage(ModManifest, "goats", () => I18n.Get("config.section.goats"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-goat-multi-drop.name"),
                tooltip: () => I18n.Get("config.enable-goat-multi-drop.tooltip"),
                getValue: () => Config.EnableGoatMultiDrop,
                setValue: value => Config.EnableGoatMultiDrop = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.goat-multi-drop-chance.name"),
                tooltip: () => I18n.Get("config.goat-multi-drop-chance.tooltip"),
                getValue: () => Config.GoatMultiDropChance,
                setValue: value => Config.GoatMultiDropChance = value,
                min: 0.05f,
                max: 1.0f,
                interval: 0.05f
            );

            // ---------------- Sub-Page 6: Void Chicken Productivity ----------------
            configMenu.AddPage(ModManifest, "void_chickens", () => I18n.Get("config.section.void-chickens"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-void-chicken-multi-drop.name"),
                tooltip: () => I18n.Get("config.enable-void-chicken-multi-drop.tooltip"),
                getValue: () => Config.EnableVoidChickenMultiDrop,
                setValue: value => Config.EnableVoidChickenMultiDrop = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.void-chicken-multi-drop-chance.name"),
                tooltip: () => I18n.Get("config.void-chicken-multi-drop-chance.tooltip"),
                getValue: () => Config.VoidChickenMultiDropChance,
                setValue: value => Config.VoidChickenMultiDropChance = value,
                min: 0.05f,
                max: 1.0f,
                interval: 0.05f
            );

            // ---------------- Sub-Page 7: Slime Hutch ----------------
            configMenu.AddPage(ModManifest, "slime_hutch", () => I18n.Get("config.section.slime-hutch"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-slime-ranching-balancing.name"),
                tooltip: () => I18n.Get("config.enable-slime-ranching-balancing.tooltip"),
                getValue: () => Config.EnableSlimeRanchingBalancing,
                setValue: value => Config.EnableSlimeRanchingBalancing = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.slime-hutch-max-balls.name"),
                tooltip: () => I18n.Get("config.slime-hutch-max-balls.tooltip"),
                getValue: () => Config.SlimeHutchMaxBalls,
                setValue: value => Config.SlimeHutchMaxBalls = value,
                min: 4,
                max: 12,
                interval: 1
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-slime-egg-press-multi-yield.name"),
                tooltip: () => I18n.Get("config.enable-slime-egg-press-multi-yield.tooltip"),
                getValue: () => Config.EnableSlimeEggPressMultiYield,
                setValue: value => Config.EnableSlimeEggPressMultiYield = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.slime-egg-press-double-chance.name"),
                tooltip: () => I18n.Get("config.slime-egg-press-double-chance.tooltip"),
                getValue: () => Config.SlimeEggPressDoubleChance,
                setValue: value => Config.SlimeEggPressDoubleChance = value,
                min: 0.05f,
                max: 1.0f,
                interval: 0.05f
            );
        }

        private void InvalidateAssetCaches()
        {
            Helper.GameContent.InvalidateCache("Data/FarmAnimals");
            Helper.GameContent.InvalidateCache("Data/Machines");
        }
    }
}
