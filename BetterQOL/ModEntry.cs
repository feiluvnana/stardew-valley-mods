using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Machines;

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
            MenuTooltipPatch.Apply(harmony, Monitor);

            GeodeMenuHandler.Initialize(helper, Monitor);
            HoverInfoOverlay.Initialize(helper, Monitor);

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Monitor.Log("BetterQOL initialized successfully: Extended Stackable limits, Geode Cracking overhaul, Hover Information, and Lookup Anything are active.", LogLevel.Debug);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            bool isLookupTriggered = (Config.LookupKey != SButton.None && e.Button == Config.LookupKey)
                                  || (Config.ControllerLookupKey != SButton.None && e.Button == Config.ControllerLookupKey);

            if (!Config.EnableLookupAnything || !isLookupTriggered)
                return;

            if (Game1.activeClickableMenu is LookupMenu)
            {
                Game1.exitActiveMenu();
                Helper.Input.Suppress(e.Button);
                return;
            }

            var subject = LookupTargetFinder.FindTargetSubject();
            if (subject != null)
            {
                Game1.activeClickableMenu = new LookupMenu(subject);
                Helper.Input.Suppress(e.Button);
            }
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
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

            // ---------------- Section 4: Hover Information & Timers (UI Info Suite 2 Style) ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.hover-info")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-crop-hover.name"),
                tooltip: () => I18n.Get("config.enable-crop-hover.tooltip"),
                getValue: () => Config.EnableCropHover,
                setValue: value => Config.EnableCropHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-machine-hover.name"),
                tooltip: () => I18n.Get("config.enable-machine-hover.tooltip"),
                getValue: () => Config.EnableMachineHover,
                setValue: value => Config.EnableMachineHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-tree-hover.name"),
                tooltip: () => I18n.Get("config.enable-tree-hover.tooltip"),
                getValue: () => Config.EnableTreeHover,
                setValue: value => Config.EnableTreeHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-animal-hover.name"),
                tooltip: () => I18n.Get("config.enable-animal-hover.tooltip"),
                getValue: () => Config.EnableAnimalHover,
                setValue: value => Config.EnableAnimalHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-water-and-fertilizer.name"),
                tooltip: () => I18n.Get("config.show-water-and-fertilizer.tooltip"),
                getValue: () => Config.ShowWaterAndFertilizer,
                setValue: value => Config.ShowWaterAndFertilizer = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-item-icon-in-tooltip.name"),
                tooltip: () => I18n.Get("config.show-item-icon-in-tooltip.tooltip"),
                getValue: () => Config.ShowItemIconInTooltip,
                setValue: value => Config.ShowItemIconInTooltip = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-exact-finish-time.name"),
                tooltip: () => I18n.Get("config.show-exact-finish-time.tooltip"),
                getValue: () => Config.ShowExactFinishTime,
                setValue: value => Config.ShowExactFinishTime = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-item-sell-price-on-hover.name"),
                tooltip: () => I18n.Get("config.show-item-sell-price-on-hover.tooltip"),
                getValue: () => Config.ShowItemSellPriceOnHover,
                setValue: value => Config.ShowItemSellPriceOnHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-bundle-need-on-hover.name"),
                tooltip: () => I18n.Get("config.show-bundle-need-on-hover.tooltip"),
                getValue: () => Config.ShowBundleNeedOnHover,
                setValue: value => Config.ShowBundleNeedOnHover = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-museum-need-on-hover.name"),
                tooltip: () => I18n.Get("config.show-museum-need-on-hover.tooltip"),
                getValue: () => Config.ShowMuseumNeedOnHover,
                setValue: value => Config.ShowMuseumNeedOnHover = value
            );

            configMenu.AddKeybind(
                mod: ModManifest,
                name: () => I18n.Get("config.hover-hotkey.name"),
                tooltip: () => I18n.Get("config.hover-hotkey.tooltip"),
                getValue: () => Config.HoverHotkey,
                setValue: value => Config.HoverHotkey = value
            );

            // ---------------- Section 5: Lookup Anything ----------------
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.lookup-anything")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-lookup-anything.name"),
                tooltip: () => I18n.Get("config.enable-lookup-anything.tooltip"),
                getValue: () => Config.EnableLookupAnything,
                setValue: value => Config.EnableLookupAnything = value
            );

            configMenu.AddKeybind(
                mod: ModManifest,
                name: () => I18n.Get("config.lookup-key.name"),
                tooltip: () => I18n.Get("config.lookup-key.tooltip"),
                getValue: () => Config.LookupKey,
                setValue: value => Config.LookupKey = value
            );

            configMenu.AddKeybind(
                mod: ModManifest,
                name: () => I18n.Get("config.controller-lookup-key.name"),
                tooltip: () => I18n.Get("config.controller-lookup-key.tooltip"),
                getValue: () => Config.ControllerLookupKey,
                setValue: value => Config.ControllerLookupKey = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-gift-tastes.name"),
                tooltip: () => I18n.Get("config.show-gift-tastes.tooltip"),
                getValue: () => Config.ShowGiftTastes,
                setValue: value => Config.ShowGiftTastes = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-item-recipes.name"),
                tooltip: () => I18n.Get("config.show-item-recipes.tooltip"),
                getValue: () => Config.ShowItemRecipes,
                setValue: value => Config.ShowItemRecipes = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-bundle-and-museum-info.name"),
                tooltip: () => I18n.Get("config.show-bundle-and-museum-info.tooltip"),
                getValue: () => Config.ShowBundleAndMuseumInfo,
                setValue: value => Config.ShowBundleAndMuseumInfo = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-community-center-progress.name"),
                tooltip: () => I18n.Get("config.show-community-center-progress.tooltip"),
                getValue: () => Config.ShowCommunityCenterProgress,
                setValue: value => Config.ShowCommunityCenterProgress = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-friendship-overview.name"),
                tooltip: () => I18n.Get("config.show-friendship-overview.tooltip"),
                getValue: () => Config.ShowFriendshipOverview,
                setValue: value => Config.ShowFriendshipOverview = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-progress-and-perfection.name"),
                tooltip: () => I18n.Get("config.show-progress-and-perfection.tooltip"),
                getValue: () => Config.ShowProgressAndPerfection,
                setValue: value => Config.ShowProgressAndPerfection = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-mine-and-guild-progress.name"),
                tooltip: () => I18n.Get("config.show-mine-and-guild-progress.tooltip"),
                getValue: () => Config.ShowMineAndGuildProgress,
                setValue: value => Config.ShowMineAndGuildProgress = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.show-museum-progress.name"),
                tooltip: () => I18n.Get("config.show-museum-progress.tooltip"),
                getValue: () => Config.ShowMuseumProgress,
                setValue: value => Config.ShowMuseumProgress = value
            );
        }

        private void InvalidateAssetCaches()
        {
            Helper.GameContent.InvalidateCache("Data/Machines");
        }
    }
}
