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
    /// <remarks>
    /// C# CONCEPTS ON DISPLAY:
    ///  * A CLASS is a blueprint that bundles related data (these properties)
    ///    and behavior (methods) into one named type.
    ///  * Each setting below is an AUTO-PROPERTY: writing `{ get; set; }`
    ///    makes the compiler secretly generate a hidden backing field plus
    ///    getter/setter code for you.
    ///  * `public` = visible to any other code (SMAPI and GMCM both need it).
    ///  * `bool` = a true/false value.
    ///  * The trailing `= true` is a DEFAULT VALUE used when the player has no
    ///    config.json yet (first launch) — SMAPI then saves it out.
    /// SMAPI maps property names straight to JSON keys, so these exact names
    /// are what players see inside config.json.
    /// </remarks>
    public class ModConfig
    {
        /// <summary>Whether to remove the driftwood fence barrier and log piles across Ginger Island Farm (Island West).</summary>
        // Auto-property: { get; set; } means the value is readable AND writable.
        // `= true` is the factory default for a fresh install.
        public bool RemoveFarmDriftwoodBarrier { get; set; } = true;

        /// <summary>Whether to widen the farmhouse exit doorway to 3x1 (3 tiles wide) across all Farmhouse maps (FarmHouse, FarmHouse1, FarmHouse2, IslandFarmHouse).</summary>
        // Same pattern, second toggle. MapPatcher checks this flag before
        // touching any tiles, so unchecking here disables the whole feature.
        public bool WidenHouseExit { get; set; } = true;
    }
}
