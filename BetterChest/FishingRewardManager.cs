// "using" directives import other libraries' namespaces so short names work:
//   StardewValley       -> core game code (Game1.random, Item, ItemRegistry)
//   StardewValley.Menus -> ItemGrabMenu, the screen shown for opened chests
//   StardewValley.Tools -> FishingRod, the tool class that reeled the chest in
// HashSet<>, List<>, IList<> and Math come from .NET's core collections and
// numerics namespaces, imported implicitly in modern .NET projects.
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

// ============================================================================
// FishingRewardManager upgrades FISHING treasure chests (and the golden
// variant). Unlike the Skull Cavern system it does NOT replace the chest's
// contents — it ADDS bonus rolls on top of whatever vanilla put inside.
// Loot comes from a flat weighted table (FishingPool); "GoldenOnly" entries
// appear exclusively in golden chests, and each piece of trash already in the
// chest grants +1 extra roll (the "trash reroll bonus").
// Key concept demonstrated: weighted random selection using a running total,
// plus HashSet lookups and C# positional records.
// ============================================================================
namespace BetterChest
{
    /// <summary>
    /// Rolls supplementary loot into fishing treasure chest menus after the vanilla
    /// contents are decided (invoked by <see cref="FishingPatches"/>).
    /// </summary>
    public static class FishingRewardManager
    {
        // "static readonly" = constructed once when the class is first used and
        // never re-pointed afterwards. HashSet<string> is a GENERIC type — the
        // angle brackets declare that it stores strings. Its superpower is
        // O(1) Contains(): lookup speed stays constant no matter how many ids.
        /// <summary>
        /// Every item id the mod treats as "trash". A HashSet gives instant Contains()
        /// lookups, and OrdinalIgnoreCase tolerates case differences. Both qualified
        /// ("(O)168") and bare ("168") id forms are listed for game-version safety.
        /// </summary>
        private static readonly HashSet<string> TrashItemIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "(O)168", // Trash
            "(O)169", // Driftwood
            "(O)170", // Broken Glasses
            "(O)171", // Broken CD
            "(O)172", // Soggy Newspaper
            "(O)0",   // Weeds
            "(O)TrashCan", // Rotten Plant
            "168", "169", "170", "171", "172", "0", "TrashCan"
        };

        // C# concept — RECORD: a compact, immutable data-carrier type. This
        // "positional" form auto-generates a constructor accepting every
        // parameter in order, a read-only property per parameter, and sensible
        // value equality — all from a single line instead of a full class body.
        /// <summary>
        /// One row of the fishing loot table: which item, how many, how likely, and
        /// whether it is reserved for golden chests.
        /// </summary>
        /// <param name="QualifiedItemId">The item's qualified id, e.g. "(O)382" for Coal.</param>
        /// <param name="MinCount">Smallest stack size this entry can produce.</param>
        /// <param name="MaxCount">Largest stack size this entry can produce.</param>
        /// <param name="Weight">Relative probability — bigger numbers are picked more often.</param>
        /// <param name="GoldenOnly">When true, the entry can only roll in golden treasure chests (defaults to false).</param>
        private record FishingLootEntry(string QualifiedId, int MinCount, int MaxCount, double Weight, bool GoldenOnly = false);

