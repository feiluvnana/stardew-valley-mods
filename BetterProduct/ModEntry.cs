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

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor;

            CookingBalancer.Initialize(Config, Monitor);
            ArtisanBalancer.Initialize(Config, Monitor);
            MeadPatches.Initialize(Config, Monitor);

            var harmony = new Harmony(ModManifest.UniqueID);
            MeadPatches.Apply(harmony);

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
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Cooking Balance",
                tooltip: () => "Ensure cooking dishes always sell for at least the configured profit margin over raw ingredients.",
                getValue: () => Config.EnableCookingBalancing,
                setValue: value => Config.EnableCookingBalancing = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Cooking Margin",
                tooltip: () => "Profit multiplier over raw ingredients (e.g. 1.25 = 125%).",
                getValue: () => Config.CookingProfitMargin,
                setValue: value => Config.CookingProfitMargin = value,
                min: 1.0f,
                max: 5.0f,
                interval: 0.05f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Energy Buff",
                tooltip: () => "Multiply energy/health restored by cooked dishes.",
                getValue: () => Config.EnableEnergyBuff,
                setValue: value => Config.EnableEnergyBuff = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Energy Multiplier",
                tooltip: () => "Energy/Health multiplier for cooked food.",
                getValue: () => Config.EnergyMultiplier,
                setValue: value => Config.EnergyMultiplier = value,
                min: 1.0f,
                max: 3.0f,
                interval: 0.05f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Buff Duration Boost",
                tooltip: () => "Boost the duration of food buffs.",
                getValue: () => Config.EnableBuffDurationBoost,
                setValue: value => Config.EnableBuffDurationBoost = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Buff Duration Multiplier",
                tooltip: () => "Multiplier for food buff durations.",
                getValue: () => Config.BuffDurationMultiplier,
                setValue: value => Config.BuffDurationMultiplier = value,
                min: 1.0f,
                max: 5.0f,
                interval: 0.1f
            );

            // Artisan Options
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Mead Flavor & Value Fix",
                tooltip: () => "Mead scales price based on the input honey flower type.",
                getValue: () => Config.EnableMeadFix,
                setValue: value => Config.EnableMeadFix = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Mead Multiplier",
                tooltip: () => "Multiplier applied to honey price when turned into mead.",
                getValue: () => Config.MeadMultiplier,
                setValue: value => Config.MeadMultiplier = value,
                min: 1.0f,
                max: 5.0f,
                interval: 0.1f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Juice Buff",
                tooltip: () => "Buff juice price multiplier.",
                getValue: () => Config.EnableJuiceBuff,
                setValue: value => Config.EnableJuiceBuff = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Juice Multiplier",
                tooltip: () => "Base vegetable multiplier for Juice.",
                getValue: () => Config.JuiceMultiplier,
                setValue: value => Config.JuiceMultiplier = value,
                min: 2.25f,
                max: 6.0f,
                interval: 0.25f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Pickle Buff",
                tooltip: () => "Buff pickle price multiplier.",
                getValue: () => Config.EnablePickleBuff,
                setValue: value => Config.EnablePickleBuff = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Pickle Multiplier",
                tooltip: () => "Base multiplier for Pickles.",
                getValue: () => Config.PickleMultiplier,
                setValue: value => Config.PickleMultiplier = value,
                min: 2.0f,
                max: 6.0f,
                interval: 0.25f
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Aged Roe Buff",
                tooltip: () => "Buff aged roe price multiplier.",
                getValue: () => Config.EnableRoeBuff,
                setValue: value => Config.EnableRoeBuff = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Aged Roe Multiplier",
                tooltip: () => "Base multiplier for Aged Roe.",
                getValue: () => Config.AgedRoeMultiplier,
                setValue: value => Config.AgedRoeMultiplier = value,
                min: 2.0f,
                max: 6.0f,
                interval: 0.25f
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Caviar Price",
                tooltip: () => "Base selling price for Caviar.",
                getValue: () => Config.CaviarPrice,
                setValue: value => Config.CaviarPrice = value,
                min: 500,
                max: 3000,
                interval: 50
            );
        }
    }
}