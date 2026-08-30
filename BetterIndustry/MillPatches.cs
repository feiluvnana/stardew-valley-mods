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

                    // Calculate quality distribution for this input quality (60/25/15 with 0% Iridium)
                    double rateSilver;
                    double rateGold;

                    switch (inputQuality)
                    {
                        case 1: // Silver (1⭐)
                            rateSilver = 60.0;
                            rateGold = 15.0;
                            break;

                        case 2: // Gold (2⭐)
                            rateSilver = 25.0;
                            rateGold = 60.0;
                            break;

                        case 4: // Iridium (4⭐)
                            rateSilver = 25.0;
                            rateGold = 75.0;
                            break;

                        default: // Normal (0⭐)
                            rateSilver = 25.0;
                            rateGold = 15.0;
                            break;
                    }

                    // Process each unit and roll output quality (0% Iridium)
                    for (int i = 0; i < totalUnitsToRoll; i++)
                    {
                        double roll = Game1.random.NextDouble() * 100.0;
                        int outputQuality;

                        if (roll < rateGold)
                            outputQuality = 2;
                        else if (roll < rateGold + rateSilver)
                            outputQuality = 1;
                        else
                            outputQuality = 0;

                        var outputObj = new StardewValley.Object(outputId, yieldMultiplier)
                        {
                            Quality = outputQuality
                        };

                        if (Config.EnableMillArtisanCategory)
                        {
                            outputObj.Category = StardewValley.Object.artisanGoodsCategory;
                        }

                        outputChest.addItem(outputObj);
                    }
                }

                // Clear input chest as items have been milled
                inputChest.Items.Clear();
                return false; // Skip vanilla dayUpdate for Mill to prevent duplicate processing
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in MillPatches DayUpdate_Prefix: {ex}", LogLevel.Error);
                return true; // Fallback to vanilla on error
            }
        }
    }
}
