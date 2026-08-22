using System;
using HarmonyLib;
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

            // Initialize Harmony Patches for Hopper Automation
            var harmony = new Harmony(ModManifest.UniqueID);
            HopperPatches.Apply(harmony, Monitor);

            // Asset Requested Events (Artisan & Cooking)
            helper.Events.Content.AssetRequested += CookingBalancer.OnAssetRequested;
            helper.Events.Content.AssetRequested += ArtisanBalancer.OnAssetRequested;

            // Game Loop Events (Hopper Automation)
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Player.Warped += OnWarped;

            Monitor.Log("BetterIndustry loaded successfully: Artisan Goods, Cooking, and Omni-Hopper Automation are active.", LogLevel.Debug);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Config.EnablePeriodicProcessing || !Context.IsWorldReady || Game1.currentLocation == null)
                return;

            uint interval = (uint)Math.Max(10, Config.ProcessIntervalTicks);
            if (e.IsMultipleOf(interval))
            {
                HopperManager.ProcessLocation(Game1.currentLocation, Game1.player);
            }
        }

        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {
            if (!Config.EnablePeriodicProcessing || !Context.IsWorldReady)
                return;

            foreach (var location in Game1.locations)
            {
                HopperManager.ProcessLocation(location, Game1.player);

                if (location.buildings.Count > 0)
                {
                    foreach (var building in location.buildings)
                    {
                        if (building.indoors.Value != null)
                        {
                            HopperManager.ProcessLocation(building.indoors.Value, Game1.player);
                        }
                    }
                }
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!Config.EnablePeriodicProcessing || !Context.IsWorldReady)
                return;

            foreach (var location in Game1.locations)
            {
                HopperManager.ProcessLocation(location, Game1.player);

                if (location.buildings.Count > 0)
                {
                    foreach (var building in location.buildings)
                    {
                        if (building.indoors.Value != null)
                        {
                            HopperManager.ProcessLocation(building.indoors.Value, Game1.player);
                        }
                    }
                }
            }
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!Config.EnablePeriodicProcessing || !Context.IsWorldReady || e.NewLocation == null)
                return;

            HopperManager.ProcessLocation(e.NewLocation, Game1.player);
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

            // ---------------- Section 3: Hopper Automation ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.automation.name")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.auto-harvest.name"),
                tooltip: () => I18n.Get("config.auto-harvest.tooltip"),
                getValue: () => Config.EnableAutoHarvest,
                setValue: value => Config.EnableAutoHarvest = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.chest-output-transfer.name"),
                tooltip: () => I18n.Get("config.chest-output-transfer.tooltip"),
                getValue: () => Config.EnableChestOutputTransfer,
                setValue: value => Config.EnableChestOutputTransfer = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.periodic-processing.name"),
                tooltip: () => I18n.Get("config.periodic-processing.tooltip"),
                getValue: () => Config.EnablePeriodicProcessing,
                setValue: value => Config.EnablePeriodicProcessing = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.process-interval.name"),
                tooltip: () => I18n.Get("config.process-interval.tooltip"),
                getValue: () => Config.ProcessIntervalTicks,
                setValue: value => Config.ProcessIntervalTicks = value,
                min: 10,
                max: 600,
                interval: 10
            );

            // ---------------- Section 4: Specific Machine Handling ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.machines.name")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.service-crab-pots.name"),
                tooltip: () => I18n.Get("config.service-crab-pots.tooltip"),
                getValue: () => Config.EnableCrabPotService,
                setValue: value => Config.EnableCrabPotService = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.service-casks.name"),
                tooltip: () => I18n.Get("config.service-casks.tooltip"),
                getValue: () => Config.EnableCaskService,
                setValue: value => Config.EnableCaskService = value
            );

            // ---------------- Section 5: Hopper Customization ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.customization.name")
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.hopper-capacity.name"),
                tooltip: () => I18n.Get("config.hopper-capacity.tooltip"),
                getValue: () => Config.HopperCapacity,
                setValue: value => Config.HopperCapacity = value,
                min: 36,
                max: 70,
                interval: 34
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.sound-effects.name"),
                tooltip: () => I18n.Get("config.sound-effects.tooltip"),
                getValue: () => Config.PlaySoundEffects,
                setValue: value => Config.PlaySoundEffects = value
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
