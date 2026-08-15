using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace ExtendedStackable
{
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }

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

            StackablePatches.Apply(Helper.ModRegistry, ModManifest.UniqueID, Monitor, Config);

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
                text: () => I18n.Get("config.section.stackable")
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.max-stack-size.name"),
                tooltip: () => I18n.Get("config.max-stack-size.tooltip"),
                getValue: () => Config.MaxStackSize,
                setValue: value => Config.MaxStackSize = value,
                min: 1,
                max: 9999
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-tackle-stacking.name"),
                tooltip: () => I18n.Get("config.enable-tackle-stacking.tooltip"),
                getValue: () => Config.EnableTackleStacking,
                setValue: value => Config.EnableTackleStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-trinket-stacking.name"),
                tooltip: () => I18n.Get("config.enable-trinket-stacking.tooltip"),
                getValue: () => Config.EnableTrinketStacking,
                setValue: value => Config.EnableTrinketStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-furniture-stacking.name"),
                tooltip: () => I18n.Get("config.enable-furniture-stacking.tooltip"),
                getValue: () => Config.EnableFurnitureStacking,
                setValue: value => Config.EnableFurnitureStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-ring-stacking.name"),
                tooltip: () => I18n.Get("config.enable-ring-stacking.tooltip"),
                getValue: () => Config.EnableRingStacking,
                setValue: value => Config.EnableRingStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-clothing-and-hat-stacking.name"),
                tooltip: () => I18n.Get("config.enable-clothing-and-hat-stacking.tooltip"),
                getValue: () => Config.EnableClothingAndHatStacking,
                setValue: value => Config.EnableClothingAndHatStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-boots-stacking.name"),
                tooltip: () => I18n.Get("config.enable-boots-stacking.tooltip"),
                getValue: () => Config.EnableBootsStacking,
                setValue: value => Config.EnableBootsStacking = value
            );
        }
    }
}