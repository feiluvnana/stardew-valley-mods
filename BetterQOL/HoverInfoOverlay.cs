using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

// HoverInfoOverlay draws UI Info Suite 2-style intelligence while playing: crop growth
// timers, machine countdowns, tree/bush stages, animal friendship, and more. Each frame
// (via SMAPI's RenderedHud event) it identifies the object under the mouse tile, asks the
// helper classes for its stats, then paints a compact parchment tooltip with MonoGame's
// SpriteBatch - all without pausing or blocking gameplay.
namespace BetterQOL
{
    /// <summary>
    /// One line of tooltip body text plus its draw color (a tiny data-holder class).
    /// </summary>
    public class TooltipLine
    {
        /// <summary>The localized text shown on this line.</summary>
        public string Text { get; set; } = string.Empty;
        /// <summary>Tint applied to the text when drawn (defaults to the game's standard text color).</summary>
        public Color Color { get; set; } = Game1.textColor;

        /// <summary>Creates a line with explicit text and color.</summary>
        public TooltipLine(string text, Color color)
        {
            Text = text;
            Color = color;
        }
    }

    /// <summary>
    /// Everything needed to render one tooltip: heading, optional icon, and body lines.
    /// Keeping DATA separate from DRAWING makes the layout code reusable and testable.
    /// A trailing "?" marks a nullable type - that field may legitimately be absent.
    /// </summary>
    public class TooltipModel
    {
        /// <summary>Bold first line (object / crop / machine name).</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Optional smaller caption under the title (e.g. "Garden Pot").</summary>
        public string? Subtitle { get; set; }
        /// <summary>Texture sheet containing the icon, if one should be drawn.</summary>
        public Texture2D? IconTexture { get; set; }
        /// <summary>Pixel region to copy within IconTexture (spritesheets pack many icons).</summary>
        public Rectangle? IconSourceRect { get; set; }
        /// <summary>Body rows; "new()" is target-typed shorthand inferring List&lt;TooltipLine&gt; (C# 9).</summary>
        public List<TooltipLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Central hub of the hover system: subscribes to SMAPI's render timing, detects what
    /// the cursor points at (objects -> terrain features -> animals -> buildings), builds a
    /// TooltipModel via small builder methods, and delegates painting. 'static' means one
    /// shared copy for the whole mod rather than per-instance objects.
    /// </summary>
    public static class HoverInfoOverlay
    {
        // "null!" suppresses the compiler's nullability warning: Initialize() is guaranteed
        // to run before any event fires, so starting as null is safe here.
        private static IModHelper Helper = null!;
        private static IMonitor Monitor = null!;
        // Expression-bodied property ("=>"): every read re-evaluates ModEntry.Config,
        // always giving us the live settings object.
        private static ModConfig Config => ModEntry.Config;

        /// <summary>Caches SMAPI services and hooks the per-frame HUD-rendered event.</summary>
        public static void Initialize(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            // RenderedHud fires once per frame right after the heads-up display draws -
            // ideal for overlays that must sit above the world but below menus/dialogue.
            helper.Events.Display.RenderedHud += OnRenderedHud;
        }

        #region 1. In-World Hover Overlay

        /// <summary>
        /// Frame callback: find a hoverable subject under the cursor and draw its tooltip.
        /// </summary>
        /// <param name="sender">Event source (unused); "?" allows null per .NET event conventions.</param>
        /// <param name="e">Carries the SpriteBatch we may draw with during this frame.</param>
        private static void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            // Only during live gameplay - never on the title screen or while loading saves.
            if (!Context.IsWorldReady || Game1.currentLocation == null)
                return;

            // Stay hidden while a menu is open, during cutscenes, and during farm events.
            if (Game1.activeClickableMenu != null || Game1.eventUp || Game1.farmEvent != null)
                return;

            // Optional "hold-to-view" mode: if a key is configured it must currently be down.
            if (Config.HoverHotkey != SButton.None && !Helper.Input.IsDown(Config.HoverHotkey))
                return;

            // SMAPI's cursor exposes three coordinate flavors at once:
            //   Tile                 - grid square under the mouse (floats; keys into dictionaries)
            //   GetScaledScreenPixels - zoom-adjusted UI pixel point (where to DRAW our box)
            //   AbsolutePixels       - raw unscaled pixels (precise Rectangle hit-tests)
            var cursor = Helper.Input.GetCursorPosition();
            Vector2 tilePos = cursor.Tile;
            Vector2 screenPos = cursor.GetScaledScreenPixels();
            Vector2 absolutePixels = cursor.AbsolutePixels;
            GameLocation location = Game1.currentLocation;

