// Using directives import types from other libraries so short names work here:
//   HarmonyLib               -> runtime method patching (HarmonyMethod, AccessTools).
//   Microsoft.Xna.Framework  -> Vector2 and Rectangle (positions & collision boxes).
//   Netcode                  -> multiplayer-synced field wrappers (NetRoot).
//   StardewModdingAPI        -> SMAPI's logging interface (IMonitor, LogLevel).
//   StardewValley            -> core game classes (Game1, Furniture, Farmer, GameLocation).
//   StardewValley.Locations  -> location subclasses (FarmHouse, IslandFarmHouse).
//   StardewValley.Objects    -> the BedFurniture class.
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

// BedPatches lets beds — especially custom oversized ones like a 4x4 double bed —
// be placed anywhere normal furniture can go, and carves out a walkable "sleeping
// zone" in the middle rows of big beds. Vanilla treats beds as special furniture
// with strict placement rules; each Harmony prefix below replaces one of those
// checks so beds behave like ordinary placeable furniture instead.
namespace BetterFurniture
{
    /// <summary>
    /// Harmony patches relaxing vanilla bed placement/collision rules and teaching
    /// the game how players walk onto and sleep in wide (4+ tile) double beds.
    /// </summary>
    public static class BedPatches
    {
        /// <summary>SMAPI logger for error reporting, assigned once by Apply().
        /// "static" = shared by every method in this class; "= null!" silences
        /// the compiler warning until it really gets assigned.</summary>
        private static IMonitor Monitor = null!;

        /// <summary>
        /// Registers every Harmony patch for bed behavior. Called once from ModEntry.
        /// </summary>
        /// <param name="harmony">SMAPI's Harmony instance for this mod.</param>
        /// <param name="monitor">SMAPI logger for error reporting.</param>
        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            Monitor = monitor;

            // Each harmony.Patch call targets one game method by reflection
            // (AccessTools.Method) and attaches our prefix to run before it.
            // The "original:" / "prefix:" labels are NAMED ARGUMENTS — they make
            // the call self-documenting regardless of parameter order.
            // A prefix returning bool acts as a gate: false = "skip the original
            // method entirely", true = "carry on into vanilla code".

