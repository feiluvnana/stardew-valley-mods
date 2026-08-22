using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;

namespace BetterIndustry
{
    public static class CookingBalancer
    {
        private static ModConfig Config => ModEntry.Config;
        private static IMonitor Monitor => ModEntry.ModMonitor;

        public static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (!Config.EnableCookingBalancing)
                return;

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    ApplyCookingBalancing(data);
                }, AssetEditPriority.Late);
            }
        }

        private static void ApplyCookingBalancing(IDictionary<string, ObjectData> objectData)
        {
            try
            {
                var cookingRecipes = DataLoader.CookingRecipes(Game1.content);
                if (cookingRecipes == null)
                    return;

                foreach (var (recipeName, recipeStr) in cookingRecipes)
                {
                    string[] parts = recipeStr.Split('/');
                    if (parts.Length < 3)
                        continue;

                    string[] ingredientsRaw = parts[0].Split(' ');
                    int totalIngredientCost = 0;

                    for (int i = 0; i < ingredientsRaw.Length; i += 2)
                    {
                        if (i + 1 >= ingredientsRaw.Length) break;
                        string ingredientId = ingredientsRaw[i];
                        if (!int.TryParse(ingredientsRaw[i + 1], out int ingredientCount))
                            ingredientCount = 1;

                        if (objectData.TryGetValue(ingredientId, out var ingData))
                        {
                            totalIngredientCost += ingData.Price * ingredientCount;
                        }
                        else if (ingredientId.StartsWith("-"))
                        {
                            // Category ingredient, estimate 100g base
                            totalIngredientCost += 100 * ingredientCount;
                        }
                    }

                    string[] yieldParts = parts[2].Split(' ');
                    string yieldId = yieldParts[0];

                    if (objectData.TryGetValue(yieldId, out var dish))
                    {
                        // Profit margin balancing
                        if (totalIngredientCost > 0)
                        {
                            int targetPrice = (int)Math.Ceiling(totalIngredientCost * Config.CookingProfitMargin);
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
                Monitor.Log($"Error applying cooking balance: {ex}", LogLevel.Error);
            }
        }
    }
}
