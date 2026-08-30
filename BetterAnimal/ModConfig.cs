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

        /// <summary>Whether Rabbit's Foot base sell price is rebalanced in Data/Objects.</summary>
        public bool EnableRabbitFootRebalance { get; set; } = true;

        /// <summary>Base sell price for Rabbit's Foot (default: 850g; 1,020g Rancher / 1,700g Iridium).</summary>
        public int RabbitFootBasePrice { get; set; } = 850;

        // =====================================================================
        // === 3. SHEEP & WOOL SETTINGS                                      ===
        // =====================================================================
        /// <summary>Whether sheep produce wool every single day when reaching 5 friendship hearts.</summary>
        public bool EnableSheepDailyShearAtMaxHearts { get; set; } = true;
    }
}
