namespace BetterProduct
{
    public class ModConfig
    {
        public bool EnableCookingBalancing { get; set; } = true;
        public float CookingProfitMargin { get; set; } = 1.25f;
        public bool EnableEnergyBuff { get; set; } = true;
        public float EnergyMultiplier { get; set; } = 1.25f;
        public bool EnableBuffDurationBoost { get; set; } = true;
        public float BuffDurationMultiplier { get; set; } = 1.5f;
        public bool EnableMeadFix { get; set; } = true;
        public float MeadMultiplier { get; set; } = 1.5f;
        public bool EnableJuiceBuff { get; set; } = true;
        public float JuiceMultiplier { get; set; } = 3.0f;
        public bool EnablePickleBuff { get; set; } = true;
        public float PickleMultiplier { get; set; } = 2.5f;
        public bool EnableRoeBuff { get; set; } = true;
        public float AgedRoeMultiplier { get; set; } = 2.5f;
        public int CaviarPrice { get; set; } = 750;

        // Dehydrator Settings
        public bool EnableDehydratorBalancing { get; set; } = true;
        public bool AllowMixedQualityDehydrating { get; set; } = true;
        public bool EnableDriedQualityScaling { get; set; } = true;
        public float DehydratorSpeedMultiplier { get; set; } = 1.0f;
        public bool AllowVegetableDehydrating { get; set; } = true;
        public bool AllowFlowerDehydrating { get; set; } = false;
    }
}