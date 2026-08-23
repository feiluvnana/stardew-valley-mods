using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace BetterFurniture
{
    public static class FurniturePatches
    {
        private static IMonitor Monitor = null!;

        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            Monitor = monitor;

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.isGroundFurniture)),
                prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(SyncFurniture_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.canBePlacedHere), new[] { typeof(GameLocation), typeof(Vector2), typeof(CollisionMask), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(SyncFurniture_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.GetAdditionalFurniturePlacementStatus), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }),
                prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(SyncFurniture_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }),
                prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(SyncFurniture_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(Draw_Prefix)),
                postfix: new HarmonyMethod(typeof(FurniturePatches), nameof(Draw_Postfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.addLights)),
                postfix: new HarmonyMethod(typeof(FurniturePatches), nameof(AddLights_Postfix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.checkForAction), new[] { typeof(Farmer), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(CheckForAction_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.performRemoveAction)),
                postfix: new HarmonyMethod(typeof(FurniturePatches), nameof(PerformRemoveAction_Postfix))
            );
        }

        public static void SyncFurniture_Prefix(Furniture __instance)
        {
            try
            {
                SyncFurnitureType(__instance);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in SyncFurniture_Prefix: {ex}", LogLevel.Error);
            }
        }

        public static void SyncFurnitureType(Furniture? furniture)
        {
            if (furniture == null || string.IsNullOrEmpty(furniture.ItemId))
                return;

            if (DataLoader.Furniture(Game1.content).TryGetValue(furniture.ItemId, out var rawData))
            {
                string[] fields = rawData.Split('/');
                if (fields.Length > 1)
                {
                    int expectedType = Furniture.getTypeNumberFromName(fields[1]);
                    if (furniture.furniture_type.Value != expectedType)
                    {
                        furniture.furniture_type.Value = expectedType;
                        furniture.RecalculateBoundingBox();
                    }
                }
            }
        }

        public static bool Draw_Prefix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
        {
            try
            {
                if (__instance == null || __instance.isTemporarilyInvisible)
                    return true;

                // Ensure single-frame items never shift out of bounds via sourceIndexOffset
                if (__instance.ItemId == "feiluvnana.BetterFurniture.PrincessNightstand" ||
                    __instance.ItemId == "feiluvnana.BetterFurniture.PrincessWallSconce")
                {
                    if (AccessTools.Field(typeof(Furniture), "sourceIndexOffset")?.GetValue(__instance) is NetInt sourceIndexOffset && sourceIndexOffset.Value != 0)
                    {
                        sourceIndexOffset.Value = 0;
                    }
                }

                if (__instance.ItemId == "feiluvnana.BetterFurniture.PrincessBedCanopy")
                {
                    ParsedItemData data = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
                    Texture2D texture = data.GetTexture();
                    Rectangle sourceRect = data.GetSourceRect();
                    Vector2 drawPos = new Vector2(__instance.boundingBox.X, __instance.boundingBox.Y - (sourceRect.Height * 4 - __instance.boundingBox.Height));
                    Vector2 localPos = Game1.GlobalToLocal(Game1.viewport, drawPos + ((__instance.shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero));

                    // Layer depth placed safely behind bed headboard (headboard draws at Top + 1)
                    // Ensuring the bed frame, headboard, and pillows render on top of the canopy backdrop
                    float layerDepth = Math.Max(0.0001f, (float)(__instance.boundingBox.Value.Top - 32) / 10000f);
                    SpriteEffects effects = __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                    spriteBatch.Draw(texture, localPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, 4f, effects, layerDepth);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in Furniture Draw_Prefix: {ex}", LogLevel.Error);
            }
            return true;
        }

        public static void Draw_Postfix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
        {
            try
            {
                if (__instance == null || __instance.isTemporarilyInvisible)
                    return;

                bool isSconce = __instance.ItemId == "feiluvnana.BetterFurniture.PrincessWallSconce";
                bool isNightstand = __instance.ItemId == "feiluvnana.BetterFurniture.PrincessNightstand";

                if (!isSconce && !isNightstand)
                    return;

                // Check whether it is lit: toggled on, or dark indoors/night/rain
                bool isLit = __instance.IsOn || (__instance.Location != null && __instance.timeToTurnOnLights());
                if (!isLit)
                    return;

                string lightId = $"feiluvnana_light_{__instance.ItemId}_{__instance.TileLocation.X}_{__instance.TileLocation.Y}";

                // Ensure light source is active in current location
                if (__instance.Location != null && __instance.lightSource == null)
                {
                    Vector2 lightPos = isSconce
                        ? new Vector2(__instance.boundingBox.X + 32, __instance.boundingBox.Y + 48)
                        : new Vector2(__instance.boundingBox.X + 32, __instance.boundingBox.Y);

                    __instance.lightSource = new LightSource(
                        lightId,
                        4,
                        lightPos,
                        2.0f,
                        Color.Black,
                        LightSource.LightContext.None,
                        0L,
                        __instance.Location.NameOrUniqueName
                    );

                    if (!__instance.Location.hasLightSource(__instance.lightSource.Id))
                    {
                        __instance.Location.sharedLights[__instance.lightSource.Id] = __instance.lightSource.Clone();
                    }
                }

                // Draw animated flickering candle flame sprite from Game1.mouseCursors
                double gameTime = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0.0;
                int frame = (int)((gameTime + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0);
                Rectangle flameSourceRect = new Rectangle(276 + frame * 12, 1985, 12, 11);
                float layerDepth = (float)(__instance.boundingBox.Value.Bottom + 2) / 10000f;
                Color flameColor = Color.White * alpha;
                Vector2 flameOrigin = new Vector2(6f, 10f);
                float flameScale = 2.5f;

                Vector2 baseDrawPos = new Vector2(__instance.boundingBox.X, __instance.boundingBox.Y - (__instance.sourceRect.Height * 4 - __instance.boundingBox.Height));

                if (isSconce)
                {
                    // Left candle flame
                    Vector2 leftFlamePos = Game1.GlobalToLocal(Game1.viewport, new Vector2(baseDrawPos.X + 17f, baseDrawPos.Y + 28f));
                    spriteBatch.Draw(Game1.mouseCursors, leftFlamePos, flameSourceRect, flameColor, 0f, flameOrigin, flameScale, SpriteEffects.None, layerDepth);

                    // Right candle flame
                    Vector2 rightFlamePos = Game1.GlobalToLocal(Game1.viewport, new Vector2(baseDrawPos.X + 47f, baseDrawPos.Y + 28f));
                    spriteBatch.Draw(Game1.mouseCursors, rightFlamePos, flameSourceRect, flameColor, 0f, flameOrigin, flameScale, SpriteEffects.None, layerDepth);
                }
                else if (isNightstand)
                {
                    // Center candle flame on top of nightstand
                    Vector2 candleFlamePos = Game1.GlobalToLocal(Game1.viewport, new Vector2(baseDrawPos.X + 32f, baseDrawPos.Y + 24f));
                    spriteBatch.Draw(Game1.mouseCursors, candleFlamePos, flameSourceRect, flameColor, 0f, flameOrigin, flameScale, SpriteEffects.None, layerDepth);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in Draw_Postfix: {ex}", LogLevel.Error);
            }
        }

        public static void AddLights_Postfix(Furniture __instance)
        {
            try
            {
                // Vanilla lamp furniture shifts to a second "lit" frame at night via sourceIndexOffset.
                // The nightstand has no lit frame, so reset the offset to keep its normal sprite visible
                // (the animated candle flame is drawn separately in Draw_Postfix).
                if (__instance?.ItemId == "feiluvnana.BetterFurniture.PrincessNightstand")
                {
                    if (AccessTools.Field(typeof(Furniture), "sourceIndexOffset")?.GetValue(__instance) is NetInt sourceIndexOffset)
                    {
                        sourceIndexOffset.Value = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in AddLights_Postfix: {ex}", LogLevel.Error);
            }
        }

        public static bool CheckForAction_Prefix(Furniture __instance, Farmer who, bool justCheckingForActivity, ref bool __result)
        {
            try
            {
                if (__instance == null)
                    return true;

                bool isSconce = __instance.ItemId == "feiluvnana.BetterFurniture.PrincessWallSconce";
                bool isNightstand = __instance.ItemId == "feiluvnana.BetterFurniture.PrincessNightstand";

                if (!isSconce && !isNightstand)
                    return true;

                if (justCheckingForActivity)
                {
                    __result = true;
                    return false;
                }

                GameLocation location = __instance.Location;
                if (location == null)
                    return true;

                __instance.IsOn = !__instance.IsOn;
                string lightId = $"feiluvnana_light_{__instance.ItemId}_{__instance.TileLocation.X}_{__instance.TileLocation.Y}";

                if (__instance.IsOn)
                {
                    Vector2 lightPos = isSconce
                        ? new Vector2(__instance.boundingBox.X + 32, __instance.boundingBox.Y + 48)
                        : new Vector2(__instance.boundingBox.X + 32, __instance.boundingBox.Y);

                    if (__instance.lightSource == null)
                    {
                        __instance.lightSource = new LightSource(
                            lightId,
                            4,
                            lightPos,
                            2.0f,
                            Color.Black,
                            LightSource.LightContext.None,
                            0L,
                            location.NameOrUniqueName
                        );
                    }

                    if (!location.hasLightSource(__instance.lightSource.Id))
                    {
                        location.sharedLights[__instance.lightSource.Id] = __instance.lightSource.Clone();
                    }
                    location.localSound("fireball");
                }
                else
                {
                    if (__instance.lightSource != null)
                    {
                        location.removeLightSource(__instance.lightSource.Id);
                        __instance.lightSource = null;
                    }
                    location.localSound("fireball");
                }

                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in CheckForAction_Prefix: {ex}", LogLevel.Error);
            }
            return true;
        }

        public static void PerformRemoveAction_Postfix(Furniture __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                if (__instance.ItemId == "feiluvnana.BetterFurniture.PrincessWallSconce" ||
                    __instance.ItemId == "feiluvnana.BetterFurniture.PrincessNightstand")
                {
                    if (__instance.lightSource != null && __instance.Location != null)
                    {
                        __instance.Location.removeLightSource(__instance.lightSource.Id);
                        __instance.lightSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in PerformRemoveAction_Postfix: {ex}", LogLevel.Error);
            }
        }

        public static void FixAllLocationAndInventoryFurniture()
        {
            try
            {
                if (Game1.player != null)
                {
                    foreach (Item item in Game1.player.Items)
                    {
                        if (item is Furniture furniture)
                        {
                            SyncFurnitureType(furniture);
                        }
                    }
                }

                if (Game1.locations != null)
                {
                    foreach (GameLocation location in Game1.locations)
                    {
                        if (location != null)
                        {
                            foreach (Furniture furniture in location.furniture)
                            {
                                SyncFurnitureType(furniture);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in FixAllLocationAndInventoryFurniture: {ex}", LogLevel.Error);
            }
        }
    }
}
