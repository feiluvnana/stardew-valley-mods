// ModConfig defines every user-facing setting for this mod. SMAPI automatically saves
// this object to the mod folder's config.json when the game closes and reloads it on
// startup - you never write serialization code yourself; plain public properties with a
// default value are all that's required. The SAME object is handed to Generic Mod Config
// Menu (see IGenericModConfigMenuApi.cs) so players can edit these values in-game.

// "namespace" groups related classes under one shared prefix so their names can't
// collide with classes from other mods or the game itself.
namespace BetterIndustry
{
    // "public class" = visible to every other piece of code (SMAPI must see it to
    // serialize/deserialize it); without a modifier, members default to "internal".
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
        //   bool -> true/false toggle.  int -> whole number (counts/thresholds).
        //   Every property here also needs a matching registration call in
        //   ModEntry.OnGameLaunched so GMCM knows how to display and edit it.

        // ---------------- Cooking Balancing & Food Quality ----------------
        /// <summary>Whether cooking dishes are rebalanced to be profitable over raw ingredients.</summary>
        public bool EnableCookingBalancing { get; set; } = true;

        /// <summary>Profit margin multiplier for cooked food over raw ingredients (e.g. 1.25 = +25% profit).</summary>
        public float CookingProfitMargin { get; set; } = 1.25f;

        /// <summary>Whether cooked dishes calculate quality (Silver, Gold, Iridium) from ingredients and enhanced Qi Seasoning.</summary>
        public bool EnableFoodQuality { get; set; } = true;

        /// <summary>Ingredient quality selection priority: HighestQuality, LowestQuality, or InventoryOrder.</summary>
        public string IngredientQualityPriority { get; set; } = "HighestQuality";

        /// <summary>Whether high-quality cooked food (especially Iridium) grants enhanced stat buffs (+2) and longer durations.</summary>
        public bool EnableEnhancedFoodBuffs { get; set; } = true;

        /// <summary>Buff duration multiplier for Iridium-quality cooked food and drinks (default 2.0x).</summary>
        public float IridiumBuffDurationMultiplier { get; set; } = 2.0f;

        // ---------------- Artisan Goods Balancing ----------------
        /// <summary>Whether mead retains the input flower honey type and 2.0x price scaling.</summary>
        public bool EnableMeadFix { get; set; } = true;

        /// <summary>Whether artisan machines use the balanced Option 2 Quarter-Step quality matrix (75/25 & 50/25) based on input quality.</summary>
        public bool EnableMachineQuality { get; set; } = true;

        /// <summary>Whether Daily Luck slightly shifts machine quality rolls toward higher star tiers.</summary>
        public bool ApplyDailyLuckToMachines { get; set; } = true;

        /// <summary>Legacy alias for EnableMachineQuality.</summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public bool EnableQualityPreserving
        {
            get => EnableMachineQuality;
            set => EnableMachineQuality = value;
        }

        /// <summary>Whether Truffle Oil price and quality scale with the input Truffle.</summary>
        public bool EnableTruffleOilFix { get; set; } = true;

        /// <summary>Price multiplier for Truffle Oil relative to the input Truffle value (default 1.5x).</summary>
        public float TruffleOilMultiplier { get; set; } = 1.5f;

        /// <summary>Whether Vegetable Juice sell price is buffed with an enhanced multiplier.</summary>
        public bool EnableJuiceBuff { get; set; } = true;

        /// <summary>Price multiplier for Vegetable Juice relative to the base vegetable price (default 2.75x, vanilla 2.25x).</summary>
        public float JuiceMultiplier { get; set; } = 2.75f;

        // Vanilla background: Casks only accept wine, beer, mead, roe and cheese/goat
        // cheese, slowly upgrading them through Silver -> Gold -> Iridium quality in the
        // cellar over in-game seasons.
        /// <summary>Whether Casks can age additional artisan goods such as Vegetable Juice.</summary>
        public bool EnableExpandedAging { get; set; } = true;

        // ---------------- Fruit Tree Automation ----------------
        /// <summary>Whether fruit automatically falls to the ground when a mature fruit tree reaches the configured fruit count.</summary>
        public bool EnableAutoFruitDrop { get; set; } = true;

        /// <summary>Number of fruit on a tree that triggers the auto-drop (default 3, the vanilla maximum).</summary>
        public int MaxFruitsBeforeDrop { get; set; } = 3;
    }
}

