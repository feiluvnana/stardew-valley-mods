using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.WorldMaps;

namespace BetterQOL
{
    /// <summary>
    /// Tracks and displays the Prismatic Butterfly granted by the Statue of Blessings (Farming Mastery).
    /// Displays the butterfly's real-time animated icon on the World Map (MapPage / minimap) for the blessed player,
    /// and provides an on-screen directional HUD tracker and enhanced buff tooltip.
    /// </summary>
    public static class ButterflyMapTracker
    {
        private static IMonitor _monitor = null!;
        private static Texture2D? _crittersTexture;

        // Daily deterministic calculation cache
        private static int _cachedDay = -1;
        private static long _cachedPlayerId = -1;
        private static string? _cachedLocationName;
        private static Vector2 _cachedTile = Vector2.Zero;

        /// <summary>
        /// Registers Harmony patches on MapPage and BuffsDisplay.
        /// </summary>
        public static void Apply(Harmony harmony, IMonitor monitor)
        {
            _monitor = monitor;

            try
            {
                // Patch MapPage.drawMiniPortraits (renders farmer mini portraits & map markers)
                var drawPortraitsMethod = AccessTools.Method(typeof(MapPage), nameof(MapPage.drawMiniPortraits), new[] { typeof(SpriteBatch), typeof(float) });
                if (drawPortraitsMethod != null)
                {
                    harmony.Patch(drawPortraitsMethod, postfix: new HarmonyMethod(typeof(ButterflyMapTracker), nameof(DrawMiniPortraits_Postfix)));
                    _monitor.Log("Successfully patched MapPage.drawMiniPortraits for Prismatic Butterfly map marker.", LogLevel.Debug);
                }
                else
                {
                    _monitor.Log("Failed to locate MapPage.drawMiniPortraits method.", LogLevel.Warn);
                }

                // Patch MapPage.performHoverAction (updates map hover tooltip)
                var hoverActionMethod = AccessTools.Method(typeof(MapPage), nameof(MapPage.performHoverAction), new[] { typeof(int), typeof(int) });
                if (hoverActionMethod != null)
                {
                    harmony.Patch(hoverActionMethod, postfix: new HarmonyMethod(typeof(ButterflyMapTracker), nameof(PerformHoverAction_Postfix)));
                    _monitor.Log("Successfully patched MapPage.performHoverAction for Prismatic Butterfly tooltip.", LogLevel.Debug);
                }
                else
                {
                    _monitor.Log("Failed to locate MapPage.performHoverAction method.", LogLevel.Warn);
                }

                // Patch BuffsDisplay.getDescription (enhances statue_of_blessings_6 hover description)
                var buffDescMethod = AccessTools.Method(typeof(BuffsDisplay), "getDescription", new[] { typeof(Buff) });
                if (buffDescMethod != null)
                {
                    harmony.Patch(buffDescMethod, postfix: new HarmonyMethod(typeof(ButterflyMapTracker), nameof(GetDescription_Postfix)));
                    _monitor.Log("Successfully patched BuffsDisplay.getDescription for Prismatic Butterfly blessing hint.", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"Failed to register Prismatic Butterfly patches: {ex.Message}", LogLevel.Warn);
            }
        }

        /// <summary>
        /// Subscribes SMAPI event listeners for HUD tracking and day transitions.
        /// </summary>
        public static void Initialize(IModHelper helper, IMonitor monitor)
        {
            _monitor = monitor;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private static void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            ResetCache();
        }

        public static void ResetCache()
        {
            _cachedDay = -1;
            _cachedPlayerId = -1;
            _cachedLocationName = null;
            _cachedTile = Vector2.Zero;
        }

        /// <summary>
        /// Retrieves the critters texture sheet ("TileSheets\\critters") containing butterfly sprites.
        /// </summary>
        private static Texture2D GetCrittersTexture()
        {
            if (_crittersTexture == null || _crittersTexture.IsDisposed)
            {
                _crittersTexture = Game1.content.Load<Texture2D>("TileSheets\\critters");
            }
            return _crittersTexture;
        }

        /// <summary>
        /// Retrieves the target location, tile position, and live Butterfly entity (if active in critters).
        /// Replicates Stardew Valley 1.6's GameLocation.tryAddPrismaticButterfly deterministic logic.
        /// </summary>
        public static (GameLocation? location, Vector2 tile, Butterfly? liveButterfly) GetButterflyInfo()
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return (null, Vector2.Zero, null);

            // Blessing of the Butterfly buff check
            if (!Game1.player.hasBuff("statue_of_blessings_6"))
                return (null, Vector2.Zero, null);

            long playerId = Game1.player.UniqueMultiplayerID;
            int currentDay = Game1.dayOfMonth;

            // Recompute target map and tile deterministically if not cached for today and this player
            if (_cachedDay != currentDay || _cachedPlayerId != playerId || _cachedLocationName == null)
            {
                Random r = Utility.CreateDaySaveRandom(playerId % 10000L, 0.0, 0.0);
                string[] candidateLocations = new[] { "Forest", "Town", "Beach", "Mountain", "Woods", "BusStop", "Backwoods" };
                string chosenLoc = candidateLocations[r.Next(candidateLocations.Length)];

                if (chosenLoc == "Beach" && Game1.getLocationFromName("BeachNightMarket") != null && Utility.isFestivalDay(Game1.dayOfMonth, Season.Winter))
                {
                    chosenLoc = "BeachNightMarket";
                }

                _cachedDay = currentDay;
                _cachedPlayerId = playerId;
                _cachedLocationName = chosenLoc;

                GameLocation? loc = Game1.getLocationFromName(chosenLoc);
                if (loc != null)
                {
                    Vector2 tile = loc.getRandomTile(r);
                    for (int i = 0; i < 32; i++)
                    {
                        if (loc.isTileLocationOpen(tile))
                            break;
                        tile = loc.getRandomTile(r);
                    }
                    _cachedTile = tile;
                }
                else
                {
                    _cachedTile = Vector2.Zero;
                }
            }

            GameLocation? targetLoc = Game1.getLocationFromName(_cachedLocationName);
            if (targetLoc == null)
                return (null, Vector2.Zero, null);

            // If the butterfly has already spawned in the target location's critters, return its live coordinates
            Butterfly? live = null;
            if (targetLoc.critters != null)
            {
                foreach (var c in targetLoc.critters)
                {
                    if (c is Butterfly b && b.isPrismatic)
                    {
                        live = b;
                        break;
                    }
                }
            }

            if (live != null)
            {
                return (targetLoc, new Vector2(live.position.X / 64f, live.position.Y / 64f), live);
            }

            return (targetLoc, _cachedTile, null);
        }

