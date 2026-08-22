using System;
using HarmonyLib;
using StardewValley;
using StardewValley.GameData.Machines;

namespace BetterGeodeCracking
{
    public static class GeodeCrusherPatches
    {
        public static void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.OutputGeodeCrusher)),
                postfix: new HarmonyMethod(typeof(GeodeCrusherPatches), nameof(OutputGeodeCrusher_Postfix))
            );
        }

        public static void OutputGeodeCrusher_Postfix(StardewValley.Object machine, Item inputItem, bool probe, ref Item? __result)
        {
            if (ModEntry.Config.AllowSpecialGeodesInCrusher && inputItem != null)
            {
                // Ensure the first cracked Golden Coconut yields the Golden Helmet
                if (inputItem.QualifiedItemId == "(O)791" && !Game1.netWorldState.Value.GoldenCoconutCracked)
                {
                    if (!probe)
                    {
                        Game1.netWorldState.Value.GoldenCoconutCracked = true;
                    }
                    __result = ItemRegistry.Create("(O)73");
                }
            }
        }
    }
}
