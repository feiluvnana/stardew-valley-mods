namespace BetterGeodeCracking
{
    public class ModConfig
    {
        /// <summary>Whether geode cracking at Clint's is completely free (0g).</summary>
        public bool FreeCracking { get; set; } = false;

        /// <summary>Custom price per geode if FreeCracking is false.</summary>
        public int CrackingPrice { get; set; } = 25;

        /// <summary>Whether geode cracking is instantaneous (skips Clint's hammering animation). Defaults to false so Clint's animation is on.</summary>
        public bool InstantCracking { get; set; } = false;

        /// <summary>Maximum number of geodes to crack in a single bulk action (999 = full stack).</summary>
        public int BulkBatchSize { get; set; } = 999;

        /// <summary>Whether to display a dedicated 'Crack All' button in the Geode Menu.</summary>
        public bool ShowCrackAllButton { get; set; } = true;

        /// <summary>Whether to show a summary toast notification after bulk cracking.</summary>
        public bool ShowSummaryToast { get; set; } = true;

        /// <summary>Whether Geode Crusher machines can process Mystery Boxes, Golden Mystery Boxes, Artifact Troves, and Golden Coconuts.</summary>
        public bool AllowSpecialGeodesInCrusher { get; set; } = true;

        /// <summary>Whether Geode Crusher machines on the farm crack geodes instantly.</summary>
        public bool InstantGeodeCrusher { get; set; } = true;

        /// <summary>Whether Geode Crusher machines require coal.</summary>
        public bool GeodeCrusherRequiresCoal { get; set; } = false;
    }
}