        /// <summary>
        /// Postfix on MapPage.drawMiniPortraits: Renders the animated Prismatic Butterfly marker on the world map.
        /// </summary>
        public static void DrawMiniPortraits_Postfix(MapPage __instance, SpriteBatch b, float alpha)
        {
            if (!ModEntry.Config.EnablePrismaticButterflyMapMarker)
                return;

            var (loc, tile, _) = GetButterflyInfo();
            if (loc == null)
                return;

            // Map location and tile to world map pixel coordinates
            var posData = WorldMapManager.GetPositionData(loc, new Point((int)tile.X, (int)tile.Y));
            if (!posData.HasValue)
                posData = WorldMapManager.GetPositionData(loc, new Point(0, 0));

            // Only draw if current map region matches (e.g. Valley map)
            if (!posData.HasValue || posData.Value.Data.Region.Id != __instance.mapRegion.Id)
                return;

            Vector2 mapPixel = posData.Value.GetMapPixelPosition();
            Vector2 screenPos = new Vector2(mapPixel.X + __instance.mapBounds.X, mapPixel.Y + __instance.mapBounds.Y);

            double time = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            float bob = (float)Math.Sin(time / 180.0) * 3f;
            Vector2 drawPos = screenPos + new Vector2(0f, bob);

            Texture2D crittersTex = GetCrittersTexture();
            Color prismaticColor = Utility.GetPrismaticColor(0, 10f);

            // 1. Pulsing halo glow behind butterfly
            float pulse = (float)(Math.Sin(time / 250.0) * 0.2 + 0.8);
            Rectangle haloSource = new Rectangle(88, 1779, 32, 32); // Circular aura from mouseCursors
            b.Draw(Game1.mouseCursors, drawPos, haloSource, prismaticColor * (0.45f * pulse * alpha), 0f, new Vector2(16f, 16f), 1.6f * pulse, SpriteEffects.None, 0.998f);

            // 2. Revolving prismatic sparkles
            for (int i = 0; i < 4; i++)
            {
                double angle = (time * 0.0025) + (i * Math.PI / 2.0);
                float dist = 24f + (float)Math.Sin((time * 0.004) + i) * 5f;
                Vector2 sparklePos = drawPos + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);
                Rectangle sparkleRect = new Rectangle(346, 400, 8, 8);
                Color sparkleColor = Utility.GetPrismaticColor(i * 120, 10f) * (0.85f * alpha);
                b.Draw(Game1.mouseCursors, sparklePos, sparkleRect, sparkleColor, (float)angle, new Vector2(4f, 4f), 1.4f, SpriteEffects.None, 0.999f);
            }

