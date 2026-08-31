using Common;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BetterFishing
{
    /// <summary>
    /// The main SMAPI mod entry point for BetterFishing.
    /// Manages configuration, Harmony patches, asset balancing, and GMCM menu integration.
    /// </summary>
    public sealed class ModEntry : Mod
    {
        public static ModConfig Config { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static IModHelper ModHelper { get; private set; } = null!;
        public static ITranslationHelper I18n { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor;
            ModHelper = helper;
            I18n = helper.Translation;

            // Apply Harmony transpiler and helper patches
            var harmony = new Harmony(ModManifest.UniqueID);
            FishingChestPatches.Apply(harmony);
            FishingExpPatches.Apply(harmony);
            CrabPotPatches.Apply(harmony);
            FishPondPatches.Apply(harmony);

            // Asset hooks for fish price balancing
            helper.Events.Content.AssetRequested += FishPriceBalancer.OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    // Invalidate Data/Objects so new price configurations apply immediately
                    Helper.GameContent.InvalidateCache("Data/Objects");
                }
            );

            // Mod description on Root Page
            configMenu.AddParagraph(ModManifest, () => I18n.Get("mod.description"));

            // Sub-page Navigation Links on Root Page
            AddPageLink(configMenu, "price-scaling", "price-scaling");
            AddPageLink(configMenu, "movement-bonuses", "movement-bonuses");
            AddPageLink(configMenu, "condition-bonuses", "condition-bonuses");
            AddPageLink(configMenu, "legendary-bonuses", "legendary-bonuses");
            AddPageLink(configMenu, "fishing-chests", "fishing-chests");
            AddPageLink(configMenu, "fishing-exp", "fishing-exp");
            AddPageLink(configMenu, "crab-pots", "crab-pots");
            AddPageLink(configMenu, "fish-ponds", "fish-ponds");

            // Sub-Page 1: Fish Price Scaling
            AddPage(configMenu, "price-scaling", "price-scaling");
            AddBool(configMenu, "enable-price-balancing", () => Config.EnableFishPriceBalancing, v => Config.EnableFishPriceBalancing = v);
            AddBool(configMenu, "prevent-nerf", () => Config.PreventNerf, v => Config.PreventNerf = v);
            AddFloat(configMenu, "base-floor", () => Config.BaseFloor, v => Config.BaseFloor = v, 0f, 100f, 1f);
            AddFloat(configMenu, "linear-factor", () => Config.LinearFactor, v => Config.LinearFactor = v, 0f, 5f, 0.05f);
            AddFloat(configMenu, "mid-tier-factor", () => Config.MidTierFactor, v => Config.MidTierFactor = v, 0f, 100f, 1f);
            AddFloat(configMenu, "apex-factor", () => Config.ApexFactor, v => Config.ApexFactor = v, 0f, 20f, 0.01f);
            AddFloat(configMenu, "apex-exponent", () => Config.ApexExponent, v => Config.ApexExponent = v, 1f, 6f, 0.05f);
            AddInt(configMenu, "price-rounding-interval", () => Config.PriceRoundingInterval, v => Config.PriceRoundingInterval = v, 1, 50);

            // Sub-Page 2: Movement Behavior Bonuses
            AddPage(configMenu, "movement-bonuses", "movement-bonuses");
            AddFloat(configMenu, "smooth-movement-bonus", () => Config.SmoothMovementBonus, v => Config.SmoothMovementBonus = v, 0f, 0.20f, 0.005f);
            AddFloat(configMenu, "mixed-movement-bonus", () => Config.MixedMovementBonus, v => Config.MixedMovementBonus = v, 0f, 0.20f, 0.005f);
            AddFloat(configMenu, "floater-movement-bonus", () => Config.FloaterMovementBonus, v => Config.FloaterMovementBonus = v, 0f, 0.20f, 0.005f);
            AddFloat(configMenu, "sinker-movement-bonus", () => Config.SinkerMovementBonus, v => Config.SinkerMovementBonus = v, 0f, 0.20f, 0.005f);
            AddFloat(configMenu, "dart-movement-bonus", () => Config.DartMovementBonus, v => Config.DartMovementBonus = v, 0f, 0.20f, 0.005f);

            // Sub-Page 3: Environmental & Location Traits
            AddPage(configMenu, "condition-bonuses", "condition-bonuses");
            AddFloat(configMenu, "rain-condition-bonus", () => Config.RainConditionBonus, v => Config.RainConditionBonus = v, 0f, 0.10f, 0.005f);
            AddFloat(configMenu, "night-window-condition-bonus", () => Config.NightWindowConditionBonus, v => Config.NightWindowConditionBonus = v, 0f, 0.10f, 0.005f);
            AddFloat(configMenu, "single-season-condition-bonus", () => Config.SingleSeasonConditionBonus, v => Config.SingleSeasonConditionBonus = v, 0f, 0.10f, 0.005f);
            AddFloat(configMenu, "isolated-location-bonus", () => Config.IsolatedLocationBonus, v => Config.IsolatedLocationBonus = v, 0f, 0.10f, 0.005f);

            // Sub-Page 4: Legendary & Signature Bonuses
            AddPage(configMenu, "legendary-bonuses", "legendary-bonuses");
            AddFloat(configMenu, "legendary-multiplier-bonus", () => Config.LegendaryFishMultiplierBonus, v => Config.LegendaryFishMultiplierBonus = v, 0f, 3.00f, 0.05f);
            AddBool(configMenu, "enable-predictable-hash-bonus", () => Config.EnablePredictableHashBonus, v => Config.EnablePredictableHashBonus = v);

            // Sub-Page 5: Fishing Treasure Chest Settings
            AddPage(configMenu, "fishing-chests", "fishing-chests");
            AddBool(configMenu, "enable-fishing-chest-buff", () => Config.EnableFishingChestBuff, v => Config.EnableFishingChestBuff = v);
            AddFloat(configMenu, "fishing-chest-decay-rate", () => Config.FishingChestDecayRate, v => Config.FishingChestDecayRate = v, 0.10f, 0.99f, 0.01f);
            AddFloat(configMenu, "golden-chest-decay-rate", () => Config.GoldenChestDecayRate, v => Config.GoldenChestDecayRate = v, 0.10f, 0.99f, 0.01f);

            // Sub-Page 6: Fishing Experience Settings
            AddPage(configMenu, "fishing-exp", "fishing-exp");
            AddBool(configMenu, "enable-fishing-exp-balancing", () => Config.EnableFishingExpBalancing, v => Config.EnableFishingExpBalancing = v);
            AddInt(configMenu, "apex-fish-exp-bonus", () => Config.ApexFishExpBonus, v => Config.ApexFishExpBonus = v, 0, 100);
            AddInt(configMenu, "legendary-fish-exp-bonus", () => Config.LegendaryFishExpBonus, v => Config.LegendaryFishExpBonus = v, 0, 300);

            // Sub-Page 7: Crab Pot Settings
            AddPage(configMenu, "crab-pots", "crab-pots");
            AddBool(configMenu, "enable-crab-pot-price-balancing", () => Config.EnableCrabPotPriceBalancing, v => Config.EnableCrabPotPriceBalancing = v);
            AddBool(configMenu, "enable-crab-pot-exp-balancing", () => Config.EnableCrabPotExpBalancing, v => Config.EnableCrabPotExpBalancing = v);
            AddBool(configMenu, "enable-crab-pot-trash-reduction", () => Config.EnableCrabPotTrashReduction, v => Config.EnableCrabPotTrashReduction = v);
            AddFloat(configMenu, "crab-pot-trash-reroll-chance", () => Config.CrabPotTrashRerollChance, v => Config.CrabPotTrashRerollChance = v, 0f, 1f, 0.05f);

            // Sub-Page 8: Fish Ponds & Aquaculture Settings
            AddPage(configMenu, "fish-ponds", "fish-ponds");
            AddBool(configMenu, "enable-fish-pond-quality", () => Config.EnableFishPondQuality, v => Config.EnableFishPondQuality = v);
            AddBool(configMenu, "enable-caviar-rebalance", () => Config.EnableCaviarRebalance, v => Config.EnableCaviarRebalance = v);
            AddInt(configMenu, "caviar-base-price", () => Config.CaviarBasePrice, v => Config.CaviarBasePrice = v, 100, 3000, 25);
        }

        private void AddPageLink(IGenericModConfigMenuApi menu, string pageId, string sectionKey)
        {
            menu.AddPageLink(ModManifest, pageId, () => I18n.Get($"config.section.{sectionKey}"));
        }

        private void AddPage(IGenericModConfigMenuApi menu, string pageId, string sectionKey)
        {
            menu.AddPage(ModManifest, pageId, () => I18n.Get($"config.section.{sectionKey}"));
        }

        private void AddSection(IGenericModConfigMenuApi menu, string sectionKey)
        {
            menu.AddSectionTitle(ModManifest, () => I18n.Get($"config.section.{sectionKey}"));
        }

        private void AddBool(IGenericModConfigMenuApi menu, string optionKey, Func<bool> getter, Action<bool> setter)
        {
            menu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get($"config.{optionKey}.name"),
                tooltip: () => I18n.Get($"config.{optionKey}.tooltip"),
                getValue: getter,
                setValue: setter
            );
        }

        private void AddInt(IGenericModConfigMenuApi menu, string optionKey, Func<int> getter, Action<int> setter, int min = 0, int max = 100, int interval = 1)
        {
            menu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get($"config.{optionKey}.name"),
                tooltip: () => I18n.Get($"config.{optionKey}.tooltip"),
                getValue: getter,
                setValue: setter,
                min: min,
                max: max,
                interval: interval
            );
        }

        private void AddFloat(IGenericModConfigMenuApi menu, string optionKey, Func<float> getter, Action<float> setter, float min = 0f, float max = 1f, float interval = 0.01f)
        {
            menu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get($"config.{optionKey}.name"),
                tooltip: () => I18n.Get($"config.{optionKey}.tooltip"),
                getValue: getter,
                setValue: setter,
                min: min,
                max: max,
                interval: interval
            );
        }
    }
}
