// ============================================================================
// ModConfig defines every user setting the mod offers. SMAPI automatically
// serializes this class to/from config.json in the mod folder
// (helper.ReadConfig / helper.WriteConfig), and Generic Mod Config Menu
// edits these same properties in-game — each public property becomes one
// saved option with its default value taken from the "= true" style initializers.
// ============================================================================
namespace BetterChest
{
    // C# CONCEPTS USED THROUGHOUT THIS FILE:
    //   * A class is a blueprint bundling related data together; "public" means
    //     every other file (and SMAPI's config serializer) can see it.
    //   * Each "public bool X { get; set; } = true;" line is an AUTO-PROPERTY:
    //     the compiler generates hidden storage plus getter/setter accessors.
    //     The trailing "= true" is the DEFAULT VALUE used for a fresh config.
    //   * Basic types: bool = true/false; int = whole numbers; float and
    //     double = decimal numbers (double is the more precise of the two).
    //   * Probability fields store 0.0-1.0 fractions (0.15f == 15%). The "f"
    //     suffix marks a float literal — bare decimals like 1.00 are double.
    /// <summary>
    /// The mod's configuration model. Property defaults here are what a fresh
    /// config.json is created with; all values are tweakable via GMCM.
    /// </summary>
    public class ModConfig
    {
        // =========================================================================
        // === 1. SKULL CAVERN CHEST SETTINGS                                    ===
        // =========================================================================
        /// <summary>Master switch: replace vanilla Skull Cavern chest loot with the mod's generated loot.</summary>
        public bool EnableCustomRewards { get; set; } = true;
        /// <summary>When custom rewards are off, strip clothing/hats/decor ("cosmetics") from vanilla chests instead.</summary>
        public bool ExcludeCosmetics { get; set; } = true;
        /// <summary>Scale roll counts and legendary odds by how deep in Skull Cavern the chest is.</summary>
        public bool EnableDepthScaling { get; set; } = true;
        /// <summary>Grow the Legendary category weight linearly with depth (10% at floor 1 up to 100% at floor 100).</summary>
        public bool ScaleLegendaryByDepth { get; set; } = true;

        // Decaying Multi-Rolls (1st and 2nd roll guaranteed 100%, each next roll has decreasing chance - Max 8 rolls)
        /// <summary>Upper limit of loot rolls per standard deep-floor (50+) chest.</summary>
        public int MaxRolls { get; set; } = 8;
        /// <summary>Chance that a 2nd roll occurs after the first (part of the decaying roll chain).</summary>
        public float Roll2Chance { get; set; } = 1.00f;
        /// <summary>Chance that a 3rd roll occurs once two rolls have happened.</summary>
        public float Roll3Chance { get; set; } = 0.80f;
        /// <summary>Chance that a 4th roll occurs once three rolls have happened.</summary>
        public float Roll4Chance { get; set; } = 0.65f;
        /// <summary>Chance that a 5th roll occurs once four rolls have happened.</summary>
        public float Roll5Chance { get; set; } = 0.50f;
        /// <summary>Chance that a 6th roll occurs once five rolls have happened.</summary>
        public float Roll6Chance { get; set; } = 0.35f;
        /// <summary>Chance that a 7th roll occurs once six rolls have happened.</summary>
        public float Roll7Chance { get; set; } = 0.20f;
        /// <summary>Chance that an 8th roll occurs once seven rolls have happened.</summary>
        public float Roll8Chance { get; set; } = 0.10f;

        // Stack Multipliers (Jackpot critical procs on stackable items - Regular Chests, Expected 1.5x Multiplier)
        /// <summary>"Jackpot" chance to multiply a rolled item's stack size by 2.</summary>
        public float DoubleStackChance { get; set; } = 0.15f;
        /// <summary>Jackpot chance to multiply a rolled item's stack size by 3.</summary>
        public float TripleStackChance { get; set; } = 0.10f;
        /// <summary>Jackpot chance to multiply a rolled item's stack size by 4.</summary>
        public float QuadrupleStackChance { get; set; } = 0.05f;
        /// <summary>Jackpot chance to multiply a rolled item's stack size by 5.</summary>
        public float QuintupleStackChance { get; set; } = 0.0f;

