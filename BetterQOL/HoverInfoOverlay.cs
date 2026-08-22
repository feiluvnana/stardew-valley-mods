using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace BetterQOL
{
    public class TooltipLine
    {
        public string Text { get; set; } = string.Empty;
        public Color Color { get; set; } = Game1.textColor;

        public TooltipLine(string text, Color color)
        {
            Text = text;
            Color = color;
        }
    }

    public class TooltipModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public Texture2D? IconTexture { get; set; }
        public Rectangle? IconSourceRect { get; set; }
        public List<TooltipLine> Lines { get; set; } = new();
    }

    public static class HoverInfoOverlay
    {
        private static IModHelper Helper = null!;
        private static IMonitor Monitor = null!;
        private static ModConfig Config => ModEntry.Config;

        public static void Initialize(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            helper.Events.Display.RenderedHud += OnRenderedHud;
        }

        private static void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.currentLocation == null)
                return;

            if (Game1.activeClickableMenu != null || Game1.eventUp || Game1.farmEvent != null)
                return;

            if (Config.HoverHotkey != SButton.None && !Helper.Input.IsDown(Config.HoverHotkey))
                return;

            var cursor = Helper.Input.GetCursorPosition();
            Vector2 tilePos = cursor.Tile;
            Vector2 screenPos = cursor.GetScaledScreenPixels();
            Vector2 absolutePixels = cursor.AbsolutePixels;
            GameLocation location = Game1.currentLocation;

            TooltipModel? tooltip = null;

            // 1. Check Objects (Machines, Casks, IndoorPots, CrabPots, etc.)
            if (location.Objects.TryGetValue(tilePos, out var obj))
            {
                if (obj is IndoorPot pot)
                {
                    if (Config.EnableCropHover && pot.hoeDirt.Value?.crop != null)
                    {
                        tooltip = BuildCropTooltip(pot.hoeDirt.Value, isGardenPot: true);
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
            if (tooltip == null && location.terrainFeatures.TryGetValue(tilePos, out var feature))
            {
                if (Config.EnableCropHover && feature is HoeDirt hoeDirt && hoeDirt.crop != null)
                {
                    tooltip = BuildCropTooltip(hoeDirt, isGardenPot: false);
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
            if (tooltip == null && Config.EnableTreeHover)
            {
                foreach (var largeFeature in location.largeTerrainFeatures)
                {
                    if (largeFeature is Bush largeBush && largeBush.getBoundingBox().Contains((int)absolutePixels.X, (int)absolutePixels.Y))
                    {
                        tooltip = BuildBushTooltip(largeBush);
                        break;
                    }
                }
            }

            // 4. Check Farm Animals
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

            // Render Tooltip if found
            if (tooltip != null)
            {
                DrawTooltip(e.SpriteBatch, tooltip, screenPos);
            }
        }

        #region Tooltip Builders

        private static TooltipModel BuildCropTooltip(HoeDirt hoeDirt, bool isGardenPot)
        {
            var info = CropHelper.GetCropInfo(hoeDirt);
            var tooltip = new TooltipModel
            {
                Title = info?.CropName ?? ModEntry.I18n.Get("hover.crop.generic"),
                Subtitle = isGardenPot ? ModEntry.I18n.Get("hover.type.garden-pot-crop") : ModEntry.I18n.Get("hover.type.crop"),
                IconTexture = Config.ShowItemIconInTooltip ? info?.IconTexture : null,
                IconSourceRect = Config.ShowItemIconInTooltip ? info?.IconSourceRect : null
            };

            if (info == null)
                return tooltip;

            if (info.IsDead)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.dead-warning"), Color.Red));
                return tooltip;
            }

            // Ready / Growth time
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

            // Regrow info
            if (info.IsRegrowable)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.regrow-cycle", new { days = info.RegrowDays }), Color.DarkSlateGray));
            }

            // Water & Fertilizer
            if (Config.ShowWaterAndFertilizer)
            {
                if (info.IsWatered)
                {
                    string waterText = info.IsPaddyWatered
                        ? ModEntry.I18n.Get("hover.crop.watered-paddy")
                        : ModEntry.I18n.Get("hover.crop.watered");
                    tooltip.Lines.Add(new TooltipLine(waterText, new Color(20, 110, 220)));
                }
                else
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.unwatered"), new Color(200, 60, 20)));
                }

                if (!string.IsNullOrEmpty(info.FertilizerName))
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crop.fertilizer", new { name = info.FertilizerName }), new Color(46, 125, 50)));
                }
            }

            return tooltip;
        }

        private static TooltipModel BuildMachineTooltip(MachineInfo info)
        {
            var tooltip = new TooltipModel
            {
                Title = info.MachineName,
                Subtitle = ModEntry.I18n.Get("hover.type.machine"),
                IconTexture = Config.ShowItemIconInTooltip ? info.HeldItemTexture : null,
                IconSourceRect = Config.ShowItemIconInTooltip ? info.HeldItemSourceRect : null
            };

            // Held item name
            if (!string.IsNullOrEmpty(info.HeldItemName))
            {
                string heldLabel = info.HeldItemStack > 1
                    ? $"{info.HeldItemName} x{info.HeldItemStack}"
                    : info.HeldItemName;

                if (info.HeldItemQuality > 0)
                {
                    string qualityStar = info.HeldItemQuality switch
                    {
                        1 => " (★ Silver)",
                        2 => " (★★ Gold)",
                        4 => " (★★★ Iridium)",
                        _ => string.Empty
                    };
                    heldLabel += qualityStar;
                }

                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.machine.producing", new { item = heldLabel }), Game1.textColor));
            }

            // Ready state
            if (info.IsReadyToHarvest)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.machine.ready-to-collect"), new Color(0, 140, 0)));
                return tooltip;
            }

            // Cask Aging Info
            if (info.IsCask && info.IsProcessing)
            {
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
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crabpot.baited", new { bait = info.CrabPotBaitName ?? "Bait" }), new Color(20, 110, 220)));
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crabpot.catching"), Color.DarkSlateGray));
                }
                else
                {
                    tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.crabpot.needs-bait"), new Color(200, 60, 20)));
                }
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

        private static TooltipModel BuildFruitTreeTooltip(FruitTree fruitTree)
        {
            var info = TreeHelper.GetFruitTreeInfo(fruitTree);
            var tooltip = new TooltipModel
            {
                Title = info?.Name ?? ModEntry.I18n.Get("hover.fruit-tree.generic"),
                Subtitle = info?.Subtitle,
                IconTexture = Config.ShowItemIconInTooltip ? info?.IconTexture : null,
                IconSourceRect = Config.ShowItemIconInTooltip ? info?.IconSourceRect : null
            };

            if (info == null)
                return tooltip;

            if (!info.IsMature)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.fruit-tree.maturing", new { days = info.DaysUntilMature }), new Color(180, 100, 0)));
                return tooltip;
            }

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

        private static TooltipModel BuildTreeTooltip(Tree tree)
        {
            var info = TreeHelper.GetTreeInfo(tree);
            var tooltip = new TooltipModel
            {
                Title = info?.Name ?? ModEntry.I18n.Get("hover.tree.generic"),
                Subtitle = info?.Subtitle
            };

            if (info == null)
                return tooltip;

            if (!info.IsMature)
            {
                tooltip.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.tree.stage", new { stage = info.GrowthStage + 1, total = 5 }), new Color(180, 100, 0)));
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

        private static TooltipModel BuildBushTooltip(Bush bush)
        {
            var info = TreeHelper.GetBushInfo(bush);
            var tooltip = new TooltipModel
            {
                Title = info?.Name ?? ModEntry.I18n.Get("hover.bush.generic"),
                Subtitle = info?.Subtitle
            };

            if (info == null)
                return tooltip;

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

        private static TooltipModel BuildGiantCropTooltip(GiantCrop giantCrop)
        {
            string cropId = giantCrop.Id;
            var itemData = ItemRegistry.GetData(cropId) ?? ItemRegistry.GetData($"(O){cropId}");
            string displayName = itemData?.DisplayName ?? ModEntry.I18n.Get("hover.giant-crop.generic");

            var tooltip = new TooltipModel
            {
                Title = ModEntry.I18n.Get("hover.giant-crop.title", new { name = displayName }),
                Subtitle = ModEntry.I18n.Get("hover.type.giant-crop")
            };

            if (Config.ShowItemIconInTooltip && itemData != null)
            {
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

        private static TooltipModel BuildAnimalTooltip(AnimalInfo info)
        {
            var tooltip = new TooltipModel
            {
                Title = info.Name,
                Subtitle = info.TypeName
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

        #region Tooltip Rendering

        private static void DrawTooltip(SpriteBatch spriteBatch, TooltipModel tooltip, Vector2 screenPos)
        {
            SpriteFont font = Game1.smallFont;
            const int paddingX = 18;
            const int paddingY = 14;
            const int iconSize = 32;
            const int lineSpacing = 24;

            // Measure Title & Subtitle
            Vector2 titleSize = font.MeasureString(tooltip.Title);
            Vector2 subSize = !string.IsNullOrEmpty(tooltip.Subtitle) ? font.MeasureString(tooltip.Subtitle) : Vector2.Zero;

            float headerWidth = titleSize.X;
            if (tooltip.IconTexture != null)
            {
                headerWidth += iconSize + 10;
            }
            if (subSize.X > 0)
            {
                headerWidth = Math.Max(headerWidth, subSize.X);
            }

            // Measure Lines
            float maxLineWidth = headerWidth;
            foreach (var line in tooltip.Lines)
            {
                Vector2 lineSize = font.MeasureString(line.Text);
                if (lineSize.X > maxLineWidth)
                {
                    maxLineWidth = lineSize.X;
                }
            }

            int boxWidth = (int)Math.Max(220, maxLineWidth + (paddingX * 2));
            int headerHeight = (int)Math.Max(iconSize, titleSize.Y + (subSize.Y > 0 ? subSize.Y * 0.8f : 0));
            int contentHeight = headerHeight + 8 + (tooltip.Lines.Count * lineSpacing);
            int boxHeight = contentHeight + (paddingY * 2);

            // Compute Position with screen boundary clamping
            int targetX = (int)screenPos.X + 24;
            int targetY = (int)screenPos.Y + 24;

            if (targetX + boxWidth > Game1.uiViewport.Width)
            {
                targetX = (int)screenPos.X - boxWidth - 12;
            }
            if (targetY + boxHeight > Game1.uiViewport.Height)
            {
                targetY = (int)screenPos.Y - boxHeight - 12;
            }

            targetX = Math.Max(8, Math.Min(targetX, Game1.uiViewport.Width - boxWidth - 8));
            targetY = Math.Max(8, Math.Min(targetY, Game1.uiViewport.Height - boxHeight - 8));

            // Draw Background Box
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

            // Draw Header
            float currentY = targetY + paddingY;
            float textStartX = targetX + paddingX;

            if (tooltip.IconTexture != null)
            {
                Rectangle srcRect = tooltip.IconSourceRect ?? new Rectangle(0, 0, tooltip.IconTexture.Width, tooltip.IconTexture.Height);
                Rectangle destRect = new Rectangle(targetX + paddingX, (int)currentY, iconSize, iconSize);

                // Icon shadow
                spriteBatch.Draw(tooltip.IconTexture, new Rectangle(destRect.X + 2, destRect.Y + 2, iconSize, iconSize), srcRect, Color.Black * 0.3f);
                spriteBatch.Draw(tooltip.IconTexture, destRect, srcRect, Color.White);

                textStartX += iconSize + 10;
            }

            // Draw Title
            Utility.drawTextWithShadow(spriteBatch, tooltip.Title, font, new Vector2(textStartX, currentY), Game1.textColor);

            // Draw Subtitle
            if (!string.IsNullOrEmpty(tooltip.Subtitle))
            {
                float subY = currentY + titleSize.Y - 2;
                Utility.drawTextWithShadow(spriteBatch, tooltip.Subtitle, font, new Vector2(textStartX, subY), Color.DimGray, 0.85f);
                currentY = subY + (subSize.Y * 0.85f) + 6;
            }
            else
            {
                currentY += Math.Max(iconSize, titleSize.Y) + 6;
            }

            // Draw subtle divider
            int dividerY = (int)currentY;
            spriteBatch.Draw(Game1.staminaRect, new Rectangle(targetX + paddingX, dividerY, boxWidth - (paddingX * 2), 2), Color.SaddleBrown * 0.25f);
            currentY += 6;

            // Draw Content Lines
            foreach (var line in tooltip.Lines)
            {
                Utility.drawTextWithShadow(spriteBatch, line.Text, font, new Vector2(targetX + paddingX, currentY), line.Color);
                currentY += lineSpacing;
            }
        }

        #endregion
    }
}
