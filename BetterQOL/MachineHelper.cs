using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Machines;
using StardewValley.Objects;
using StardewValley.TokenizableStrings;

namespace BetterQOL
{
    /// <summary>
    /// Snapshot of ONE placeable machine's state for the hover tooltip. Built fresh
    /// on every hover by MachineHelper.GetMachineInfo; boolean mode flags decide
    /// which group of fields the renderer should display.
    /// </summary>
    public class MachineInfo
    {
        /// <summary>Localized machine name ("Keg", "Bee House"...).</summary>
        public string MachineName { get; set; } = string.Empty;
        /// <summary>Optional grey subtitle line under the name.</summary>
        public string? Subtitle { get; set; }
        /// <summary>Name of the item inside the machine (its ingredient or output).</summary>
        public string? HeldItemName { get; set; }
        /// <summary>Stack size of the held item (machines usually hold 1).</summary>
        public int HeldItemStack { get; set; } = 1;
        /// <summary>Quality code of the held item: 0 normal, 1 silver, 2 gold, 4 iridium.</summary>
        public int HeldItemQuality { get; set; } = 0;
        /// <summary>Texture atlas containing the held item's icon (null = not resolved).</summary>
        public Texture2D? HeldItemTexture { get; set; }
        /// <summary>Pick-region inside that atlas identifying the exact icon.</summary>
        public Rectangle? HeldItemSourceRect { get; set; }

        /// <summary>True when the finished product can be collected right now.</summary>
        public bool IsReadyToHarvest { get; set; }
        /// <summary>True while the machine is mid-processing (timer still running).</summary>
        public bool IsProcessing { get; set; }
        /// <summary>In-game minutes left on the processing timer.</summary>
        public int MinutesRemaining { get; set; }

        /// <summary>Human-readable countdown, e.g. "4h 20m" or "Tomorrow".</summary>
        public string? TimeRemainingText { get; set; }
        /// <summary>Clock prediction, e.g. "Today at 1:40pm" (shown when ShowExactFinishTime).</summary>
        public string? TargetFinishTimeText { get; set; }

        // Idle state
        /// <summary>True when the machine sits empty, waiting for input.</summary>
        public bool IsIdle { get; set; }
        /// <summary>Translated status line explaining WHY it's idle or what it does.</summary>
        public string? IdleStatusText { get; set; }

        // Special machine details
        // The generic timer fields above don't fit every device, so these extra
        // fields carry specialized info for the few machines with unique rules.
        /// <summary>True when the inspected machine is a Cask (ages cheese/wine in the cellar).</summary>
        public bool IsCask { get; set; }
        /// <summary>Quality the cask's contents have RIGHT NOW.</summary>
        public int CaskCurrentQuality { get; set; }
        /// <summary>Nights until the contents reach the NEXT quality tier.</summary>
        public int CaskDaysToNextQuality { get; set; }
        /// <summary>Nights until the contents reach iridium (best) quality.</summary>
        public int CaskDaysToIridium { get; set; }
        /// <summary>The upcoming quality code the contents are aging toward.</summary>
        public int CaskNextQuality { get; set; }

        /// <summary>True when the inspected machine is a Crab Pot.</summary>
        public bool IsCrabPot { get; set; }
        /// <summary>Whether bait is loaded (unbaited pots catch nothing but junk).</summary>
        public bool CrabPotHasBait { get; set; }
        /// <summary>Bait's display name, shown while baited and waiting.</summary>
        public string? CrabPotBaitName { get; set; }

        /// <summary>True for the Auto-Grabber (collects barn/coop produce into a chest).</summary>
        public bool IsAutoGrabber { get; set; }
        /// <summary>How many items the Auto-Grabber has collected so far.</summary>
        public int AutoGrabberItemCount { get; set; }
    }

    /// <summary>
    /// Snapshot of a whole BUILDING (fish pond, mill, coop...) for the hover
    /// tooltip. Unlike MachineInfo it renders a flexible LIST of colored text
    /// lines, because buildings show varied info rather than one timer.
    /// </summary>
    public class BuildingMachineInfo
    {
        /// <summary>Localized building name, sometimes enriched (e.g. "Fish Pond (Largemouth Bass)").</summary>
        public string BuildingName { get; set; } = string.Empty;
        /// <summary>Optional subtitle line under the name.</summary>
        public string? Subtitle { get; set; }
        /// <summary>Texture atlas for the building's associated icon.</summary>
        public Texture2D? IconTexture { get; set; }
        /// <summary>Pick-region inside that atlas for the icon.</summary>
        public Rectangle? IconSourceRect { get; set; }
        /// <summary>Tooltip body: one colored text line per fact. "= new()" is target-typed shorthand for "new List&lt;TooltipLine&gt;()".</summary>
        public List<TooltipLine> Lines { get; set; } = new();
    }

