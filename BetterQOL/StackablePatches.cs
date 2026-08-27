using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Objects.Trinkets;

namespace BetterQOL
{
    /// <summary>
    /// Teaches the game to stack item types vanilla never allows (rings, clothing,
    /// hats, boots, furniture, trinkets, fishing tackle) up to MaxStackSize.
    /// Uses the HARMONY library to bolt extra code onto THREE game methods across
    /// every relevant item subclass:
    ///   maximumStackSize() - how many of this item fit in one inventory slot
    ///   canStackWith()     - may two given items merge into the same slot?
    ///   getOne()           - the game's "make an exact copy" factory method
    /// </summary>
    public static class StackablePatches
    {
        // Expression-bodied properties ("=>"): each read simply forwards to the
        // shared statics published by ModEntry, giving this class tidy shortcuts.
        /// <summary>Shortcut to the live user settings owned by ModEntry.</summary>
        private static ModConfig Config => ModEntry.Config;
        /// <summary>Shortcut to SMAPI's logger owned by ModEntry.</summary>
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Installs all stacking patches; called once from ModEntry.Entry with the
        /// mod's shared Harmony instance. Patches are attached MANUALLY here instead
        /// of via [HarmonyPatch] attributes because the SAME postfix must hook EIGHT
        /// different item classes - looping over types keeps that repetition low.
        /// </summary>
        /// <param name="harmony">Shared patcher instance created in ModEntry.</param>
        /// <param name="monitor">SMAPI logger for diagnostics.</param>
        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            // try/catch is a safety net: if patching fails (game update renamed a
            // method, conflicting mod...), we log the error instead of crashing the
            // whole game during startup. "ex" is the caught exception object.
            try
            {
                // Patch maximumStackSize
                // Each call below attaches our shared postfix to whichever class
                // actually DECLARES the method. typeof(X) fetches a class's runtime
                // description (a System.Type object), which reflection needs.
                PatchMaxStackSize(harmony, typeof(Item));
                PatchMaxStackSize(harmony, typeof(StardewValley.Object));
                PatchMaxStackSize(harmony, typeof(Ring));
                PatchMaxStackSize(harmony, typeof(Clothing));
                PatchMaxStackSize(harmony, typeof(Hat));
                PatchMaxStackSize(harmony, typeof(Boots));
                PatchMaxStackSize(harmony, typeof(Furniture));
                PatchMaxStackSize(harmony, typeof(Trinket));

                // Patch canStackWith
                PatchCanStackWith(harmony, typeof(Item));
                PatchCanStackWith(harmony, typeof(StardewValley.Object));
                PatchCanStackWith(harmony, typeof(Ring));
                PatchCanStackWith(harmony, typeof(Clothing));
                PatchCanStackWith(harmony, typeof(Hat));
                PatchCanStackWith(harmony, typeof(Boots));
                PatchCanStackWith(harmony, typeof(Furniture));
                PatchCanStackWith(harmony, typeof(Trinket));

                // Patch getOne
                PatchGetOne(harmony, typeof(Item));
                PatchGetOne(harmony, typeof(StardewValley.Object));
                PatchGetOne(harmony, typeof(Ring));
                PatchGetOne(harmony, typeof(Clothing));
                PatchGetOne(harmony, typeof(Hat));
                PatchGetOne(harmony, typeof(Boots));
                PatchGetOne(harmony, typeof(Furniture));
                PatchGetOne(harmony, typeof(Trinket));

                monitor.Log("Harmony patches for BetterQOL stackable items applied successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                monitor.Log($"Failed to apply BetterQOL stackable harmony patches: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Attaches Item_maximumStackSize_Postfix to one class's maximumStackSize().
        /// </summary>
        /// <param name="harmony">Shared patcher instance.</param>
        /// <param name="type">Item subclass to patch (located via reflection).</param>
        private static void PatchMaxStackSize(Harmony harmony, Type type)
        {
            // AccessTools is Harmony's reflection helper: search THIS class only for
            // a method named like Item.maximumStackSize ("nameof" gives the name as
            // text but is checked by the compiler, so renames can't silently break).
            var method = AccessTools.DeclaredMethod(type, nameof(Item.maximumStackSize));
            // Abstract methods have no body, so Harmony cannot hook them - skip those.
            if (method != null && !method.IsAbstract)
            {
                // Attach our postfix. Named arguments (postfix:) document intent:
                // our code RUNS AFTER the original and may overwrite its result.
                harmony.Patch(
                    original: method,
                    postfix: new HarmonyMethod(typeof(StackablePatches), nameof(Item_maximumStackSize_Postfix))
                );
            }
        }

        /// <summary>Same idea as PatchMaxStackSize, but for canStackWith(ISalable).</summary>
        /// <param name="harmony">Shared patcher instance.</param>
        /// <param name="type">Item subclass to patch.</param>
        private static void PatchCanStackWith(Harmony harmony, Type type)
        {
            // The extra array pins the overload: methods can share a name with
            // different parameter lists, so we demand the ISalable-parameter one.
            var method = AccessTools.DeclaredMethod(type, nameof(Item.canStackWith), new[] { typeof(ISalable) });
            if (method != null && !method.IsAbstract)
            {
                harmony.Patch(
                    original: method,
                    postfix: new HarmonyMethod(typeof(StackablePatches), nameof(Item_canStackWith_Postfix))
                );
            }
        }

        /// <summary>Same idea again, for getOne() (the clone factory).</summary>
        /// <param name="harmony">Shared patcher instance.</param>
        /// <param name="type">Item subclass to patch.</param>
        private static void PatchGetOne(Harmony harmony, Type type)
        {
            var method = AccessTools.DeclaredMethod(type, nameof(Item.getOne));
            if (method != null && !method.IsAbstract)
            {
                harmony.Patch(
                    original: method,
                    postfix: new HarmonyMethod(typeof(StackablePatches), nameof(Item_getOne_Postfix))
                );
            }
        }

        /// <summary>
        /// Postfix for getOne(), the game's "duplicate this item" method used when a
        /// stack splits. The vanilla clone copies only basics, losing extra state
        /// that made two items IDENTICAL - which would break our canStackWith rules
        /// right after a split. This hook re-copies that state so halves re-merge.
        /// </summary>
        /// <param name="__instance">Harmony-injected: the item getOne() was called on.</param>
        /// <param name="__result">Harmony-injected return value; "ref" lets this code
        /// affect the object CALLERS receive (here we mutate the clone in place).</param>
        public static void Item_getOne_Postfix(Item __instance, ref Item __result)
        {
            // Guard clause: nothing to fix if source or clone is absent.
            if (__instance == null || __result == null)
                return;

            // TRINKETS (1.6 charm-like accessories): copy the random generation seed
            // (which drives stats/appearance), any custom name template, and every
            // modData entry. Identical seeds are exactly what lets trinkets stack.
            if (__instance is Trinket sourceTrinket && __result is Trinket resultTrinket)
            {
                resultTrinket.generationSeed.Value = sourceTrinket.generationSeed.Value;
                resultTrinket.displayNameOverrideTemplate.Value = sourceTrinket.displayNameOverrideTemplate.Value;
                // foreach over a dictionary walks its key/value pairs ("kvp" is the
                // conventional abbreviation); each pair is re-inserted into the clone.
                foreach (var kvp in sourceTrinket.modData.Pairs)
                {
                    resultTrinket.modData[kvp.Key] = kvp.Value;
                }
            }
            else if (__instance is Boots sourceBoots && __result is Boots resultBoots)
            {
                // BOOTS remember upgraded state: which stat sheet was applied plus
                // defense/immunity bonuses gained. Clone those across.
                resultBoots.appliedBootSheetIndex.Value = sourceBoots.appliedBootSheetIndex.Value;
                resultBoots.defenseBonus.Value = sourceBoots.defenseBonus.Value;
                resultBoots.immunityBonus.Value = sourceBoots.immunityBonus.Value;
            }
            else if (__instance is StardewValley.Object sourceObj && __result is StardewValley.Object resultObj)
            {
                // FISHING TACKLE wears out with use ("uses" = remaining durability).
                // Two tackles may only stack when EQUALLY worn, so the clone must
                // inherit the exact remaining-durability counter.
                if (sourceObj.Category == StardewValley.Object.tackleCategory)
                {
                    resultObj.uses.Value = sourceObj.uses.Value;
                }
            }
        }

        /// <summary>
        /// Postfix for maximumStackSize(). The game computed its vanilla answer
        /// (a hardcoded small cap, or zero for unstackables); this hook OVERWRITES
        /// that number whenever the item belongs to a category whose stacking
        /// option is enabled in the mod config.
        /// </summary>
        /// <param name="__instance">Harmony-injected: the item being asked.</param>
        /// <param name="__result">Harmony-injected return value; "ref" lets us replace it.</param>
        public static void Item_maximumStackSize_Postfix(Item __instance, ref int __result)
        {
            if (__instance == null)
                return;

            // A chain of type tests checked top-to-bottom; first match wins.
            // "X is Y" both tests the type and, via PATTERN MATCHING, lets later
            // conditions treat the item AS that specific subclass.
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
                // Tackle isn't its own class: it's a plain Object whose Category
                // equals the game's tackleCategory constant, so we capture the Object
                // and compare its category value.
                __result = Config.MaxStackSize;
            }
        }

        /// <summary>
        /// Postfix for canStackWith(other). The vanilla method answered false for
        /// these unstackable types; this hook re-evaluates with per-type equality
        /// rules, allowing a merge ONLY when both copies are effectively identical.
        /// </summary>
        /// <param name="__instance">The item whose stack we'd merge INTO.</param>
        /// <param name="other">The candidate item being merged (the game's shop/inventory
        /// interface ISalable is used so shops work too).</param>
        /// <param name="__result">Harmony-injected verdict; "ref bool" lets us flip it.</param>
        public static void Item_canStackWith_Postfix(Item __instance, ISalable other, ref bool __result)
        {
            // Guard clauses, checked in order:
            // 1. Nothing to compare against nulls.
            if (__instance == null || other == null)
                return;

            // 2. Vanilla already said YES - keep its answer untouched.
            if (__result)
                return;

            // 3. Different concrete classes (Ring vs Trinket...) never stack.
            // GetType() returns the exact runtime class of each object.
            if (__instance.GetType() != other.GetType())
                return;

            // TRINKETS may merge only when they're truly twins: same base item id,
            // same random generation seed, same "ascended" upgrade flag (stored as a
            // marker key in modData), same custom name template, same description text.
            if (Config.EnableTrinketStacking && __instance is Trinket thisTrinket && other is Trinket otherTrinket)
            {
                bool thisAscended = thisTrinket.modData.ContainsKey("feiluvnana.BetterForge/IsAscended") || thisTrinket.modData.ContainsKey("feiluvnana.BetterTrinket/IsAscended");
                bool otherAscended = otherTrinket.modData.ContainsKey("feiluvnana.BetterForge/IsAscended") || otherTrinket.modData.ContainsKey("feiluvnana.BetterTrinket/IsAscended");

                if (thisTrinket.QualifiedItemId == otherTrinket.QualifiedItemId &&
                    thisTrinket.generationSeed.Value == otherTrinket.generationSeed.Value &&
                    thisAscended == otherAscended &&
                    thisTrinket.displayNameOverrideTemplate.Value == otherTrinket.displayNameOverrideTemplate.Value &&
                    thisTrinket.getDescription() == otherTrinket.getDescription())
                {
                    __result = true;
                }
            }
            // RINGS come in two flavours with different rules:
            else if (Config.EnableRingStacking && __instance is Ring thisRing && other is Ring otherRing)
            {
                // COMBINED RINGS (two rings fused at the forge): comparable only when
                // they contain the SAME inner rings in the SAME order, compared
                // element-by-element with an index loop ("break" exits early on the
                // first mismatch, flipping the match flag to false).
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
                // PLAIN rings: "is not CombinedRing" excludes fusions; otherwise a
                // simple id comparison decides.
                else if (thisRing is not CombinedRing && otherRing is not CombinedRing)
                {
                    if (thisRing.QualifiedItemId == otherRing.QualifiedItemId)
                    {
                        __result = true;
                    }
                }
            }
            // CLOTHING: same item id, same dye colour, same dyeable flag, same price
            // (dyed variants differ in clothesColor, so they won't merge).
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
            // HATS: id plus draw-related state (hairstyle offset handling, hair draw
            // style, prismatic shimmer) - all must match to stack.
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
            // BOOTS: id plus every upgraded stat (skin index, defense, immunity).
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
            // FURNITURE: id plus placement state (rotation and furniture category),
            // so a rotated chair won't merge with an unrotated one.
            else if (Config.EnableFurnitureStacking && __instance is Furniture thisFurn && other is Furniture otherFurn)
            {
                if (thisFurn.QualifiedItemId == otherFurn.QualifiedItemId &&
                    thisFurn.currentRotation.Value == otherFurn.currentRotation.Value &&
                    thisFurn.furniture_type.Value == otherFurn.furniture_type.Value)
                {
                    __result = true;
                }
            }
            // TACKLE: same category constant on both sides (it's not its own class),
            // plus identical id, quality AND remaining durability ("uses").
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

        /// <summary>
        /// Tiny helper returning the clothing's qualified id unchanged. It exists as
        /// an EXPRESSION-BODIED method ("=> expression" instead of a body with return):
        /// the compiler turns it into "return expression;" automatically.
        /// </summary>
        /// <param name="clothing">The clothing item being compared.</param>
        /// <returns>Its QualifiedItemId.</returns>
        private static string mechanicalQualifiedId(Clothing clothing) => clothing.QualifiedItemId;
    }
}
