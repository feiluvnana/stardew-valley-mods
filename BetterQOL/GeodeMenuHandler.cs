using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace BetterQOL
{
    public static class GeodeMenuHandler
    {
        public const int CrackAllButtonID = 99801;

        private static IModHelper Helper = null!;
        private static IMonitor Monitor = null!;
        private static ModConfig Config => ModEntry.Config;

        public static ClickableComponent? CrackAllButton { get; private set; }
        private static bool isHoveringCrackAll = false;
        private static int lastAnimationTimer = 0;

        public static void Initialize(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            helper.Events.Display.WindowResized += OnWindowResized;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private static void UpdateCrackAllButton(GeodeMenu menu)
        {
            if (!Config.ShowCrackAllButton)
            {
                if (CrackAllButton != null && menu.allClickableComponents != null)
                {
                    menu.allClickableComponents.Remove(CrackAllButton);
                }
                CrackAllButton = null;

                if (menu.geodeSpot != null)
                {
                    menu.geodeSpot.rightNeighborID = (menu.trashCan != null) ? menu.trashCan.myID : (menu.okButton != null ? menu.okButton.myID : -99998);
                }
                if (menu.trashCan != null)
                {
                    menu.trashCan.leftNeighborID = 11;
                }
                if (menu.inventory?.inventory != null)
                {
                    foreach (var comp in menu.inventory.inventory)
                    {
                        if (comp != null && comp.myID < 12)
                        {
                            comp.upNeighborID = GeodeMenu.region_geodeSpot;
                        }
                    }
                }
                return;
            }

            string label = ModEntry.I18n.Get("button.crack-all");
            int textWidth = (int)Game1.smallFont.MeasureString(label).X;
            int btnWidth = Math.Max(140, textWidth + 32);
            int btnHeight = 44;
            int btnX = menu.geodeSpot.bounds.Right - btnWidth - 16;
            int btnY = menu.geodeSpot.bounds.Y + 16;

            int rightTargetID = (menu.trashCan != null) ? menu.trashCan.myID : (menu.okButton != null ? menu.okButton.myID : -99998);

            // Find closest inventory slot under button for gamepad Down navigation
            int closestSlotId = -99998;
            if (menu.inventory?.inventory != null && menu.inventory.inventory.Count > 0)
            {
                float closestDist = float.MaxValue;
                int btnCenterX = btnX + btnWidth / 2;
                foreach (var comp in menu.inventory.inventory)
                {
                    if (comp != null && comp.bounds.Y > btnY)
                    {
                        float dist = Math.Abs(comp.bounds.Center.X - btnCenterX);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestSlotId = comp.myID;
                        }
                    }
                }
            }

            if (CrackAllButton == null || CrackAllButton.bounds.X != btnX || CrackAllButton.bounds.Y != btnY || CrackAllButton.bounds.Width != btnWidth || CrackAllButton.bounds.Height != btnHeight)
            {
                CrackAllButton = new ClickableComponent(new Rectangle(btnX, btnY, btnWidth, btnHeight), "CrackAll")
                {
                    myID = CrackAllButtonID,
                    leftNeighborID = GeodeMenu.region_geodeSpot, // 998
                    rightNeighborID = rightTargetID,
                    downNeighborID = closestSlotId,
                    upNeighborID = -500
                };
            }
            else
            {
                CrackAllButton.leftNeighborID = GeodeMenu.region_geodeSpot;
                CrackAllButton.rightNeighborID = rightTargetID;
                CrackAllButton.downNeighborID = closestSlotId;
                CrackAllButton.upNeighborID = -500;
            }

            // Link other components so gamepad navigation can reach CrackAllButton seamlessly
            if (menu.geodeSpot != null)
            {
                menu.geodeSpot.rightNeighborID = CrackAllButtonID;
            }

            if (menu.trashCan != null)
            {
                menu.trashCan.leftNeighborID = CrackAllButtonID;
            }

            if (menu.okButton != null && menu.trashCan == null)
            {
                menu.okButton.leftNeighborID = CrackAllButtonID;
            }

            if (menu.inventory?.inventory != null)
            {
                foreach (var comp in menu.inventory.inventory)
                {
                    if (comp != null && comp.myID < 12)
                    {
                        // Slots positioned under the CrackAllButton navigate UP to the button
                        if (comp.bounds.Center.X >= btnX - 32)
                        {
                            comp.upNeighborID = CrackAllButtonID;
                        }
                        else
                        {
                            comp.upNeighborID = GeodeMenu.region_geodeSpot;
                        }
                    }
                }
            }

            if (menu.allClickableComponents != null && !menu.allClickableComponents.Contains(CrackAllButton))
            {
                menu.allClickableComponents.Add(CrackAllButton);
            }
        }

        private static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is GeodeMenu menu)
            {
                UpdateCrackAllButton(menu);
                UpdateMenuDescription(menu);
                lastAnimationTimer = 0;
            }
            else
            {
                CrackAllButton = null;
                isHoveringCrackAll = false;
                lastAnimationTimer = 0;
            }
        }

        private static void OnWindowResized(object? sender, WindowResizedEventArgs e)
        {
            if (Game1.activeClickableMenu is GeodeMenu menu)
            {
                UpdateCrackAllButton(menu);
                if (Game1.options.SnappyMenus)
                {
                    menu.populateClickableComponentList();
                }
            }
        }

        private static void UpdateMenuDescription(GeodeMenu menu)
        {
            if (menu.alertTimer <= 0)
            {
                if (Config.FreeCracking)
                {
                    menu.descriptionText = ModEntry.I18n.Get("menu.description.free");
                }
                else if (Config.CrackingPrice != 25)
                {
                    menu.descriptionText = ModEntry.I18n.Get("menu.description.price", new { price = Config.CrackingPrice });
                }
            }
        }

        private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (Game1.activeClickableMenu is GeodeMenu menu)
            {
                UpdateMenuDescription(menu);

                // Check if vanilla single crack animation just triggered (starts around 2700ms)
                if (menu.geodeAnimationTimer > 0 && lastAnimationTimer == 0)
                {
                    if (Config.FreeCracking)
                    {
                        Game1.player.Money += 25; // Net 0g
                    }
                    else if (Config.CrackingPrice != 25)
                    {
                        int refund = 25 - Config.CrackingPrice;
                        Game1.player.Money += refund;
                    }
                }
                lastAnimationTimer = menu.geodeAnimationTimer;
            }
            else
            {
                lastAnimationTimer = 0;
            }
        }

        private static void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (!Config.ShowCrackAllButton || Game1.activeClickableMenu is not GeodeMenu menu)
                return;

            UpdateCrackAllButton(menu);
            if (CrackAllButton == null)
                return;

            int mouseX = Game1.getMouseX(true);
            int mouseY = Game1.getMouseY(true);
            isHoveringCrackAll = CrackAllButton.containsPoint(mouseX, mouseY) || (Game1.options.SnappyMenus && menu.currentlySnappedComponent == CrackAllButton);

            bool isHovered = isHoveringCrackAll;

            // Draw button background
            Color boxColor = isHovered ? Color.Wheat : Color.White;
            IClickableMenu.drawTextureBox(
                e.SpriteBatch,
                Game1.mouseCursors,
                new Rectangle(432, 439, 9, 9),
                CrackAllButton.bounds.X,
                CrackAllButton.bounds.Y,
                CrackAllButton.bounds.Width,
                CrackAllButton.bounds.Height,
                boxColor,
                4f,
                drawShadow: true
            );

            // Draw Button Label
            string label = ModEntry.I18n.Get("button.crack-all");
            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = new Vector2(
                CrackAllButton.bounds.X + (CrackAllButton.bounds.Width - textSize.X) / 2f,
                CrackAllButton.bounds.Y + (CrackAllButton.bounds.Height - textSize.Y) / 2f
            );
            Utility.drawTextWithShadow(e.SpriteBatch, label, Game1.smallFont, textPos, Game1.textColor);

            // Draw hover tooltip on top
            if (isHovered)
            {
                IClickableMenu.drawHoverText(e.SpriteBatch, ModEntry.I18n.Get("tooltip.crack-all"), Game1.smallFont);
            }
        }

        private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu is not GeodeMenu menu || menu.waitingForServerResponse)
                return;

            if (!e.Button.IsUseToolButton() && !e.Button.IsActionButton())
                return;

            UpdateCrackAllButton(menu);

            int mouseX = (int)e.Cursor.ScreenPixels.X;
            int mouseY = (int)e.Cursor.ScreenPixels.Y;

            bool isCrackAllClicked = Config.ShowCrackAllButton && CrackAllButton != null && (CrackAllButton.containsPoint(mouseX, mouseY) || (Game1.options.SnappyMenus && menu.currentlySnappedComponent == CrackAllButton));
            bool isAnvilClicked = menu.geodeSpot != null && menu.geodeSpot.containsPoint(mouseX, mouseY);
            bool isShiftDown = Helper.Input.IsDown(SButton.LeftShift) || Helper.Input.IsDown(SButton.RightShift);

            if (isCrackAllClicked)
            {
                Helper.Input.Suppress(e.Button);

                Item? targetItem = menu.heldItem;
                if (targetItem == null || !GeodeCrackerLogic.IsCrackable(targetItem))
                {
                    targetItem = null;
                    // If Crack All is clicked without holding an item, crack first geode stack found in inventory
                    foreach (var invItem in Game1.player.Items)
                    {
                        if (invItem != null && GeodeCrackerLogic.IsCrackable(invItem))
                        {
                            targetItem = invItem;
                            break;
                        }
                    }
                }

                if (targetItem != null && GeodeCrackerLogic.IsCrackable(targetItem))
                {
                    int countToCrack = Math.Min(targetItem.Stack, Config.BulkBatchSize);
                    int pricePerGeode = Config.FreeCracking ? 0 : Math.Max(0, Config.CrackingPrice);
                    if (pricePerGeode > 0 && Game1.player.Money < pricePerGeode)
                    {
                        menu.wiggleWordsTimer = 500;
                        Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
                        return;
                    }

                    var result = GeodeCrackerLogic.ProcessBatch(Game1.player, targetItem, countToCrack, Config);
                    if (result.CountCracked > 0)
                    {
                        if (targetItem == menu.heldItem && targetItem.Stack <= 0)
                        {
                            menu.heldItem = null;
                        }

                        // Sparkle animation on anvil
                        int sparkX = (menu.geodeSpot?.bounds.X ?? menu.xPositionOnScreen) + 392 - 32;
                        int sparkY = (menu.geodeSpot?.bounds.Y ?? menu.yPositionOnScreen) + 192 - 32;
                        menu.sparkle = new TemporaryAnimatedSprite(
                            "TileSheets\\animations",
                            new Rectangle(0, 640, 64, 64),
                            100f,
                            8,
                            0,
                            new Vector2(sparkX, sparkY),
                            flicker: false,
                            flipped: false
                        );
                    }
                }
                else
                {
                    menu.wiggleWordsTimer = 500;
                    Game1.playSound("cancel");
                }
            }
            else if (isAnvilClicked)
            {
                Item? targetItem = menu.heldItem;
                if (targetItem == null || !GeodeCrackerLogic.IsCrackable(targetItem))
                {
                    if (isShiftDown)
                    {
                        foreach (var invItem in Game1.player.Items)
                        {
                            if (invItem != null && GeodeCrackerLogic.IsCrackable(invItem))
                            {
                                targetItem = invItem;
                                break;
                            }
                        }
                    }
                }

                if (targetItem != null && GeodeCrackerLogic.IsCrackable(targetItem))
                {
                    if (isShiftDown)
                    {
                        // Shift+Click on Anvil -> crack entire stack instantly
                        Helper.Input.Suppress(e.Button);

                        int countToCrack = Math.Min(targetItem.Stack, Config.BulkBatchSize);
                        int pricePerGeode = Config.FreeCracking ? 0 : Math.Max(0, Config.CrackingPrice);
                        if (pricePerGeode > 0 && Game1.player.Money < pricePerGeode)
                        {
                            menu.wiggleWordsTimer = 500;
                            Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
                            return;
                        }

                        var result = GeodeCrackerLogic.ProcessBatch(Game1.player, targetItem, countToCrack, Config);
                        if (result.CountCracked > 0)
                        {
                            if (targetItem == menu.heldItem && targetItem.Stack <= 0)
                            {
                                menu.heldItem = null;
                            }

                            int sparkX = (menu.geodeSpot?.bounds.X ?? menu.xPositionOnScreen) + 392 - 32;
                            int sparkY = (menu.geodeSpot?.bounds.Y ?? menu.yPositionOnScreen) + 192 - 32;
                            menu.sparkle = new TemporaryAnimatedSprite(
                                "TileSheets\\animations",
                                new Rectangle(0, 640, 64, 64),
                                100f,
                                8,
                                0,
                                new Vector2(sparkX, sparkY),
                                flicker: false,
                                flipped: false
                            );
                        }
                    }
                    else if (Config.InstantCracking && targetItem == menu.heldItem)
                    {
                        // Single crack instant mode
                        Helper.Input.Suppress(e.Button);

                        int pricePerGeode = Config.FreeCracking ? 0 : Math.Max(0, Config.CrackingPrice);
                        if (pricePerGeode > 0 && Game1.player.Money < pricePerGeode)
                        {
                            menu.wiggleWordsTimer = 500;
                            Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
                            return;
                        }

                        var result = GeodeCrackerLogic.ProcessBatch(Game1.player, menu.heldItem, 1, Config);
                        if (result.CountCracked > 0)
                        {
                            if (menu.heldItem.Stack <= 0)
                            {
                                menu.heldItem = null;
                            }

                            int sparkX = (menu.geodeSpot?.bounds.X ?? menu.xPositionOnScreen) + 392 - 32;
                            int sparkY = (menu.geodeSpot?.bounds.Y ?? menu.yPositionOnScreen) + 192 - 32;
                            menu.sparkle = new TemporaryAnimatedSprite(
                                "TileSheets\\animations",
                                new Rectangle(0, 640, 64, 64),
                                100f,
                                8,
                                0,
                                new Vector2(sparkX, sparkY),
                                flicker: false,
                                flipped: false
                            );
                        }
                    }
                }
            }
        }
    }
}
