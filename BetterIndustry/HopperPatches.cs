using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace BetterIndustry
{
    public static class HopperPatches
    {
        private static IMonitor Monitor = null!;

        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            Monitor = monitor;

            try
            {
                // Patch Chest.CheckAutoLoad
                var checkAutoLoadMethod = AccessTools.Method(typeof(Chest), nameof(Chest.CheckAutoLoad), new[] { typeof(Farmer) });
                if (checkAutoLoadMethod != null)
                {
                    harmony.Patch(
                        original: checkAutoLoadMethod,
                        prefix: new HarmonyMethod(typeof(HopperPatches), nameof(Prefix_CheckAutoLoad))
                    );
                }

                // Patch Chest.GetActualCapacity
                var getActualCapacityMethod = AccessTools.Method(typeof(Chest), nameof(Chest.GetActualCapacity));
                if (getActualCapacityMethod != null)
                {
                    harmony.Patch(
                        original: getActualCapacityMethod,
                        postfix: new HarmonyMethod(typeof(HopperPatches), nameof(Postfix_GetActualCapacity))
                    );
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error applying Harmony patches for BetterIndustry Hopper automation: {ex}", LogLevel.Error);
            }
        }

        private static bool Prefix_CheckAutoLoad(Chest __instance, Farmer who)
        {
            try
            {
                if (HopperManager.IsHopper(__instance))
                {
                    HopperManager.ProcessHopper(__instance, __instance.Location, who);
                    return false; // Skip vanilla 1-direction loading
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in CheckAutoLoad patch: {ex}", LogLevel.Error);
            }

            return true;
        }

        private static void Postfix_GetActualCapacity(Chest __instance, ref int __result)
        {
            try
            {
                if (HopperManager.IsHopper(__instance) && ModEntry.Config.HopperCapacity > 36)
                {
                    __result = ModEntry.Config.HopperCapacity;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in GetActualCapacity patch: {ex}", LogLevel.Error);
            }
        }
    }
}
