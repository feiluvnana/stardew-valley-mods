// ModConfig holds every user-adjustable setting for BetterEvent.
// SMAPI serializes these public properties to the mod's config.json on exit
// and reloads them at launch, and Generic Mod Config Menu can bind each one
// to a checkbox/slider so players never have to hand-edit JSON.
namespace BetterEvent
{
    /// <summary>
    /// Settings controlling which extra seasons host the Calico Desert Festival,
    /// which days it runs, and whether Calico Eggs carry over between seasons.
    /// </summary>
    /// <remarks>
    /// C# REFRESHER: every member below is an auto-property — `{ get; set; }`
    /// generates a hidden backing field plus accessor code automatically, and
    /// the `= value` part supplies the default used before any config.json
    /// exists. Types used here: `bool` stores true/false, `int` stores whole
    /// numbers (Stardew months are always exactly 28 days).
    /// </remarks>
    public class ModConfig
    {
        /// <summary>Run the Desert Festival during Summer as well as Spring.</summary>
        // The vanilla festival is Spring 15-17; this toggle adds a Summer edition.
        public bool EnableSummer { get; set; } = true;

        /// <summary>Run the Desert Festival during Fall as well as Spring.</summary>
        // Adds a Fall edition when true.
        public bool EnableFall { get; set; } = true;

        /// <summary>Run the Desert Festival during Winter as well as Spring.</summary>
        // Adds a Winter edition when true.
        public bool EnableWinter { get; set; } = true;

        /// <summary>
        /// Keep the player's Calico Eggs (festival currency) when a season ends,
        /// instead of vanilla wiping them, so progress toward shop items persists
        /// across repeated festivals.
        /// </summary>
        // Vanilla queues "remove CalicoEgg overnight" once a festival ends;
        // ModEntry deletes that queued removal while this flag is on.
        public bool KeepEggs { get; set; } = true;

        /// <summary>
        /// Day of the month each extended-season festival starts on
        /// (e.g. 15 means it opens on the 15th).
        /// </summary>
        // Whole number 1..28; ModEntry clamps user input into that range
        // with Math.Clamp so an invalid value can never reach the game data.
        public int FestivalStartDay { get; set; } = 15;

        /// <summary>
        /// Day of the month each extended-season festival ends on (inclusive),
        /// paired with FestivalStartDay to define its length.
        /// </summary>
        // Default 15..17 mirrors vanilla's three-day Spring layout;
        // "inclusive" means the end day itself is still part of the festival.
        public int FestivalEndDay { get; set; } = 17;
    }
}
