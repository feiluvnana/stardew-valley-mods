using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.BigCraftables;

namespace BetterForge
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

            TrinketPatches.Initialize(Config, Monitor);
            EnchantmentPatches.Initialize(Config, Monitor);

            var harmony = new Harmony(ModManifest.UniqueID);
            TrinketPatches.Apply(harmony);
            EnchantmentPatches.Apply(harmony);

            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            Helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (e.IsMultipleOf(30) && Context.IsWorldReady && Game1.player != null)
            {
                TrinketAscensionLogic.UpdateAscensionLuckBuff(Game1.player);
            }
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, BigCraftableData>().Data;
                    string desc = I18n.Get("anvil.description");

                    if (data.TryGetValue("Anvil", out var anvilData))
                    {
                        anvilData.Description = desc;
                    }
                    if (data.TryGetValue("289", out var anvilData289))
                    {
                        anvilData289.Description = desc;
                    }
                });
            }
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
                    TrinketPatches.Config = Config;
                    EnchantmentPatches.Config = Config;
                },
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    TrinketPatches.Config = Config;
                    EnchantmentPatches.Config = Config;
                }
            );

            // Section 1: Weapon & Tool Enchanting Options
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.enchanting")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.uniform-chances.name"),
                tooltip: () => I18n.Get("config.uniform-chances.tooltip"),
                getValue: () => Config.UniformEnchantmentChances,
                setValue: value => Config.UniformEnchantmentChances = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.randomize-seed.name"),
                tooltip: () => I18n.Get("config.randomize-seed.tooltip"),
                getValue: () => Config.RandomizeEnchantmentSeed,
                setValue: value => Config.RandomizeEnchantmentSeed = value
            );

            // Section 2: Trinket Reforging & Anvil Options
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
                name: () => I18n.Get("config.show-messages.name"),
                tooltip: () => I18n.Get("config.show-messages.tooltip"),
                getValue: () => Config.ShowReforgeSuccessMessage,
                setValue: value => Config.ShowReforgeSuccessMessage = value
            );
        }
    }
}
