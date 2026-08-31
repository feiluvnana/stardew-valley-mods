using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
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
        /// Applies the postfix patch to FishingRod.pullFishFromWater.
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
                        postfix: new HarmonyMethod(typeof(FishingExpPatches), nameof(PullFishFromWater_Postfix))
                    );
                    ModEntry.ModMonitor.Log("Hooked FishingRod.pullFishFromWater with balanced EXP postfix.", LogLevel.Trace);
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
        /// Harmony Postfix for FishingRod.pullFishFromWater.
        /// Directly awards bonus EXP for apex (>=85) and legendary fish without side-effects on fishDifficulty.
        /// </summary>
        public static void PullFishFromWater_Postfix(FishingRod __instance, string fishId, int fishDifficulty, bool isBossFish, bool fromFishPond)
        {
            try
            {
                if (fromFishPond || ModEntry.Config == null || !ModEntry.Config.EnableFishingExpBalancing)
                    return;

                Farmer who = __instance.getLastFarmerToUse();
                if (who == null)
                    return;

                if (isBossFish || FishPriceBalancer.IsLegendaryFish(fishId))
                {
                    who.gainExperience(Farmer.fishingSkill, ModEntry.Config.LegendaryFishExpBonus);
                }
                else if (fishDifficulty >= 85)
                {
                    who.gainExperience(Farmer.fishingSkill, ModEntry.Config.ApexFishExpBonus);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error in FishingRod.pullFishFromWater postfix: {ex}", LogLevel.Error);
            }
        }
    }
}
