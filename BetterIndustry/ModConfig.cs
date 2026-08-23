// ModConfig defines every user-facing setting for this mod. SMAPI automatically saves
// this object to the mod folder's config.json when the game closes and reloads it on
// startup - you never write serialization code yourself; plain public properties with a
// default value are all that's required. The SAME object is handed to Generic Mod Config
// Menu (see IGenericModConfigMenuApi.cs) so players can edit these values in-game.
namespace BetterIndustry
{
    /// <summary>
    /// All configurable options for BetterIndustry, persisted to config.json by SMAPI
    /// and exposed to the in-game settings menu via Generic Mod Config Menu.
    /// </summary>
    public class ModConfig
    {
        // Property cheat-sheet for beginners:
        //   { get; set; }        -> auto-property: the compiler generates the hidden backing field.
        //   "= true"/"= 1.25f"   -> the DEFAULT written to config.json on first launch;
        //                           after that, your edited config.json wins until reset via GMCM.
        //   float literals need an "f" suffix (1.25f): without it, 1.25 is a double,
        //   which won't implicitly fit inside a float property.

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

