// Using directives import types from other libraries so short names work here:
//   HarmonyLib                        -> runtime method patching (prefixes/postfixes).
//   Microsoft.Xna.Framework           -> Vector2, Rectangle, Color (math & drawing data).
//   Microsoft.Xna.Framework.Graphics  -> SpriteBatch (the game's batched image
//                                        drawer) and Texture2D (a GPU-loaded image).
//   Netcode                           -> multiplayer-synced field wrappers (NetInt).
//   StardewModdingAPI                 -> SMAPI's logging interface (IMonitor).
//   StardewValley                     -> core game code (Game1, Furniture, Farmer).
//   StardewValley.ItemTypeDefinitions -> ParsedItemData: parsed metadata for an item.
//   StardewValley.Objects             -> the Furniture class family.
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
    /// <summary>
    /// Harmony patches covering generic furniture behavior plus the custom
    /// "Princess" pieces this mod adds (nightstand, wall sconce, bed canopy):
    ///   - keeps each item's furniture type in sync with our Data/Furniture edits,
    ///   - draws the bed canopy BEHIND the bed via a fully custom draw call,
    ///   - animates flickering candle flames on sconces/nightstands, and
    ///   - creates/removes real in-game LightSources so they actually glow.
    /// "static" = this class is never instantiated; all members live on the class.
    /// </summary>
    public static class FurniturePatches
    {
        /// <summary>SMAPI logger used for error reports. "= null!" defers the
        /// null-warning until Apply() assigns the real logger.</summary>
        private static IMonitor Monitor = null!;

        /// <summary>
        /// Wires up every Harmony patch for furniture behavior. Called once from
        /// ModEntry.Entry when the game boots.
        /// </summary>
        /// <param name="harmony">The mod's Harmony instance doing the patching.</param>
        /// <param name="monitor">SMAPI logger for error reporting.</param>
        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            // Save the logger so patch methods below can report errors.
            Monitor = monitor;

            // HOW PATCHING WORKS:
            //   AccessTools.Method(...) finds a game method by REFLECTION (looking
            //   it up by name at runtime). Overloads (same name, different
            //   parameter lists) need the extra Type[] array to pick one exactly.
            //   new HarmonyMethod(...) wraps one of OUR methods as the hook.
            //   A PREFIX runs BEFORE the original method; a POSTFIX runs AFTER it.

            // Before the game asks "does this furniture sit on the floor?", make
            // sure its furniture type matches our edited Data/Furniture row.
            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.isGroundFurniture)),
                prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(SyncFurniture_Prefix))
            );

            // Same sync trick before placement checks/actions: the type drives
            // collision boxes and placement rules, so it must be up to date.
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

            // Drawing: the PREFIX can take over rendering certain items entirely
            // (the canopy); the POSTFIX adds extra sprites after vanilla finishes
            // (the animated candle flames).
            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(Draw_Prefix)),
                postfix: new HarmonyMethod(typeof(FurniturePatches), nameof(Draw_Postfix))
            );

            // After vanilla processes lamp lights turning on, fix our nightstand's sprite.
            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.addLights)),
                postfix: new HarmonyMethod(typeof(FurniturePatches), nameof(AddLights_Postfix))
            );

            // Right-clicking furniture calls checkForAction — intercept it for our
            // sconce/nightstand so clicking toggles them on and off.
            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.checkForAction), new[] { typeof(Farmer), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(FurniturePatches), nameof(CheckForAction_Prefix))
            );

            // After furniture is picked back up, clean up its light source.
            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.performRemoveAction)),
                postfix: new HarmonyMethod(typeof(FurniturePatches), nameof(PerformRemoveAction_Postfix))
            );
        }

        /// <summary>
        /// Harmony PREFIX running before several Furniture methods. It just keeps
        /// the item's furniture type synchronized with our custom data. Returning
        /// void (instead of a bool) means the original method ALWAYS runs after.
        /// </summary>
        /// <param name="__instance">Harmony magic parameter: the exact Furniture
        /// object the patched method was called on — like "this" inside it.</param>
        public static void SyncFurniture_Prefix(Furniture __instance)
        {
            // try/catch: if our code throws an exception (a runtime error),
            // execution jumps into catch and we log it instead of crashing
            // the game or breaking the patched method.
            try
            {
                SyncFurnitureType(__instance);
            }
            catch (Exception ex)
            {
                // $"..." interpolates the exception details into the log text.
                Monitor.Log($"Error in SyncFurniture_Prefix: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Re-reads an item's Data/Furniture row and forces its in-memory
        /// furniture_type to match. Needed because the game snapshots furniture
        /// data when items are created; if our asset edit lands later, or a save
        /// predates this mod, items can carry a stale type until fixed here.
        /// </summary>
        /// <param name="furniture">Any furniture item; "Furniture?" documents that null is allowed.</param>
        public static void SyncFurnitureType(Furniture? furniture)
        {
            // Guard clause: nothing to do for null items or items lacking an ID.
            if (furniture == null || string.IsNullOrEmpty(furniture.ItemId))
                return;

            // Fetch ALL current furniture definitions from the (edited) asset.
            // "TryGetValue(key, out var rawData)" looks up our item's row; rawData
            // receives the raw definition string, and it returns true only if found.
            if (DataLoader.Furniture(Game1.content).TryGetValue(furniture.ItemId, out var rawData))
            {
                // Definition rows are slash-separated; field [1] holds the type
                // name (e.g. "lamp", "painting", "rug", "bed double").
                string[] fields = rawData.Split('/');
                if (fields.Length > 1)
                {
                    // Convert the human-readable type NAME into the numeric ID
                    // the game actually stores internally.
                    int expectedType = Furniture.getTypeNumberFromName(fields[1]);
                    if (furniture.furniture_type.Value != expectedType)
                    {
                        // ".Value" is required because furniture_type is a Netcode
                        // wrapper (NetInt): a field automatically synced between
                        // players in multiplayer rather than a plain int.
                        furniture.furniture_type.Value = expectedType;
                        // The type changed, so the solid-collision box may need resizing.
                        furniture.RecalculateBoundingBox();
                    }
                }
            }
        }

        /// <summary>
        /// Harmony PREFIX for Furniture.draw — runs every time ANY furniture is
        /// drawn. The returned bool decides what happens next:
        ///   true  -> let vanilla drawing proceed normally, or
        ///   false -> skip vanilla entirely (we drew this item ourselves).
        /// Used here to hand-draw the bed canopy BEHIND the bed and to pin
        /// single-frame items so they never shift animation frames.
        /// </summary>
        /// <param name="__instance">The furniture item being drawn.</param>
        /// <param name="spriteBatch">MonoGame's batched sprite drawer — it collects
        /// thousands of images per frame and submits them to the GPU together.</param>
        /// <param name="x">Tile X of the furniture on the map.</param>
        /// <param name="y">Tile Y of the furniture on the map.</param>
        /// <param name="alpha">Transparency multiplier from 0..1 to draw with.</param>
        public static bool Draw_Prefix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
        {
            try
            {
                // Invisible items (e.g. during cutscenes) fall through to normal code.
                if (__instance == null || __instance.isTemporarilyInvisible)
                    return true;

                // Ensure single-frame items never shift out of bounds via sourceIndexOffset
                // Vanilla lamp/painting code shifts WHICH sprite slice of the
                // tilesheet gets used by changing this offset (its "lit" night
                // frame etc.). Our nightstand/sconce have no second frame, so we
                // force the offset back to 0. This uses REFLECTION:
                // AccessTools.Field grabs a PRIVATE field by name at runtime,
                // "?." avoids a crash if the field is ever missing/renamed, and
                // "is NetInt sourceIndexOffset" type-tests AND casts in one step.
                if (__instance.ItemId == "feiluvnana.BetterFurniture.PrincessNightstand" ||
                    __instance.ItemId == "feiluvnana.BetterFurniture.PrincessWallSconce")
                {
                    if (AccessTools.Field(typeof(Furniture), "sourceIndexOffset")?.GetValue(__instance) is NetInt sourceIndexOffset && sourceIndexOffset.Value != 0)
                    {
                        sourceIndexOffset.Value = 0;
                    }
                }

                // The CANOPY gets a completely custom draw (vanilla is skipped with
                // "return false" below) so it can hang BEHIND the bed instead of
                // covering it like an ordinary painting would.
                if (__instance.ItemId == "feiluvnana.BetterFurniture.PrincessBedCanopy")
                {
                    // Item metadata lookup: QualifiedItemId looks like "(F)feiluvnana...";
                    // GetDataOrErrorItem always answers (with a pink error sprite if missing).
                    ParsedItemData data = ItemRegistry.GetDataOrErrorItem(__instance.QualifiedItemId);
                    Texture2D texture = data.GetTexture();       // The PNG as loaded onto the GPU.
                    Rectangle sourceRect = data.GetSourceRect(); // Which slice of that PNG belongs to this item.
                    // Anchor pixel where drawing starts, in world pixels. The
                    // subtraction lifts the sprite so its BOTTOM lands on the bed's
                    // top edge (the art is sourceRect.Height*4 px tall because each
                    // 16px tile scales up 4x to 64px on screen).
                    Vector2 drawPos = new Vector2(__instance.boundingBox.X, __instance.boundingBox.Y - (sourceRect.Height * 4 - __instance.boundingBox.Height));
                    // While shaken (player bumps it), add random -1..+1 pixel jitter.
                    // GlobalToLocal converts WORLD coordinates into CAMERA/SCREEN
                    // coordinates — required before calling spriteBatch.Draw.
                    // "? :" is the TERNARY conditional operator: condition ? a : b.
                    Vector2 localPos = Game1.GlobalToLocal(Game1.viewport, drawPos + ((__instance.shakeTimer > 0) ? new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)) : Vector2.Zero));

                    // Layer depth placed safely behind bed headboard (headboard draws at Top + 1)
                    // Ensuring the bed frame, headboard, and pillows render on top of the canopy backdrop
                    // SpriteBatch sorts sprites by layerDepth (0.0 = furthest back,
                    // 1.0 = front). Dividing a map Y pixel by 10000 squeezes the
                    // whole map height into that range; Math.Max clamps it above
                    // zero so it never rounds to "behind everything".
                    float layerDepth = Math.Max(0.0001f, (float)(__instance.boundingBox.Value.Top - 32) / 10000f);
                    // Mirror horizontally if the furniture was placed flipped.
                    SpriteEffects effects = __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                    // The actual draw call, argument by argument: texture, screen
                    // position, source slice, tint (White = unchanged; "* alpha"
                    // applies transparency), rotation 0, origin at the sprite's
                    // top-left corner, scale 4f (16px art -> 64px tile), flip mode,
                    // and the sort depth computed above.
                    spriteBatch.Draw(texture, localPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, 4f, effects, layerDepth);
                    return false; // Skip vanilla draw — this item is fully handled.
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in Furniture Draw_Prefix: {ex}", LogLevel.Error);
            }
            return true;
        }

        /// <summary>
        /// Harmony POSTFIX for Furniture.draw — runs AFTER vanilla has drawn the
        /// furniture normally. Adds animated, flickering candle flames on top of
        /// the wall sconce and nightstand, and lazily registers a real LightSource
        /// so the surrounding area actually glows at night.
        /// </summary>
        /// <param name="__instance">The furniture that was just drawn.</param>
        /// <param name="spriteBatch">The batched sprite drawer.</param>
        /// <param name="x">Tile X of the furniture.</param>
        /// <param name="y">Tile Y of the furniture.</param>
        /// <param name="alpha">Transparency multiplier 0..1.</param>
        public static void Draw_Postfix(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha)
        {
            try
            {
                if (__instance == null || __instance.isTemporarilyInvisible)
                    return;

                // Identify exactly which custom pieces we're looking at by ID.
                bool isSconce = __instance.ItemId == "feiluvnana.BetterFurniture.PrincessWallSconce";
                bool isNightstand = __instance.ItemId == "feiluvnana.BetterFurniture.PrincessNightstand";

                // Anything else: nothing to add, leave immediately.
                if (!isSconce && !isNightstand)
                    return;

                // Check whether it is lit: toggled on, or dark indoors/night/rain
                // "||" short-circuits: if IsOn is already true, the right-hand
                // check isn't even evaluated.
                bool isLit = __instance.IsOn || (__instance.Location != null && __instance.timeToTurnOnLights());
                if (!isLit)
                    return;

                // Build a stable unique ID for this piece's light (item + tile),
                // using string interpolation so every placed unit gets its own.
                string lightId = $"feiluvnana_light_{__instance.ItemId}_{__instance.TileLocation.X}_{__instance.TileLocation.Y}";

                // Ensure light source is active in current location
                // First time we see this piece while lit: create its light.
                if (__instance.Location != null && __instance.lightSource == null)
                {
                    // Sconces glow lower on the wall (+48px down); nightstands at
                    // the box top. This multi-line ternary picks based on kind.
                    Vector2 lightPos = isSconce
                        ? new Vector2(__instance.boundingBox.X + 32, __instance.boundingBox.Y + 48)
                        : new Vector2(__instance.boundingBox.X + 32, __instance.boundingBox.Y);

                    // LightSource constructor arguments, in order:
                    //   id           -> unique string built above,
                    //   textureIndex -> 4 selects the round-glow sprite from the
                    //                   game's light spritesheet,
                    //   position     -> world pixel position of the glow center,
                    //   radius       -> brightness radius (2 tiles),
                    //   color        -> Black keeps the glow neutral/candle-like,
                    //   context      -> None = not tied to day/night logic,
                    //   ownerId      -> 0L = not owned by any specific player,
                    //   locationName -> which map this light belongs to.
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

                    // Register a COPY into the map's shared (multiplayer-visible)
                    // light pool, unless an identical one is already registered.
                    if (!__instance.Location.hasLightSource(__instance.lightSource.Id))
                    {
                        __instance.Location.sharedLights[__instance.lightSource.Id] = __instance.lightSource.Clone();
                    }
                }

                // Draw animated flickering candle flame sprite from Game1.mouseCursors
                // (the shared UI sprite atlas — it happens to contain a 4-frame
                // fire animation we reuse for candles).
                // "?.": currentGameTime could be null outside normal gameplay;
                // "??" supplies a fallback of 0.0 milliseconds in that case.
                double gameTime = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0.0;
                // Pick animation FRAME 0..3 from elapsed milliseconds:
                //   total % 400   -> wraps around every 400ms (loop length),
                //   / 100         -> splits that into four 100ms slices,
                //   + x*3047/y*88 -> per-tile phase offset so flames on different
                //                    placements don't flicker in perfect unison.
                int frame = (int)((gameTime + (double)(x * 3047) + (double)(y * 88)) % 400.0 / 100.0);
                // Slide the source rect along one atlas row: each flame frame is
                // 12px wide, starting at pixel column 276, row 1985, size 12x11.
                Rectangle flameSourceRect = new Rectangle(276 + frame * 12, 1985, 12, 11);
                // Draw just BELOW the item's bottom edge (+2px) so the flame
                // overlaps the candle art instead of floating above it.
                float layerDepth = (float)(__instance.boundingBox.Value.Bottom + 2) / 10000f;
                Color flameColor = Color.White * alpha; // Untinted; honors draw alpha.
                Vector2 flameOrigin = new Vector2(6f, 10f); // Pivot: center-x, near flame base.
                float flameScale = 2.5f; // 12px source art -> 30px on screen.

                // Same anchor math as the canopy draw: top-left of the visible
                // sprite, lifted so tall sprites line up with the collision box.
                Vector2 baseDrawPos = new Vector2(__instance.boundingBox.X, __instance.boundingBox.Y - (__instance.sourceRect.Height * 4 - __instance.boundingBox.Height));

                if (isSconce)
                {
                    // Left candle flame (pixel offsets tuned to the art).
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

        /// <summary>
        /// Harmony POSTFIX on Furniture.addLights, which vanilla runs when ambient
        /// lights should turn on (dusk, dark rooms...). Only tweaks our nightstand:
        /// it has no separate "lit" sprite frame, so we pin its offset to 0.
        /// </summary>
        /// <param name="__instance">The furniture whose lights were just processed.</param>
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

        /// <summary>
        /// Harmony PREFIX on Furniture.checkForAction, called when the player
        /// interacts (right-clicks) with furniture. For our sconce/nightstand this
        /// TOGGLES them on/off, creating or removing the actual LightSource.
        /// </summary>
        /// <param name="__instance">The clicked furniture.</param>
        /// <param name="who">The player who clicked it.</param>
        /// <param name="justCheckingForActivity">True when the game only wants to
        /// know WHETHER the object does something (cursor tooltip), not perform it.</param>
        /// <param name="__result">Harmony hook to set the method's bool answer
        /// ("true = an action was performed").</param>
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

                // Tooltip pass: claim "yes, this does something" without acting.
                if (justCheckingForActivity)
                {
                    __result = true;
                    return false;
                }

                GameLocation location = __instance.Location;
                if (location == null)
                    return true;

                // Flip the switch. "!" inverts a boolean (on -> off, off -> on).
                __instance.IsOn = !__instance.IsOn;
                string lightId = $"feiluvnana_light_{__instance.ItemId}_{__instance.TileLocation.X}_{__instance.TileLocation.Y}";

                // Turning ON: create the light if needed, then register it.
                if (__instance.IsOn)
                {
                    Vector2 lightPos = isSconce
                        ? new Vector2(__instance.boundingBox.X + 32, __instance.boundingBox.Y + 48)
                        : new Vector2(__instance.boundingBox.X + 32, __instance.boundingBox.Y);

                    // Lazily build the LightSource on first toggle-on.
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

                    // Register a clone in the map's shared light pool (visible to
                    // all players in multiplayer) if not already present.
                    if (!location.hasLightSource(__instance.lightSource.Id))
                    {
                        location.sharedLights[__instance.lightSource.Id] = __instance.lightSource.Clone();
                    }
                    // Play a little ignition sound effect.
                    location.localSound("fireball");
                }
                else
                {
                    // Turning OFF: unregister the light and drop our reference.
                    if (__instance.lightSource != null)
                    {
                        location.removeLightSource(__instance.lightSource.Id);
                        __instance.lightSource = null;
                    }
                    location.localSound("fireball");
                }

                // Report success and skip vanilla checkForAction — we handled it.
                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in CheckForAction_Prefix: {ex}", LogLevel.Error);
            }
            return true;
        }

        /// <summary>
        /// Harmony POSTFIX on Furniture.performRemoveAction, which runs when the
        /// player picks the furniture back up. Cleans up so no orphaned light
        /// keeps glowing from a tile where the item no longer exists.
        /// </summary>
        /// <param name="__instance">The furniture being removed.</param>
        public static void PerformRemoveAction_Postfix(Furniture __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                // Only our glowing pieces ever own a light worth cleaning up.
                if (__instance.ItemId == "feiluvnana.BetterFurniture.PrincessWallSconce" ||
                    __instance.ItemId == "feiluvnana.BetterFurniture.PrincessNightstand")
                {
                    if (__instance.lightSource != null && __instance.Location != null)
                    {
                        // Unregister from the map, then clear the reference.
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

        /// <summary>
        /// Sweeps EVERYWHERE furniture can live — the local player's inventory and
        /// every loaded map — and re-syncs each item's furniture type. Subscribed
        /// in ModEntry to run on save load and day start, so saves created before
        /// this mod (or before its data edits) get corrected automatically.
        /// </summary>
        public static void FixAllLocationAndInventoryFurniture()
        {
            try
            {
                // Part 1: fix furniture sitting in the player's inventory.
                if (Game1.player != null)
                {
                    // "is Furniture furniture" is PATTERN MATCHING: it tests the
                    // type AND gives us a correctly-typed variable in one step.
                    foreach (Item item in Game1.player.Items)
                    {
                        if (item is Furniture furniture)
                        {
                            SyncFurnitureType(furniture);
                        }
                    }
                }

                // Part 2: fix furniture already PLACED in every game location.
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