    /// <summary>
    /// Static helper converting machines and buildings into tooltip records.
    /// "Static class" means it cannot be instantiated and holds only shared
    /// functions - callers write MachineHelper.GetMachineInfo(...) directly on the
    /// class name. Everything here READS game state; nothing modifies it.
    /// </summary>
    public static class MachineHelper
    {
        /// <summary>
        /// Inspects one placed world object and decides what its tooltip should say.
        /// Stardew treats MANY devices as machines (furnace, keg, cask, crab pot,
        /// auto-grabber...), each with slightly different rules, so this method is a
        /// big routing table: identify the device first, then fill the right fields.
        /// </summary>
        /// <param name="obj">The placed object under the cursor. StardewValley.Object
        /// is the game's generic placed-item class - the "StardewValley." prefix
        /// disambiguates it from System.Object, C#'s base of all types).</param>
        /// <returns>MachineInfo to display, or null when not tooltip-worthy.</returns>
        public static MachineInfo? GetMachineInfo(StardewValley.Object obj)
        {
            if (obj == null)
                return null;

            // Handle Cask
            // "is Cask cask" is a DECLARATION PATTERN: it type-checks AND captures
            // the cast result into a new variable in one go. Casks age cheese/wine
            // toward better quality over days, so they get their own builder below.
            if (obj is Cask cask)
            {
                return GetCaskInfo(cask);
            }

            // Handle Crab Pot
            if (obj is CrabPot crabPot)
            {
                return GetCrabPotInfo(crabPot);
            }

            // Fences, indoor pots, scarecrows, forage, and spawned ground objects are never machines
            if (obj is Fence || obj is IndoorPot || obj.IsScarecrow() || obj.isForage() || obj.IsSpawnedObject)
            {
                return null;
            }

            // Check if it's a Chest (containers, not machines, unless Auto-Grabber)
            // Plain chests shouldn't get a machine tooltip, BUT the Auto-Grabber is
            // internally a chest subclass, so it's exempted by id/name checks.
            // StringComparison.OrdinalIgnoreCase makes name tests case-insensitive.
            if (obj is Chest chest && !chest.QualifiedItemId.Contains("165") && !chest.Name.Contains("Auto-Grabber", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Cache commonly used identifiers for the checks below.
            // QualifiedItemId carries a category prefix like "(BC)" (big craftable);
            // ItemId is the bare number/name. "??" replaces a possibly-null Name
            // with "" so later .Contains calls can never hit a null reference.
            string qualifiedId = obj.QualifiedItemId;
            string itemId = obj.ItemId;
            string name = obj.Name ?? string.Empty;
            // Data-driven machines (defined in Data/Machines or added by mods)
            // expose a MachineData row; null for hard-coded specials like statues.
            MachineData? machineData = obj.GetMachineData();

            // Check special machines
            // Vanilla identifies some odd devices inconsistently, so each check tries
            // BOTH an id fragment AND the display name as a fallback ("||" = OR).
            bool isAutoGrabber = qualifiedId.Contains("165") || name.Contains("Auto-Grabber", StringComparison.OrdinalIgnoreCase);
            bool isCoffeeMaker = qualifiedId.Contains("246") || name.Contains("Coffee Maker", StringComparison.OrdinalIgnoreCase);
            bool isWorkbench = qualifiedId.Contains("208") || name.Contains("Workbench", StringComparison.OrdinalIgnoreCase);
            bool isSewingMachine = qualifiedId.Contains("247") || qualifiedId.Contains("SewingMachine", StringComparison.OrdinalIgnoreCase) || name.Contains("Sewing Machine", StringComparison.OrdinalIgnoreCase);
            bool isAnvil = qualifiedId.Contains("Anvil", StringComparison.OrdinalIgnoreCase) || name.Contains("Anvil", StringComparison.OrdinalIgnoreCase);
            bool isMiniForge = qualifiedId.Contains("MiniForge", StringComparison.OrdinalIgnoreCase) || name.Contains("Mini-Forge", StringComparison.OrdinalIgnoreCase);
            bool isStatue = qualifiedId.Contains("160") || qualifiedId.Contains("StatueOf", StringComparison.OrdinalIgnoreCase) || name.Contains("Statue of", StringComparison.OrdinalIgnoreCase);

            // A machine MUST either:
            // 1. Have valid MachineData in Data/Machines (vanilla & modded data-driven machines)
            // 2. Be one of the known special machine devices checked above
            // 3. Be a placed BigCraftable that currently holds an active item or is ready for harvest (fallback for legacy modded machines)
            bool isKnownMachine = machineData != null
                               || isAutoGrabber
                               || isCoffeeMaker
                               || isWorkbench
                               || isSewingMachine
                               || isAnvil
                               || isMiniForge
                               || isStatue
                               || (obj.bigCraftable.Value && (obj.heldObject.Value != null || obj.readyForHarvest.Value));

            if (!isKnownMachine)
            {
                // If it's not a known machine and has no machine data, skip completely.
                // Pure decorations (tables, torches...), twigs, stones, weeds, etc. land here: no tooltip for them.
                return null;
            }

            // From here on we know it's SOME kind of machine; start a record with
            // its localized display name and specialize as checks below match.
            var info = new MachineInfo
            {
                MachineName = obj.DisplayName
            };

            // 1. Auto-Grabber
            // The grabber never "processes"; it silently collects produce into an
            // internal chest overnight, so its tooltip shows an item count instead
            // of a timer. It returns early with its own flavour of info.
            if (isAutoGrabber)
            {
                info.IsAutoGrabber = true;
                // Its heldObject is a hidden Chest holding everything collected.
                if (obj.heldObject.Value is Chest agChest)
                {
                    // LINQ Count(predicate): counts items passing a test. "i => i != null"
                    // is a LAMBDA - a tiny inline function mapping each item to true/false.
                    int count = agChest.Items.Count(i => i != null);
                    info.AutoGrabberItemCount = count;
                    if (count > 0)
                    {
                        info.IsReadyToHarvest = true;
                        // Anonymous object 'new { count }' feeds the number into the
                        // translation template so it can print "...items waiting".
                        info.HeldItemName = ModEntry.I18n.Get("hover.autograbber.items-ready", new { count });
                    }
                    else
                    {
                        info.IsIdle = true;
                        info.IdleStatusText = ModEntry.I18n.Get("hover.autograbber.empty");
                    }
                }
                else
                {
                    // No internal chest yet: nothing has been collected so far.
                    info.IsIdle = true;
                    info.IdleStatusText = ModEntry.I18n.Get("hover.autograbber.empty");
                }
                return info;
            }

            // 2. Workbench
            // Crafting stations never run timers; one descriptive line suffices.
            if (isWorkbench)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.workbench.desc");
                return info;
            }

            // 3. Sewing Machine
            if (isSewingMachine)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.sewing.desc");
                return info;
            }

            // 4. Anvil
            if (isAnvil)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.anvil.desc");
                return info;
            }

