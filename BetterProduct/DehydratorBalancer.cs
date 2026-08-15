using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace BetterProduct
{
    public static class DehydratorBalancer
    {
        private static ModConfig Config = null!;
        private static IMonitor Monitor = null!;

        public static void Initialize(ModConfig config, IMonitor monitor)
        {
            Config = config;
            Monitor = monitor;
        }

        public static void Apply(Harmony harmony)
        {
            try
            {
                var dropInMethod = AccessTools.Method(
                    typeof(StardewValley.Object),
                    nameof(StardewValley.Object.performObjectDropInAction),
                    new[] { typeof(Item), typeof(bool), typeof(Farmer), typeof(bool) }
                );

                if (dropInMethod != null)
                {
                    harmony.Patch(
                        original: dropInMethod,
                        prefix: new HarmonyMethod(typeof(DehydratorBalancer), nameof(Object_performObjectDropInAction_Prefix))
                    );
                    Monitor.Log("Dehydrator Harmony prefix patch applied successfully.", LogLevel.Trace);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply Dehydrator patches: {ex}", LogLevel.Error);
            }
        }

        public static bool IsDehydrator(StardewValley.Object machine)
        {
            if (machine == null) return false;
            return machine.QualifiedItemId == "(BC)298"
                || machine.ItemId == "298"
                || machine.QualifiedItemId == "(BC)Dehydrator"
                || machine.Name == "Dehydrator";
        }

        public static bool IsMushroom(Item item)
        {
            if (item == null) return false;
            if (item.HasContextTag("wild_mushroom_item") || item.HasContextTag("mushroom_item"))
                return true;

            return item.ItemId is "404" or "420" or "422" or "257" or "281" or "851";
        }

        public static bool IsValidDehydratorInput(Item item)
        {
            if (item == null) return false;
            if (item.QualifiedItemId == "(O)398" || item.ItemId == "398") // Grapes
                return true;
            if (item.Category == StardewValley.Object.FruitsCategory)
                return true;
            if (IsMushroom(item))
                return true;
            if (Config.AllowVegetableDehydrating && (item.Category == StardewValley.Object.VegetableCategory || item.Category == -75))
                return true;
            if (Config.AllowFlowerDehydrating && (item.Category == StardewValley.Object.flowersCategory || item.Category == -80))
                return true;

            return false;
        }

        public static bool Object_performObjectDropInAction_Prefix(
            StardewValley.Object __instance,
            Item dropInItem,
            bool probe,
            Farmer who,
            ref bool __result)
        {
            if (!Config.EnableDehydratorBalancing || !IsDehydrator(__instance) || dropInItem == null || who == null)
                return true;

            // If machine is currently processing or ready for harvest, let vanilla handle it
            if (__instance.heldObject.Value != null || __instance.readyForHarvest.Value || __instance.MinutesUntilReady > 0)
                return true;

            if (!IsValidDehydratorInput(dropInItem))
                return true;

            if (!Config.AllowMixedQualityDehydrating)
                return true;

            // Check total count across player inventory matching QualifiedItemId
            int totalMatching = 0;
            foreach (var item in who.Items)
            {
                if (item != null && item.QualifiedItemId == dropInItem.QualifiedItemId)
                {
                    totalMatching += item.Stack;
                }
            }

            if (totalMatching < 5)
            {
                // Not enough items total to start a batch
                return true;
            }

            if (probe)
            {
                __result = true;
                return false;
            }

            // Consume 5 items across inventory stacks and record individual qualities
            List<int> inputQualities = new List<int>();
            int remainingToConsume = 5;

            // 1. Consume from the active drop-in item first
            int takeFromActive = Math.Min(dropInItem.Stack, remainingToConsume);
            for (int i = 0; i < takeFromActive; i++)
            {
                inputQualities.Add(dropInItem.Quality);
            }
            dropInItem.Stack -= takeFromActive;
            remainingToConsume -= takeFromActive;

            if (dropInItem.Stack <= 0)
            {
                who.removeItemFromInventory(dropInItem);
            }

            // 2. Pull remaining from other inventory slots
            if (remainingToConsume > 0)
            {
                for (int i = 0; i < who.Items.Count && remainingToConsume > 0; i++)
                {
                    var item = who.Items[i];
                    if (item != null && item != dropInItem && item.QualifiedItemId == dropInItem.QualifiedItemId)
                    {
                        int take = Math.Min(item.Stack, remainingToConsume);
                        for (int k = 0; k < take; k++)
                        {
                            inputQualities.Add(item.Quality);
                        }
                        item.Stack -= take;
                        remainingToConsume -= take;

                        if (item.Stack <= 0)
                        {
                            who.Items[i] = null;
                        }
                    }
                }
            }

            // Calculate output quality from weighted score
            int outputQuality = 0;
            if (Config.EnableDriedQualityScaling && inputQualities.Count > 0)
            {
                float totalQualityScore = 0f;
                foreach (int q in inputQualities)
                {
                    totalQualityScore += q;
                }
                float avgScore = totalQualityScore / inputQualities.Count;

                if (avgScore >= 3.0f)
                    outputQuality = 4; // Iridium
                else if (avgScore >= 1.5f)
                    outputQuality = 2; // Gold
                else if (avgScore >= 0.5f)
                    outputQuality = 1; // Silver
                else
                    outputQuality = 0; // Normal
            }

            // Create output object
            StardewValley.Object outputObj;
            if (dropInItem.QualifiedItemId == "(O)398" || dropInItem.ItemId == "398")
            {
                outputObj = ItemRegistry.Create<StardewValley.Object>("(O)Raisins");
            }
            else if (IsMushroom(dropInItem))
            {
                outputObj = ItemRegistry.Create<StardewValley.Object>("(O)DriedMushrooms");
                outputObj.preservedParentSheetIndex.Value = dropInItem.ItemId;
                outputObj.preserve.Value = StardewValley.Object.PreserveType.DriedFruit;
            }
            else
            {
                outputObj = ItemRegistry.Create<StardewValley.Object>("(O)DriedFruit");
                outputObj.preservedParentSheetIndex.Value = dropInItem.ItemId;
                outputObj.preserve.Value = StardewValley.Object.PreserveType.DriedFruit;
            }

            outputObj.Quality = outputQuality;
            __instance.heldObject.Value = outputObj;

            // Set processing time
            if (Config.DehydratorSpeedMultiplier <= 0f)
            {
                __instance.MinutesUntilReady = 0;
                __instance.readyForHarvest.Value = true;
                __instance.showNextIndex.Value = true;
            }
            else
            {
                int baseMinutes = 1750;
                __instance.MinutesUntilReady = Math.Max(1, (int)(baseMinutes / Config.DehydratorSpeedMultiplier));
                __instance.showNextIndex.Value = true;
            }

            // Audio visual feedback
            who.currentLocation.playSound("Ship");
            who.currentLocation.playSound("bubbles");
            __instance.shakeTimer = 50;

            __result = true;
            return false;
        }
    }
}
