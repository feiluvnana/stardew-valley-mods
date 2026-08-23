namespace BetterIndustry
{
    public class ModConfig
    {
        // ---------------- Cooking Balancing ----------------
        /// <summary>Whether cooking dishes are rebalanced to be profitable over raw ingredients.</summary>
        public bool EnableCookingBalancing { get; set; } = true;

        /// <summary>Profit margin multiplier for cooked food over raw ingredients (e.g. 1.25 = +25% profit).</summary>
        public float CookingProfitMargin { get; set; } = 1.25f;

        // ---------------- Artisan Goods Balancing ----------------
        /// <summary>Whether mead retains the input flower honey type and price scaling.</summary>
        public bool EnableMeadFix { get; set; } = true;
    }
}
