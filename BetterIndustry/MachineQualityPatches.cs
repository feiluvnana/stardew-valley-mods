using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;

namespace BetterIndustry
{
    /// <summary>
    /// Harmony patches that apply the balanced Option 2 Quarter-Step quality matrix
    /// (75/25 & 50/25) to outputs produced by artisan machines in Stardew Valley 1.6.
    /// </summary>
    public static class MachineQualityPatches
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Applies the Harmony patch on MachineDataUtility.GetOutputItem.
        /// </summary>
        /// <param name="harmony">Harmony instance.</param>
        public static void Apply(Harmony harmony)
        {
            try
            {
                var method = AccessTools.Method(
                    typeof(MachineDataUtility),
                    nameof(MachineDataUtility.GetOutputItem),
                    new[]
                    {
                        typeof(StardewValley.Object),
                        typeof(MachineItemOutput),
                        typeof(Item),
                        typeof(Farmer),
                        typeof(bool),
                        typeof(int?).MakeByRefType()
                    }
                );

                if (method != null)
                {
                    harmony.Patch(
                        original: method,
                        postfix: new HarmonyMethod(typeof(MachineQualityPatches), nameof(GetOutputItem_Postfix))
                    );
                    Monitor.Log("Hooked MachineDataUtility.GetOutputItem with Option 2 quality matrix successfully.", LogLevel.Trace);
                }
                else
                {
                    Monitor.Log("Could not locate MachineDataUtility.GetOutputItem for machine quality balancing.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply MachineQualityPatches: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Harmony Postfix on MachineDataUtility.GetOutputItem: rolls the output quality using the
        /// Option 2 Quarter-Step probability matrix based on the input item's quality.
        /// </summary>
        public static void GetOutputItem_Postfix(
            StardewValley.Object machine,
            MachineItemOutput outputData,
            Item inputItem,
            Farmer who,
            bool probe,
            ref int? overrideMinutesUntilReady,
            ref Item? __result)
        {
            // Do not alter during UI probes / tooltips, or when the feature is disabled, or if result is missing
            if (probe || !Config.EnableMachineQuality || inputItem == null || __result == null)
                return;

            try
            {
                // Casks manage their own internal aging progression, so do not override cask aging outputs
                if (machine != null && (machine.ItemId == "163" || machine.QualifiedItemId == "(BC)163"))
                    return;

                // Determine base probabilities using 40/30/20/10 Matrix (identical to Cooking):
                // Normal (0⭐)  -> 40% Normal, 30% Silver, 20% Gold, 10% Iridium
                // Silver (1⭐)  -> 30% Normal, 40% Silver, 20% Gold, 10% Iridium
                // Gold (2⭐)    -> 30% Normal, 20% Silver, 40% Gold, 10% Iridium
                // Iridium (4⭐) -> 30% Normal, 20% Silver, 10% Gold, 40% Iridium
                double rateNormal;
                double rateSilver;
                double rateGold;
                double rateIridium;

                bool isLargeAnimalProduct = IsLargeAnimalProduct(inputItem);

                if (isLargeAnimalProduct)
                {
                    // Large animal products (Large Milk, Large Eggs, Dinosaur Egg, Ostrich Egg)
                    // have a Gold-level floor
                    if (inputItem.Quality >= 4) // Iridium Large input
                    {
                        rateNormal = 30.0;
                        rateSilver = 20.0;
                        rateGold = 10.0;
                        rateIridium = 40.0;
                    }
                    else // Normal, Silver, or Gold Large input
                    {
                        rateNormal = 30.0;
                        rateSilver = 20.0;
                        rateGold = 40.0;
                        rateIridium = 10.0;
                    }
                }
                else
                {
                    switch (inputItem.Quality)
                    {
                        case 1: // Silver (1⭐)
                            rateNormal = 30.0;
                            rateSilver = 40.0;
                            rateGold = 20.0;
                            rateIridium = 10.0;
                            break;

                        case 2: // Gold (2⭐)
                            rateNormal = 30.0;
                            rateSilver = 20.0;
                            rateGold = 40.0;
                            rateIridium = 10.0;
                            break;

                        case 4: // Iridium (4⭐)
                            rateNormal = 30.0;
                            rateSilver = 20.0;
                            rateGold = 10.0;
                            rateIridium = 40.0;
                            break;

                        default: // Normal (0⭐)
                            rateNormal = 40.0;
                            rateSilver = 30.0;
                            rateGold = 20.0;
                            rateIridium = 10.0;
                            break;
                    }
                }

                // Apply Daily Luck influence if enabled (identical to Cooking)
                if (Config.ApplyDailyLuckToMachines)
                {
                    double dailyLuck = who?.DailyLuck ?? Game1.player.DailyLuck;
                    double luckShift = dailyLuck * 100.0;
                    if (Math.Abs(luckShift) > 0.001)
                    {
                        double shift = 0.50 * luckShift;
                        rateIridium += shift;
                        rateGold += shift;
                        rateSilver -= shift;
                        rateNormal -= shift;

                        rateNormal = Math.Max(0.0, rateNormal);
                        rateSilver = Math.Max(0.0, rateSilver);
                        rateGold = Math.Max(0.0, rateGold);
                        rateIridium = Math.Max(0.0, rateIridium);

                        double sum = rateNormal + rateSilver + rateGold + rateIridium;
                        if (sum > 0)
                        {
                            rateNormal = (rateNormal / sum) * 100.0;
                            rateSilver = (rateSilver / sum) * 100.0;
                            rateGold = (rateGold / sum) * 100.0;
                            rateIridium = (rateIridium / sum) * 100.0;
                        }
                    }
                }

                // Roll quality from probability bands
                double roll = Game1.random.NextDouble() * 100.0;
                int quality;

                if (roll < rateIridium)
                {
                    quality = 4; // Iridium
                }
                else if (roll < rateIridium + rateGold)
                {
                    quality = 2; // Gold
                }
                else if (roll < rateIridium + rateGold + rateSilver)
                {
                    quality = 1; // Silver
                }
                else
                {
                    quality = 0; // Normal
                }

                __result.Quality = quality;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error calculating machine quality in GetOutputItem_Postfix: {ex}", LogLevel.Trace);
            }
        }

        /// <summary>
        /// Checks if an input item is a large or high-tier animal product that guarantees at least Gold quality.
        /// </summary>
        private static bool IsLargeAnimalProduct(Item item)
        {
            if (item == null) return false;

            string id = item.ItemId;
            string qid = item.QualifiedItemId;

            return id is "186" or "438" or "174" or "182" or "107" or "289" or "442"
                || qid is "(O)186" or "(O)438" or "(O)174" or "(O)182" or "(O)107" or "(O)289" or "(O)442"
                || string.Equals(id, "LargeMilk", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "LargeGoatMilk", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "LargeEgg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "LargeBrownEgg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "DinosaurEgg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "OstrichEgg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "DuckEgg", StringComparison.OrdinalIgnoreCase);
        }
    }
}
