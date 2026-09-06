using HarmonyLib;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Objects.Trinkets;

// TrinketReforgeLogic powers BetterForge's "Never Downgrade" Anvil trinket
// reforging. A trinket's stats all flow from one number — its generationSeed —
// so instead of letting the game reroll blindly (which can make a trinket worse),
// this code simulates candidate seeds ahead of time, grades each result with
// Evaluate(), and only commits the best upgrade found.
namespace BetterForge
{
    /// <summary>
    /// A simple "report card" describing how good one particular trinket roll is:
    /// its tier (1..MaxTier), a 0-1 quality score, and a human-readable summary.
    /// </summary>
    public class TrinketEvaluation
    {
        /// <summary>The rolled tier/quality level of this trinket.</summary>
        public int Tier { get; set; } = 1;

        /// <summary>Highest tier this trinket type can reach (varies per trinket).</summary>
        public int MaxTier { get; set; } = 5;

        /// <summary>Normalized 0.0-1.0 quality score used to compare rolls.</summary>
        public float Score { get; set; } = 0f;

        /// <summary>Short text summary of the actual stats, shown in HUD messages.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>True if this roll hit the special "Perfect" max roll.</summary>
        public bool IsMaxRoll { get; set; } = false;
    }

    /// <summary>
    /// Grades trinket rolls from their generation seed and performs never-downgrade
    /// reforges by searching for an upgrading seed before applying it.
    /// </summary>
    public static class TrinketReforgeLogic
    {
        // Data-field keys stored on each trinket item so the mod can remember how
        // many times it was reforged (the legacy key keeps data from the old mod name).
        /// <summary>modData key storing how many times a trinket has been reforged.</summary>
        public const string ReforgeCountKey = "feiluvnana.BetterForge/ReforgeCount";

        /// <summary>Older modData key kept for migrating saves from the previous mod name.</summary>
        public const string LegacyReforgeCountKey = "feiluvnana.BetterTrinket/ReforgeCount";

        /// <summary>
        /// Clears the trinket's cached display strings so tooltips show fresh stats,
        /// then re-applies its passive effects if a player is currently wearing it.
        /// </summary>
        /// <param name="trinket">The trinket whose stats just changed.</param>
        /// <param name="who">The player, or null if nobody is wearing it.</param>
        public static void ResetCachedDescription(Trinket trinket, Farmer? who)
        {
            if (trinket == null) return;

            // The game caches display names/descriptions for performance. Reflection
            // via AccessTools lets us reset those private fields so the new roll's
            // numbers appear immediately in the tooltip.
            AccessTools.Field(typeof(Trinket), "_description")?.SetValue(trinket, null);
            AccessTools.Field(typeof(Trinket), "displayNameOverride")?.SetValue(trinket, null);
            AccessTools.Field(typeof(StardewValley.Object), "displayName")?.SetValue(trinket, null);

            // If someone is WEARING this trinket right now, remove and re-add its
            // effects so the new stat values actually take hold.
            if (who != null && who.trinketItems.Contains(trinket))
            {
                trinket.Unapply(who);
                trinket.Apply(who);
            }
        }

