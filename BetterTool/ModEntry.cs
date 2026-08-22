using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace BetterTool
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

            var harmony = new Harmony(ModManifest.UniqueID);
            HopperPatches.Apply(harmony, Monitor);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Player.Warped += OnWarped;
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
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            // Section: Automation Features (Non-Vanilla Enhancements)
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

            // Section: Specific Machine Enhancements
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

            // Section: Hopper Customization
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
    }
}