        // Floor 100 Special Chest Buff Settings (Expected ~5.0 items, Expected 3.0x Multiplier)
        /// <summary>Give milestone floors' special chests (220/320/420/520) supercharged loot rolls.</summary>
        public bool EnableFloor100Buff { get; set; } = true;
        /// <summary>Force all seven loot categories to equal weight for special-chest rolls.</summary>
        public bool Floor100AllCategoriesEqual { get; set; } = true;
        /// <summary>Maximum number of rolls a special floor-100-style chest can contain.</summary>
        public int Floor100MaxRolls { get; set; } = 12;
        /// <summary>Special chest: chance that a 2nd roll occurs (decaying chain).</summary>
        public float Floor100Roll2Chance { get; set; } = 0.94f;
        /// <summary>Special chest: chance that a 3rd roll occurs.</summary>
        public float Floor100Roll3Chance { get; set; } = 0.91f;
        /// <summary>Special chest: chance that a 4th roll occurs.</summary>
        public float Floor100Roll4Chance { get; set; } = 0.86f;
        /// <summary>Special chest: chance that a 5th roll occurs.</summary>
        public float Floor100Roll5Chance { get; set; } = 0.79f;
        /// <summary>Special chest: chance that a 6th roll occurs.</summary>
        public float Floor100Roll6Chance { get; set; } = 0.71f;
        /// <summary>Special chest: chance that a 7th roll occurs.</summary>
        public float Floor100Roll7Chance { get; set; } = 0.63f;
        /// <summary>Special chest: chance that an 8th roll occurs.</summary>
        public float Floor100Roll8Chance { get; set; } = 0.53f;
        /// <summary>Special chest: chance that a 9th roll occurs.</summary>
        public float Floor100Roll9Chance { get; set; } = 0.42f;
        /// <summary>Special chest: chance that a 10th roll occurs.</summary>
        public float Floor100Roll10Chance { get; set; } = 0.30f;
        /// <summary>Special chest: chance that an 11th roll occurs.</summary>
        public float Floor100Roll11Chance { get; set; } = 0.18f;
        /// <summary>Special chest: chance that a 12th roll occurs.</summary>
        public float Floor100Roll12Chance { get; set; } = 0.05f;
        /// <summary>Special chest: jackpot chance for a x2 stack multiplier.</summary>
        public float Floor100DoubleStackChance { get; set; } = 0.20f;
        /// <summary>Special chest: jackpot chance for a x3 stack multiplier.</summary>
        public float Floor100TripleStackChance { get; set; } = 0.25f;
        /// <summary>Special chest: jackpot chance for a x4 stack multiplier.</summary>
        public float Floor100QuadrupleStackChance { get; set; } = 0.10f;
        /// <summary>Special chest: jackpot chance for a x5 stack multiplier.</summary>
        public float Floor100QuintupleStackChance { get; set; } = 0.05f;

        // Category Weights (Equal 15.0 each, ~14.285% chance per category across all 7 categories)
        // NOTE the switch to type "double": doubles store bigger, more precise
        // decimals than floats, and literals WITHOUT an "f" suffix (like 15.0)
        // are doubles in C#. Weights are RELATIVE — only their ratios matter.
        /// <summary>Relative weight of the Legendary category when picking a category roll.</summary>
        public double LegendaryWeight { get; set; } = 15.0;
        /// <summary>Relative weight of the Agriculture category.</summary>
        public double AgricultureWeight { get; set; } = 15.0;
        /// <summary>Relative weight of the Mining category.</summary>
        public double MiningWeight { get; set; } = 15.0;
        /// <summary>Relative weight of the Fishing category.</summary>
        public double FishingWeight { get; set; } = 15.0;
        /// <summary>Relative weight of the Combat category.</summary>
        public double CombatWeight { get; set; } = 15.0;
        /// <summary>Relative weight of the Foraging category.</summary>
        public double ForagingWeight { get; set; } = 15.0;
        /// <summary>Relative weight of the Lootboxes category.</summary>
        public double LootboxWeight { get; set; } = 15.0;

        // Category Toggles
        /// <summary>Allow any Legendary-category items to appear at all.</summary>
        public bool EnableLegendaryCategory { get; set; } = true;
        /// <summary>Allow any Agriculture-category items to appear at all.</summary>
        public bool EnableAgricultureCategory { get; set; } = true;
        /// <summary>Allow any Mining-category items to appear at all.</summary>
        public bool EnableMiningCategory { get; set; } = true;
        /// <summary>Allow any Fishing-category items to appear at all.</summary>
        public bool EnableFishingCategory { get; set; } = true;
        /// <summary>Allow any Combat-category items to appear at all.</summary>
        public bool EnableCombatCategory { get; set; } = true;
        /// <summary>Allow any Foraging-category items to appear at all.</summary>
        public bool EnableForagingCategory { get; set; } = true;
        /// <summary>Allow any Lootbox-category items to appear at all.</summary>
        public bool EnableLootboxCategory { get; set; } = true;

