// =====================================================================================
// ArtisanBalancer.cs - rebalances ARTISAN GOODS: the valuable products machines turn raw
// ingredients into. Examples: Keg -> wine/mead/juice, Preserves Jar -> pickles & jelly,
// Cheese Press -> cheese, Mayonnaise Machine -> mayonnaise, Loom -> cloth,
// Oil Maker -> oils, Dehydrator & Fish Smoker (Stardew 1.6 machines), and the cellar
// Cask used for aging.
//
// Rather than patching game CODE, this file rewrites the game-DATA asset "Data/Machines",
// a giant dictionary describing each machine's input triggers and outputs. Data edits
// made through SMAPI's AssetRequested event survive game updates far better than code
// patches do, because the data format is officially moddable.
// =====================================================================================
using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData;
using StardewValley.GameData.Machines;

// Namespace grouping every BetterIndustry class under one shared prefix.
namespace BetterIndustry
{
    /// <summary>
    /// Applies BetterIndustry's machine tweaks to the "Data/Machines" asset: quality
    /// preservation for all artisan machines, flower-mead 2.0x pricing, vegetable juice
    /// buff, truffle-oil price/quality scaling, and expanded cask aging.
    /// </summary>
    // "static class" = can never be instantiated with "new". It's merely a container of
    // functions; all state arrives as parameters or via ModEntry's shared statics.
    public static class ArtisanBalancer
    {
        // Expression-bodied read-only properties ("=> expression") forwarding to ModEntry's
        // shared config/logger so every method below can use them conveniently.
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// SMAPI event handler fired while ANY game asset loads. Filters for
        /// "Data/Machines" (the per-machine behaviour definitions) and queues our edits.
        /// Subscribed once from ModEntry.Entry().
        /// </summary>
        /// <param name="sender">Event source supplied by SMAPI (unused).</param>
        /// <param name="e">Names the loading asset and provides editing helpers.</param>
        public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // NameWithoutLocale drops language suffixes so "Data/Machines" matches for
            // every translation; IsEquivalentTo compares asset ids robustly (case- and
            // format-safe).
            if (!e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
                return;

            // Queue an editor for this asset. "asset => { ... }" is a LAMBDA - a function
            // passed around as a value, executed by SMAPI during the load. Priority Late
            // makes us run after most other mods so our overrides usually win conflicts.
            e.Edit(asset =>
            {
                try
                {
                    // View the asset as a dictionary: machine-id key -> MachineData record
                    // we may mutate freely; whatever we leave behind is what gets cached.
                    // ".Data" unwraps the actual IDictionary<string, MachineData>.
                    var data = asset.AsDictionary<string, MachineData>().Data;

                    // One small, focused method per machine keeps each tweak auditable.
                    ApplyKegEdits(data);
                    ApplyPreservesJarEdits(data);
                    ApplyCheesePressEdits(data);
                    ApplyMayonnaiseMachineEdits(data);
                    ApplyLoomEdits(data);
                    ApplyOilMakerEdits(data);
                    ApplyDehydratorEdits(data);
                    ApplyFishSmokerEdits(data);
                    ApplyCaskEdits(data);
                }
                catch (Exception ex)
                {
                    // A malformed edit must never crash the whole asset load: log the full
                    // exception (message + stack trace) and keep the unedited remainder.
                    Monitor.Log($"Error applying machine balance in ArtisanBalancer: {ex}", LogLevel.Error);
                }
            }, AssetEditPriority.Late);
        }

        /// <summary>
        /// Finds a machine's data by trying several alternative IDs. The game has referred
        /// to machines differently across versions: qualified id "(BC)12" (BC = "big
        /// craftable"), legacy number "12", or friendly name "Keg".
        /// </summary>
        /// <param name="data">The whole machine dictionary from the asset.</param>
        /// <param name="keys">Any number of candidate IDs, tried in order.</param>
        /// <returns>The first matching MachineData, or null if none matched.</returns>
        private static MachineData? GetMachine(IDictionary<string, MachineData> data, params string[] keys)
        {
            // "params" lets callers pass a comma-separated ID list; C# wraps it into a
            // string[] automatically. "MachineData?" (question mark) = nullable, i.e. this
            // method may legitimately return null.
            foreach (var key in keys)
            {
                // TryGetValue = throw-free dictionary lookup; "out var machine" both
                // declares and receives the found value in one statement.
                if (data.TryGetValue(key, out var machine) && machine != null)
                    return machine;
            }
            // No candidate existed (vanilla change / odd mod state) - caller decides.
            return null;
        }

