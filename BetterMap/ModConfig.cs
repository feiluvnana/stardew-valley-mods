// ModConfig lists BetterMap's user settings. SMAPI serializes this object to the mod
// folder's config.json automatically (saved on quit, loaded on startup), and ModEntry
// registers these same properties with Generic Mod Config Menu so they can be toggled
// in-game without hand-editing JSON.
namespace BetterMap
{
    /// <summary>
    /// Persisted settings for BetterMap, stored in config.json and editable through
    /// Generic Mod Config Menu. Both options default to enabled.
    /// </summary>
    public class ModConfig
    {
        /// <summary>Whether to remove the driftwood fence barrier and log piles across Ginger Island Farm (Island West).</summary>
        public bool RemoveFarmDriftwoodBarrier { get; set; } = true;

        /// <summary>Whether to widen the farmhouse exit doorway to 3x1 (3 tiles wide) across all Farmhouse maps (FarmHouse, FarmHouse1, FarmHouse2, IslandFarmHouse).</summary>
        public bool WidenHouseExit { get; set; } = true;
    }
}
