using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Objects;

namespace BetterChest
{
    public enum LootCategory
    {
        Legendary,
        Agriculture,
        Mining,
        Fishing,
        Combat,
        Foraging,
        Lootboxes
    }

    public class RewardEntry
    {
        public string QualifiedItemId { get; set; }
        public LootCategory Category { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
        public double Weight { get; set; }
        public Func<ModConfig, bool> IsEnabled { get; set; }
        public bool AllowMultiplier { get; set; }

        public RewardEntry(
            string qualifiedItemId,
            LootCategory category,
            int minCount,
            int maxCount,
            double weight,
            Func<ModConfig, bool> isEnabled,
            bool allowMultiplier = true)
        {
            QualifiedItemId = qualifiedItemId;
            Category = category;
            MinCount = minCount;
            MaxCount = maxCount;
            Weight = weight;
            IsEnabled = isEnabled;
            AllowMultiplier = allowMultiplier && maxCount > 1;
        }
    }

    public static class RewardGenerator
    {
        private static readonly List<RewardEntry> RewardPool = new()
        {
            // =========================================================================
            // === 1. LEGENDARY CATEGORY (15% Category Weight)                       ===
            // =========================================================================
            new("(O)74", LootCategory.Legendary, 1, 2, 25.0, c => c.EnableLegendaryCategory && c.EnablePrismaticShard),
            new("(O)279", LootCategory.Legendary, 1, 2, 20.0, c => c.EnableLegendaryCategory && c.EnableMagicRockCandy),
            new("(O)GoldenAnimalCracker", LootCategory.Legendary, 1, 2, 20.0, c => c.EnableLegendaryCategory && c.EnableGoldenAnimalCracker && (!c.GatekeepMasteryItems || ProgressionHelper.IsMasteryUnlocked("Farming")), false),
            new("(BC)272", LootCategory.Legendary, 1, 1, 20.0, c => c.EnableLegendaryCategory && c.EnableAutoPetter && (!c.GatekeepAutoPetter || ProgressionHelper.IsCommunityCenterCompleted()), false), // Auto-Petter
            new("(O)896", LootCategory.Legendary, 1, 2, 15.0, c => c.EnableLegendaryCategory && c.EnableGalaxySoul && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked()), false),  // Galaxy Soul
            new("(O)StardropTea", LootCategory.Legendary, 1, 3, 15.0, c => c.EnableLegendaryCategory && c.EnableStardropTea),
            new("(O)PrizeTicket", LootCategory.Legendary, 2, 5, 15.0, c => c.EnableLegendaryCategory && c.EnablePrizeTicket),

            // =========================================================================
            // === 2. AGRICULTURE CATEGORY (15% Category Weight)                     ===
            // =========================================================================
            new("(O)918", LootCategory.Agriculture, 10, 25, 20.0, c => c.EnableAgricultureCategory && c.EnableFertilizers && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),     // Hyper Speed-Gro
            new("(O)919", LootCategory.Agriculture, 10, 25, 20.0, c => c.EnableAgricultureCategory && c.EnableFertilizers && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),     // Deluxe Fertilizer
            new("(O)347", LootCategory.Agriculture, 2, 6, 20.0, c => c.EnableAgricultureCategory && c.EnableRareSeeds),         // Rare Seed
            new("(O)486", LootCategory.Agriculture, 5, 15, 18.0, c => c.EnableAgricultureCategory && c.EnableRareSeeds),        // Starfruit Seeds
            new("(O)920", LootCategory.Agriculture, 10, 25, 18.0, c => c.EnableAgricultureCategory && c.EnableFertilizers && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),     // Deluxe Retaining Soil
            new("(O)645", LootCategory.Agriculture, 1, 3, 18.0, c => c.EnableAgricultureCategory && c.EnableSprinklers),        // Iridium Sprinkler
            new("(O)805", LootCategory.Agriculture, 10, 20, 15.0, c => c.EnableAgricultureCategory && c.EnableFertilizers),     // Tree Fertilizer
            new("(O)915", LootCategory.Agriculture, 1, 2, 12.0, c => c.EnableAgricultureCategory && c.EnableSprinklers && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),        // Pressure Nozzle
            new("(O)913", LootCategory.Agriculture, 1, 2, 12.0, c => c.EnableAgricultureCategory && c.EnableSprinklers && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),        // Enricher

