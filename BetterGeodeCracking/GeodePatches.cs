using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace BetterGeodeCracking
{
    public static class GeodePatches
    {
        public const int CrackAllButtonID = 99801;

        private static ModConfig Config = null!;
        private static IMonitor Monitor = null!;

        // Custom UI button for Bulk Crack
        public static ClickableComponent? CrackAllButton { get; private set; }
        private static bool isHoveringCrackAll = false;

        public static void Apply(string uniqueId, IMonitor monitor, ModConfig config)
        {
            Config = config;
            Monitor = monitor;

            var harmony = new Harmony(uniqueId);

            try
            {
                // Patch GeodeMenu constructor to hook button and snappy menu setup
                harmony.Patch(
                    original: AccessTools.Constructor(typeof(GeodeMenu)),
                    postfix: new HarmonyMethod(typeof(GeodePatches), nameof(GeodeMenu_ctor_Postfix))
                );

                // Patch IClickableMenu.populateClickableComponentList so CrackAllButton is included whenever components are refreshed
                harmony.Patch(
                    original: AccessTools.Method(typeof(IClickableMenu), nameof(IClickableMenu.populateClickableComponentList)),
                    postfix: new HarmonyMethod(typeof(GeodePatches), nameof(IClickableMenu_populateClickableComponentList_Postfix))
                );

                // Patch GeodeMenu actions
                harmony.Patch(
                    original: AccessTools.Method(typeof(GeodeMenu), nameof(GeodeMenu.receiveLeftClick)),
                    prefix: new HarmonyMethod(typeof(GeodePatches), nameof(GeodeMenu_receiveLeftClick_Prefix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(GeodeMenu), nameof(GeodeMenu.performHoverAction)),
                    postfix: new HarmonyMethod(typeof(GeodePatches), nameof(GeodeMenu_performHoverAction_Postfix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(GeodeMenu), nameof(GeodeMenu.startGeodeCrack)),
                    postfix: new HarmonyMethod(typeof(GeodePatches), nameof(GeodeMenu_startGeodeCrack_Postfix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(GeodeMenu), nameof(GeodeMenu.draw), new[] { typeof(SpriteBatch) }),
                    postfix: new HarmonyMethod(typeof(GeodePatches), nameof(GeodeMenu_draw_Postfix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(GeodeMenu), nameof(GeodeMenu.gameWindowSizeChanged)),
                    postfix: new HarmonyMethod(typeof(GeodePatches), nameof(GeodeMenu_gameWindowSizeChanged_Postfix))
                );

                // Patch Geode Crusher machine if enabled
                harmony.Patch(
                    original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performObjectDropInAction)),
                    postfix: new HarmonyMethod(typeof(GeodePatches), nameof(Object_performObjectDropInAction_Postfix))
                );

                Monitor.Log("Harmony patches for BetterGeodeCracking applied successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply BetterGeodeCracking harmony patches: {ex}", LogLevel.Error);
            }
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

        public static void GeodeMenu_ctor_Postfix(GeodeMenu __instance)
        {
            UpdateCrackAllButton(__instance);
        }

        public static void IClickableMenu_populateClickableComponentList_Postfix(IClickableMenu __instance)
        {
            if (__instance is GeodeMenu geodeMenu)
            {
                UpdateCrackAllButton(geodeMenu);
            }
        }

        public static bool GeodeMenu_receiveLeftClick_Prefix(GeodeMenu __instance, int x, int y, bool playSound)
        {
            if (__instance.waitingForServerResponse)
                return false;

            UpdateCrackAllButton(__instance);

            bool isCrackAllClicked = Config.ShowCrackAllButton && CrackAllButton != null && (CrackAllButton.containsPoint(x, y) || __instance.currentlySnappedComponent == CrackAllButton);
            bool isAnvilClicked = __instance.geodeSpot.containsPoint(x, y);

            if (isCrackAllClicked || isAnvilClicked)
            {
                Item? targetItem = __instance.heldItem;
                if (targetItem == null && isCrackAllClicked)
                {
                    // If Crack All is clicked without holding an item, crack first geode stack found in inventory
                    foreach (var invItem in Game1.player.Items)
                    {
                        if (invItem != null && Utility.IsGeode(invItem))
                        {
                            targetItem = invItem;
                            break;
                        }
                    }
                }

                if (targetItem != null && Utility.IsGeode(targetItem))
                {
                    bool isShiftDown = Game1.oldKBState.IsKeyDown(Keys.LeftShift) || Game1.oldKBState.IsKeyDown(Keys.RightShift);
                    bool isBulk = isCrackAllClicked || isShiftDown;

                    if (isBulk || Config.InstantCracking)
                    {
                        int countToCrack = isBulk ? Math.Min(targetItem.Stack, Config.BulkBatchSize) : 1;

                        int pricePerGeode = Config.FreeCracking ? 0 : Math.Max(0, Config.CrackingPrice);
                        if (pricePerGeode > 0 && Game1.player.Money < pricePerGeode)
                        {
                            __instance.wiggleWordsTimer = 500;
                            Game1.dayTimeMoneyBox.moneyShakeTimer = 1000;
                            return false;
                        }

                        // Process the batch
                        var result = GeodeCrackerLogic.ProcessBatch(Game1.player, targetItem, countToCrack, Config);
                        if (result.CountCracked > 0)
                        {
                            if (targetItem == __instance.heldItem && targetItem.Stack <= 0)
                            {
                                __instance.heldItem = null;
                            }

                            // Trigger sparkle animation on anvil
                            __instance.sparkle = new TemporaryAnimatedSprite(
                                "TileSheets\\animations",
                                new Rectangle(0, 640, 64, 64),
                                100f,
                                8,
                                0,
                                new Vector2(__instance.geodeSpot.bounds.X + 392 - 32, __instance.geodeSpot.bounds.Y + 192 - 32),
                                flicker: false,
                                flipped: false
                            );

                            return false; // Handled!
                        }
                    }
                    else if (!isBulk && !Config.InstantCracking)
                    {
                        // Single crack with Clint's animation
                        if (__instance.geodeAnimationTimer <= 0 && __instance.heldItem != null)
                        {
                            int freeSpots = Game1.player.freeSpotsInInventory();
                            if (freeSpots > 1 || (freeSpots == 1 && __instance.heldItem.Stack == 1))
                            {
                                if (__instance.heldItem.QualifiedItemId == "(O)791" && !Game1.netWorldState.Value.GoldenCoconutCracked)
                                {
                                    __instance.waitingForServerResponse = true;
                                    Game1.player.team.goldenCoconutMutex.RequestLock(delegate
                                    {
                                        __instance.waitingForServerResponse = false;
                                        __instance.geodeTreasureOverride = ItemRegistry.Create("(O)73");
                                        __instance.startGeodeCrack();
                                    }, delegate
                                    {
                                        __instance.waitingForServerResponse = false;
                                        __instance.startGeodeCrack();
                                    });
                                }
                                else
                                {
                                    __instance.startGeodeCrack();
                                }
                                return false;
                            }
                            else
                            {
                                __instance.descriptionText = Game1.content.LoadString("Strings\\UI:GeodeMenu_InventoryFull");
                                __instance.wiggleWordsTimer = 500;
                                __instance.alertTimer = 1500;
                                return false;
                            }
                        }
                    }
                }
                else if (isCrackAllClicked && targetItem == null)
                {
                    // Crack all clicked but player has no geodes
                    __instance.wiggleWordsTimer = 500;
                    Game1.playSound("cancel");
                    return false;
                }
            }

            return true;
        }

        public static void GeodeMenu_performHoverAction_Postfix(GeodeMenu __instance, int x, int y)
        {
            UpdateCrackAllButton(__instance);
            isHoveringCrackAll = Config.ShowCrackAllButton && CrackAllButton != null && (CrackAllButton.containsPoint(x, y) || __instance.currentlySnappedComponent == CrackAllButton);

            if (isHoveringCrackAll)
            {
                __instance.hoverText = ModEntry.I18n.Get("tooltip.crack-all");
                return;
            }

            if (__instance.alertTimer <= 0)
            {
                if (Config.FreeCracking)
                {
                    __instance.descriptionText = ModEntry.I18n.Get("menu.description.free");
                }
                else if (Config.CrackingPrice != 25)
                {
                    __instance.descriptionText = ModEntry.I18n.Get("menu.description.price", new { price = Config.CrackingPrice });
                }
                else
                {
                    __instance.descriptionText = ModEntry.I18n.Get("menu.description.price", new { price = 25 });
                }
            }
        }

        public static void GeodeMenu_startGeodeCrack_Postfix(GeodeMenu __instance)
        {
            // If vanilla single crack animation is run, refund the 25g difference if free or custom priced
            if (Config.FreeCracking)
            {
                Game1.player.Money += 25; // 0g net cost
            }
            else if (Config.CrackingPrice != 25)
            {
                int refund = 25 - Config.CrackingPrice;
                Game1.player.Money += refund;
            }
        }

        public static void GeodeMenu_draw_Postfix(GeodeMenu __instance, SpriteBatch b)
        {
            if (!Config.ShowCrackAllButton)
                return;

            UpdateCrackAllButton(__instance);
            if (CrackAllButton == null)
                return;

            bool isHovered = isHoveringCrackAll || (__instance.currentlySnappedComponent == CrackAllButton);

            // Draw button background
            Color boxColor = isHovered ? Color.Wheat : Color.White;
            IClickableMenu.drawTextureBox(
                b,
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
            Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, Game1.textColor);

            // Draw hover tooltip on top
            if (isHovered && !string.IsNullOrEmpty(__instance.hoverText))
            {
                IClickableMenu.drawHoverText(b, __instance.hoverText, Game1.smallFont);
            }
        }

        public static void GeodeMenu_gameWindowSizeChanged_Postfix(GeodeMenu __instance)
        {
            UpdateCrackAllButton(__instance);
            if (Game1.options.SnappyMenus)
            {
                __instance.populateClickableComponentList();
            }
        }

        public static void Object_performObjectDropInAction_Postfix(StardewValley.Object __instance, Item dropInItem, bool probe, Farmer who, bool __result)
        {
            if (!probe && __result && (__instance.QualifiedItemId == "(BC)182" || __instance.ItemId == "182")) // Geode Crusher
            {
                if (Config.InstantGeodeCrusher)
                {
                    __instance.MinutesUntilReady = 0;
                    __instance.readyForHarvest.Value = true;
                    __instance.showNextIndex.Value = true;
                }
            }
        }
    }
}
