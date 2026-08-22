namespace BetterQOL
{
    public class ModConfig
    {
        // ---------------- Blacksmith Geode Cracking ----------------
        /// <summary>Whether to skip the cracking animation for instantaneous results in single clicks.</summary>
        public bool InstantCracking { get; set; } = false;

        /// <summary>Whether to show the dedicated 'Crack All' button in Clint's geode menu.</summary>
        public bool ShowCrackAllButton { get; set; } = true;

        /// <summary>Maximum batch size when clicking 'Crack All' or Shift+Clicking on the anvil.</summary>
        public int BulkBatchSize { get; set; } = 999;

        /// <summary>Whether to display a HUD summary toast after bulk cracking.</summary>
        public bool ShowSummaryToast { get; set; } = true;

        // ---------------- Farm Machine Options (Geode Crusher) ----------------

        /// <summary>Whether Geode Crusher machines process instantly (0 minutes).</summary>
        public bool InstantGeodeCrusher { get; set; } = false;

        /// <summary>Whether Geode Crusher machines require 1 Coal to operate.</summary>
        public bool GeodeCrusherRequiresCoal { get; set; } = true;

        // ---------------- Item Stacking Options ----------------
        /// <summary>Maximum stack size limit for normally unstackable items.</summary>
        public int MaxStackSize { get; set; } = 999;

        /// <summary>Allow fishing tackle/bobbers with matching durability to stack.</summary>
        public bool EnableTackleStacking { get; set; } = true;

        /// <summary>Allow identical 1.6 trinkets to stack.</summary>
        public bool EnableTrinketStacking { get; set; } = true;

        /// <summary>Allow furniture and decorations to stack.</summary>
        public bool EnableFurnitureStacking { get; set; } = true;

        /// <summary>Allow identical rings and combined rings to stack.</summary>
        public bool EnableRingStacking { get; set; } = true;

        /// <summary>Allow identical clothing and hats to stack.</summary>
        public bool EnableClothingAndHatStacking { get; set; } = true;

        /// <summary>Allow identical boots to stack.</summary>
        public bool EnableBootsStacking { get; set; } = true;
    }
}
