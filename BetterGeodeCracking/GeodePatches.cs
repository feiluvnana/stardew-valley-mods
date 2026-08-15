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
                // Patch GeodeMenu
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
            int btnWidth = 140;
            int btnHeight = 44;
            int btnX = menu.geodeSpot.bounds.Right - btnWidth - 16;
            int btnY = menu.geodeSpot.bounds.Y + 16;

            if (CrackAllButton == null || CrackAllButton.bounds.X != btnX || CrackAllButton.bounds.Y != btnY)
            {
                CrackAllButton = new ClickableComponent(new Rectangle(btnX, btnY, btnWidth, btnHeight), "CrackAll")
                {
                    myID = 99801,
                    upNeighborID = -99998,
                    downNeighborID = 0,
                    leftNeighborID = 998
                };
            }

            if (menu.allClickableComponents != null && !menu.allClickableComponents.Contains(CrackAllButton))
            {
                menu.allClickableComponents.Add(CrackAllButton);
            }
        }

        public static bool GeodeMenu_receiveLeftClick_Prefix(GeodeMenu __instance, int x, int y, bool playSound)
        {
            if (__instance.waitingForServerResponse)
                return false;

            UpdateCrackAllButton(__instance);

            bool isCrackAllClicked = Config.ShowCrackAllButton && CrackAllButton != null && CrackAllButton.containsPoint(x, y);
            bool isAnvilClicked = __instance.geodeSpot.containsPoint(x, y);

            if (isCrackAllClicked || isAnvilClicked)
            {
                Item? targetItem = __instance.heldItem;
                bool isInventoryItem = false;

                if (targetItem == null && isCrackAllClicked)
                {
                    // If Crack All is clicked without holding an item, crack first geode stack found in inventory
                    foreach (var invItem in Game1.player.Items)
                    {
                        if (invItem != null && Utility.IsGeode(invItem))
                        {
                            targetItem = invItem;
                            isInventoryItem = true;
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
                            targetItem.Stack -= result.CountCracked;
                            if (targetItem.Stack <= 0)
                            {
                                if (isInventoryItem)
                                {
                                    Game1.player.removeItemFromInventory(targetItem);
                                }
                                else
                                {
                                    __instance.heldItem = null;
                                }
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
            }

            return true;
        }

        public static void GeodeMenu_performHoverAction_Postfix(GeodeMenu __instance, int x, int y)
        {
            UpdateCrackAllButton(__instance);
            isHoveringCrackAll = Config.ShowCrackAllButton && CrackAllButton != null && CrackAllButton.containsPoint(x, y);

            if (isHoveringCrackAll)
            {
                __instance.hoverText = "Crack Entire Stack (Shift+Click)";
                return;
            }

            if (__instance.alertTimer <= 0)
            {
                if (Config.FreeCracking)
                {
                    __instance.descriptionText = "Clint can break these open for you for free.\n(Hold Shift or click 'Crack All' to open stack)";
                }
                else if (Config.CrackingPrice != 25)
                {
                    __instance.descriptionText = $"Clint can break these open for you for {Config.CrackingPrice}g.\n(Hold Shift or click 'Crack All' to open stack)";
                }
                else
                {
                    __instance.descriptionText = "Clint can break these open for you for 25g.\n(Hold Shift or click 'Crack All' to open stack)";
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

            // Draw button background
            Color boxColor = isHoveringCrackAll ? Color.Wheat : Color.White;
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
            string label = "Crack All";
            Vector2 textSize = Game1.smallFont.MeasureString(label);
            Vector2 textPos = new Vector2(
                CrackAllButton.bounds.X + (CrackAllButton.bounds.Width - textSize.X) / 2f,
                CrackAllButton.bounds.Y + (CrackAllButton.bounds.Height - textSize.Y) / 2f
            );
            Utility.drawTextWithShadow(b, label, Game1.smallFont, textPos, Game1.textColor);
        }

        public static void GeodeMenu_gameWindowSizeChanged_Postfix(GeodeMenu __instance)
        {
            UpdateCrackAllButton(__instance);
        }

        public static void Object_performObjectDropInAction_Postfix(StardewValley.Object __instance, Item dropInItem, bool probe, Farmer who, bool __result)
        {
            if (!probe && __result && __instance.QualifiedItemId == "(BC)182") // Geode Crusher
            {
                if (Config.InstantGeodeCrusher)
                {
                    __instance.MinutesUntilReady = 0;
                    __instance.readyForHarvest.Value = true;
                }
            }
        }
    }
}
