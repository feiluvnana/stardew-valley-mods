using System;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace ExtendedStackable
{
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }

    public class ModEntry : Mod
    {
        public static ModConfig Config { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor;

            StackablePatches.Apply(Helper.ModRegistry, ModManifest.UniqueID, Monitor, Config);

            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Max Stack Size",
                tooltip: () => "The maximum stack size for stackable items.",
                getValue: () => Config.MaxStackSize,
                setValue: value => Config.MaxStackSize = value,
                min: 1,
                max: 9999
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Tackle Stacking",
                tooltip: () => "Allow fishing tackles/bobbers with matching durability to stack.",
                getValue: () => Config.EnableTackleStacking,
                setValue: value => Config.EnableTackleStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Trinket Stacking",
                tooltip: () => "Allow identical trinkets to stack.",
                getValue: () => Config.EnableTrinketStacking,
                setValue: value => Config.EnableTrinketStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Furniture Stacking",
                tooltip: () => "Allow furniture and decorations to stack.",
                getValue: () => Config.EnableFurnitureStacking,
                setValue: value => Config.EnableFurnitureStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Ring Stacking",
                tooltip: () => "Allow identical rings to stack.",
                getValue: () => Config.EnableRingStacking,
                setValue: value => Config.EnableRingStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Clothing & Hat Stacking",
                tooltip: () => "Allow matching clothing and hats to stack.",
                getValue: () => Config.EnableClothingAndHatStacking,
                setValue: value => Config.EnableClothingAndHatStacking = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Boots Stacking",
                tooltip: () => "Allow identical boots to stack.",
                getValue: () => Config.EnableBootsStacking,
                setValue: value => Config.EnableBootsStacking = value
            );
        }
    }
}