        /// <summary>
        /// Simulates what stats a given seed would produce for a trinket, without
        /// touching any real item. Each trinket family has its own rolling rules.
        /// </summary>
        /// <param name="itemId">The trinket's ID, e.g. "(TR)fairybox".</param>
        /// <param name="seed">The generation seed whose outcome we want to grade.</param>
        /// <returns>An evaluation with tier, score, perfect-roll flag, and summary text.</returns>
        public static TrinketEvaluation Evaluate(string itemId, int seed)
        {
            var eval = new TrinketEvaluation();

            // Create a private random generator seeded ONLY by `seed`: every call with
            // the same seed replays the exact same rolls. That's what makes results
            // predictable and lets us "test" seeds before committing them.
            Random r = Utility.CreateRandom(seed);

            // Normalize the ID: strip the "(TR)" category prefix, whitespace, and
            // uppercase letters so switch matching is simple ("FairyBox" -> "fairybox").
            string cleanId = itemId.Replace("(TR)", "").Trim().ToLowerInvariant();

            switch (cleanId)
            {
                case "fairybox":
                {
                    eval.MaxTier = 5;
                    // Cascading probability checks matching vanilla FairyBoxTrinketEffect.GenerateTrinketEffect()
                    int num = 1;
                    if (r.NextBool(0.45)) num = 2;
                    if (r.NextBool(0.25)) num = 3;
                    if (r.NextBool(0.125)) num = 4;
                    if (r.NextBool(0.0675)) num = 5;

                    eval.Tier = num;
                    eval.Score = (num - 1) / 4.0f;
                    eval.IsMaxRoll = (num == 5);
                    // Mirror vanilla FairyBox math so the summary matches real behavior:
                    // higher tiers heal faster (smaller interval) and heal harder (power).
                    float interval = (5000 - num * 300) / 1000f;
                    float power = 0.7f + num * 0.1f;
                    eval.Summary = $"Level {num}/5 (Heal Pulse: {interval:0.0}s, Power: {power:0.0}x)";
                    break;
                }

                case "magicquiver":
                {
                    eval.MaxTier = 5;
                    int minDmg, maxDmg;
                    float delay;
                    string style = "Normal";

                    if (r.NextBool(0.04))
                    {
                        // Rare 4% jackpot: fixed high-damage "Perfect" quiver.
                        style = "Perfect";
                        minDmg = 30;
                        maxDmg = 35;
                        delay = 900f;
                    }
                    else if (r.NextBool(0.1))
                    {
                        // 10% branch: a special Rapid or Heavy firing style.
                        if (r.NextBool(0.5))
                        {
                            style = "Rapid";
                            // r.Next(10, 15) gives 10-14 (upper bound is exclusive);
                            // the -2 mirrors how vanilla offsets its base damage roll.
                            minDmg = r.Next(10, 15) - 2;
                            maxDmg = minDmg + 5;
                            delay = 600 + r.Next(11) * 10;
                        }
                        else
                        {
                            style = "Heavy";
                            minDmg = r.Next(25, 41) - 2;
                            maxDmg = minDmg + 5;
                            delay = 1500 + r.Next(6) * 100;
                        }
                    }
                    else
                    {
                        // Remaining ~86%: ordinary quiver with middling stats.
                        minDmg = r.Next(15, 31) - 2;
                        maxDmg = minDmg + 5;
                        delay = 1100 + r.Next(11) * 100;
                    }

                    float avgDmg = (minDmg + maxDmg) / 2.0f;
                    // DPS = average damage per second; slower delay means lower DPS.
                    float dps = avgDmg / (delay / 1000f);
                    eval.Score = Math.Clamp((dps - 10f) / 26f, 0f, 1f);
                    eval.Tier = style switch
                    {
                        "Perfect" => 5,
                        "Heavy" => 4,
                        "Rapid" => 4,
                        _ => Math.Clamp(1 + (int)Math.Floor(eval.Score * 2.99f), 1, 3)
                    };
                    eval.IsMaxRoll = (style == "Perfect");
                    eval.Summary = $"{style} | Cooldown: {delay / 1000f:0.00}s | Dmg: {minDmg}-{maxDmg}";
                    break;
                }

                case "icerod":
                {
                    eval.MaxTier = 5;
                    // In vanilla SDV 1.6: IceOrbTrinketEffect checks NextBool(0.05) first,
                    // then rolls duration = Next(2, 5) * 1000 and delay = Next(3, 6) * 1000f.
                    bool isPerfect = r.NextBool(0.05);
                    float delay;
                    int freeze;

                    if (isPerfect)
                    {
                        delay = 3000f;
                        freeze = 4000;
                    }
                    else
                    {
                        freeze = r.Next(2, 5) * 1000;
                        delay = (float)r.Next(3, 6) * 1000f;
                    }

                    // Convert both stats to 0-1 subscores (shorter delay scores higher),
                    // then blend them evenly. Non-perfect rolls are capped at 0.95 so a
                    // Perfect always outranks everything else.
                    float delayScore = 1.0f - ((delay - 3000f) / 2000f);
                    float freezeScore = (freeze - 2000) / 2000f;
                    eval.Score = isPerfect ? 1.0f : Math.Clamp(delayScore * 0.5f + freezeScore * 0.5f, 0f, 0.95f);
                    eval.Tier = isPerfect ? 5 : Math.Clamp(1 + (int)Math.Floor(eval.Score * 3.99f), 1, 4);
                    eval.IsMaxRoll = isPerfect;
                    eval.Summary = $"Delay: {delay / 1000f:0.0}s | Freeze: {freeze / 1000f:0.0}s{(isPerfect ? " (Perfect)" : "")}";
                    break;
                }

                case "goldenspur":
                case "iridiumspur":
                {
                    eval.MaxTier = 5;
                    int duration = r.Next(5, 11); // 5 to 10 seconds
                    eval.Score = (duration - 5) / 5.0f;
                    eval.Tier = Math.Clamp(duration - 5 + 1, 1, 5);
                    eval.IsMaxRoll = (duration == 10);
                    eval.Summary = $"Crit Speed Boost: {duration}s [5s-10s]";
                    break;
                }

                case "parrotegg":
                {
                    eval.MaxTier = 4;
                    // r.Next(1, 5) yields 1-4: the parrot's bonus coin-drop level.
                    int level = r.Next(1, 5);
                    eval.Tier = level;
                    eval.Score = (level - 1) / 3.0f;
                    eval.IsMaxRoll = (level == 4);
                    eval.Summary = $"Level {level}/4 ({level * 10}% Gold Coin Drop Chance)";
                    break;
                }

                case "frogegg":
                {
                    eval.MaxTier = 7;
                    // Weighted variant pick: 20% guaranteed basic Green, then two
                    // 80%-chance gates that select mid-tier or rare frog colors.
                    // r.Next(3) gives 0-2, r.Next(3)+3 gives 3-5, etc.
                    int variant = 0;
                    if (r.NextBool(0.2)) variant = 0;
                    else if (r.NextBool(0.8)) variant = r.Next(3);
                    else if (r.NextBool(0.8)) variant = r.Next(3) + 3;
                    else variant = r.Next(2) + 6;

                    string[] variantNames = { "Green", "Yellow", "Red", "Blue", "Void", "Poison", "Prismatic", "Prismatic" };
                    // Guard against an out-of-range index before indexing the array;
                    // the `? :` ternary picks the fallback name if needed.
                    string name = variant >= 0 && variant < variantNames.Length ? variantNames[variant] : "Green";
                    eval.Tier = Math.Clamp(variant + 1, 1, 7);
                    eval.Score = variant / 6.0f;
                    eval.IsMaxRoll = (variant >= 6);
                    eval.Summary = $"{name} Frog Variant";
                    break;
                }

                case "basiliskpaw":
                {
                    // Basilisk Paw has only one fixed effect (debuff immunity), so
                    // every seed grades as an automatic perfect roll.
                    eval.MaxTier = 1;
                    eval.Tier = 1;
                    eval.Score = 1.0f;
                    eval.IsMaxRoll = true;
                    eval.Summary = "Debuff Immunity (Perfect)";
                    break;
                }

                default:
                {
                    // Unknown/custom trinket: assume a neutral mid-tier result so
                    // reforging still behaves sensibly instead of crashing.
                    eval.MaxTier = 5;
                    eval.Tier = 3;
                    eval.Score = 0.5f;
                    eval.Summary = "Standard Trinket";
                    break;
                }
            }

            return eval;
        }

