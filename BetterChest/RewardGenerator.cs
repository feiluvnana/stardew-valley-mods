// "using" directives import other libraries' namespaces so short names work:
//   StardewValley         -> core game code: Item, ItemRegistry, Game1
//   StardewValley.Objects -> Furniture, checked by IsCosmeticItem below
// Func<>, List<>, Dictionary<> and Random come from .NET core namespaces that
// modern projects import implicitly (ImplicitUsings).
using StardewValley;
using StardewValley.Objects;

// ============================================================================
// RewardGenerator is the BRAIN of the Skull Cavern overhaul: it owns the full
// loot table (RewardPool) and turns one chest opening into concrete items via
// a two-stage weighted lottery — first pick a CATEGORY (Legendary, Mining...),
// then pick an ITEM inside that category, then roll the stack size and any
// jackpot multipliers (x2..x5). ChestPatches calls GenerateRewards once per
// player per chest; config toggles and ProgressionHelper gates filter the
// table down to eligible entries first.
// Key concepts demonstrated: enums, Func<T,TResult> predicate delegates,
// value tuples inside a Dictionary, and cumulative-weight probability rolls.
// ============================================================================
namespace BetterChest
{
    // C# concept — ENUM: a fixed set of NAMED CONSTANTS backed by integers
    // (first member = 0, then 1, 2, ...). Writing LootCategory.Mining beats a
    // magic number like 2 — typos become compile errors instead of silent bugs.
    /// <summary>
    /// The loot families a chest roll can draw from. Category choice and item choice
    /// are separate dice rolls, each weighted independently.
    /// </summary>
    public enum LootCategory
    {
        /// <summary>Ultra-rare endgame items (Prismatic Shard, Auto-Petter...).</summary>
        Legendary,
        /// <summary>Farming gear: fertilizers, rare seeds, sprinklers.</summary>
        Agriculture,
        /// <summary>Ores, bars, bombs and mining resources.</summary>
        Mining,
        /// <summary>Baits, tackle and fish-related foods.</summary>
        Fishing,
        /// <summary>Combat consumables and monster drops.</summary>
        Combat,
        /// <summary>Hardwood, mushrooms and foraged goods.</summary>
        Foraging,
        /// <summary>Geodes, mystery boxes and other "loot box" items.</summary>
        Lootboxes
    }

    /// <summary>
    /// One possible loot drop: which item, which category, stack range, weight,
    /// a predicate deciding whether it is currently allowed, and whether jackpot
    /// multipliers may apply to it.
    /// </summary>
    public class RewardEntry
    {
        /// <summary>The item's qualified game id, e.g. "(O)74" (Prismatic Shard) or "(BC)272" (Auto-Petter).</summary>
        public string QualifiedItemId { get; set; }
        /// <summary>Which category bucket this entry belongs to for the two-stage roll.</summary>
        public LootCategory Category { get; set; }
        /// <summary>Smallest base stack this entry can produce.</summary>
        public int MinCount { get; set; }
        /// <summary>Largest base stack this entry can produce.</summary>
        public int MaxCount { get; set; }
        /// <summary>Relative probability within its category — higher = more common.</summary>
        public double Weight { get; set; }
        /// <summary>
        /// A Func&lt;ModConfig, bool&gt; is a DELEGATE: a method stored in a variable.
        /// It takes the config and answers "is this entry allowed right now?"
        /// (checks toggles plus progression gates).
        /// </summary>
        public Func<ModConfig, bool> IsEnabled { get; set; }
        /// <summary>Whether x2..x5 stack multipliers are allowed to proc on this item.</summary>
        public bool AllowMultiplier { get; set; }

