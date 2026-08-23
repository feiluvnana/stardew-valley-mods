using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace BetterChest
{
    public static class FishingRewardManager
    {
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

        private record FishingLootEntry(string QualifiedId, int MinCount, int MaxCount, double Weight, bool GoldenOnly = false);

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

        public static void EnhanceFishingChest(FishingRod rod, ItemGrabMenu grabMenu)
        {
            if (grabMenu.ItemsToGrabMenu?.actualInventory == null)
                return;

            IList<Item> inventory = grabMenu.ItemsToGrabMenu.actualInventory;
            bool isGolden = rod.goldenTreasure;
            Random random = Game1.random;

            // 1. Determine target base rolls
            int minRolls = isGolden ? ModEntry.Config.GoldenChestMinRolls : ModEntry.Config.FishingChestMinRolls;
            int maxRolls = isGolden ? ModEntry.Config.GoldenChestMaxRolls : ModEntry.Config.FishingChestMaxRolls;
            if (maxRolls < minRolls)
                maxRolls = minRolls;

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

            int finalDesiredCount = Math.Min(12, targetRolls + trashCount);

            // 3. Build eligible fishing pool
            var eligiblePool = new List<FishingLootEntry>();
            double totalWeight = 0;
            foreach (var entry in FishingPool)
            {
                if (!entry.GoldenOnly || isGolden)
                {
                    eligiblePool.Add(entry);
                    totalWeight += entry.Weight;
                }
            }

            if (eligiblePool.Count == 0 || totalWeight <= 0)
                return;

            // 4. Generate supplementary items until reaching desired roll count
            int initialCount = inventory.Count;
            int itemsAdded = 0;
            int safetyLimit = 30;
            while (itemsAdded < finalDesiredCount && (inventory.Count < 12) && safetyLimit-- > 0)
            {
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

                int stack = chosen.MinCount;
                if (chosen.MaxCount > chosen.MinCount)
                {
                    stack += random.Next(chosen.MaxCount - chosen.MinCount + 1);
                }

                Item? item = ItemRegistry.Create(chosen.QualifiedId, stack, allowNull: true);
                if (item != null && item.ItemId != "Error" && item.QualifiedItemId != "(O)Error")
                {
                    inventory.Add(item);
                    itemsAdded++;
                }
            }

            // 5. Fallback safety guarantee: ensure chest is never empty
            if (inventory.Count == 0)
            {
                Item? fallback = ItemRegistry.Create(isGolden ? "(O)DeluxeBait" : "(O)685", 15);
                if (fallback != null)
                {
                    inventory.Add(fallback);
                }
            }
        }

        public static bool IsTrashOrTrivial(Item item)
        {
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
