using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;

// CropHelper inspects a tilled-soil tile (and the crop growing in it) and packs up
// everything a hover tooltip needs: what's planted, its icon, water/fertilizer state,
// and exactly how many in-game nights remain until harvest.
namespace BetterQOL
{
    /// <summary>
    /// Data container describing one soil tile's crop for tooltip display. Built fresh
    /// by CropHelper.GetCropInfo every time the player hovers a tile.
    /// </summary>
    public class CropInfo
    {
        /// <summary>Display name of the item this crop yields (e.g. "Parsnip").</summary>
        public string CropName { get; set; } = string.Empty;
        /// <summary>Texture atlas containing the crop's icon (null if it couldn't load).</summary>
        public Texture2D? IconTexture { get; set; }
        /// <summary>Pick-region within that atlas identifying the specific icon image.</summary>
        public Rectangle? IconSourceRect { get; set; }
        /// <summary>True if the crop withered (e.g. survived past its season's last day).</summary>
        public bool IsDead { get; set; }
        /// <summary>True when the crop can be harvested right now.</summary>
        public bool IsReadyToHarvest { get; set; }
        /// <summary>Nights of growth still needed (0 = ready today).</summary>
        public int DaysRemaining { get; set; }
        /// <summary>Current visual growth stage, counted 1-based for humans.</summary>
        public int CurrentStage { get; set; }
        /// <summary>Total number of growth stages the crop passes through.</summary>
        public int TotalStages { get; set; }
        /// <summary>True for multi-harvest crops (blueberries, cranberries...) that regrow after picking.</summary>
        public bool IsRegrowable { get; set; }
        /// <summary>Nights a regrowable crop needs to produce again after harvest.</summary>
        public int RegrowDays { get; set; }
        /// <summary>True if the soil was watered today (or the crop self-waters as a paddy).</summary>
        public bool IsWatered { get; set; }
        /// <summary>All fertilizers joined into one comma-separated display string, or null if none.</summary>
        public string? FertilizerName { get; set; }
        /// <summary>Fertilizers as a parsed list of display names. "= new()" is target-typed shorthand for "new List&lt;string&gt;()".</summary>
        public List<string> FertilizerNames { get; set; } = new();
        /// <summary>Computed convenience property: "=>" makes it evaluate on every read instead of storing a value.</summary>
        public bool IsFertilized => FertilizerNames.Count > 0;
        /// <summary>True for paddy crops (rice) that drink from adjacent water tiles.</summary>
        public bool IsPaddyCrop { get; set; }
        /// <summary>True when a paddy crop currently qualifies as watered via nearby water.</summary>
        public bool IsPaddyWatered { get; set; }
        /// <summary>True when the tile is just bare tilled soil (or an empty Garden Pot) with no crop.</summary>
        public bool IsHoeDirtOnly { get; set; }
    }

