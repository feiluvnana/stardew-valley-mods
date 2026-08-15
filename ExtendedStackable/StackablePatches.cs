using System;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;

namespace ExtendedStackable
{
    public static class StackablePatches
    {
        private static ModConfig Config = null!;
        private static IMonitor Monitor = null!;

        public static void Apply(IModRegistry modRegistry, string uniqueId, IMonitor monitor, ModConfig config)
        {
            Config = config;
            Monitor = monitor;

            var harmony = new Harmony(uniqueId);

            try
            {
                // Patch maximumStackSize
                PatchMaxStackSize(harmony, typeof(StardewValley.Object));
                PatchMaxStackSize(harmony, typeof(Ring));
                PatchMaxStackSize(harmony, typeof(Clothing));
                PatchMaxStackSize(harmony, typeof(Hat));
                PatchMaxStackSize(harmony, typeof(Boots));
                PatchMaxStackSize(harmony, typeof(Furniture));
                PatchMaxStackSize(harmony, typeof(Trinket));

                // Patch canStackWith
                PatchCanStackWith(harmony, typeof(StardewValley.Object));
                PatchCanStackWith(harmony, typeof(Ring));
                PatchCanStackWith(harmony, typeof(Clothing));
                PatchCanStackWith(harmony, typeof(Hat));
                PatchCanStackWith(harmony, typeof(Boots));
                PatchCanStackWith(harmony, typeof(Furniture));
                PatchCanStackWith(harmony, typeof(Trinket));

                // Patch getOne
                PatchGetOne(harmony, typeof(StardewValley.Object));
                PatchGetOne(harmony, typeof(Ring));
                PatchGetOne(harmony, typeof(Clothing));
                PatchGetOne(harmony, typeof(Hat));
                PatchGetOne(harmony, typeof(Boots));
                PatchGetOne(harmony, typeof(Furniture));
                PatchGetOne(harmony, typeof(Trinket));

                Monitor.Log("Harmony patches for ExtendedStackable applied successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply ExtendedStackable harmony patches: {ex}", LogLevel.Error);
            }
        }

