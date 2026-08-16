namespace BetterMap
{
    public class ModConfig
    {
        /// <summary>Whether to remove the driftwood fence barrier and log piles across Ginger Island Farm (Island West).</summary>
        public bool RemoveFarmDriftwoodBarrier { get; set; } = true;

        /// <summary>Whether to widen the farmhouse exit doorway to 3x1 (3 tiles wide) across all Farmhouse maps (FarmHouse, FarmHouse1, FarmHouse2, IslandFarmHouse).</summary>
        public bool WidenHouseExit { get; set; } = true;
    }
}