            // =========================================================================
            // === 3. MINING CATEGORY (15% Category Weight)                          ===
            // =========================================================================
            new("(O)386", LootCategory.Mining, 10, 25, 25.0, c => c.EnableMiningCategory && c.EnableIridiumItems),       // Iridium Ore
            new("(O)288", LootCategory.Mining, 5, 15, 25.0, c => c.EnableMiningCategory && c.EnableBombs),               // Mega Bomb
            new("(O)909", LootCategory.Mining, 5, 15, 22.0, c => c.EnableMiningCategory && c.EnableRadioactiveItems && (!c.GatekeepRadioactiveItems || ProgressionHelper.IsQiRoomUnlocked())),    // Radioactive Ore
            new("(O)337", LootCategory.Mining, 3, 10, 22.0, c => c.EnableMiningCategory && c.EnableIridiumItems),       // Iridium Bar
            new("(O)910", LootCategory.Mining, 2, 6, 20.0, c => c.EnableMiningCategory && c.EnableRadioactiveItems && (!c.GatekeepRadioactiveItems || ProgressionHelper.IsQiRoomUnlocked())),     // Radioactive Bar
            new("(O)848", LootCategory.Mining, 6, 16, 20.0, c => c.EnableMiningCategory && (!c.GatekeepIslandItems || ProgressionHelper.IsIslandUnlocked())),                                // Cinder Shard
            new("(O)70", LootCategory.Mining, 3, 8, 20.0, c => c.EnableMiningCategory),                                  // Jade (Staircases)
            new("(O)72", LootCategory.Mining, 3, 8, 18.0, c => c.EnableMiningCategory),                                  // Diamond

            // =========================================================================
            // === 4. FISHING CATEGORY (15% Category Weight)                         ===
            // =========================================================================
            new("(O)ChallengeBait", LootCategory.Fishing, 15, 35, 22.0, c => c.EnableFishingCategory && c.EnableFishingTackle && (!c.GatekeepMasteryItems || ProgressionHelper.IsMasteryUnlocked("Fishing"))),
            new("(O)DeluxeBait", LootCategory.Fishing, 20, 40, 22.0, c => c.EnableFishingCategory && c.EnableFishingTackle),
            new("(O)908", LootCategory.Fishing, 10, 25, 20.0, c => c.EnableFishingCategory && c.EnableFishingTackle && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),  // Magic Bait
            new("(O)694", LootCategory.Fishing, 1, 3, 18.0, c => c.EnableFishingCategory && c.EnableFishingTackle),  // Trap Bobber
            new("(O)856", LootCategory.Fishing, 1, 2, 16.0, c => c.EnableFishingCategory && c.EnableFishingTackle),  // Curiosity Lure
            new("(O)SeaJelly", LootCategory.Fishing, 1, 3, 16.0, c => c.EnableFishingCategory),                      // Sea Jelly
            new("(O)RiverJelly", LootCategory.Fishing, 1, 3, 16.0, c => c.EnableFishingCategory),                    // River Jelly
            new("(O)CaveJelly", LootCategory.Fishing, 1, 3, 16.0, c => c.EnableFishingCategory),                     // Cave Jelly
            new("(O)265", LootCategory.Fishing, 2, 5, 15.0, c => c.EnableFishingCategory),                            // Seafoam Pudding
            new("(O)242", LootCategory.Fishing, 2, 6, 15.0, c => c.EnableFishingCategory),                            // Dish O' The Sea

