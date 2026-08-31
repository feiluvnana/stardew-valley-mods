// =============================================================================
//  BetterEvent — moves the Calico Desert Festival into Summer/Fall/Winter and
//  stops vanilla from deleting your Calico Eggs between festivals.
//  Techniques shown: editing a DATA asset (a dictionary of game objects),
//  SMAPI game-loop events (SaveLoaded / DayStarted / DayEnding), switch
//  expressions, Math.Clamp, and the Generic Mod Config Menu API.
// =============================================================================
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using Common;
using System;
using System.Linq;

namespace BetterEvent
{
    /// <summary>
    /// SMAPI entry point for BetterEvent: reshapes the Data/PassiveFestivals
    /// asset to match the active season, protects Calico Eggs from the nightly
    /// cleanup lists, and exposes a Generic Mod Config Menu settings page.
    /// </summary>
    public class ModEntry : Mod
    {
        private int _savedCalicoEggCount = 0;
        /// <summary>User settings loaded once from config.json.</summary>
        /// <remarks>
        /// All three members below are `static`: attached to the CLASS rather
        /// than an object, so helper code could use them without any instance
        /// reference. Each is assigned exactly once inside Entry(), which is
        /// why they start as `null!` placeholders.
        /// </remarks>
        public static ModConfig Config { get; private set; } = null!;
        /// <summary>SMAPI's logger, cached statically for convenient access.</summary>
        public static IMonitor ModMonitor { get; private set; } = null!;
        /// <summary>Translation helper that reads strings from the i18n folder.</summary>
        public static ITranslationHelper I18n { get; private set; } = null!;

        /// <summary>One-time initialization: load config, cache services, subscribe events.</summary>
        public override void Entry(IModHelper helper)
        {
            // Load config.json into our settings object (generic type argument).
            Config = helper.ReadConfig<ModConfig>();
            // Cache SMAPI services in our static fields for easy access later.
            ModMonitor = Monitor;
            I18n = helper.Translation;

            // Event wiring: `+=` attaches each handler method to its event:
            //   AssetRequested -> edit game data assets whenever they load
            //   GameLaunched   -> all mods ready, time to register GMCM
            //   SaveLoaded     -> a save file just finished loading
            //   DayStarted     -> a new in-game morning began
            //   DayEnding      -> moments before the day is saved
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
        }

        /// <summary>
        /// Returns true when the Desert Festival should exist during the given
        /// season. Spring is ALWAYS enabled (vanilla behaviour); every other
        /// season honors its own config toggle.
        /// </summary>
        /// <param name="season">The season to test — `Season` is an ENUM, a
        /// fixed list of named constants (Spring/Summer/Fall/Winter).</param>
        public static bool IsSeasonEnabled(Season season)
        {
            // SWITCH EXPRESSION: `x switch { pattern => result, ... }` returns
            // a value per matched arm — a compact replacement for if/else chains.
            return season switch
            {
                // Spring: vanilla festival, always on.
                // Summer/Fall/Winter: each follows its own config toggle.
                // `_`: the "discard" catch-all arm, matches anything else.
                Season.Spring => true,
                Season.Summer => Config.EnableSummer,
                Season.Fall => Config.EnableFall,
                Season.Winter => Config.EnableWinter,
                _ => false
            };
        }

