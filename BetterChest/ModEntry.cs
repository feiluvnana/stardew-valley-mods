using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using Common;

namespace BetterChest
{
    /// <summary>
    /// The mod's main entry point. Sets up shared services, Harmony patches, and GMCM configuration.
    /// </summary>
    public class ModEntry : Mod
    {
        public const string GeneratedModDataKey = "feiluvnana.BetterChest/Generated";

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
            ChestPatches.Apply(harmony);
            FishingPatches.Apply(harmony);

            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            try
            {
                if (e.NewLocation is MineShaft shaft && shaft.mineLevel > 120)
                {
                    ProcessMineShaftChests(shaft);
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Error tagging Skull Cavern chests on level warp: {ex}", LogLevel.Error);
            }
        }

        private void ProcessMineShaftChests(MineShaft shaft)
        {
            var netIsTreasureRoom = Helper.Reflection.GetField<Netcode.NetBool>(shaft, "netIsTreasureRoom", required: false);
            bool isTreasureRoom = netIsTreasureRoom?.GetValue()?.Value ?? false;

            if (!isTreasureRoom && shaft.mineLevel != 220)
                return;

            foreach (var pair in shaft.Objects.Pairs)
            {
                if (pair.Value is Chest chest)
                {
                    if (chest.modData.ContainsKey(GeneratedModDataKey))
                        continue;

                    chest.modData[GeneratedModDataKey] = "true";

                    if (Config.EnableCustomRewards)
                    {
                        chest.Items.Clear();
                    }
                }
            }
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

            // General Section
            AddSection(configMenu, "general");
            AddBool(configMenu, "enable-custom-rewards", () => Config.EnableCustomRewards, v => Config.EnableCustomRewards = v);
            AddBool(configMenu, "exclude-cosmetics", () => Config.ExcludeCosmetics, v => Config.ExcludeCosmetics = v);
            AddBool(configMenu, "enable-depth-scaling", () => Config.EnableDepthScaling, v => Config.EnableDepthScaling = v);
            AddBool(configMenu, "scale-legendary-by-depth", () => Config.ScaleLegendaryByDepth, v => Config.ScaleLegendaryByDepth = v);

            // Progression & Gatekeeping Section
            AddSection(configMenu, "progression-gatekeeping");
            AddBool(configMenu, "gatekeep-mastery-items", () => Config.GatekeepMasteryItems, v => Config.GatekeepMasteryItems = v);
            AddBool(configMenu, "gatekeep-island-items", () => Config.GatekeepIslandItems, v => Config.GatekeepIslandItems = v);
            AddBool(configMenu, "gatekeep-qi-items", () => Config.GatekeepQiItems, v => Config.GatekeepQiItems = v);
            AddBool(configMenu, "gatekeep-mystery-boxes", () => Config.GatekeepMysteryBoxes, v => Config.GatekeepMysteryBoxes = v);
            AddBool(configMenu, "gatekeep-calico-eggs", () => Config.GatekeepCalicoEggs, v => Config.GatekeepCalicoEggs = v);
            AddBool(configMenu, "gatekeep-radioactive-items", () => Config.GatekeepRadioactiveItems, v => Config.GatekeepRadioactiveItems = v);
            AddBool(configMenu, "gatekeep-auto-petter", () => Config.GatekeepAutoPetter, v => Config.GatekeepAutoPetter = v);

            // Decaying Multi-Roll Section (Regular Chests)
            AddSection(configMenu, "decaying-rolls");
            AddInt(configMenu, "max-rolls", () => Config.MaxRolls, v => Config.MaxRolls = v, 1, 8);
            AddFloat(configMenu, "roll-2-chance", () => Config.Roll2Chance, v => Config.Roll2Chance = v);
            AddFloat(configMenu, "roll-3-chance", () => Config.Roll3Chance, v => Config.Roll3Chance = v);
            AddFloat(configMenu, "roll-4-chance", () => Config.Roll4Chance, v => Config.Roll4Chance = v);
            AddFloat(configMenu, "roll-5-chance", () => Config.Roll5Chance, v => Config.Roll5Chance = v);
            AddFloat(configMenu, "roll-6-chance", () => Config.Roll6Chance, v => Config.Roll6Chance = v);
            AddFloat(configMenu, "roll-7-chance", () => Config.Roll7Chance, v => Config.Roll7Chance = v);
            AddFloat(configMenu, "roll-8-chance", () => Config.Roll8Chance, v => Config.Roll8Chance = v);

            // Stack Multipliers Section (Regular Chests)
            AddSection(configMenu, "stack-multipliers");
            AddFloat(configMenu, "double-stack-chance", () => Config.DoubleStackChance, v => Config.DoubleStackChance = v);
            AddFloat(configMenu, "triple-stack-chance", () => Config.TripleStackChance, v => Config.TripleStackChance = v);
            AddFloat(configMenu, "quadruple-stack-chance", () => Config.QuadrupleStackChance, v => Config.QuadrupleStackChance = v);
            AddFloat(configMenu, "quintuple-stack-chance", () => Config.QuintupleStackChance, v => Config.QuintupleStackChance = v);

            // Floor 100 Special Chest Buff Section
            AddSection(configMenu, "floor-100-buffs");
            AddBool(configMenu, "enable-floor-100-buff", () => Config.EnableFloor100Buff, v => Config.EnableFloor100Buff = v);
            AddBool(configMenu, "floor-100-all-categories-equal", () => Config.Floor100AllCategoriesEqual, v => Config.Floor100AllCategoriesEqual = v);
            AddInt(configMenu, "floor-100-max-rolls", () => Config.Floor100MaxRolls, v => Config.Floor100MaxRolls = v, 1, 12);
            AddFloat(configMenu, "floor-100-roll-2-chance", () => Config.Floor100Roll2Chance, v => Config.Floor100Roll2Chance = v);
            AddFloat(configMenu, "floor-100-roll-3-chance", () => Config.Floor100Roll3Chance, v => Config.Floor100Roll3Chance = v);
            AddFloat(configMenu, "floor-100-roll-4-chance", () => Config.Floor100Roll4Chance, v => Config.Floor100Roll4Chance = v);
            AddFloat(configMenu, "floor-100-roll-5-chance", () => Config.Floor100Roll5Chance, v => Config.Floor100Roll5Chance = v);
            AddFloat(configMenu, "floor-100-roll-6-chance", () => Config.Floor100Roll6Chance, v => Config.Floor100Roll6Chance = v);
            AddFloat(configMenu, "floor-100-roll-7-chance", () => Config.Floor100Roll7Chance, v => Config.Floor100Roll7Chance = v);
            AddFloat(configMenu, "floor-100-roll-8-chance", () => Config.Floor100Roll8Chance, v => Config.Floor100Roll8Chance = v);
            AddFloat(configMenu, "floor-100-roll-9-chance", () => Config.Floor100Roll9Chance, v => Config.Floor100Roll9Chance = v);
            AddFloat(configMenu, "floor-100-roll-10-chance", () => Config.Floor100Roll10Chance, v => Config.Floor100Roll10Chance = v);
            AddFloat(configMenu, "floor-100-roll-11-chance", () => Config.Floor100Roll11Chance, v => Config.Floor100Roll11Chance = v);
            AddFloat(configMenu, "floor-100-roll-12-chance", () => Config.Floor100Roll12Chance, v => Config.Floor100Roll12Chance = v);
            AddFloat(configMenu, "floor-100-double-stack", () => Config.Floor100DoubleStackChance, v => Config.Floor100DoubleStackChance = v);
            AddFloat(configMenu, "floor-100-triple-stack", () => Config.Floor100TripleStackChance, v => Config.Floor100TripleStackChance = v);
            AddFloat(configMenu, "floor-100-quadruple-stack", () => Config.Floor100QuadrupleStackChance, v => Config.Floor100QuadrupleStackChance = v);
            AddFloat(configMenu, "floor-100-quintuple-stack", () => Config.Floor100QuintupleStackChance, v => Config.Floor100QuintupleStackChance = v);

            // Category Weights Section
            AddSection(configMenu, "category-weights");
            AddFloat(configMenu, "legendary-weight", () => (float)Config.LegendaryWeight, v => Config.LegendaryWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "agriculture-weight", () => (float)Config.AgricultureWeight, v => Config.AgricultureWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "mining-weight", () => (float)Config.MiningWeight, v => Config.MiningWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "fishing-weight", () => (float)Config.FishingWeight, v => Config.FishingWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "combat-weight", () => (float)Config.CombatWeight, v => Config.CombatWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "foraging-weight", () => (float)Config.ForagingWeight, v => Config.ForagingWeight = v, 0f, 100f, 1f);
            AddFloat(configMenu, "lootbox-weight", () => (float)Config.LootboxWeight, v => Config.LootboxWeight = v, 0f, 100f, 1f);

            // Category Toggles
            AddSection(configMenu, "category-toggles");
            AddBool(configMenu, "enable-legendary-category", () => Config.EnableLegendaryCategory, v => Config.EnableLegendaryCategory = v);
            AddBool(configMenu, "enable-agriculture-category", () => Config.EnableAgricultureCategory, v => Config.EnableAgricultureCategory = v);
            AddBool(configMenu, "enable-mining-category", () => Config.EnableMiningCategory, v => Config.EnableMiningCategory = v);
            AddBool(configMenu, "enable-fishing-category", () => Config.EnableFishingCategory, v => Config.EnableFishingCategory = v);
            AddBool(configMenu, "enable-combat-category", () => Config.EnableCombatCategory, v => Config.EnableCombatCategory = v);
            AddBool(configMenu, "enable-foraging-category", () => Config.EnableForagingCategory, v => Config.EnableForagingCategory = v);
            AddBool(configMenu, "enable-lootbox-category", () => Config.EnableLootboxCategory, v => Config.EnableLootboxCategory = v);

            // Detailed Item Feature Toggles
            AddSection(configMenu, "item-toggles");
            AddBool(configMenu, "enable-fertilizers", () => Config.EnableFertilizers, v => Config.EnableFertilizers = v);
            AddBool(configMenu, "enable-auto-petter", () => Config.EnableAutoPetter, v => Config.EnableAutoPetter = v);
            AddBool(configMenu, "enable-radioactive-items", () => Config.EnableRadioactiveItems, v => Config.EnableRadioactiveItems = v);
            AddBool(configMenu, "enable-iridium-items", () => Config.EnableIridiumItems, v => Config.EnableIridiumItems = v);
            AddBool(configMenu, "enable-bombs", () => Config.EnableBombs, v => Config.EnableBombs = v);
            AddBool(configMenu, "enable-fishing-tackle", () => Config.EnableFishingTackle, v => Config.EnableFishingTackle = v);
            AddBool(configMenu, "enable-slime-eggs", () => Config.EnableSlimeEggs, v => Config.EnableSlimeEggs = v);
            AddBool(configMenu, "enable-combat-consumables", () => Config.EnableCombatConsumables, v => Config.EnableCombatConsumables = v);
            AddBool(configMenu, "enable-rare-seeds", () => Config.EnableRareSeeds, v => Config.EnableRareSeeds = v);
            AddBool(configMenu, "enable-coal", () => Config.EnableCoal, v => Config.EnableCoal = v);
            AddBool(configMenu, "enable-hardwood", () => Config.EnableHardwood, v => Config.EnableHardwood = v);
            AddBool(configMenu, "enable-mystery-boxes", () => Config.EnableMysteryBoxes, v => Config.EnableMysteryBoxes = v);
            AddBool(configMenu, "enable-omni-geodes", () => Config.EnableOmniGeodes, v => Config.EnableOmniGeodes = v);
            AddBool(configMenu, "enable-calico-eggs", () => Config.EnableCalicoEggs, v => Config.EnableCalicoEggs = v);

            // Fishing Treasure Chests GMCM Section
            AddSection(configMenu, "fishing-chests");
            AddBool(configMenu, "enable-fishing-chest-buff", () => Config.EnableFishingChestBuff, v => Config.EnableFishingChestBuff = v);
            AddInt(configMenu, "fishing-chest-min-rolls", () => Config.FishingChestMinRolls, v => Config.FishingChestMinRolls = v, 1, 10);
            AddInt(configMenu, "fishing-chest-max-rolls", () => Config.FishingChestMaxRolls, v => Config.FishingChestMaxRolls = v, 1, 12);
            AddInt(configMenu, "golden-chest-min-rolls", () => Config.GoldenChestMinRolls, v => Config.GoldenChestMinRolls = v, 1, 10);
            AddInt(configMenu, "golden-chest-max-rolls", () => Config.GoldenChestMaxRolls, v => Config.GoldenChestMaxRolls = v, 1, 12);
            AddBool(configMenu, "enable-fishing-trash-reroll-bonus", () => Config.EnableFishingTrashRerollBonus, v => Config.EnableFishingTrashRerollBonus = v);
        }

        private void AddSection(IGenericModConfigMenuApi menu, string sectionKey)
        {
            menu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get($"config.section.{sectionKey}"));
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

        private void AddInt(IGenericModConfigMenuApi menu, string optionKey, Func<int> getter, Action<int> setter, int min, int max, int interval = 1)
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

        private void AddFloat(IGenericModConfigMenuApi menu, string optionKey, Func<float> getter, Action<float> setter, float min = 0.0f, float max = 1.0f, float interval = 0.01f)
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