            TooltipModel? tooltip = null;

            // 1. Check Objects (Machines, Casks, IndoorPots, CrabPots, etc.)
            // TryGetValue = single-step dictionary lookup that also outputs the found value;
            // location.Objects maps each tile position to whatever is placed there.
            if (!location.Objects.TryGetValue(tilePos, out var obj))
            {
                // Fallback: check if the tile directly below has a BigCraftable spanning into this tile
                if (location.Objects.TryGetValue(new Vector2(tilePos.X, tilePos.Y + 1), out var belowObj) && belowObj.bigCraftable.Value)
                {
                    obj = belowObj;
                }
            }

            if (obj != null)
            {
                // "is IndoorPot pot" = pattern matching: type test + cast + new variable in one go.
                if (obj is IndoorPot pot)
                {
                    if (Config.EnableCropHover && pot.hoeDirt.Value != null)
                    {
                        var dirt = pot.hoeDirt.Value;
                        if (dirt.crop != null || dirt.state.Value == HoeDirt.watered || !string.IsNullOrEmpty(dirt.fertilizer.Value))
                        {
                            tooltip = BuildCropTooltip(dirt, isGardenPot: true);
                        }
                    }
                    else if (Config.EnableTreeHover && pot.bush.Value != null)
                    {
                        tooltip = BuildBushTooltip(pot.bush.Value);
                    }
                }
                else if (Config.EnableMachineHover)
                {
                    var machineInfo = MachineHelper.GetMachineInfo(obj);
                    if (machineInfo != null)
                    {
                        tooltip = BuildMachineTooltip(machineInfo);
                    }
                }
            }

            // 2. Check Terrain Features (Crops in HoeDirt, Fruit Trees, Wild Trees, Bushes, Giant Crops)
            // TerrainFeatures is tile-keyed just like Objects; only tried if nothing matched above.
            if (tooltip == null && location.terrainFeatures.TryGetValue(tilePos, out var feature))
            {
                if (Config.EnableCropHover && feature is HoeDirt hoeDirt)
                {
                    if (hoeDirt.crop != null || hoeDirt.state.Value == HoeDirt.watered || !string.IsNullOrEmpty(hoeDirt.fertilizer.Value))
                    {
                        tooltip = BuildCropTooltip(hoeDirt, isGardenPot: false);
                    }
                }
                else if (Config.EnableTreeHover && feature is FruitTree fruitTree)
                {
                    tooltip = BuildFruitTreeTooltip(fruitTree);
                }
                else if (Config.EnableTreeHover && feature is Tree tree)
                {
                    tooltip = BuildTreeTooltip(tree);
                }
                else if (Config.EnableTreeHover && feature is Bush bush)
                {
                    tooltip = BuildBushTooltip(bush);
                }
                else if (Config.EnableCropHover && feature is GiantCrop giantCrop)
                {
                    tooltip = BuildGiantCropTooltip(giantCrop);
                }
            }

            // 3. Check Large Terrain Features (Bushes spanning larger areas)
            // These aren't stored in a tile dictionary, so scan the list and pixel-test
            // each one's bounding box instead.
            if (tooltip == null && Config.EnableTreeHover)
            {
                foreach (var largeFeature in location.largeTerrainFeatures)
                {
                    // Rectangle.Contains: point-in-rect hit test; casts truncate float pixels to ints.
                    if (largeFeature is Bush largeBush && largeBush.getBoundingBox().Contains((int)absolutePixels.X, (int)absolutePixels.Y))
                    {
                        tooltip = BuildBushTooltip(largeBush);
                        break;
                    }
                }
            }

            // 4. Check Farm Animals
            // .Values iterates only the animals of this name-keyed multiplayer-synced dictionary.
            if (tooltip == null && Config.EnableAnimalHover)
            {
                foreach (var animal in location.animals.Values)
                {
                    if (animal != null && animal.GetBoundingBox().Contains((int)absolutePixels.X, (int)absolutePixels.Y))
                    {
                        var animalInfo = AnimalHelper.GetFarmAnimalInfo(animal);
                        if (animalInfo != null)
                        {
                            tooltip = BuildAnimalTooltip(animalInfo);
                            break;
                        }
                    }
                }
            }