            // 5. Mini-Forge
            if (isMiniForge)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.miniforge.desc");
                return info;
            }

            // Held item / output
            // Whatever a machine consumes or has produced lives in heldObject.
            var held = obj.heldObject.Value;
            if (held != null)
            {
                info.HeldItemName = held.DisplayName;
                info.HeldItemStack = held.Stack;
                info.HeldItemQuality = held.Quality;

                // Resolve the held item's icon for the tooltip (atlas + source rect).
                var itemData = ItemRegistry.GetData(held.QualifiedItemId);
                if (itemData != null)
                {
                    try
                    {
                        info.HeldItemTexture = itemData.GetTexture();
                        info.HeldItemSourceRect = itemData.GetSourceRect();
                    }
                    catch
                    {
                        // Ignore texture failures
                    }
                }
            }

            // Ready state
            // Either the game explicitly flagged readiness, or an output exists while
            // the timer has expired - both mean "collect me now".
            if (obj.readyForHarvest.Value || (held != null && obj.MinutesUntilReady <= 0))
            {
                info.IsReadyToHarvest = true;
                info.IsProcessing = false;
                return info;
            }

            // Processing countdown
            // MinutesUntilReady is the game's universal machine timer, ticking down
            // in in-game minutes while time passes (10 in-game minutes per ~7 real
            // seconds at default speed).
            if (obj.MinutesUntilReady > 0)
            {
                info.IsProcessing = true;
                info.MinutesRemaining = obj.MinutesUntilReady;

                // "out" parameters let ONE call hand back TWO results: the formatter
                // computes both the countdown text and the predicted clock time.
                FormatMachineTime(obj.MinutesUntilReady, out string timeRemaining, out string finishTime);
                info.TimeRemainingText = timeRemaining;
                info.TargetFinishTimeText = finishTime;
                return info;
            }

