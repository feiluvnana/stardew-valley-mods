using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

// GeodeMenuHandler customizes Clint's GeodeMenu (the blacksmith geode-cracking screen):
// it injects a "Crack All" button, keeps it positioned and gamepad-navigable, repaints
// it every frame, and converts clicks / Shift+clicks on the anvil into instant bulk
// cracking handled by GeodeCrackerLogic.
namespace BetterQOL
{
    /// <summary>
    /// Lifecycle owner of the Crack All button inside GeodeMenu instances: creation,
    /// layout, snap-navigation wiring, per-frame rendering, and click routing.
    /// </summary>
    public static class GeodeMenuHandler
    {
        /// <summary>Custom component id chosen far above vanilla ids so nothing collides.</summary>
        public const int CrackAllButtonID = 99801;

        // "null!" = "assigned later, trust me": Initialize() runs before any event can fire.
        private static IModHelper Helper = null!;
        private static IMonitor Monitor = null!;
        // Expression-bodied property aliasing the shared live config for brevity.
        private static ModConfig Config => ModEntry.Config;

        /// <summary>The injected button, or null whenever the geode menu is closed/disabled.</summary>
        public static ClickableComponent? CrackAllButton { get; private set; }
        private static bool isHoveringCrackAll = false;

        /// <summary>Caches SMAPI services and subscribes the five events this handler needs.</summary>
        public static void Initialize(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            helper.Events.Display.MenuChanged += OnMenuChanged;               // menu opened/closed -> attach or forget our button
            helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu; // redraw button every frame atop the open menu
            helper.Events.Display.WindowResized += OnWindowResized;           // re-layout after resolution changes
            helper.Events.Input.ButtonPressed += OnButtonPressed;             // intercept clicks for instant cracking
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;            // ~60/sec tick to reassert description text
        }

        /// <summary>
        /// Idempotently syncs the Crack All button with current config and menu geometry:
        /// removes it when disabled, otherwise (re)creates it at the right spot and rewires
        /// neighbor ids so keyboard/controller navigation flows naturally around it.
        /// </summary>
        private static void UpdateCrackAllButton(GeodeMenu menu)
        {
            if (!Config.ShowCrackAllButton)
            {
                if (CrackAllButton != null && menu.allClickableComponents != null)
                {
                    menu.allClickableComponents.Remove(CrackAllButton);
                }
                CrackAllButton = null;

                // Restore the vanilla neighbor chain now that our button is gone.
                // Nested ?: ternaries pick trash can, else OK button, else -99998 ("none").
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
                    // "menu.inventory?.inventory" uses ?. so a missing toolbar can't crash;
                    // inventory slot ids 0-11 are the top row of the player's backpack grid.
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

            // Size the button to fit its translated label (140px minimum), anchored near the anvil.
            string label = ModEntry.I18n.Get("button.crack-all");
            int textWidth = (int)Game1.smallFont.MeasureString(label).X;
            int btnWidth = Math.Max(140, textWidth + 32);
            int btnHeight = 44;
            int btnX = menu.geodeSpot.bounds.Right - btnWidth - 16;
            int btnY = menu.geodeSpot.bounds.Y + 16;

            int rightTargetID = (menu.trashCan != null) ? menu.trashCan.myID : (menu.okButton != null ? menu.okButton.myID : -99998);

            // Find closest inventory slot under button for gamepad Down navigation
            // Classic "nearest match" scan: remember the best candidate seen so far.
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

            // Rebuild the component only when its geometry changed; otherwise just
            // refresh the neighbor ids in place (cheaper, avoids GC churn each frame).
            if (CrackAllButton == null || CrackAllButton.bounds.X != btnX || CrackAllButton.bounds.Y != btnY || CrackAllButton.bounds.Width != btnWidth || CrackAllButton.bounds.Height != btnHeight)
            {
                // Object-initializer syntax "{ ... }" sets public fields right after construction.
                // The *NeighborID fields form the menu's snap grid used for gamepad focus.
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

            // Register with the menu's master component list so it participates in
            // snapping and click hit-testing like any vanilla button.
            if (menu.allClickableComponents != null && !menu.allClickableComponents.Contains(CrackAllButton))
            {
                menu.allClickableComponents.Add(CrackAllButton);
            }
        }

        /// <summary>
        /// SMAPI event: a different menu just opened (or closed). Build our button when a
        /// GeodeMenu appears; forget it otherwise.
        /// </summary>
        private static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            // Declaration pattern: "is GeodeMenu menu" type-checks AND gives a typed variable.
            if (e.NewMenu is GeodeMenu menu)
            {
                UpdateCrackAllButton(menu);
                UpdateMenuDescription(menu);
            }
            else
            {
                CrackAllButton = null;
                isHoveringCrackAll = false;
            }
        }

        /// <summary>Repositions the button after a window/resolution change.</summary>
        private static void OnWindowResized(object? sender, WindowResizedEventArgs e)
        {
            if (Game1.activeClickableMenu is GeodeMenu menu)
            {
                UpdateCrackAllButton(menu);
                if (Game1.options.SnappyMenus)
                {
                    // Rebuild the snap list so controller focus uses the fresh bounds.
                    menu.populateClickableComponentList();
                }
            }
        }

        /// <summary>
        /// Rewrites the menu's flavor text to mention bulk cracking - but only once any
        /// transient alert ("you need a geode!") has expired.
        /// </summary>
        private static void UpdateMenuDescription(GeodeMenu menu)
        {
            if (menu.alertTimer <= 0)
            {
                menu.descriptionText = ModEntry.I18n.Get("menu.description.standard");
            }
        }

        /// <summary>Every game tick (~60/sec), reassert our description if vanilla overwrote it.</summary>
        private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (Game1.activeClickableMenu is GeodeMenu menu)
            {
                UpdateMenuDescription(menu);
            }
        }

