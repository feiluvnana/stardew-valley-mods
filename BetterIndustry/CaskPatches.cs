using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Objects;

namespace BetterIndustry
{
    /// <summary>
    /// Harmony patches on <see cref="Cask"/> to allow casks to age artisan goods outside the cellar.
    /// </summary>
    public static class CaskPatches
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Applies Harmony patches to Cask.IsValidCaskLocation.
        /// </summary>
        /// <param name="harmony">Harmony instance.</param>
        public static void Apply(Harmony harmony)
        {
            try
            {
                var method = AccessTools.Method(typeof(Cask), nameof(Cask.IsValidCaskLocation));
                if (method != null)
                {
                    harmony.Patch(
                        original: method,
                        postfix: new HarmonyMethod(typeof(CaskPatches), nameof(IsValidCaskLocation_Postfix))
                    );
                    Monitor.Log("Hooked Cask.IsValidCaskLocation successfully.", LogLevel.Trace);
                }
                else
                {
                    Monitor.Log("Could not locate Cask.IsValidCaskLocation method.", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply CaskPatches: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Postfix on Cask.IsValidCaskLocation: returns true when casks are allowed outside the cellar.
        /// </summary>
        public static void IsValidCaskLocation_Postfix(Cask __instance, ref bool __result)
        {
            if (__result)
                return;

            if (Config.EnableCasksEverywhere)
            {
                __result = true;
            }
        }
    }
}
