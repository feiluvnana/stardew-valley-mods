using System;
using StardewValley;
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

        public static TrinketEvaluation Evaluate(string itemId, int seed)
        {
            var eval = new TrinketEvaluation();
            Random r = Utility.CreateRandom(seed);

            string cleanId = itemId.Replace("(TR)", "").Trim();

            switch (cleanId)
            {
                case "FairyBox":
                {
                    eval.MaxTier = 5;
                    float roll = (float)r.NextDouble();
                    if (roll < 0.03f) eval.Tier = 5;
                    else if (roll < 0.12f) eval.Tier = 4;
                    else if (roll < 0.30f) eval.Tier = 3;
                    else if (roll < 0.60f) eval.Tier = 2;
                    else eval.Tier = 1;

                    eval.Score = (eval.Tier - 1) / 4.0f;
                    eval.IsMaxRoll = eval.Tier == 5;
                    float interval = 10.0f - eval.Tier * 1.2f;
                    eval.Summary = $"Level {eval.Tier}/5 (Heal Pulse: {interval:0.0}s)";
                    break;
                }

                case "MagicQuiver":
                {
                    eval.MaxTier = 5;
                    int delay = r.Next(54, 97); // 54 to 96 frames (0.9s to 1.6s)
                    int minDmg = r.Next(20, 31);
                    int maxDmg = r.Next(35, 46);

                    float delayScore = 1.0f - ((delay - 54) / 42.0f);
                    float dmgScore = ((minDmg - 20) / 10.0f + (maxDmg - 35) / 10.0f) / 2.0f;
                    eval.Score = Math.Clamp(delayScore * 0.6f + dmgScore * 0.4f, 0f, 1f);

                    eval.Tier = 1 + (int)Math.Round(eval.Score * 4.0f);
                    eval.IsMaxRoll = delay <= 56 && maxDmg >= 44;
                    eval.Summary = $"Cooldown: {delay / 60.0f:0.00}s [0.90s-1.60s] | Dmg: {minDmg}-{maxDmg}";
                    break;
                }

                case "IceRod":
                {
                    eval.MaxTier = 5;
                    int delay = r.Next(180, 301); // 3.0s to 5.0s
                    int duration = r.Next(180, 301); // 3.0s to 5.0s

                    float delayScore = 1.0f - ((delay - 180) / 120.0f);
                    float durScore = (duration - 180) / 120.0f;
                    eval.Score = Math.Clamp(delayScore * 0.5f + durScore * 0.5f, 0f, 1f);

                    eval.Tier = 1 + (int)Math.Round(eval.Score * 4.0f);
                    eval.IsMaxRoll = delay <= 190 && duration >= 290;
                    eval.Summary = $"Delay: {delay / 60.0f:0.0}s [3.0s-5.0s] | Freeze: {duration / 60.0f:0.0}s [3.0s-5.0s]";
                    break;
                }

                case "GoldenSpur":
                {
                    eval.MaxTier = 5;
                    int duration = r.Next(5, 11); // 5 to 10 seconds
                    eval.Score = (duration - 5) / 5.0f;
                    eval.Tier = duration - 5 + 1; // 1 to 6 mapped to 1..5
                    if (eval.Tier > 5) eval.Tier = 5;
                    eval.IsMaxRoll = duration >= 10;
                    eval.Summary = $"Speed Duration: {duration}s [5s-10s]";
                    break;
                }

                case "ParrotEgg":
                {
                    eval.MaxTier = 4;
                    int level = r.Next(1, 5); // 1 to 4
                    eval.Tier = level;
                    eval.Score = (level - 1) / 3.0f;
                    eval.IsMaxRoll = level == 4;
                    eval.Summary = $"Level {level}/4 ({level * 10}% Coin Chance)";
                    break;
                }

                case "FrogEgg":
                {
                    eval.MaxTier = 6;
                    int variant = r.Next(0, 6); // 0=Green, 1=Yellow, 2=Red, 3=Blue, 4=Void, 5=Prismatic
                    eval.Tier = variant + 1;
                    eval.Score = variant / 5.0f;
                    eval.IsMaxRoll = variant == 5;

                    string[] variantNames = { "Green", "Yellow", "Red", "Blue", "Void", "Prismatic" };
                    string name = variant >= 0 && variant < variantNames.Length ? variantNames[variant] : "Green";
                    int level = Math.Min(5, variant + 1);
                    float cd = 12.0f - level * 1.0f;
                    eval.Summary = $"{name} Frog (Lvl {level}: {cd:0.0}s CD, {2.5f + level * 0.5f:0.0} Reach)";
                    break;
                }

                case "BasiliskPaw":
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

            // Apply selected seed
            trinket.generationSeed.Value = bestSeed;

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
