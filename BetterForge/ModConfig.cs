// ModConfig defines every user-adjustable setting for BetterForge.
// SMAPI automatically serializes these public properties to the mod's
// config.json file when the game closes, and reads them back on launch,
// so each property below becomes a real option players can tweak
// (either by editing config.json or through Generic Mod Config Menu).
namespace BetterForge
{
    /// <summary>
    /// All BetterForge settings. SMAPI saves this object as config.json and
    /// Generic Mod Config Menu binds each property to a menu control.
    /// </summary>
    public class ModConfig
    {
        // Weapon & Tool Enchantment Options

        /// <summary>
        /// When true, forging with a Prismatic Shard picks among ALL valid
        /// enchantments with an equal 1-in-N chance instead of vanilla's
        /// weighted (uneven) odds.
        /// </summary>
        public bool UniformEnchantmentChances { get; set; } = true;

        /// <summary>
        /// When true, enchant results use the game's shared random generator
        /// (different every time). When false, results are derived from a seed
        /// based on the save file and enchant count, making rolls deterministic.
        /// </summary>
        public bool RandomizeEnchantmentSeed { get; set; } = true;

        // Trinket Reforging & Anvil Options

        /// <summary>
        /// When true, Anvil trinket reforges can never produce a result worse
        /// than the trinket's current roll ("Never Downgrade" guarantee).
        /// </summary>
        public bool PreventDowngrades { get; set; } = true;

        /// <summary>
        /// How many Iridium Bars a trinket reforge at the Anvil costs.
        /// </summary>
        public int IridiumBarCost { get; set; } = 3;

        /// <summary>
        /// Whether to show the on-screen HUD message after a successful
        /// trinket reforge (e.g. "upgraded to tier X" / "PERFECT roll!").
        /// </summary>
        public bool ShowReforgeSuccessMessage { get; set; } = true;
    }
}
