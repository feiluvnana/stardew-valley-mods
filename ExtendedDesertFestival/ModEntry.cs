using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;

namespace ExtendedDesertFestival
{
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
    }

    public class ModEntry : Mod
    {
        public static ModConfig Config { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static ITranslationHelper I18n { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            ModMonitor = Monitor;
            I18n = helper.Translation;

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
        }

        public static bool IsSeasonEnabled(Season season)
        {
            return season switch
            {
                Season.Spring => true,
                Season.Summer => Config.EnableSummer,
                Season.Fall => Config.EnableFall,
                Season.Winter => Config.EnableWinter,
                _ => false
            };
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/PassiveFestivals"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, PassiveFestivalData>().Data;
                    if (data.TryGetValue("DesertFestival", out var festival))
                    {
                        Season currentSeason = Context.IsWorldReady ? Game1.season : Season.Spring;
                        if (currentSeason == Season.Spring)
                        {
                            // Never alter vanilla Spring Desert Festival (always Spring 15-17)
                            festival.Season = Season.Spring;
                            festival.StartDay = 15;
                            festival.EndDay = 17;
                        }
                        else if (IsSeasonEnabled(currentSeason))
                        {
                            // Extended Desert Festival for Summer, Fall, Winter
                            festival.Season = currentSeason;
                            int start = Math.Clamp(Config.FestivalStartDay, 1, 28);
                            int end = Math.Clamp(Config.FestivalEndDay, start, 28);
                            festival.StartDay = start;
                            festival.EndDay = end;
                        }
                    }
                });
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Invalidate cache upon loading save to ensure correct season config for mid-season loads
            Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (Config.KeepEggs && Game1.player?.team?.itemsToRemoveOvernight != null)
            {
                Game1.player.team.itemsToRemoveOvernight.Remove("CalicoEgg");
                Game1.player.team.itemsToRemoveOvernight.Remove("(O)CalicoEgg");
            }

            // Invalidate cache on 1st day of month (season transition)
            if (Game1.dayOfMonth == 1)
            {
                Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
            }
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            if (Config.KeepEggs && Game1.player?.team?.itemsToRemoveOvernight != null)
            {
                Game1.player.team.itemsToRemoveOvernight.Remove("CalicoEgg");
                Game1.player.team.itemsToRemoveOvernight.Remove("(O)CalicoEgg");
            }
        }



        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    Config = new ModConfig();
                    Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
                },
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
                }
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.festivals")
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.start-day.name"),
                tooltip: () => I18n.Get("config.start-day.tooltip"),
                getValue: () => Config.FestivalStartDay,
                setValue: value =>
                {
                    Config.FestivalStartDay = value;
                    Helper.GameContent.InvalidateCache("Data/PassiveFestivals");
                },
                min: 1,
                max: 28
            );

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