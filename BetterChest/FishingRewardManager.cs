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
            "168", "169", "170", "171", "172"
        };

        private static readonly Dictionary<string, (int StandardMin, int GoldenMin)> ResourceMinimums = new(StringComparer.OrdinalIgnoreCase)
        {
            // Ores & Materials
            { "(O)382", (8, 20) },          // Coal
            { "382", (8, 20) },
            { "(O)378", (10, 25) },         // Copper Ore
            { "378", (10, 25) },
            { "(O)380", (10, 25) },         // Iron Ore
            { "380", (10, 25) },
            { "(O)384", (8, 20) },          // Gold Ore
            { "384", (8, 20) },
            { "(O)386", (3, 8) },           // Iridium Ore
            { "386", (3, 8) },

            // Baits & Tackle Consumables
            { "(O)685", (15, 35) },         // Bait
            { "685", (15, 35) },
            { "(O)DeluxeBait", (10, 20) },  // Deluxe Bait
            { "DeluxeBait", (10, 20) },
            { "(O)774", (5, 12) },          // Wild Bait
            { "774", (5, 12) },
            { "(O)ChallengeBait", (10, 20) },// Challenge Bait
            { "ChallengeBait", (10, 20) },
            { "(O)908", (5, 12) },          // Magic Bait
            { "908", (5, 12) },
            { "(O)703", (5, 10) },          // Magnet
            { "703", (5, 10) },

            // Geodes & Boxes
            { "(O)535", (3, 6) },           // Geode
            { "535", (3, 6) },
            { "(O)536", (3, 6) },           // Frozen Geode
            { "536", (3, 6) },
            { "(O)537", (3, 6) },           // Magma Geode
            { "537", (3, 6) },
            { "(O)749", (3, 8) },           // Omni Geode
            { "749", (3, 8) },
            { "(O)MysteryBox", (2, 4) },    // Mystery Box
            { "MysteryBox", (2, 4) },
            { "(O)GoldenMysteryBox", (2, 3) }, // Golden Mystery Box
            { "GoldenMysteryBox", (2, 3) }
        };

        public static void EnhanceFishingChest(FishingRod rod, ItemGrabMenu grabMenu)
        {
            if (grabMenu.ItemsToGrabMenu?.actualInventory == null)
                return;

            IList<Item> inventory = grabMenu.ItemsToGrabMenu.actualInventory;
            bool isGolden = rod.goldenTreasure;
            Random random = Game1.random;

            // 1. Filter out trash duds and low-count stone/wood junk
            if (ModEntry.Config.FilterFishingChestJunk)
            {
                for (int i = inventory.Count - 1; i >= 0; i--)
                {
                    Item? item = inventory[i];
                    if (item == null)
                        continue;

                    if (IsJunkDud(item))
                    {
                        inventory.RemoveAt(i);
                    }
                }
            }

            // 2. Apply minimum stack floors and multiplier for resources
            if (ModEntry.Config.BoostFishingResourceStacks)
            {
                float multiplier = isGolden
                    ? ModEntry.Config.GoldenChestStackMultiplier
                    : ModEntry.Config.FishingResourceStackMultiplier;

                foreach (Item? item in inventory)
                {
                    if (item == null)
                        continue;

                    if (ResourceMinimums.TryGetValue(item.QualifiedItemId, out var mins) ||
                        ResourceMinimums.TryGetValue(item.ItemId, out mins))
                    {
                        int minFloor = isGolden ? mins.GoldenMin : mins.StandardMin;
                        if (item.Stack < minFloor)
                        {
                            item.Stack = minFloor;
                        }

                        if (multiplier > 1.0f)
                        {
                            int boosted = (int)Math.Round(item.Stack * multiplier);
                            item.Stack = Math.Max(minFloor, boosted);
                        }
                    }
                }
            }

            // 3. 1.6 Golden Fishing Treasure Chest Enhancements
            if (isGolden && ModEntry.Config.EnableGoldenChestBuff)
            {
                // Pearl Bonus (Boosted chance)
                if (ModEntry.Config.GoldenChestPearlBonus && !HasItem(inventory, "(O)797", "797"))
                {
                    if (random.NextDouble() < 0.20) // 20% bonus chance
                    {
                        AddItemSafely(inventory, ItemRegistry.Create("(O)797", 1));
                    }
                }

                // 1.6 Marine Jellies Bonus (25% chance)
                if (random.NextDouble() < 0.25)
                {
                    string[] jellies = { "(O)SeaJelly", "(O)RiverJelly", "(O)CaveJelly" };
                    string chosenJelly = jellies[random.Next(jellies.Length)];
                    if (!HasItem(inventory, chosenJelly))
                    {
                        AddItemSafely(inventory, ItemRegistry.Create(chosenJelly, random.Next(1, 3)));
                    }
                }

                // Fishing Buff Food (20% chance)
                if (random.NextDouble() < 0.20)
                {
                    string foodId = random.NextDouble() < 0.5 ? "(O)265" : "(O)242"; // Seafoam Pudding or Dish O' The Sea
                    AddItemSafely(inventory, ItemRegistry.Create(foodId, random.Next(1, 3)));
                }
            }

            // 4. Artifact & Power Item Fairness Check
            if (ModEntry.Config.EnableFishingArtifactProtection && Game1.player != null)
            {
                // Dino Egg check if player hasn't found one and fishing level >= 5
                if (Game1.player.FishingLevel >= 5 && !Game1.player.hasOrWillReceiveMail("DinoEggFound") && !HasItem(inventory, "(O)107", "107"))
                {
                    if (random.NextDouble() < 0.05) // 5% bonus check
                    {
                        AddItemSafely(inventory, ItemRegistry.Create("(O)107", 1));
                    }
                }

                // Ancient Seed check
                if (!HasItem(inventory, "(O)114", "114") && random.NextDouble() < 0.05)
                {
                    AddItemSafely(inventory, ItemRegistry.Create("(O)114", 1));
                }
            }

            // 5. Fallback safety guarantee: ensure chest never ends up completely empty
            if (inventory.Count == 0)
            {
                Item fallback = isGolden
                    ? ItemRegistry.Create("(O)DeluxeBait", 25)
                    : ItemRegistry.Create("(O)685", 20);

                if (fallback != null)
                {
                    inventory.Add(fallback);
                }
            }
        }

        private static bool IsJunkDud(Item item)
        {
            if (TrashItemIds.Contains(item.QualifiedItemId) || TrashItemIds.Contains(item.ItemId))
                return true;

            // 1x-3x Stone or Wood duds
            if ((item.QualifiedItemId == "(O)390" || item.ItemId == "390") && item.Stack <= 3) // Stone
                return true;

            if ((item.QualifiedItemId == "(O)388" || item.ItemId == "388") && item.Stack <= 3) // Wood
                return true;

            return false;
        }

        private static bool HasItem(IList<Item> inventory, string qualifiedId, string? rawId = null)
        {
            foreach (var item in inventory)
            {
                if (item == null) continue;
                if (string.Equals(item.QualifiedItemId, qualifiedId, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (rawId != null && string.Equals(item.ItemId, rawId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void AddItemSafely(IList<Item> inventory, Item? newItem)
        {
            if (newItem == null || newItem.ItemId == "Error" || newItem.QualifiedItemId == "(O)Error")
                return;

            if (inventory.Count < 12) // Prevent overflowing inventory UI
            {
                inventory.Add(newItem);
            }
        }
    }
}
