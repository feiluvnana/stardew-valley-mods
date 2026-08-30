using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace BetterQOL.Transparency
{
    /// <summary>
    /// Manages the state, alpha caching, and distance calculations for custom object and terrain transparency.
    /// </summary>
    public static class TransparencyManager
    {
        private static readonly Dictionary<object, PerScreen<float>> Alphas = new();
        private static IModHelper Helper = null!;
        private static IMonitor Monitor = null!;

        /// <summary>The current tile coordinate of the local player.</summary>
        public static Vector2 CurrentPlayerTile { get; private set; } = Vector2.Zero;

        /// <summary>Per-screen flag indicating if transparency is temporarily disabled.</summary>
        public static PerScreen<bool> DisableTransparency { get; } = new(() => false);

        /// <summary>Per-screen flag indicating if full maximum transparency is forced on all objects.</summary>
        public static PerScreen<bool> FullTransparency { get; } = new(() => false);

        /// <summary>
        /// Initializes event subscriptions for player movement, location changes, and keybind inputs.
        /// </summary>
        /// <param name="helper">SMAPI helper.</param>
        /// <param name="monitor">SMAPI monitor.</param>
        public static void Initialize(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        }

        /// <summary>
        /// Clears all cached alpha values across all objects.
        /// </summary>
        public static void ClearCache()
        {
            Alphas.Clear();
        }

        /// <summary>
        /// Calculates and steps the alpha opacity for a specific object instance smoothly towards its target.
        /// </summary>
        /// <param name="instance">The object or terrain feature instance.</param>
        /// <param name="changeToApply">Alpha delta per step (e.g. -0.05f to fade out, +0.05f to fade in, or 0f to query).</param>
        /// <param name="minimum">Minimum opacity clamp (0.0 to 1.0).</param>
        /// <returns>The resulting alpha opacity (0.0 to 1.0).</returns>
        public static float GetAlpha(object instance, float changeToApply, float minimum)
        {
            if (Alphas.TryGetValue(instance, out var value))
            {
                value.Value = Math.Clamp(value.Value + changeToApply, minimum, 1f);
                return value.Value;
            }

            value = new PerScreen<float>
            {
                Value = Math.Clamp(1f + changeToApply, minimum, 1f)
            };
            Alphas[instance] = value;
            return value.Value;
        }

        /// <summary>
        /// Determines if a farm building should be made transparent.
        /// </summary>
        public static bool ShouldBeTransparent(Building building)
        {
            if (FullTransparency.Value)
                return true;
            if (DisableTransparency.Value)
                return false;

            Rectangle rect = new(building.tileX.Value, building.tileY.Value, building.tilesWide.Value, building.tilesHigh.Value);
            int threshold = ModEntry.Config.BuildingTileDistance + (rect.Width + rect.Height) / 2;

            if ((!ModEntry.Config.BuildingBelowPlayerOnly || CurrentPlayerTile.Y < rect.Top)
                && Vector2.Distance(new Vector2(rect.Center.X, rect.Center.Y), CurrentPlayerTile) < threshold)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a bush should be made transparent.
        /// </summary>
        public static bool ShouldBeTransparent(Bush bush)
        {
            if (FullTransparency.Value)
                return true;
            if (DisableTransparency.Value)
                return false;

            Point center = bush.getRenderBounds().Center;
            if ((!ModEntry.Config.BushBelowPlayerOnly || CurrentPlayerTile.Y < bush.Tile.Y)
                && Vector2.Distance(new Vector2(center.X, center.Y), CurrentPlayerTile * 64f) < ModEntry.Config.BushTileDistance * 64f)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a wild tree should be made transparent.
        /// </summary>
        public static bool ShouldBeTransparent(Tree tree)
        {
            if (FullTransparency.Value)
                return true;
            if (DisableTransparency.Value)
                return false;

            Vector2 canopyCenter = new(tree.Tile.X, tree.Tile.Y - 2f);
            if ((!ModEntry.Config.TreeBelowPlayerOnly || CurrentPlayerTile.Y < tree.Tile.Y)
                && Vector2.Distance(canopyCenter, CurrentPlayerTile) < ModEntry.Config.TreeTileDistance)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a fruit tree should be made transparent.
        /// </summary>
        public static bool ShouldBeTransparent(FruitTree fruitTree)
        {
            if (FullTransparency.Value)
                return true;
            if (DisableTransparency.Value)
                return false;

            Vector2 canopyCenter = new(fruitTree.Tile.X, fruitTree.Tile.Y - 2f);
            if ((!ModEntry.Config.TreeBelowPlayerOnly || CurrentPlayerTile.Y < fruitTree.Tile.Y)
                && Vector2.Distance(canopyCenter, CurrentPlayerTile) < ModEntry.Config.TreeTileDistance)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a patch of grass should be made transparent.
        /// </summary>
        public static bool ShouldBeTransparent(Grass grass)
        {
            if (FullTransparency.Value)
                return true;
            if (DisableTransparency.Value)
                return false;

            if ((!ModEntry.Config.GrassBelowPlayerOnly || CurrentPlayerTile.Y < grass.Tile.Y)
                && Vector2.Distance(grass.Tile, CurrentPlayerTile) < ModEntry.Config.GrassTileDistance)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a crop should be made transparent.
        /// </summary>
        public static bool ShouldBeTransparent(Crop crop)
        {
            if (FullTransparency.Value)
                return true;
            if (DisableTransparency.Value)
                return false;

            if ((!ModEntry.Config.CropBelowPlayerOnly || CurrentPlayerTile.Y < crop.tilePosition.Y)
                && Vector2.Distance(crop.tilePosition, CurrentPlayerTile) < ModEntry.Config.CropTileDistance)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a placed object or big craftable should be made transparent.
        /// </summary>
        public static bool ShouldBeTransparent(StardewValley.Object obj, int x, int y, bool isBigCraftable)
        {
            if (FullTransparency.Value)
                return true;
            if (DisableTransparency.Value)
                return false;

            bool belowPlayerOnly = isBigCraftable ? ModEntry.Config.CraftableBelowPlayerOnly : ModEntry.Config.ObjectBelowPlayerOnly;
            int distance = isBigCraftable ? ModEntry.Config.CraftableTileDistance : ModEntry.Config.ObjectTileDistance;

            if ((!belowPlayerOnly || CurrentPlayerTile.Y < y)
                && Utility.distance(x, CurrentPlayerTile.X, y, CurrentPlayerTile.Y) < distance)
            {
                return true;
            }

            return false;
        }

        private static void OnUpdateTicking(object? sender, UpdateTickingEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;

            CurrentPlayerTile = Game1.player.Tile;

            if (!ModEntry.Config.EnableTransparency)
                return;

            var location = Game1.player.currentLocation;
            if (location == null)
                return;

            // Periodic grass transparency updates
            if (ModEntry.Config.EnableGrassTransparency)
            {
                foreach (var terrainFeature in location.terrainFeatures.Values)
                {
                    if (terrainFeature is Grass grass)
                    {
                        if (ShouldBeTransparent(grass))
                            GetAlpha(grass, -0.05f, ModEntry.Config.GrassMinimumOpacity);
                        else
                            GetAlpha(grass, 0.05f, ModEntry.Config.GrassMinimumOpacity);
                    }
                }
            }

            // Periodic crop transparency updates
            if (ModEntry.Config.EnableCropTransparency)
            {
                // Ground crops
                foreach (var terrainFeature in location.terrainFeatures.Values)
                {
                    if (terrainFeature is HoeDirt { crop: { } crop })
                    {
                        if (ShouldBeTransparent(crop))
                            GetAlpha(crop, -0.05f, ModEntry.Config.CropMinimumOpacity);
                        else
                            GetAlpha(crop, 0.05f, ModEntry.Config.CropMinimumOpacity);
                    }
                }

                // Garden pot crops
                foreach (var obj in location.objects.Values)
                {
                    if (obj is IndoorPot pot && pot.hoeDirt.Value?.crop is { } indoorCrop)
                    {
                        if (ShouldBeTransparent(indoorCrop))
                            GetAlpha(indoorCrop, -0.05f, ModEntry.Config.CropMinimumOpacity);
                        else
                            GetAlpha(indoorCrop, 0.05f, ModEntry.Config.CropMinimumOpacity);
                    }
                }
            }
        }

        private static void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !ModEntry.Config.EnableTransparency)
                return;

            bool fullPressed = ModEntry.Config.FullTransparencyKey.JustPressed();
            bool disablePressed = ModEntry.Config.DisableTransparencyKey.JustPressed();

            if (fullPressed && disablePressed)
            {
                if (ModEntry.Config.DisableTransparencyKey.GetKeybindCurrentlyDown().Buttons.Length >
                    ModEntry.Config.FullTransparencyKey.GetKeybindCurrentlyDown().Buttons.Length)
                {
                    ToggleDisableTransparency();
                }
                else
                {
                    ToggleFullTransparency();
                }
            }
            else if (fullPressed)
            {
                ToggleFullTransparency();
            }
            else if (disablePressed)
            {
                ToggleDisableTransparency();
            }
        }

        private static void ToggleFullTransparency()
        {
            FullTransparency.Value = !FullTransparency.Value;
            DisableTransparency.Value = false;
            Monitor.Log($"Full transparency keybind pressed. Mode: {(FullTransparency.Value ? "Full" : "Default")}.", LogLevel.Trace);
        }

        private static void ToggleDisableTransparency()
        {
            DisableTransparency.Value = !DisableTransparency.Value;
            FullTransparency.Value = false;
            Monitor.Log($"Disable transparency keybind pressed. Mode: {(DisableTransparency.Value ? "Disabled" : "Default")}.", LogLevel.Trace);
        }

        private static void OnDayEnding(object? sender, DayEndingEventArgs e) => ClearCache();
        private static void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e) => ClearCache();
        private static void OnWarped(object? sender, WarpedEventArgs e) => ClearCache();
    }
}
