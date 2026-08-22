using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;

namespace BetterQOL
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
            StackablePatches.Apply(harmony, Monitor);
            GeodeCrusherPatches.Apply(harmony);

            GeodeMenuHandler.Initialize(helper, Monitor);

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Monitor.Log("BetterQOL initialized successfully: Extended Stackable limits and Geode Cracking overhaul are active.", LogLevel.Debug);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    if (Config.AllowSpecialGeodesInCrusher)
                    {
                        var data = asset.AsDictionary<string, ObjectData>().Data;
                        foreach (var obj in data.Values)
                        {
                            if (obj.ContextTags != null && obj.ContextTags.Contains("geode_crusher_ignored"))
                            {
                                obj.ContextTags.Remove("geode_crusher_ignored");
                            }
                        }
                    }
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, MachineData>().Data;
                    if (data.TryGetValue("(BC)182", out var geodeCrusher))
                    {
                        if (Config.InstantGeodeCrusher)
                        {
                            if (geodeCrusher.OutputRules != null)
                            {
                                foreach (var rule in geodeCrusher.OutputRules)
                                {
                                    rule.MinutesUntilReady = 0;
                                }
                            }
                        }

                        if (!Config.GeodeCrusherRequiresCoal)
                        {
                            geodeCrusher.AdditionalConsumedItems?.Clear();
                            geodeCrusher.InvalidCountMessage = null;
                            if (geodeCrusher.OutputRules != null)
                            {
                                foreach (var rule in geodeCrusher.OutputRules)
                                {
                                    rule.InvalidCountMessage = null;
                                }
                            }
                        }
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
                    Helper.WriteConfig(Config);
                    InvalidateAssetCaches();
                },
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    InvalidateAssetCaches();
                }
            );

            // ---------------- Section 1: Blacksmith Geode Cracking ----------------
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

            // ---------------- Section 2: Farm Machine Options ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.farm-machines")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.allow-special-geodes-in-crusher.name"),
                tooltip: () => I18n.Get("config.allow-special-geodes-in-crusher.tooltip"),
                getValue: () => Config.AllowSpecialGeodesInCrusher,
                setValue: value =>
                {
                    Config.AllowSpecialGeodesInCrusher = value;
                    InvalidateAssetCaches();
                }
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.instant-geode-crusher.name"),
                tooltip: () => I18n.Get("config.instant-geode-crusher.tooltip"),
                getValue: () => Config.InstantGeodeCrusher,
                setValue: value =>
                {
                    Config.InstantGeodeCrusher = value;
                    InvalidateAssetCaches();
                }
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.geode-crusher-requires-coal.name"),
                tooltip: () => I18n.Get("config.geode-crusher-requires-coal.tooltip"),
                getValue: () => Config.GeodeCrusherRequiresCoal,
                setValue: value =>
                {
                    Config.GeodeCrusherRequiresCoal = value;
                    InvalidateAssetCaches();
                }
            );

            // ---------------- Section 3: Item Stacking Options ----------------
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

        private void InvalidateAssetCaches()
        {
            Helper.GameContent.InvalidateCache("Data/Objects");
            Helper.GameContent.InvalidateCache("Data/Machines");
        }
    }
}
