using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace BetterFishing
{
    /// <summary>
    /// Handles dynamic difficulty-based fish price scaling and trait bonuses in Data/Objects.
    /// Operates at AssetEditPriority.Late to ensure full compatibility with the Price Catalogue
    /// power book, item tooltips, derived artisan goods, and other mods.
    /// </summary>
    public static class FishPriceBalancer
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// Known item IDs of Legendary and Qi Extended Family fish.
        /// </summary>
        public static readonly HashSet<string> LegendaryFishIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "159", // Crimsonfish
            "898", // Son of Crimsonfish
            "160", // Angler
            "899", // Ms. Angler
            "163", // Legend
            "900", // Legend II
            "775", // Glacierfish
            "901", // Glacierfish Jr.
            "682", // Mutant Carp
            "902"  // Radioactive Carp
        };

        /// <summary>
        /// Determines whether a given fish ID represents a Legendary or Extended Family fish.
        /// </summary>
        public static bool IsLegendaryFish(string? fishId, string? fishName = null)
        {
            if (string.IsNullOrWhiteSpace(fishId))
                return false;

            string cleanId = fishId.StartsWith("(O)") ? fishId[3..] : fishId;
            if (LegendaryFishIds.Contains(cleanId))
                return true;

            if (!string.IsNullOrWhiteSpace(fishName))
            {
                return fishName.Contains("Legend", StringComparison.OrdinalIgnoreCase) ||
                       fishName.Contains("Crimsonfish", StringComparison.OrdinalIgnoreCase) ||
                       fishName.Contains("Glacierfish", StringComparison.OrdinalIgnoreCase) ||
                       fishName.Contains("Angler", StringComparison.OrdinalIgnoreCase) ||
                       fishName.Contains("Mutant Carp", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        /// <summary>
        /// Known item IDs of fish exclusive to small, isolated, or specialized sub-locations.
        /// (Secret Woods, Mines floors 20/60/100, Witch's Swamp, Sewers, Desert, Submarine, Pirate Cove, Caldera).
        /// </summary>
        private static readonly HashSet<string> IsolatedLocationFishIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "156", // Ghostfish (Mines 20/60)
            "158", // Stonefish (Mines 20)
            "161", // Ice Pip (Mines 60)
            "162", // Lava Eel (Mines 100 / Caldera)
            "164", // Sandfish (Desert)
            "165", // Scorpion Carp (Desert)
            "682", // Mutant Carp (Sewers)
            "902", // Radioactive Carp (Sewers)
            "734", // Woodskip (Secret Woods)
            "795", // Void Salmon (Witch's Swamp)
            "798", // Midnight Squid (Submarine)
            "799", // Spookfish (Submarine)
            "800", // Blobfish (Submarine)
            "836", // Blue Discus (Ginger Island Pond/River)
            "837", // Lionfish (Ginger Island Ocean)
            "838"  // Stingray (Pirate Cove)
        };

        /// <summary>
        /// Asset requested hook: intercepts Data/Objects and adjusts fish prices.
        /// </summary>
        public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (!Config.EnableFishPriceBalancing)
                return;

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    ApplyFishPriceBalancing(data);
                }, AssetEditPriority.Late);
            }
        }

        /// <summary>
        /// Iterates over Data/Fish to calculate balanced prices based on difficulty, movement,
        /// weather, spawn constraints, location, and legendary status.
        /// </summary>
        private static void ApplyFishPriceBalancing(IDictionary<string, ObjectData> objectData)
        {
            try
            {
                var fishData = ModEntry.ModHelper.GameContent.Load<Dictionary<string, string>>("Data/Fish");
                if (fishData == null || fishData.Count == 0)
                    return;

                int modifiedCount = 0;

                foreach (var (rawFishId, fishStr) in fishData)
                {
                    string cleanFishId = rawFishId.StartsWith("(O)") ? rawFishId[3..] : rawFishId;
                    string[] parts = fishStr.Split('/');

                    // Skip malformed entries or crab pot trap catches (trap catches have "trap" at index 1)
                    if (parts.Length < 7)
                        continue;

                    string difficultyStr = parts[1];
                    if (difficultyStr.Equals("trap", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!int.TryParse(difficultyStr, out int difficulty) || difficulty <= 0)
                        continue;

                    string movementType = parts.Length > 2 ? parts[2].ToLowerInvariant().Trim() : "mixed";
                    string spawnTimes = parts.Length > 5 ? parts[5] : "";
                    string seasons = parts.Length > 6 ? parts[6].ToLowerInvariant().Trim() : "";
                    string weather = parts.Length > 7 ? parts[7].ToLowerInvariant().Trim() : "both";

                    // 1. Calculate Base Difficulty Price Curve:
                    // P_base(D) = BaseFloor + LinearFactor*D + MidTierFactor*(D/50)^2 + ApexFactor * max(0, (D-50)/10)^ApexExponent
                    float dNorm = difficulty / 50.0f;
                    float basePrice = Config.BaseFloor + (Config.LinearFactor * difficulty) + (Config.MidTierFactor * dNorm * dNorm);

                    if (difficulty > 50)
                    {
                        float apexBase = (difficulty - 50) / 10.0f;
                        basePrice += Config.ApexFactor * (float)Math.Pow(apexBase, Config.ApexExponent);
                    }

                    // 2. Calculate Movement & Trait Multiplier:
                    float traitMultiplier = 1.0f;

                    // A. Movement behavior bonus
                    switch (movementType)
                    {
                        case "smooth":
                            traitMultiplier += Config.SmoothMovementBonus;
                            break;
                        case "floater":
                            traitMultiplier += Config.FloaterMovementBonus;
                            break;
                        case "sinker":
                            traitMultiplier += Config.SinkerMovementBonus;
                            break;
                        case "dart":
                            traitMultiplier += Config.DartMovementBonus;
                            break;
                        case "mixed":
                        default:
                            traitMultiplier += Config.MixedMovementBonus;
                            break;
                    }

                    // B. Environmental constraints
                    // Rain-only
                    if (weather.Equals("rainy", StringComparison.OrdinalIgnoreCase))
                    {
                        traitMultiplier += Config.RainConditionBonus;
                    }

                    // Night catch or tight time window (<= 6 hours total availability)
                    if (IsTightOrNightWindow(spawnTimes))
                    {
                        traitMultiplier += Config.NightWindowConditionBonus;
                    }

                    // Single season only
                    if (IsSingleSeasonOnly(seasons))
                    {
                        traitMultiplier += Config.SingleSeasonConditionBonus;
                    }

                    // Small / Isolated location
                    if (IsolatedLocationFishIds.Contains(cleanFishId) || IsolatedLocationFishIds.Contains(parts[0]))
                    {
                        traitMultiplier += Config.IsolatedLocationBonus;
                    }

                    // C. Legendary status bonus
                    bool isLegendary = LegendaryFishIds.Contains(cleanFishId) ||
                                       (parts.Length > 0 && parts[0].Contains("Legend", StringComparison.OrdinalIgnoreCase)) ||
                                       (parts.Length > 0 && parts[0].Contains("Crimsonfish", StringComparison.OrdinalIgnoreCase)) ||
                                       (parts.Length > 0 && parts[0].Contains("Glacierfish", StringComparison.OrdinalIgnoreCase)) ||
                                       (parts.Length > 0 && parts[0].Contains("Angler", StringComparison.OrdinalIgnoreCase));

                    if (isLegendary)
                    {
                        traitMultiplier += Config.LegendaryFishMultiplierBonus;
                    }

                    // D. Predictable deterministic species hash bonus (0% to +8%)
                    if (Config.EnablePredictableHashBonus && !isLegendary)
                    {
                        int hashVal = Math.Abs(cleanFishId.GetHashCode()) % 9;
                        traitMultiplier += hashVal * 0.01f;
                    }

                    // 3. Final Evaluated Price & Rounding:
                    float rawEvaluatedPrice = basePrice * traitMultiplier;
                    int rounding = Math.Max(1, Config.PriceRoundingInterval);
                    int finalPrice = (int)(Math.Round(rawEvaluatedPrice / rounding) * rounding);
                    if (finalPrice < 5)
                        finalPrice = 5;

                    // Apply to Data/Objects dictionary
                    if (objectData.TryGetValue(cleanFishId, out var objData) || objectData.TryGetValue(rawFishId, out objData))
                    {
                        int originalPrice = objData.Price;
                        if (Config.PreventNerf && finalPrice < originalPrice)
                        {
                            finalPrice = originalPrice;
                        }

                        if (objData.Price != finalPrice)
                        {
                            objData.Price = finalPrice;
                            modifiedCount++;
                        }
                    }
                }

                // Apply Crab Pot price balancing
                ApplyCrabPotPrices(objectData);

                // Apply Caviar price balancing
                ApplyCaviarPrice(objectData);

                Monitor.Log($"BetterFishing: Evaluated and updated prices for {modifiedCount} fish species, crab pot catches & caviar.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error applying fish price balancing in BetterFishing: {ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Applies rebalanced base sell price for Caviar (Item ID 445).
        /// </summary>
        private static void ApplyCaviarPrice(IDictionary<string, ObjectData> objectData)
        {
            if (!Config.EnableCaviarRebalance)
                return;

            if (objectData.TryGetValue("445", out var objData) || objectData.TryGetValue("(O)445", out objData))
            {
                if (Config.PreventNerf && Config.CaviarBasePrice < objData.Price)
                    return;

                objData.Price = Config.CaviarBasePrice;
            }
        }

        /// <summary>
        /// Applies rebalanced base sell prices for the 10 Crab Pot catches.
        /// </summary>
        private static void ApplyCrabPotPrices(IDictionary<string, ObjectData> objectData)
        {
            if (!Config.EnableCrabPotPriceBalancing)
                return;

            var crabPotPrices = new Dictionary<string, int>
            {
                ["715"] = Config.LobsterPrice,
                ["717"] = Config.CrabPrice,
                ["716"] = Config.CrayfishPrice,
                ["721"] = Config.SnailPrice,
                ["723"] = Config.OysterPrice,
                ["720"] = Config.ShrimpPrice,
                ["718"] = Config.CocklePrice,
                ["372"] = Config.ClamPrice,
                ["719"] = Config.MusselPrice,
                ["722"] = Config.PeriwinklePrice
            };

            foreach (var (itemId, price) in crabPotPrices)
            {
                if (objectData.TryGetValue(itemId, out var objData) || objectData.TryGetValue($"(O){itemId}", out objData))
                {
                    if (Config.PreventNerf && price < objData.Price)
                        continue;

                    objData.Price = price;
                }
            }
        }

        /// <summary>
        /// Checks whether a fish's spawn times represent a tight window (<= 6 hours) or a night catch (starts >= 1800).
        /// </summary>
        private static bool IsTightOrNightWindow(string spawnTimes)
        {
            if (string.IsNullOrWhiteSpace(spawnTimes))
                return false;

            string[] tokens = spawnTimes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2)
                return false;

            int totalHours = 0;
            bool startsAtNight = false;

            for (int i = 0; i < tokens.Length; i += 2)
            {
                if (i + 1 >= tokens.Length)
                    break;

                if (int.TryParse(tokens[i], out int startTime) && int.TryParse(tokens[i + 1], out int endTime))
                {
                    if (startTime >= 1800)
                        startsAtNight = true;

                    int span = endTime - startTime;
                    if (span > 0)
                        totalHours += span;
                }
            }

            return startsAtNight || (totalHours > 0 && totalHours <= 600);
        }

        /// <summary>
        /// Checks whether a fish is only available during a single specific season.
        /// </summary>
        private static bool IsSingleSeasonOnly(string seasons)
        {
            if (string.IsNullOrWhiteSpace(seasons) || seasons.Equals("all", StringComparison.OrdinalIgnoreCase))
                return false;

            string[] seasonTokens = seasons.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return seasonTokens.Length == 1;
        }
    }
}
