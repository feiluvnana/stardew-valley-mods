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

        // Full / High-tier base minimum floors (Fishing Level 9-10+)
        private static readonly Dictionary<string, (int StandardMin, int GoldenMin)> HighResourceMinimums = new(StringComparer.OrdinalIgnoreCase)
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

        // Mid-tier minimum floors (Fishing Level 5-8)
        private static readonly Dictionary<string, (int StandardMin, int GoldenMin)> MidResourceMinimums = new(StringComparer.OrdinalIgnoreCase)
        {
            { "(O)382", (5, 15) },          // Coal
            { "382", (5, 15) },
            { "(O)378", (7, 18) },          // Copper Ore
            { "378", (7, 18) },
            { "(O)380", (6, 16) },          // Iron Ore
            { "380", (6, 16) },
            { "(O)384", (5, 14) },          // Gold Ore
            { "384", (5, 14) },
            { "(O)386", (2, 5) },           // Iridium Ore
            { "386", (2, 5) },
            { "(O)685", (10, 25) },         // Bait
            { "685", (10, 25) },
            { "(O)DeluxeBait", (7, 15) },   // Deluxe Bait
            { "DeluxeBait", (7, 15) },
            { "(O)774", (4, 8) },           // Wild Bait
            { "774", (4, 8) },
            { "(O)ChallengeBait", (6, 14) },
            { "ChallengeBait", (6, 14) },
            { "(O)908", (3, 8) },
            { "908", (3, 8) },
            { "(O)703", (3, 7) },           // Magnet
            { "703", (3, 7) },
            { "(O)535", (2, 4) },           // Geode
            { "535", (2, 4) },
            { "(O)536", (2, 4) },           // Frozen Geode
            { "536", (2, 4) },
            { "(O)537", (2, 4) },           // Magma Geode
            { "537", (2, 4) },
            { "(O)749", (2, 5) },           // Omni Geode
            { "749", (2, 5) },
            { "(O)MysteryBox", (1, 3) },
            { "MysteryBox", (1, 3) },
            { "(O)GoldenMysteryBox", (1, 2) },
            { "GoldenMysteryBox", (1, 2) }
        };

        // Low-tier minimum floors (Fishing Level 0-4)
        private static readonly Dictionary<string, (int StandardMin, int GoldenMin)> LowResourceMinimums = new(StringComparer.OrdinalIgnoreCase)
        {
            { "(O)382", (3, 10) },          // Coal
            { "382", (3, 10) },
            { "(O)378", (4, 12) },          // Copper Ore
            { "378", (4, 12) },
            { "(O)380", (3, 10) },          // Iron Ore
            { "380", (3, 10) },
            { "(O)384", (2, 8) },           // Gold Ore
            { "384", (2, 8) },
            { "(O)386", (1, 3) },           // Iridium Ore
            { "386", (1, 3) },
            { "(O)685", (5, 18) },          // Bait
            { "685", (5, 18) },
            { "(O)DeluxeBait", (4, 10) },   // Deluxe Bait
            { "DeluxeBait", (4, 10) },
            { "(O)774", (2, 6) },           // Wild Bait
            { "774", (2, 6) },
            { "(O)ChallengeBait", (4, 8) },
            { "ChallengeBait", (4, 8) },
            { "(O)908", (2, 5) },
            { "908", (2, 5) },
            { "(O)703", (2, 5) },           // Magnet
            { "703", (2, 5) },
            { "(O)535", (1, 3) },           // Geode
            { "535", (1, 3) },
            { "(O)536", (1, 3) },           // Frozen Geode
            { "536", (1, 3) },
            { "(O)537", (1, 3) },           // Magma Geode
            { "537", (1, 3) },
            { "(O)749", (1, 3) },           // Omni Geode
            { "749", (1, 3) },
            { "(O)MysteryBox", (1, 2) },
            { "MysteryBox", (1, 2) },
            { "(O)GoldenMysteryBox", (1, 2) },
            { "GoldenMysteryBox", (1, 2) }
        };

        public static void EnhanceFishingChest(FishingRod rod, ItemGrabMenu grabMenu)
        {
            if (grabMenu.ItemsToGrabMenu?.actualInventory == null)
                return;

            IList<Item> inventory = grabMenu.ItemsToGrabMenu.actualInventory;
            bool isGolden = rod.goldenTreasure;
            Random random = Game1.random;
            int fishingLevel = Game1.player?.FishingLevel ?? 0;
            int deepestMine = ProgressionHelper.GetDeepestMineLevel();

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

            // 2. Progression Gatekeeping on Fishing Drops (Replace over-leveled items with appropriate tier)
            if (ModEntry.Config.GatekeepFishingHighTierLoot)
            {
                for (int i = 0; i < inventory.Count; i++)
                {
                    Item? item = inventory[i];
                    if (item == null)
                        continue;

                    string qId = item.QualifiedItemId;
                    string id = item.ItemId;

                    // Prismatic Shard: requires Mine Level >= 120 or Fishing Level >= 7
                    if (qId == "(O)74" || id == "74")
                    {
                        if (deepestMine < 120 && fishingLevel < 7)
                        {
                            inventory[i] = ItemRegistry.Create(deepestMine >= 80 || fishingLevel >= 5 ? "(O)72" : "(O)749", item.Stack); // Downgrade to Diamond or Omni Geode
                        }
                    }
                    // Iridium Bar: requires Mine Level >= 120 or Fishing Level >= 8
                    else if (qId == "(O)337" || id == "337")
                    {
                        if (deepestMine < 120 && fishingLevel < 8)
                        {
                            inventory[i] = ItemRegistry.Create(deepestMine >= 80 || fishingLevel >= 5 ? "(O)336" : "(O)335", item.Stack); // Downgrade to Gold Bar or Iron Bar
                        }
                    }
                    // Iridium Ore: requires Mine Level >= 120 or Fishing Level >= 9
                    else if (qId == "(O)386" || id == "386")
                    {
                        if (deepestMine < 120 && fishingLevel < 9)
                        {
                            inventory[i] = ItemRegistry.Create(deepestMine >= 80 || fishingLevel >= 6 ? "(O)384" : "(O)380", item.Stack); // Downgrade to Gold or Iron
                        }
                    }
                    // Gold Ore: requires Mine Level >= 80 or Fishing Level >= 7
                    else if (qId == "(O)384" || id == "384")
                    {
                        if (deepestMine < 80 && fishingLevel < 7)
                        {
                            inventory[i] = ItemRegistry.Create(deepestMine >= 40 || fishingLevel >= 4 ? "(O)380" : "(O)378", item.Stack); // Downgrade to Iron or Copper
                        }
                    }
                    // Iron Ore: requires Mine Level >= 40 or Fishing Level >= 4
                    else if (qId == "(O)380" || id == "380")
                    {
                        if (deepestMine < 40 && fishingLevel < 4)
                        {
                            inventory[i] = ItemRegistry.Create("(O)378", item.Stack); // Downgrade to Copper
                        }
                    }
                    // Magma Geode: requires Mine Level >= 80 or Fishing Level >= 7
                    else if (qId == "(O)537" || id == "537")
                    {
                        if (deepestMine < 80 && fishingLevel < 7)
                        {
                            inventory[i] = ItemRegistry.Create(deepestMine >= 40 || fishingLevel >= 4 ? "(O)536" : "(O)535", item.Stack);
                        }
                    }
                    // Frozen Geode: requires Mine Level >= 40 or Fishing Level >= 4
                    else if (qId == "(O)536" || id == "536")
                    {
                        if (deepestMine < 40 && fishingLevel < 4)
                        {
                            inventory[i] = ItemRegistry.Create("(O)535", item.Stack);
                        }
                    }
                    // Mystery Box: requires Mr. Qi cutscene triggered
                    else if (qId == "(O)MysteryBox" || id == "MysteryBox")
                    {
                        if (!ProgressionHelper.IsMysteryBoxUnlocked())
                        {
                            inventory[i] = ItemRegistry.Create("(O)749", item.Stack); // Downgrade to Omni Geode
                        }
                    }
                    // Golden Mystery Box: requires Combat Mastery or 30+ boxes
                    else if (qId == "(O)GoldenMysteryBox" || id == "GoldenMysteryBox")
                    {
                        if (!ProgressionHelper.IsMasteryUnlocked("Combat") && ProgressionHelper.GetMysteryBoxesOpened() < 30)
                        {
                            inventory[i] = ItemRegistry.Create(ProgressionHelper.IsMysteryBoxUnlocked() ? "(O)MysteryBox" : "(O)749", item.Stack);
                        }
                    }
                    // Golden Animal Cracker: requires Farming Mastery
                    else if (qId == "(O)GoldenAnimalCracker" || id == "GoldenAnimalCracker")
                    {
                        if (!ProgressionHelper.IsMasteryUnlocked("Farming"))
                        {
                            inventory[i] = ItemRegistry.Create("(O)DeluxeBait", 20);
                        }
                    }
                    // Challenge Bait: requires Fishing Mastery
                    else if (qId == "(O)ChallengeBait" || id == "ChallengeBait")
                    {
                        if (!ProgressionHelper.IsMasteryUnlocked("Fishing"))
                        {
                            inventory[i] = ItemRegistry.Create("(O)DeluxeBait", item.Stack);
                        }
                    }
                    // Magic Bait: requires Qi Room unlocked
                    else if (qId == "(O)908" || id == "908")
                    {
                        if (!ProgressionHelper.IsQiRoomUnlocked())
                        {
                            inventory[i] = ItemRegistry.Create("(O)DeluxeBait", item.Stack);
                        }
                    }
                    // Ginger Island items: Cinder Shard, Dragon Tooth, Golden Coconut
                    else if ((qId == "(O)848" || id == "848" || qId == "(O)852" || id == "852" || qId == "(O)791" || id == "791") && !ProgressionHelper.IsIslandUnlocked())
                    {
                        inventory[i] = ItemRegistry.Create("(O)749", item.Stack); // Downgrade to Omni Geode
                    }
                    // Qi Room items: Galaxy Soul, Pressure Nozzle, Enricher
                    else if ((qId == "(O)896" || id == "896" || qId == "(O)915" || id == "915" || qId == "(O)913" || id == "913") && !ProgressionHelper.IsQiRoomUnlocked())
                    {
                        inventory[i] = ItemRegistry.Create("(O)72", item.Stack); // Downgrade to Diamond
                    }
                }
            }

            // 3. Apply minimum stack floors and multiplier for resources (Scaled by Fishing Level)
            if (ModEntry.Config.BoostFishingResourceStacks)
            {
                Dictionary<string, (int StandardMin, int GoldenMin)> activeMinimums;
                if (!ModEntry.Config.ScaleFishingResourcesByLevel || fishingLevel >= 9)
                {
                    activeMinimums = HighResourceMinimums;
                }
                else if (fishingLevel >= 5)
                {
                    activeMinimums = MidResourceMinimums;
                }
                else
                {
                    activeMinimums = LowResourceMinimums;
                }

                float multiplier = isGolden
                    ? ModEntry.Config.GoldenChestStackMultiplier
                    : ModEntry.Config.FishingResourceStackMultiplier;

                foreach (Item? item in inventory)
                {
                    if (item == null)
                        continue;

                    if (activeMinimums.TryGetValue(item.QualifiedItemId, out var mins) ||
                        activeMinimums.TryGetValue(item.ItemId, out mins))
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

            // 4. 1.6 Golden Fishing Treasure Chest Enhancements
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

            // 5. Artifact & Rare Item Fairness Check
            if (ModEntry.Config.EnableFishingArtifactProtection && Game1.player != null)
            {
                // Dino Egg check: require Fishing Level >= 5 and (Mine Level >= 40 or Fishing Level >= 7)
                bool dinoEligible = fishingLevel >= 5 && (deepestMine >= 40 || fishingLevel >= 7);
                if (dinoEligible && !Game1.player.hasOrWillReceiveMail("DinoEggFound") && !HasItem(inventory, "(O)107", "107"))
                {
                    if (random.NextDouble() < 0.05) // 5% bonus check
                    {
                        AddItemSafely(inventory, ItemRegistry.Create("(O)107", 1));
                    }
                }

                // Ancient Seed check: require Fishing Level >= 3 (preventing day 1 instant drop)
                bool seedEligible = fishingLevel >= 3 && (Game1.year > 1 || Game1.season != Season.Spring || Game1.dayOfMonth >= 10 || fishingLevel >= 5);
                if (seedEligible && !HasItem(inventory, "(O)114", "114") && random.NextDouble() < 0.05)
                {
                    AddItemSafely(inventory, ItemRegistry.Create("(O)114", 1));
                }
            }

            // 6. Fallback safety guarantee: ensure chest never ends up completely empty
            if (inventory.Count == 0)
            {
                Item fallback = isGolden
                    ? ItemRegistry.Create("(O)DeluxeBait", 25)
                    : ItemRegistry.Create("(O)685", fishingLevel >= 5 ? 20 : 10);

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
