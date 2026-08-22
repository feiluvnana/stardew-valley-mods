using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace BetterChest
{
    public static class FishingPatches
    {
        public static void Apply(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(FishingRod), nameof(FishingRod.openTreasureMenuEndFunction)),
                    postfix: new HarmonyMethod(typeof(FishingPatches), nameof(OpenTreasureMenuEndFunction_Postfix))
                );
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed to patch FishingRod.openTreasureMenuEndFunction: {ex}", LogLevel.Error);
            }
        }

        public static void OpenTreasureMenuEndFunction_Postfix(FishingRod __instance)
        {
            try
            {
                if (!ModEntry.Config.EnableFishingChestBuff)
                    return;

                if (Game1.activeClickableMenu is ItemGrabMenu grabMenu)
                {
                    FishingRewardManager.EnhanceFishingChest(__instance, grabMenu);
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error applying BetterChest fishing treasure enhancements: {ex}", LogLevel.Error);
            }
        }
    }
}
