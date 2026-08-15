using System;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ExtendedDesertFestival
{
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
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

            var harmony = new Harmony(ModManifest.UniqueID);
            try
            {
                // Patch Utility.isFestivalDay(int dayOfMonth, Season season)
                MethodInfo isFestivalDayMethod = AccessTools.Method(typeof(Utility), nameof(Utility.isFestivalDay), new[] { typeof(int), typeof(Season) });
                if (isFestivalDayMethod != null)
                {
                    harmony.Patch(
                        original: isFestivalDayMethod,
                        postfix: new HarmonyMethod(typeof(ModEntry), nameof(Utility_isFestivalDay_Postfix))
                    );
                }

                // Patch DesertFestival.CleanupFestival
                Type desertFestivalType = AccessTools.TypeByName("StardewValley.DesertFestival");
                if (desertFestivalType != null)
                {
                    MethodInfo cleanupMethod = AccessTools.Method(desertFestivalType, "CleanupFestival");
                    if (cleanupMethod != null)
                    {
                        harmony.Patch(
                            original: cleanupMethod,
                            prefix: new HarmonyMethod(typeof(ModEntry), nameof(DesertFestival_CleanupFestival_Prefix))
                        );
                    }
                }

                Monitor.Log("Harmony patches for ExtendedDesertFestival applied successfully.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to apply ExtendedDesertFestival harmony patches: {ex}", LogLevel.Error);
            }

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        public static void Utility_isFestivalDay_Postfix(int day, Season season, ref bool __result)
        {
            if (__result)
                return;

            if (day >= 22 && day <= 24)
            {
                if (Config.EnableSummer && season == Season.Summer)
                {
                    __result = true;
                }
                else if (Config.EnableFall && season == Season.Fall)
                {
                    __result = true;
                }
                else if (Config.EnableWinter && season == Season.Winter)
                {
                    __result = true;
                }
            }
        }

        public static bool DesertFestival_CleanupFestival_Prefix()
        {
            // If KeepEggs is true, we allow standard cleanup except player inventory egg clearing
            // Or return true to proceed with cleanup
            return true;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => I18n.Get("config.section.festivals")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-summer.name"),
                tooltip: () => I18n.Get("config.enable-summer.tooltip"),
                getValue: () => Config.EnableSummer,
                setValue: value => Config.EnableSummer = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-fall.name"),
                tooltip: () => I18n.Get("config.enable-fall.tooltip"),
                getValue: () => Config.EnableFall,
                setValue: value => Config.EnableFall = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Get("config.enable-winter.name"),
                tooltip: () => I18n.Get("config.enable-winter.tooltip"),
                getValue: () => Config.EnableWinter,
                setValue: value => Config.EnableWinter = value
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