        // Two shorthand features at work below:
        //   * "new()" with no type name is TARGET-TYPED NEW (C# 9): the compiler
        //     infers List<FishingLootEntry> from the field's declared type.
        //   * The brace block is a COLLECTION INITIALIZER — syntactic sugar that
        //     calls Add() once per element, and each bare "new(...)" likewise
        //     infers the FishingLootEntry record type.
        /// <summary>
        /// The full fishing loot table. Weights are relative, so a 25.0 weight is
        /// twice as likely as a 12.5 weight regardless of the total.
        /// </summary>
        private static readonly List<FishingLootEntry> FishingPool = new()
        {
            // Ores & Resources
            new("(O)382", 6, 18, 25.0),         // Coal
            new("(O)378", 5, 15, 25.0),         // Copper Ore
            new("(O)380", 5, 12, 22.0),         // Iron Ore
            new("(O)384", 4, 10, 18.0),         // Gold Ore
            new("(O)386", 2, 5, 12.0),          // Iridium Ore
            new("(O)388", 15, 40, 20.0),        // Wood (High count)
            new("(O)390", 15, 40, 20.0),        // Stone (High count)

            // Baits & Tackles
            new("(O)685", 10, 25, 25.0),        // Bait
            new("(O)DeluxeBait", 8, 20, 20.0),  // Deluxe Bait
            new("(O)774", 5, 12, 16.0),         // Wild Bait
            new("(O)703", 3, 8, 16.0),          // Magnet
            new("(O)694", 1, 2, 12.0),          // Trap Bobber
            new("(O)695", 1, 2, 10.0),          // Cork Bobber
            new("(O)693", 1, 1, 8.0),           // Dressed Spinner
            new("(O)856", 1, 1, 10.0),          // Curiosity Lure

            // Geodes & Mystery Boxes
            new("(O)535", 2, 5, 22.0),          // Geode
            new("(O)536", 2, 5, 20.0),          // Frozen Geode
            new("(O)537", 2, 5, 18.0),          // Magma Geode
            new("(O)749", 2, 6, 20.0),          // Omni Geode
            new("(O)MysteryBox", 1, 3, 16.0),   // Mystery Box
            new("(O)GoldenMysteryBox", 1, 2, 12.0, GoldenOnly: true), // Golden Mystery Box

            // Gems & Valuables
            new("(O)72", 1, 2, 14.0),           // Diamond
            new("(O)64", 1, 2, 14.0),           // Ruby
            new("(O)60", 1, 2, 14.0),           // Emerald
            new("(O)62", 1, 2, 14.0),           // Aquamarine
            new("(O)66", 1, 3, 16.0),           // Amethyst
            new("(O)70", 1, 3, 16.0),           // Jade
            new("(O)797", 1, 1, 8.0),           // Pearl
            new("(O)74", 1, 1, 3.0),            // Prismatic Shard (Rare)

            // Marine Jellies & Buff Foods
            new("(O)SeaJelly", 1, 2, 10.0),     // Sea Jelly
            new("(O)RiverJelly", 1, 2, 10.0),   // River Jelly
            new("(O)CaveJelly", 1, 2, 10.0),    // Cave Jelly
            new("(O)265", 1, 2, 12.0),          // Seafoam Pudding
            new("(O)242", 1, 2, 14.0),          // Dish O' The Sea
            new("(O)219", 1, 2, 14.0),          // Trout Soup
            new("(O)773", 1, 2, 12.0),          // Life Elixir

            // Rare Artifacts & Collectibles
            new("(O)107", 1, 1, 5.0),           // Dinosaur Egg
            new("(O)114", 1, 1, 6.0),           // Ancient Seed
            new("(O)GoldenAnimalCracker", 1, 1, 6.0, GoldenOnly: true), // Golden Animal Cracker
            new("(O)StardropTea", 1, 1, 5.0, GoldenOnly: true)          // Stardrop Tea
        };

