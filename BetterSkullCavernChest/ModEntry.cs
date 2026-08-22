using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace BetterSkullCavernChest
{
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatValue = null, string? fieldId = null);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    }

    public class ModEntry : Mod
    {
        private const string GeneratedModDataKey = "feiluvnana.BetterSkullCavernChest/Generated";

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

            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (e.NewLocation is MineShaft shaft && shaft.mineLevel > 120)
            {
                ProcessMineShaftChests(shaft);
            }
        }

        public static void ProcessMineShaftChests(MineShaft shaft)
        {
            if (shaft == null || shaft.mineLevel <= 120)
                return;

            bool isTreasureRoom = false;
            try
            {
                var netIsTreasureRoomField = ModHelper?.Reflection.GetField<Netcode.NetBool>(shaft, "netIsTreasureRoom", required: false);
                if (netIsTreasureRoomField != null)
                {
                    isTreasureRoom = netIsTreasureRoomField.GetValue()?.Value ?? false;
                }
            }
            catch
            {
                // Fallback
            }

            bool isForcedSpecialChest = shaft.mineLevel == 220 || shaft.mineLevel == 320 || shaft.mineLevel == 420;

            if (!isTreasureRoom && !isForcedSpecialChest)
                return;

            if (shaft.Objects == null)
                return;

            // Ensure special chest exists on repeatable runs for Floor 100/200/300 even if marked consumed by vanilla
            if (isForcedSpecialChest)
            {
                Vector2 vector = new Vector2(9f, 9f);
                if (shaft.mineLevel == 320)
                    vector.X += 1f;

                if (!shaft.overlayObjects.ContainsKey(vector) && !shaft.Objects.ContainsKey(vector))
                {
                    Chest chest = new Chest(new List<Item>(), vector);
                    chest.SetBigCraftableSpriteIndex(344);
                    shaft.overlayObjects[vector] = chest;
                }

                if (shaft.mineLevel == 320 || shaft.mineLevel == 420)
                {
                    Vector2 secVector = vector + new Vector2(-2f, 0f);
                    if (!shaft.overlayObjects.ContainsKey(secVector) && !shaft.Objects.ContainsKey(secVector))
                    {
                        Chest secChest = new Chest(new List<Item>(), secVector)
                        {
                            Tint = new Color(255, 210, 200)
                        };
                        secChest.SetBigCraftableSpriteIndex(344);
                        shaft.overlayObjects[secVector] = secChest;
                    }
                }

                if (shaft.mineLevel == 420)
                {
                    Vector2 tertVector = vector + new Vector2(2f, 0f);
                    if (!shaft.overlayObjects.ContainsKey(tertVector) && !shaft.Objects.ContainsKey(tertVector))
                    {
                        Chest tertChest = new Chest(new List<Item>(), tertVector)
                        {
                            Tint = new Color(216, 255, 240)
                        };
                        tertChest.SetBigCraftableSpriteIndex(344);
                        shaft.overlayObjects[tertVector] = tertChest;
                    }
                }
            }

            var allChests = new List<Chest>();
            foreach (var obj in shaft.Objects.Values)
            {
                if (obj is Chest c) allChests.Add(c);
            }
            foreach (var obj in shaft.overlayObjects.Values)
            {
                if (obj is Chest c && !allChests.Contains(c)) allChests.Add(c);
            }

            foreach (var chest in allChests)
            {
                if (chest.modData.ContainsKey(GeneratedModDataKey))
                    continue;

                chest.modData[GeneratedModDataKey] = "true";
                bool isSpecial = isForcedSpecialChest || (chest.giftbox.Value == false && chest.bigCraftableSpriteIndex.Value == 344);

                if (Config.EnableCustomRewards)
                {
                    var rewards = RewardGenerator.GenerateRewards(Config, Game1.random, isSpecialChest: isSpecial);
                    if (rewards.Count > 0)
                    {
                        chest.Items.Clear();
                        foreach (var reward in rewards)
                        {
                            chest.Items.Add(reward);
                        }
                    }
                }
                else if (Config.ExcludeCosmetics)
                {
                    for (int i = chest.Items.Count - 1; i >= 0; i--)
                    {
                        if (chest.Items[i] != null && RewardGenerator.IsCosmeticItem(chest.Items[i]))
                        {
                            chest.Items.RemoveAt(i);
                        }
                    }

                    if (chest.Items.Count == 0)
                    {
                        var fallback = RewardGenerator.GenerateRewards(Config, Game1.random, isSpecialChest: isSpecial);
                        foreach (var item in fallback)
                        {
                            chest.Items.Add(item);
                        }
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
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.general"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-custom-rewards.name"),
                tooltip: () => I18n.Get("config.enable-custom-rewards.tooltip"),
                getValue: () => Config.EnableCustomRewards,
                setValue: value => Config.EnableCustomRewards = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.exclude-cosmetics.name"),
                tooltip: () => I18n.Get("config.exclude-cosmetics.tooltip"),
                getValue: () => Config.ExcludeCosmetics,
                setValue: value => Config.ExcludeCosmetics = value
            );

            // Decaying Multi-Roll Section (Regular Chests)
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.decaying-rolls"));
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.max-rolls.name"),
                tooltip: () => I18n.Get("config.max-rolls.tooltip"),
                getValue: () => Config.MaxRolls,
                setValue: value => Config.MaxRolls = value,
                min: 1,
                max: 6
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-2-chance.name"),
                tooltip: () => I18n.Get("config.roll-2-chance.tooltip"),
                getValue: () => Config.Roll2Chance,
                setValue: value => Config.Roll2Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-3-chance.name"),
                tooltip: () => I18n.Get("config.roll-3-chance.tooltip"),
                getValue: () => Config.Roll3Chance,
                setValue: value => Config.Roll3Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-4-chance.name"),
                tooltip: () => I18n.Get("config.roll-4-chance.tooltip"),
                getValue: () => Config.Roll4Chance,
                setValue: value => Config.Roll4Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-5-chance.name"),
                tooltip: () => I18n.Get("config.roll-5-chance.tooltip"),
                getValue: () => Config.Roll5Chance,
                setValue: value => Config.Roll5Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.roll-6-chance.name"),
                tooltip: () => I18n.Get("config.roll-6-chance.tooltip"),
                getValue: () => Config.Roll6Chance,
                setValue: value => Config.Roll6Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            // Stack Multipliers Section (Regular Chests)
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.stack-multipliers"));
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.double-stack-chance.name"),
                tooltip: () => I18n.Get("config.double-stack-chance.tooltip"),
                getValue: () => Config.DoubleStackChance,
                setValue: value => Config.DoubleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.triple-stack-chance.name"),
                tooltip: () => I18n.Get("config.triple-stack-chance.tooltip"),
                getValue: () => Config.TripleStackChance,
                setValue: value => Config.TripleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.quadruple-stack-chance.name"),
                tooltip: () => I18n.Get("config.quadruple-stack-chance.tooltip"),
                getValue: () => Config.QuadrupleStackChance,
                setValue: value => Config.QuadrupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.quintuple-stack-chance.name"),
                tooltip: () => I18n.Get("config.quintuple-stack-chance.tooltip"),
                getValue: () => Config.QuintupleStackChance,
                setValue: value => Config.QuintupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            // Floor 100 Special Chest Buff Section
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.floor-100-buffs"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-floor-100-buff.name"),
                tooltip: () => I18n.Get("config.enable-floor-100-buff.tooltip"),
                getValue: () => Config.EnableFloor100Buff,
                setValue: value => Config.EnableFloor100Buff = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-all-categories-equal.name"),
                tooltip: () => I18n.Get("config.floor-100-all-categories-equal.tooltip"),
                getValue: () => Config.Floor100AllCategoriesEqual,
                setValue: value => Config.Floor100AllCategoriesEqual = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-max-rolls.name"),
                tooltip: () => I18n.Get("config.floor-100-max-rolls.tooltip"),
                getValue: () => Config.Floor100MaxRolls,
                setValue: value => Config.Floor100MaxRolls = value,
                min: 1,
                max: 12
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-2-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-2-chance.tooltip"),
                getValue: () => Config.Floor100Roll2Chance,
                setValue: value => Config.Floor100Roll2Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-3-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-3-chance.tooltip"),
                getValue: () => Config.Floor100Roll3Chance,
                setValue: value => Config.Floor100Roll3Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-4-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-4-chance.tooltip"),
                getValue: () => Config.Floor100Roll4Chance,
                setValue: value => Config.Floor100Roll4Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-5-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-5-chance.tooltip"),
                getValue: () => Config.Floor100Roll5Chance,
                setValue: value => Config.Floor100Roll5Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-6-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-6-chance.tooltip"),
                getValue: () => Config.Floor100Roll6Chance,
                setValue: value => Config.Floor100Roll6Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-7-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-7-chance.tooltip"),
                getValue: () => Config.Floor100Roll7Chance,
                setValue: value => Config.Floor100Roll7Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-8-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-8-chance.tooltip"),
                getValue: () => Config.Floor100Roll8Chance,
                setValue: value => Config.Floor100Roll8Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-9-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-9-chance.tooltip"),
                getValue: () => Config.Floor100Roll9Chance,
                setValue: value => Config.Floor100Roll9Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-10-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-10-chance.tooltip"),
                getValue: () => Config.Floor100Roll10Chance,
                setValue: value => Config.Floor100Roll10Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-11-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-11-chance.tooltip"),
                getValue: () => Config.Floor100Roll11Chance,
                setValue: value => Config.Floor100Roll11Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-roll-12-chance.name"),
                tooltip: () => I18n.Get("config.floor-100-roll-12-chance.tooltip"),
                getValue: () => Config.Floor100Roll12Chance,
                setValue: value => Config.Floor100Roll12Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-double-stack.name"),
                tooltip: () => I18n.Get("config.floor-100-double-stack.tooltip"),
                getValue: () => Config.Floor100DoubleStackChance,
                setValue: value => Config.Floor100DoubleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-triple-stack.name"),
                tooltip: () => I18n.Get("config.floor-100-triple-stack.tooltip"),
                getValue: () => Config.Floor100TripleStackChance,
                setValue: value => Config.Floor100TripleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-quadruple-stack.name"),
                tooltip: () => I18n.Get("config.floor-100-quadruple-stack.tooltip"),
                getValue: () => Config.Floor100QuadrupleStackChance,
                setValue: value => Config.Floor100QuadrupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.floor-100-quintuple-stack.name"),
                tooltip: () => I18n.Get("config.floor-100-quintuple-stack.tooltip"),
                getValue: () => Config.Floor100QuintupleStackChance,
                setValue: value => Config.Floor100QuintupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            // Category Weights Section
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.category-weights"));
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.legendary-weight.name"),
                tooltip: () => I18n.Get("config.legendary-weight.tooltip"),
                getValue: () => (float)Config.LegendaryWeight,
                setValue: value => Config.LegendaryWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.agriculture-weight.name"),
                tooltip: () => I18n.Get("config.agriculture-weight.tooltip"),
                getValue: () => (float)Config.AgricultureWeight,
                setValue: value => Config.AgricultureWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.mining-weight.name"),
                tooltip: () => I18n.Get("config.mining-weight.tooltip"),
                getValue: () => (float)Config.MiningWeight,
                setValue: value => Config.MiningWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.fishing-weight.name"),
                tooltip: () => I18n.Get("config.fishing-weight.tooltip"),
                getValue: () => (float)Config.FishingWeight,
                setValue: value => Config.FishingWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.combat-weight.name"),
                tooltip: () => I18n.Get("config.combat-weight.tooltip"),
                getValue: () => (float)Config.CombatWeight,
                setValue: value => Config.CombatWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.foraging-weight.name"),
                tooltip: () => I18n.Get("config.foraging-weight.tooltip"),
                getValue: () => (float)Config.ForagingWeight,
                setValue: value => Config.ForagingWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.lootbox-weight.name"),
                tooltip: () => I18n.Get("config.lootbox-weight.tooltip"),
                getValue: () => (float)Config.LootboxWeight,
                setValue: value => Config.LootboxWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );

            // Category Toggles
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.category-toggles"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-legendary-category.name"),
                tooltip: () => I18n.Get("config.enable-legendary-category.tooltip"),
                getValue: () => Config.EnableLegendaryCategory,
                setValue: value => Config.EnableLegendaryCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-agriculture-category.name"),
                tooltip: () => I18n.Get("config.enable-agriculture-category.tooltip"),
                getValue: () => Config.EnableAgricultureCategory,
                setValue: value => Config.EnableAgricultureCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-mining-category.name"),
                tooltip: () => I18n.Get("config.enable-mining-category.tooltip"),
                getValue: () => Config.EnableMiningCategory,
                setValue: value => Config.EnableMiningCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-fishing-category.name"),
                tooltip: () => I18n.Get("config.enable-fishing-category.tooltip"),
                getValue: () => Config.EnableFishingCategory,
                setValue: value => Config.EnableFishingCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-combat-category.name"),
                tooltip: () => I18n.Get("config.enable-combat-category.tooltip"),
                getValue: () => Config.EnableCombatCategory,
                setValue: value => Config.EnableCombatCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-foraging-category.name"),
                tooltip: () => I18n.Get("config.enable-foraging-category.tooltip"),
                getValue: () => Config.EnableForagingCategory,
                setValue: value => Config.EnableForagingCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-lootbox-category.name"),
                tooltip: () => I18n.Get("config.enable-lootbox-category.tooltip"),
                getValue: () => Config.EnableLootboxCategory,
                setValue: value => Config.EnableLootboxCategory = value
            );

            // Detailed Item Feature Toggles
            configMenu.AddSectionTitle(mod: ModManifest, text: () => I18n.Get("config.section.item-toggles"));
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-fertilizers.name"),
                tooltip: () => I18n.Get("config.enable-fertilizers.tooltip"),
                getValue: () => Config.EnableFertilizers,
                setValue: value => Config.EnableFertilizers = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-auto-petter.name"),
                tooltip: () => I18n.Get("config.enable-auto-petter.tooltip"),
                getValue: () => Config.EnableAutoPetter,
                setValue: value => Config.EnableAutoPetter = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-radioactive-items.name"),
                tooltip: () => I18n.Get("config.enable-radioactive-items.tooltip"),
                getValue: () => Config.EnableRadioactiveItems,
                setValue: value => Config.EnableRadioactiveItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.gatekeep-radioactive-items.name"),
                tooltip: () => I18n.Get("config.gatekeep-radioactive-items.tooltip"),
                getValue: () => Config.GatekeepRadioactiveItems,
                setValue: value => Config.GatekeepRadioactiveItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-iridium-items.name"),
                tooltip: () => I18n.Get("config.enable-iridium-items.tooltip"),
                getValue: () => Config.EnableIridiumItems,
                setValue: value => Config.EnableIridiumItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-bombs.name"),
                tooltip: () => I18n.Get("config.enable-bombs.tooltip"),
                getValue: () => Config.EnableBombs,
                setValue: value => Config.EnableBombs = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-fishing-tackle.name"),
                tooltip: () => I18n.Get("config.enable-fishing-tackle.tooltip"),
                getValue: () => Config.EnableFishingTackle,
                setValue: value => Config.EnableFishingTackle = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-slime-eggs.name"),
                tooltip: () => I18n.Get("config.enable-slime-eggs.tooltip"),
                getValue: () => Config.EnableSlimeEggs,
                setValue: value => Config.EnableSlimeEggs = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-combat-consumables.name"),
                tooltip: () => I18n.Get("config.enable-combat-consumables.tooltip"),
                getValue: () => Config.EnableCombatConsumables,
                setValue: value => Config.EnableCombatConsumables = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-rare-seeds.name"),
                tooltip: () => I18n.Get("config.enable-rare-seeds.tooltip"),
                getValue: () => Config.EnableRareSeeds,
                setValue: value => Config.EnableRareSeeds = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-warp-totems.name"),
                tooltip: () => I18n.Get("config.enable-warp-totems.tooltip"),
                getValue: () => Config.EnableWarpTotems,
                setValue: value => Config.EnableWarpTotems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-mystery-boxes.name"),
                tooltip: () => I18n.Get("config.enable-mystery-boxes.tooltip"),
                getValue: () => Config.EnableMysteryBoxes,
                setValue: value => Config.EnableMysteryBoxes = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-omni-geodes.name"),
                tooltip: () => I18n.Get("config.enable-omni-geodes.tooltip"),
                getValue: () => Config.EnableOmniGeodes,
                setValue: value => Config.EnableOmniGeodes = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-calico-eggs.name"),
                tooltip: () => I18n.Get("config.enable-calico-eggs.tooltip"),
                getValue: () => Config.EnableCalicoEggs,
                setValue: value => Config.EnableCalicoEggs = value
            );
        }
    }
}