            // 5. Check Pets
            if (tooltip == null && Config.EnableAnimalHover)
            {
                foreach (var character in location.characters)
                {
                    if (character is Pet pet && pet.GetBoundingBox().Contains((int)absolutePixels.X, (int)absolutePixels.Y))
                    {
                        var petInfo = AnimalHelper.GetPetInfo(pet);
                        if (petInfo != null)
                        {
                            tooltip = BuildAnimalTooltip(petInfo);
                            break;
                        }
                    }
                }
            }

            // 6. Check Buildings (Fish Pond, Mill, Junimo Hut, Silo, Shipping Bin, etc.)
            // Buildings cover many tiles at once; occupiesTile() knows their full footprint.
            if (tooltip == null && Config.EnableMachineHover && location.buildings.Count > 0)
            {
                foreach (var building in location.buildings)
                {
                    if (building != null && building.occupiesTile(tilePos))
                    {
                        var buildingInfo = MachineHelper.GetBuildingInfo(building);
                        if (buildingInfo != null)
                        {
                            tooltip = BuildBuildingTooltip(buildingInfo);
                            break;
                        }
                    }
                }
            }

            // Render World Tooltip if found
            if (tooltip != null)
            {
                DrawWorldTooltip(e.SpriteBatch, tooltip, screenPos);
            }
        }

        #endregion

        #region 2. Tooltip Builders

        /// <summary>
        /// Composes the crop tooltip: name, days remaining / ready state, regrow cycle,
        /// plus watered &amp; fertilizer status.
        /// </summary>
        /// <param name="hoeDirt">Tilled-soil object holding the crop (in ground or garden pot).</param>
        /// <param name="isGardenPot">True tweaks wording for crops growing inside IndoorPots.</param>
        /// <returns>A ready-to-render tooltip model.</returns>
        private static TooltipModel BuildCropTooltip(HoeDirt hoeDirt, bool isGardenPot)
        {
            var info = CropHelper.GetCropInfo(hoeDirt);
            if (info == null)
                return new TooltipModel { Title = ModEntry.I18n.Get("hover.crop.generic").ToString() };

            string title = info.CropName;
            string? subtitle = null;
            if (info.IsHoeDirtOnly)
            {
                if (isGardenPot)
                {
                    title = ModEntry.I18n.Get("hover.type.garden-pot-empty").ToString();
                }
                else
                {
                    title = ModEntry.I18n.Get("hover.dirt.tilled").ToString();
                }
            }
            else
            {
                subtitle = isGardenPot ? ModEntry.I18n.Get("hover.type.garden-pot-crop").ToString() : null;
            }

            var tooltip = new TooltipModel
            {
                Title = title,
                Subtitle = subtitle,
                IconTexture = Config.ShowItemIconInTooltip ? info.IconTexture : null,
                IconSourceRect = Config.ShowItemIconInTooltip ? info.IconSourceRect : null
            };

            if (info.IsDead)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.dead-warning"), Color.Red));
                return tooltip;
            }

            if (!info.IsHoeDirtOnly)
            {
                // Days remaining / Ready state
                if (info.IsReadyToHarvest)
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.ready-to-harvest"), new Color(0, 140, 0)));
                }
                else
                {
                    if (info.DaysRemaining == 1)
                    {
                        tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.ready-tomorrow", new { stage = info.CurrentStage, totalStages = info.TotalStages }), new Color(180, 100, 0)));
                    }
                    else
                    {
                        tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.ready-in-days", new { days = info.DaysRemaining, stage = info.CurrentStage, totalStages = info.TotalStages }), Game1.textColor));
                    }
                }

