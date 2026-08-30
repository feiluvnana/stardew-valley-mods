using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;

namespace BetterQOL.Transparency
{
    /// <summary>
    /// Harmony patches injecting dynamic transparency logic into buildings, trees, bushes, grass, crops, and objects.
    /// </summary>
    public static class TransparencyPatches
    {
        private static IMonitor Monitor = null!;
        private static IModHelper Helper = null!;

        /// <summary>
        /// Registers all transparency Harmony patches.
        /// </summary>
        public static void Apply(Harmony harmony, IMonitor monitor, IModHelper helper)
        {
            Monitor = monitor;
            Helper = helper;

            try
            {
                // 1. Building transparency
                harmony.Patch(
                    original: AccessTools.Method(typeof(Building), nameof(Building.Update), new[] { typeof(GameTime) }),
                    postfix: new HarmonyMethod(typeof(TransparencyPatches), nameof(Building_Update_Postfix))
                );

                // 2. Bush transparency
                harmony.Patch(
                    original: AccessTools.Method(typeof(Bush), nameof(Bush.tickUpdate), new[] { typeof(GameTime) }),
                    postfix: new HarmonyMethod(typeof(TransparencyPatches), nameof(Bush_tickUpdate_Postfix))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(Bush), nameof(Bush.draw), new[] { typeof(SpriteBatch) }),
                    transpiler: new HarmonyMethod(typeof(TransparencyPatches), nameof(Bush_draw_Transpiler))
                );

                // 3. Tree transparency
                harmony.Patch(
                    original: AccessTools.Method(typeof(Tree), nameof(Tree.tickUpdate), new[] { typeof(GameTime) }),
                    postfix: new HarmonyMethod(typeof(TransparencyPatches), nameof(Tree_tickUpdate_Postfix))
                );

                // 4. FruitTree transparency
                harmony.Patch(
                    original: AccessTools.Method(typeof(FruitTree), nameof(FruitTree.tickUpdate), new[] { typeof(GameTime) }),
                    postfix: new HarmonyMethod(typeof(TransparencyPatches), nameof(FruitTree_tickUpdate_Postfix))
                );

                // 5. Grass transparency
                harmony.Patch(
                    original: AccessTools.Method(typeof(Grass), nameof(Grass.draw), new[] { typeof(SpriteBatch) }),
                    transpiler: new HarmonyMethod(typeof(TransparencyPatches), nameof(Grass_draw_Transpiler))
                );

                // 6. Crop transparency
                harmony.Patch(
                    original: AccessTools.Method(typeof(Crop), nameof(Crop.draw), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(Color), typeof(float) }),
                    transpiler: new HarmonyMethod(typeof(TransparencyPatches), nameof(Crop_draw_Transpiler))
                );
                harmony.Patch(
                    original: AccessTools.Method(typeof(Crop), nameof(Crop.drawWithOffset), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(Color), typeof(float), typeof(Vector2) }),
                    transpiler: new HarmonyMethod(typeof(TransparencyPatches), nameof(Crop_drawWithOffset_Transpiler))
                );

                // 7. Object & BigCraftable transparency
                harmony.Patch(
                    original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                    prefix: new HarmonyMethod(typeof(TransparencyPatches), nameof(Object_draw_Prefix))
                );

