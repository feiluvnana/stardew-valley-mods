using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace BetterIndustry
{
    public class ModEntry : Mod
    {
        public static ModConfig Config { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static ITranslationHelper I18n { get; private set; } = null!;
        public static IModHelper ModHelper { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor;
            I18n = helper.Translation;
            ModHelper = helper;

            // Asset Requested Events (Artisan & Cooking)
            helper.Events.Content.AssetRequested += CookingBalancer.OnAssetRequested;
            helper.Events.Content.AssetRequested += ArtisanBalancer.OnAssetRequested;

            // Game Loop Events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += FruitTreeDropper.OnDayStarted;

            Monitor.Log("BetterIndustry loaded successfully: Artisan Goods and Cooking Balance are active.", LogLevel.Debug);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
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

            // ---------------- Section 1: Cooking Balance ----------------
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
                name: () => I18n.Get("config.quality-preserving.name"),
                tooltip: () => I18n.Get("config.quality-preserving.tooltip"),
                getValue: () => Config.EnableQualityPreserving,
                setValue: value => Config.EnableQualityPreserving = value
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

        private void InvalidateAssetCaches()
        {
            Helper.GameContent.InvalidateCache("Data/Objects");
            Helper.GameContent.InvalidateCache("Data/Machines");
            Helper.GameContent.InvalidateCache("Data/CookingRecipes");
        }
    }
}