    /// <summary>
    /// Static helper (no instances - call methods on the class name) that reads a soil
    /// tile and packs everything a hover tooltip needs into a CropInfo record.
    /// </summary>
    public static class CropHelper
    {
        /// <summary>
        /// Turns the raw fertilizer string stored on a soil tile into human-readable
        /// display names. Vanilla stores one id, but fertilizer mods may stack several,
        /// joined with "|" characters.
        /// </summary>
        /// <param name="fertilizerRaw">Raw id string from HoeDirt.fertilizer (may be null/empty).</param>
        /// <returns>Display names in first-applied order, with " xN" suffixes for stacked repeats; empty list if none.</returns>
        public static List<string> ParseFertilizerNames(string? fertilizerRaw)
        {
            var results = new List<string>();
            // IsNullOrWhiteSpace catches null, "", and whitespace-only strings at once.
            if (string.IsNullOrWhiteSpace(fertilizerRaw))
                return results;

            // Ultimate Fertilizer and multiple fertilizer mods separate IDs by '|'
            // Split('|', ...) chops the string at every pipe. The two options are enum
            // flags combined with bitwise OR: RemoveEmptyEntries drops gaps like "a||b",
            // TrimEntries strips stray spaces around each token.
            var tokens = fertilizerRaw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                return results;

            // Count occurrences to support stacking mode (e.g. Speed-Gro x3)
            // Dictionary<string, int> maps each fertilizer id -> how many times it appeared.
            // The comparer makes keys case-insensitive so "speed-gro" and "Speed-Gro" merge.
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            // Dictionaries don't guarantee iteration order, so this side list remembers
            // the order ids were first seen, keeping tooltip output stable.
            var distinctIds = new List<string>();
            foreach (var token in tokens)
            {
                string id = token;
                if (!counts.ContainsKey(id))
                {
                    // First sighting: start its counter at 0 and record its position.
                    counts[id] = 0;
                    distinctIds.Add(id);
                }
                counts[id]++;
            }

            foreach (var id in distinctIds)
            {
                int count = counts[id];
                // Resolve the pretty display name. "??" retries with the "(O)"-prefixed
                // form ("(O)" = the object item category) for ids saved without it.
                var fertData = ItemRegistry.GetData(id) ?? ItemRegistry.GetData($"(O){id}");
                // First "??" handles a failed lookup entirely; second falls back to the raw id.
                string name = fertData?.DisplayName ?? id;
                // Interpolated string ($"..."): {name} and {count} are filled in at
                // runtime, producing e.g. "Speed-Gro x3".
                if (count > 1)
                {
                    results.Add($"{name} x{count}");
                }
                else
                {
                    results.Add(name);
                }
            }

            return results;
        }

