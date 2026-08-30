using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Tools;
using System.Reflection;
using System.Reflection.Emit;

namespace BetterFishing
{
    /// <summary>
    /// Harmony patches on <see cref="FishingRod.doPullFishFromWater"/> to balance
    /// fishing experience (EXP) for apex and legendary fish.
    /// </summary>
    public static class FishingExpPatches
    {
        /// <summary>
        /// Applies the transpiler patch to FishingRod.pullFishFromWater.
        /// </summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                var targetMethod = AccessTools.Method(
                    typeof(FishingRod),
                    nameof(FishingRod.pullFishFromWater),
                    new[]
                    {
                        typeof(string), // fishId
                        typeof(int),    // fishSize
                        typeof(int),    // fishQuality
                        typeof(int),    // fishDifficulty
                        typeof(bool),   // treasureCaught
                        typeof(bool),   // wasPerfect
                        typeof(bool),   // fromFishPond
                        typeof(bool),   // setFlagOnCatch
                        typeof(bool),   // isBossFish
                        typeof(int)     // numCatch
                    }
                );

                if (targetMethod != null)
                {
                    harmony.Patch(
                        original: targetMethod,
                        transpiler: new HarmonyMethod(typeof(FishingExpPatches), nameof(PullFishFromWater_Transpiler))
                    );
                    ModEntry.ModMonitor.Log("Hooked FishingRod.pullFishFromWater with balanced EXP transpiler.", LogLevel.Trace);
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
        /// Harmony Transpiler for FishingRod.pullFishFromWater.
        /// Replaces the vanilla `fishDifficulty / 3` division with `ComputeDifficultyExp(fishDifficulty, isBossFish, fishId)`.
        /// </summary>
        public static IEnumerable<CodeInstruction> PullFishFromWater_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var codes = new List<CodeInstruction>(instructions);
            var parameters = original.GetParameters();

            // Find parameter positions
            int isBossFishIndex = -1;
            int fishIdIndex = -1;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Name == "isBossFish")
                    isBossFishIndex = i + 1; // +1 for 'this' instance
                else if (parameters[i].Name == "fishId")
                    fishIdIndex = i + 1;
            }

            // Fallback indices if parameter names differ
            if (isBossFishIndex == -1) isBossFishIndex = 9;
            if (fishIdIndex == -1) fishIdIndex = 1;

            bool patched = false;
            for (int i = 0; i < codes.Count; i++)
            {
                // Match ldarg fishDifficulty followed by ldc.i4.3 followed by div
                if (!patched && i + 1 < codes.Count &&
                    codes[i].opcode == OpCodes.Ldc_I4_3 &&
                    codes[i + 1].opcode == OpCodes.Div &&
                    i > 0 && codes[i - 1].opcode == OpCodes.Ldarg_S &&
                    codes[i - 1].operand is byte argIdx && argIdx == 4) // fishDifficulty is arg 4 (index 3 + 1 for 'this')
                {
                    // Replace ldc.i4.3 with ldarg for isBossFish
                    codes[i].opcode = OpCodes.Ldarg_S;
                    codes[i].operand = (byte)isBossFishIndex;

                    // Insert ldarg for fishId
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_S, (byte)fishIdIndex));

                    // Replace div with call to ComputeDifficultyExp
                    codes[i + 2].opcode = OpCodes.Call;
                    codes[i + 2].operand = AccessTools.Method(typeof(FishingExpPatches), nameof(ComputeDifficultyExp));

                    patched = true;
                    i += 2;
                }
            }

            foreach (var code in codes)
            {
                yield return code;
            }
        }

        /// <summary>
        /// Computes the difficulty component of fishing experience with targeted apex and legendary bonuses.
        /// </summary>
        public static int ComputeDifficultyExp(int difficulty, bool isBossFish, string fishId)
        {
            int exp = difficulty / 3;

            if (ModEntry.Config != null && ModEntry.Config.EnableFishingExpBalancing)
            {
                if (isBossFish || FishPriceBalancer.IsLegendaryFish(fishId))
                {
                    exp += ModEntry.Config.LegendaryFishExpBonus;
                }
                else if (difficulty >= 85)
                {
                    exp += ModEntry.Config.ApexFishExpBonus;
                }
            }

            return exp;
        }
    }
}