        /// <summary>
        /// Rewrites the Data/PassiveFestivals GAME DATA asset — a dictionary of
        /// festival definitions the game reads to schedule passive festivals —
        /// so the Desert Festival lands in whichever season is current.
        /// </summary>
        /// <remarks>
        /// Unlike BetterMap's map edits, this asset is a DICTIONARY asset:
        /// string keys mapping to PassiveFestivalsData-style record objects.
        /// The edit registered here runs every time the game (re)loads it.
        /// </remarks>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // React only to the passive-festival data table.
            if (e.NameWithoutLocale.IsEquivalentTo("Data/PassiveFestivals"))
            {
                e.Edit(asset =>
                {
                    // Treat the asset as Dictionary<string, PassiveFestivalData>;
                    // `.Data` is the actual, editable dictionary instance.
                    var data = asset.AsDictionary<string, PassiveFestivalData>().Data;
                    // TryGetValue = safe lookup: returns false instead of throwing
                    // when the key is missing. `out var festival` declares a new
                    // variable and fills it with the found entry in one step.
                    if (data.TryGetValue("DesertFestival", out var festival))
                    {
                        // Use the CURRENT season if a world is loaded; otherwise
                        // fall back to Spring. Context.IsWorldReady tells whether
                        // an actual save/world exists right now, and Game1 is
                        // the game's global-state hub class.
                        Season currentSeason = Context.IsWorldReady ? Game1.season : Season.Spring;
                        if (IsSeasonEnabled(currentSeason))
                        {
                            festival.Season = currentSeason;
                            int start = Math.Clamp(Config.FestivalStartDay, 1, 28);
                            int end = Math.Clamp(Config.FestivalEndDay, start, 28);
                            festival.StartDay = start;
                            festival.EndDay = end;
                        }
                        // If the season is disabled by config we leave the entry
                        // alone, so no extra festival is scheduled there at all.
                    }
                });
            }
        }

