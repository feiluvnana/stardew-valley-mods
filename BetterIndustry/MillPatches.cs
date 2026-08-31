using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace BetterIndustry
{
    /// <summary>
    /// Harmony patches on <see cref="Building"/> to preserve star quality when processing grains in the Mill.
    /// </summary>
    public static class MillPatches
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Applies Harmony patches to Building.dayUpdate.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                var method = AccessTools.Method(typeof(Building), nameof(Building.dayUpdate));
                if (method != null)
                {
                    harmony.Patch(
                        original: method,
                        prefix: new HarmonyMethod(typeof(MillPatches), nameof(DayUpdate_Prefix))
                    );
                    Monitor.Log("Hooked Building.dayUpdate (Mill) successfully.", LogLevel.Trace);
                }
                else
                {
                    Monitor.Log("Could not locate Building.dayUpdate method.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply MillPatches: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Prefix on Building.dayUpdate: when the building is a Mill, processes input grains into output goods
        /// while preserving star quality via the Option 2 40/30/20/10 matrix.
        /// </summary>
        public static bool DayUpdate_Prefix(Building __instance, int dayOfMonth)
        {
            if (!Config.EnableMillQualityMatrix || __instance == null)
                return true;

            try
            {
                string buildingType = __instance.buildingType?.Value ?? string.Empty;
                if (!buildingType.Equals("Mill", StringComparison.OrdinalIgnoreCase))
                    return true;

                Chest? inputChest = __instance.GetBuildingChest("Input");
                Chest? outputChest = __instance.GetBuildingChest("Output");

                if (inputChest == null || outputChest == null || inputChest.Items.Count == 0)
                    return true;

                var itemsToProcess = new List<Item>(inputChest.Items);
                if (itemsToProcess.Count == 0)
                    return true;

                foreach (Item item in itemsToProcess)
                {
                    if (item == null)
                        continue;

                    string itemId = item.ItemId;
                    string qid = item.QualifiedItemId;

                    string? outputId = null;
                    int yieldMultiplier = 1;

                    // Wheat (262) -> Wheat Flour (246)
                    if (itemId == "262" || qid == "(O)262" || item.Name.Equals("Wheat", StringComparison.OrdinalIgnoreCase))
                    {
                        outputId = "246";
                        yieldMultiplier = 1;
                    }
                    // Beet (284) -> Sugar (245), 1 Beet yields 3 Sugar
                    else if (itemId == "284" || qid == "(O)284" || item.Name.Equals("Beet", StringComparison.OrdinalIgnoreCase))
                    {
                        outputId = "245";
                        yieldMultiplier = 3;
                    }
                    // Unmilled Rice (271) -> Rice (423)
                    else if (itemId == "271" || qid == "(O)271" || item.Name.Equals("Unmilled Rice", StringComparison.OrdinalIgnoreCase))
                    {
                        outputId = "423";
                        yieldMultiplier = 1;
                    }

                    if (outputId == null)
                        continue;

                    int totalUnitsToRoll = item.Stack;
                    int inputQuality = item.Quality;

                    // Calculate quality distribution for this input quality (Model A: Strict Quality Step-Down, 0% Iridium)
                    // Normal (0⭐)  -> 100% Normal, 0% Silver, 0% Gold (No free upgrades)
                    // Silver (1⭐)  ->  70% Normal, 30% Silver, 0% Gold
                    // Gold (2⭐)    ->  40% Normal, 45% Silver, 15% Gold
                    // Iridium (4⭐) ->  20% Normal, 50% Silver, 30% Gold
                    double rateSilver;
                    double rateGold;

                    switch (inputQuality)
                    {
                        case 1: // Silver (1⭐)
                            rateSilver = 30.0;
                            rateGold = 0.0;
                            break;

                        case 2: // Gold (2⭐)
                            rateSilver = 45.0;
                            rateGold = 15.0;
                            break;

                        case 4: // Iridium (4⭐)
                            rateSilver = 50.0;
                            rateGold = 30.0;
                            break;

                        default: // Normal (0⭐)
                            rateSilver = 0.0;
                            rateGold = 0.0;
                            break;
                    }

                    // Roll output qualities and aggregate into stacked batches
                    int countGold = 0;
                    int countSilver = 0;
                    int countNormal = 0;

                    for (int i = 0; i < totalUnitsToRoll; i++)
                    {
                        double roll = Game1.random.NextDouble() * 100.0;
                        if (roll < rateGold)
                            countGold++;
                        else if (roll < rateGold + rateSilver)
                            countSilver++;
                        else
                            countNormal++;
                    }

                    void AddMillOutput(int units, int quality)
                    {
                        if (units <= 0) return;
                        var outputObj = new StardewValley.Object(outputId, units * yieldMultiplier)
                        {
                            Quality = quality
                        };

                        if (Config.EnableMillArtisanCategory)
                        {
                            outputObj.Category = StardewValley.Object.artisanGoodsCategory;
                        }

                        Item? leftovers = outputChest.addItem(outputObj);
                        if (leftovers != null && leftovers.Stack > 0)
                        {
                            // If output chest is completely full, refund uncrafted input back into inputChest
                            int refundUnits = (int)Math.Ceiling((double)leftovers.Stack / yieldMultiplier);
                            var refund = new StardewValley.Object(item.ItemId, refundUnits) { Quality = item.Quality };
                            inputChest.addItem(refund);
                        }
                    }

                    AddMillOutput(countNormal, 0);
                    AddMillOutput(countSilver, 1);
                    AddMillOutput(countGold, 2);
                }

                // Remove processed items from input chest
                inputChest.clearNulls();
                for (int i = inputChest.Items.Count - 1; i >= 0; i--)
                {
                    Item processed = inputChest.Items[i];
                    if (processed != null)
                    {
                        string pid = processed.ItemId;
                        string pqid = processed.QualifiedItemId;
                        // Only remove items this mod actually processed (Wheat, Beet, Unmilled Rice)
                        if (pid is "262" or "284" or "271" || pqid is "(O)262" or "(O)284" or "(O)271")
                        {
                            inputChest.Items.RemoveAt(i);
                        }
                    }
                }
                inputChest.clearNulls();

                // Return false to prevent vanilla Mill from double-processing grains
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in MillPatches DayUpdate_Prefix: {ex}", LogLevel.Error);
                return true; // Fallback to vanilla on error
            }
        }
    }
}
