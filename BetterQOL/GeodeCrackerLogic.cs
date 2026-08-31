using Microsoft.Xna.Framework;
using StardewValley;

namespace BetterQOL
{
    /// <summary>
    /// A tiny data container describing the outcome of one bulk-cracking run. Wrapping
    /// several related values in a class like this is the idiomatic C# way to return
    /// multiple results from a single method call (instead of awkward "out" parameters).
    /// </summary>
    public class GeodeBatchResult
    {
        /// <summary>
        /// How many geodes were actually cracked (may be fewer than requested if the
        /// stack ran dry or the player ran out of money). Auto-properties ("get; set;")
        /// make C# generate the hidden backing variable for us.
        /// </summary>
        public int CountCracked { get; set; }
        /// <summary>
        /// Every treasure rolled during the batch, already merged into tidy stacks.
        /// "List&lt;Item&gt;" is a GENERIC collection: a growable array holding Item objects.
        /// The "= new()" initializer creates an empty list immediately so this is never null.
        /// </summary>
        public List<Item> Treasures { get; set; } = new();
    }

    /// <summary>
    /// Pure game logic for cracking geodes in bulk. "static" means this class can never
    /// be instantiated with "new" - it simply bundles related functions that operate on
    /// the data passed in. Keeping LOGIC here separate from UI (GeodeMenuHandler) makes
    /// each side easier to read, test, and reuse.
    /// </summary>
    public static class GeodeCrackerLogic
    {
        /// <summary>
        /// Clint's fee: 25 gold per geode. "const" marks a compile-time constant whose
        /// value is baked into the program and can never change while the game runs.
        /// </summary>
        public const int CrackingPrice = 25;

        /// <summary>
        /// Determines if an item is a crackable geode, mystery box, artifact trove, or golden coconut.
        /// </summary>
        public static bool IsCrackable(Item? item)
        {
            // No item at all -> nothing to crack. Early "guard clauses" like this keep
            // the rest of the method free of nested null checks.
            if (item == null) return false;
            // QualifiedItemId is the namespaced id form, e.g. "(O)749" where "(O)" means
            // "object" category. Qualified ids are unambiguous across all item types.
            string qid = item.QualifiedItemId;
            // Mystery boxes exist in several flavors (regular, golden, from machines),
            // so match ANY id containing "MysteryBox" rather than listing each one.
            if (qid == "(O)MysteryBox" || qid == "(O)GoldenMysteryBox" || qid.Contains("MysteryBox"))
                return true;
            // "(O)791" = Golden Coconut; "(O)275" = Artifact Trove.
            if (qid == "(O)791" || qid == "(O)275")
                return true;
            // For everything else defer to the game's own geode test. Named-argument
            // syntax ("disallow_special_geodes:") documents what "false" means here:
            // special geodes ARE allowed to count as crackable.
            return Utility.IsGeode(item, disallow_special_geodes: false);
        }

