// "using" directives import other libraries' namespaces so short names work:
//   HarmonyLib          -> the Harmony patching library (Patch, Transpiler...)
//   StardewModdingAPI   -> SMAPI: logging types (IMonitor, LogLevel)
//   StardewValley       -> core game code
//   StardewValley.Tools -> tools the player holds, e.g. FishingRod
//   System.Reflection.Emit -> CIL OpCodes (Ldc_R4, Call, etc.)
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Tools;
using System.Reflection.Emit;

// ============================================================================
// FishingPatches tunes FISHING treasure chests (standard chests and 1.6 golden
// chests) by modifying the probability decay multiplier of the vanilla roll loop.
//
// HOW IT WORKS:
// In vanilla Stardew Valley 1.6 (FishingRod.openTreasureMenuEndFunction), chest
// loot is rolled in a decaying loop:
//     while (random <= decayRate) { decayRate *= (golden ? 0.6f : 0.4f); rollItem(); }
//
// Instead of replacing or flooding the chest with flat custom items, this mod
// uses a Harmony TRANSPILER to replace the hardcoded float constants (0.4f and 0.6f)
// with live calls to GetFishingChestDecayRate() and GetGoldenChestDecayRate().
//
// This guarantees:
//   1. 100% vanilla 1.6 loot progression, depth scaling, luck scaling, and mastery checks.
//   2. Clean, balanced multi-rolls without inventory-flooding or game-breaking loot.
//   3. Full GMCM in-game configurability.
// ============================================================================
namespace BetterChest
{
    /// <summary>
    /// Registers and hosts the Harmony transpiler patch on
    /// <see cref="FishingRod.openTreasureMenuEndFunction"/> to customize roll decay rates.
    /// </summary>
    public static class FishingPatches
    {
        /// <summary>
        /// Wires this class's transpiler patch into the game. Called once at startup
        /// from <see cref="ModEntry.Entry"/>.
        /// </summary>
        /// <param name="harmony">The shared Harmony instance created by the mod entry point.</param>
        public static void Apply(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(FishingRod), nameof(FishingRod.openTreasureMenuEndFunction)),
                    transpiler: new HarmonyMethod(typeof(FishingPatches), nameof(OpenTreasureMenuEndFunction_Transpiler))
                );
                ModEntry.ModMonitor.Log("Hooked FishingRod.openTreasureMenuEndFunction with decaying roll transpiler.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed to patch FishingRod.openTreasureMenuEndFunction: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Harmony Transpiler for <c>FishingRod.openTreasureMenuEndFunction</c>.
        /// Replaces the hardcoded decay constants (0.4f for standard chests, 0.6f for golden chests)
        /// with dynamic helper calls returning the user-configured decay rates.
        /// </summary>
        /// <param name="instructions">The original method's CIL instructions.</param>
        /// <returns>Modified CIL instruction stream.</returns>
        public static IEnumerable<CodeInstruction> OpenTreasureMenuEndFunction_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                // Match ldc.r4 constants for 0.4f and 0.6f
                // In-place modification preserves existing labels and blocks (e.g. jump targets from brtrue.s)
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float val)
                {
                    // Standard fishing chest decay rate (vanilla 0.40f -> mod default 0.60f)
                    if (Math.Abs(val - 0.4f) < 0.001f)
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(FishingPatches), nameof(GetFishingChestDecayRate));
                    }
                    // Golden fishing chest decay rate (vanilla 0.60f -> mod default 0.80f)
                    else if (Math.Abs(val - 0.6f) < 0.001f)
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(FishingPatches), nameof(GetGoldenChestDecayRate));
                    }
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Returns the effective decay rate for standard fishing treasure chests.
        /// </summary>
        public static float GetFishingChestDecayRate()
        {
            if (ModEntry.Config != null && ModEntry.Config.EnableFishingChestBuff)
                return ModEntry.Config.FishingChestDecayRate;

            return 0.4f;
        }

        /// <summary>
        /// Returns the effective decay rate for golden fishing treasure chests.
        /// </summary>
        public static float GetGoldenChestDecayRate()
        {
            if (ModEntry.Config != null && ModEntry.Config.EnableFishingChestBuff)
                return ModEntry.Config.GoldenChestDecayRate;

            return 0.6f;
        }
    }
}