        /// <summary>
        /// Runs right after a save loads: refreshes the festival asset so its
        /// dates match the season the player loaded INTO (handles mid-season
        /// loads where a stale copy might already be cached).
        /// </summary>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Invalidate cache upon loading save to ensure correct season config for mid-season loads
            Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
        }

        /// <summary>
        /// Morning chores: cancel tonight's queued CalicoEgg deletion (when
        /// KeepEggs is on) and refresh festival dates on the 1st of the month,
        /// because a new month means a new season.
        /// </summary>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // VANILLA BEHAVIOUR: after a festival ends, the game queues CalicoEgg
            // for overnight deletion through this shared farmhand-team list.
            // The `?.` chain ("null-conditional operator") stops safely and
            // yields null if ANY link along the path is missing — no crash.
            if (Config.KeepEggs && Game1.player?.team?.itemsToRemoveOvernight != null)
            {
                // Remove BOTH id styles: the legacy plain id and the modern
                // qualified id, whose "(O)" prefix means category Object —
                // covering either form vanilla happens to use.
                Game1.player.team.itemsToRemoveOvernight.Remove("CalicoEgg");
                Game1.player.team.itemsToRemoveOvernight.Remove("(O)CalicoEgg");
            }

            if (Config.KeepEggs && _savedCalicoEggCount > 0)
            {
                int currentCount = Game1.player.Items
                    .Where(i => i != null && (i.ItemId == "CalicoEgg" || i.QualifiedItemId == "(O)CalicoEgg"))
                    .Sum(i => i.Stack);
                int lost = _savedCalicoEggCount - currentCount;
                if (lost > 0)
                {
                    var eggs = ItemRegistry.Create("(O)CalicoEgg", lost);
                    Game1.player.addItemToInventory(eggs);
                    Monitor.Log($"BetterEvent: Restored {lost} Calico Eggs that were removed overnight.", LogLevel.Info);
                }
                _savedCalicoEggCount = 0;
            }

            // Invalidate cache on 1st day of month (season transition)
            // Note: The primary invalidation happens in OnDayEnding (Day 28). This is kept as a backup.
            // New month = new season: force the festival data to regenerate so
            // the extended festival appears in whichever season just began.
            if (Game1.dayOfMonth == 1)
            {
                Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
            }

            // Morning festival reminders for active seasons (Summer, Fall, Winter, Spring)
            int startDay = Math.Clamp(Config.FestivalStartDay, 1, 28);
            if (IsSeasonEnabled(Game1.season))
            {
                if (Game1.dayOfMonth == startDay - 1)
                {
                    Game1.showGlobalMessage(I18n.Get("hud.festival-tomorrow"));
                }
                else if (Game1.dayOfMonth == startDay)
                {
                    Game1.showGlobalMessage(I18n.Get("hud.festival-today"));
                }
            }
            
            if (startDay == 1 && Game1.dayOfMonth == 28)
            {
                // Check if next season has the festival enabled
                Season nextSeason = Game1.season switch
                {
                    Season.Spring => Season.Summer,
                    Season.Summer => Season.Fall,
                    Season.Fall => Season.Winter,
                    Season.Winter => Season.Spring,
                    _ => Game1.season
                };
                if (IsSeasonEnabled(nextSeason))
                {
                    Game1.showGlobalMessage(I18n.Get("hud.festival-tomorrow"));
                }
            }
        }

        /// <summary>
        /// Last chance before the night is saved: strip the queued egg removals
        /// again, since vanilla may re-add them during end-of-day processing.
        /// </summary>
        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            if (Config.KeepEggs)
            {
                _savedCalicoEggCount = Game1.player.Items
                    .Where(i => i != null && (i.ItemId == "CalicoEgg" || i.QualifiedItemId == "(O)CalicoEgg"))
                    .Sum(i => i.Stack);
            }

            // Same defensive chain and dual-id removal as OnDayStarted above.
            if (Config.KeepEggs && Game1.player?.team?.itemsToRemoveOvernight != null)
            {
                Game1.player.team.itemsToRemoveOvernight.Remove("CalicoEgg");
                Game1.player.team.itemsToRemoveOvernight.Remove("(O)CalicoEgg");
            }
            
            if (Game1.dayOfMonth == 28)
            {
                Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
            }
        }



        /// <summary>
        /// Registers BetterEvent's options with Generic Mod Config Menu after
        /// every mod has finished loading.
        /// </summary>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Fetch GMCM's live object through our mirror interface; null means
            // GMCM isn't installed, so skip silently (no menu, no crash).
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // Page registration: reset restores defaults, save writes config.json.
            configMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    // Fresh defaults in memory...
                    Config = new ModConfig();
                    // ...applied immediately via cache invalidation.
                    Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
                },
                save: () =>
                {
                    // Persist toggles to disk, then apply them right away.
                    Helper.WriteConfig(Config);
                    Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
                }
            );

            // Mod description on Root Page
            configMenu.AddParagraph(ModManifest, () => I18n.Get("mod.description"));

            // Heading for the whole page's option group.
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.festivals")
            );

            // Slider for the festival START day, limited to valid days 1..28.
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.start-day.name"),
                tooltip: () => I18n.Get("config.start-day.tooltip"),
                getValue: () => Config.FestivalStartDay,
                setValue: value =>
                {
                    Config.FestivalStartDay = value;
                    // Invalidate on change so the next asset reload uses the
                    // new date instead of a stale cached table.
                    Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
                },
                min: 1,
                max: 28
            );

            // Slider for the END day; clamping against StartDay happens later
            // inside OnAssetRequested before values reach the game data.
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.end-day.name"),
                tooltip: () => I18n.Get("config.end-day.tooltip"),
                getValue: () => Config.FestivalEndDay,
                setValue: value =>
                {
                    Config.FestivalEndDay = value;
                    Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
                },
                min: 1,
                max: 28
            );

            // Checkbox enabling the Summer edition of the festival.
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-summer.name"),
                tooltip: () => I18n.Get("config.enable-summer.tooltip"),
                getValue: () => Config.EnableSummer,
                setValue: value =>
                {
                    Config.EnableSummer = value;
                    Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
                }
            );

            // Checkbox enabling the Fall edition.
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-fall.name"),
                tooltip: () => I18n.Get("config.enable-fall.tooltip"),
                getValue: () => Config.EnableFall,
                setValue: value =>
                {
                    Config.EnableFall = value;
                    Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
                }
            );

            // Checkbox enabling the Winter edition.
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-winter.name"),
                tooltip: () => I18n.Get("config.enable-winter.tooltip"),
                getValue: () => Config.EnableWinter,
                setValue: value =>
                {
                    Config.EnableWinter = value;
                    Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
                }
            );

            // Checkbox for keeping Calico Eggs across seasons. No cache
            // invalidation needed here: the flag is checked live every morning.
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.keep-eggs.name"),
                tooltip: () => I18n.Get("config.keep-eggs.tooltip"),
                getValue: () => Config.KeepEggs,
                setValue: value => Config.KeepEggs = value
            );
        }
    }
}