                Monitor.Log("TransparencyPatches applied successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply transparency patches: {ex}", LogLevel.Error);
            }
        }

        // ---------------- Building Patch ----------------
        private static void Building_Update_Postfix(Building __instance)
        {
            try
            {
                if (!ModEntry.Config.EnableTransparency || !ModEntry.Config.EnableBuildingTransparency || !__instance.fadeWhenPlayerIsBehind.Value)
                    return;

                IReflectedField<float> alphaField = Helper.Reflection.GetField<float>(__instance, "alpha");
                if (TransparencyManager.ShouldBeTransparent(__instance))
                {
                    alphaField.SetValue(TransparencyManager.GetAlpha(__instance, -0.05f, ModEntry.Config.BuildingMinimumOpacity));
                }
                else
                {
                    alphaField.SetValue(TransparencyManager.GetAlpha(__instance, 0.05f, ModEntry.Config.BuildingMinimumOpacity));
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Error in Building_Update_Postfix: {ex}", LogLevel.Error);
            }
        }

        // ---------------- Bush Patches ----------------
        private static void Bush_tickUpdate_Postfix(Bush __instance)
        {
            try
            {
                if (!ModEntry.Config.EnableTransparency || !ModEntry.Config.EnableBushTransparency || __instance.size.Value == 3)
                    return;

                if (TransparencyManager.ShouldBeTransparent(__instance))
                {
                    TransparencyManager.GetAlpha(__instance, -0.05f, ModEntry.Config.BushMinimumOpacity);
                }
                else
                {
                    TransparencyManager.GetAlpha(__instance, 0.05f, ModEntry.Config.BushMinimumOpacity);
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Error in Bush_tickUpdate_Postfix: {ex}", LogLevel.Error);
            }
        }

        private static IEnumerable<CodeInstruction> Bush_draw_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var list = new List<CodeInstruction>(instructions);
                MethodInfo applyMethod = AccessTools.Method(typeof(TransparencyPatches), nameof(ApplyBushTransparency));

                for (int i = 0; i < list.Count - 2; i++)
                {
                    if (list[i].opcode != OpCodes.Call && list[i].opcode != OpCodes.Callvirt)
                        continue;

                    object? operand = list[i].operand;
                    if (operand == null || !operand.ToString()!.Contains("Color", StringComparison.Ordinal))
                        continue;

                    if (list[i + 1].opcode == OpCodes.Ldarg_0 && list[i + 2].opcode == OpCodes.Ldfld)
                    {
                        object? fieldOperand = list[i + 2].operand;
                        if (fieldOperand != null && fieldOperand.ToString()!.Contains("shakeRotation", StringComparison.Ordinal))
                        {
                            list.InsertRange(i + 1, new[]
                            {
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Call, applyMethod)
                            });
                            break;
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Error transpiling Bush.draw: {ex}", LogLevel.Error);
                return instructions;
            }
        }

        public static Color ApplyBushTransparency(Color originalColor, object instance)
        {
            if (!ModEntry.Config.EnableTransparency || !ModEntry.Config.EnableBushTransparency)
                return originalColor;

            return originalColor * TransparencyManager.GetAlpha(instance, 0f, ModEntry.Config.BushMinimumOpacity);
        }

        // ---------------- Tree & FruitTree Patches ----------------
        private static void Tree_tickUpdate_Postfix(Tree __instance)
        {
            try
            {
                if (!ModEntry.Config.EnableTransparency || !ModEntry.Config.EnableTreeTransparency || __instance.growthStage.Value < 5 || __instance.stump.Value)
                    return;

                if (TransparencyManager.ShouldBeTransparent(__instance))
                {
                    __instance.alpha = TransparencyManager.GetAlpha(__instance, -0.05f, ModEntry.Config.TreeMinimumOpacity);
                }
                else
                {
                    __instance.alpha = TransparencyManager.GetAlpha(__instance, 0.05f, ModEntry.Config.TreeMinimumOpacity);
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Error in Tree_tickUpdate_Postfix: {ex}", LogLevel.Error);
            }
        }

        private static void FruitTree_tickUpdate_Postfix(FruitTree __instance)
        {
            try
            {
                if (!ModEntry.Config.EnableTransparency || !ModEntry.Config.EnableTreeTransparency || __instance.growthStage.Value < 4 || __instance.stump.Value)
                    return;

                if (TransparencyManager.ShouldBeTransparent(__instance))
                {
                    __instance.alpha = TransparencyManager.GetAlpha(__instance, -0.05f, ModEntry.Config.TreeMinimumOpacity);
                }
                else
                {
                    __instance.alpha = TransparencyManager.GetAlpha(__instance, 0.05f, ModEntry.Config.TreeMinimumOpacity);
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Error in FruitTree_tickUpdate_Postfix: {ex}", LogLevel.Error);
            }
        }

        // ---------------- Grass Patches ----------------
        private static IEnumerable<CodeInstruction> Grass_draw_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var list = new List<CodeInstruction>(instructions);
                MethodInfo applyMethod = AccessTools.Method(typeof(TransparencyPatches), nameof(ApplyGrassTransparency));

                for (int i = 0; i < list.Count - 2; i++)
                {
                    if (list[i].opcode != OpCodes.Call && list[i].opcode != OpCodes.Callvirt)
                        continue;

                    object? operand = list[i].operand;
                    if (operand == null || !operand.ToString()!.Contains("Color", StringComparison.Ordinal))
                        continue;

                    if (list[i + 1].opcode == OpCodes.Ldarg_0 && list[i + 2].opcode == OpCodes.Ldfld)
                    {
                        object? fieldOperand = list[i + 2].operand;
                        if (fieldOperand != null && fieldOperand.ToString()!.Contains("shakeRotation", StringComparison.Ordinal))
                        {
                            list.InsertRange(i + 1, new[]
                            {
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Call, applyMethod)
                            });
                            break;
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Error transpiling Grass.draw: {ex}", LogLevel.Error);
                return instructions;
            }
        }

        public static Color ApplyGrassTransparency(Color originalColor, object instance)
        {
            if (!ModEntry.Config.EnableTransparency || !ModEntry.Config.EnableGrassTransparency)
                return originalColor;

            return originalColor * TransparencyManager.GetAlpha(instance, 0f, ModEntry.Config.GrassMinimumOpacity);
        }

        // ---------------- Crop Patches ----------------
        private static IEnumerable<CodeInstruction> Crop_draw_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var list = new List<CodeInstruction>(instructions);
                MethodInfo applyMethod = AccessTools.Method(typeof(TransparencyPatches), nameof(ApplyCropTransparency));

                for (int i = 0; i < list.Count - 1; i++)
                {
                    if ((list[i].opcode == OpCodes.Ldarg_3 || list[i].opcode == OpCodes.Ldloc_3) &&
                        list[i + 1].opcode == OpCodes.Ldarg_S && list[i + 1].operand?.ToString() == "4")
                    {
                        list.InsertRange(i + 1, new[]
                        {
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Call, applyMethod)
                        });
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Error transpiling Crop.draw: {ex}", LogLevel.Error);
                return instructions;
            }
        }

        private static IEnumerable<CodeInstruction> Crop_drawWithOffset_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var list = new List<CodeInstruction>(instructions);
                MethodInfo applyMethod = AccessTools.Method(typeof(TransparencyPatches), nameof(ApplyCropTransparency));

                for (int i = 0; i < list.Count - 1; i++)
                {
                    if (list[i].opcode != OpCodes.Ldarg_3)
                    {
                        if (list[i].opcode != OpCodes.Callvirt)
                            continue;

                        object? operand = list[i].operand;
                        if (operand == null || !operand.ToString()!.Contains("get_Value", StringComparison.Ordinal))
                            continue;
                    }

                    if (list[i + 1].opcode == OpCodes.Ldarg_S && list[i + 1].operand?.ToString() == "4")
                    {
                        list.InsertRange(i + 1, new[]
                        {
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Call, applyMethod)
                        });
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Error transpiling Crop.drawWithOffset: {ex}", LogLevel.Error);
                return instructions;
            }
        }

        public static Color ApplyCropTransparency(Color originalColor, object instance)
        {
            if (!ModEntry.Config.EnableTransparency || !ModEntry.Config.EnableCropTransparency)
                return originalColor;

            return originalColor * TransparencyManager.GetAlpha(instance, 0f, ModEntry.Config.CropMinimumOpacity);
        }

        // ---------------- Object & BigCraftable Patch ----------------
        private static void Object_draw_Prefix(StardewValley.Object __instance, int x, int y, ref float alpha)
        {
            try
            {
                if (!ModEntry.Config.EnableTransparency)
                    return;

                bool isBigCraftable = __instance.bigCraftable.Value;
                bool isEnabled = isBigCraftable ? ModEntry.Config.EnableCraftableTransparency : ModEntry.Config.EnableObjectTransparency;

                if (!isEnabled || TransparencyManager.DisableTransparency.Value)
                    return;

                float minOpacity = isBigCraftable ? ModEntry.Config.CraftableMinimumOpacity : ModEntry.Config.ObjectMinimumOpacity;

                if (TransparencyManager.ShouldBeTransparent(__instance, x, y, isBigCraftable))
                {
                    alpha = TransparencyManager.GetAlpha(__instance, -0.05f, minOpacity);
                }
                else
                {
                    alpha = TransparencyManager.GetAlpha(__instance, 0.05f, minOpacity);
                }
            }
            catch (Exception ex)
            {
                Monitor.LogOnce($"Error in Object_draw_Prefix: {ex}", LogLevel.Error);
            }
        }
    }
}