        /// <summary>
        /// Calculates the success probability for upgrading a trinket from currentTier to currentTier + 1.
        /// Uses a normalized (M - i) / M curve such that the total expected rolls from Tier 1 to MaxTier (M)
        /// equals targetTotalRolls (default 15 rolls = 45 Iridium Bars at 3 bars/roll) across every trinket family.
        /// </summary>
        /// <param name="currentTier">Current tier (1-indexed).</param>
        /// <param name="maxTier">Maximum tier for this trinket family (M).</param>
        /// <param name="targetTotalRolls">Total expected rolls from Tier 1 to MaxTier (default 15.0 = 45 Iridium Bars).</param>
        /// <returns>Success probability clamped between 0.05 and 0.95.</returns>
        public static double CalculateUpgradeSuccessChance(int currentTier, int maxTier, double targetTotalRolls = 15.0)
        {
            if (currentTier >= maxTier || maxTier <= 1)
                return 0.0;

            // Harmonic number H_{M-1} = 1/1 + 1/2 + ... + 1/(M-1)
            double harmonicSum = 0.0;
            for (int j = 1; j < maxTier; j++)
            {
                harmonicSum += 1.0 / j;
            }

            // Normalization factor k_M = (M * H_{M-1}) / targetTotalRolls
            // P(i, M) = ((M - i) / M) * k_M
            double factor = (maxTier * harmonicSum) / targetTotalRolls;
            double rawChance = ((double)(maxTier - currentTier) / maxTier) * factor;

            return Math.Clamp(rawChance, 0.05, 0.95);
        }

