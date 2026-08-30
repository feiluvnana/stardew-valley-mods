namespace BetterAnimal
{
    /// <summary>
    /// Configuration settings for the BetterAnimal mod.
    /// Fully customizable via Generic Mod Config Menu (GMCM).
    /// </summary>
    public sealed class ModConfig
    {
        // =====================================================================
        // === 1. DUCK REBALANCE SETTINGS                                    ===
        // =====================================================================
        /// <summary>Whether high-friendship ducks drop both a Duck Feather and a standard Duck Egg (dual harvest).</summary>
        public bool EnableDuckDualDrop { get; set; } = true;

        /// <summary>Minimum friendship hearts required for the duck dual drop mechanic (default: 4 hearts).</summary>
        public int DuckDualDropMinHearts { get; set; } = 4;

        /// <summary>Probability of dual drop when duck reaches 5 hearts (default: 1.0 = 100% guarantee).</summary>
        public float DuckDualDropChance { get; set; } = 1.0f;

        /// <summary>Whether Duck Feathers can be processed in the Loom into luxury Down Cloth.</summary>
        public bool EnableDuckFeatherLoom { get; set; } = true;

        // =====================================================================
        // === 2. RABBIT PRODUCTIVITY & MULTI-DROP SETTINGS                  ===
        // =====================================================================
        /// <summary>Whether rabbit produce cooldown is reduced from vanilla 4 days.</summary>
        public bool EnableRabbitCooldownReduction { get; set; } = true;

        /// <summary>Days required for a rabbit to produce wool or rabbit's foot (default: 2 days, matching ducks and goats).</summary>
        public int RabbitDaysToProduce { get; set; } = 2;

        /// <summary>Whether high-friendship rabbits can drop multiple items (bonus wool or lucky foot).</summary>
        public bool EnableRabbitMultiDrop { get; set; } = true;

        /// <summary>Chance of a multi-drop when a rabbit has >= 3 friendship hearts (default: 0.35 / 35%).</summary>
        public float RabbitMultiDropChance { get; set; } = 0.35f;

        // =====================================================================
        // === 3. SHEEP & WOOL SETTINGS                                      ===
        // =====================================================================
        /// <summary>Whether sheep produce wool every single day when reaching 5 friendship hearts.</summary>
        public bool EnableSheepDailyShearAtMaxHearts { get; set; } = true;

        // =====================================================================
        // === 4. DINOSAUR SETTINGS                                          ===
        // =====================================================================
        /// <summary>Whether dinosaur egg produce cooldown is reduced from vanilla 7 days.</summary>
        public bool EnableDinosaurCooldownReduction { get; set; } = true;

        /// <summary>Days required for a dinosaur to lay an egg (default: 3 days).</summary>
        public int DinosaurDaysToProduce { get; set; } = 3;

        /// <summary>Whether high-friendship dinosaurs have a chance to lay a bonus second egg.</summary>
        public bool EnableDinosaurMultiDrop { get; set; } = true;

        /// <summary>Minimum friendship hearts required for dinosaur bonus egg drop (default: 4 hearts).</summary>
        public int DinosaurMultiDropMinHearts { get; set; } = 4;

        /// <summary>Chance of a bonus egg drop when dinosaur reaches min hearts (default: 0.25 / 25%).</summary>
        public float DinosaurMultiDropChance { get; set; } = 0.25f;

        // =====================================================================
        // === 5. GOAT SETTINGS                                              ===
        // =====================================================================
        /// <summary>Whether high-friendship goats have a chance to produce bonus milk on harvest.</summary>
        public bool EnableGoatMultiDrop { get; set; } = true;

        /// <summary>Minimum friendship hearts required for goat bonus milk drop (default: 4 hearts).</summary>
        public int GoatMultiDropMinHearts { get; set; } = 4;

        /// <summary>Chance of bonus milk when goat reaches min hearts (default: 0.35 / 35%).</summary>
        public float GoatMultiDropChance { get; set; } = 0.35f;

        // =====================================================================
        // === 6. VOID CHICKEN SETTINGS                                      ===
        // =====================================================================
        /// <summary>Whether high-friendship void chickens have a chance to lay a bonus second void egg.</summary>
        public bool EnableVoidChickenMultiDrop { get; set; } = true;

        /// <summary>Minimum friendship hearts required for void chicken bonus egg drop (default: 4 hearts).</summary>
        public int VoidChickenMultiDropMinHearts { get; set; } = 4;

        /// <summary>Chance of bonus egg when void chicken reaches min hearts (default: 0.25 / 25%).</summary>
        public float VoidChickenMultiDropChance { get; set; } = 0.25f;

        // =====================================================================
        // === 7. SLIME HUTCH & RANCHING SETTINGS                            ===
        // =====================================================================
        /// <summary>Whether Slime Hutch daily slime ball capacity and harvesting yields are enhanced.</summary>
        public bool EnableSlimeRanchingBalancing { get; set; } = true;

        /// <summary>Maximum daily Slime Balls produced in a populated Slime Hutch (default: 6, vanilla is 4).</summary>
        public int SlimeHutchMaxBalls { get; set; } = 6;

        /// <summary>Whether the Slime Egg-Press has a chance to produce a bonus second slime egg.</summary>
        public bool EnableSlimeEggPressMultiYield { get; set; } = true;

        /// <summary>Chance that the Slime Egg-Press produces 2x Slime Eggs (default: 0.25 / 25%).</summary>
        public float SlimeEggPressDoubleChance { get; set; } = 0.25f;
    }
}