        // Item Specific Toggles
        /// <summary>Prismatic Shards may drop from chests.</summary>
        public bool EnablePrismaticShard { get; set; } = true;
        /// <summary>Magic Rock Candy may drop from chests.</summary>
        public bool EnableMagicRockCandy { get; set; } = true;
        /// <summary>Golden Animal Crackers may drop from chests.</summary>
        public bool EnableGoldenAnimalCracker { get; set; } = true;
        /// <summary>Auto-Petter may drop from chests.</summary>
        public bool EnableAutoPetter { get; set; } = true;
        /// <summary>Galaxy Souls may drop from chests.</summary>
        public bool EnableGalaxySoul { get; set; } = true;
        /// <summary>Prize Tickets may drop from chests.</summary>
        public bool EnablePrizeTicket { get; set; } = true;
        /// <summary>Stardrop Tea may drop from chests.</summary>
        public bool EnableStardropTea { get; set; } = true;
        /// <summary>Fertilizers (Hyper Speed-Gro, Deluxe Fertilizer, etc.) may drop from chests.</summary>
        public bool EnableFertilizers { get; set; } = true;
        /// <summary>Sprinklers and related gear (Iridium Sprinkler, Pressure Nozzle, Enricher) may drop from chests.</summary>
        public bool EnableSprinklers { get; set; } = true;
        /// <summary>Rare/expensive seeds may drop from chests.</summary>
        public bool EnableRareSeeds { get; set; } = true;
        /// <summary>Radioactive ore and bars may drop from chests.</summary>
        public bool EnableRadioactiveItems { get; set; } = true;
        /// <summary>Iridium ore and bars may drop from chests.</summary>
        public bool EnableIridiumItems { get; set; } = true;
        /// <summary>Coal may drop from chests.</summary>
        public bool EnableCoal { get; set; } = true;
        /// <summary>Hardwood may drop from chests.</summary>
        public bool EnableHardwood { get; set; } = true;
        /// <summary>Bombs (Mega Bomb) may drop from chests.</summary>
        public bool EnableBombs { get; set; } = true;
        /// <summary>Baits and tackle may drop from chests.</summary>
        public bool EnableFishingTackle { get; set; } = true;
        /// <summary>Slime eggs may drop from chests.</summary>
        public bool EnableSlimeEggs { get; set; } = true;
        /// <summary>Combat consumables (Life Elixir, Triple Shot Espresso) may drop from chests.</summary>
        public bool EnableCombatConsumables { get; set; } = true;
        /// <summary>Mystery Boxes may drop from chests.</summary>
        public bool EnableMysteryBoxes { get; set; } = true;
        /// <summary>Artifact Troves may drop from chests.</summary>
        public bool EnableArtifactTroves { get; set; } = true;
        /// <summary>Omni Geodes may drop from chests.</summary>
        public bool EnableOmniGeodes { get; set; } = true;
        /// <summary>Calico Eggs may drop from chests.</summary>
        public bool EnableCalicoEggs { get; set; } = true;

        // =========================================================================
        // === 2. PROGRESSION & GATEKEEPING SETTINGS                             ===
        // =========================================================================
        /// <summary>Hide Mastery-gated items until the player has unlocked Mastery.</summary>
        public bool GatekeepMasteryItems { get; set; } = true;
        /// <summary>Hide Ginger Island items until the island is unlocked.</summary>
        public bool GatekeepIslandItems { get; set; } = true;
        /// <summary>Hide Mr. Qi items until Qi's Walnut Room is accessible.</summary>
        public bool GatekeepQiItems { get; set; } = true;
        /// <summary>Hide Mystery Boxes until Qi's mystery box event has occurred.</summary>
        public bool GatekeepMysteryBoxes { get; set; } = true;
        /// <summary>Only offer Calico Eggs during the Desert Festival.</summary>
        public bool GatekeepCalicoEggs { get; set; } = true;
        /// <summary>Hide Radioactive Ore/Bars until their unlock conditions are met.</summary>
        public bool GatekeepRadioactiveItems { get; set; } = true;
        /// <summary>Require the Community Center (or Joja) completion before Auto-Petter can drop.</summary>
        public bool GatekeepAutoPetter { get; set; } = false;
    }
}