        // "const" = a compile-time constant: fixed forever, cannot be changed at runtime.
        // Stardew tags items with invisible "context tags"; every iridium-quality item
        // automatically carries "quality_iridium", which we exploit to target just those
        // inputs.
        private const string IridiumQualityTag = "quality_iridium";
        // Suffix stamped onto our CLONED rule ids so repeat edits can recognize and skip
        // rules we injected earlier (keeps the asset edit idempotent).
        private const string IridiumRuleSuffix = "_BI_Iridium";

        /// <summary>
        /// Core of the quality-preserving feature: makes every output of a machine inherit
        /// the input's star quality, EXCEPT where vanilla hard-codes a high quality (the
        /// gold cap on large animal products). For those cases a higher-priority clone
        /// rule is injected that fires only for iridium inputs, so iridium stays iridium
        /// while silver/gold behave exactly like vanilla.
        /// </summary>
        /// <param name="machine">Machine whose OutputRules list is rewritten in place.</param>
        private static void ApplyQualityPreservingToAllOutputs(MachineData machine)
        {
            // Machines without output rules have nothing to preserve.
            if (machine.OutputRules == null || machine.OutputRules.Count == 0)
                return;

            // Build a NEW ordered list instead of inserting while enumerating - mutating
            // a List<T> during a foreach throws InvalidOperationException.
            var newOrder = new List<MachineOutputRule>();
            bool changed = false;

            // Output rules evaluate top-down; the first matching rule supplies the result,
            // so anything added EARLIER in the list acts as higher priority.
            foreach (var rule in machine.OutputRules)
            {
                // Vanilla caps large animal products at gold quality (e.g., Large Goat Milk -> Gold Goat Cheese).
                // Keep that floor for lower qualities, but let iridium inputs pass through via a higher-priority
                // duplicate rule gated on the "quality_iridium" context tag.
                bool hasFixedHighQualityOutput = false;
                if (rule.OutputItem != null)
                {
                    // Quality is numeric: 0 = normal, 1 = silver, 2 = gold. A rule with a
                    // fixed quality >= 2 FORCES gold-or-better no matter the input.
                    foreach (var output in rule.OutputItem)
                    {
                        if (output.Quality >= 2)
                            hasFixedHighQualityOutput = true;
                        else
                            // Normal-quality output: simply inherit the input's stars.
                            output.CopyQuality = true;
                    }
                }

                // Decide whether to prepend an iridium-only clone of this rule. Every
                // clause must hold: the rule caps quality, it has triggers, we haven't
                // already added our clone (making repeat edits harmless), and cloning
                // succeeded. "&&" short-circuits left to right.
                if (
                    hasFixedHighQualityOutput
                    && rule.Triggers != null
                    && rule.Triggers.Count > 0
                    && !HasRule(machine.OutputRules, rule.Id + IridiumRuleSuffix)
                    && TryCreateIridiumPassthroughRule(rule, out var iridiumRule)
                )
                {
                    // Clone goes FIRST: an iridium input matches it and keeps iridium
                    // quality; everyone else falls through to the original rule below.
                    newOrder.Add(iridiumRule);
                    changed = true;
                }

                // The original rule is always retained, after any injected clone.
                newOrder.Add(rule);
            }

            // Commit the rebuilt list only when something was actually added.
            if (changed)
                machine.OutputRules = newOrder;
        }

