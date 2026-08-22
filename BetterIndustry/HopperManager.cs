using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace BetterIndustry
{
    public static class HopperManager
    {
        private static readonly Vector2[] CardinalOffsets = new[]
        {
            new Vector2(0, -1), // North
            new Vector2(0, 1),  // South
            new Vector2(-1, 0), // West
            new Vector2(1, 0)   // East
        };

        /// <summary>
        /// Checks whether the given chest is an AutoLoader Hopper.
        /// </summary>
        public static bool IsHopper(Chest? chest)
        {
            if (chest == null)
                return false;

            return chest.SpecialChestType == Chest.SpecialChestTypes.AutoLoader ||
                   chest.QualifiedItemId == "(BC)275";
        }

        /// <summary>
        /// Process all hoppers in the given location.
        /// </summary>
        public static void ProcessLocation(GameLocation? location, Farmer? who = null)
        {
            if (location == null)
                return;

            who ??= Game1.player;

            var hoppers = new List<Chest>();
            foreach (var kvp in location.objects.Pairs)
            {
                if (kvp.Value is Chest chest && IsHopper(chest))
                {
                    hoppers.Add(chest);
                }
            }

            foreach (var hopper in hoppers)
            {
                ProcessHopper(hopper, location, who);
            }
        }

        /// <summary>
        /// Performs 4-directional automation for a hopper:
        /// 1. Pulls / harvests finished products from all 4 adjacent machines (North, South, West, East).
        /// 2. Pushes / feeds raw materials into all 4 adjacent machines (North, South, West, East).
        /// 3. Transfers collected items into any adjacent Chest or Mini-Shipping Bin.
        /// </summary>
        public static void ProcessHopper(Chest hopper, GameLocation location, Farmer? who = null)
        {
            if (hopper == null || location == null)
                return;

            who ??= Game1.player;
            var config = ModEntry.Config;
            var hopperPos = hopper.TileLocation;

            var adjacentChests = new List<Chest>();
            var connectedMachines = new List<StardewValley.Object>();

            foreach (var offset in CardinalOffsets)
            {
                var targetPos = hopperPos + offset;
                if (location.objects.TryGetValue(targetPos, out var obj) && obj != null)
                {
                    if (obj is Chest chest && !IsHopper(chest))
                    {
                        adjacentChests.Add(chest);
                    }
                    else if (obj is not Chest || obj.heldObject.Value != null)
                    {
                        connectedMachines.Add(obj);
                    }
                }
            }

            // Step 1: Harvest from all adjacent machines (4 directions)
            if (config.EnableAutoHarvest)
            {
                foreach (var machine in connectedMachines)
                {
                    TryHarvestMachine(machine, hopper, adjacentChests, location, who);
                }
            }

            // Step 2: Feed raw materials into all adjacent empty machines (4 directions)
            foreach (var machine in connectedMachines)
            {
                TryReloadMachine(machine, hopper, location, who);
            }

            // Step 3: Transfer any remaining collected items into adjacent output chests
            if (config.EnableChestOutputTransfer && adjacentChests.Count > 0 && hopper.Items.Count > 0)
            {
                foreach (var chest in adjacentChests)
                {
                    TryTransferToChest(hopper, chest, location);
                }
            }
        }

        /// <summary>
        /// Transfers items from the hopper into a designated output chest or mini-shipping bin.
        /// </summary>
        private static void TryTransferToChest(Chest hopper, Chest targetChest, GameLocation location)
        {
            if (hopper == null || targetChest == null || hopper.Items.Count == 0)
                return;

            bool anyTransferred = false;
            for (int i = 0; i < hopper.Items.Count; i++)
            {
                var item = hopper.Items[i];
                if (item == null)
                    continue;

                int initialStack = item.Stack;
                var leftover = targetChest.addItem(item);

                if (leftover == null || leftover.Stack < initialStack)
                {
                    anyTransferred = true;
                    hopper.Items[i] = (leftover != null && leftover.Stack > 0) ? leftover : null;
                }
            }

            if (anyTransferred)
            {
                hopper.clearNulls();
                if (ModEntry.Config.PlaySoundEffects)
                {
                    location.localSound("coin");
                }
            }
        }

        /// <summary>
        /// Attempts to harvest finished products from an adjacent machine into connected chests or the hopper.
        /// </summary>
        private static bool TryHarvestMachine(StardewValley.Object machine, Chest hopper, List<Chest> adjacentChests, GameLocation location, Farmer who)
        {
            if (machine == null)
                return false;

            var config = ModEntry.Config;

            // Handle Crab Pots
            if (machine is CrabPot crabPot)
            {
                if (!config.EnableCrabPotService)
                    return false;

                if (crabPot.readyForHarvest.Value && crabPot.heldObject.Value != null)
                {
                    var crabItem = crabPot.heldObject.Value;
                    Chest? targetStorage = FindStorageWithSpace(crabItem, adjacentChests, hopper, config.EnableChestOutputTransfer);
                    if (targetStorage != null)
                    {
                        var leftover = targetStorage.addItem(crabItem);
                        if (leftover == null || leftover.Stack < crabItem.Stack)
                        {
                            crabPot.heldObject.Value = (leftover != null && leftover.Stack > 0) ? (leftover as StardewValley.Object) : null;
                            crabPot.readyForHarvest.Value = (crabPot.heldObject.Value != null);
                            crabPot.tileIndexToShow = 710;

                            if (config.PlaySoundEffects)
                                location.localSound("coin");

                            return true;
                        }
                    }
                }
                return false;
            }

            // Handle Casks
            if (machine is Cask && !config.EnableCaskService)
            {
                return false;
            }

            // Standard Machines
            bool isReady = machine.readyForHarvest.Value || (machine.heldObject.Value != null && machine.MinutesUntilReady <= 0);
            if (!isReady || machine.heldObject.Value == null)
                return false;

            var machineData = machine.GetMachineData();

            // Handle RecalculateOnCollect
            if (machine.lastOutputRuleId.Value != null && machineData?.OutputRules != null)
            {
                var rule = machineData.OutputRules.FirstOrDefault(p => p.Id == machine.lastOutputRuleId.Value);
                if (rule != null && rule.RecalculateOnCollect)
                {
                    var oldHeld = machine.heldObject.Value;
                    machine.heldObject.Value = null;
                    machine.OutputMachine(machineData, rule, machine.lastInputItem.Value, who, location, probe: false, heldObjectOnly: true);
                    if (machine.heldObject.Value == null)
                    {
                        machine.heldObject.Value = oldHeld;
                    }
                }
            }

            var producedItem = machine.heldObject.Value;
            if (producedItem == null)
                return false;

            Chest? destination = FindStorageWithSpace(producedItem, adjacentChests, hopper, config.EnableChestOutputTransfer);
            if (destination == null)
                return false;

            var leftoverItem = destination.addItem(producedItem);
            if (leftoverItem != null && leftoverItem.Stack == producedItem.Stack)
            {
                // Destination has no space
                return false;
            }

            // Successfully harvested (all or partial)
            if (leftoverItem == null || leftoverItem.Stack <= 0)
            {
                machine.heldObject.Value = null;
                machine.readyForHarvest.Value = false;
                machine.showNextIndex.Value = false;
                machine.ResetParentSheetIndex();

                // Post-harvest triggers
                if (MachineDataUtility.TryGetMachineOutputRule(machine, machineData, MachineOutputTrigger.OutputCollected, producedItem.getOne(), who, location, out var outputRule, out _, out _, out _))
                {
                    machine.OutputMachine(machineData, outputRule, machine.lastInputItem.Value, who, location, probe: false);
                }

                if (machineData?.StatsToIncrementWhenHarvested != null)
                {
                    MachineDataUtility.UpdateStats(machineData.StatsToIncrementWhenHarvested, producedItem, producedItem.Stack);
                }

                if (machine.IsTapper() && location.terrainFeatures.TryGetValue(machine.TileLocation, out var tf) && tf is Tree tree)
                {
                    tree.UpdateTapperProduct(machine, producedItem);
                }

                if (machineData?.ExperienceGainOnHarvest != null && who != null)
                {
                    string[] expList = machineData.ExperienceGainOnHarvest.Split(' ');
                    for (int i = 0; i < expList.Length; i += 2)
                    {
                        int skillNumber = Farmer.getSkillNumberFromName(expList[i]);
                        if (skillNumber != -1 && ArgUtility.TryGetInt(expList, i + 1, out var expAmount, out _, "int amount"))
                        {
                            who.gainExperience(skillNumber, expAmount);
                        }
                    }
                }

                if (config.PlaySoundEffects)
                {
                    location.localSound("coin");
                }

                return true;
            }
            else
            {
                // Partial transfer
                machine.heldObject.Value = leftoverItem as StardewValley.Object;
                return true;
            }
        }

        /// <summary>
        /// Attempts to reload empty machines with items from the hopper.
        /// </summary>
        private static bool TryReloadMachine(StardewValley.Object machine, Chest hopper, GameLocation location, Farmer who)
        {
            if (machine == null || hopper == null || hopper.Items.Count == 0)
                return false;

            var config = ModEntry.Config;

            // Handle Crab Pots baiting
            if (machine is CrabPot crabPot)
            {
                if (config.EnableCrabPotService && crabPot.bait.Value == null)
                {
                    for (int i = 0; i < hopper.Items.Count; i++)
                    {
                        var item = hopper.Items[i];
                        if (item is StardewValley.Object obj && (obj.Category == StardewValley.Object.baitCategory || obj.QualifiedItemId == "(O)685" || obj.QualifiedItemId == "(O)703" || obj.QualifiedItemId == "(O)774" || obj.QualifiedItemId == "(O)DeluxeBait" || obj.QualifiedItemId == "(O)ChallengeBait"))
                        {
                            crabPot.bait.Value = (StardewValley.Object)obj.getOne();
                            obj.Stack--;
                            if (obj.Stack <= 0)
                            {
                                hopper.Items[i] = null;
                            }
                            hopper.clearNulls();

                            if (config.PlaySoundEffects)
                                location.localSound("dirtyHit");

                            return true;
                        }
                    }
                }
                return false;
            }

            // Handle Casks
            if (machine is Cask && !config.EnableCaskService)
            {
                return false;
            }

            // Standard machine reload
            if (machine.heldObject.Value == null)
            {
                bool loaded = machine.AttemptAutoLoad(hopper.Items, who);
                if (loaded)
                {
                    hopper.clearNulls();
                    if (config.PlaySoundEffects)
                    {
                        location.localSound("furnace");
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the best storage container with capacity for the item.
        /// </summary>
        private static Chest? FindStorageWithSpace(Item item, List<Chest> adjacentChests, Chest hopper, bool preferAdjacentChests)
        {
            if (preferAdjacentChests && adjacentChests.Count > 0)
            {
                foreach (var chest in adjacentChests)
                {
                    if (CanHoldItem(chest, item))
                        return chest;
                }
            }

            if (CanHoldItem(hopper, item))
                return hopper;

            if (!preferAdjacentChests && adjacentChests.Count > 0)
            {
                foreach (var chest in adjacentChests)
                {
                    if (CanHoldItem(chest, item))
                        return chest;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks whether the given chest has room for the item.
        /// </summary>
        private static bool CanHoldItem(Chest chest, Item item)
        {
            if (chest == null || item == null)
                return false;

            int capacity = chest.GetActualCapacity();
            if (chest.Items.Count < capacity)
                return true;

            // Check if stackable with existing item
            foreach (var existing in chest.Items)
            {
                if (existing != null && existing.canStackWith(item) && existing.Stack < existing.maximumStackSize())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
