using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BetterTrinket
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

            TrinketPatches.Initialize(Config, Monitor);

            var harmony = new Harmony(ModManifest.UniqueID);
            TrinketPatches.Apply(harmony);

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

            // Reforging Options
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

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.pity-system.name"),
                tooltip: () => I18n.Get("config.pity-system.tooltip"),
                getValue: () => Config.EnablePitySystem,
                setValue: value => Config.EnablePitySystem = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.pity-rolls.name"),
                tooltip: () => I18n.Get("config.pity-rolls.tooltip"),
                getValue: () => Config.RollsForGuaranteedUpgrade,
                setValue: value => Config.RollsForGuaranteedUpgrade = value,
                min: 1,
                max: 10,
                interval: 1
            );

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
                name: () => I18n.Get("config.show-tooltips.name"),
                tooltip: () => I18n.Get("config.show-tooltips.tooltip"),
                getValue: () => Config.ShowStatRangesInTooltips,
                setValue: value => Config.ShowStatRangesInTooltips = value
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