        /// <summary>
        /// Paints the Crack All button (background + centered label) after the menu itself
        /// each frame, plus the stock hover tooltip when moused over or gamepad-snapped.
        /// </summary>
        private static void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            // Negated pattern "is not GeodeMenu menu": exit early for every other menu.
            if (!Config.ShowCrackAllButton || Game1.activeClickableMenu is not GeodeMenu menu)
                return;

            UpdateCrackAllButton(menu);
            if (CrackAllButton == null)
                return;

            // getMouseX/Y(true) returns UI-scaled coordinates matching component bounds.
            int mouseX = Game1.getMouseX(true);
            int mouseY = Game1.getMouseY(true);
            // Controller users "hover" via the currently snapped component, not the mouse.
            isHoveringCrackAll = CrackAllButton.containsPoint(mouseX, mouseY) || (Game1.options.SnappyMenus && menu.currentlySnappedComponent == CrackAllButton);

            bool isHovered = isHoveringCrackAll;

            // Draw button background
            // Ternary brightens the box on hover; drawTextureBox stretches a 9x9 cell of
            // cursors.png at 4x scale into the button rect (vanilla-style beveled button).
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
            // Center the text by subtracting half of its measured pixel size.
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

        /// <summary>
        /// Cancels any in-flight single-geode smash animation so bulk results appear instantly.
        /// </summary>
        private static void ResetGeodeAnimation(GeodeMenu menu)
        {
            menu.geodeAnimationTimer = 0;
            menu.geodeDestructionAnimation = null;
            menu.geodeTreasure = null;
        }

        private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu is not GeodeMenu menu || menu.waitingForServerResponse)
                return;

            if (!e.Button.IsUseToolButton() && !e.Button.IsActionButton())
                return;

            UpdateCrackAllButton(menu);

            int mouseX = Game1.getMouseX(true);
            int mouseY = Game1.getMouseY(true);

            bool isCrackAllClicked = Config.ShowCrackAllButton && CrackAllButton != null && (CrackAllButton.containsPoint(mouseX, mouseY) || (Game1.options.SnappyMenus && menu.currentlySnappedComponent == CrackAllButton));
            bool isAnvilClicked = menu.geodeSpot != null && menu.geodeSpot.containsPoint(mouseX, mouseY);
            // IsDown polls live held state (as opposed to discrete press events).
            bool isShiftDown = Helper.Input.IsDown(SButton.LeftShift) || Helper.Input.IsDown(SButton.RightShift);