            // 3. Animated flapping butterfly sprite (frames 394, 395, 396, 395)
            int[] frames = new[] { 394, 395, 396, 395 };
            int currentFrame = frames[(int)((time / 120.0) % frames.Length)];

            int cols = crittersTex.Width / 16;
            Rectangle srcRect = new Rectangle((currentFrame % cols) * 16, (currentFrame / cols) * 16, 16, 16);
            Vector2 origin = new Vector2(8f, 8f);
            float scale = 3.5f;

            // Soft drop shadow
            b.Draw(crittersTex, drawPos + new Vector2(2f, 3f), srcRect, Color.Black * (0.4f * alpha), 0f, origin, scale, SpriteEffects.None, 0.999f);

            // Prismatic butterfly body
            b.Draw(crittersTex, drawPos, srcRect, prismaticColor * alpha, 0f, origin, scale, SpriteEffects.None, 1f);
        }

        /// <summary>
        /// Postfix on MapPage.performHoverAction: Sets MapPage.hoverText when hovering over the butterfly icon.
        /// </summary>
        public static void PerformHoverAction_Postfix(MapPage __instance, int x, int y)
        {
            if (!ModEntry.Config.EnablePrismaticButterflyMapMarker)
                return;

            var (loc, tile, _) = GetButterflyInfo();
            if (loc == null)
                return;

            var posData = WorldMapManager.GetPositionData(loc, new Point((int)tile.X, (int)tile.Y));
            if (!posData.HasValue)
                posData = WorldMapManager.GetPositionData(loc, new Point(0, 0));

            if (!posData.HasValue || posData.Value.Data.Region.Id != __instance.mapRegion.Id)
                return;

            Vector2 mapPixel = posData.Value.GetMapPixelPosition();
            Vector2 screenPos = new Vector2(mapPixel.X + __instance.mapBounds.X, mapPixel.Y + __instance.mapBounds.Y);

            // Hitbox around butterfly marker (56x56 px)
            Rectangle hitBox = new Rectangle((int)screenPos.X - 28, (int)screenPos.Y - 28, 56, 56);
            if (hitBox.Contains(x, y))
            {
                string title = ModEntry.I18n.Get("butterfly.map-title").ToString();
                string body = ModEntry.I18n.Get("butterfly.map-body", new { location = loc.DisplayName }).ToString();
                __instance.hoverText = $"{title}\n{body}";
            }
        }

        /// <summary>
        /// Postfix on BuffsDisplay.getDescription: Adds exact location guidance to the blessing buff tooltip.
        /// </summary>
        public static void GetDescription_Postfix(Buff buff, ref string __result)
        {
            if (buff?.id == "statue_of_blessings_6")
            {
                var (loc, _, _) = GetButterflyInfo();
                if (loc != null)
                {
                    string hint = ModEntry.I18n.Get("butterfly.buff-desc", new { location = loc.DisplayName }).ToString();
                    __result = $"{__result}\n{hint}";
                }
            }
        }