        /// <summary>
        /// Adds the mod's bonus loot rolls to a fishing treasure chest menu.
        /// </summary>
        /// <param name="rod">The fishing rod that caught the chest (tells us if it is golden).</param>
        /// <param name="grabMenu">The ItemGrabMenu shown for the treasure chest.</param>
        public static void EnhanceFishingChest(FishingRod rod, ItemGrabMenu grabMenu)
        {
            // "?. " only walks into ItemsToGrabMenu if it isn't null, preventing a
            // NullReferenceException when the menu has no inventory attached yet.
            if (grabMenu.ItemsToGrabMenu?.actualInventory == null)
                return;

            // IList<Item> is an INTERFACE — a contract promising certain members
            // (Add, Count, an indexer...) without pinning down the concrete list
            // class. Coding against the interface lets this work with whatever
            // list implementation the game uses internally.
            IList<Item> inventory = grabMenu.ItemsToGrabMenu.actualInventory;
            bool isGolden = rod.goldenTreasure;
            Random random = Game1.random; // the game's shared, save-seeded RNG

            // 1. Determine target base rolls
            // Ternary "? :" picks config values based on chest type: condition ? a : b.
            int minRolls = isGolden ? ModEntry.Config.GoldenChestMinRolls : ModEntry.Config.FishingChestMinRolls;
            int maxRolls = isGolden ? ModEntry.Config.GoldenChestMaxRolls : ModEntry.Config.FishingChestMaxRolls;
            // Guard against bad config where max < min.
            if (maxRolls < minRolls)
                maxRolls = minRolls;

            // random.Next(min, max) EXCLUDES its upper bound, so "+1" makes max inclusive.
            int targetRolls = random.Next(minRolls, maxRolls + 1);

            // 2. Count trash / trivial items for the guarantee reroll bonus (+1 roll per trash)
            int trashCount = 0;
            if (ModEntry.Config.EnableFishingTrashRerollBonus)
            {
                foreach (var item in inventory)
                {
                    if (item != null && IsTrashOrTrivial(item))
                    {
                        trashCount++;
                    }
                }
            }

            // Never exceed 12 total bonus items no matter how much trash was present.
            int finalDesiredCount = Math.Min(12, targetRolls + trashCount);

            // 3. Build eligible fishing pool
            var eligiblePool = new List<FishingLootEntry>();
            double totalWeight = 0;
            foreach (var entry in FishingPool)
            {
                // Skip "GoldenOnly" entries unless this is a golden chest.
                if (!entry.GoldenOnly || isGolden)
                {
                    eligiblePool.Add(entry);
                    totalWeight += entry.Weight;
                }
            }

            // Nothing can roll (empty table or all weights zero) — bail out safely.
            if (eligiblePool.Count == 0 || totalWeight <= 0)
                return;

            // 4. Generate supplementary items until reaching desired roll count
            int initialCount = inventory.Count;
            int itemsAdded = 0;
            int safetyLimit = 30;
            // "safetyLimit-- > 0" checks THEN decrements: hard stop after 30 attempts
            // so a broken item id can never cause an infinite loop.
            while (itemsAdded < finalDesiredCount && (inventory.Count < 12) && safetyLimit-- > 0)
            {
                // Weighted pick: throw a dart uniformly across 0..totalWeight, then walk
                // the entries accumulating their weights — whichever entry the dart lands
                // in wins. Heavier weights simply occupy wider slices of the range.
                double roll = random.NextDouble() * totalWeight;
                double cumulative = 0;
                FishingLootEntry chosen = eligiblePool[0];

                foreach (var entry in eligiblePool)
                {
                    cumulative += entry.Weight;
                    if (roll <= cumulative)
                    {
                        chosen = entry;
                        break;
                    }
                }

                // Roll the stack size: base count plus a random 0..(range) offset.
                int stack = chosen.MinCount;
                if (chosen.MaxCount > chosen.MinCount)
                {
                    stack += random.Next(chosen.MaxCount - chosen.MinCount + 1);
                }

                // allowNull: true returns null instead of throwing for unknown ids.
                Item? item = ItemRegistry.Create(chosen.QualifiedId, stack, allowNull: true);
                // The "?" after Item marks a nullable reference type; also reject the
                // game's placeholder "Error" items that appear for invalid ids.
                if (item != null && item.ItemId != "Error" && item.QualifiedItemId != "(O)Error")
                {
                    inventory.Add(item);
                    itemsAdded++;
                }
            }

            // 5. Fallback safety guarantee: ensure chest is never empty
            if (inventory.Count == 0)
            {
                // Golden chests get Deluxe Bait, normal ones get basic Bait.
                Item? fallback = ItemRegistry.Create(isGolden ? "(O)DeluxeBait" : "(O)685", 15);
                if (fallback != null)
                {
                    inventory.Add(fallback);
                }
            }
        }

        /// <summary>
        /// Decides whether an item counts as "trash" for the trash reroll bonus.
        /// </summary>
        /// <param name="item">The item to classify.</param>
        /// <returns>True if the item is junk, or a tiny (1-3) stack of stone or wood.</returns>
        public static bool IsTrashOrTrivial(Item item)
        {
            // Check BOTH id forms (qualified and bare) against the trash set.
            if (TrashItemIds.Contains(item.QualifiedItemId) || TrashItemIds.Contains(item.ItemId))
                return true;

            // Low-count 1x-3x Stone or Wood duds
            if ((item.QualifiedItemId == "(O)390" || item.ItemId == "390") && item.Stack <= 3)
                return true;

            if ((item.QualifiedItemId == "(O)388" || item.ItemId == "388") && item.Stack <= 3)
                return true;

            return false;
        }
    }
}
