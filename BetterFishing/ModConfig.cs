namespace BetterFishing
{
    /// <summary>
    /// Configuration options for the BetterFishing mod.
    /// Supports in-game customization via Generic Mod Config Menu (GMCM).
    /// </summary>
    public sealed class ModConfig
    {
        // =====================================================================
        // === 1. FISH PRICE BALANCING SETTINGS                              ===
        // =====================================================================
        /// <summary>Enable dynamic difficulty-based fish price scaling.</summary>
        public bool EnableFishPriceBalancing { get; set; } = true;

        /// <summary>Base price floor for all rod-caught fish.</summary>
        public float BaseFloor { get; set; } = 20.0f;

        /// <summary>Linear scaling factor multiplied by difficulty (D).</summary>
        public float LinearFactor { get; set; } = 0.80f;

        /// <summary>Quadratic factor for mid-tier difficulty curve ((D/50)^2).</summary>
        public float MidTierFactor { get; set; } = 25.0f;

        /// <summary>Apex scaling factor for high-tier difficulty curve above D=50.</summary>
        public float ApexFactor { get; set; } = 0.9293f;

        /// <summary>Apex power exponent for high-tier difficulty curve.</summary>
        public float ApexExponent { get; set; } = 4.34f;

        // =====================================================================
        // === 2. MOVEMENT BEHAVIOR BONUSES                                  ===
        // =====================================================================
        /// <summary>Bonus multiplier for smooth movement fish (default: +2%).</summary>
        public float SmoothMovementBonus { get; set; } = 0.02f;

        /// <summary>Bonus multiplier for mixed movement fish (default: +3%).</summary>
        public float MixedMovementBonus { get; set; } = 0.03f;

        /// <summary>Bonus multiplier for floater movement fish (default: +4%).</summary>
        public float FloaterMovementBonus { get; set; } = 0.04f;

        /// <summary>Bonus multiplier for sinker movement fish (default: +5%).</summary>
        public float SinkerMovementBonus { get; set; } = 0.05f;

        /// <summary>Bonus multiplier for dart movement fish (default: +6%).</summary>
        public float DartMovementBonus { get; set; } = 0.06f;

        // =====================================================================
        // === 3. ENVIRONMENTAL & LOCATION TRAIT BONUSES                     ===
        // =====================================================================
        /// <summary>Bonus multiplier for rain-only fish catches (default: +2%).</summary>
        public float RainConditionBonus { get; set; } = 0.02f;

        /// <summary>Bonus multiplier for night catches or tight time windows (&lt;= 6 hrs, default: +2%).</summary>
        public float NightWindowConditionBonus { get; set; } = 0.02f;

        /// <summary>Bonus multiplier for single-season exclusive fish (default: +2%).</summary>
        public float SingleSeasonConditionBonus { get; set; } = 0.02f;

        /// <summary>Bonus multiplier for small / isolated locations (Mines, Swamp, Desert, Submarine, etc., default: +2%).</summary>
        public float IsolatedLocationBonus { get; set; } = 0.02f;

        // =====================================================================
        // === 4. LEGENDARY & SIGNATURE BONUSES                              ===
        // =====================================================================
        /// <summary>Dedicated prize multiplier bonus for all Legendary fish (default: 0.00f; Legend anchored at 5,000g).</summary>
        public float LegendaryFishMultiplierBonus { get; set; } = 0.00f;

        /// <summary>Enable deterministic species hash bonus (0% to +8% based on ItemId).</summary>
        public bool EnablePredictableHashBonus { get; set; } = true;

        /// <summary>Protect vanilla fish from being priced lower than their vanilla base price.</summary>
        public bool PreventNerf { get; set; } = true;

        /// <summary>Rounding interval for final evaluated fish prices (e.g. 5 rounds to nearest 5g).</summary>
        public int PriceRoundingInterval { get; set; } = 5;

        // =====================================================================
        // === 5. FISHING TREASURE CHEST SETTINGS                            ===
        // =====================================================================
        /// <summary>Enable the decaying-roll enhancement for fishing treasure chests.</summary>
        public bool EnableFishingChestBuff { get; set; } = true;

        /// <summary>Probability decay multiplier for regular fishing treasure chest rolls (vanilla default is 0.40; mod default is 0.45).</summary>
        public float FishingChestDecayRate { get; set; } = 0.45f;

        /// <summary>Probability decay multiplier for 1.6 golden fishing treasure chest rolls (vanilla default is 0.60; mod default is 0.60).</summary>
        public float GoldenChestDecayRate { get; set; } = 0.60f;

        // =====================================================================
        // === 6. FISHING EXPERIENCE (EXP) SETTINGS                          ===
        // =====================================================================
        /// <summary>Enable targeted fishing experience balancing for apex and legendary fish.</summary>
        public bool EnableFishingExpBalancing { get; set; } = true;

        /// <summary>Experience bonus added to challenging/apex fish catches (Difficulty >= 85, default: +15 EXP).</summary>
        public int ApexFishExpBonus { get; set; } = 15;

        /// <summary>Experience bonus added to Legendary fish catches (default: +60 EXP).</summary>
        public int LegendaryFishExpBonus { get; set; } = 60;

        // =====================================================================
        // === 7. CRAB POT OVERHAUL SETTINGS                                 ===
        // =====================================================================
        /// <summary>Enable rebalanced base sell prices for crab pot catches.</summary>
        public bool EnableCrabPotPriceBalancing { get; set; } = true;

        /// <summary>Enable tiered harvest experience for crab pots.</summary>
        public bool EnableCrabPotExpBalancing { get; set; } = true;

        /// <summary>Enable trash chance reduction (rerolls trash into valid catches).</summary>
        public bool EnableCrabPotTrashReduction { get; set; } = true;

        /// <summary>Probability of converting a trash roll into a valid shellfish catch (default: 0.65 / 65%).</summary>
        public float CrabPotTrashRerollChance { get; set; } = 0.65f;

        public int LobsterPrice { get; set; } = 200;
        public int CrabPrice { get; set; } = 150;
        public int CrayfishPrice { get; set; } = 110;
        public int SnailPrice { get; set; } = 95;
        public int OysterPrice { get; set; } = 95;
        public int ShrimpPrice { get; set; } = 90;
        public int CocklePrice { get; set; } = 75;
        public int ClamPrice { get; set; } = 75;
        public int MusselPrice { get; set; } = 55;
        public int PeriwinklePrice { get; set; } = 45;

        public int LobsterExp { get; set; } = 24;
        public int CrabExp { get; set; } = 16;
        public int Tier2CrabPotExp { get; set; } = 12;
        public int Tier1CrabPotExp { get; set; } = 8;

        // =====================================================================
        // === 8. FISH POND & AQUACULTURE SETTINGS                           ===
        // =====================================================================
        /// <summary>Enable star quality calculation (Silver, Gold, Iridium) for Fish Pond outputs based on population tier and luck.</summary>
        public bool EnableFishPondQuality { get; set; } = true;

        /// <summary>Enable Caviar base sell price rebalancing in Data/Objects.</summary>
        public bool EnableCaviarRebalance { get; set; } = true;

        /// <summary>Base sell price for Caviar (default: 800g; 1,120g Artisan / 2,240g Iridium Artisan).</summary>
        public int CaviarBasePrice { get; set; } = 800;
    }
}