        /// <summary>
        /// SMAPI RenderedHud handler: Displays a directional edge pointer and on-screen beacon when the player is in the same map.
        /// </summary>
        private static void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || !ModEntry.Config.EnablePrismaticButterflyHudTracker)
                return;

            // Suppress during cutscenes, menus, or events
            if (Game1.activeClickableMenu != null || Game1.eventUp || Game1.farmEvent != null)
                return;

            var (loc, tile, live) = GetButterflyInfo();
            if (loc == null || Game1.currentLocation == null)
                return;

            // Only track while in the same location as the butterfly
            if (!Game1.currentLocation.Name.Equals(loc.Name, StringComparison.OrdinalIgnoreCase))
                return;

            Vector2 butterflyWorldPos = live != null ? live.position : (tile * 64f + new Vector2(32f, 32f));
            Vector2 butterflyScreenPos = Game1.GlobalToLocal(Game1.viewport, butterflyWorldPos);

            SpriteBatch b = e.SpriteBatch;
            Rectangle viewportRect = new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height);

            double time = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
            Texture2D crittersTex = GetCrittersTexture();
            Color prismaticColor = Utility.GetPrismaticColor(0, 10f);

            int[] frames = new[] { 394, 395, 396, 395 };
            int currentFrame = frames[(int)((time / 120.0) % frames.Length)];
            int cols = crittersTex.Width / 16;
            Rectangle srcRect = new Rectangle((currentFrame % cols) * 16, (currentFrame / cols) * 16, 16, 16);

            // 1. If butterfly is on-screen: Draw an elevated glowing beacon indicator directly above it
            if (viewportRect.Contains((int)butterflyScreenPos.X, (int)butterflyScreenPos.Y))
            {
                float bob = (float)Math.Sin(time / 150.0) * 6f;
                Vector2 beaconPos = butterflyScreenPos + new Vector2(0f, -60f + bob);
                Rectangle arrowSource = new Rectangle(412, 495, 5, 8); // Down arrow from mouseCursors

                // Prismatic beacon halo
                Rectangle haloSource = new Rectangle(88, 1779, 32, 32);
                b.Draw(Game1.mouseCursors, beaconPos, haloSource, prismaticColor * 0.4f, 0f, new Vector2(16, 16), 1.2f, SpriteEffects.None, 1f);

                // Downward pointing arrow
                b.Draw(Game1.mouseCursors, beaconPos, arrowSource, prismaticColor, 0f, new Vector2(2.5f, 4f), 3.5f, SpriteEffects.None, 1f);
                return;
            }

            // 2. Off-screen: Project directional edge indicator along viewport boundary
            Vector2 playerScreenPos = Game1.GlobalToLocal(Game1.viewport, Game1.player.Position + new Vector2(32f, 32f));
            Vector2 diff = butterflyScreenPos - playerScreenPos;
            if (diff == Vector2.Zero)
                diff = new Vector2(0, -1);

            float angle = (float)Math.Atan2(diff.Y, diff.X);

            // Elliptical boundary projection with 70px edge margin
            int margin = 70;
            Vector2 center = new Vector2(Game1.uiViewport.Width / 2f, Game1.uiViewport.Height / 2f);
            float rx = Math.Max(100f, (Game1.uiViewport.Width / 2f) - margin);
            float ry = Math.Max(100f, (Game1.uiViewport.Height / 2f) - margin);

            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            float scaleFactor = 1f / Math.Max(Math.Abs(cos) / rx, Math.Abs(sin) / ry);
            Vector2 indicatorPos = center + new Vector2(cos * scaleFactor, sin * scaleFactor);

            // Calculate distance in tiles
            float distanceTiles = Vector2.Distance(Game1.player.Position / 64f, butterflyWorldPos / 64f);
            string distText = $"{(int)distanceTiles}m";

            // Badge box sizing
            Vector2 textSize = Game1.tinyFont.MeasureString(distText);
            int badgeWidth = (int)textSize.X + 44;
            int badgeHeight = 34;
            Rectangle badgeRect = new Rectangle((int)indicatorPos.X - badgeWidth / 2, (int)indicatorPos.Y - badgeHeight / 2, badgeWidth, badgeHeight);

            // Background dialog box
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60), badgeRect.X, badgeRect.Y, badgeRect.Width, badgeRect.Height, Color.White * 0.9f, 1f, false);

            // Directional arrow pointing outward
            Vector2 arrowOffset = new Vector2((float)Math.Cos(angle) * 22f, (float)Math.Sin(angle) * 22f);
            Rectangle edgeArrowRect = new Rectangle(412, 495, 5, 8);
            b.Draw(Game1.mouseCursors, indicatorPos + arrowOffset, edgeArrowRect, prismaticColor, angle - (float)(Math.PI / 2.0), new Vector2(2.5f, 4f), 3.0f, SpriteEffects.None, 1f);

            // Fluttering mini butterfly inside badge
            Vector2 miniButterflyPos = new Vector2(badgeRect.X + 16, badgeRect.Y + badgeHeight / 2);
            b.Draw(crittersTex, miniButterflyPos, srcRect, prismaticColor, 0f, new Vector2(8f, 8f), 1.8f, SpriteEffects.None, 1f);

            // Distance text
            Vector2 textPos = new Vector2(badgeRect.X + 28, badgeRect.Y + (badgeHeight - textSize.Y) / 2);
            Utility.drawTextWithShadow(b, distText, Game1.tinyFont, textPos, Game1.textColor);
        }
    }
}
