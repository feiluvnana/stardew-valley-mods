using System;
using HarmonyLib;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Objects.Trinkets;

namespace BetterTrinket
{
    public class TrinketEvaluation
    {
        public int Tier { get; set; } = 1;
        public int MaxTier { get; set; } = 5;
        public float Score { get; set; } = 0f;
        public string Summary { get; set; } = string.Empty;
        public bool IsMaxRoll { get; set; } = false;
        public string StarString { get; set; } = string.Empty;
    }

    public static class TrinketReforgeLogic
    {
        public const string ReforgeCountKey = "feiluvnana.BetterTrinket/ReforgeCount";

        public static void ResetCachedDescription(Trinket trinket, Farmer? who)
        {
            if (trinket == null) return;

            AccessTools.Field(typeof(Trinket), "_description")?.SetValue(trinket, null);
            AccessTools.Field(typeof(Trinket), "displayNameOverride")?.SetValue(trinket, null);
            AccessTools.Field(typeof(StardewValley.Object), "displayName")?.SetValue(trinket, null);

            if (who != null && who.trinketItems.Contains(trinket))
            {
                trinket.Unapply(who);
                trinket.Apply(who);
            }
        }

        public static TrinketEvaluation Evaluate(string itemId, int seed)
        {
            var eval = new TrinketEvaluation();
            Random r = Utility.CreateRandom(seed);

            string cleanId = itemId.Replace("(TR)", "").Trim().ToLowerInvariant();

            switch (cleanId)
            {
                case "fairybox":
                {
                    eval.MaxTier = 5;
                    int num = 1;
                    if (r.NextBool(0.45)) num = 2;
                    else if (r.NextBool(0.25)) num = 3;
                    else if (r.NextBool(0.125)) num = 4;
                    else if (r.NextBool(0.0675)) num = 5;

                    eval.Tier = num;
                    eval.Score = (num - 1) / 4.0f;
                    eval.IsMaxRoll = (num == 5);
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
                        style = "Perfect";
                        minDmg = 30;
                        maxDmg = 35;
                        delay = 900f;
                    }
                    else if (r.NextBool(0.1))
                    {
                        if (r.NextBool(0.5))
                        {
                            style = "Rapid";
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
                        minDmg = r.Next(15, 31) - 2;
                        maxDmg = minDmg + 5;
                        delay = 1100 + r.Next(11) * 100;
                    }

                    float avgDmg = (minDmg + maxDmg) / 2.0f;
                    float dps = avgDmg / (delay / 1000f);
                    eval.Score = Math.Clamp((dps - 10f) / 26f, 0f, 1f);
                    eval.Tier = style switch
                    {
                        "Perfect" => 5,
                        "Heavy" => 4,
                        "Rapid" => 4,
                        _ => Math.Clamp(1 + (int)Math.Round(eval.Score * 3.0f), 1, 5)
                    };
                    eval.IsMaxRoll = (style == "Perfect");
                    eval.Summary = $"{style} | Cooldown: {delay / 1000f:0.00}s | Dmg: {minDmg}-{maxDmg}";
                    break;
                }

                case "icerod":
                {
                    eval.MaxTier = 5;
                    float delay = r.Next(3000, 5001);
                    int freeze = r.Next(2000, 4001);
                    bool isPerfect = false;

                    if (r.NextDouble() < 0.05)
                    {
                        isPerfect = true;
                        delay = 3000f;
                        freeze = 4000;
                    }

                    float delayScore = 1.0f - ((delay - 3000f) / 2000f);
                    float freezeScore = (freeze - 2000) / 2000f;
                    eval.Score = isPerfect ? 1.0f : Math.Clamp(delayScore * 0.5f + freezeScore * 0.5f, 0f, 0.95f);
                    eval.Tier = isPerfect ? 5 : Math.Clamp(1 + (int)Math.Round(eval.Score * 3.5f), 1, 5);
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
                    int maxLevel = Math.Min(4, (int)(1 + (Game1.player?.totalMoneyEarned ?? 0) / 750000));
                    int stat = r.Next(0, Math.Max(1, maxLevel));
                    int level = stat + 1;
                    eval.Tier = level;
                    eval.Score = (level - 1) / 3.0f;
                    eval.IsMaxRoll = (level == 4);
                    eval.Summary = $"Level {level}/4 ({level * 10}% Gold Coin Drop Chance)";
                    break;
                }

                case "frogegg":
                {
                    eval.MaxTier = 7;
                    int variant = 0;
                    if (r.NextBool(0.2)) variant = 0;
                    else if (r.NextBool(0.8)) variant = r.Next(3);
                    else if (r.NextBool(0.8)) variant = r.Next(3) + 3;
                    else variant = r.Next(2) + 6;

                    string[] variantNames = { "Green", "Yellow", "Red", "Blue", "Void", "Poison", "Prismatic", "Prismatic" };
                    string name = variant >= 0 && variant < variantNames.Length ? variantNames[variant] : "Green";
                    eval.Tier = Math.Clamp(variant + 1, 1, 7);
                    eval.Score = variant / 6.0f;
                    eval.IsMaxRoll = (variant >= 6);
                    eval.Summary = $"{name} Frog Variant";
                    break;
                }

                case "basiliskpaw":
                {
                    eval.MaxTier = 1;
                    eval.Tier = 1;
                    eval.Score = 1.0f;
                    eval.IsMaxRoll = true;
                    eval.Summary = "Debuff Immunity (Perfect)";
                    break;
                }

                default:
                {
                    eval.MaxTier = 5;
                    eval.Tier = 3;
                    eval.Score = 0.5f;
                    eval.Summary = "Standard Trinket";
                    break;
                }
            }

            // Generate star rating string
            int fullStars = Math.Clamp(eval.Tier, 0, eval.MaxTier);
            int emptyStars = Math.Max(0, eval.MaxTier - fullStars);
            eval.StarString = new string('★', fullStars) + new string('☆', emptyStars);

            return eval;
        }