        /// <summary>
        /// Reports whether a rules list already contains a rule with the given Id.
        /// Guards against adding duplicate iridium clones when the asset gets edited
        /// multiple times within one session.
        /// </summary>
        /// <param name="rules">List of rules to scan.</param>
        /// <param name="id">Rule id to look for (compared case-insensitively).</param>
        /// <returns>true when a matching rule exists.</returns>
        private static bool HasRule(List<MachineOutputRule> rules, string id)
        {
            // Plain linear search - the lists are tiny, so LINQ isn't needed here.
            foreach (var rule in rules)
            {
                // OrdinalIgnoreCase = letter-by-letter compare ignoring case, avoiding
                // culture-dependent string surprises.
                if (string.Equals(rule.Id, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Builds a copy of a rule that triggers ONLY on iridium-quality inputs and
        /// outputs the same product at the input's own (iridium) quality. Uses a JSON
        /// serialize/deserialize round-trip as a quick DEEP CLONE - producing an
        /// independent object whose nested lists don't alias the original's.
        /// </summary>
        /// <param name="source">The rule to duplicate.</param>
        /// <param name="clone">Receives the finished clone (valid only on success).</param>
        /// <returns>true when the clone was built; false means 'clone' is unusable.</returns>
        private static bool TryCreateIridiumPassthroughRule(MachineOutputRule source, out MachineOutputRule clone)
        {
            // "out" parameters must be definitely assigned before EVERY return, so seed a
            // placeholder first. The "Try*" naming convention signals "returns bool plus
            // an out result" - C#'s standard alternative to throwing exceptions.
            clone = new MachineOutputRule();
            try
            {
                // Deep-clone trick: convert the object to a JSON text blob, then parse
                // that text back into a brand-new object graph.
                var json = JsonSerializer.Serialize(source);
                var cloned = JsonSerializer.Deserialize<MachineOutputRule>(json);
                // Defensive: bail if deserialization produced an empty shell.
                if (cloned == null || cloned.Triggers == null || cloned.OutputItem == null)
                    return false;

                // Rename the clone so HasRule() can spot it on subsequent edits.
                cloned.Id = source.Id + IridiumRuleSuffix;

                // Restrict every trigger to iridium inputs by demanding the quality tag.
                foreach (var trigger in cloned.Triggers)
                {
                    // "??=" assigns only when the left side is null - shorthand for
                    // "if (trigger.RequiredTags == null) trigger.RequiredTags = new List<string>();"
                    trigger.RequiredTags ??= new List<string>();
                    bool hasTag = false;
                    foreach (var tag in trigger.RequiredTags)
                    {
                        if (string.Equals(tag, IridiumQualityTag, StringComparison.OrdinalIgnoreCase))
                        {
                            hasTag = true;
                            break;   // Stop scanning; the tag is already present.
                        }
                    }
                    if (!hasTag)
                        trigger.RequiredTags.Add(IridiumQualityTag);
                }

                // Quality = -1 means "no fixed quality"; combined with CopyQuality the
                // output mirrors the iridium input instead of being snapped to gold.
                foreach (var output in cloned.OutputItem)
                {
                    output.Quality = -1;
                    output.CopyQuality = true;
                }

                clone = cloned;
                return true;
            }
            catch (Exception ex)
            {
                // LogLevel.Trace writes only to the log file: a failed clone just leaves
                // that one rule with vanilla behaviour, so no console alarm is needed.
                Monitor.Log($"Could not create iridium passthrough rule for '{source.Id}': {ex}", LogLevel.Trace);
                return false;
            }
        }

        /// <summary>
        /// Keg adjustments: (1) mead made from flower honey remembers WHICH flower honey
        /// was used and sells for 2x its price, (2) vegetable juice price multiplied by
        /// the configured buff factor, (3) optionally all keg outputs inherit quality.
        /// </summary>
        private static void ApplyKegEdits(IDictionary<string, MachineData> data)
        {
            // Fetch the Keg trying several historical IDs. "?." is the null-conditional
            // operator: if keg is null, the whole expression evaluates to null instead of
            // crashing - a compact guard.
            var keg = GetMachine(data, "(BC)12", "12", "Keg");
            if (keg?.OutputRules == null) return;

            // Walk every output rule (honey->mead, fruit->wine, vegetable->juice, ...) and
            // each candidate item inside it, tweaking whichever apply.
            foreach (var rule in keg.OutputRules)
            {
                if (rule.OutputItem == null) continue;

                foreach (var output in rule.OutputItem)
                {
                    // 1. Flower Honey Mead Fix
                    // "DROP_IN_PRESERVE_ID" is a magic placeholder telling the game to
                    // resolve the preserve flavour at runtime - so mead inherits exactly
                    // WHICH flower honey was kegged. CopyPrice copies the HONEY's price as
                    // the base value...
                    if (Config.EnableMeadFix && IsMeadOutput(rule, output))
                    {
                        output.PreserveId = "DROP_IN_PRESERVE_ID";
                        output.CopyPrice = true;
                        // ...then PriceModifiers stack math on top: Multiply by 2.0f gives
                        // the classic "mead = 2x honey value" scaling.
                        output.PriceModifiers = new List<QuantityModifier>
                        {
                            // "new()" = target-typed new: the compiler infers the type
                            // (QuantityModifier) from the surrounding collection.
                            new()
                            {
                                Modification = QuantityModifier.ModificationType.Multiply,
                                Amount = 2.0f
                            }
                        };
                    }

                    // 2. Vegetable Juice Buff
                    // Juice is a "preserve-type" product (generic template id 350); this
                    // multiplies its computed price by the user's factor (default 2.75x,
                    // up from vanilla's 2.25x).
                    if (Config.EnableJuiceBuff && IsJuiceOutput(rule, output))
                    {
                        output.PriceModifiers = new List<QuantityModifier>
                        {
                            new()
                            {
                                Modification = QuantityModifier.ModificationType.Multiply,
                                Amount = Config.JuiceMultiplier
                            }
                        };
                    }

                    // 3. Quality Preserving
                    // CopyQuality hands the fruit/vegetable's star rating (silver/gold/
                    // iridium) down to the drink instead of resetting it to normal.
                    if (Config.EnableQualityPreserving)
                    {
                        output.CopyQuality = true;
                    }
                }
            }
        }

        /// <summary>
        /// True when this rule/output combination produces MEAD (item id 459). Several
        /// naming schemes are accepted ("459", "(O)459", ids/rule names like "Mead") so
        /// detection survives game-version differences.
        /// </summary>
        private static bool IsMeadOutput(MachineOutputRule rule, MachineItemOutput output)
        {
            // "||" short-circuits: evaluation stops at the first true condition.
            return output.ItemId == "459"
                || output.ItemId == "(O)459"
                || string.Equals(output.Id, "Mead", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Mead", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Default_Mead", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True when this output is JUICE: either a preserve-type product flagged with
        /// PreserveType "Juice" (any kegged vegetable) or the explicit juice template id
        /// 350 / matching rule names.
        /// </summary>
        private static bool IsJuiceOutput(MachineOutputRule rule, MachineItemOutput output)
        {
            return string.Equals(output.PreserveType, "Juice", StringComparison.OrdinalIgnoreCase)
                || output.ItemId == "350"
                || output.ItemId == "(O)350"
                || string.Equals(output.Id, "Juice", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Juice", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.Id, "Default_Juice", StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>
        /// Preserves Jar (pickles, jelly, caviar...): switches every output to copy the
        /// input item's quality, so gold-star produce yields gold-star preserves.
        /// </summary>
        private static void ApplyPreservesJarEdits(IDictionary<string, MachineData> data)
        {
            // Feature disabled in config? Touch nothing at all.
            if (!Config.EnableQualityPreserving) return;

            var jar = GetMachine(data, "(BC)15", "15", "PreservesJar");
            if (jar != null)
            {
                ApplyQualityPreservingToAllOutputs(jar);
            }
        }

        /// <summary>
        /// Cheese Press (milk -> cheese, goat milk -> goat cheese): enables quality
        /// inheritance, including the iridium passthrough for large milks.
        /// </summary>
        private static void ApplyCheesePressEdits(IDictionary<string, MachineData> data)
        {
            // Feature toggle check first - skip all asset work when disabled.
            if (!Config.EnableQualityPreserving) return;

            var press = GetMachine(data, "(BC)16", "16", "CheesePress");
            if (press != null)
            {
                ApplyQualityPreservingToAllOutputs(press);
            }
        }

        /// <summary>
        /// Mayonnaise Machine (eggs -> mayonnaise, including gold dino/void variants):
        /// enables quality inheritance with the iridium passthrough behaviour.
        /// </summary>
        private static void ApplyMayonnaiseMachineEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableQualityPreserving) return;

            var mayo = GetMachine(data, "(BC)24", "24", "MayonnaiseMachine");
            if (mayo != null)
            {
                ApplyQualityPreservingToAllOutputs(mayo);
            }
        }

        /// <summary>
        /// Loom (wool -> cloth): enables quality inheritance, so premium wool weaves
        /// into premium cloth.
        /// </summary>
        private static void ApplyLoomEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableQualityPreserving) return;

            var loom = GetMachine(data, "(BC)17", "17", "Loom");
            if (loom != null)
            {
                ApplyQualityPreservingToAllOutputs(loom);
            }
        }

        /// <summary>
        /// Oil Maker (sunflower, corn, sunflower seeds... -> oil; truffles -> truffle oil).
        /// Fixes Truffle Oil so its price AND quality scale off the input Truffle
        /// (vanilla ignores truffle quality entirely); other oils just gain quality
        /// inheritance when the toggle is on.
        /// </summary>
        private static void ApplyOilMakerEdits(IDictionary<string, MachineData> data)
        {
            var oilMaker = GetMachine(data, "(BC)19", "19", "OilMaker");
            if (oilMaker?.OutputRules == null) return;

            foreach (var rule in oilMaker.OutputRules)
            {
                if (rule.OutputItem == null) continue;

                foreach (var output in rule.OutputItem)
                {
                    // Identify the Truffle Oil output across ID styles: numeric "432",
                    // qualified "(O)432", or human-readable Ids on the output/rule.
                    bool isTruffleOil = output.ItemId == "432"
                        || output.ItemId == "(O)432"
                        || string.Equals(output.Id, "TruffleOil", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(rule.Id, "Truffle", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(rule.Id, "TruffleOil", StringComparison.OrdinalIgnoreCase);

                    if (isTruffleOil && Config.EnableTruffleOilFix)
                    {
                        // CopyPrice: the base sell price comes from THIS batch's truffle,
                        // so an expensive iridium truffle yields pricier oil.
                        // CopyQuality: the star rating carries over as well.
                        output.CopyPrice = true;
                        output.CopyQuality = true;
                        // Then PriceModifiers multiply that copied price by the user's
                        // configured factor (default 1.5x).
                        output.PriceModifiers = new List<QuantityModifier>
                        {
                            new()
                            {
                                Modification = QuantityModifier.ModificationType.Multiply,
                                Amount = Config.TruffleOilMultiplier
                            }
                        };
                    }
                    else if (Config.EnableQualityPreserving)
                    {
                        // Non-truffle oils (sunflower/corn) just gain quality inheritance.
                        output.CopyQuality = true;
                    }
                }
            }
        }

        /// <summary>
        /// Dehydrator (Stardew 1.6: dries produce/mushrooms into "Dried X" items):
        /// enables quality inheritance for its outputs.
        /// </summary>
        private static void ApplyDehydratorEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableQualityPreserving) return;

            var dehydrator = GetMachine(data, "(BC)Dehydrator", "Dehydrator", "(BC)272", "272");
            if (dehydrator != null)
            {
                ApplyQualityPreservingToAllOutputs(dehydrator);
            }
        }

        /// <summary>
        /// Fish Smoker (Stardew 1.6: fish + coal -> smoked fish): enables quality
        /// inheritance so quality fish smokes into quality product.
        /// </summary>
        private static void ApplyFishSmokerEdits(IDictionary<string, MachineData> data)
        {
            if (!Config.EnableQualityPreserving) return;

            var smoker = GetMachine(data, "(BC)FishSmoker", "FishSmoker", "(BC)274", "274");
            if (smoker != null)
            {
                ApplyQualityPreservingToAllOutputs(smoker);
            }
        }

        /// <summary>
        /// Cask (cellar aging): adds a brand-new aging rule so Vegetable Juice can also
        /// be aged, climbing the quality ladder (silver -> gold -> iridium) like wine.
        /// Vanilla casks only accept wine, beer, mead, roe and cheese/goat cheese.
        /// </summary>
        private static void ApplyCaskEdits(IDictionary<string, MachineData> data)
        {
            // Respect the expanded-aging toggle.
            if (!Config.EnableExpandedAging) return;

            var cask = GetMachine(data, "(BC)163", "163", "Cask");
            if (cask?.OutputRules == null) return;

            // Check if Juice aging rule is already added. ".Exists(...)" is a List method
            // that returns true when ANY element matches the lambda predicate ("r =>" and
            // "t =>" declare tiny inline test functions). Checking first keeps this edit
            // idempotent: running twice never creates duplicate rules.
            bool hasJuiceRule = cask.OutputRules.Exists(r =>
                string.Equals(r.Id, "BetterIndustry_Juice", StringComparison.OrdinalIgnoreCase) ||
                (r.Triggers != null && r.Triggers.Exists(t => t.RequiredItemId == "(O)350" || t.RequiredItemId == "350")));

            if (!hasJuiceRule)
            {
                // Object-initializer syntax: "new Type { Prop = value, ... }" assigns
                // properties immediately after construction - a compact way to build
                // config-style objects without a long constructor parameter list.
                var juiceRule = new MachineOutputRule
                {
                    // Unique rule id; the "BetterIndustry_" prefix marks it as ours.
                    Id = "BetterIndustry_Juice",
                    // Trigger: fire specifically when item id 350 (the juice template,
                    // i.e. any vegetable juice) is placed into the cask.
                    Triggers = new List<MachineOutputTriggerRule>
                    {
                        new()
                        {
                            Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                            RequiredItemId = "(O)350"
                        }
                    },
                    UseFirstValidOutput = true,
                    OutputItem = new List<MachineItemOutput>
                    {
                        new()
                        {
                            // Delegate output creation to the Cask's own C# method
                            // "OutputCask" (format: "Namespace.Class, Assembly:Method"),
                            // reusing vanilla's aging math untouched.
                            OutputMethod = "StardewValley.Objects.Cask, Stardew Valley:OutputCask",
                            // Free-form key/value bag read by that method: this multiplier
                            // controls how fast juice climbs the aging quality ladder.
                            CustomData = new Dictionary<string, string>
                            {
                                ["AgingMultiplier"] = "4"
                            }
                        }
                    }
                };

                cask.OutputRules.Add(juiceRule);
            }
        }
    }
}

