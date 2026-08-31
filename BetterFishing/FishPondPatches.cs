using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;

namespace BetterFishing
{
    /// <summary>
    /// Harmony patches on <see cref="FishPond"/> to roll quality (Silver, Gold, Iridium) on Roe and other outputs.
    /// </summary>
    public static class FishPondPatches
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Applies the Harmony patch to FishPond.dayUpdate.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                var method = AccessTools.Method(typeof(FishPond), nameof(FishPond.dayUpdate));
                if (method != null)
                {
                    harmony.Patch(
                        original: method,
                        postfix: new HarmonyMethod(typeof(FishPondPatches), nameof(DayUpdate_Postfix))
                    );
                    Monitor.Log("Hooked FishPond.dayUpdate successfully.", LogLevel.Trace);
                }
                else
                {
                    Monitor.Log("Could not locate FishPond.dayUpdate method.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply FishPondPatches: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Postfix on FishPond.dayUpdate: evaluates newly produced output and assigns star quality (Silver, Gold, Iridium)
        /// based on pond population tier and daily luck.
        /// </summary>
        public static void DayUpdate_Postfix(FishPond __instance)
        {
            if (!Config.EnableFishPondQuality || __instance == null)
                return;

            try
            {
                var outputItem = __instance.output.Value;
                if (outputItem is not StardewValley.Object obj || !CanHaveQuality(obj))
                    return;

                // Calculate quality based on occupants (1 to 10) and player daily luck
                int occupants = Math.Clamp(__instance.currentOccupants.Value, 1, 10);

                double rateNormal;
                double rateSilver;
                double rateGold;
                double rateIridium;

                if (occupants <= 3)
                {
                    rateNormal = 70.0;
                    rateSilver = 25.0;
                    rateGold = 5.0;
                    rateIridium = 0.0;
                }
                else if (occupants <= 6)
                {
                    rateNormal = 40.0;
                    rateSilver = 35.0;
                    rateGold = 20.0;
                    rateIridium = 5.0;
                }
                else if (occupants <= 9)
                {
                    rateNormal = 20.0;
                    rateSilver = 35.0;
                    rateGold = 35.0;
                    rateIridium = 10.0;
                }
                else // 10 occupants
                {
                    rateNormal = 10.0;
                    rateSilver = 25.0;
                    rateGold = 40.0;
                    rateIridium = 25.0;
                }

                // Apply Daily Luck influence
                double dailyLuck = Game1.player?.DailyLuck ?? 0.0;
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

                obj.Quality = quality;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error applying fish pond quality: {ex}", LogLevel.Error);
            }
        }

        private static bool CanHaveQuality(StardewValley.Object obj)
        {
            if (obj.QualifiedItemId is "(O)812" or "812" or "(O)814" or "814" or "(O)447" or "447")
                return true; // Roe, Squid Ink, Aged Roe

            return obj.Category is StardewValley.Object.FishCategory
                or StardewValley.Object.EggCategory
                or StardewValley.Object.MilkCategory
                or StardewValley.Object.meatCategory
                or StardewValley.Object.VegetableCategory
                or StardewValley.Object.FruitsCategory
                or StardewValley.Object.flowersCategory
                or StardewValley.Object.GreensCategory;
        }
    }
}