        /// <summary>
        /// Performs one Anvil reforge roll:
        /// Success probability is governed by CalculateUpgradeSuccessChance.
        /// On success: advances the trinket to the next tier and applies upgraded stats.
        /// On failure: preserves the current tier and stats ("Never Downgrade" protection).
        /// </summary>
        /// <param name="trinket">The trinket being reforged.</param>
        /// <param name="who">The player doing the reforge (receives messages/sounds).</param>
        /// <param name="config">Current mod settings.</param>
        /// <returns>The generation seed that was applied.</returns>
        public static int ProcessReforge(Trinket trinket, Farmer who, ModConfig config)
        {
            // When PreventDowngrades is off, simply reroll once without any upgrade protection
            if (!config.PreventDowngrades)
            {
                int randomSeed = Game1.random.Next();
                trinket.RerollStats(randomSeed);
                IncrementReforgeCount(trinket);
                ResetCachedDescription(trinket, who);

                var eval = Evaluate(trinket.ItemId, randomSeed);
                if (config.ShowReforgeSuccessMessage)
                {
                    if (eval.IsMaxRoll)
                    {
                        Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("hud.reforge-perfect", new { item = trinket.DisplayName }), 1));
                        who.currentLocation?.playSound("yoba");
                    }
                    else
                    {
                        Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("hud.reforge-upgrade", new { item = trinket.DisplayName, tier = eval.Tier, maxTier = eval.MaxTier }), 1));
                    }
                }
                return randomSeed;
            }

            // Grade what the trinket currently has
            int currentSeed = trinket.generationSeed.Value;
            var currentEval = Evaluate(trinket.ItemId, currentSeed);

            // If already at max tier, cannot upgrade further
            if (currentEval.IsMaxRoll || currentEval.Tier >= currentEval.MaxTier)
            {
                return currentSeed;
            }

            // Calculate normalized success chance so all trinkets average 45 Iridium Bars (45 / IridiumBarCost rolls)
            double targetRolls = 45.0 / Math.Max(1, config.IridiumBarCost);
            double chance = CalculateUpgradeSuccessChance(currentEval.Tier, currentEval.MaxTier, targetRolls);
            bool isSuccess = Game1.random.NextDouble() < chance;

            if (!isSuccess)
            {
                // Roll failed: Never Downgrade protection keeps current stats!
                IncrementReforgeCount(trinket);
                ResetCachedDescription(trinket, who);

                if (config.ShowReforgeSuccessMessage)
                {
                    Game1.addHUDMessage(new HUDMessage(
                        ModEntry.I18n.Get("hud.reforge-fail", new { item = trinket.DisplayName, tier = currentEval.Tier, maxTier = currentEval.MaxTier }),
                        2
                    ));
                }
                return currentSeed;
            }

            // Roll succeeded: search for a seed that produces tier i + 1
            int targetTier = currentEval.Tier + 1;
            int bestSeed = currentSeed;
            float bestScore = -1f;

            Random rng = Game1.random;

            for (int attempt = 0; attempt < 1500; attempt++)
            {
                int cand = rng.Next();
                var candEval = Evaluate(trinket.ItemId, cand);

                if (candEval.Tier == targetTier)
                {
                    if (candEval.Score > bestScore)
                    {
                        bestScore = candEval.Score;
                        bestSeed = cand;
                        if (candEval.IsMaxRoll) break;
                    }
                }
            }

            // Safety fallback: if no exact match found, accept candEval.Tier >= targetTier
            if (bestSeed == currentSeed)
            {
                for (int attempt = 0; attempt < 1000; attempt++)
                {
                    int cand = rng.Next();
                    var candEval = Evaluate(trinket.ItemId, cand);
                    if (candEval.Tier >= targetTier && candEval.Score > bestScore)
                    {
                        bestScore = candEval.Score;
                        bestSeed = cand;
                        break;
                    }
                }
            }

            // Commit winning seed
            trinket.RerollStats(bestSeed);
            IncrementReforgeCount(trinket);
            ResetCachedDescription(trinket, who);

            var finalEval = Evaluate(trinket.ItemId, bestSeed);

            if (finalEval.IsMaxRoll || finalEval.Tier >= finalEval.MaxTier)
            {
                if (config.ShowReforgeSuccessMessage)
                {
                    Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("hud.reforge-perfect", new { item = trinket.DisplayName }), 1));
                    who.currentLocation?.playSound("yoba");
                }
            }
            else
            {
                if (config.ShowReforgeSuccessMessage)
                {
                    Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("hud.reforge-upgrade", new { item = trinket.DisplayName, tier = finalEval.Tier, maxTier = finalEval.MaxTier }), 1));
                }
            }

            return bestSeed;
        }

        private static void IncrementReforgeCount(Trinket trinket)
        {
            int reforgeCount = 0;
            if (trinket.modData.TryGetValue(ReforgeCountKey, out string? countStr) && int.TryParse(countStr, out int count))
            {
                reforgeCount = count;
            }
            else if (trinket.modData.TryGetValue(LegacyReforgeCountKey, out string? legCountStr) && int.TryParse(legCountStr, out int legCount))
            {
                reforgeCount = legCount;
            }
            trinket.modData[ReforgeCountKey] = (reforgeCount + 1).ToString();
        }
    }
}
