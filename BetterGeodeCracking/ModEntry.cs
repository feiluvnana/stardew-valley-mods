using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BetterGeodeCracking
{
    public class ModEntry : Mod
    {
        public static ModConfig Config { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor;

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
                text: () => "Blacksmith Geode Cracking"
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Free Cracking",
                tooltip: () => "Makes geode, trove, coconut, and mystery box cracking completely free (0g) at Clint's shop.",
                getValue: () => Config.FreeCracking,
                setValue: value => Config.FreeCracking = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Custom Cracking Price",
                tooltip: () => "The gold cost per geode when Free Cracking is disabled.",
                getValue: () => Config.CrackingPrice,
                setValue: value => Config.CrackingPrice = value,
                min: 0,
                max: 100,
                interval: 5
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Instant Cracking",
                tooltip: () => "Skips the 2.7-second delay to crack geodes instantaneously.",
                getValue: () => Config.InstantCracking,
                setValue: value => Config.InstantCracking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Show 'Crack All' Button",
                tooltip: () => "Displays a dedicated 'Crack All' button in Clint's geode menu.",
                getValue: () => Config.ShowCrackAllButton,
                setValue: value => Config.ShowCrackAllButton = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Bulk Batch Size",
                tooltip: () => "Maximum number of geodes to crack in a single batch (999 = full stack).",
                getValue: () => Config.BulkBatchSize,
                setValue: value => Config.BulkBatchSize = value,
                min: 1,
                max: 999,
                interval: 10
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Show Summary Toast",
                tooltip: () => "Displays a HUD notification summarizing cracked geodes.",
                getValue: () => Config.ShowSummaryToast,
                setValue: value => Config.ShowSummaryToast = value
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Farm Machine Options"
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Instant Geode Crusher",
                tooltip: () => "Makes Geode Crusher machines on the farm process geodes immediately.",
                getValue: () => Config.InstantGeodeCrusher,
                setValue: value => Config.InstantGeodeCrusher = value
            );
        }
    }
}
