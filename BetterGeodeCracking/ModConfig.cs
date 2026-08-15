namespace BetterGeodeCracking
{
    public class ModConfig
    {
        /// <summary>Whether geode cracking at Clint's is completely free (0g).</summary>
        public bool FreeCracking { get; set; } = true;

        /// <summary>Custom price per geode if FreeCracking is false.</summary>
        public int CrackingPrice { get; set; } = 0;

        /// <summary>Whether geode cracking is instantaneous (skips the 2.7-second animation).</summary>
        public bool InstantCracking { get; set; } = true;

        /// <summary>Maximum number of geodes to crack in a single bulk action (999 = full stack).</summary>
        public int BulkBatchSize { get; set; } = 999;

        /// <summary>Whether to display a dedicated 'Crack All' button in the Geode Menu.</summary>
        public bool ShowCrackAllButton { get; set; } = true;

        /// <summary>Whether to show a summary toast notification after bulk cracking.</summary>
        public bool ShowSummaryToast { get; set; } = true;

        /// <summary>Whether Geode Crusher machines on the farm crack geodes instantly.</summary>
        public bool InstantGeodeCrusher { get; set; } = false;

        /// <summary>Whether Geode Crusher machines require coal.</summary>
        public bool GeodeCrusherRequiresCoal { get; set; } = true;
    }
}