            // =========================================================================
            // === 5. COMBAT CATEGORY (15% Category Weight)                          ===
            // =========================================================================
            new("(O)773", LootCategory.Combat, 3, 8, 22.0, c => c.EnableCombatCategory && c.EnableCombatConsumables),   // Life Elixir
            new("(O)253", LootCategory.Combat, 3, 10, 22.0, c => c.EnableCombatCategory && c.EnableCombatConsumables),   // Triple Shot Espresso
            new("(O)852", LootCategory.Combat, 2, 5, 20.0, c => c.EnableCombatCategory && (!c.GatekeepIslandItems || ProgressionHelper.IsIslandUnlocked())),                                // Dragon Tooth
            new("(O)872", LootCategory.Combat, 2, 5, 20.0, c => c.EnableCombatCategory && (!c.GatekeepIslandItems || ProgressionHelper.IsIslandUnlocked())),                                // Fairy Dust
            new("(O)879", LootCategory.Combat, 2, 5, 18.0, c => c.EnableCombatCategory),                                // Monster Musk
            new("(O)857", LootCategory.Combat, 1, 2, 15.0, c => c.EnableCombatCategory && c.EnableSlimeEggs && (!c.GatekeepIslandItems || ProgressionHelper.IsIslandUnlocked()), false),    // Tiger Slime Egg
            new("(O)439", LootCategory.Combat, 1, 2, 15.0, c => c.EnableCombatCategory && c.EnableSlimeEggs, false),    // Purple Slime Egg

            // =========================================================================
            // === 6. FORAGING CATEGORY (15% Category Weight)                        ===
            // =========================================================================
            new("(O)709", LootCategory.Foraging, 15, 40, 22.0, c => c.EnableForagingCategory),                        // Hardwood
            new("(O)MysticTreeSeed", LootCategory.Foraging, 2, 6, 22.0, c => c.EnableForagingCategory && (!c.GatekeepMasteryItems || ProgressionHelper.IsMasteryUnlocked("Foraging"))), // Mystic Tree Seed
            new("(O)791", LootCategory.Foraging, 2, 6, 22.0, c => c.EnableForagingCategory && (!c.GatekeepIslandItems || ProgressionHelper.IsIslandUnlocked())),                            // Golden Coconut
            new("(O)851", LootCategory.Foraging, 3, 8, 20.0, c => c.EnableForagingCategory && (!c.GatekeepIslandItems || ProgressionHelper.IsIslandUnlocked())),                            // Magma Cap
            new("(O)422", LootCategory.Foraging, 5, 12, 20.0, c => c.EnableForagingCategory),                           // Purple Mushroom
            new("(O)261", LootCategory.Foraging, 3, 8, 20.0, c => c.EnableForagingCategory && c.EnableWarpTotems),     // Warp Totem: Desert
            new("(O)688", LootCategory.Foraging, 3, 8, 20.0, c => c.EnableForagingCategory && c.EnableWarpTotems),     // Warp Totem: Farm

            // =========================================================================
            // === 7. LOOTBOXES CATEGORY (15% Category Weight)                       ===
            // =========================================================================
            new("(O)749", LootCategory.Lootboxes, 10, 25, 28.0, c => c.EnableLootboxCategory && c.EnableOmniGeodes),    // Omni Geode
            new("(O)MysteryBox", LootCategory.Lootboxes, 3, 10, 25.0, c => c.EnableLootboxCategory && c.EnableMysteryBoxes && (!c.GatekeepMysteryBoxes || ProgressionHelper.IsMysteryBoxUnlocked())),
            new("(O)275", LootCategory.Lootboxes, 3, 10, 25.0, c => c.EnableLootboxCategory && c.EnableArtifactTroves), // Artifact Trove
            new("(O)GoldenMysteryBox", LootCategory.Lootboxes, 2, 5, 22.0, c => c.EnableLootboxCategory && c.EnableMysteryBoxes && (!c.GatekeepMasteryItems || ProgressionHelper.IsMasteryUnlocked("Combat")) && (!c.GatekeepMysteryBoxes || ProgressionHelper.IsMysteryBoxUnlocked())),
            new("(O)CalicoEgg", LootCategory.Lootboxes, 15, 40, 22.0, c => c.EnableLootboxCategory && c.EnableCalicoEggs && (!c.GatekeepCalicoEggs || ProgressionHelper.IsDesertFestivalActive())),
            new("(O)TreasureTotem", LootCategory.Lootboxes, 1, 3, 18.0, c => c.EnableLootboxCategory && (!c.GatekeepMasteryItems || ProgressionHelper.IsMasteryUnlocked("Foraging"))),
        };