        private static void PatchMaxStackSize(Harmony harmony, Type type)
        {
            var method = type.GetMethod(nameof(Item.maximumStackSize), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (method != null && !method.IsAbstract)
            {
                harmony.Patch(
                    original: method,
                    postfix: new HarmonyMethod(typeof(StackablePatches), nameof(Item_maximumStackSize_Postfix))
                );
            }
        }

        private static void PatchCanStackWith(Harmony harmony, Type type)
        {
            var method = type.GetMethod(nameof(Item.canStackWith), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new[] { typeof(ISalable) }, null);
            if (method != null && !method.IsAbstract)
            {
                harmony.Patch(
                    original: method,
                    postfix: new HarmonyMethod(typeof(StackablePatches), nameof(Item_canStackWith_Postfix))
                );
            }
        }

        private static void PatchGetOne(Harmony harmony, Type type)
        {
            var method = type.GetMethod(nameof(Item.getOne), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (method != null && !method.IsAbstract)
            {
                harmony.Patch(
                    original: method,
                    postfix: new HarmonyMethod(typeof(StackablePatches), nameof(Item_getOne_Postfix))
                );
            }
        }

        public static void Item_getOne_Postfix(Item __instance, ref Item __result)
        {
            if (__instance == null || __result == null)
                return;

            if (__instance is Trinket sourceTrinket && __result is Trinket resultTrinket)
            {
                resultTrinket.generationSeed.Value = sourceTrinket.generationSeed.Value;
                resultTrinket.displayNameOverrideTemplate.Value = sourceTrinket.displayNameOverrideTemplate.Value;
            }
            else if (__instance is Boots sourceBoots && __result is Boots resultBoots)
            {
                resultBoots.appliedBootSheetIndex.Value = sourceBoots.appliedBootSheetIndex.Value;
                resultBoots.defenseBonus.Value = sourceBoots.defenseBonus.Value;
                resultBoots.immunityBonus.Value = sourceBoots.immunityBonus.Value;
            }
            else if (__instance is StardewValley.Object sourceObj && __result is StardewValley.Object resultObj)
            {
                if (sourceObj.Category == StardewValley.Object.tackleCategory)
                {
                    resultObj.uses.Value = sourceObj.uses.Value;
                }
            }
        }

        public static void Item_maximumStackSize_Postfix(Item __instance, ref int __result)
        {
            if (__instance == null)
                return;

            if (Config.EnableTrinketStacking && __instance is Trinket)
            {
                __result = Config.MaxStackSize;
            }
            else if (Config.EnableRingStacking && __instance is Ring)
            {
                __result = Config.MaxStackSize;
            }
            else if (Config.EnableClothingAndHatStacking && (__instance is Clothing || __instance is Hat))
            {
                __result = Config.MaxStackSize;
            }
            else if (Config.EnableBootsStacking && __instance is Boots)
            {
                __result = Config.MaxStackSize;
            }
            else if (Config.EnableFurnitureStacking && __instance is Furniture)
            {
                __result = Config.MaxStackSize;
            }
            else if (Config.EnableTackleStacking && __instance is StardewValley.Object obj && obj.Category == StardewValley.Object.tackleCategory)
            {
                __result = Config.MaxStackSize;
            }
        }

        public static void Item_canStackWith_Postfix(Item __instance, ISalable other, ref bool __result)
        {
            if (__instance == null || other == null)
                return;

            if (__result)
                return;

            if (__instance.GetType() != other.GetType())
                return;

            if (Config.EnableTrinketStacking && __instance is Trinket thisTrinket && other is Trinket otherTrinket)
            {
                if (thisTrinket.QualifiedItemId == otherTrinket.QualifiedItemId &&
                    thisTrinket.generationSeed.Value == otherTrinket.generationSeed.Value &&
                    thisTrinket.displayNameOverrideTemplate.Value == otherTrinket.displayNameOverrideTemplate.Value &&
                    thisTrinket.getDescription() == otherTrinket.getDescription())
                {
                    __result = true;
                }
            }
            else if (Config.EnableRingStacking && __instance is Ring thisRing && other is Ring otherRing)
            {
                if (thisRing is CombinedRing thisCombined && otherRing is CombinedRing otherCombined)
                {
                    if (thisCombined.combinedRings.Count == otherCombined.combinedRings.Count)
                    {
                        bool match = true;
                        for (int i = 0; i < thisCombined.combinedRings.Count; i++)
                        {
                            if (thisCombined.combinedRings[i].QualifiedItemId != otherCombined.combinedRings[i].QualifiedItemId)
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match)
                        {
                            __result = true;
                        }
                    }
                }
                else if (thisRing is not CombinedRing && otherRing is not CombinedRing)
                {
                    if (thisRing.QualifiedItemId == otherRing.QualifiedItemId)
                    {
                        __result = true;
                    }
                }
            }
            else if (Config.EnableClothingAndHatStacking && __instance is Clothing thisClothing && other is Clothing otherClothing)
            {
                if (thisClothing.QualifiedItemId == mechanicalQualifiedId(otherClothing) &&
                    thisClothing.clothesColor.Value == otherClothing.clothesColor.Value &&
                    thisClothing.dyeable.Value == otherClothing.dyeable.Value &&
                    thisClothing.Price == otherClothing.Price)
                {
                    __result = true;
                }
            }
            else if (Config.EnableClothingAndHatStacking && __instance is Hat thisHat && other is Hat otherHat)
            {
                if (thisHat.QualifiedItemId == otherHat.QualifiedItemId &&
                    thisHat.ignoreHairstyleOffset.Value == otherHat.ignoreHairstyleOffset.Value &&
                    thisHat.hairDrawType.Value == otherHat.hairDrawType.Value &&
                    thisHat.isPrismatic.Value == otherHat.isPrismatic.Value)
                {
                    __result = true;
                }
            }
            else if (Config.EnableBootsStacking && __instance is Boots thisBoots && other is Boots otherBoots)
            {
                if (thisBoots.QualifiedItemId == otherBoots.QualifiedItemId &&
                    thisBoots.appliedBootSheetIndex.Value == otherBoots.appliedBootSheetIndex.Value &&
                    thisBoots.defenseBonus.Value == otherBoots.defenseBonus.Value &&
                    thisBoots.immunityBonus.Value == otherBoots.immunityBonus.Value)
                {
                    __result = true;
                }
            }
            else if (Config.EnableFurnitureStacking && __instance is Furniture thisFurn && other is Furniture otherFurn)
            {
                if (thisFurn.QualifiedItemId == otherFurn.QualifiedItemId &&
                    thisFurn.currentRotation.Value == otherFurn.currentRotation.Value &&
                    thisFurn.furniture_type.Value == otherFurn.furniture_type.Value)
                {
                    __result = true;
                }
            }
            else if (Config.EnableTackleStacking && __instance is StardewValley.Object thisObj && other is StardewValley.Object otherObj)
            {
                if (thisObj.Category == StardewValley.Object.tackleCategory &&
                    otherObj.Category == StardewValley.Object.tackleCategory &&
                    thisObj.QualifiedItemId == otherObj.QualifiedItemId &&
                    thisObj.Quality == otherObj.Quality &&
                    thisObj.uses.Value == otherObj.uses.Value)
                {
                    __result = true;
                }
            }
        }

        private static string mechanicalQualifiedId(Clothing clothing) => clothing.QualifiedItemId;
    }
}