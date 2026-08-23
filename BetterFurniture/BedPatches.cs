using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace BetterFurniture
{
    public static class BedPatches
    {
        private static IMonitor Monitor = null!;

        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            Monitor = monitor;

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

        public static bool CanPlaceThisFurnitureHere_Prefix(Furniture furniture, ref bool __result)
        {
            if (furniture is BedFurniture)
            {
                __result = true;
                return false;
            }
            return true;
        }

        public static bool CanBePlacedHere_Prefix(Furniture __instance, ref bool __result)
        {
            if (__instance is BedFurniture)
            {
                __result = true;
                return false;
            }
            return true;
        }

        public static bool GetAdditionalFurniturePlacementStatus_Prefix(Furniture __instance, ref int __result)
        {
            if (__instance is BedFurniture)
            {
                __result = 0;
                return false;
            }
            return true;
        }

        public static bool PlacementAction_Prefix(BedFurniture __instance, GameLocation location, int x, int y, Farmer who, ref bool __result)
        {
            try
            {
                __instance.Location = location;
                Vector2 vector = new Vector2(x / 64, y / 64);
                if (__instance.TileLocation != vector)
                {
                    __instance.TileLocation = vector;
                }
                else
                {
                    __instance.RecalculateBoundingBox();
                }

                if (!location.furniture.Contains(__instance))
                {
                    location.furniture.Add(__instance);
                }

                who?.reduceActiveItemByOne();

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
                return true;
            }
        }

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
                if (__instance.bedType == BedFurniture.BedType.Double && __instance.getTilesWide() >= 4)
                {
                    int originX = (int)__instance.TileLocation.X;
                    int originY = (int)__instance.TileLocation.Y;
                    int width = __instance.getTilesWide();
                    int height = __instance.getTilesHigh();

                    if (layer_name == "Back")
                    {
                        if (property_name == "NoFurniture" && tile_x == originX - 1 && (tile_y == originY + 1 || tile_y == originY + 2))
                        {
                            property_value = "T";
                            __result = true;
                            return false;
                        }

                        // Check if row is in the middle walkable sleeping rows
                        bool isSleepRow = (height >= 4)
                            ? (tile_y == originY + 1 || tile_y == originY + 2)
                            : (tile_y == originY + 1);

                        if (tile_x >= originX && tile_x < originX + width && isSleepRow)
                        {
                            if (property_name == "Bed")
                            {
                                property_value = "T";
                                __result = true;
                                return false;
                            }

                            // Middle 2x2 sleeping zone (Columns X+1 and X+2 on sleep rows)
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
            return true;
        }

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
                    Rectangle headboard = boundingBox;
                    headboard.Height = 64;
                    if (headboard.Intersects(rect))
                    {
                        __result = true;
                        return false;
                    }

                    // Bottom 1 tile (footboard, starting at Y + 3 tiles = 192px) is solid
                    Rectangle footboard = boundingBox;
                    footboard.Y += 192;
                    footboard.Height = Math.Max(0, boundingBox.Height - 192);
                    if (footboard.Height > 0 && footboard.Intersects(rect))
                    {
                        __result = true;
                        return false;
                    }

                    // Middle 2 rows (Y+1 and Y+2, 128px) are walkable sleeping zone
                    __result = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in IntersectsForCollision_Prefix: {ex}", LogLevel.Error);
            }
            return true;
        }

        public static bool ShiftPositionForBed_Prefix(Farmer who)
        {
            try
            {
                GameLocation currentLocation = who.currentLocation;
                if (currentLocation == null)
                    return true;

                BedFurniture bedAtTile = BedFurniture.GetBedAtTile(currentLocation, (int)(who.position.X / 64f), (int)(who.position.Y / 64f));
                if (bedAtTile != null && bedAtTile.bedType == BedFurniture.BedType.Double && bedAtTile.getTilesWide() >= 4)
                {
                    int originX = (int)bedAtTile.TileLocation.X;
                    int originY = (int)bedAtTile.TileLocation.Y;

                    bool isSpouse = false;
                    Farmer? owner = null;
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
                        who.faceDirection(3);
                    }
                    else
                    {
                        // Solo player sleeps on Left Pillow slot (Column X + 1), facing right
                        who.Position = new Vector2((originX + 1) * 64f, (originY + 1) * 64f);
                        who.faceDirection(1);
                    }

                    who.position.Y += 32f;
                    (who.NetFields.Root as NetRoot<Farmer>)?.CancelInterpolation();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in ShiftPositionForBed_Prefix: {ex}", LogLevel.Error);
            }
            return true;
        }
    }
}
