namespace BetterTool
{
    public class ModConfig
    {
        /// <summary>Whether hoppers placed below a machine (Y - 1) will automatically pull/harvest finished products.</summary>
        public bool EnableAutoHarvest { get; set; } = true;

        /// <summary>Whether hoppers will automatically pass items downward into a regular chest or mini-shipping bin below (Y + 1).</summary>
        public bool EnableChestOutputTransfer { get; set; } = true;

        /// <summary>Whether hoppers periodically check and process machines in the background without requiring player interaction.</summary>
        public bool EnablePeriodicProcessing { get; set; } = true;

        /// <summary>Frequency in game ticks between background processing checks (60 ticks = 1 real second).</summary>
        public int ProcessIntervalTicks { get; set; } = 60;

        /// <summary>Whether hoppers can auto-bait crab pots below and harvest catches from crab pots above.</summary>
        public bool EnableCrabPotService { get; set; } = true;

        /// <summary>Whether hoppers can load ingredients into casks below and harvest aged products from casks above.</summary>
        public bool EnableCaskService { get; set; } = true;

        /// <summary>Storage capacity of the hopper (36 slots vanilla, or 70 slots expanded like Big Chest).</summary>
        public int HopperCapacity { get; set; } = 36;

        /// <summary>Whether to play sound effects when items are automatically loaded, harvested, or transferred.</summary>
        public bool PlaySoundEffects { get; set; } = true;
    }
}
