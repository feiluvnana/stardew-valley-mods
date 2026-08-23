using StardewModdingAPI;
using StardewModdingAPI.Events;
using Common;

namespace BetterMap
{
    public class ModEntry : Mod
    {
        public static ModEntry Instance { get; private set; } = null!;
        public ModConfig Config { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = Helper.ReadConfig<ModConfig>();

            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Monitor.Log("BetterMap loaded successfully.", LogLevel.Info);
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Maps/Island_W"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchIslandWest(editor.Data, Config, Monitor);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/IslandFarmHouse"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchIslandFarmHouse(editor.Data, Config, Monitor);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchFarmHouse(editor.Data, Config, Monitor);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse1") || e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse1_marriage"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchFarmHouse1(editor.Data, Config, Monitor);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse2") || e.NameWithoutLocale.IsEquivalentTo("Maps/FarmHouse2_marriage"))
            {
                e.Edit(asset =>
                {
                    var editor = asset.AsMap();
                    MapPatcher.PatchFarmHouse2(editor.Data, Config, Monitor);
                });
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            RegisterGenericModConfigMenu();
        }

        private void RegisterGenericModConfigMenu()
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () =>
                {
                    Config = new ModConfig();
                    Helper.WriteConfig(Config);
                    ReloadMaps();
                },
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    ReloadMaps();
                }
            );

            // Section: Ginger Island Farm
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => Helper.Translation.Get("gmcm.section.ginger_island.title"),
                tooltip: () => Helper.Translation.Get("gmcm.section.ginger_island.description")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => Config.RemoveFarmDriftwoodBarrier,
                setValue: value => Config.RemoveFarmDriftwoodBarrier = value,
                name: () => Helper.Translation.Get("gmcm.remove_farm_driftwood_barrier.name"),
                tooltip: () => Helper.Translation.Get("gmcm.remove_farm_driftwood_barrier.tooltip")
            );

            // Section: Farmhouse Doorways
            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => Helper.Translation.Get("gmcm.section.farmhouse.title"),
                tooltip: () => Helper.Translation.Get("gmcm.section.farmhouse.description")
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                getValue: () => Config.WidenHouseExit,
                setValue: value => Config.WidenHouseExit = value,
                name: () => Helper.Translation.Get("gmcm.widen_house_exit.name"),
                tooltip: () => Helper.Translation.Get("gmcm.widen_house_exit.tooltip")
            );
        }

        private void ReloadMaps()
        {
            Helper.GameContent.InvalidateCache("Maps/Island_W");
            Helper.GameContent.InvalidateCache("Maps/IslandFarmHouse");
            Helper.GameContent.InvalidateCache("Maps/FarmHouse");
            Helper.GameContent.InvalidateCache("Maps/FarmHouse1");
            Helper.GameContent.InvalidateCache("Maps/FarmHouse1_marriage");
            Helper.GameContent.InvalidateCache("Maps/FarmHouse2");
            Helper.GameContent.InvalidateCache("Maps/FarmHouse2_marriage");
            Monitor.Log("BetterMap: Invalidated map cache and reloaded maps.", LogLevel.Debug);
        }
    }
}
