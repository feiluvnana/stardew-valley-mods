namespace BetterChest
{
    public class ModConfig
    {
        // =========================================================================
        // === 1. SKULL CAVERN CHEST SETTINGS                                    ===
        // =========================================================================
        public bool EnableCustomRewards { get; set; } = true;
        public bool ExcludeCosmetics { get; set; } = true;
        public bool EnableDepthScaling { get; set; } = true;
        public bool ScaleLegendaryByDepth { get; set; } = true;

        // Decaying Multi-Rolls (1st and 2nd roll guaranteed 100%, each next roll has decreasing chance - Max 8 rolls)
        public int MaxRolls { get; set; } = 8;
        public float Roll2Chance { get; set; } = 1.00f;
        public float Roll3Chance { get; set; } = 0.80f;
        public float Roll4Chance { get; set; } = 0.65f;
        public float Roll5Chance { get; set; } = 0.50f;
        public float Roll6Chance { get; set; } = 0.35f;
        public float Roll7Chance { get; set; } = 0.20f;
        public float Roll8Chance { get; set; } = 0.10f;

        // Stack Multipliers (Jackpot critical procs on stackable items - Regular Chests, Expected 1.5x Multiplier)
        public float DoubleStackChance { get; set; } = 0.15f;
        public float TripleStackChance { get; set; } = 0.10f;
        public float QuadrupleStackChance { get; set; } = 0.05f;
        public float QuintupleStackChance { get; set; } = 0.0f;

        // Floor 100 Special Chest Buff Settings (Expected ~5.0 items, Expected 3.0x Multiplier)
        public bool EnableFloor100Buff { get; set; } = true;
        public bool Floor100AllCategoriesEqual { get; set; } = true;
        public int Floor100MaxRolls { get; set; } = 12;
        public float Floor100Roll2Chance { get; set; } = 0.94f;
        public float Floor100Roll3Chance { get; set; } = 0.91f;
        public float Floor100Roll4Chance { get; set; } = 0.86f;
        public float Floor100Roll5Chance { get; set; } = 0.79f;
        public float Floor100Roll6Chance { get; set; } = 0.71f;
        public float Floor100Roll7Chance { get; set; } = 0.63f;
        public float Floor100Roll8Chance { get; set; } = 0.53f;
        public float Floor100Roll9Chance { get; set; } = 0.42f;
        public float Floor100Roll10Chance { get; set; } = 0.30f;
        public float Floor100Roll11Chance { get; set; } = 0.18f;
        public float Floor100Roll12Chance { get; set; } = 0.05f;
        public float Floor100DoubleStackChance { get; set; } = 0.20f;
        public float Floor100TripleStackChance { get; set; } = 0.25f;
        public float Floor100QuadrupleStackChance { get; set; } = 0.10f;
        public float Floor100QuintupleStackChance { get; set; } = 0.05f;

        // Category Weights (Equal 15.0 each, ~14.285% chance per category across all 7 categories)
        public double LegendaryWeight { get; set; } = 15.0;
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
        public bool EnableAutoPetter { get; set; } = true;
        public bool EnableGalaxySoul { get; set; } = true;
        public bool EnablePrizeTicket { get; set; } = true;
        public bool EnableStardropTea { get; set; } = true;
        public bool EnableFertilizers { get; set; } = true;
        public bool EnableSprinklers { get; set; } = true;
        public bool EnableRareSeeds { get; set; } = true;
        public bool EnableRadioactiveItems { get; set; } = true;
        public bool EnableIridiumItems { get; set; } = true;
        public bool EnableCoal { get; set; } = true;
        public bool EnableHardwood { get; set; } = true;
        public bool EnableBombs { get; set; } = true;
        public bool EnableFishingTackle { get; set; } = true;
        public bool EnableSlimeEggs { get; set; } = true;
        public bool EnableCombatConsumables { get; set; } = true;
        public bool EnableMysteryBoxes { get; set; } = true;
        public bool EnableArtifactTroves { get; set; } = true;
        public bool EnableOmniGeodes { get; set; } = true;
        public bool EnableCalicoEggs { get; set; } = true;

        // =========================================================================
        // === 2. PROGRESSION & GATEKEEPING SETTINGS                             ===
        // =========================================================================
        public bool GatekeepMasteryItems { get; set; } = true;
        public bool GatekeepIslandItems { get; set; } = true;
        public bool GatekeepQiItems { get; set; } = true;
        public bool GatekeepMysteryBoxes { get; set; } = true;
        public bool GatekeepCalicoEggs { get; set; } = true;
        public bool GatekeepRadioactiveItems { get; set; } = true;
        public bool GatekeepAutoPetter { get; set; } = false;

        // =========================================================================
        // === 3. FISHING TREASURE CHEST SETTINGS                                ===
        // =========================================================================
        public bool EnableFishingChestBuff { get; set; } = true;
        public int FishingChestMinRolls { get; set; } = 3;
        public int FishingChestMaxRolls { get; set; } = 5;
        public int GoldenChestMinRolls { get; set; } = 5;
        public int GoldenChestMaxRolls { get; set; } = 8;
        public bool EnableFishingTrashRerollBonus { get; set; } = true;
    }
}