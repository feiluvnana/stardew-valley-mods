using System;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

// ============================================================================
// FishingPatches improves FISHING treasure chests (the chest you reel in on
// your fishing line). It uses Harmony — a library that injects extra code
// into existing game methods at runtime — so the mod never edits game files.
// When the vanilla code opens a treasure chest menu, our "postfix" (code
// that runs AFTER the original method) hands the chest to
// FishingRewardManager, which tops it up with extra rolls of useful loot.
// Key concept demonstrated: the Harmony Apply + Postfix patching pattern.
// ============================================================================
namespace BetterChest
{
    /// <summary>
    /// Registers and hosts the Harmony patch on
    /// <see cref="FishingRod.openTreasureMenuEndFunction"/>, the method the game
    /// calls when the player successfully reels in a fishing treasure chest.
    /// </summary>
    public static class FishingPatches
    {
        /// <summary>
        /// Wires this class's postfix patch into the game. Called once at startup
        /// from <see cref="ModEntry.Entry"/>.
        /// </summary>
        /// <param name="harmony">The shared Harmony instance created by the mod entry point.</param>
        public static void Apply(Harmony harmony)
        {
            try
            {
                // harmony.Patch(...) modifies a game method AT RUNTIME:
                //   original -> the game method we hook. AccessTools.Method finds it via
                //              reflection (typeof = the FishingRod class, nameof = the
                //              method's name), giving a compile-checked name instead of
                //              a typo-prone string.
                //   postfix  -> OUR method that runs AFTER the original finishes.
                //              (A "prefix" would run BEFORE it instead.)
                harmony.Patch(
                    original: AccessTools.Method(typeof(FishingRod), nameof(FishingRod.openTreasureMenuEndFunction)),
                    postfix: new HarmonyMethod(typeof(FishingPatches), nameof(OpenTreasureMenuEndFunction_Postfix))
                );
            }
            catch (Exception ex)
            {
                // If a game update renames/removes the target method, patching throws.
                // We log it ($"..." = string interpolation, embedding variables in text)
                // and keep playing instead of crashing the game.
                ModEntry.ModMonitor.Log($"Failed to patch FishingRod.openTreasureMenuEndFunction: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Runs after the vanilla treasure-chest menu opens, adding the mod's bonus
        /// loot when the player has enabled the fishing chest buff in config.
        /// </summary>
        /// <param name="__instance">
        /// Harmony special parameter: automatically filled with the FishingRod
        /// object whose patched method was called (the rod the player fished with).
        /// </param>
        public static void OpenTreasureMenuEndFunction_Postfix(FishingRod __instance)
        {
            try
            {
                // Early exit if the user turned this feature off in config.
                if (!ModEntry.Config.EnableFishingChestBuff)
                    return;

                // Pattern matching: "x is Type y" tests whether the menu currently on
                // screen is an ItemGrabMenu AND casts it into grabMenu in one step.
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
