namespace BetterMap
{
    public class ModConfig
    {
        /// <summary>Whether to remove the wreckage and obstacles along the transition between Island South (beach) and Island West (farm).</summary>
        public bool RemoveBeachFarmWreck { get; set; } = true;

        /// <summary>Whether to remove the large shipwreck at the southern beach of Island West.</summary>
        public bool RemoveIslandWestShipwreck { get; set; } = true;
    }
}
