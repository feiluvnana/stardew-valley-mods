namespace BetterSkullCavernChest
{
    public class ModConfig
    {
        // General
        public bool EnableCustomRewards { get; set; } = true;
        public bool ExcludeCosmetics { get; set; } = true;

        // Decaying Multi-Rolls (1st roll 100%, each next roll has decreasing chance)
        public int MaxRolls { get; set; } = 5;
        public float Roll2Chance { get; set; } = 0.70f;
        public float Roll3Chance { get; set; } = 0.45f;
        public float Roll4Chance { get; set; } = 0.25f;
        public float Roll5Chance { get; set; } = 0.10f;

        // Stack Multipliers (Jackpot critical procs on stackable items - Regular Chests)
        public float DoubleStackChance { get; set; } = 0.15f;
        public float TripleStackChance { get; set; } = 0.05f;
        public float QuadrupleStackChance { get; set; } = 0.0f;
        public float QuintupleStackChance { get; set; } = 0.0f;

        // Floor 100 Special Chest Buff Settings
        public bool EnableFloor100Buff { get; set; } = true;
        public bool Floor100AllCategoriesEqual { get; set; } = true;
        public int Floor100MaxRolls { get; set; } = 7;
        public float Floor100Roll2Chance { get; set; } = 0.85f;
        public float Floor100Roll3Chance { get; set; } = 0.70f;
        public float Floor100Roll4Chance { get; set; } = 0.55f;
        public float Floor100Roll5Chance { get; set; } = 0.40f;
        public float Floor100Roll6Chance { get; set; } = 0.25f;
        public float Floor100Roll7Chance { get; set; } = 0.15f;
        public float Floor100DoubleStackChance { get; set; } = 0.25f;
        public float Floor100TripleStackChance { get; set; } = 0.15f;
        public float Floor100QuadrupleStackChance { get; set; } = 0.10f;
        public float Floor100QuintupleStackChance { get; set; } = 0.05f;

        // Category Weights (Target: 10% Legendary, 15% each for other 6 categories)
        public double LegendaryWeight { get; set; } = 10.0;
        public double AgricultureWeight { get; set; } = 15.0;
        public double MiningWeight { get; set; } = 15.0;
        public double FishingWeight { get; set; } = 15.0;
        public double CombatWeight { get; set; } = 15.0;
        public double ForagingWeight { get; set; } = 15.0;
        public double LootboxWeight { get; set; } = 15.0;

        // Category Toggles
        public bool EnableLegendaryCategory { get; set; } = true;
        public bool EnableAgricultureCategory { get; set; } = true;
        public bool EnableMiningCategory { get; set; } = true;
        public bool EnableFishingCategory { get; set; } = true;
        public bool EnableCombatCategory { get; set; } = true;
        public bool EnableForagingCategory { get; set; } = true;
        public bool EnableLootboxCategory { get; set; } = true;

        // Item Specific Toggles
        public bool EnablePrismaticShard { get; set; } = true;
        public bool EnableMagicRockCandy { get; set; } = true;
        public bool EnableGoldenAnimalCracker { get; set; } = true;
        public bool EnableGalaxySoul { get; set; } = true;
        public bool EnablePrizeTicket { get; set; } = true;
        public bool EnableStardropTea { get; set; } = true;
        public bool EnableBooks { get; set; } = true;
        public bool EnableFertilizers { get; set; } = true;
        public bool EnableSprinklers { get; set; } = true;
        public bool EnableRareSeeds { get; set; } = true;
        public bool EnableRadioactiveItems { get; set; } = true;
        public bool EnableIridiumItems { get; set; } = true;
        public bool EnableBombs { get; set; } = true;
        public bool EnableFishingTackle { get; set; } = true;
        public bool EnableSlimeEggs { get; set; } = true;
        public bool EnableCombatConsumables { get; set; } = true;
        public bool EnableWarpTotems { get; set; } = true;
        public bool EnableMysteryBoxes { get; set; } = true;
        public bool EnableArtifactTroves { get; set; } = true;
        public bool EnableOmniGeodes { get; set; } = true;
        public bool EnableCalicoEggs { get; set; } = true;
    }
}