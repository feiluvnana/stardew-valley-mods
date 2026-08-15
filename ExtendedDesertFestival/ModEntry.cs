using System;
using System.Collections.Generic;
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
        void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string>? tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string>? formatValue = null, string? fieldId = null);
    }

    public class ModEntry : Mod
    {
        public static ModConfig Config { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;
        public static ITranslationHelper I18n { get; private set; } = null!;

        private static readonly Dictionary<long, List<Item>> StashedEggs = new();

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
                            prefix: new HarmonyMethod(typeof(ModEntry), nameof(DesertFestival_CleanupFestival_Prefix)),
                            postfix: new HarmonyMethod(typeof(ModEntry), nameof(DesertFestival_CleanupFestival_Postfix))
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

            if (day >= Config.FestivalStartDay && day <= Config.FestivalEndDay)
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

        public static void DesertFestival_CleanupFestival_Prefix()
        {
            if (!Config.KeepEggs)
                return;

            StashedEggs.Clear();
            try
            {
                foreach (Farmer farmer in Game1.getAllFarmers())
                {
                    if (farmer?.Items == null) continue;
                    var eggs = new List<Item>();
                    for (int i = 0; i < farmer.Items.Count; i++)
                    {
                        Item? item = farmer.Items[i];
                        if (item != null && (item.ItemId == "CalicoEgg" || item.QualifiedItemId == "(O)CalicoEgg"))
                        {
                            Item clone = ItemRegistry.Create(item.QualifiedItemId, item.Stack);
                            eggs.Add(clone);
                        }
                    }
                    if (eggs.Count > 0)
                    {
                        StashedEggs[farmer.UniqueMultiplayerID] = eggs;
                    }
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Error stashing Calico Eggs during festival cleanup: {ex}", LogLevel.Warn);
            }
        }

        public static void DesertFestival_CleanupFestival_Postfix()
        {
            if (!Config.KeepEggs || StashedEggs.Count == 0)
                return;

            try
            {
                foreach (Farmer farmer in Game1.getAllFarmers())
                {
                    if (farmer != null && StashedEggs.TryGetValue(farmer.UniqueMultiplayerID, out var eggs))
                    {
                        foreach (var egg in eggs)
                        {
                            farmer.addItemToInventory(egg);
                        }
                    }
                }
                StashedEggs.Clear();
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"Error restoring Calico Eggs after festival cleanup: {ex}", LogLevel.Warn);
            }
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

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.start-day.name"),
                tooltip: () => I18n.Get("config.start-day.tooltip"),
                getValue: () => Config.FestivalStartDay,
                setValue: value => Config.FestivalStartDay = value,
                min: 1,
                max: 28
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.Get("config.end-day.name"),
                tooltip: () => I18n.Get("config.end-day.tooltip"),
                getValue: () => Config.FestivalEndDay,
                setValue: value => Config.FestivalEndDay = value,
                min: 1,
                max: 28
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