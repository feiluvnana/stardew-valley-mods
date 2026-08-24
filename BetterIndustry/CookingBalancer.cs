// =====================================================================================
// CookingBalancer.cs - makes cooked DISHES reliably profitable.
//
// It reads every recipe from the game's "Data/CookingRecipes" asset (raw '/'-separated
// text lines), adds up the sell value of each dish's ingredients, and raises the dish's
// sell Price in the "Data/Objects" asset so it earns Config.CookingProfitMargin over
// ingredient cost. Prices are only ever RAISED, never lowered, so dishes that are
// already profitable in vanilla stay untouched.
// =====================================================================================
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;

namespace BetterIndustry
{
    /// <summary>
    /// Rewrites dish prices in the "Data/Objects" asset based on ingredient costs taken
    /// from the "Data/CookingRecipes" asset, enforcing a configurable profit margin
    /// (+25% by default).
    /// </summary>
    // static class refresher: cannot be instantiated with "new"; just a container of
    // functions whose shared state lives in ModEntry's static properties.
    public static class CookingBalancer
    {
        // Expression-bodied read-only getters forwarding to ModEntry's shared config and
        // logger objects (see ModEntry.cs for how those are populated at startup).
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        /// <summary>
        /// SMAPI asset hook: whenever the game loads "Data/Objects" (the master list of
        /// every item's data, including sell Price), queue an edit applying the balancing.
        /// Subscribed once from ModEntry.Entry().
        /// </summary>
        /// <param name="sender">Event source supplied by SMAPI (unused).</param>
        /// <param name="e">Names the loading asset and offers editing helpers.</param>
        public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // Honour the config toggle first: when disabled, never touch the asset.
            if (!Config.EnableCookingBalancing)
                return;

            // React only to the one asset we care about ("Data/Objects").
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                // Register an editor lambda executed during the load. AsDictionary exposes
                // the asset as itemId -> ObjectData so entries can be modified in place;
                // AssetEditPriority.Late runs us after most other mods' edits.
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    ApplyCookingBalancing(data);
                }, AssetEditPriority.Late);
            }
        }

        /// <summary>
        /// For every cooking recipe: sums the ingredient sell prices, divides by how many
        /// dishes one craft produces, applies the profit margin, and raises the dish's
        /// Price when it is currently below that target.
        /// </summary>
        /// <param name="objectData">The live Data/Objects dictionary (item id -> item data).</param>
        private static void ApplyCookingBalancing(IDictionary<string, ObjectData> objectData)
        {
            // Any problem (missing asset, malformed recipe...) is caught and logged rather
            // than breaking the whole asset load.
            try
            {
                // Load the recipe list ourselves. Each entry's VALUE is a raw text string
                // roughly shaped "<ingredients>/<flags>/<yield item> <count>/<display name>",
                // e.g. "244 1/0/226 1/Fried Egg".
                var cookingRecipes = ModEntry.ModHelper.GameContent.Load<Dictionary<string, string>>("Data/CookingRecipes");
                if (cookingRecipes == null)
                    return;

                // Loop over all recipes: recipeName is the internal key ("Fried Egg") and
                // recipeStr the packed definition string shown above. The parenthesised
                // "(recipeName, recipeStr)" deconstruction unpacks each KeyValuePair.
                foreach (var (recipeName, recipeStr) in cookingRecipes)
                {
                    // Split('/') cuts the definition into segments at every slash.
                    string[] parts = recipeStr.Split('/');
                    if (parts.Length < 3)
                        continue;   // Malformed/shorter-than-expected entry - skip safely.

                    // parts[0] lists ingredients as SPACE-separated PAIRS:
                    // "<itemId> <amount> <itemId> <amount> ...".
                    string[] ingredientsRaw = parts[0].Split(' ');
                    int totalIngredientCost = 0;

                    // Walk the array two slots at a time (i = id slot, i+1 = count slot).
                    for (int i = 0; i < ingredientsRaw.Length; i += 2)
                    {
                        // Guard against an odd trailing token (defensive coding).
                        if (i + 1 >= ingredientsRaw.Length) break;
                        string ingredientId = ingredientsRaw[i];
                        // Normalise the id by stripping the "(O)" object-category prefix,
                        // so "(O)244" and "244" look up identically in the dictionary.
                        string normId = ingredientId.StartsWith("(O)") ? ingredientId.Substring(3) : ingredientId;

                        // int.TryParse converts text -> number WITHOUT throwing; the "out"
                        // variable receives the parsed value. Unparseable counts fall back
                        // to 1 via this if/negation pattern.
                        if (!int.TryParse(ingredientsRaw[i + 1], out int ingredientCount))
                            ingredientCount = 1;

                        // Try the normalised id first, then the raw form - "||" stops at
                        // the first successful lookup and reuses the same 'ingData' var.
                        if (objectData.TryGetValue(normId, out var ingData) || objectData.TryGetValue(ingredientId, out ingData))
                        {
                            // Real item found: cost contribution = sell price x quantity.
                            totalIngredientCost += ingData.Price * ingredientCount;
                        }
                        // No concrete item? Recipes may demand an entire CATEGORY or context
                        // tag instead ("category_fish", "category_egg", "-80" style ids).
                        else if (ingredientId.StartsWith("-") || ingredientId.StartsWith("category_", StringComparison.OrdinalIgnoreCase) || ingredientId.StartsWith("tag_", StringComparison.OrdinalIgnoreCase))
                        {
                            // Category or context tag ingredient (e.g. category_fish, category_egg), estimate 100g base
                            totalIngredientCost += 100 * ingredientCount;
                        }
                    }

                    // parts[2] describes the RESULT: "<yieldItemId>[ <count>]".
                    string[] yieldParts = parts[2].Split(' ');
                    string yieldId = yieldParts[0];
                    string normYieldId = yieldId.StartsWith("(O)") ? yieldId.Substring(3) : yieldId;
                    int yieldCount = 1;   // Assume one dish unless the string says otherwise.
                    if (yieldParts.Length > 1 && int.TryParse(yieldParts[1], out int parsedYield))
                    {
                        // Math.Max(1, ...) clamps nonsense like 0 or negatives back to 1.
                        yieldCount = Math.Max(1, parsedYield);
                    }

                    // Look up the dish itself in Data/Objects (normalised id first, raw second).
                    if (objectData.TryGetValue(normYieldId, out var dish) || objectData.TryGetValue(yieldId, out dish))
                    {
                        // Profit margin balancing per unit produced
                        if (totalIngredientCost > 0)
                        {
                            // Cost per dish = total cost / dishes produced. Casting to double
                            // BEFORE dividing keeps the decimals (integer division would
                            // truncate); Math.Ceiling rounds UP to the next whole gold, and
                            // the leading (int) cast stores it as a whole number.
                            int targetPrice = (int)Math.Ceiling(((double)totalIngredientCost / yieldCount) * Config.CookingProfitMargin);
                            // Raise-only policy: never nerf a dish that already sells higher.
                            if (dish.Price < targetPrice)
                            {
                                dish.Price = targetPrice;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the full exception (message + stack trace) for debugging mod conflicts.
                Monitor.Log($"Error applying cooking balance: {ex}", LogLevel.Error);
            }
        }
    }
}
