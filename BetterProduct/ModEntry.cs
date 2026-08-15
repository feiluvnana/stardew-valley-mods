using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BetterProduct
{
    public class ModEntry : Mod
    {
        public static ModConfig Config { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static ITranslationHelper I18n { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor;
            I18n = helper.Translation;

            CookingBalancer.Initialize(Config, Monitor);
            ArtisanBalancer.Initialize(Config, Monitor);
            MeadPatches.Initialize(Config, Monitor);
            DehydratorBalancer.Initialize(Config, Monitor);

            var harmony = new Harmony(ModManifest.UniqueID);
            MeadPatches.Apply(harmony);
            DehydratorBalancer.Apply(harmony);

            helper.Events.Content.AssetRequested += CookingBalancer.OnAssetRequested;
            helper.Events.Content.AssetRequested += ArtisanBalancer.OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    Helper.GameContent.InvalidateCache("Data/Objects");
                    Helper.GameContent.InvalidateCache("Data/CookingRecipes");
                }
            );

            // Cooking Options
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
                name: () => I18n.Get("config.energy-buff.name"),
                tooltip: () => I18n.Get("config.energy-buff.tooltip"),
                getValue: () => Config.EnableEnergyBuff,
                setValue: value => Config.EnableEnergyBuff = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.energy-multiplier.name"),
                tooltip: () => I18n.Get("config.energy-multiplier.tooltip"),
                getValue: () => Config.EnergyMultiplier,
                setValue: value => Config.EnergyMultiplier = value,
                min: 1.0f,
                max: 3.0f,
                interval: 0.05f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.buff-duration-boost.name"),
                tooltip: () => I18n.Get("config.buff-duration-boost.tooltip"),
                getValue: () => Config.EnableBuffDurationBoost,
                setValue: value => Config.EnableBuffDurationBoost = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.buff-duration-multiplier.name"),
                tooltip: () => I18n.Get("config.buff-duration-multiplier.tooltip"),
                getValue: () => Config.BuffDurationMultiplier,
                setValue: value => Config.BuffDurationMultiplier = value,
                min: 1.0f,
                max: 5.0f,
                interval: 0.1f
            );

            // Artisan Options
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

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.mead-multiplier.name"),
                tooltip: () => I18n.Get("config.mead-multiplier.tooltip"),
                getValue: () => Config.MeadMultiplier,
                setValue: value => Config.MeadMultiplier = value,
                min: 1.0f,
                max: 5.0f,
                interval: 0.1f
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
                min: 2.25f,
                max: 6.0f,
                interval: 0.25f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.pickle-buff.name"),
                tooltip: () => I18n.Get("config.pickle-buff.tooltip"),
                getValue: () => Config.EnablePickleBuff,
                setValue: value => Config.EnablePickleBuff = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.pickle-multiplier.name"),
                tooltip: () => I18n.Get("config.pickle-multiplier.tooltip"),
                getValue: () => Config.PickleMultiplier,
                setValue: value => Config.PickleMultiplier = value,
                min: 2.0f,
                max: 6.0f,
                interval: 0.25f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roe-buff.name"),
                tooltip: () => I18n.Get("config.roe-buff.tooltip"),
                getValue: () => Config.EnableRoeBuff,
                setValue: value => Config.EnableRoeBuff = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.aged-roe-multiplier.name"),
                tooltip: () => I18n.Get("config.aged-roe-multiplier.tooltip"),
                getValue: () => Config.AgedRoeMultiplier,
                setValue: value => Config.AgedRoeMultiplier = value,
                min: 2.0f,
                max: 6.0f,
                interval: 0.25f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.caviar-price.name"),
                tooltip: () => I18n.Get("config.caviar-price.tooltip"),
                getValue: () => Config.CaviarPrice,
                setValue: value => Config.CaviarPrice = value,
                min: 500,
                max: 3000,
                interval: 50
            );

            // Dehydrator Options
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.dehydrator")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.dehydrator-balancing.name"),
                tooltip: () => I18n.Get("config.dehydrator-balancing.tooltip"),
                getValue: () => Config.EnableDehydratorBalancing,
                setValue: value => Config.EnableDehydratorBalancing = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.dehydrator-mixed-quality.name"),
                tooltip: () => I18n.Get("config.dehydrator-mixed-quality.tooltip"),
                getValue: () => Config.AllowMixedQualityDehydrating,
                setValue: value => Config.AllowMixedQualityDehydrating = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.dehydrator-quality-scaling.name"),
                tooltip: () => I18n.Get("config.dehydrator-quality-scaling.tooltip"),
                getValue: () => Config.EnableDriedQualityScaling,
                setValue: value => Config.EnableDriedQualityScaling = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.dehydrator-speed-multiplier.name"),
                tooltip: () => I18n.Get("config.dehydrator-speed-multiplier.tooltip"),
                getValue: () => Config.DehydratorSpeedMultiplier,
                setValue: value => Config.DehydratorSpeedMultiplier = value,
                min: 0.0f,
                max: 10.0f,
                interval: 0.5f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.dehydrator-allow-vegetables.name"),
                tooltip: () => I18n.Get("config.dehydrator-allow-vegetables.tooltip"),
                getValue: () => Config.AllowVegetableDehydrating,
                setValue: value => Config.AllowVegetableDehydrating = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.dehydrator-allow-flowers.name"),
                tooltip: () => I18n.Get("config.dehydrator-allow-flowers.tooltip"),
                getValue: () => Config.AllowFlowerDehydrating,
                setValue: value => Config.AllowFlowerDehydrating = value
            );
        }
    }
}