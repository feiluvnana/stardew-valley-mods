// ModConfig holds every user-adjustable setting for ExtendedDesertFestival.
// SMAPI serializes these public properties to the mod's config.json on exit
// and reloads them at launch, and Generic Mod Config Menu can bind each one
// to a checkbox/slider so players never have to hand-edit JSON.
namespace ExtendedDesertFestival
{
    /// <summary>
    /// Settings controlling which extra seasons host the Calico Desert Festival,
    /// which days it runs, and whether Calico Eggs carry over between seasons.
    /// </summary>
    public class ModConfig
    {
        /// <summary>Run the Desert Festival during Summer as well as Spring.</summary>
        public bool EnableSummer { get; set; } = true;

        /// <summary>Run the Desert Festival during Fall as well as Spring.</summary>
        public bool EnableFall { get; set; } = true;

        /// <summary>Run the Desert Festival during Winter as well as Spring.</summary>
        public bool EnableWinter { get; set; } = true;

        /// <summary>
        /// Keep the player's Calico Eggs (festival currency) when a season ends,
        /// instead of vanilla wiping them, so progress toward shop items persists
        /// across repeated festivals.
        /// </summary>
        public bool KeepEggs { get; set; } = true;

        /// <summary>
        /// Day of the month each extended-season festival starts on
        /// (e.g. 22 means it opens on the 22nd).
        /// </summary>
        public int FestivalStartDay { get; set; } = 22;

        /// <summary>
        /// Day of the month each extended-season festival ends on (inclusive),
        /// paired with FestivalStartDay to define its length.
        /// </summary>
        public int FestivalEndDay { get; set; } = 24;
    }
}
