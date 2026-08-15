namespace BetterMap
{
    public class ModConfig
    {
        /// <summary>Whether to remove the driftwood fence barrier and log piles on Ginger Island Farm (Island West).</summary>
        public bool RemoveFarmDriftwoodBarrier { get; set; } = true;

        /// <summary>Whether to remove the wreckage, debris, and driftwood along the transition between Island South (beach) and Island West (farm).</summary>
        public bool RemoveBeachFarmWreck { get; set; } = true;

        /// <summary>Whether to remove the large shipwreck at the southern beach of Island West.</summary>
        public bool RemoveIslandWestShipwreck { get; set; } = true;
    }
}