            // Idle special cases (Coffee Maker, Statues, or regular idle machine)
            if (isCoffeeMaker)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.coffeemaker.desc");
                return info;
            }

            if (isStatue)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.statue.desc");
                return info;
            }

            // Generic idle data-driven machine
            if (machineData != null)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.machine.idle");
                return info;
            }

            // Reached only for oddities that matched a special id but revealed no
            // usable state - safer to show nothing than a wrong tooltip.
            return null;
        }

        /// <summary>
        /// Builds the tooltip for a BUILDING on the farm. Buildings are a separate
        /// system from placed-object machines, and nearly every type has bespoke
        /// rules, so this is another router: one block per building kind, each
        /// appending colored text lines; returns null when nothing applied.
        /// </summary>
        /// <param name="building">The building under the cursor.</param>
        /// <returns>BuildingMachineInfo, or null when nothing worth showing.</returns>
        public static BuildingMachineInfo? GetBuildingInfo(Building building)
        {
            if (building == null)
                return null;

            var info = new BuildingMachineInfo
            {
                BuildingName = GetLocalizedBuildingName(building)
            };

            // 1. Fish Pond
            // Fish ponds raise fish, occasionally producing roe/items and sometimes
            // posting a "bring me N of item X" quest to grow the population.
            if (building is FishPond fishPond)
            {
                // Identify which fish lives here and resolve its display name/icon.
                string fishId = fishPond.fishType.Value;
                // Ternary: look up the fish id (with the usual "(O)"-prefix retry),
                // or just null when the pond is empty.
                var fishData = !string.IsNullOrEmpty(fishId) ? (ItemRegistry.GetData(fishId) ?? ItemRegistry.GetData($"(O){fishId}")) : null;
                string fishName = fishData?.DisplayName ?? ModEntry.I18n.Get("hover.fishpond.generic-fish");
                // Interpolation appends " (Fish Name)" to the pond's title line.
                info.BuildingName = $"{info.BuildingName} ({fishName})";

                if (fishData != null)
                {
                    try
                    {
                        info.IconTexture = fishData.GetTexture();
                        info.IconSourceRect = fishData.GetSourceRect();
                    }
                    catch { }
                }

                // Population line in blue: current occupants versus the pond's cap.
                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.fishpond.population", new { count = fishPond.FishCount, max = fishPond.maxOccupants.Value }),
                    new Color(20, 110, 220)
                ));

                // Priority 1: an output (roe etc.) is waiting in the collection bin.
                if (fishPond.output.Value != null)
                {
                    var outputData = ItemRegistry.GetData(fishPond.output.Value.QualifiedItemId);
                    string outName = outputData?.DisplayName ?? fishPond.output.Value.DisplayName;
                    // Append " xN" only when more than one dropped; ternary picks "" otherwise.
                    string stackStr = fishPond.output.Value.Stack > 1 ? $" x{fishPond.output.Value.Stack}" : "";
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.fishpond.output-ready", new { item = $"{outName}{stackStr}" }),
                        new Color(0, 140, 0)
                    ));
                }
                // Priority 2: the pond posted a "bring me N of item X" quest.
                else if (fishPond.neededItem.Value != null)
                {
                    // Seed a translated placeholder, then overwrite with the real name.
                    string neededItemName = ModEntry.I18n.Get("hover.fishpond.default-item").ToString();
                    var neededItem = fishPond.neededItem.Value;
                    if (neededItem != null)
                    {
                        var itmData = ItemRegistry.GetData(neededItem.QualifiedItemId);
                        neededItemName = itmData?.DisplayName ?? neededItem.DisplayName;
                        int neededCount = fishPond.neededItemCount.Value;
                        if (neededCount > 1) neededItemName = $"{neededItemName} x{neededCount}";
                    }
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.fishpond.needs-quest-item", new { item = neededItemName }),
                        new Color(220, 20, 60)
                    ));
                }
                else
                {
                    // Otherwise predict population growth: daysSinceSpawn counts up
                    // toward the species' SpawnTime cadence (-1 = no growth pending).
                    if (fishPond.daysSinceSpawn.Value >= 0 && fishPond.FishCount < fishPond.maxOccupants.Value)
                    {
                        // "??" default of 3 mirrors the game's common spawn cadence.
                        int spawnRate = fishPond.GetFishPondData()?.SpawnTime ?? 3;
                        int daysLeft = Math.Max(0, spawnRate - fishPond.daysSinceSpawn.Value);
                        if (daysLeft <= 1)
                        {
                            info.Lines.Add(new TooltipLine(
                                ModEntry.I18n.Get("hover.fishpond.spawning-tomorrow"),
                                new Color(0, 140, 0)
                            ));
                        }
                        else
                        {
                            info.Lines.Add(new TooltipLine(
                                ModEntry.I18n.Get("hover.fishpond.spawning-in", new { days = daysLeft }),
                                new Color(180, 100, 0)
                            ));
                        }
                    }
                    else
                    {
                        // Full pond or no growth scheduled: neutral grey status line.
                        info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.fishpond.producing"), Color.DarkSlateGray));
                    }
                }

                return info;
            }

            // 2. Mill
            // The mill grinds wheat/beets/etc.: input goes in one chest, flour/sugar
            // appears in the other. Both counts make useful tooltip lines.
            // "?." may yield null; comparing "null == true" is simply false, so a
            // missing type string fails this check safely instead of crashing.
            if (building.buildingType.Value?.Equals("Mill", StringComparison.OrdinalIgnoreCase) == true)
            {
                var inputChest = building.GetBuildingChest("Input");
                var outputChest = building.GetBuildingChest("Output");

                // "??" guards against a missing chest; the lambda skips null slots
                // while counting ("i => i != null" keeps only non-null items).
                int inputCount = inputChest?.Items.Count(i => i != null) ?? 0;
                int outputCount = outputChest?.Items.Count(i => i != null) ?? 0;

                if (outputCount > 0)
                {
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.mill.output-ready", new { count = outputCount }),
                        new Color(0, 140, 0)
                    ));
                }

                if (inputCount > 0)
                {
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.mill.processing-input", new { count = inputCount }),
                        new Color(180, 100, 0)
                    ));
                }
                else if (outputCount == 0)
                {
                    // Nothing in, nothing out: report the mill as idle (grey).
                    info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.mill.idle"), Color.DarkSlateGray));
                }

                return info;
            }

            // 3. Junimo Hut
            // Junimos harvest nearby crops into a storage chest, pausing for rain,
            // winter, or when the player disables harvesting.
            if (building is JunimoHut junimoHut)
            {
                var outputChest = junimoHut.GetOutputChest();
                int itemCount = outputChest?.Items.Count(i => i != null) ?? 0;

                // Active only when harvesting is enabled, it isn't raining HERE, and
                // the season isn't winter ("&&" chains all three requirements).
                bool isHarvesting = !junimoHut.noHarvest.Value && !Game1.IsRainingHere(junimoHut.GetParentLocation()) && Game1.season != Season.Winter;
                if (isHarvesting)
                {
                    info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.junimohut.active"), new Color(0, 140, 0)));
                }
                else
                {
                    info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.junimohut.paused"), Color.DarkSlateGray));
                }

                // A Junimo "raisin" snack keeps them working through winter; show how
                // many days of the effect remain.
                if (junimoHut.raisinDays.Value > 0)
                {
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.junimohut.raisins-active", new { days = junimoHut.raisinDays.Value }),
                        new Color(180, 50, 180)
                    ));
                }

                if (itemCount > 0)
                {
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.junimohut.items-stored", new { count = itemCount }),
                        new Color(20, 110, 220)
                    ));
                }

                return info;
            }

            // 4. Silo
            // Silos store hay cut from grass. The tooltip reports GLOBAL hay storage:
            // total hay on the farm versus capacity (240 hay per silo).
            if (building.buildingType.Value?.Equals("Silo", StringComparison.OrdinalIgnoreCase) == true)
            {
                int hay = Game1.getFarm()?.piecesOfHay?.Value ?? 0;
                int siloCount = 0;
                if (Game1.getFarm() != null)
                {
                    // Loop over every farm building counting silos - capacity scales
                    // with how many were built.
                    foreach (var b in Game1.getFarm().buildings)
                    {
                        if (b.buildingType.Value?.Equals("Silo", StringComparison.OrdinalIgnoreCase) == true)
                            siloCount++;
                    }
                }
                // Math.Max(1, ...) avoids multiplying by zero when this is somehow
                // the last silo being hovered.
                int maxHay = Math.Max(1, siloCount) * 240;

                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.silo.hay-count", new { current = hay, max = maxHay }),
                    new Color(180, 100, 0)
                ));
                return info;
            }

            // 5. Shipping Bin
            if (building is ShippingBin || building.buildingType.Value?.Equals("Shipping Bin", StringComparison.OrdinalIgnoreCase) == true)
            {
                var farm = Game1.getFarm();
                // Count everything queued for overnight sale in THIS player's bin.
                int itemsCount = farm != null ? farm.getShippingBin(Game1.player).Count : 0;
                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.shippingbin.items", new { count = itemsCount }),
                    new Color(20, 110, 220)
                ));
                return info;
            }

            // 6. Pet Bowl (SDV 1.6)
            if (building is PetBowl petBowl || building.buildingType.Value?.Equals("Pet Bowl", StringComparison.OrdinalIgnoreCase) == true)
            {
                bool isWatered = false;
                // A second pattern check safely extracts the typed bowl to read its flag.
                if (building is PetBowl pb) isWatered = pb.watered.Value;
                // Ternary picks BOTH the text and its color based on water state.
                info.Lines.Add(new TooltipLine(
                    isWatered ? ModEntry.I18n.Get("hover.petbowl.watered").ToString() : ModEntry.I18n.Get("hover.petbowl.unwatered").ToString(),
                    isWatered ? new Color(20, 110, 220) : new Color(200, 60, 20)
                ));
                return info;
            }

            // 7. Slime Hutch
            if (building.GetIndoors() is SlimeHutch || building.buildingType.Value?.Equals("Slime Hutch", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (building.GetIndoors() is SlimeHutch sh)
                {
                    // LINQ counts again: slimes living here, water troughs filled,
                    // and slime balls sitting on the floor. Each Count takes a lambda.
                    int slimeCount = sh.characters.Count(c => c is StardewValley.Monsters.GreenSlime);
                    int troughsWatered = sh.waterSpots.Count(w => w);
                    int slimeBalls = sh.Objects.Pairs.Count(o => o.Value.QualifiedItemId == "(BC)56" || o.Value.Name == "Slime Ball");

                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.slimehutch.slimes-format", new { current = slimeCount, max = 20 }).ToString(),
                        slimeCount >= 20 ? new Color(0, 140, 0) : new Color(20, 110, 220)
                    ));
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.slimehutch.troughs-format", new { watered = troughsWatered, total = 4 }).ToString(),
                        troughsWatered == 4 ? new Color(0, 140, 0) : new Color(200, 60, 20)
                    ));
                    if (slimeBalls > 0)
                    {
                        info.Lines.Add(new TooltipLine(
                            ModEntry.I18n.Get("hover.slimehutch.slimeballs-format", new { count = slimeBalls }).ToString(),
                            new Color(0, 140, 0)
                        ));
                    }
                }
                return info;
            }

            // 8. Stable
            if (building is Stable || building.buildingType.Value?.Equals("Stable", StringComparison.OrdinalIgnoreCase) == true)
            {
                string hName = Game1.player.horseName.Value ?? ModEntry.I18n.Get("hover.stable.horse").ToString();
                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.stable.horse-info", new { name = hName }).ToString(),
                    new Color(180, 100, 0)
                ));
                return info;
            }

            // 9. Animal Housing (Barn, Coop) - Only for actual AnimalHouse locations
            if (building.GetIndoors() is AnimalHouse animalHouse)
            {
                int current = animalHouse.animalsThatLiveHere.Count;
                int max = building.maxOccupants.Value > 0 ? building.maxOccupants.Value : animalHouse.animalLimit.Value;
                bool doorOpen = building.animalDoorOpen.Value;

                info.Lines.Add(new TooltipLine(
                    ModEntry.I18n.Get("hover.building.occupants", new { current = current, max = max }),
                    new Color(20, 110, 220)
                ));
                info.Lines.Add(new TooltipLine(
                    doorOpen ? ModEntry.I18n.Get("hover.animalhouse.door-open").ToString() : ModEntry.I18n.Get("hover.animalhouse.door-closed").ToString(),
                    doorOpen ? new Color(0, 140, 0) : Color.DarkSlateGray
                ));
                return info;
            }

            // 10. Shed / Big Shed
            if (building.GetIndoors() is Shed || building.buildingType.Value?.Contains("Shed", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (building.GetIndoors() is Shed shed)
                {
                    // Count every placed object inside the shed's interior map.
                    int objCount = shed.Objects.Pairs.Count();
                    info.Lines.Add(new TooltipLine(
                        ModEntry.I18n.Get("hover.shed.objects-count", new { count = objCount }).ToString(),
                        Color.DarkSlateGray
                    ));
                }
                return info;
            }

            // 11. Greenhouse
            if (building.buildingType.Value?.Equals("Greenhouse", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Repaired = Community Center pantry bundles finished OR the Joja
                // route bought the repair (mail flag "jojaPantry"/"ccPantry").
                bool isRepaired = Game1.player.hasCompletedCommunityCenter()
                               || Game1.MasterPlayer.mailReceived.Contains("jojaPantry")
                               || Game1.MasterPlayer.mailReceived.Contains("ccPantry");
                info.Lines.Add(new TooltipLine(
                    isRepaired ? ModEntry.I18n.Get("hover.greenhouse.repaired").ToString() : ModEntry.I18n.Get("hover.greenhouse.needs-repair").ToString(),
                    isRepaired ? new Color(0, 140, 0) : new Color(200, 60, 20)
                ));
                return info;
            }

            // 12. FarmHouse / Cabin
            if (building.buildingType.Value?.Equals("FarmHouse", StringComparison.OrdinalIgnoreCase) == true || building.buildingType.Value?.Equals("Cabin", StringComparison.OrdinalIgnoreCase) == true)
            {
                // HouseUpgradeLevel: 0 starting cabin up to 3 full upgrades. A switch
                // expression maps each level to its own translated label.
                int lvl = Game1.player.HouseUpgradeLevel;
                string lvlText = lvl switch
                {
                    0 => ModEntry.I18n.Get("hover.farmhouse.level-0").ToString(),
                    1 => ModEntry.I18n.Get("hover.farmhouse.level-1").ToString(),
                    2 => ModEntry.I18n.Get("hover.farmhouse.level-2").ToString(),
                    3 => ModEntry.I18n.Get("hover.farmhouse.level-3").ToString(),
                    // "_" discard arm future-proofs against modded levels beyond 3.
                    _ => ModEntry.I18n.Get("hover.farmhouse.level-default", new { level = lvl }).ToString()
                };
                info.Lines.Add(new TooltipLine(lvlText, new Color(180, 100, 0)));
                return info;
            }

            // 13. Obelisks & Special Towers
            if (building.buildingType.Value?.Contains("Obelisk", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Lowercase once, then a switch with "when" GUARD clauses: each arm's
                // extra condition (s.Contains...) filters which arm matches.
                string bType = building.buildingType.Value.ToLower();
                string dest = bType switch
                {
                    var s when s.Contains("earth") => ModEntry.I18n.Get("hover.obelisk.destination-mountains").ToString(),
                    var s when s.Contains("water") => ModEntry.I18n.Get("hover.obelisk.destination-beach").ToString(),
                    var s when s.Contains("desert") => ModEntry.I18n.Get("hover.obelisk.destination-desert").ToString(),
                    var s when s.Contains("island") => ModEntry.I18n.Get("hover.obelisk.destination-island").ToString(),
                    _ => ModEntry.I18n.Get("hover.obelisk.warp-destination").ToString()
                };
                info.Lines.Add(new TooltipLine(dest, new Color(180, 50, 180)));
                return info;
            }

            if (building.buildingType.Value?.Equals("Gold Clock", StringComparison.OrdinalIgnoreCase) == true)
            {
                info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.goldclock.effect").ToString(), new Color(180, 100, 0)));
                return info;
            }

            if (building.buildingType.Value?.Equals("Well", StringComparison.OrdinalIgnoreCase) == true)
            {
                info.Lines.Add(new TooltipLine(ModEntry.I18n.Get("hover.well.water-source").ToString(), new Color(20, 110, 220)));
                return info;
            }

            // No block above claimed this building type AND no lines were added:
            // show nothing rather than an empty tooltip.
            if (info.Lines.Count == 0)
                return null;

            return info;
        }

        /// <summary>
        /// Builds the tooltip for a Cask, which ages cheese/wine/mead toward higher
        /// quality over many nights. Casks are special because the interesting info
        /// isn't a minute timer but quality progression math.
        /// </summary>
        /// <param name="cask">The Cask being hovered.</param>
        /// <returns>A MachineInfo flagged IsCask with aging details filled in.</returns>
        private static MachineInfo GetCaskInfo(Cask cask)
        {
            var info = new MachineInfo
            {
                MachineName = cask.DisplayName,
                IsCask = true
            };

            // Empty cask: nothing to age, show an idle line.
            var held = cask.heldObject.Value;
            if (held == null)
            {
                info.IsIdle = true;
                info.IdleStatusText = ModEntry.I18n.Get("hover.cask.empty");
                return info;
            }

            info.HeldItemName = held.DisplayName;
            info.HeldItemStack = held.Stack;
            info.HeldItemQuality = held.Quality;
            info.CaskCurrentQuality = held.Quality;

            // Resolve the icon of whatever is inside the cask.
            var itemData = ItemRegistry.GetData(held.QualifiedItemId);
            if (itemData != null)
            {
                try
                {
                    info.HeldItemTexture = itemData.GetTexture();
                    info.HeldItemSourceRect = itemData.GetSourceRect();
                }
                catch
                {
                    // Ignore texture failures
                }
            }

            // Done aging when: the game says it's ready, contents already hit
            // iridium (4), or the internal maturity countdown reached zero.
            if (cask.readyForHarvest.Value || held.Quality >= 4 || cask.daysToMature.Value <= 0)
            {
                info.IsReadyToHarvest = true;
                info.IsProcessing = false;
                return info;
            }

            info.IsProcessing = true;
            // daysToMature counts down in RAW units at rate 1.0. The cellar's cask
            // multiplier ("agingRate") makes real time pass faster: divide by rate.
            float rawDaysRemaining = cask.daysToMature.Value;
            float rate = Math.Max(0.1f, cask.agingRate.Value);

            // In SDV Cask aging thresholds (raw units):
            // Normal (56..42) -> Silver at 42
            // Silver (42..28) -> Gold at 28
            // Gold (28..0) -> Iridium at 0
            if (held.Quality == 0) // Normal -> Silver
            {
                info.CaskNextQuality = 1; // Silver
                float days = Math.Max(0f, rawDaysRemaining - 42f) / rate;
                info.CaskDaysToNextQuality = Math.Max(1, (int)Math.Ceiling(days - 0.001f));
            }
            else if (held.Quality == 1) // Silver -> Gold
            {
                info.CaskNextQuality = 2; // Gold
                float days = Math.Max(0f, rawDaysRemaining - 28f) / rate;
                info.CaskDaysToNextQuality = Math.Max(1, (int)Math.Ceiling(days - 0.001f));
            }
            else // Gold (2) -> Iridium
            {
                info.CaskNextQuality = 4; // Iridium
                float days = Math.Max(0f, rawDaysRemaining) / rate;
                info.CaskDaysToNextQuality = Math.Max(1, (int)Math.Ceiling(days - 0.001f));
            }

            // Total nights until iridium from right now. Math.Ceiling rounds UP to
            // whole nights; the tiny "- 0.001" epsilon avoids showing "2 days" for
            // something that is actually exactly 2.000 days (floating-point noise).
            info.CaskDaysToIridium = Math.Max(1, (int)Math.Ceiling((Math.Max(0f, rawDaysRemaining) / rate) - 0.001f));

            return info;
        }

        /// <summary>
        /// Builds the tooltip for a CRAB POT. Pots have two simple states: a catch
        /// waiting to be collected, or baited and waiting / empty.
        /// </summary>
        /// <param name="crabPot">The CrabPot being hovered.</param>
        /// <returns>A MachineInfo flagged IsCrabPot.</returns>
        private static MachineInfo GetCrabPotInfo(CrabPot crabPot)
        {
            var info = new MachineInfo
            {
                MachineName = crabPot.DisplayName,
                IsCrabPot = true
            };

            // A held object means something was caught: treat it as ready to harvest
            // and resolve its name/stack/quality/icon like any machine output.
            var held = crabPot.heldObject.Value;
            if (held != null)
            {
                info.HeldItemName = held.DisplayName;
                info.HeldItemStack = held.Stack;
                info.HeldItemQuality = held.Quality;
                info.IsReadyToHarvest = true;

                var itemData = ItemRegistry.GetData(held.QualifiedItemId);
                if (itemData != null)
                {
                    try
                    {
                        info.HeldItemTexture = itemData.GetTexture();
                        info.HeldItemSourceRect = itemData.GetSourceRect();
                    }
                    catch
                    {
                        // Ignore texture failures
                    }
                }
                return info;
            }

            // Empty pot: its state depends on whether bait is loaded.
            var bait = crabPot.bait.Value;
            if (bait != null)
            {
                info.CrabPotHasBait = true;
                info.CrabPotBaitName = bait.DisplayName;
                // Baited + empty = "working" (waiting for a catch overnight).
                info.IsProcessing = true;
            }
            else
            {
                info.CrabPotHasBait = false;
                info.IsProcessing = false;
            }

            return info;
        }

        /// <summary>
        /// Converts a machine's remaining MINUTES into friendly text: how long is
        /// left ("4h 20m", "Tomorrow", "In 3 days") and, when possible, the exact
        /// clock time it will finish ("Today at 1:40pm").
        /// </summary>
        /// <param name="minutesRemaining">Minutes left on the machine timer.</param>
        /// <param name="timeRemaining">Receives the countdown phrase (an "out" parameter:
        /// the method MUST assign it before returning).</param>
        /// <param name="finishTime">Receives the predicted finish-clock phrase.</param>
        public static void FormatMachineTime(int minutesRemaining, out string timeRemaining, out string finishTime)
        {
            // Game1.timeOfDay encodes clock time as an int: 600 = 6:00am,
            // 1330 = 1:30pm, 2600 = 2:00am. Integer division and remainder split it.
            // "/ 100" keeps the hour digits; "% 100" (modulo) keeps the minutes part.
            int currentDayTime = Game1.timeOfDay;
            int curHours = currentDayTime / 100;
            int curMins = currentDayTime % 100;

            // In Stardew Valley, 6am = 600, 2am = 2600. Total 20 game hours (1200 mins) during the day.
            // Minutes elapsed since the 6am wake-up, then how many remain before 2am
            // forces sleep (floored at 0 so late nights never go negative).
            int minsPassedToday = (curHours - 6) * 60 + curMins;
            int minsLeftToday = Math.Max(0, (20 * 60) - minsPassedToday);

            if (minutesRemaining <= minsLeftToday)
            {
                // Completes today
                // Split total minutes into whole hours + leftover minutes with the
                // same divide/modulo pair, then pick a matching translation template.
                int hours = minutesRemaining / 60;
                int mins = minutesRemaining % 60;

                if (hours > 0 && mins > 0)
                {
                    timeRemaining = ModEntry.I18n.Get("hover.time.hours-minutes", new { hours, minutes = mins });
                }
                else if (hours > 0)
                {
                    timeRemaining = ModEntry.I18n.Get("hover.time.hours", new { hours });
                }
                else
                {
                    // Show at least "1m" so sub-minute timers don't read as zero.
                    timeRemaining = ModEntry.I18n.Get("hover.time.minutes", new { minutes = Math.Max(1, mins) });
                }

                // Utility.ModifyTime adds game-minutes to a clock value while keeping
                // its HHmm format valid; getTimeOfDayString renders it as text.
                int targetTimeInt = Utility.ModifyTime(currentDayTime, minutesRemaining);
                string timeString = Game1.getTimeOfDayString(targetTimeInt);
                finishTime = ModEntry.I18n.Get("hover.time.today-at", new { time = timeString });
            }
            else
            {
                // Completes in future day
                int remAfterToday = minutesRemaining - minsLeftToday;
                // Full days are 1600 minutes in SDV's machine countdown logic (1200 day + 400 night)
                // Machines keep counting through the sleeping hours, so each calendar
                // day consumes 1600 minutes. Integer division gives whole extra days;
                // modulo leaves the leftover minutes on the final morning.
                int daysAhead = 1 + (remAfterToday / 1600);
                int minsInFinalDay = remAfterToday % 1600;

                if (daysAhead == 1)
                {
                    timeRemaining = ModEntry.I18n.Get("hover.time.tomorrow");
                }
                else
                {
                    timeRemaining = ModEntry.I18n.Get("hover.time.days", new { days = daysAhead });
                }

                // A non-positive leftover means it completes exactly at 6am wakeup;
                // otherwise add the leftover minutes to the 6am starting point.
                int targetTimeInt = minsInFinalDay <= 0 ? 600 : Utility.ModifyTime(600, minsInFinalDay);
                string timeString = Game1.getTimeOfDayString(targetTimeInt);

                if (daysAhead == 1)
                {
                    finishTime = ModEntry.I18n.Get("hover.time.tomorrow-at", new { time = timeString });
                }
                else
                {
                    finishTime = ModEntry.I18n.Get("hover.time.in-days-at", new { days = daysAhead, time = timeString });
                }
            }
        }

        /// <summary>
        /// Resolves a localized building name, using Data/Buildings display name tokens or fallback translation keys.
        /// </summary>
        public static string GetLocalizedBuildingName(Building building)
        {
            try
            {
                var data = building.GetData();
                if (data != null && !string.IsNullOrEmpty(data.Name))
                {
                    string parsed = TokenParser.ParseText(data.Name);
                    if (!string.IsNullOrEmpty(parsed))
                        return parsed;
                }
            }
            catch { }

            string bType = building.buildingType.Value ?? string.Empty;
            return bType switch
            {
                "Fish Pond" => ModEntry.I18n.Get("hover.building.fishpond"),
                "Mill" => ModEntry.I18n.Get("hover.building.mill"),
                "Silo" => ModEntry.I18n.Get("hover.building.silo"),
                "Shipping Bin" => ModEntry.I18n.Get("hover.building.shipping-bin"),
                "Pet Bowl" => ModEntry.I18n.Get("hover.building.pet-bowl"),
                "Slime Hutch" => ModEntry.I18n.Get("hover.building.slime-hutch"),
                "Stable" => ModEntry.I18n.Get("hover.building.stable"),
                "Barn" => ModEntry.I18n.Get("hover.building.barn"),
                "Big Barn" => ModEntry.I18n.Get("hover.building.big-barn"),
                "Deluxe Barn" => ModEntry.I18n.Get("hover.building.deluxe-barn"),
                "Coop" => ModEntry.I18n.Get("hover.building.coop"),
                "Big Coop" => ModEntry.I18n.Get("hover.building.big-coop"),
                "Deluxe Coop" => ModEntry.I18n.Get("hover.building.deluxe-coop"),
                "Shed" => ModEntry.I18n.Get("hover.building.shed"),
                "Big Shed" => ModEntry.I18n.Get("hover.building.big-shed"),
                "Greenhouse" => ModEntry.I18n.Get("hover.building.greenhouse"),
                "FarmHouse" => ModEntry.I18n.Get("hover.building.farmhouse"),
                "Cabin" => ModEntry.I18n.Get("hover.building.cabin"),
                "Gold Clock" => ModEntry.I18n.Get("hover.building.gold-clock"),
                "Well" => ModEntry.I18n.Get("hover.building.well"),
                "Junimo Hut" => ModEntry.I18n.Get("hover.building.junimo-hut"),
                "Earth Obelisk" => ModEntry.I18n.Get("hover.building.earth-obelisk"),
                "Water Obelisk" => ModEntry.I18n.Get("hover.building.water-obelisk"),
                "Desert Obelisk" => ModEntry.I18n.Get("hover.building.desert-obelisk"),
                "Island Obelisk" => ModEntry.I18n.Get("hover.building.island-obelisk"),
                _ => !string.IsNullOrEmpty(bType) ? bType : ModEntry.I18n.Get("hover.building.generic")
            };
        }
    }
}