        public static int ProcessReforge(Trinket trinket, Farmer who, ModConfig config)
        {
            int currentSeed = trinket.generationSeed.Value;
            var currentEval = Evaluate(trinket.ItemId, currentSeed);

            // Read existing reforge count
            int count = 0;
            if (trinket.modData.TryGetValue(ReforgeCountKey, out string? countStr) && int.TryParse(countStr, out int parsedCount))
            {
                count = parsedCount;
            }
            count++;

            bool isPity = config.EnablePitySystem && (count >= config.RollsForGuaranteedUpgrade);
            float targetMinScore = 0f;

            if (isPity)
            {
                // Force an upgrade or top tier
                targetMinScore = Math.Min(1.0f, currentEval.Score + 0.2f);
            }
            else if (config.PreventDowngrades)
            {
                targetMinScore = currentEval.Score;
            }

            int bestSeed = currentSeed;
            float bestScore = currentEval.Score;
            bool foundImprovement = false;

            Random rng = Game1.random;

            // Search candidate seeds
            for (int i = 0; i < 500; i++)
            {
                int candidateSeed = rng.Next();
                var candidateEval = Evaluate(trinket.ItemId, candidateSeed);

                if (candidateEval.Score > bestScore)
                {
                    bestScore = candidateEval.Score;
                    bestSeed = candidateSeed;
                    foundImprovement = true;

                    if (candidateEval.IsMaxRoll)
                        break;
                }
                else if (!foundImprovement && candidateEval.Score >= targetMinScore)
                {
                    bestScore = candidateEval.Score;
                    bestSeed = candidateSeed;
                    foundImprovement = true;
                }
            }

            // Apply selected seed and re-roll stats natively
            trinket.RerollStats(bestSeed);
            ResetCachedDescription(trinket, who);

            var finalEval = Evaluate(trinket.ItemId, bestSeed);

            if (finalEval.IsMaxRoll)
            {
                count = 0; // Reset pity counter on max roll
                if (config.ShowReforgeSuccessMessage)
                {
                    Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("hud.reforge-perfect", new { item = trinket.DisplayName })));
                    who.currentLocation.playSound("yoba");
                }
            }
            else if (finalEval.Score > currentEval.Score)
            {
                count = 0; // Reset pity counter on improvement
                if (config.ShowReforgeSuccessMessage)
                {
                    Game1.addHUDMessage(new HUDMessage(ModEntry.I18n.Get("hud.reforge-upgrade", new { item = trinket.DisplayName, tier = finalEval.Tier, maxTier = finalEval.MaxTier })));
                }
            }

            // Save updated reforge count
            trinket.modData[ReforgeCountKey] = count.ToString();

            return bestSeed;
        }
    }
}
