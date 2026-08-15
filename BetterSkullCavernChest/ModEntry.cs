using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
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
        public static ModConfig Config { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;

        private static readonly AccessTools.FieldRef<MineShaft, NetBool>? NetIsTreasureRoomRef =
            AccessTools.FieldRefAccess<MineShaft, NetBool>("netIsTreasureRoom");

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor;

            var harmony = new Harmony(ModManifest.UniqueID);
            try
            {
                var addLevelChestsMethod = AccessTools.Method(typeof(MineShaft), "addLevelChests");
                if (addLevelChestsMethod != null)
                {
                    harmony.Patch(
                        original: addLevelChestsMethod,
                        postfix: new HarmonyMethod(typeof(ModEntry), nameof(MineShaft_addLevelChests_Postfix))
                    );
                }

                Monitor.Log("Harmony patches for BetterSkullCavernChest applied successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply BetterSkullCavernChest harmony patches: {ex}", LogLevel.Error);
            }

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        public static void MineShaft_addLevelChests_Postfix(MineShaft? __instance)
        {
            if (__instance == null || __instance.mineLevel <= 120)
                return;

            bool isTreasureRoom = NetIsTreasureRoomRef != null && NetIsTreasureRoomRef(__instance).Value;
            bool isForcedSpecialChest = __instance.mineLevel == 220 || __instance.mineLevel == 320 || __instance.mineLevel == 420;

            if (!isTreasureRoom && !isForcedSpecialChest)
                return;

            if (__instance.Objects == null)
                return;

            // Ensure special chest exists on repeatable runs for Floor 100/200/300 even if marked consumed by vanilla
            if (isForcedSpecialChest)
            {
                Vector2 vector = new Vector2(9f, 9f);
                if (__instance.mineLevel == 320)
                    vector.X += 1f;

                if (!__instance.overlayObjects.ContainsKey(vector) && !__instance.Objects.ContainsKey(vector))
                {
                    Chest chest = new Chest(new List<Item>(), vector);
                    chest.SetBigCraftableSpriteIndex(344);
                    __instance.overlayObjects[vector] = chest;
                }

                if (__instance.mineLevel == 320 || __instance.mineLevel == 420)
                {
                    Vector2 secVector = vector + new Vector2(-2f, 0f);
                    if (!__instance.overlayObjects.ContainsKey(secVector) && !__instance.Objects.ContainsKey(secVector))
                    {
                        Chest secChest = new Chest(new List<Item>(), secVector)
                        {
                            Tint = new Color(255, 210, 200)
                        };
                        secChest.SetBigCraftableSpriteIndex(344);
                        __instance.overlayObjects[secVector] = secChest;
                    }
                }

                if (__instance.mineLevel == 420)
                {
                    Vector2 tertVector = vector + new Vector2(2f, 0f);
                    if (!__instance.overlayObjects.ContainsKey(tertVector) && !__instance.Objects.ContainsKey(tertVector))
                    {
                        Chest tertChest = new Chest(new List<Item>(), tertVector)
                        {
                            Tint = new Color(216, 255, 240)
                        };
                        tertChest.SetBigCraftableSpriteIndex(344);
                        __instance.overlayObjects[tertVector] = tertChest;
                    }
                }
            }

            foreach (var obj in __instance.Objects.Values)
            {
                if (obj is Chest chest)
                {
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
            configMenu.AddSectionTitle(mod: ModManifest, text: () => "General Settings");
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Custom Rewards",
                tooltip: () => "Replace Skull Cavern chest rewards with the enhanced 7-category loot system.",
                getValue: () => Config.EnableCustomRewards,
                setValue: value => Config.EnableCustomRewards = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Exclude Cosmetics",
                tooltip: () => "Exclude hats, clothing, and decorative items when custom rewards are disabled.",
                getValue: () => Config.ExcludeCosmetics,
                setValue: value => Config.ExcludeCosmetics = value
            );

            // Decaying Multi-Roll Section (Regular Chests)
            configMenu.AddSectionTitle(mod: ModManifest, text: () => "Decaying Multi-Rolls (Regular Chests)");
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Max Rolls Per Chest",
                tooltip: () => "Maximum number of item rolls a regular chest can attempt (1 to 5).",
                getValue: () => Config.MaxRolls,
                setValue: value => Config.MaxRolls = value,
                min: 1,
                max: 5
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Roll #2 Chance",
                tooltip: () => "Probability (0.0 to 1.0) to successfully roll a 2nd item.",
                getValue: () => Config.Roll2Chance,
                setValue: value => Config.Roll2Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.05f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Roll #3 Chance",
                tooltip: () => "Probability (0.0 to 1.0) to successfully roll a 3rd item.",
                getValue: () => Config.Roll3Chance,
                setValue: value => Config.Roll3Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.05f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Roll #4 Chance",
                tooltip: () => "Probability (0.0 to 1.0) to successfully roll a 4th item.",
                getValue: () => Config.Roll4Chance,
                setValue: value => Config.Roll4Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.05f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Roll #5 Chance",
                tooltip: () => "Probability (0.0 to 1.0) to successfully roll a 5th item.",
                getValue: () => Config.Roll5Chance,
                setValue: value => Config.Roll5Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.05f
            );

            // Stack Multipliers Section (Regular Chests)
            configMenu.AddSectionTitle(mod: ModManifest, text: () => "Jackpot Stack Multipliers (Regular Chests)");
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Double Stack Chance (2x)",
                tooltip: () => "Chance (0.0 to 1.0) for a rolled item stack to be doubled.",
                getValue: () => Config.DoubleStackChance,
                setValue: value => Config.DoubleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Triple Stack Chance (3x)",
                tooltip: () => "Chance (0.0 to 1.0) for a rolled item stack to be tripled.",
                getValue: () => Config.TripleStackChance,
                setValue: value => Config.TripleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Quadruple Stack Chance (4x)",
                tooltip: () => "Chance (0.0 to 1.0) for a rolled item stack in regular chests to be quadrupled (4x).",
                getValue: () => Config.QuadrupleStackChance,
                setValue: value => Config.QuadrupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Quintuple Stack Chance (5x)",
                tooltip: () => "Chance (0.0 to 1.0) for a rolled item stack in regular chests to be quintupled (5x).",
                getValue: () => Config.QuintupleStackChance,
                setValue: value => Config.QuintupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            // Floor 100 Special Chest Buff Section
            configMenu.AddSectionTitle(mod: ModManifest, text: () => "Floor 100 Special Chest Buffs");
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Floor 100 Buff",
                tooltip: () => "Enable the dedicated enhanced loot table and roll system on Floor 100 (and special forced chest levels).",
                getValue: () => Config.EnableFloor100Buff,
                setValue: value => Config.EnableFloor100Buff = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "All Categories Equal (Floor 100)",
                tooltip: () => "When enabled, all 7 active categories have equal probability (~14.28% each), giving Legendary items the same rate as others.",
                getValue: () => Config.Floor100AllCategoriesEqual,
                setValue: value => Config.Floor100AllCategoriesEqual = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Max Rolls",
                tooltip: () => "Maximum number of item rolls for Floor 100 special chests (1 to 10).",
                getValue: () => Config.Floor100MaxRolls,
                setValue: value => Config.Floor100MaxRolls = value,
                min: 1,
                max: 10
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Roll #2 Chance",
                tooltip: () => "Probability (0.0 to 1.0) to roll a 2nd item on Floor 100.",
                getValue: () => Config.Floor100Roll2Chance,
                setValue: value => Config.Floor100Roll2Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.05f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Roll #3 Chance",
                tooltip: () => "Probability (0.0 to 1.0) to roll a 3rd item on Floor 100.",
                getValue: () => Config.Floor100Roll3Chance,
                setValue: value => Config.Floor100Roll3Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.05f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Roll #4 Chance",
                tooltip: () => "Probability (0.0 to 1.0) to roll a 4th item on Floor 100.",
                getValue: () => Config.Floor100Roll4Chance,
                setValue: value => Config.Floor100Roll4Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.05f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Roll #5 Chance",
                tooltip: () => "Probability (0.0 to 1.0) to roll a 5th item on Floor 100.",
                getValue: () => Config.Floor100Roll5Chance,
                setValue: value => Config.Floor100Roll5Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.05f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Roll #6 Chance",
                tooltip: () => "Probability (0.0 to 1.0) to roll a 6th item on Floor 100.",
                getValue: () => Config.Floor100Roll6Chance,
                setValue: value => Config.Floor100Roll6Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.05f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Roll #7 Chance",
                tooltip: () => "Probability (0.0 to 1.0) to roll a 7th item on Floor 100.",
                getValue: () => Config.Floor100Roll7Chance,
                setValue: value => Config.Floor100Roll7Chance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.05f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Double Stack (2x)",
                tooltip: () => "Chance (0.0 to 1.0) for an item stack on Floor 100 to be doubled.",
                getValue: () => Config.Floor100DoubleStackChance,
                setValue: value => Config.Floor100DoubleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Triple Stack (3x)",
                tooltip: () => "Chance (0.0 to 1.0) for an item stack on Floor 100 to be tripled.",
                getValue: () => Config.Floor100TripleStackChance,
                setValue: value => Config.Floor100TripleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Quadruple Stack (4x)",
                tooltip: () => "Chance (0.0 to 1.0) for an item stack on Floor 100 to be quadrupled (4x).",
                getValue: () => Config.Floor100QuadrupleStackChance,
                setValue: value => Config.Floor100QuadrupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Floor 100 Quintuple Stack (5x)",
                tooltip: () => "Chance (0.0 to 1.0) for an item stack on Floor 100 to be quintupled (5x Mega Jackpot).",
                getValue: () => Config.Floor100QuintupleStackChance,
                setValue: value => Config.Floor100QuintupleStackChance = value,
                min: 0.0f,
                max: 1.0f,
                interval: 0.01f
            );

            // Category Weights Section
            configMenu.AddSectionTitle(mod: ModManifest, text: () => "Category Roll Weights");
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Legendary Weight (10.0)",
                tooltip: () => "Relative weight for Legendary items (~10% chance).",
                getValue: () => (float)Config.LegendaryWeight,
                setValue: value => Config.LegendaryWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Agriculture Weight (15.0)",
                tooltip: () => "Relative weight for Agriculture items (~15% chance).",
                getValue: () => (float)Config.AgricultureWeight,
                setValue: value => Config.AgricultureWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Mining Weight (15.0)",
                tooltip: () => "Relative weight for Mining items (~15% chance).",
                getValue: () => (float)Config.MiningWeight,
                setValue: value => Config.MiningWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Fishing Weight (15.0)",
                tooltip: () => "Relative weight for Fishing items (~15% chance).",
                getValue: () => (float)Config.FishingWeight,
                setValue: value => Config.FishingWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Combat Weight (15.0)",
                tooltip: () => "Relative weight for Combat items (~15% chance).",
                getValue: () => (float)Config.CombatWeight,
                setValue: value => Config.CombatWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Foraging Weight (15.0)",
                tooltip: () => "Relative weight for Foraging items (~15% chance).",
                getValue: () => (float)Config.ForagingWeight,
                setValue: value => Config.ForagingWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Lootbox Weight (15.0)",
                tooltip: () => "Relative weight for Lootbox items (~15% chance).",
                getValue: () => (float)Config.LootboxWeight,
                setValue: value => Config.LootboxWeight = value,
                min: 0.0f,
                max: 100.0f,
                interval: 1.0f
            );

            // Category Toggles
            configMenu.AddSectionTitle(mod: ModManifest, text: () => "Category Toggles");
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Legendary Category",
                tooltip: () => "Enable or disable all Legendary items.",
                getValue: () => Config.EnableLegendaryCategory,
                setValue: value => Config.EnableLegendaryCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Agriculture Category",
                tooltip: () => "Enable or disable all Agriculture items.",
                getValue: () => Config.EnableAgricultureCategory,
                setValue: value => Config.EnableAgricultureCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Mining Category",
                tooltip: () => "Enable or disable all Mining items.",
                getValue: () => Config.EnableMiningCategory,
                setValue: value => Config.EnableMiningCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Fishing Category",
                tooltip: () => "Enable or disable all Fishing items.",
                getValue: () => Config.EnableFishingCategory,
                setValue: value => Config.EnableFishingCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Combat Category",
                tooltip: () => "Enable or disable all Combat items.",
                getValue: () => Config.EnableCombatCategory,
                setValue: value => Config.EnableCombatCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Foraging Category",
                tooltip: () => "Enable or disable all Foraging items.",
                getValue: () => Config.EnableForagingCategory,
                setValue: value => Config.EnableForagingCategory = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Lootboxes Category",
                tooltip: () => "Enable or disable all Lootboxes & Troves.",
                getValue: () => Config.EnableLootboxCategory,
                setValue: value => Config.EnableLootboxCategory = value
            );

            // Detailed Item Feature Toggles
            configMenu.AddSectionTitle(mod: ModManifest, text: () => "Item Feature Toggles");
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Books (All 24 Books)",
                tooltip: () => "Allow Skill & Power Books to appear in chests.",
                getValue: () => Config.EnableBooks,
                setValue: value => Config.EnableBooks = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Fertilizers & Attachments",
                tooltip: () => "Allow Hyper Speed-Gro, Deluxe Fertilizers, Pressure Nozzles, and Enrichers to appear.",
                getValue: () => Config.EnableFertilizers,
                setValue: value => Config.EnableFertilizers = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Radioactive Ore & Bars",
                tooltip: () => "Allow Radioactive Ore and Radioactive Bars to appear.",
                getValue: () => Config.EnableRadioactiveItems,
                setValue: value => Config.EnableRadioactiveItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Iridium Ore & Bars",
                tooltip: () => "Allow Iridium Ore and Iridium Bars to appear.",
                getValue: () => Config.EnableIridiumItems,
                setValue: value => Config.EnableIridiumItems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Bombs & Mega Bombs",
                tooltip: () => "Allow Mega Bombs and Bombs to appear.",
                getValue: () => Config.EnableBombs,
                setValue: value => Config.EnableBombs = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Fishing Tackle & Baits",
                tooltip: () => "Allow Trap Bobber, Curiosity Lure, Deluxe & Challenge Bait to appear.",
                getValue: () => Config.EnableFishingTackle,
                setValue: value => Config.EnableFishingTackle = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Slime Incubator Eggs",
                tooltip: () => "Allow Tiger, Purple, and Blue Slime Eggs to appear.",
                getValue: () => Config.EnableSlimeEggs,
                setValue: value => Config.EnableSlimeEggs = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Combat Consumables",
                tooltip: () => "Allow Life Elixir and Triple Shot Espresso to appear.",
                getValue: () => Config.EnableCombatConsumables,
                setValue: value => Config.EnableCombatConsumables = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Rare & Ancient Seeds",
                tooltip: () => "Allow Rare Seeds and Ancient Seeds to appear.",
                getValue: () => Config.EnableRareSeeds,
                setValue: value => Config.EnableRareSeeds = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Warp Totems",
                tooltip: () => "Allow Desert and Farm Warp Totems to appear.",
                getValue: () => Config.EnableWarpTotems,
                setValue: value => Config.EnableWarpTotems = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Mystery Boxes & Troves",
                tooltip: () => "Allow Mystery Boxes, Golden Mystery Boxes, and Artifact Troves to appear.",
                getValue: () => Config.EnableMysteryBoxes,
                setValue: value => Config.EnableMysteryBoxes = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Omni Geodes",
                tooltip: () => "Allow Omni Geodes to appear.",
                getValue: () => Config.EnableOmniGeodes,
                setValue: value => Config.EnableOmniGeodes = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Calico Eggs",
                tooltip: () => "Allow Calico Eggs to appear.",
                getValue: () => Config.EnableCalicoEggs,
                setValue: value => Config.EnableCalicoEggs = value
            );
        }
    }
}