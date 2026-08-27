using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Tools;
using System.Reflection.Emit;

namespace BetterFishing
{
    /// <summary>
    /// Harmony patches on <see cref="FishingRod.openTreasureMenuEndFunction"/> to customize
    /// decaying roll rates for standard and golden fishing treasure chests.
    /// </summary>
    public static class FishingChestPatches
    {
        /// <summary>
        /// Applies the transpiler patch to FishingRod.openTreasureMenuEndFunction.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(FishingRod), nameof(FishingRod.openTreasureMenuEndFunction)),
                    transpiler: new HarmonyMethod(typeof(FishingChestPatches), nameof(OpenTreasureMenuEndFunction_Transpiler))
                );
                ModEntry.ModMonitor.Log("Hooked FishingRod.openTreasureMenuEndFunction with decaying roll transpiler.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed to patch FishingRod.openTreasureMenuEndFunction: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Harmony Transpiler for FishingRod.openTreasureMenuEndFunction.
        /// Replaces hardcoded float constants (0.4f for standard chests, 0.6f for golden chests)
        /// with dynamic helper calls returning the user-configured decay rates.
        /// </summary>
        public static IEnumerable<CodeInstruction> OpenTreasureMenuEndFunction_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float val)
                {
                    // Standard fishing chest decay rate (vanilla 0.40f -> mod default 0.60f)
                    if (Math.Abs(val - 0.4f) < 0.001f)
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(FishingChestPatches), nameof(GetFishingChestDecayRate));
                    }
                    // Golden fishing chest decay rate (vanilla 0.60f -> mod default 0.80f)
                    else if (Math.Abs(val - 0.6f) < 0.001f)
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = AccessTools.Method(typeof(FishingChestPatches), nameof(GetGoldenChestDecayRate));
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
