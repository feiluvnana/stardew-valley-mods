using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace BetterQOL
{
    public class GeodeBatchResult
    {
        public int CountCracked { get; set; }
        public List<Item> Treasures { get; set; } = new();
    }

    public static class GeodeCrackerLogic
    {
        public const int CrackingPrice = 25;

        /// <summary>
        /// Determines if an item is a crackable geode, mystery box, artifact trove, or golden coconut.
        /// </summary>
        public static bool IsCrackable(Item? item)
        {
            if (item == null) return false;
            string qid = item.QualifiedItemId;
            if (qid == "(O)MysteryBox" || qid == "(O)GoldenMysteryBox" || qid.Contains("MysteryBox"))
                return true;
            if (qid == "(O)791" || qid == "(O)275")
                return true;
            return Utility.IsGeode(item, disallow_special_geodes: false);
        }

        /// <summary>
        /// Cracks a batch of geodes from a stack, generating treasures faithfully according to vanilla 1.6 mechanics.
        /// </summary>
        public static GeodeBatchResult ProcessBatch(Farmer who, Item geodeStack, int requestedCount, ModConfig config)
        {
            var result = new GeodeBatchResult();
            if (geodeStack == null || !IsCrackable(geodeStack) || requestedCount <= 0)
                return result;

            int available = geodeStack.Stack;
            int countToCrack = Math.Min(available, requestedCount);

            int affordable = who.Money / CrackingPrice;
            countToCrack = Math.Min(countToCrack, affordable);

            if (countToCrack <= 0)
                return result;

            var rawTreasures = new List<Item>();
            string qualifiedItemId = geodeStack.QualifiedItemId;
            bool isMysteryBox = qualifiedItemId == "(O)MysteryBox" || qualifiedItemId == "(O)GoldenMysteryBox" || qualifiedItemId.Contains("MysteryBox");
            bool isGoldenCoconut = qualifiedItemId == "(O)791";
            bool isArtifactTrove = qualifiedItemId == "(O)275";

            for (int i = 0; i < countToCrack; i++)
            {
                Item? treasure = null;

                if (isGoldenCoconut && !Game1.netWorldState.Value.GoldenCoconutCracked)
                {
                    Game1.netWorldState.Value.GoldenCoconutCracked = true;
                    treasure = ItemRegistry.Create("(O)73");
                }
                else
                {
                    treasure = Utility.getTreasureFromGeode(geodeStack);

                    if (isMysteryBox)
                    {
                        Game1.stats.Increment("MysteryBoxesOpened");
                    }
                    else
                    {
                        Game1.stats.GeodesCracked++;
                    }

                    if (!isArtifactTrove && !(treasure is StardewValley.Object { Type: "Minerals" }) && treasure is StardewValley.Object { Type: "Arch" } && !who.hasOrWillReceiveMail("artifactFound"))
                    {
                        treasure = ItemRegistry.Create("(O)390", 5);
                        who.mailReceived.Add("artifactFound");
                    }
                }

                if (treasure != null)
                {
                    rawTreasures.Add(treasure);
                }
            }

            string geodeDisplayName = geodeStack.DisplayName;

            // Consolidate identical items into stacks
            var consolidatedTreasures = ConsolidateTreasures(rawTreasures);

            // Deduct geode stack first so inventory slot is freed up if fully consumed
            geodeStack.Stack -= countToCrack;
            if (geodeStack.Stack <= 0)
            {
                who.removeItemFromInventory(geodeStack);
            }

            // Add consolidated treasures into player inventory or drop safely as debris
            foreach (var item in consolidatedTreasures)
            {
                Item? leftover = who.addItemToInventory(item);
                if (leftover != null && leftover.Stack > 0)
                {
                    Game1.createItemDebris(leftover, new Vector2(who.StandingPixel.X, who.StandingPixel.Y), -1, who.currentLocation);
                }
            }

            // Deduct standard cracking fee (25g per geode)
            int totalCost = CrackingPrice * countToCrack;
            if (totalCost > 0)
            {
                who.Money = Math.Max(0, who.Money - totalCost);
            }

            // Play audio cues
            PlayCrackingSoundEffects(isMysteryBox || isArtifactTrove);

            result.CountCracked = countToCrack;
            result.Treasures = consolidatedTreasures;

            // Optional HUD notification for bulk actions
            if (config.ShowSummaryToast && countToCrack > 1)
            {
                Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("toast.cracked-summary", new { count = countToCrack, name = geodeDisplayName })));
            }

            return result;
        }

        /// <summary>
        /// Consolidates duplicate items into single stacks where possible without exceeding max stack size.
        /// </summary>
        public static List<Item> ConsolidateTreasures(List<Item> items)
        {
            var consolidated = new List<Item>();
            foreach (var item in items)
            {
                if (item == null) continue;

                if (item.maximumStackSize() > 1)
                {
                    foreach (var existing in consolidated)
                    {
                        if (existing.canStackWith(item))
                        {
                            int maxStack = existing.maximumStackSize();
                            int space = maxStack - existing.Stack;
                            if (space >= item.Stack)
                            {
                                existing.Stack += item.Stack;
                                item.Stack = 0;
                                break;
                            }
                            else if (space > 0)
                            {
                                existing.Stack = maxStack;
                                item.Stack -= space;
                            }
                        }
                    }
                }

                if (item.Stack > 0)
                {
                    consolidated.Add(item);
                }
            }
            return consolidated;
        }

        private static void PlayCrackingSoundEffects(bool woodWhack)
        {
            Game1.playSound("hammer");
            if (woodWhack)
            {
                Game1.playSound("woodWhack");
            }
            else
            {
                Game1.playSound("stoneCrack");
            }
            Game1.playSound("discoverMineral");
        }
    }
}
