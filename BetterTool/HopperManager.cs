using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace BetterTool
{
    public static class HopperManager
    {
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
        /// Performs strictly downward automation for a hopper:
        /// 1. Pulls / harvests finished products from the machine directly ABOVE (Y - 1).
        /// 2. Pushes / feeds raw materials into the machine directly BELOW (Y + 1), OR
        ///    transfers items downward into a Chest / Mini-Shipping Bin directly BELOW (Y + 1).
        /// </summary>
        public static void ProcessHopper(Chest hopper, GameLocation location, Farmer? who = null)
        {
            if (hopper == null || location == null)
                return;

            who ??= Game1.player;
            var config = ModEntry.Config;
            var hopperPos = hopper.TileLocation;

            var abovePos = new Vector2(hopperPos.X, hopperPos.Y - 1f);
            var belowPos = new Vector2(hopperPos.X, hopperPos.Y + 1f);

            // Step 1: Harvest from machine directly ABOVE (Y - 1)
            if (config.EnableAutoHarvest && location.objects.TryGetValue(abovePos, out var aboveObj) && aboveObj != null)
            {
                if (aboveObj is not Chest || aboveObj.heldObject.Value != null)
                {
                    TryHarvestFromAboveMachine(aboveObj, hopper, location, who);
                }
            }

            // Step 2: Push downward to object directly BELOW (Y + 1)
            if (location.objects.TryGetValue(belowPos, out var belowObj) && belowObj != null)
            {
                if (belowObj is Chest belowChest)
                {
                    // Output destination: Chest, MiniShippingBin, or another Hopper below
                    if (config.EnableAdjacentChestOutput)
                    {
                        TryTransferToBelowChest(hopper, belowChest, location);
                    }
                }
                else
                {
                    // Machine to feed below
                    TryReloadBelowMachine(belowObj, hopper, location, who);
                }
            }
        }

        /// <summary>
        /// Transfers items downward from the hopper into a chest or mini-shipping bin below it.
        /// </summary>
        private static void TryTransferToBelowChest(Chest hopper, Chest targetChest, GameLocation location)
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
        /// Attempts to harvest finished products from a machine directly above the hopper.
        /// </summary>
        private static bool TryHarvestFromAboveMachine(StardewValley.Object machine, Chest hopper, GameLocation location, Farmer who)
        {
            if (machine == null)
                return false;

            var config = ModEntry.Config;

            // Handle Crab Pots
            if (machine is CrabPot crabPot)
            {
                if (crabPot.readyForHarvest.Value && crabPot.heldObject.Value != null)
                {
                    var crabItem = crabPot.heldObject.Value;
                    int initialStack = crabItem.Stack;
                    var leftover = hopper.addItem(crabItem);

                    if (leftover == null || leftover.Stack < initialStack)
                    {
                        crabPot.heldObject.Value = (leftover != null && leftover.Stack > 0) ? (leftover as StardewValley.Object) : null;
                        crabPot.readyForHarvest.Value = (crabPot.heldObject.Value != null);
                        crabPot.tileIndexToShow = 710;

                        if (config.PlaySoundEffects)
                            location.localSound("coin");

                        return true;
                    }
                }
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

            var leftoverItem = hopper.addItem(producedItem);
            if (leftoverItem != null && leftoverItem.Stack == producedItem.Stack)
            {
                // Hopper has no space
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
        /// Attempts to reload empty machines directly below the hopper.
        /// </summary>
        private static bool TryReloadBelowMachine(StardewValley.Object machine, Chest hopper, GameLocation location, Farmer who)
        {
            if (machine == null || hopper == null || hopper.Items.Count == 0)
                return false;

            var config = ModEntry.Config;

            // Handle Crab Pots baiting
            if (machine is CrabPot crabPot)
            {
                if (config.ServiceCrabPots && crabPot.bait.Value == null)
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
    }
}
