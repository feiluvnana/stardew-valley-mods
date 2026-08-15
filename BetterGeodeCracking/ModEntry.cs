using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BetterGeodeCracking
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

            GeodePatches.Apply(ModManifest.UniqueID, Monitor, Config);

            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
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

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.blacksmith")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.free-cracking.name"),
                tooltip: () => I18n.Get("config.free-cracking.tooltip"),
                getValue: () => Config.FreeCracking,
                setValue: value => Config.FreeCracking = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.cracking-price.name"),
                tooltip: () => I18n.Get("config.cracking-price.tooltip"),
                getValue: () => Config.CrackingPrice,
                setValue: value => Config.CrackingPrice = value,
                min: 0,
                max: 100,
                interval: 5
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

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.farm-machines")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.instant-geode-crusher.name"),
                tooltip: () => I18n.Get("config.instant-geode-crusher.tooltip"),
                getValue: () => Config.InstantGeodeCrusher,
                setValue: value => Config.InstantGeodeCrusher = value
            );
        }
    }
}