        public static List<Item> GenerateRewards(ModConfig config, Random random, bool isSpecialChest = false, int mineLevel = 121)
        {
            var results = new List<Item>();
            bool applySpecialBuff = isSpecialChest && config.EnableFloor100Buff;
            int relativeDepth = Math.Max(1, mineLevel > 120 ? mineLevel - 120 : mineLevel);
            bool isShallowFloor = config.EnableDepthScaling && !applySpecialBuff && relativeDepth < 50;

            // Organize eligible items into category buckets
            var categoryPools = new Dictionary<LootCategory, (List<RewardEntry> Entries, double TotalWeight)>();
            foreach (LootCategory cat in Enum.GetValues(typeof(LootCategory)))
            {
                categoryPools[cat] = (new List<RewardEntry>(), 0);
            }

            foreach (var entry in RewardPool)
            {
                if (entry.IsEnabled(config))
                {
                    var pool = categoryPools[entry.Category];
                    pool.Entries.Add(entry);
                    pool.TotalWeight += entry.Weight;
                    categoryPools[entry.Category] = pool;
                }
            }

            // Determine category weights (equal weights if floor 100 buff with AllCategoriesEqual enabled)
            bool equalCategories = applySpecialBuff && config.Floor100AllCategoriesEqual;
            double legWeight = equalCategories ? 15.0 : config.LegendaryWeight;
            double agrWeight = equalCategories ? 15.0 : config.AgricultureWeight;
            double minWeight = equalCategories ? 15.0 : config.MiningWeight;
            double fisWeight = equalCategories ? 15.0 : config.FishingWeight;
            double comWeight = equalCategories ? 15.0 : config.CombatWeight;
            double forWeight = equalCategories ? 15.0 : config.ForagingWeight;
            double looWeight = equalCategories ? 15.0 : config.LootboxWeight;

            // Scale Legendary weight linearly with depth if enabled (low at lower floors, full at floor 100)
            if (!applySpecialBuff && (config.ScaleLegendaryByDepth || config.EnableDepthScaling))
            {
                // Linear scaling from floor 1 (10% of base weight) to floor 100 (100% of base weight)
                double depthFactor = Math.Clamp(0.10 + 0.90 * ((relativeDepth - 1.0) / 99.0), 0.10, 1.0);
                legWeight *= depthFactor;
            }

            // Build active category list
            var activeCategories = new List<(LootCategory Category, double Weight)>();
            if (categoryPools[LootCategory.Legendary].Entries.Count > 0 && legWeight > 0)
                activeCategories.Add((LootCategory.Legendary, legWeight));
            if (categoryPools[LootCategory.Agriculture].Entries.Count > 0 && agrWeight > 0)
                activeCategories.Add((LootCategory.Agriculture, agrWeight));
            if (categoryPools[LootCategory.Mining].Entries.Count > 0 && minWeight > 0)
                activeCategories.Add((LootCategory.Mining, minWeight));
            if (categoryPools[LootCategory.Fishing].Entries.Count > 0 && fisWeight > 0)
                activeCategories.Add((LootCategory.Fishing, fisWeight));
            if (categoryPools[LootCategory.Combat].Entries.Count > 0 && comWeight > 0)
                activeCategories.Add((LootCategory.Combat, comWeight));
            if (categoryPools[LootCategory.Foraging].Entries.Count > 0 && forWeight > 0)
                activeCategories.Add((LootCategory.Foraging, forWeight));
            if (categoryPools[LootCategory.Lootboxes].Entries.Count > 0 && looWeight > 0)
                activeCategories.Add((LootCategory.Lootboxes, looWeight));

            double totalCatWeight = 0;
            foreach (var c in activeCategories)
                totalCatWeight += c.Weight;

            if (activeCategories.Count == 0 || totalCatWeight <= 0)
                return results;

            // Determine number of rolls using diminishing probabilities
            int maxRolls;
            float[] decayChances;

            if (applySpecialBuff)
            {
                maxRolls = config.Floor100MaxRolls;
                decayChances = new[]
                {
                    1.0f,
                    config.Floor100Roll2Chance,
                    config.Floor100Roll3Chance,
                    config.Floor100Roll4Chance,
                    config.Floor100Roll5Chance,
                    config.Floor100Roll6Chance,
                    config.Floor100Roll7Chance,
                    config.Floor100Roll8Chance,
                    config.Floor100Roll9Chance,
                    config.Floor100Roll10Chance,
                    config.Floor100Roll11Chance,
                    config.Floor100Roll12Chance
                };
            }
            else if (isShallowFloor)
            {
                // Shallow floor depth scaling (Floors 1-49: capped at 3 rolls with scaled chances)
                maxRolls = Math.Min(config.MaxRolls, 3);
                decayChances = new[]
                {
                    1.0f,
                    config.Roll2Chance * 0.75f,
                    config.Roll3Chance * 0.50f
                };
            }
            else
            {
                maxRolls = config.MaxRolls;
                decayChances = new[]
                {
                    1.0f,
                    config.Roll2Chance,
                    config.Roll3Chance,
                    config.Roll4Chance,
                    config.Roll5Chance,
                    config.Roll6Chance
                };
            }

            int rolls = 1; // 1st roll is 100% guaranteed
            for (int r = 1; r < maxRolls; r++)
            {
                if (r < decayChances.Length && random.NextDouble() < decayChances[r])
                {
                    rolls++;
                }
                else
                {
                    break;
                }
            }

            // Stack multiplier rates
            float x5Chance;
            float x4Chance;
            float x3Chance;
            float x2Chance;

            if (applySpecialBuff)
            {
                x5Chance = config.Floor100QuintupleStackChance;
                x4Chance = config.Floor100QuadrupleStackChance;
                x3Chance = config.Floor100TripleStackChance;
                x2Chance = config.Floor100DoubleStackChance;
            }
            else if (isShallowFloor)
            {
                // Shallow floors limit stack multiplier jackpot up to 2x
                x5Chance = 0f;
                x4Chance = 0f;
                x3Chance = 0f;
                x2Chance = config.DoubleStackChance;
            }
            else
            {
                x5Chance = config.QuintupleStackChance;
                x4Chance = config.QuadrupleStackChance;
                x3Chance = config.TripleStackChance;
                x2Chance = config.DoubleStackChance;
            }

            // Roll each item
            for (int i = 0; i < rolls; i++)
            {
                // 1. Select Category
                double catRoll = random.NextDouble() * totalCatWeight;
                double cumCat = 0;
                LootCategory selectedCategory = activeCategories[0].Category;

                foreach (var c in activeCategories)
                {
                    cumCat += c.Weight;
                    if (catRoll <= cumCat)
                    {
                        selectedCategory = c.Category;
                        break;
                    }
                }

                // 2. Select Item from chosen Category
                var catData = categoryPools[selectedCategory];
                if (catData.Entries.Count == 0 || catData.TotalWeight <= 0)
                    continue;

                double itemRoll = random.NextDouble() * catData.TotalWeight;
                double cumItem = 0;
                RewardEntry selectedItem = catData.Entries[0];

                foreach (var entry in catData.Entries)
                {
                    cumItem += entry.Weight;
                    if (itemRoll <= cumItem)
                    {
                        selectedItem = entry;
                        break;
                    }
                }

                // 3. Determine Stack Size and Stack Multipliers
                int stack = selectedItem.MinCount;
                if (selectedItem.MaxCount > selectedItem.MinCount)
                {
                    stack += random.Next(selectedItem.MaxCount - selectedItem.MinCount + 1);
                }

                if (selectedItem.AllowMultiplier)
                {
                    double multRoll = random.NextDouble();
                    if (multRoll < x5Chance)
                    {
                        stack *= 5;
                    }
                    else if (multRoll < x5Chance + x4Chance)
                    {
                        stack *= 4;
                    }
                    else if (multRoll < x5Chance + x4Chance + x3Chance)
                    {
                        stack *= 3;
                    }
                    else if (multRoll < x5Chance + x4Chance + x3Chance + x2Chance)
                    {
                        stack *= 2;
                    }
                }

                // 4. Create and validate item
                Item? item = ItemRegistry.Create(selectedItem.QualifiedItemId, stack, allowNull: true);
                if (item != null && item.ItemId != "Error" && item.QualifiedItemId != "(O)Error")
                {
                    results.Add(item);
                }
            }

            return results;
        }

        public static bool IsCosmeticItem(Item item)
        {
            if (item is Clothing || item is Hat)
                return true;

            if (item is Furniture furniture && furniture.furniture_type.Value == Furniture.decor)
                return true;

            return false;
        }
    }
}