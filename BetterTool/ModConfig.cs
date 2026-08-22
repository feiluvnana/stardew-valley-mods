namespace BetterTool
{
    public class ModConfig
    {
        public bool EnableAutoHarvest { get; set; } = true;
        public bool EnableAdjacentChestOutput { get; set; } = true;
        public int ProcessIntervalTicks { get; set; } = 60; // 60 ticks = ~1 second
        public bool PlaySoundEffects { get; set; } = true;
        public int HopperCapacity { get; set; } = 36; // 36 or 70
        public bool ServiceCrabPots { get; set; } = true;
        public bool ServiceCasks { get; set; } = true;
    }
}
