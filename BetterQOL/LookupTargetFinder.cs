using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace BetterQOL
{
    /// <summary>
    /// Answers the question "WHAT is the cursor pointing at right now?" whenever the
    /// player presses the lookup key. It checks, in priority order: any open menu,
    /// on-screen HUD widgets (toolbar), then the world itself - and hands each match to
    /// a Build*Subject factory in LookupDataManager which produces the page data.
    /// "static" again: no instances needed, just a bundle of query functions.
    /// </summary>
    public static class LookupTargetFinder
    {
        /// <summary>
        /// Entry point: resolve whatever is under the mouse into a LookupSubject, or
        /// null when there's nothing meaningful to show.
        /// </summary>
        public static LookupSubject? FindTargetSubject()
        {
            // Ask SMAPI for the cursor. GetScaledScreenPixels returns UI-space pixels -
            // coordinates already adjusted for the game's zoom level, matching the same
            // space menus use for their clickable rectangles.
            var cursor = ModEntry.ModHelper.Input.GetCursorPosition();
            Vector2 uiPixels = cursor.GetScaledScreenPixels();
            int mouseX = (int)uiPixels.X;
            int mouseY = (int)uiPixels.Y;

            // 1. If a menu is open, inspect hovered items or elements in menu
            // Menus take top priority: hovering an inventory slot should describe THAT.
            if (Game1.activeClickableMenu != null)
            {
                var menuSubject = FindTargetInMenu(Game1.activeClickableMenu, mouseX, mouseY);
                if (menuSubject != null)
                    return menuSubject;
            }

            // 2. Check OnScreenMenus (e.g. Toolbar / Hotbar)
            // The toolbar is not an IClickableMenu; it lives in this separate HUD list.
            if (Game1.onScreenMenus != null)
            {
                foreach (var onScreenMenu in Game1.onScreenMenus)
                {
                    if (onScreenMenu is Toolbar toolbar && toolbar.buttons != null)
                    {
                        // Walk every hotbar button; index i corresponds 1:1 with the
                        // same slot in the player's inventory list.
                        for (int i = 0; i < toolbar.buttons.Count; i++)
                        {
                            if (toolbar.buttons[i].containsPoint(mouseX, mouseY))
                            {
                                if (i >= 0 && i < Game1.player.Items.Count && Game1.player.Items[i] != null)
                                {
                                    return LookupDataManager.BuildItemSubject(Game1.player.Items[i]);
                                }
                            }
                        }
                    }
                }
            }

            // 3. If in world, inspect hovered entities, characters, objects, or terrain
            if (Context.IsWorldReady && Game1.currentLocation != null)
            {
                return FindTargetInWorld(Game1.currentLocation);
            }

            return null;
        }

        /// <summary>
        /// Menu-specific lookup. Each "is SomeMenu m" check below is PATTERN MATCHING:
        /// a combined type-test plus cast, so the typed variable only exists when the
        /// menu actually is that type. First match that yields an item wins.
        /// </summary>
        private static LookupSubject? FindTargetInMenu(IClickableMenu menu, int mouseX, int mouseY)
        {

            // GameMenu (Inventory, Crafting, Social)
            // GameMenu is a tabbed container; pages[...] picks the visible tab's content.
            if (menu is GameMenu gameMenu && gameMenu.pages != null && gameMenu.currentTab < gameMenu.pages.Count)
            {
                var activePage = gameMenu.pages[gameMenu.currentTab];
                if (activePage is InventoryPage invPage)
                {
                    // "a ?? b" (null-coalescing): prefer the engine's hovered item, but
                    // fall back to our own hit-test against the inventory grid.
                    var item = invPage.hoveredItem ?? invPage.inventory?.getItemAt(mouseX, mouseY);
                    if (item != null)
                    {
                        return LookupDataManager.BuildItemSubject(item);
                    }

                    // Check equipment icons (boots, rings, hats, shirts, pants, trinkets)
                    if (invPage.equipmentIcons != null)
                    {
                        foreach (var icon in invPage.equipmentIcons)
                        {
                            if (icon != null && icon.containsPoint(mouseX, mouseY))
                            {
                                var equipItem = invPage.hoveredItem;
                                if (equipItem == null)
                                {
                                    // The icon only knows its own label; match keywords in
                                    // that name to figure out WHICH equipment slot to read
                                    // from the player object. ".Value" unwraps the Netcode
                                    // wrapper around each equipped item reference.
                                    string slotName = icon.name ?? string.Empty;
                                    if (slotName.Contains("Hat")) equipItem = Game1.player.hat.Value;
                                    else if (slotName.Contains("Left Ring")) equipItem = Game1.player.leftRing.Value;
                                    else if (slotName.Contains("Right Ring")) equipItem = Game1.player.rightRing.Value;
                                    else if (slotName.Contains("Boots")) equipItem = Game1.player.boots.Value;
                                    else if (slotName.Contains("Shirt")) equipItem = Game1.player.shirtItem.Value;
                                    else if (slotName.Contains("Pants")) equipItem = Game1.player.pantsItem.Value;
                                    else if (slotName.Contains("Trinket") && Game1.player.trinketItems.Count > 0)
                                    {
                                        // Trinkets form a list; show the first occupied one.
                                        for (int i = 0; i < Game1.player.trinketItems.Count; i++)
                                        {
                                            if (Game1.player.trinketItems[i] != null)
                                            {
                                                equipItem = Game1.player.trinketItems[i];
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (equipItem != null)
                                    return LookupDataManager.BuildItemSubject(equipItem);
                            }
                        }
                    }
                }

                if (activePage is CraftingPage craftPage)
                {
                    var item = craftPage.hoverItem ?? craftPage.inventory?.getItemAt(mouseX, mouseY);
                    if (item != null)
                    {
                        return LookupDataManager.BuildItemSubject(item);
                    }
                    // Hovering a RECIPE entry: create a throwaway instance of what the
                    // recipe produces so we can describe it like any other item.
                    if (craftPage.hoverRecipe != null)
                    {
                        var created = craftPage.hoverRecipe.createItem();
                        if (created != null)
                        {
                            return LookupDataManager.BuildItemSubject(created);
                        }
                    }
                }

                if (activePage is SocialPage socialPage)
                {
                    var npc = GetHoveredSocialNPC(socialPage, mouseX, mouseY);
                    if (npc != null)
                    {
                        return LookupDataManager.BuildNPCSubject(npc);
                    }
                }
            }

            // ItemGrabMenu (Chest / Inventory)
            // One generic pattern covers chests, gift menus, etc. - three possible item
            // sources chained with ?? so the first non-null wins.
            if (menu is ItemGrabMenu itemGrabMenu)
            {
                var item = itemGrabMenu.hoveredItem
                        ?? itemGrabMenu.inventory?.getItemAt(mouseX, mouseY)
                        ?? itemGrabMenu.ItemsToGrabMenu?.getItemAt(mouseX, mouseY);
                if (item != null)
                {
                    return LookupDataManager.BuildItemSubject(item);
                }
            }

            // ShopMenu
            if (menu is ShopMenu shopMenu)
            {
                // hoveredItem is stored as ISalable (an INTERFACE - a contract many item
                // types implement), so cast to Item before treating it as one.
                var item = (shopMenu.hoveredItem as Item) ?? shopMenu.inventory?.getItemAt(mouseX, mouseY);
                if (item != null)
                {
                    return LookupDataManager.BuildItemSubject(item);
                }
            }

            // MuseumMenu (Museum donation screen)
            if (menu is MuseumMenu museumMenu)
            {
                var item = museumMenu.hoveredItem ?? museumMenu.inventory?.getItemAt(mouseX, mouseY) ?? museumMenu.heldItem;
                if (item != null)
                {
                    return LookupDataManager.BuildItemSubject(item);
                }
            }

            // ForgeMenu (Volcano Forge & Anvil)
            // Long ?? chain: hovered item, then either forge slot ONLY if the cursor is
            // actually over that slot, then whatever is held on the cursor.
            if (menu is ForgeMenu forgeMenu)
            {
                var item = forgeMenu.hoveredItem
                        ?? forgeMenu.inventory?.getItemAt(mouseX, mouseY)
                        ?? (forgeMenu.leftIngredientSpot?.containsPoint(mouseX, mouseY) == true ? forgeMenu.leftIngredientSpot.item : null)
                        ?? (forgeMenu.rightIngredientSpot?.containsPoint(mouseX, mouseY) == true ? forgeMenu.rightIngredientSpot.item : null)
                        ?? forgeMenu.heldItem;
                if (item != null)
                {
                    return LookupDataManager.BuildItemSubject(item);
                }
            }

            // TailoringMenu (Sewing Machine & Dye Pots)
            // Identical structure to the forge: two ingredient slots plus held item.
            if (menu is TailoringMenu tailoringMenu)
            {
                var item = tailoringMenu.hoveredItem
                        ?? tailoringMenu.inventory?.getItemAt(mouseX, mouseY)
                        ?? (tailoringMenu.leftIngredientSpot?.containsPoint(mouseX, mouseY) == true ? tailoringMenu.leftIngredientSpot.item : null)
                        ?? (tailoringMenu.rightIngredientSpot?.containsPoint(mouseX, mouseY) == true ? tailoringMenu.rightIngredientSpot.item : null)
                        ?? tailoringMenu.heldItem;
                if (item != null)
                {
                    return LookupDataManager.BuildItemSubject(item);
                }
            }

            // GeodeMenu (Clint's geode cracking menu)
            if (menu is GeodeMenu geodeMenu)
            {
                var item = geodeMenu.hoveredItem ?? geodeMenu.inventory?.getItemAt(mouseX, mouseY) ?? geodeMenu.heldItem;
                if (item != null)
                {
                    return LookupDataManager.BuildItemSubject(item);
                }
            }

            // JunimoNoteMenu (Community Center bundle screen)
            if (menu is JunimoNoteMenu junimoMenu)
            {
                var item = junimoMenu.inventory?.getItemAt(mouseX, mouseY);
                if (item != null)
                {
                    return LookupDataManager.BuildItemSubject(item);
                }
            }

            // MenuWithInventory
            // Catch-all base class shared by many menus (fishing, quests, ...), so this
            // last check handles everything the specific menus above missed.
            if (menu is MenuWithInventory menuWithInv)
            {
                var item = menuWithInv.hoveredItem ?? menuWithInv.inventory?.getItemAt(mouseX, mouseY);
                if (item != null)
                {
                    return LookupDataManager.BuildItemSubject(item);
                }
            }

            return null;
        }

        /// <summary>
        /// World-space lookup. Layers are checked roughly "biggest first" (farmers,
        /// characters, animals, clumps, buildings) down to tile-based dictionaries
        /// (objects, terrain features) so tall/animated things win over the ground
        /// beneath them. Always ends with a tile-info fallback, never null.
        /// </summary>
        private static LookupSubject? FindTargetInWorld(GameLocation location)
        {
            var cursor = ModEntry.ModHelper.Input.GetCursorPosition();
            // Tile = the grid cell under the cursor (dictionary key into the map data).
            Vector2 tilePos = cursor.Tile;
            // AbsolutePixels = raw unscaled pixel position - right coordinate space for
            // testing entity bounding boxes.
            Vector2 absPixels = cursor.AbsolutePixels;
            int absX = (int)absPixels.X;
            int absY = (int)absPixels.Y;

            // 1. Check Player Character / Farmers
            // "farmers" includes every human player in multiplayer, not just yourself.
            foreach (var farmer in location.farmers)
            {
                if (farmer != null)
                {
                    // Sprites anchor at the feet, so build a generous box extending UP
                    // from Position (-80px) to cover head and torso too.
                    Rectangle farmerBox = new Rectangle((int)farmer.Position.X - 16, (int)farmer.Position.Y - 80, 96, 128);
                    // Three-way hit test: generous sprite box OR the game's own collision
                    // box OR simply sharing the cursor's grid tile.
                    if (farmerBox.Contains(absX, absY) || farmer.GetBoundingBox().Contains(absX, absY) || farmer.TilePoint == tilePos.ToPoint())
                    {
                        return LookupDataManager.BuildFarmerSubject(farmer);
                    }
                }
            }

            // 2. Characters / NPCs / Monsters / Pets
            foreach (var character in location.characters)
            {
                if (character == null)
                    continue;

                // Generous bounding box for body/head and feet
                Rectangle spriteBox = new Rectangle((int)character.Position.X - 16, (int)character.Position.Y - 80, 96, 128);
                bool hitsCharacter = spriteBox.Contains(absX, absY)
                                  || character.GetBoundingBox().Contains(absX, absY)
                                  || character.TilePoint == tilePos.ToPoint();

                if (hitsCharacter)
                {
                    // All of these derive from the Character base class; "is" checks pick
                    // the most specific type so monsters get combat stats while villagers
                    // get gift schedules. ORDER MATTERS: Monster before generic NPC.
                    if (character is Monster monster)
                    {
                        return LookupDataManager.BuildMonsterSubject(monster);
                    }
                    if (character is Pet pet)
                    {
                        return LookupDataManager.BuildPetSubject(pet);
                    }
                    if (character is NPC npc && npc.IsVillager)
                    {
                        return LookupDataManager.BuildNPCSubject(npc);
                    }
                }
            }

            // 3. Farm Animals
            // animals is a name-keyed dictionary; .Values iterates the FarmAnimal objects.
            foreach (var animal in location.animals.Values)
            {
                if (animal == null)
                    continue;

                Rectangle animalBox = new Rectangle((int)animal.Position.X - 16, (int)animal.Position.Y - 64, 96, 96);
                if (animalBox.Contains(absX, absY) || animal.GetBoundingBox().Contains(absX, absY) || animal.TilePoint == tilePos.ToPoint())
                {
                    return LookupDataManager.BuildAnimalSubject(animal);
                }
            }

            // 4. Resource Clumps (Hardwood Stumps, Logs, Boulders, Meteorites, Fossil Rock)
            // Clumps occupy multiple tiles and store their own bounding boxes.
            if (location.resourceClumps.Count > 0)
            {
                foreach (var clump in location.resourceClumps)
                {
                    if (clump != null && clump.getBoundingBox().Contains(absX, absY))
                    {
                        return LookupDataManager.BuildResourceClumpSubject(clump);
                    }
                }
            }

            // 5. Buildings (Fish Pond, Barn, Coop, Junimo Hut, Silo, Mill, Slime Hutch, Stable, Pet Bowl)
            // occupiesTile() accounts for a building's full multi-tile footprint.
            foreach (var building in location.buildings)
            {
                if (building != null && building.occupiesTile(tilePos))
                {
                    // Fish ponds get a dedicated page (fish stats, population); every
                    // other building falls back to the generic building builder.
                    if (building is FishPond fishPond)
                    {
                        return LookupDataManager.BuildFishPondSubject(fishPond);
                    }
                    return LookupDataManager.BuildBuildingSubject(building);
                }
            }

            // 6. Objects (Chests, Machines, IndoorPots, Items placed in world)
            // Objects live in a dictionary keyed by tile position - TryGetValue does the
            // lookup and outputs the value in one step.
            if (location.Objects.TryGetValue(tilePos, out var obj))
            {
                if (obj is Chest chest)
                {
                    return LookupDataManager.BuildChestSubject(chest);
                }
                // A garden pot growing a crop: look up the crop's HARVEST item id and
                // describe that item (so you see what the pot will produce).
                if (obj is IndoorPot pot && pot.hoeDirt.Value?.crop != null)
                {
                    string harvestId = pot.hoeDirt.Value.crop.indexOfHarvest.Value;
                    var cropItem = ItemRegistry.Create(harvestId);
                    if (cropItem != null)
                    {
                        return LookupDataManager.BuildItemSubject(cropItem);
                    }
                }
                // Machine currently processing something: describe the OUTPUT inside it
                // (e.g. a keg shows "Wine"), which is more useful than the empty keg.
                if (obj.heldObject.Value != null)
                {
                    return LookupDataManager.BuildItemSubject(obj.heldObject.Value);
                }
                return LookupDataManager.BuildItemSubject(obj);
            }

            // 7. Terrain Features (Crops in HoeDirt, Giant Crops, Fruit Trees, Wild Trees, Bushes)
            if (location.terrainFeatures.TryGetValue(tilePos, out var feature))
            {
                if (feature is GiantCrop giantCrop)
                {
                    return LookupDataManager.BuildGiantCropSubject(giantCrop);
                }
                // Tilled soil with a planted crop: describe the crop's harvest item.
                if (feature is HoeDirt hoeDirt && hoeDirt.crop != null)
                {
                    string harvestId = hoeDirt.crop.indexOfHarvest.Value;
                    var cropItem = ItemRegistry.Create(harvestId);
                    if (cropItem != null)
                    {
                        return LookupDataManager.BuildItemSubject(cropItem);
                    }
                }
                if (feature is FruitTree fruitTree)
                {
                    return LookupDataManager.BuildFruitTreeSubject(fruitTree);
                }
                if (feature is Tree tree)
                {
                    return LookupDataManager.BuildTreeSubject(tree);
                }
                if (feature is Bush bush)
                {
                    return LookupDataManager.BuildBushSubject(bush);
                }
            }

            // 8. Large Terrain Features (Bushes spanning larger areas)
            // Not tile-keyed - pixel-test each bush's bounding box instead.
            foreach (var largeFeature in location.largeTerrainFeatures)
            {
                if (largeFeature is Bush largeBush && largeBush.getBoundingBox().Contains(absX, absY))
                {
                    return LookupDataManager.BuildBushSubject(largeBush);
                }
            }

            // 9. Fallback: Tile Location Info
            return LookupDataManager.BuildTileSubject(location, tilePos);
        }

        /// <summary>
        /// Resolves which villager row the cursor is over on the Social tab. The needed
        /// fields are PRIVATE, so we use REFLECTION - runtime introspection that reads
        /// fields by name without compile-time access. Reflection is slower and less
        /// safe than normal code, hence the surrounding try/catch.
        /// </summary>
        private static NPC? GetHoveredSocialNPC(SocialPage socialPage, int mouseX, int mouseY)
        {
            try
            {
                // typeof(SocialPage) gets the class's runtime metadata; BindingFlags say
                // "look at non-public instance fields too".
                var slotField = typeof(SocialPage).GetField("characterSlots", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                // "is System.Collections.IList slots" both casts and null-checks the
                // boxed reflection result in one expression.
                if (slotField?.GetValue(socialPage) is System.Collections.IList slots)
                {
                    var namesField = typeof(SocialPage).GetField("names", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var names = namesField?.GetValue(socialPage) as System.Collections.IList;

                    for (int i = 0; i < slots.Count; i++)
                    {
                        // Find the row under the cursor, then map its index to the
                        // parallel "names" list to learn WHICH villager it is.
                        if (slots[i] is ClickableComponent comp && comp.containsPoint(mouseX, mouseY))
                        {
                            if (names != null && i < names.Count && names[i] is string name)
                            {
                                return Game1.getCharacterFromName(name);
                            }
                        }
                    }
                }
            }
            // Swallow ANY reflection failure (field renamed by a game update, etc.) -
            // a broken social lookup should never crash the game.
            catch { }
            return null;
        }
    }
}
