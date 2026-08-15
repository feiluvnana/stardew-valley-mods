using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Objects;

namespace BetterSkullCavernChest
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
            // === 1. LEGENDARY CATEGORY (10% Category Weight)                       ===
            // =========================================================================
            new("(O)74", LootCategory.Legendary, 1, 3, 25.0, c => c.EnableLegendaryCategory && c.EnablePrismaticShard),
            new("(O)279", LootCategory.Legendary, 1, 2, 20.0, c => c.EnableLegendaryCategory && c.EnableMagicRockCandy),
            new("(O)GoldenAnimalCracker", LootCategory.Legendary, 1, 1, 20.0, c => c.EnableLegendaryCategory && c.EnableGoldenAnimalCracker, false),
            new("(BC)272", LootCategory.Legendary, 1, 1, 20.0, c => c.EnableLegendaryCategory && c.EnableAutoPetter, false), // Auto-Petter
            new("(O)896", LootCategory.Legendary, 1, 1, 15.0, c => c.EnableLegendaryCategory && c.EnableGalaxySoul, false),  // Galaxy Soul
            new("(O)StardropTea", LootCategory.Legendary, 1, 2, 15.0, c => c.EnableLegendaryCategory && c.EnableStardropTea),
            new("(O)PrizeTicket", LootCategory.Legendary, 2, 5, 15.0, c => c.EnableLegendaryCategory && c.EnablePrizeTicket),

            // =========================================================================
            // === 2. AGRICULTURE CATEGORY (15% Category Weight)                     ===
            // =========================================================================
            new("(O)918", LootCategory.Agriculture, 10, 30, 20.0, c => c.EnableAgricultureCategory && c.EnableFertilizers),     // Hyper Speed-Gro
            new("(O)919", LootCategory.Agriculture, 10, 30, 20.0, c => c.EnableAgricultureCategory && c.EnableFertilizers),     // Deluxe Fertilizer
            new("(O)347", LootCategory.Agriculture, 2, 6, 20.0, c => c.EnableAgricultureCategory && c.EnableRareSeeds),         // Rare Seed
            new("(O)486", LootCategory.Agriculture, 5, 20, 18.0, c => c.EnableAgricultureCategory && c.EnableRareSeeds),        // Starfruit Seeds
            new("(O)920", LootCategory.Agriculture, 10, 30, 18.0, c => c.EnableAgricultureCategory && c.EnableFertilizers),     // Deluxe Retaining Soil
            new("(O)645", LootCategory.Agriculture, 1, 3, 18.0, c => c.EnableAgricultureCategory && c.EnableSprinklers),        // Iridium Sprinkler
            new("(O)805", LootCategory.Agriculture, 10, 25, 15.0, c => c.EnableAgricultureCategory && c.EnableFertilizers),     // Tree Fertilizer
            new("(O)915", LootCategory.Agriculture, 1, 2, 12.0, c => c.EnableAgricultureCategory && c.EnableSprinklers),        // Pressure Nozzle
            new("(O)913", LootCategory.Agriculture, 1, 2, 12.0, c => c.EnableAgricultureCategory && c.EnableSprinklers),        // Enricher

            // =========================================================================
            // === 3. MINING CATEGORY (15% Category Weight)                          ===
            // =========================================================================
            new("(O)386", LootCategory.Mining, 10, 30, 25.0, c => c.EnableMiningCategory && c.EnableIridiumItems),       // Iridium Ore
            new("(O)288", LootCategory.Mining, 5, 20, 25.0, c => c.EnableMiningCategory && c.EnableBombs),               // Mega Bomb
            new("(O)909", LootCategory.Mining, 5, 20, 22.0, c => c.EnableMiningCategory && c.EnableRadioactiveItems && (!c.GatekeepRadioactiveItems || IsRadioactiveUnlocked())),    // Radioactive Ore
            new("(O)337", LootCategory.Mining, 5, 15, 22.0, c => c.EnableMiningCategory && c.EnableIridiumItems),       // Iridium Bar
            new("(O)910", LootCategory.Mining, 3, 8, 20.0, c => c.EnableMiningCategory && c.EnableRadioactiveItems && (!c.GatekeepRadioactiveItems || IsRadioactiveUnlocked())),     // Radioactive Bar
            new("(O)848", LootCategory.Mining, 5, 20, 20.0, c => c.EnableMiningCategory),                                // Cinder Shard
            new("(O)70", LootCategory.Mining, 3, 8, 20.0, c => c.EnableMiningCategory),                                  // Jade (Staircases)
            new("(O)72", LootCategory.Mining, 3, 8, 18.0, c => c.EnableMiningCategory),                                  // Diamond

            // =========================================================================
            // === 4. FISHING CATEGORY (15% Category Weight)                         ===
            // =========================================================================
            new("(O)ChallengeBait", LootCategory.Fishing, 15, 45, 22.0, c => c.EnableFishingCategory && c.EnableFishingTackle),
            new("(O)DeluxeBait", LootCategory.Fishing, 20, 50, 22.0, c => c.EnableFishingCategory && c.EnableFishingTackle),
            new("(O)908", LootCategory.Fishing, 10, 30, 20.0, c => c.EnableFishingCategory && c.EnableFishingTackle),  // Magic Bait
            new("(O)694", LootCategory.Fishing, 1, 3, 18.0, c => c.EnableFishingCategory && c.EnableFishingTackle),  // Trap Bobber
            new("(O)856", LootCategory.Fishing, 1, 2, 16.0, c => c.EnableFishingCategory && c.EnableFishingTackle),  // Curiosity Lure
            new("(O)SeaJelly", LootCategory.Fishing, 1, 3, 16.0, c => c.EnableFishingCategory),                      // Sea Jelly
            new("(O)RiverJelly", LootCategory.Fishing, 1, 3, 16.0, c => c.EnableFishingCategory),                    // River Jelly
            new("(O)CaveJelly", LootCategory.Fishing, 1, 3, 16.0, c => c.EnableFishingCategory),                     // Cave Jelly
            new("(O)265", LootCategory.Fishing, 2, 5, 15.0, c => c.EnableFishingCategory),                            // Seafoam Pudding
            new("(O)242", LootCategory.Fishing, 3, 6, 15.0, c => c.EnableFishingCategory),                            // Dish O' The Sea

            // =========================================================================
            // === 5. COMBAT CATEGORY (15% Category Weight)                          ===
            // =========================================================================
            new("(O)773", LootCategory.Combat, 3, 6, 22.0, c => c.EnableCombatCategory && c.EnableCombatConsumables),   // Life Elixir
            new("(O)253", LootCategory.Combat, 3, 8, 22.0, c => c.EnableCombatCategory && c.EnableCombatConsumables),   // Triple Shot Espresso
            new("(O)852", LootCategory.Combat, 2, 5, 20.0, c => c.EnableCombatCategory),                                // Dragon Tooth
            new("(O)872", LootCategory.Combat, 2, 5, 20.0, c => c.EnableCombatCategory),                                // Fairy Dust
            new("(O)879", LootCategory.Combat, 2, 5, 18.0, c => c.EnableCombatCategory),                                // Monster Musk
            new("(O)857", LootCategory.Combat, 1, 1, 15.0, c => c.EnableCombatCategory && c.EnableSlimeEggs, false),    // Tiger Slime Egg
            new("(O)439", LootCategory.Combat, 1, 1, 15.0, c => c.EnableCombatCategory && c.EnableSlimeEggs, false),    // Purple Slime Egg

            // =========================================================================
            // === 6. FORAGING CATEGORY (15% Category Weight)                        ===
            // =========================================================================
            new("(O)709", LootCategory.Foraging, 20, 50, 22.0, c => c.EnableForagingCategory),                        // Hardwood
            new("(O)MysticTreeSeed", LootCategory.Foraging, 2, 6, 22.0, c => c.EnableForagingCategory),                  // Mystic Tree Seed
            new("(O)791", LootCategory.Foraging, 2, 6, 22.0, c => c.EnableForagingCategory),                            // Golden Coconut
            new("(O)851", LootCategory.Foraging, 3, 8, 20.0, c => c.EnableForagingCategory),                            // Magma Cap
            new("(O)422", LootCategory.Foraging, 5, 15, 20.0, c => c.EnableForagingCategory),                           // Purple Mushroom
            new("(O)261", LootCategory.Foraging, 3, 8, 20.0, c => c.EnableForagingCategory && c.EnableWarpTotems),     // Warp Totem: Desert
            new("(O)688", LootCategory.Foraging, 3, 8, 20.0, c => c.EnableForagingCategory && c.EnableWarpTotems),     // Warp Totem: Farm

            // =========================================================================
            // === 7. LOOTBOXES CATEGORY (15% Category Weight)                       ===
            // =========================================================================
            new("(O)749", LootCategory.Lootboxes, 10, 30, 28.0, c => c.EnableLootboxCategory && c.EnableOmniGeodes),    // Omni Geode
            new("(O)MysteryBox", LootCategory.Lootboxes, 3, 10, 25.0, c => c.EnableLootboxCategory && c.EnableMysteryBoxes),
            new("(O)275", LootCategory.Lootboxes, 3, 10, 25.0, c => c.EnableLootboxCategory && c.EnableArtifactTroves), // Artifact Trove
            new("(O)GoldenMysteryBox", LootCategory.Lootboxes, 2, 5, 22.0, c => c.EnableLootboxCategory && c.EnableMysteryBoxes),
            new("(O)CalicoEgg", LootCategory.Lootboxes, 15, 50, 22.0, c => c.EnableLootboxCategory && c.EnableCalicoEggs),
            new("(O)TreasureTotem", LootCategory.Lootboxes, 1, 3, 18.0, c => c.EnableLootboxCategory),
        };

        public static List<Item> GenerateRewards(ModConfig config, Random random, bool isSpecialChest = false)
        {
            var results = new List<Item>();
            bool applySpecialBuff = isSpecialChest && config.EnableFloor100Buff;

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
            int maxRolls = applySpecialBuff ? config.Floor100MaxRolls : config.MaxRolls;
            float[] decayChances = applySpecialBuff
                ? new[] { 1.0f, config.Floor100Roll2Chance, config.Floor100Roll3Chance, config.Floor100Roll4Chance, config.Floor100Roll5Chance, config.Floor100Roll6Chance, config.Floor100Roll7Chance, config.Floor100Roll8Chance, config.Floor100Roll9Chance, config.Floor100Roll10Chance }
                : new[] { 1.0f, config.Roll2Chance, config.Roll3Chance, config.Roll4Chance, config.Roll5Chance };

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
            float x5Chance = applySpecialBuff ? config.Floor100QuintupleStackChance : config.QuintupleStackChance;
            float x4Chance = applySpecialBuff ? config.Floor100QuadrupleStackChance : config.QuadrupleStackChance;
            float x3Chance = applySpecialBuff ? config.Floor100TripleStackChance : config.TripleStackChance;
            float x2Chance = applySpecialBuff ? config.Floor100DoubleStackChance : config.DoubleStackChance;

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

        public static bool IsRadioactiveUnlocked()
        {
            if (Game1.netWorldState?.Value != null && Game1.netWorldState.Value.GoldenWalnuts >= 100)
                return true;

            if (Game1.player != null)
            {
                if (Game1.player.hasOrWillReceiveMail("QiNutDoor") ||
                    (Game1.player.team != null && Game1.player.team.SpecialOrderRuleActive("MineConditionsLocked")) ||
                    Game1.player.stats.Get("WalnutsCollected") >= 100)
                {
                    return true;
                }
            }

            return false;
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