            if (isCrackAllClicked)
            {
                // Suppress: consume this click so the underlying game/menu doesn't react too.
                Helper.Input.Suppress(e.Button);

                // Priority: 1) Held item on cursor, 2) Item on anvil, 3) First crackable geode stack found in inventory
                // "Item?" (nullable) expresses "no candidate found yet" as plain null.
                Item? targetItem = null;
                bool isHeld = false;
                bool isAnvil = false;

                if (menu.heldItem != null && GeodeCrackerLogic.IsCrackable(menu.heldItem))
                {
                    targetItem = menu.heldItem;
                    isHeld = true;
                }
                else if (menu.geodeSpot?.item != null && GeodeCrackerLogic.IsCrackable(menu.geodeSpot.item))
                {
                    targetItem = menu.geodeSpot.item;
                    isAnvil = true;
                }
                else
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

                if (targetItem != null && GeodeCrackerLogic.IsCrackable(targetItem))
                {
                    // Broke? Mirror vanilla feedback: wobble the description + shake the coin display.
                    if (Game1.player.Money < GeodeCrackerLogic.CrackingPrice)
                    {
                        menu.wiggleWordsTimer = 500;
                        Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
                        return;
                    }

                    // Cap the batch by both what's in the stack and the configured maximum.
                    int countToCrack = Math.Min(targetItem.Stack, Config.BulkBatchSize);
                    var result = GeodeCrackerLogic.ProcessBatch(Game1.player, targetItem, countToCrack, Config);
                    if (result.CountCracked > 0)
                    {
                        if (isHeld && targetItem.Stack <= 0)
                        {
                            menu.heldItem = null;
                        }
                        else if (isAnvil && targetItem.Stack <= 0 && menu.geodeSpot != null)
                        {
                            menu.geodeSpot.item = null;
                        }

                        // Reset any pending single-geode animations
                        ResetGeodeAnimation(menu);

                        // Sparkle animation on anvil
                        // 8-frame 64px burst from the shared animations sheet (100ms per frame),
                        // centered on the anvil artwork (+392,+192 from the menu origin).
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
                Item? targetItem = null;
                bool isHeld = false;
                bool isAnvil = false;

                if (menu.heldItem != null && GeodeCrackerLogic.IsCrackable(menu.heldItem))
                {
                    targetItem = menu.heldItem;
                    isHeld = true;
                }
                else if (menu.geodeSpot?.item != null && GeodeCrackerLogic.IsCrackable(menu.geodeSpot.item))
                {
                    targetItem = menu.geodeSpot.item;
                    isAnvil = true;
                }
                else if (isShiftDown)
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

                if (targetItem != null && GeodeCrackerLogic.IsCrackable(targetItem))
                {
                    if (isShiftDown)
                    {
                        // Shift+Click on Anvil -> crack entire stack instantly
                        Helper.Input.Suppress(e.Button);

                        // Same broke-guard as the Crack All button path.
                        if (Game1.player.Money < GeodeCrackerLogic.CrackingPrice)
                        {
                            menu.wiggleWordsTimer = 500;
                            Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
                            return;
                        }

                        // Cap by stack size and configured batch maximum, same as above.
                        int countToCrack = Math.Min(targetItem.Stack, Config.BulkBatchSize);
                        var result = GeodeCrackerLogic.ProcessBatch(Game1.player, targetItem, countToCrack, Config);
                        if (result.CountCracked > 0)
                        {
                            if (isHeld && targetItem.Stack <= 0)
                            {
                                menu.heldItem = null;
                            }
                            else if (isAnvil && targetItem.Stack <= 0 && menu.geodeSpot != null)
                            {
                                menu.geodeSpot.item = null;
                            }

                            // Reset single geode animation
                            ResetGeodeAnimation(menu);

                            // Sparkle burst centered on the anvil artwork.
                            int sparkX = (menu.geodeSpot?.bounds.X ?? menu.xPositionOnScreen) + 392 - 32;
                            int sparkY = (menu.geodeSpot?.bounds.Y ?? menu.yPositionOnScreen) + 192 - 32;
                            // 8-frame 64px animation from the shared animations sheet (100ms/frame).
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
                    else if (Config.InstantCracking && (isHeld || isAnvil))
                    {
                        // Single crack instant mode
                        // Only bypass the vanilla animation when a geode was held or on the anvil
                        // (plain Shift+Click from inventory is handled by the branch above).
                        Helper.Input.Suppress(e.Button);

                        // Same broke-guard as the Crack All button path.
                        if (Game1.player.Money < GeodeCrackerLogic.CrackingPrice)
                        {
                            menu.wiggleWordsTimer = 500;
                            Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
                            return;
                        }

                        var result = GeodeCrackerLogic.ProcessBatch(Game1.player, targetItem, 1, Config);
                        if (result.CountCracked > 0)
                        {
                            if (isHeld && targetItem.Stack <= 0)
                            {
                                menu.heldItem = null;
                            }
                            else if (isAnvil && targetItem.Stack <= 0 && menu.geodeSpot != null)
                            {
                                menu.geodeSpot.item = null;
                            }

                            // Reset single geode animation
                            ResetGeodeAnimation(menu);

                            // Sparkle burst centered on the anvil artwork.
                            int sparkX = (menu.geodeSpot?.bounds.X ?? menu.xPositionOnScreen) + 392 - 32;
                            int sparkY = (menu.geodeSpot?.bounds.Y ?? menu.yPositionOnScreen) + 192 - 32;
                            // Same 8-frame sparkle animation as the bulk paths above.
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
