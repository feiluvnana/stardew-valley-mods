using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Tools;

namespace BetterFishing
{
    /// <summary>
    /// Harmony patches on <see cref="FishingRod.pullFishFromWater"/> to balance
    /// fishing experience (EXP) for apex and legendary fish.
    /// </summary>
    public static class FishingExpPatches
    {
        /// <summary>
        /// Applies the prefix patch to FishingRod.pullFishFromWater.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                var targetMethod = AccessTools.DeclaredMethod(typeof(FishingRod), nameof(FishingRod.pullFishFromWater));

                if (targetMethod != null)
                {
                    harmony.Patch(
                        original: targetMethod,
                        prefix: new HarmonyMethod(typeof(FishingExpPatches), nameof(PullFishFromWater_Prefix))
                    );
                    ModEntry.ModMonitor.Log("Hooked FishingRod.pullFishFromWater with balanced EXP prefix.", LogLevel.Trace);
                }
                else
                {
                    ModEntry.ModMonitor.Log("Could not find FishingRod.pullFishFromWater matching signature.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed to patch FishingRod.pullFishFromWater: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Harmony Prefix for FishingRod.pullFishFromWater.
        /// Adjusts fishDifficulty to provide balanced EXP bonus for apex (>=85) and legendary fish.
        /// </summary>
        public static void PullFishFromWater_Prefix(string fishId, ref int fishDifficulty, bool isBossFish)
        {
            try
            {
                if (ModEntry.Config == null || !ModEntry.Config.EnableFishingExpBalancing)
                    return;

                if (isBossFish || FishPriceBalancer.IsLegendaryFish(fishId))
                {
                    fishDifficulty += ModEntry.Config.LegendaryFishExpBonus * 3;
                }
                else if (fishDifficulty >= 85)
                {
                    fishDifficulty += ModEntry.Config.ApexFishExpBonus * 3;
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error in FishingRod.pullFishFromWater prefix: {ex}", LogLevel.Error);
            }
        }
    }
}