                // Regrow schedule
                if (info.IsRegrowable)
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.regrow-cycle", new { days = info.RegrowDays }), Color.DarkSlateGray));
                }
            }

            // Water & Fertilizer
            if (Config.ShowWaterAndFertilizer)
            {
                if (info.IsWatered)
                {
                    // Ternary "condition ? a : b": pick paddy-specific phrasing for rice-style watering.
                    string waterText = info.IsPaddyWatered
                        ? ModEntry.I18n.Get("hover.crop.watered-paddy")
                        : ModEntry.I18n.Get("hover.crop.watered");
                    tooltip.Lines.Add(new TooltipLine(waterText, new Color(20, 110, 220)));
                }
                else
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.unwatered"), new Color(200, 60, 20)));
                }

                if (info.FertilizerNames.Count > 0)
                {
                    foreach (var fertName in info.FertilizerNames)
                    {
                        tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.fertilizer", new { name = fertName }), new Color(46, 125, 50)));
                    }
                }
                else if (!string.IsNullOrEmpty(info.FertilizerName))
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.fertilizer", new { name = info.FertilizerName }), new Color(46, 125, 50)));
                }
            }

            return tooltip;
        }

        /// <summary>
        /// Composes the machine tooltip: held input + quality, readiness, cask aging,
        /// crab pot bait state, idle text, or processing countdown - depending on flags.
        /// </summary>
        private static TooltipModel BuildMachineTooltip(MachineInfo info)
        {
            var tooltip = new TooltipModel
            {
                Title = info.MachineName,
                Subtitle = null,
                IconTexture = Config.ShowItemIconInTooltip ? info.HeldItemTexture : null,
                IconSourceRect = Config.ShowItemIconInTooltip ? info.HeldItemSourceRect : null
            };

            // Held item product
            if (!string.IsNullOrEmpty(info.HeldItemName))
            {
                // Append "xN" for stacked inputs; $"" interpolates variables into text.
                string heldLabel = info.HeldItemStack > 1
                    ? $"{info.HeldItemName} x{info.HeldItemStack}"
                    : info.HeldItemName;

                if (info.HeldItemQuality > 0)
                {
                    // Switch EXPRESSION (C# 8): each arm maps a quality code to a label;
                    // "_" is the default arm. Quality codes: 1=silver, 2=gold, 4=iridium.
                    string qualityStar = info.HeldItemQuality switch
                    {
                        1 => $" ({ModEntry.I18n.Get("hover.quality.silver")})",
                        2 => $" ({ModEntry.I18n.Get("hover.quality.gold")})",
                        4 => $" ({ModEntry.I18n.Get("hover.quality.iridium")})",
                        _ => string.Empty
                    };
                    heldLabel += qualityStar;
                }

                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.machine.producing", new { item = heldLabel }), Game1.textColor));
            }

            // Ready state
            // Finished goods wait for pickup - green emphasis, then stop adding lines.
            if (info.IsReadyToHarvest)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.machine.ready-to-collect"), new Color(0, 140, 0)));
                return tooltip;
            }

            // Cask Aging Info
            // Casks raise wine/cheese/roe quality over real time; show both the days to
            // the NEXT tier and to full iridium.
            if (info.IsCask && info.IsProcessing)
            {
                // Same switch-expression trick as above, for the cask's target quality.
                string nextQualityName = info.CaskNextQuality switch
                {
                    1 => ModEntry.I18n.Get("hover.quality.silver"),
                    2 => ModEntry.I18n.Get("hover.quality.gold"),
                    4 => ModEntry.I18n.Get("hover.quality.iridium"),
                    _ => string.Empty
                };

                tooltip.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.cask.aging-to-next", new { quality = nextQualityName, days = info.CaskDaysToNextQuality }),
                    new Color(180, 100, 0)
                ));

                if (info.CaskNextQuality != 4)
                {
                    tooltip.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.cask.aging-to-iridium", new { days = info.CaskDaysToIridium }),
                        Color.DarkSlateGray
                    ));
                }
                return tooltip;
            }

            // Crab Pot
            if (info.IsCrabPot)
            {
                if (info.CrabPotHasBait)
                {
                    // ?? (null-coalescing): fall back to generic text when bait name is unknown.
                    string baitName = info.CrabPotBaitName ?? ModEntry.I18n.Get("hover.crabpot.default-bait").ToString();
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crabpot.baited", new { bait = baitName }), new Color(20, 110, 220)));
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crabpot.catching"), Color.DarkSlateGray));
                }
                else
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crabpot.needs-bait"), new Color(200, 60, 20)));
                }
                return tooltip;
            }

            // Idle state
            // Custom idle text when available, otherwise the generic translation (??.)
            if (info.IsIdle)
            {
                tooltip.Lines.Add(new TooltipLine(info.IdleStatusText ?? ModEntry.I18n.Get("hover.machine.idle"), Color.DarkSlateGray));
                return tooltip;
            }

            // Processing countdown
            if (info.IsProcessing)
            {
                if (!string.IsNullOrEmpty(info.TimeRemainingText))
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.machine.time-remaining", new { time = info.TimeRemainingText }), new Color(180, 100, 0)));
                }

                if (Config.ShowExactFinishTime && !string.IsNullOrEmpty(info.TargetFinishTimeText))
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.machine.ready-at", new { time = info.TargetFinishTimeText }), Color.DarkSlateGray));
                }
            }

            return tooltip;
        }

        /// <summary>
        /// Wraps pre-computed building lines (fish ponds, mills, silos...) into a tooltip.
        /// The nullable "?" return signals "nothing worth showing for this building".
        /// </summary>
        private static TooltipModel? BuildBuildingTooltip(BuildingMachineInfo info)
        {
            if (info == null || info.Lines.Count == 0)
                return null;

            var tooltip = new TooltipModel
            {
                Title = info.BuildingName,
                Subtitle = info.Subtitle,
                IconTexture = Config.ShowItemIconInTooltip ? info.IconTexture : null,
                IconSourceRect = Config.ShowItemIconInTooltip ? info.IconSourceRect : null
            };

            foreach (var line in info.Lines)
            {
                tooltip.Lines.Add(line);
            }

            return tooltip;
        }

        /// <summary>
        /// Composes the fruit tree tooltip: maturation countdown, fruit count or season
        /// status, fertilizer and lightning state.
        /// </summary>
        private static TooltipModel BuildFruitTreeTooltip(FruitTree fruitTree)
        {
            var info = TreeHelper.GetFruitTreeInfo(fruitTree);
            // "?." (null-conditional) reads Name only if info isn't null;
            // "??" substitutes a generic title when info itself is null.
            var tooltip = new TooltipModel
            {
                Title = info?.Name ?? ModEntry.I18n.Get("hover.fruit-tree.generic"),
                Subtitle = null,
                IconTexture = Config.ShowItemIconInTooltip ? info?.IconTexture : null,
                IconSourceRect = Config.ShowItemIconInTooltip ? info?.IconSourceRect : null
            };

            if (info == null)
                return tooltip;

            if (!info.IsMature)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.fruit-tree.maturing", new { days = info.DaysUntilMature }), new Color(180, 100, 0)));
                if (info.IsFertilized)
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.fruit-tree.fertilized"), new Color(46, 125, 50)));
                }
                return tooltip;
            }

            // Lightning-struck trees slowly turn to coal - warn prominently in red.
            if (info.StruckByLightning)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.fruit-tree.lightning", new { days = info.LightningDaysRemaining }), Color.Red));
            }

            if (info.FruitsOnTree > 0)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.fruit-tree.fruit-count", new { count = info.FruitsOnTree }), new Color(0, 140, 0)));
            }
            else
            {
                if (info.IsInSeason)
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.fruit-tree.in-season"), new Color(20, 110, 220)));
                }
                else
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.fruit-tree.out-of-season"), Color.DarkSlateGray));
                }
            }

            return tooltip;
        }

        /// <summary>
        /// Composes the wild tree tooltip: growth stage out of 5, moss, and tapper status.
        /// </summary>
        private static TooltipModel BuildTreeTooltip(Tree tree)
        {
            var info = TreeHelper.GetTreeInfo(tree);
            var tooltip = new TooltipModel
            {
                Title = info?.Name ?? ModEntry.I18n.Get("hover.tree.generic")
            };

            if (info == null)
                return tooltip;

            if (!info.IsMature)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.tree.stage", new { stage = info.GrowthStage + 1, total = 5 }), new Color(180, 100, 0)));
                // Engine stores growth stages 0-based (+1 above); players read "stage X of 5".
                if (info.IsFertilized)
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.tree.fertilized"), new Color(46, 125, 50)));
                }
            }
            else
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.tree.fully-grown"), new Color(0, 140, 0)));
            }

            if (info.HasMoss)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.tree.has-moss"), new Color(46, 125, 50)));
            }

            if (info.IsTapped)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.tree.tapped"), new Color(20, 110, 220)));
            }

            return tooltip;
        }

        /// <summary>
        /// Composes the bush tooltip: tea bush maturity/harvest window or berry bloom state.
        /// Returns null for unharvestable decorative or non-blooming berry bushes to prevent empty tooltips.
        /// </summary>
        private static TooltipModel? BuildBushTooltip(Bush bush)
        {
            var info = TreeHelper.GetBushInfo(bush);
            if (info == null)
                return null;

            // Only show tooltips for berry bushes if they are actively blooming (ready to harvest)
            if (!info.IsTeaBush && !info.IsInBloom)
                return null;

            var tooltip = new TooltipModel
            {
                Title = info.Name
            };

            if (info.IsTeaBush && !info.IsMature)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.bush.tea-maturing", new { days = info.DaysUntilMature }), new Color(180, 100, 0)));
            }
            else if (info.IsInBloom)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.bush.ready-to-harvest"), new Color(0, 140, 0)));
            }
            else if (info.IsTeaBush)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.bush.tea-harvest-window"), Color.DarkSlateGray));
            }

            return tooltip;
        }

        /// <summary>
        /// Composes the giant crop tooltip (harvestable with an axe) plus its produce icon.
        /// </summary>
        private static TooltipModel BuildGiantCropTooltip(GiantCrop giantCrop)
        {
            string cropId = giantCrop.Id;
            // Fallback chain: try the raw id, then the "(O)"-qualified form; "??" keeps
            // trying when GetData misses, and "?." guards the DisplayName access.
            var itemData = ItemRegistry.GetData(cropId) ?? ItemRegistry.GetData($"(O){cropId}");
            string displayName = itemData?.DisplayName ?? ModEntry.I18n.Get("hover.giant-crop.generic");

            var tooltip = new TooltipModel
            {
                Title = ModEntry.I18n.Get("hover.giant-crop.title", new { name = displayName })
            };

            if (Config.ShowItemIconInTooltip && itemData != null)
            {
                // Some custom crops expose broken sprite data; ignore failures quietly.
                try
                {
                    tooltip.IconTexture = itemData.GetTexture();
                    tooltip.IconSourceRect = itemData.GetSourceRect();
                }
                catch { }
            }

            tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.giant-crop.ready-axe"), new Color(0, 140, 0)));
            return tooltip;
        }

        /// <summary>
        /// Composes the animal/pet tooltip: petted-today status, heart meter, ready produce.
        /// </summary>
        private static TooltipModel BuildAnimalTooltip(AnimalInfo info)
        {
            var tooltip = new TooltipModel
            {
                // $"" string interpolation embeds name/type straight into the title.
                Title = $"{info.Name} ({info.TypeName})"
            };

            // Pet status
            if (info.WasPetToday)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.animal.petted-today"), new Color(0, 140, 0)));
            }
            else
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.animal.needs-petting"), new Color(200, 60, 20)));
            }

            // Friendship
            // ":0.0" is a format specifier rounding the float to exactly one decimal ("3.5").
            tooltip.Lines.Add(new TooltipLine(
                ModEntry.I18n.Get("hover.animal.friendship", new { hearts = $"{info.Hearts:0.0}", max = "5.0" }),
                new Color(220, 20, 60)
            ));

            // Produce
            if (info.HasProduceReady && !string.IsNullOrEmpty(info.ProduceName))
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.animal.produce-ready", new { item = info.ProduceName }), new Color(20, 110, 220)));
            }

            return tooltip;
        }

        #endregion

        #region 3. Polished Compact UI Rendering

        /// <summary>
        /// Measures content, positions a parchment panel near the cursor (clamped to stay
        /// on-screen), then draws background -> icon -> title -> divider -> body lines in order.
        /// </summary>
        private static void DrawWorldTooltip(SpriteBatch spriteBatch, TooltipModel tooltip, Vector2 screenPos)
        {
            // A SpriteFont is a packed glyph sheet plus metrics; smallFont is the game's
            // compact UI face. The consts below are tuned pixel spacing values.
            SpriteFont font = Game1.smallFont;
            const int paddingX = 26;
            const int paddingY = 22;
            const int iconSize = 28;
            const int lineSpacing = 24;

            // Measure Title
            // MeasureString computes on-screen pixel size BEFORE drawing - essential for layout.
            Vector2 titleSize = font.MeasureString(tooltip.Title);
            float headerWidth = titleSize.X;
            // Reserve room beside the title when an icon will share the header row.
            if (tooltip.IconTexture != null)
            {
                headerWidth += iconSize + 10;
            }

            // Measure Lines with wrapping if necessary
            float maxLineWidth = headerWidth;
            // Named TUPLE elements let one list carry wrapped text + measured size + color together.
            var processedLines = new List<(string WrappedText, Vector2 Size, Color Color)>();

            foreach (var line in tooltip.Lines)
            {
                // Game1.parseText word-wraps text to a 340px column, inserting newlines as needed.
                string wrapped = Game1.parseText(line.Text, font, 340);
                Vector2 size = font.MeasureString(wrapped);
                if (size.X > maxLineWidth)
                {
                    maxLineWidth = size.X;
                }
                processedLines.Add((wrapped, size, line.Color));
            }

            // Widen for horizontal padding; never narrower than 230px so tiny tooltips stay readable.
            int boxWidth = (int)Math.Max(230, maxLineWidth + (paddingX * 2) + 16);
            int headerHeight = (int)Math.Max(iconSize, titleSize.Y);

            // Total body height = per line the larger of fixed spacing vs. actual text height.
            int contentLinesHeight = 0;
            foreach (var pl in processedLines)
            {
                contentLinesHeight += (int)Math.Max(lineSpacing, pl.Size.Y + 2);
            }

            int dividerHeight = 12;
            int boxHeight = (paddingY * 2) + headerHeight + dividerHeight + contentLinesHeight + 12;

            // Compute Position with screen boundary clamping
            // Offset 24px from cursor; flip to the other side near right/bottom edges;
            // Math.Max/Min chains guarantee the whole box stays visible.
            int targetX = (int)screenPos.X + 24;
            int targetY = (int)screenPos.Y + 24;

            if (targetX + boxWidth > Game1.uiViewport.Width)
            {
                targetX = (int)screenPos.X - boxWidth - 16;
            }
            if (targetY + boxHeight > Game1.uiViewport.Height)
            {
                targetY = (int)screenPos.Y - boxHeight - 16;
            }

            targetX = Math.Max(12, Math.Min(targetX, Game1.uiViewport.Width - boxWidth - 12));
            targetY = Math.Max(12, Math.Min(targetY, Game1.uiViewport.Height - boxHeight - 12));

            // Native Stardew Parchment Box
            // drawTextureBox nine-slice-stretches a 60x60 region of menuTexture into our
            // rectangle (corners stay crisp, edges stretch); drawShadow adds a drop shadow,
            // making this look identical to vanilla's built-in tooltip panels.
            IClickableMenu.drawTextureBox(
                spriteBatch,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                targetX,
                targetY,
                boxWidth,
                boxHeight,
                Color.White,
                1f,
                drawShadow: true
            );

            float currentY = targetY + paddingY;
            float textStartX = targetX + paddingX;

            // Draw Icon
            if (tooltip.IconTexture != null)
            {
                // Missing source rect? Fall back to the ENTIRE texture ("??" again).
                Rectangle srcRect = tooltip.IconSourceRect ?? new Rectangle(0, 0, tooltip.IconTexture.Width, tooltip.IconTexture.Height);
                Rectangle destRect = new Rectangle(targetX + paddingX, (int)currentY, iconSize, iconSize);

                // Two Draw calls: a translucent black copy offset 1px (fake shadow), then the
                // real sprite on top. "Color * float" scales alpha/rgb - a common MonoGame tint trick.
                spriteBatch.Draw(tooltip.IconTexture, new Rectangle(destRect.X + 1, destRect.Y + 1, iconSize, iconSize), srcRect, Color.Black * 0.35f);
                spriteBatch.Draw(tooltip.IconTexture, destRect, srcRect, Color.White);

                // Push the title text right past the icon column.
                textStartX += iconSize + 10;
            }

            // Draw Title
            // Utility.drawTextWithShadow draws text twice (dark offset underneath) for contrast.
            Utility.drawTextWithShadow(spriteBatch, tooltip.Title, font, new Vector2(textStartX, currentY + 1), Game1.textColor);
            // Walk currentY downward as each element is painted - classic immediate-mode layout.
            currentY += headerHeight + 6;

            // Subtle divider line
            // staminaRect is a 1x1 white texture; stretching + alpha-tinting it draws flat rules.
            spriteBatch.Draw(Game1.staminaRect, new Rectangle(targetX + paddingX, (int)currentY, boxWidth - (paddingX * 2), 2), Color.SaddleBrown * 0.25f);
            currentY += 8;

            // Draw Content Lines
            // Body lines reuse the measurements cached above, so wrapping costs nothing extra.
            foreach (var pl in processedLines)
            {
                Utility.drawTextWithShadow(spriteBatch, pl.WrappedText, font, new Vector2(targetX + paddingX, currentY), pl.Color);
                currentY += (int)Math.Max(lineSpacing, pl.Size.Y + 2);
            }
        }

        #endregion
    }
}
