namespace BetterTrinket
{
    public class ModConfig
    {
        public bool PreventDowngrades { get; set; } = true;
        public bool EnablePitySystem { get; set; } = true;
        public int RollsForGuaranteedUpgrade { get; set; } = 3;
        public int IridiumBarCost { get; set; } = 3;
        public bool ShowStatRangesInTooltips { get; set; } = true;
        public bool ShowReforgeSuccessMessage { get; set; } = true;
    }
}
