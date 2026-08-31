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
        /// <summary>Whether mead retains the input flower honey type and price scaling.</summary>
        public bool EnableMeadFix { get; set; } = true;

        /// <summary>Price multiplier for Flower Honey Mead relative to input honey (default: 1.35x).</summary>
        public float FlowerMeadMultiplier { get; set; } = 1.35f;

        /// <summary>Whether artisan machines use the balanced 60/25/15 quality matrix (Normal, Silver, Gold, 0% Iridium) based on input quality.</summary>
        public bool EnableMachineQuality { get; set; } = true;

        /// <summary>Whether Truffle Oil price and quality scale with the input Truffle.</summary>
        public bool EnableTruffleOilFix { get; set; } = true;

        /// <summary>Price multiplier for Truffle Oil relative to base Truffle value (default 1.5x -> 937g base / 1,967g Gold Artisan).</summary>
        public float TruffleOilMultiplier { get; set; } = 1.5f;

        /// <summary>Whether Cooking Oil receives the Artisan Goods category (-26) for the +40% Artisan profession bonus.</summary>
        public bool EnableCookingOilArtisanCategory { get; set; } = true;

        /// <summary>Whether Vegetable Juice sell price is buffed with an enhanced multiplier.</summary>
        public bool EnableJuiceBuff { get; set; } = true;

        /// <summary>Price multiplier for Vegetable Juice relative to the base vegetable price (default 2.75x, vanilla 2.25x).</summary>
        public float JuiceMultiplier { get; set; } = 2.75f;

        // Vanilla background: Casks only accept wine, beer, mead, roe and cheese/goat
        // cheese, slowly upgrading them through Silver -> Gold -> Iridium quality in the
        // cellar over in-game seasons.
        /// <summary>Whether Casks can age additional artisan goods such as Vegetable Juice.</summary>
        public bool EnableExpandedAging { get; set; } = true;

        // ---------------- Fruit Tree Balancing & Automation ----------------
        /// <summary>Whether fruit automatically falls to the ground when a mature fruit tree reaches the configured fruit count.</summary>
        public bool EnableAutoFruitDrop { get; set; } = true;

        /// <summary>Number of fruit on a tree that triggers the auto-drop (default 3, the vanilla maximum).</summary>
        public int MaxFruitsBeforeDrop { get; set; } = 3;

        /// <summary>Whether fruit tree fruit base prices are rebalanced for guaranteed positive Year-1 ROI.</summary>
        public bool EnableFruitTreeRebalance { get; set; } = true;

        // ---------------- Minerals & Monster Loot Balancing ----------------
        /// <summary>Whether the 41 geode minerals and 4 foraged minerals are rebalanced for consistent cracking profits.</summary>
        public bool EnableMineralPriceRebalance { get; set; } = true;

        /// <summary>Whether mid/late game monster loot (Solar/Void Essence, Squid Ink, Bone Fragment) sell prices are rebalanced.</summary>
        public bool EnableMonsterLootRebalance { get; set; } = true;

        // ---------------- Tree Tapper Productivity ----------------
        /// <summary>Whether Tree Tappers have multi-harvest yield chances (2x/3x syrups).</summary>
        public bool EnableTapperMultiYield { get; set; } = true;

        /// <summary>Chance of double syrup harvest from Standard Tappers (default: 0.35 / 35%).</summary>
        public float StandardTapperDoubleChance { get; set; } = 0.35f;

        /// <summary>Chance of triple syrup harvest from Heavy Tappers (default: 0.20 / 20%, 100% 2x is guaranteed).</summary>
        public float HeavyTapperTripleChance { get; set; } = 0.20f;

        // ---------------- Artisanal Milling ----------------
        /// <summary>Whether milled goods (Wheat Flour, Sugar, Rice) have their base prices rebalanced.</summary>
        public bool EnableMillBalancing { get; set; } = true;

        /// <summary>Whether milled goods receive the Artisan Goods category (-26) for the +40% Artisan profession bonus.</summary>
        public bool EnableMillArtisanCategory { get; set; } = true;

        /// <summary>Whether the Mill preserves grain quality into output goods via the 60/25/15 matrix.</summary>
        public bool EnableMillQualityMatrix { get; set; } = true;

        /// <summary>Base sell price for Wheat Flour (default 90g).</summary>
        public int WheatFlourBasePrice { get; set; } = 90;

        /// <summary>Base sell price for Sugar (vanilla default 50g).</summary>
        public int SugarBasePrice { get; set; } = 50;

        /// <summary>Base sell price for Rice (default 140g).</summary>
        public int RiceBasePrice { get; set; } = 140;
    }
}