        /// <summary>
        /// Cracks a batch of geodes from a stack, generating treasures faithfully according to vanilla 1.6 mechanics.
        /// </summary>
        public static GeodeBatchResult ProcessBatch(Farmer who, Item geodeStack, int requestedCount, ModConfig config)
        {
            var result = new GeodeBatchResult();
            // Guard clause: reject invalid input up front (null stack, non-crackable item,
            // or nonsense count). The empty result object is still returned so callers
            // never have to null-check.
            if (geodeStack == null || !IsCrackable(geodeStack) || requestedCount <= 0)
                return result;

            int available = geodeStack.Stack;
            // Never crack more than the stack actually holds...
            int countToCrack = Math.Min(available, requestedCount);

            // ...nor more than the player can afford at 25g apiece. Integer division
            // truncates, so 100 gold buys exactly 4 cracks.
            int affordable = who.Money / CrackingPrice;
            countToCrack = Math.Min(countToCrack, affordable);

            if (countToCrack <= 0)
                return result;

            var rawTreasures = new List<Item>();
            // Classify the stack ONCE before looping so each iteration can branch on
            // simple booleans instead of re-parsing item ids.
            string qualifiedItemId = geodeStack.QualifiedItemId;
            bool isMysteryBox = qualifiedItemId == "(O)MysteryBox" || qualifiedItemId == "(O)GoldenMysteryBox" || qualifiedItemId.Contains("MysteryBox");
            bool isGoldenCoconut = qualifiedItemId == "(O)791";
            bool isArtifactTrove = qualifiedItemId == "(O)275";

            // Roll one treasure per geode, mimicking vanilla's exact order of operations.
            for (int i = 0; i < countToCrack; i++)
            {
                // "Item?" (nullable reference type) honestly says a crack might yield null.
                Item? treasure = null;

                // Special case: the FIRST golden coconut ever cracked always yields the
                // Golden Walnut ("(O)73") and permanently flips a synced world flag so
                // it can never happen again. netWorldState replicates across multiplayer.
                if (isGoldenCoconut && !Game1.netWorldState.Value.GoldenCoconutCracked)
                {
                    Game1.netWorldState.Value.GoldenCoconutCracked = true;
                    treasure = ItemRegistry.Create("(O)73");
                    Game1.stats.GeodesCracked++;
                }
                else
                {
                    // Ask the game's own loot roller - identical odds to handing geodes
                    // to Clint one at a time.
                    treasure = Utility.getTreasureFromGeode(geodeStack);

                    // Vanilla tracks separate lifetime counters for mystery boxes vs geodes.
                    if (isMysteryBox)
                    {
                        Game1.stats.Increment("MysteryBoxesOpened");
                    }
                    else
                    {
                        Game1.stats.GeodesCracked++;
                    }

                    // Vanilla quirk: the very FIRST artifact-typed drop is hijacked into
                    // 5 stone unless the "artifactFound" mail flag was already granted.
                    // Property patterns like "{ Type: \"Arch\" }" type-check AND extract
                    // a field in one expression (no separate "is" + cast needed).
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

            // Capture the display name NOW, before the stack shrinks or vanishes below.
            string geodeDisplayName = geodeStack.DisplayName;

            // Consolidate identical items into stacks
            var consolidatedTreasures = ConsolidateTreasures(rawTreasures);

            // Deduct geode stack first so inventory slot is freed up if fully consumed
            geodeStack.Stack -= countToCrack;
            // Stack hit zero: remove the now-empty entry from the inventory grid entirely.
            if (geodeStack.Stack <= 0)
            {
                who.removeItemFromInventory(geodeStack);
            }

            // Add consolidated treasures into player inventory or drop safely as debris
            foreach (var item in consolidatedTreasures)
            {
                // addItemToInventory returns whatever DIDN'T fit (null when fully stored).
                Item? leftover = who.addItemToInventory(item);
                if (leftover != null && leftover.Stack > 0)
                {
                    // Inventory full: spill the remainder onto the ground as a bouncing
                    // pickup at the farmer's feet (-1 means "no particular direction").
                    Game1.createItemDebris(leftover, new Vector2(who.StandingPixel.X, who.StandingPixel.Y), -1, who.currentLocation);
                }
            }

            // Deduct standard cracking fee (25g per geode)
            int totalCost = CrackingPrice * countToCrack;
            if (totalCost > 0)
            {
                // Math.Max clamps at 0 so money can never dip below zero.
                who.Money = Math.Max(0, who.Money - totalCost);
            }

            // Play audio cues
            PlayCrackingSoundEffects(isMysteryBox || isArtifactTrove);

            result.CountCracked = countToCrack;
            result.Treasures = consolidatedTreasures;

            // Optional HUD notification for bulk actions
            // Announce only multi-cracks - single cracks don't need the extra noise.
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
            // The new, tidier list we'll build up and return.
            var consolidated = new List<Item>();
            foreach (var item in items)
            {
                // Defensive skip: nothing to do with a null entry.
                if (item == null) continue;

                // Stackables (stone, seeds, most resources) merge; unique gear does not.
                if (item.maximumStackSize() > 1)
                {
                    // Hunt for an earlier pile of the same item type with spare capacity.
                    foreach (var existing in consolidated)
                    {
                        if (existing.canStackWith(item))
                        {
                            int maxStack = existing.maximumStackSize();
                            int space = maxStack - existing.Stack;
                            if (space >= item.Stack)
                            {
                                // Entire incoming pile fits: absorb it fully and stop searching.
                                existing.Stack += item.Stack;
                                item.Stack = 0;
                                break;
                            }
                            else if (space > 0)
                            {
                                // Partial fit: top off this pile to its cap. Whatever remains
                                // on "item" keeps looping to try filling yet another pile.
                                existing.Stack = maxStack;
                                item.Stack -= space;
                            }
                        }
                    }
                }

                // Anything that survived merging (non-stackables plus leftovers) earns
                // its own slot in the result list.
                if (item.Stack > 0)
                {
                    consolidated.Add(item);
                }
            }
            return consolidated;
        }

        /// <summary>
        /// Replays the exact sound sequence vanilla uses when Clint smashes a geode, so
        /// instant/bulk cracking still feels authentic. "private" hides this helper from
        /// all other classes - it's an internal implementation detail.
        /// </summary>
        private static void PlayCrackingSoundEffects(bool woodWhack)
        {
            // Base impact thud shared by every crack.
            Game1.playSound("hammer");
            // Wooden crates get a wood knock; stones get the rock-splitting crunch.
            if (woodWhack)
            {
                Game1.playSound("woodWhack");
            }
            else
            {
                Game1.playSound("stoneCrack");
            }
            // The signature chime that plays as the mineral reveal pops out.
            Game1.playSound("discoverMineral");
        }
    }
}
