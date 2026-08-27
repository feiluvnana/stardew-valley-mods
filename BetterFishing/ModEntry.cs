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

            // Apply Harmony transpiler patches for fishing treasure chest decay & balanced fishing exp
            var harmony = new Harmony(ModManifest.UniqueID);
            FishingChestPatches.Apply(harmony);
            FishingExpPatches.Apply(harmony);

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

            // Section 1: Fish Price Scaling
            AddSection(configMenu, "price-scaling");
            AddBool(configMenu, "enable-price-balancing", () => Config.EnableFishPriceBalancing, v => Config.EnableFishPriceBalancing = v);
            AddBool(configMenu, "prevent-nerf", () => Config.PreventNerf, v => Config.PreventNerf = v);
            AddFloat(configMenu, "base-floor", () => Config.BaseFloor, v => Config.BaseFloor = v, 0f, 100f, 1f);
            AddFloat(configMenu, "linear-factor", () => Config.LinearFactor, v => Config.LinearFactor = v, 0f, 5f, 0.05f);
            AddFloat(configMenu, "mid-tier-factor", () => Config.MidTierFactor, v => Config.MidTierFactor = v, 0f, 100f, 1f);
            AddFloat(configMenu, "apex-factor", () => Config.ApexFactor, v => Config.ApexFactor = v, 0f, 20f, 0.01f);
            AddFloat(configMenu, "apex-exponent", () => Config.ApexExponent, v => Config.ApexExponent = v, 1f, 6f, 0.05f);
            AddInt(configMenu, "price-rounding-interval", () => Config.PriceRoundingInterval, v => Config.PriceRoundingInterval = v, 1, 50);

            // Section 2: Movement Behavior Bonuses
            AddSection(configMenu, "movement-bonuses");
            AddFloat(configMenu, "smooth-movement-bonus", () => Config.SmoothMovementBonus, v => Config.SmoothMovementBonus = v, 0f, 0.20f, 0.005f);
            AddFloat(configMenu, "mixed-movement-bonus", () => Config.MixedMovementBonus, v => Config.MixedMovementBonus = v, 0f, 0.20f, 0.005f);
            AddFloat(configMenu, "floater-movement-bonus", () => Config.FloaterMovementBonus, v => Config.FloaterMovementBonus = v, 0f, 0.20f, 0.005f);
            AddFloat(configMenu, "sinker-movement-bonus", () => Config.SinkerMovementBonus, v => Config.SinkerMovementBonus = v, 0f, 0.20f, 0.005f);
            AddFloat(configMenu, "dart-movement-bonus", () => Config.DartMovementBonus, v => Config.DartMovementBonus = v, 0f, 0.20f, 0.005f);

            // Section 3: Environmental & Location Traits
            AddSection(configMenu, "condition-bonuses");
            AddFloat(configMenu, "rain-condition-bonus", () => Config.RainConditionBonus, v => Config.RainConditionBonus = v, 0f, 0.10f, 0.005f);
            AddFloat(configMenu, "night-window-condition-bonus", () => Config.NightWindowConditionBonus, v => Config.NightWindowConditionBonus = v, 0f, 0.10f, 0.005f);
            AddFloat(configMenu, "single-season-condition-bonus", () => Config.SingleSeasonConditionBonus, v => Config.SingleSeasonConditionBonus = v, 0f, 0.10f, 0.005f);
            AddFloat(configMenu, "isolated-location-bonus", () => Config.IsolatedLocationBonus, v => Config.IsolatedLocationBonus = v, 0f, 0.10f, 0.005f);

            // Section 4: Legendary & Signature Bonuses
            AddSection(configMenu, "legendary-bonuses");
            AddFloat(configMenu, "legendary-multiplier-bonus", () => Config.LegendaryFishMultiplierBonus, v => Config.LegendaryFishMultiplierBonus = v, 0f, 3.00f, 0.05f);
            AddBool(configMenu, "enable-predictable-hash-bonus", () => Config.EnablePredictableHashBonus, v => Config.EnablePredictableHashBonus = v);

            // Section 5: Fishing Treasure Chest Settings
            AddSection(configMenu, "fishing-chests");
            AddBool(configMenu, "enable-fishing-chest-buff", () => Config.EnableFishingChestBuff, v => Config.EnableFishingChestBuff = v);
            AddFloat(configMenu, "fishing-chest-decay-rate", () => Config.FishingChestDecayRate, v => Config.FishingChestDecayRate = v, 0.10f, 0.99f, 0.01f);
            AddFloat(configMenu, "golden-chest-decay-rate", () => Config.GoldenChestDecayRate, v => Config.GoldenChestDecayRate = v, 0.10f, 0.99f, 0.01f);

            // Section 6: Fishing Experience Settings
            AddSection(configMenu, "fishing-exp");
            AddBool(configMenu, "enable-fishing-exp-balancing", () => Config.EnableFishingExpBalancing, v => Config.EnableFishingExpBalancing = v);
            AddInt(configMenu, "apex-fish-exp-bonus", () => Config.ApexFishExpBonus, v => Config.ApexFishExpBonus = v, 0, 100);
            AddInt(configMenu, "legendary-fish-exp-bonus", () => Config.LegendaryFishExpBonus, v => Config.LegendaryFishExpBonus = v, 0, 300);
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

    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);
    }
}
