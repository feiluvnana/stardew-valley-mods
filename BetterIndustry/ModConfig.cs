namespace BetterIndustry
{
    public class ModConfig
    {
        // ---------------- Cooking Balancing ----------------
        /// <summary>Whether cooking dishes are rebalanced to be profitable over raw ingredients.</summary>
        public bool EnableCookingBalancing { get; set; } = true;

        /// <summary>Profit margin multiplier for cooked food over raw ingredients (e.g. 1.25 = +25% profit).</summary>
        public float CookingProfitMargin { get; set; } = 1.25f;

        /// <summary>Whether to boost stamina and health restored by cooked food.</summary>
        public bool EnableEnergyBuff { get; set; } = true;

        /// <summary>Energy and health recovery multiplier for cooked dishes.</summary>
        public float EnergyMultiplier { get; set; } = 1.25f;

        /// <summary>Whether to extend the duration of stat buffs provided by cooked food.</summary>
        public bool EnableBuffDurationBoost { get; set; } = true;

        /// <summary>Buff duration multiplier for food buffs (1.5 = +50% duration).</summary>
        public float BuffDurationMultiplier { get; set; } = 1.5f;

        // ---------------- Artisan Goods Balancing ----------------
        /// <summary>Whether mead retains the input flower honey type and price scaling.</summary>
        public bool EnableMeadFix { get; set; } = true;

        /// <summary>Multiplier applied to flower honey price when brewed into mead.</summary>
        public float MeadMultiplier { get; set; } = 1.5f;

        /// <summary>Whether vegetable juice selling prices are buffed.</summary>
        public bool EnableJuiceBuff { get; set; } = true;

        /// <summary>Base vegetable multiplier for vegetable juice in kegs.</summary>
        public float JuiceMultiplier { get; set; } = 3.0f;

        /// <summary>Whether pickled vegetables in preserves jars are buffed.</summary>
        public bool EnablePickleBuff { get; set; } = true;

        /// <summary>Base multiplier for pickled goods in preserves jars.</summary>
        public float PickleMultiplier { get; set; } = 2.5f;

        /// <summary>Whether aged roe and caviar prices are buffed.</summary>
        public bool EnableRoeBuff { get; set; } = true;

        /// <summary>Base multiplier for aged roe in preserves jars.</summary>
        public float AgedRoeMultiplier { get; set; } = 2.5f;

        /// <summary>Flat base selling price for Caviar.</summary>
        public int CaviarPrice { get; set; } = 750;

        // ---------------- Automation (Hopper & Machines) ----------------
        /// <summary>Whether hoppers automatically pull/harvest finished products from adjacent machines (North, South, West, East).</summary>
        public bool EnableAutoHarvest { get; set; } = true;

        /// <summary>Whether hoppers automatically transfer harvested items into adjacent regular chests or mini-shipping bins.</summary>
        public bool EnableChestOutputTransfer { get; set; } = true;

        /// <summary>Whether hoppers periodically process machines in the background without requiring player interaction.</summary>
        public bool EnablePeriodicProcessing { get; set; } = true;

        /// <summary>Frequency in game ticks between background processing checks (60 ticks = 1 real second).</summary>
        public int ProcessIntervalTicks { get; set; } = 60;

        /// <summary>Whether hoppers can auto-bait crab pots and harvest catches from them.</summary>
        public bool EnableCrabPotService { get; set; } = true;

        /// <summary>Whether hoppers can load ingredients into casks and harvest finished aged products from them.</summary>
        public bool EnableCaskService { get; set; } = true;

        /// <summary>Storage capacity of the hopper (36 slots vanilla, or 70 slots expanded like Big Chest).</summary>
        public int HopperCapacity { get; set; } = 36;

        /// <summary>Whether to play sound effects when items are automatically loaded, harvested, or transferred.</summary>
        public bool PlaySoundEffects { get; set; } = true;
    }
}