            harmony.Patch(
                original: AccessTools.Method(typeof(BedFurniture), nameof(BedFurniture.DoesTileHaveProperty)),
                prefix: new HarmonyMethod(typeof(BedPatches), nameof(DoesTileHaveProperty_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(BedFurniture), nameof(BedFurniture.IntersectsForCollision)),
                prefix: new HarmonyMethod(typeof(BedPatches), nameof(IntersectsForCollision_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(BedFurniture), nameof(BedFurniture.ShiftPositionForBed)),
                prefix: new HarmonyMethod(typeof(BedPatches), nameof(ShiftPositionForBed_Prefix))
            );

            // Bypass all placement restrictions for beds
            harmony.Patch(
                original: AccessTools.Method(typeof(BedFurniture), nameof(BedFurniture.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }),
                prefix: new HarmonyMethod(typeof(BedPatches), nameof(PlacementAction_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.canBePlacedHere), new[] { typeof(GameLocation), typeof(Vector2), typeof(CollisionMask), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(BedPatches), nameof(CanBePlacedHere_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.GetAdditionalFurniturePlacementStatus), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }),
                prefix: new HarmonyMethod(typeof(BedPatches), nameof(GetAdditionalFurniturePlacementStatus_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.CanPlaceThisFurnitureHere)),
                prefix: new HarmonyMethod(typeof(BedPatches), nameof(CanPlaceThisFurnitureHere_Prefix))
            );
        }

        /// <summary>
        /// Harmony prefix: allows beds in any location, skipping vanilla's
        /// "this furniture can't go here" location checks.
        /// </summary>
        /// <param name="furniture">The furniture being placed.</param>
        /// <param name="__result">Harmony hook to set the original method's return value.</param>
        /// <returns>False (skip vanilla) for beds, true otherwise.</returns>
        public static bool CanPlaceThisFurnitureHere_Prefix(Furniture furniture, ref bool __result)
        {
            // `is BedFurniture` type test: if it's a bed, force the answer to true.
            if (furniture is BedFurniture)
            {
                __result = true;
                return false; // false tells Harmony to skip the original method entirely.
            }
            return true; // Not a bed: run vanilla code unchanged.
        }

        /// <summary>
        /// Harmony prefix: lets beds overlap anything when checking a placement tile,
        /// so they can be placed freely like regular furniture.
        /// </summary>
        /// <param name="__instance">Harmony hook: the Furniture object the method was called on.</param>
        /// <param name="__result">Replaces the original method's bool return value.</param>
        public static bool CanBePlacedHere_Prefix(Furniture __instance, ref bool __result)
        {
            if (__instance is BedFurniture)
            {
                __result = true;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Harmony prefix: reports 0 ("no problem") whenever vanilla computes extra
        /// placement restrictions for beds, removing them entirely.
        /// </summary>
        public static bool GetAdditionalFurniturePlacementStatus_Prefix(Furniture __instance, ref int __result)
        {
            if (__instance is BedFurniture)
            {
                __result = 0; // 0 = placement allowed, no special conditions.
                return false;
            }
            return true;
        }

        /// <summary>
        /// Replaces BedFurniture.placementAction with a simplified version that skips
        /// all bed-specific validation: just move the bed onto the clicked tile,
        /// consume one item from the player's hands, and set up lights/sounds.
        /// </summary>
        /// <param name="__instance">The bed item being placed.</param>
        /// <param name="location">The location receiving the bed.</param>
        /// <param name="x">Pixel X coordinate of the click.</param>
        /// <param name="y">Pixel Y coordinate of the click.</param>
        /// <param name="who">The player doing the placing (may be null in edge cases).</param>
        /// <param name="__result">Set to true = "placement succeeded".</param>
        public static bool PlacementAction_Prefix(BedFurniture __instance, GameLocation location, int x, int y, Farmer who, ref bool __result)
        {
            // "try": if anything below throws an exception, control jumps to the
            // catch block at the bottom so the game never hard-crashes here.
            try
            {
                // Remember which location and tile the bed now occupies. Pixel
                // coordinates are divided by 64 because one tile = 64x64 pixels.
                __instance.Location = location;
                Vector2 vector = new Vector2(x / 64, y / 64);
                if (__instance.TileLocation != vector)
                {
                    // Setting TileLocation moves the bed AND recalculates its box.
                    __instance.TileLocation = vector;
                }
                else
                {
                    // Same tile: just refresh the collision box manually.
                    __instance.RecalculateBoundingBox();
                }

                // Register the bed in the location's furniture list (skip if already there).
                if (!location.furniture.Contains(__instance))
                {
                    location.furniture.Add(__instance);
                }

                // Remove one of the held bed items from the inventory.
                // ?. runs the call only if `who` isn't null.
                who?.reduceActiveItemByOne();

                // Standard post-placement bookkeeping copied from vanilla:
                // mark bed tiles, run entry effects, glow light, play sound.
                __instance.UpdateBedTile(check_bounds: false);
                __instance.actionOnPlayerEntryOrPlacement(location, dropDown: false);
                __instance.initializeLightSource(vector);
                location.playSound("woodyStep");

                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in Bed PlacementAction_Prefix: {ex}", LogLevel.Error);
                return true; // On error, let vanilla try — better than a broken placement.
            }
        }

        /// <summary>
        /// Teaches the tile system which parts of a wide double bed count as "Bed"
        /// tiles and where clicking triggers sleep, so the walkable middle works.
        /// </summary>
        /// <param name="__instance">The bed being queried.</param>
        /// <param name="tile_x">Tile X being checked.</param>
        /// <param name="tile_y">Tile Y being checked.</param>
        /// <param name="property_name">Which map property is asked about (e.g. "Bed").</param>
        /// <param name="layer_name">Which map layer ("Back", "Buildings", ...).</param>
        /// <param name="property_value">Output value the game expects for a "true" answer.</param>
        /// <param name="__result">True if the tile has the requested property.</param>
        public static bool DoesTileHaveProperty_Prefix(
            BedFurniture __instance,
            int tile_x,
            int tile_y,
            string property_name,
            string layer_name,
            ref string property_value,
            ref bool __result)
        {
            try
            {
                // Only special-case WIDE double beds: type Double AND 4+ tiles across.
                if (__instance.bedType == BedFurniture.BedType.Double && __instance.getTilesWide() >= 4)
                {
                    // The bed's top-left corner tile and its full footprint size.
                    // "(int)" CASTS the stored float coordinate to a whole number
                    // (tile coordinates live in floats; tiles are whole numbers).
                    int originX = (int)__instance.TileLocation.X;
                    int originY = (int)__instance.TileLocation.Y;
                    int width = __instance.getTilesWide();
                    int height = __instance.getTilesHigh();

                    if (layer_name == "Back")
                    {
                        // Vanilla marks tiles just left of beds as "NoFurniture" so you
                        // can't block them; allow that spot for our big beds too.
                        if (property_name == "NoFurniture" && tile_x == originX - 1 && (tile_y == originY + 1 || tile_y == originY + 2))
                        {
                            property_value = "T";
                            __result = true;
                            return false;
                        }

                        // Check if row is in the middle walkable sleeping rows
                        // For 4+ tall beds rows Y+1..Y+2 are the sleeping zone;
                        // shorter beds only use row Y+1. Ternary picks between them.
                        bool isSleepRow = (height >= 4)
                            ? (tile_y == originY + 1 || tile_y == originY + 2)
                            : (tile_y == originY + 1);

                        // Tile must be inside the bed's horizontal span...
                        if (tile_x >= originX && tile_x < originX + width && isSleepRow)
                        {
                            // ...and on a sleep row it counts as a "Bed" tile,
                            // which is what lets players walk onto it.
                            if (property_name == "Bed")
                            {
                                property_value = "T";
                                __result = true;
                                return false;
                            }

                            // Middle 2x2 sleeping zone (Columns X+1 and X+2 on sleep rows)
                            // Clicking these tiles with "TouchAction" = "Sleep" opens
                            // the sleep dialog — i.e. this is where you climb in.
                            bool isSleepColumn = (tile_x == originX + 1 || tile_x == originX + 2);
                            if (isSleepColumn && property_name == "TouchAction")
                            {
                                property_value = "Sleep";
                                __result = true;
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in DoesTileHaveProperty_Prefix: {ex}", LogLevel.Error);
            }
            return true; // Fall back to vanilla for anything we didn't handle.
        }

        /// <summary>
        /// Custom collision for wide double beds: headboard and footboard are solid,
        /// but the two middle rows are walkable so players can step onto the mattress.
        /// </summary>
        /// <param name="__instance">The bed whose collision box is tested.</param>
        /// <param name="rect">The moving object's rectangle (e.g. the player).</param>
        /// <param name="__result">True if `rect` collides with solid bed parts.</param>
        public static bool IntersectsForCollision_Prefix(
            BedFurniture __instance,
            Rectangle rect,
            ref bool __result)
        {
            try
            {
                if (__instance.bedType == BedFurniture.BedType.Double && __instance.getTilesWide() >= 4 && __instance.getTilesHigh() >= 4)
                {
                    Rectangle boundingBox = __instance.GetBoundingBox();

                    // Top 1 tile (headboard, 64px) is solid
                    // Copy the full box but keep only its top 64 pixels.
                    // (Rectangle is a STRUCT — a value type — so "=" copies the
                    // whole box; editing the copy never touches the original.)
                    Rectangle headboard = boundingBox;
                    headboard.Height = 64;
                    if (headboard.Intersects(rect))
                    {
                        __result = true;
                        return false;
                    }

                    // Bottom 1 tile (footboard, starting at Y + 3 tiles = 192px) is solid
                    // Shift a copy of the box down to the last row; Math.Max guards
                    // against a negative height if the bed were somehow shorter.
                    Rectangle footboard = boundingBox;
                    footboard.Y += 192;
                    footboard.Height = Math.Max(0, boundingBox.Height - 192);
                    if (footboard.Height > 0 && footboard.Intersects(rect))
                    {
                        __result = true;
                        return false;
                    }

                    // Middle 2 rows (Y+1 and Y+2, 128px) are walkable sleeping zone
                    __result = false; // No collision: player can stand/sleep here.
                    return false;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in IntersectsForCollision_Prefix: {ex}", LogLevel.Error);
            }
            return true;
        }

        /// <summary>
        /// When a player enters a wide double bed's sleep tiles, snap them onto the
        /// correct pillow slot (spouse vs solo player) instead of vanilla's offset,
        /// which doesn't fit oversized beds.
        /// </summary>
        /// <param name="who">The player stepping onto the bed.</param>
        public static bool ShiftPositionForBed_Prefix(Farmer who)
        {
            try
            {
                GameLocation currentLocation = who.currentLocation;
                if (currentLocation == null)
                    return true;

                // Look up which bed (if any) occupies the tile the player stands on.
                // Positions are divided by 64 to convert pixels -> tile coordinates.
                BedFurniture bedAtTile = BedFurniture.GetBedAtTile(currentLocation, (int)(who.position.X / 64f), (int)(who.position.Y / 64f));
                if (bedAtTile != null && bedAtTile.bedType == BedFurniture.BedType.Double && bedAtTile.getTilesWide() >= 4)
                {
                    int originX = (int)bedAtTile.TileLocation.X;
                    int originY = (int)bedAtTile.TileLocation.Y;

                    bool isSpouse = false;
                    // "Farmer?" = a reference ALLOWED to be null until we find the
                    // owner below. The "?" is C#'s nullable reference annotation.
                    Farmer? owner = null;
                    // Find the bed's owner: the farmhouse owner normally; on the
                    // island farm house the master player owns everything.
                    // "currentLocation is FarmHouse farmHouse" is PATTERN MATCHING:
                    // it type-checks AND hands us a correctly-typed variable.
                    if (currentLocation is FarmHouse farmHouse && farmHouse.HasOwner)
                    {
                        owner = farmHouse.owner;
                    }
                    else if (currentLocation is IslandFarmHouse)
                    {
                        owner = Game1.MasterPlayer;
                    }

                    if (owner != null)
                    {
                        // Decide who sleeps on the right side:
                        // - the owner's spouse, OR
                        // - a non-owner player while the owner is unmarried
                        //   (e.g. a farmhand in co-op before anyone marries).
                        if (owner.team.GetSpouse(owner.UniqueMultiplayerID) == who.UniqueMultiplayerID)
                        {
                            isSpouse = true;
                        }
                        else if (owner != who && !owner.isMarriedOrRoommates())
                        {
                            isSpouse = true;
                        }
                    }

                    if (isSpouse)
                    {
                        // Spouse sleeps on Right Pillow slot (Column X + 2), facing left
                        who.Position = new Vector2((originX + 2) * 64f, (originY + 1) * 64f);
                        who.faceDirection(3); // 3 = left in Stardew's direction codes.
                    }
                    else
                    {
                        // Solo player sleeps on Left Pillow slot (Column X + 1), facing right
                        who.Position = new Vector2((originX + 1) * 64f, (originY + 1) * 64f);
                        who.faceDirection(1); // 1 = right.
                    }

                    // Nudge down half a tile to center on the pillow sprite.
                    who.position.Y += 32f;
                    // Netcode bookkeeping: cancel movement interpolation so the player
                    // teleports instantly instead of smoothly sliding to the bed spot.
                    (who.NetFields.Root as NetRoot<Farmer>)?.CancelInterpolation();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in ShiftPositionForBed_Prefix: {ex}", LogLevel.Error);
            }
            return true; // Not our special bed case: run vanilla positioning.
        }
    }
}
