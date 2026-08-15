namespace BetterForge
{
    public class ModConfig
    {
        // Weapon & Tool Enchantment Options
        public bool UniformEnchantmentChances { get; set; } = true;
        public bool RandomizeEnchantmentSeed { get; set; } = true;

        // Trinket Reforging & Anvil Options
        public bool PreventDowngrades { get; set; } = true;
        public int IridiumBarCost { get; set; } = 3;
        public bool ShowReforgeSuccessMessage { get; set; } = true;
    }
}
