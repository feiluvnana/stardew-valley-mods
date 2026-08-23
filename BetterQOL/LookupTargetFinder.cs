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
    public static class LookupTargetFinder
    {
        public static LookupSubject? FindTargetSubject()
        {
            var cursor = ModEntry.ModHelper.Input.GetCursorPosition();
            Vector2 uiPixels = cursor.GetScaledScreenPixels();
            int mouseX = (int)uiPixels.X;
            int mouseY = (int)uiPixels.Y;

            // 1. If a menu is open, inspect hovered items or elements in menu
            if (Game1.activeClickableMenu != null)
            {
                var menuSubject = FindTargetInMenu(Game1.activeClickableMenu, mouseX, mouseY);
                if (menuSubject != null)
                    return menuSubject;
            }

            // 2. Check OnScreenMenus (e.g. Toolbar / Hotbar)
            if (Game1.onScreenMenus != null)
            {
                foreach (var onScreenMenu in Game1.onScreenMenus)
                {
                    if (onScreenMenu is Toolbar toolbar && toolbar.buttons != null)
                    {
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

        private static LookupSubject? FindTargetInMenu(IClickableMenu menu, int mouseX, int mouseY)
        {

            // GameMenu (Inventory, Crafting, Social)
            if (menu is GameMenu gameMenu && gameMenu.pages != null && gameMenu.currentTab < gameMenu.pages.Count)
            {
                var activePage = gameMenu.pages[gameMenu.currentTab];
                if (activePage is InventoryPage invPage)
                {
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
                                    string slotName = icon.name ?? string.Empty;
                                    if (slotName.Contains("Hat")) equipItem = Game1.player.hat.Value;
                                    else if (slotName.Contains("Left Ring")) equipItem = Game1.player.leftRing.Value;
                                    else if (slotName.Contains("Right Ring")) equipItem = Game1.player.rightRing.Value;
                                    else if (slotName.Contains("Boots")) equipItem = Game1.player.boots.Value;
                                    else if (slotName.Contains("Shirt")) equipItem = Game1.player.shirtItem.Value;
                                    else if (slotName.Contains("Pants")) equipItem = Game1.player.pantsItem.Value;
                                    else if (slotName.Contains("Trinket") && Game1.player.trinketItems.Count > 0)
                                    {
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

        private static LookupSubject? FindTargetInWorld(GameLocation location)
        {
            var cursor = ModEntry.ModHelper.Input.GetCursorPosition();
            Vector2 tilePos = cursor.Tile;
            Vector2 absPixels = cursor.AbsolutePixels;
            int absX = (int)absPixels.X;
            int absY = (int)absPixels.Y;

            // 1. Check Player Character / Farmers
            foreach (var farmer in location.farmers)
            {
                if (farmer != null)
                {
                    Rectangle farmerBox = new Rectangle((int)farmer.Position.X - 16, (int)farmer.Position.Y - 80, 96, 128);
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
            foreach (var building in location.buildings)
            {
                if (building != null && building.occupiesTile(tilePos))
                {
                    if (building is FishPond fishPond)
                    {
                        return LookupDataManager.BuildFishPondSubject(fishPond);
                    }
                    return LookupDataManager.BuildBuildingSubject(building);
                }
            }

            // 6. Objects (Chests, Machines, IndoorPots, Items placed in world)
            if (location.Objects.TryGetValue(tilePos, out var obj))
            {
                if (obj is Chest chest)
                {
                    return LookupDataManager.BuildChestSubject(chest);
                }
                if (obj is IndoorPot pot && pot.hoeDirt.Value?.crop != null)
                {
                    string harvestId = pot.hoeDirt.Value.crop.indexOfHarvest.Value;
                    var cropItem = ItemRegistry.Create(harvestId);
                    if (cropItem != null)
                    {
                        return LookupDataManager.BuildItemSubject(cropItem);
                    }
                }
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

        private static NPC? GetHoveredSocialNPC(SocialPage socialPage, int mouseX, int mouseY)
        {
            try
            {

                var slotField = typeof(SocialPage).GetField("characterSlots", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (slotField?.GetValue(socialPage) is System.Collections.IList slots)
                {
                    var namesField = typeof(SocialPage).GetField("names", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var names = namesField?.GetValue(socialPage) as System.Collections.IList;

                    for (int i = 0; i < slots.Count; i++)
                    {
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
            catch { }
            return null;
        }
    }
}
