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
        /// <summary>Whether mead retains the input flower honey type and 2.0x price scaling.</summary>
        public bool EnableMeadFix { get; set; } = true;

        /// <summary>Whether artisan machines (Keg, Preserves Jar, Cheese Press, Mayonnaise Machine, Loom, Oil Maker) retain input item quality.</summary>
        public bool EnableQualityPreserving { get; set; } = true;

        /// <summary>Whether Truffle Oil price and quality scale with the input Truffle.</summary>
        public bool EnableTruffleOilFix { get; set; } = true;

        /// <summary>Price multiplier for Truffle Oil relative to the input Truffle value (default 1.5x).</summary>
        public float TruffleOilMultiplier { get; set; } = 1.5f;

        /// <summary>Whether Vegetable Juice sell price is buffed with an enhanced multiplier.</summary>
        public bool EnableJuiceBuff { get; set; } = true;

        /// <summary>Price multiplier for Vegetable Juice relative to the base vegetable price (default 2.75x, vanilla 2.25x).</summary>
        public float JuiceMultiplier { get; set; } = 2.75f;

        /// <summary>Whether Casks can age additional artisan goods such as Vegetable Juice.</summary>
        public bool EnableExpandedAging { get; set; } = true;

        // ---------------- Fruit Tree Automation ----------------
        /// <summary>Whether fruit automatically falls to the ground when a mature fruit tree reaches the configured fruit count.</summary>
        public bool EnableAutoFruitDrop { get; set; } = true;

        /// <summary>Number of fruit on a tree that triggers the auto-drop (default 3, the vanilla maximum).</summary>
        public int MaxFruitsBeforeDrop { get; set; } = 3;
    }
}