        /// <summary>
        /// Reads one tilled-soil tile - a HoeDirt "terrain feature" (also what fills a
        /// Garden Pot) - reporting water/fertilizer state, the growing crop, its icon,
        /// and how many nights remain until harvest.
        /// </summary>
        /// <param name="hoeDirt">The soil tile under the cursor.</param>
        /// <returns>CropInfo for the tooltip, or null if the input tile was null.</returns>
        public static CropInfo? GetCropInfo(HoeDirt hoeDirt)
        {
            if (hoeDirt == null)
                return null;

            var info = new CropInfo();

            // 1. Water status
            // state is a net-synced int: 0 = dry, 1 = watered (HoeDirt.watered constant).
            info.IsWatered = hoeDirt.state.Value == HoeDirt.watered;

            // 2. Fertilizer status
            string? fertilizerRaw = hoeDirt.fertilizer.Value;
            // Reuse the parser above to turn raw ids into display names...
            info.FertilizerNames = ParseFertilizerNames(fertilizerRaw);
            if (info.FertilizerNames.Count > 0)
            {
                // ...then join them into one comma-separated string for a single tooltip line.
                info.FertilizerName = string.Join(", ", info.FertilizerNames);
            }

            Crop? crop = hoeDirt.crop;
            if (crop == null)
            {
                // Bare tilled soil or empty garden pot
                // Nothing planted: mark it and bail out early with a translated label.
                info.IsHoeDirtOnly = true;
                info.CropName = ModEntry.I18n.Get("hover.dirt.tilled");
                return info;
            }

            // Paddy crops (like rice) hydrate themselves while planted beside water,
            // so the game checks surrounding tiles instead of requiring watering can use.
            info.IsPaddyCrop = crop.isPaddyCrop();
            if (info.IsPaddyCrop)
            {
                info.IsPaddyWatered = hoeDirt.paddyWaterCheck();
                if (info.IsPaddyWatered)
                {
                    // Treat a hydrated paddy as watered for the tooltip.
                    info.IsWatered = true;
                }
            }

            // 3. Dead crop check
            // Crops wither when their season ends (e.g. summer crops on Fall day 1).
            if (crop.dead.Value)
            {
                info.IsDead = true;
                info.CropName = ModEntry.I18n.Get("hover.crop.dead");
                return info;
            }

            // 4. Crop identity & Icon
            // indexOfHarvest is the item id this crop yields (e.g. "(O)24" = parsnip).
            string harvestId = crop.indexOfHarvest.Value;
            ParsedItemData? harvestData = null;
            if (!string.IsNullOrEmpty(harvestId))
            {
                // Same "(O)"-prefix fallback trick as elsewhere for modded bare ids.
                harvestData = ItemRegistry.GetData(harvestId) ?? ItemRegistry.GetData($"(O){harvestId}");
            }

            if (harvestData != null)
            {
                info.CropName = harvestData.DisplayName;
                try
                {
                    // Grab the icon: which texture atlas to draw from, and which
                    // rectangle inside that atlas holds this item's picture.
                    info.IconTexture = harvestData.GetTexture();
                    info.IconSourceRect = harvestData.GetSourceRect();
                }
                catch
                {
                    // Fallback if texture cannot be loaded
                }
            }
            else
            {
                // Unknown/unresolvable id: show a generic translated label instead.
                info.CropName = ModEntry.I18n.Get("hover.crop.generic");
            }

            // 5. Growth stages & Days remaining
            // phaseDays is a list where entry i = nights the crop must spend in stage i.
            // The LAST entry is special (it covers time spent at the finished stage), so
            // the count of real "growing" stages is list length minus one.
            int phaseCount = crop.phaseDays.Count;
            info.TotalStages = Math.Max(1, phaseCount > 0 ? phaseCount - 1 : 1);
            // +1 converts the game's 0-based phase into a 1-based stage number for
            // display; Min stops it from overshooting the final stage.
            info.CurrentStage = Math.Min(crop.currentPhase.Value + 1, info.TotalStages);

            // GetData() fetches this crop's row from the game's Data/Crops asset.
            // "?." short-circuits to null if it fails, then "??" substitutes -1 as an
            // "unknown" marker. RegrowDays > 0 means the crop regrows after each harvest
            // (blueberries, cranberries, grapes...).
            int regrow = crop.GetData()?.RegrowDays ?? -1;
            info.IsRegrowable = regrow > 0;
            info.RegrowDays = Math.Max(0, regrow);

            // fullyGrown is set on regrowable crops after their first harvest: the crop
            // parks in its final phase while dayOfCurrentPhase counts down regrow nights.
            if (crop.fullyGrown.Value)
            {
                if (crop.dayOfCurrentPhase.Value <= 0)
                {
                    info.IsReadyToHarvest = true;
                    info.DaysRemaining = 0;
                }
                else
                {
                    info.IsReadyToHarvest = false;
                    // Still regrowing: however many nights remain on the countdown.
                    info.DaysRemaining = Math.Max(0, crop.dayOfCurrentPhase.Value);
                }
            }
            else
            {
                // Already in the final stage -> pickable right now.
                if (crop.currentPhase.Value >= info.TotalStages)
                {
                    info.IsReadyToHarvest = true;
                    info.DaysRemaining = 0;
                }
                else if (crop.currentPhase.Value < crop.phaseDays.Count)
                {
                    // Nights still owed in the CURRENT stage: stage length minus nights served.
                    int currentPhaseRemaining = Math.Max(0, crop.phaseDays[crop.currentPhase.Value] - crop.dayOfCurrentPhase.Value);
                    int remainingPhasesSum = 0;

                    // Add up every FUTURE growing stage. The loop deliberately stops before
                    // the last list entry (Count - 1): a crop becomes harvestable the moment
                    // it ENTERS the final stage, so that entry adds no extra waiting.
                    for (int i = crop.currentPhase.Value + 1; i < crop.phaseDays.Count - 1; i++)
                    {
                        remainingPhasesSum += crop.phaseDays[i];
                    }

                    // Floor at 0 so rounding quirks can never show negative days.
                    info.DaysRemaining = Math.Max(0, currentPhaseRemaining + remainingPhasesSum);
                    info.IsReadyToHarvest = info.DaysRemaining <= 0;
                }
            }

            return info;
        }
    }
}