        /// <summary>
        /// Creates a loot table row. Called with positional arguments like
        /// <c>new("(O)74", LootCategory.Legendary, 1, 2, 25.0, c => ..., false)</c>.
        /// </summary>
        /// <param name="qualifiedItemId">Qualified item id to create.</param>
        /// <param name="category">Loot category bucket.</param>
        /// <param name="minCount">Minimum stack size.</param>
        /// <param name="maxCount">Maximum stack size.</param>
        /// <param name="weight">Relative selection weight.</param>
        /// <param name="isEnabled">Config/progression predicate controlling availability.</param>
        /// <param name="allowMultiplier">True to permit stack multiplier jackpots (default true).</param>
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
            // Multipliers only make sense on entries that already vary in stack size.
            AllowMultiplier = allowMultiplier && maxCount > 1;
        }
    }

    /// <summary>
    /// Static class holding the Skull Cavern loot table and the roll algorithm that
    /// converts config + progression state into a list of <see cref="Item"/> rewards.
    /// </summary>
    public static class RewardGenerator
    {
        // How to READ each row: new(itemId, category, minStack, maxStack, weight,
        // availabilityPredicate, allowMultipliers). The predicate argument, e.g.
        // "c => c.EnableLegendaryCategory && c.EnablePrismaticShard", is a LAMBDA
        // EXPRESSION — an anonymous function captured for later use. NOTHING in
        // it runs while this table is built; GenerateRewards INVOKES the stored
        // delegate ("entry.IsEnabled(config)") at roll time, so flipping a
        // config toggle or meeting a progression gate takes effect instantly
        // without rebuilding the table.
        /// <summary>
        /// The master loot table. Each row's lambda ("c => c.EnableX") is a predicate
        /// evaluated at roll time against the live config, so toggles/gates apply
        /// instantly without rebuilding the table.
        /// </summary>
        private static readonly List<RewardEntry> RewardPool = new()
        {
            // =========================================================================
            // === 1. LEGENDARY CATEGORY (15% Category Weight)                       ===
            // =========================================================================
            new("(O)74", LootCategory.Legendary, 1, 2, 25.0, c => c.EnableLegendaryCategory && c.EnablePrismaticShard, false),
            new("(O)279", LootCategory.Legendary, 1, 2, 20.0, c => c.EnableLegendaryCategory && c.EnableMagicRockCandy, false),
            new("(O)GoldenAnimalCracker", LootCategory.Legendary, 1, 2, 20.0, c => c.EnableLegendaryCategory && c.EnableGoldenAnimalCracker && (!c.GatekeepMasteryItems || ProgressionHelper.IsMasteryUnlocked("Farming")), false),
            new("(BC)272", LootCategory.Legendary, 1, 1, 20.0, c => c.EnableLegendaryCategory && c.EnableAutoPetter && (!c.GatekeepAutoPetter || ProgressionHelper.IsCommunityCenterCompleted()), false), // Auto-Petter
            new("(O)896", LootCategory.Legendary, 1, 2, 15.0, c => c.EnableLegendaryCategory && c.EnableGalaxySoul && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked()), false),  // Galaxy Soul
            new("(O)StardropTea", LootCategory.Legendary, 1, 3, 15.0, c => c.EnableLegendaryCategory && c.EnableStardropTea, false),
            new("(O)PrizeTicket", LootCategory.Legendary, 2, 4, 15.0, c => c.EnableLegendaryCategory && c.EnablePrizeTicket, false),

            // =========================================================================
            // === 2. AGRICULTURE CATEGORY (15% Category Weight)                     ===
            // =========================================================================
            new("(O)918", LootCategory.Agriculture, 10, 25, 20.0, c => c.EnableAgricultureCategory && c.EnableFertilizers && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),     // Hyper Speed-Gro
            new("(O)919", LootCategory.Agriculture, 10, 25, 20.0, c => c.EnableAgricultureCategory && c.EnableFertilizers && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),     // Deluxe Fertilizer
            new("(O)347", LootCategory.Agriculture, 2, 6, 20.0, c => c.EnableAgricultureCategory && c.EnableRareSeeds),         // Rare Seed (Preserved)
            new("(O)486", LootCategory.Agriculture, 5, 15, 18.0, c => c.EnableAgricultureCategory && c.EnableRareSeeds),        // Starfruit Seeds
            new("(O)920", LootCategory.Agriculture, 10, 25, 18.0, c => c.EnableAgricultureCategory && c.EnableFertilizers && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),     // Deluxe Retaining Soil
            new("(O)645", LootCategory.Agriculture, 1, 2, 18.0, c => c.EnableAgricultureCategory && c.EnableSprinklers, false), // Iridium Sprinkler (No Mult)
            new("(O)805", LootCategory.Agriculture, 10, 20, 15.0, c => c.EnableAgricultureCategory && c.EnableFertilizers),     // Tree Fertilizer
            new("(O)915", LootCategory.Agriculture, 1, 2, 12.0, c => c.EnableAgricultureCategory && c.EnableSprinklers && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),        // Pressure Nozzle (Mult Allowed)
            new("(O)913", LootCategory.Agriculture, 1, 2, 12.0, c => c.EnableAgricultureCategory && c.EnableSprinklers && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),        // Enricher (Mult Allowed)

            // =========================================================================
            // === 3. MINING CATEGORY (15% Category Weight)                          ===
            // =========================================================================
            new("(O)386", LootCategory.Mining, 10, 25, 25.0, c => c.EnableMiningCategory && c.EnableIridiumItems),       // Iridium Ore
            new("(O)288", LootCategory.Mining, 5, 15, 25.0, c => c.EnableMiningCategory && c.EnableBombs),               // Mega Bomb
            new("(O)909", LootCategory.Mining, 5, 15, 22.0, c => c.EnableMiningCategory && c.EnableRadioactiveItems && (!c.GatekeepRadioactiveItems || ProgressionHelper.IsQiRoomUnlocked())),    // Radioactive Ore
            new("(O)337", LootCategory.Mining, 2, 6, 22.0, c => c.EnableMiningCategory && c.EnableIridiumItems),        // Iridium Bar (Balanced)
            new("(O)910", LootCategory.Mining, 2, 4, 20.0, c => c.EnableMiningCategory && c.EnableRadioactiveItems && (!c.GatekeepRadioactiveItems || ProgressionHelper.IsQiRoomUnlocked())),     // Radioactive Bar (Balanced)
            new("(O)382", LootCategory.Mining, 35, 90, 24.0, c => c.EnableMiningCategory && c.EnableCoal),               // Coal (New)
            new("(O)848", LootCategory.Mining, 6, 16, 20.0, c => c.EnableMiningCategory && (!c.GatekeepIslandItems || ProgressionHelper.IsIslandUnlocked())),                                // Cinder Shard
            new("(O)70", LootCategory.Mining, 3, 8, 20.0, c => c.EnableMiningCategory),                                  // Jade (Staircases)
            new("(O)72", LootCategory.Mining, 3, 8, 18.0, c => c.EnableMiningCategory),                                  // Diamond

            // =========================================================================
            // === 4. FISHING CATEGORY (15% Category Weight)                         ===
            // =========================================================================
            new("(O)ChallengeBait", LootCategory.Fishing, 15, 35, 22.0, c => c.EnableFishingCategory && c.EnableFishingTackle && (!c.GatekeepMasteryItems || ProgressionHelper.IsMasteryUnlocked("Fishing"))),
            new("(O)DeluxeBait", LootCategory.Fishing, 20, 40, 22.0, c => c.EnableFishingCategory && c.EnableFishingTackle),
            new("(O)908", LootCategory.Fishing, 10, 25, 20.0, c => c.EnableFishingCategory && c.EnableFishingTackle && (!c.GatekeepQiItems || ProgressionHelper.IsQiRoomUnlocked())),  // Magic Bait
            new("(O)694", LootCategory.Fishing, 1, 3, 18.0, c => c.EnableFishingCategory && c.EnableFishingTackle),  // Trap Bobber (Stackable & Mult)
            new("(O)856", LootCategory.Fishing, 1, 2, 16.0, c => c.EnableFishingCategory && c.EnableFishingTackle),  // Curiosity Lure (Stackable & Mult)
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
            new("(O)709", LootCategory.Foraging, 30, 80, 24.0, c => c.EnableForagingCategory && c.EnableHardwood),    // Hardwood (Buffed)
            new("(O)MysticTreeSeed", LootCategory.Foraging, 2, 6, 22.0, c => c.EnableForagingCategory && (!c.GatekeepMasteryItems || ProgressionHelper.IsMasteryUnlocked("Foraging"))), // Mystic Tree Seed
            new("(O)791", LootCategory.Foraging, 2, 6, 22.0, c => c.EnableForagingCategory && (!c.GatekeepIslandItems || ProgressionHelper.IsIslandUnlocked())),                            // Golden Coconut
            new("(O)851", LootCategory.Foraging, 3, 8, 20.0, c => c.EnableForagingCategory && (!c.GatekeepIslandItems || ProgressionHelper.IsIslandUnlocked())),                            // Magma Cap
            new("(O)422", LootCategory.Foraging, 5, 12, 20.0, c => c.EnableForagingCategory),                           // Purple Mushroom

            // =========================================================================
            // === 7. LOOTBOXES CATEGORY (15% Category Weight)                       ===
            // =========================================================================
            new("(O)749", LootCategory.Lootboxes, 10, 25, 28.0, c => c.EnableLootboxCategory && c.EnableOmniGeodes),    // Omni Geode
            new("(O)MysteryBox", LootCategory.Lootboxes, 3, 10, 25.0, c => c.EnableLootboxCategory && c.EnableMysteryBoxes && (!c.GatekeepMysteryBoxes || ProgressionHelper.IsMysteryBoxUnlocked())),
            new("(O)275", LootCategory.Lootboxes, 3, 10, 25.0, c => c.EnableLootboxCategory && c.EnableArtifactTroves), // Artifact Trove
            new("(O)GoldenMysteryBox", LootCategory.Lootboxes, 2, 5, 22.0, c => c.EnableLootboxCategory && c.EnableMysteryBoxes && (!c.GatekeepMasteryItems || ProgressionHelper.IsMasteryUnlocked("Combat")) && (!c.GatekeepMysteryBoxes || ProgressionHelper.IsMysteryBoxUnlocked())),
            new("(O)CalicoEgg", LootCategory.Lootboxes, 15, 40, 22.0, c => c.EnableLootboxCategory && c.EnableCalicoEggs && (!c.GatekeepCalicoEggs || ProgressionHelper.IsDesertFestivalActive())),
            new("(O)TreasureTotem", LootCategory.Lootboxes, 2, 5, 18.0, c => c.EnableLootboxCategory && (!c.GatekeepMasteryItems || ProgressionHelper.IsMasteryUnlocked("Foraging")), false), // Treasure Totem (Buffed base stack, No Mult)
        };

        // "static" method — call it as RewardGenerator.GenerateRewards(...) with
        // no object instance. Return type List<Item> is a GENERIC collection: a
        // resizable array that only accepts Stardew Valley Item references.
        /// <summary>
        /// Rolls a full chest's worth of rewards using the two-stage weighted lottery.
        /// </summary>
        /// <param name="config">Live mod settings (toggles, weights, chances).</param>
        /// <param name="random">Random number generator to use (usually Game1.random).</param>
        /// <param name="isSpecialChest">True for the milestone-floor special chests (220/320/420/520).</param>
        /// <param name="mineLevel">The floor the chest sits on; used for depth scaling.</param>
        /// <returns>The rolled items (may be empty if everything is gated off).</returns>
        public static List<Item> GenerateRewards(ModConfig config, Random random, bool isSpecialChest = false, int mineLevel = 121)
        {
            var results = new List<Item>();
            // Special chests only get their buff when the user hasn't disabled it.
            bool applySpecialBuff = isSpecialChest && config.EnableFloor100Buff;
            // "Relative depth": Skull Cavern floors count up from 1 above level 120.
            int relativeDepth = Math.Max(1, mineLevel > 120 ? mineLevel - 120 : mineLevel);
            bool isShallowFloor = config.EnableDepthScaling && !applySpecialBuff && relativeDepth < 50;

            // Organize eligible items into category buckets
            // Dictionary key = category; value = a TUPLE bundling the entry list with
            // its summed weight. Tuples are copies when read from a dictionary, so the
            // code below must write the tuple BACK after changing it.
            var categoryPools = new Dictionary<LootCategory, (List<RewardEntry> Entries, double TotalWeight)>();
            // Enum.GetValues(typeof(LootCategory)) uses reflection to fetch an
            // array of every enum member — one empty bucket per category, with
            // no hard-coded list to maintain.
            foreach (LootCategory cat in Enum.GetValues(typeof(LootCategory)))
            {
                categoryPools[cat] = (new List<RewardEntry>(), 0);
            }

            foreach (var entry in RewardPool)
            {
                // Invoke the stored predicate delegate — runs the config/progression checks.
                if (entry.IsEnabled(config))
                {
                    var pool = categoryPools[entry.Category];
                    pool.Entries.Add(entry);
                    pool.TotalWeight += entry.Weight;
                    categoryPools[entry.Category] = pool; // write the modified copy back
                }
            }

            // Determine category weights (equal weights if floor 100 buff with AllCategoriesEqual enabled)
            // Each line is a ternary: special buff + equal mode ? fixed 15 : config value.
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
                // Math.Clamp keeps the factor inside [0.10, 1.0] no matter the depth.
                double depthFactor = Math.Clamp(0.10 + 0.90 * ((relativeDepth - 1.0) / 99.0), 0.10, 1.0);
                legWeight *= depthFactor;
            }

            // Build active category list
            // A list of (enum, weight) tuples — only categories that still have eligible
            // items AND a positive weight make it in.
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

            // Sum every active category's weight — the "size of the whole pie".
            double totalCatWeight = 0;
            foreach (var c in activeCategories)
                totalCatWeight += c.Weight;

            // All categories empty or weightless: nothing can roll.
            if (activeCategories.Count == 0 || totalCatWeight <= 0)
                return results;

            // Determine number of rolls using diminishing probabilities & guaranteed minimums
            // These locals are DECLARED here but filled in exactly ONE of the
            // three branches below (special / shallow / standard chest types).
            int minRolls;
            int maxRolls;
            // decayChances[r] = chance the (r+1)th roll is granted once r rolls exist;
            // the chain STOPS at the first failure ("break" below).
            // C# concept — ARRAY: "float[]" is a fixed-length, zero-indexed list
            // of floats; decayChances[r] reads slot r directly.
            float[] decayChances;

            if (applySpecialBuff)
            {
                // Floor 100 Special Chest: Min 3 guaranteed rolls, up to Floor100MaxRolls (12)
                minRolls = 3;
                maxRolls = config.Floor100MaxRolls;
                decayChances = new[]
                {
                    1.0f,
                    1.0f,
                    1.0f,
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
                // Shallow floor depth scaling (Floors 1-49: Min 1 guaranteed roll, max 4 rolls)
                minRolls = 1;
                maxRolls = Math.Min(config.MaxRolls, 4);
                decayChances = new[]
                {
                    1.0f,
                    0.60f,
                    0.35f,
                    0.20f
                };
            }
            else
            {
                // Standard deep floors (Floors 50+: Min 2 guaranteed rolls, max 8 rolls)
                minRolls = 2;
                maxRolls = config.MaxRolls;
                decayChances = new[]
                {
                    1.0f,
                    1.0f,
                    config.Roll3Chance,
                    config.Roll4Chance,
                    config.Roll5Chance,
                    config.Roll6Chance,
                    config.Roll7Chance,
                    config.Roll8Chance
                };
            }

            // Start with the guaranteed minimum, then gamble for extra rolls one at a
            // time; the first failed chance check ends the chain entirely.
            int rolls = minRolls;
            for (int r = minRolls; r < maxRolls; r++)
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
                // Same weighted-dart technique as everywhere else: uniform number across
                // the total weight, then walk until the running total catches up.
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
                // C# note — "continue" abandons THIS loop iteration and jumps to
                // the next one ("break" would exit the loop entirely).
                // Category somehow empty (shouldn't happen) — skip to the next roll.
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
                    // Next(n) returns 0..n-1, so "+1" includes MaxCount itself.
                    stack += random.Next(selectedItem.MaxCount - selectedItem.MinCount + 1);
                }

                if (selectedItem.AllowMultiplier)
                {
                    // One roll tested against CUMULATIVE bands, so exactly one outcome wins:
                    // [0..x5) -> x5, [x5..x5+x4) -> x4, and so on down to the x2 band.
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
                // Reject nulls AND the game's placeholder "Error" items for unknown ids.
                if (item != null && item.ItemId != "Error" && item.QualifiedItemId != "(O)Error")
                {
                    results.Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// Detects "cosmetic" items (clothing, hats, decorative furniture) that the
        /// ExcludeCosmetics option strips from vanilla chests.
        /// </summary>
        /// <param name="item">The item to classify.</param>
        /// <returns>True if the item is clothing, a hat, or decor-type furniture.</returns>
        public static bool IsCosmeticItem(Item item)
        {
            // "is" type checks work on the object's actual runtime class.
            if (item is Clothing || item is Hat)
                return true;

            // Furniture needs a deeper check: only the "decor" furniture type counts.
            // furniture_type is a NetInt (synced value), so read it through ".Value".
            if (item is Furniture furniture && furniture.furniture_type.Value == Furniture.decor)
                return true;

            return false;
        }
    }
}
