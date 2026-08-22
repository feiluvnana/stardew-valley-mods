using System;
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
            // 1. If a menu is open, inspect hovered items or elements in menu
            if (Game1.activeClickableMenu != null)
            {
                return FindTargetInMenu(Game1.activeClickableMenu);
            }

            // 2. If in world, inspect hovered entities, characters, objects, or terrain
            if (Context.IsWorldReady && Game1.currentLocation != null)
            {
                return FindTargetInWorld(Game1.currentLocation);
            }

            return null;
        }

        private static LookupSubject? FindTargetInMenu(IClickableMenu menu)
        {
            // GameMenu (Inventory, Crafting, Social)
            if (menu is GameMenu gameMenu && gameMenu.pages != null && gameMenu.currentTab < gameMenu.pages.Count)
            {
                var activePage = gameMenu.pages[gameMenu.currentTab];
                if (activePage is InventoryPage invPage && invPage.hoveredItem != null)
                {
                    return LookupDataManager.BuildItemSubject(invPage.hoveredItem);
                }

                if (activePage is CraftingPage craftPage)
                {
                    if (craftPage.hoverItem != null)
                    {
                        return LookupDataManager.BuildItemSubject(craftPage.hoverItem);
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
                    var npc = GetHoveredSocialNPC(socialPage);
                    if (npc != null)
                    {
                        return LookupDataManager.BuildNPCSubject(npc);
                    }
                }
            }

            // ItemGrabMenu (Chest / Inventory)
            if (menu is ItemGrabMenu itemGrabMenu && itemGrabMenu.hoveredItem != null)
            {
                return LookupDataManager.BuildItemSubject(itemGrabMenu.hoveredItem);
            }

            // ShopMenu
            if (menu is ShopMenu shopMenu && shopMenu.hoveredItem is Item shopItem)
            {
                return LookupDataManager.BuildItemSubject(shopItem);
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

            // 1. Characters / NPCs / Monsters / Pets
            foreach (var character in location.characters)
            {
                if (character != null && character.GetBoundingBox().Contains(absX, absY))
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

            // 2. Farm Animals
            foreach (var animal in location.animals.Values)
            {
                if (animal != null && animal.GetBoundingBox().Contains(absX, absY))
                {
                    return LookupDataManager.BuildAnimalSubject(animal);
                }
            }

            // 3. Buildings (e.g. Fish Pond)
            foreach (var building in location.buildings)
            {
                if (building is FishPond fishPond && building.occupiesTile(tilePos))
                {
                    return LookupDataManager.BuildFishPondSubject(fishPond);
                }
            }

            // 4. Objects (Machines, IndoorPots, Items placed in world)
            if (location.Objects.TryGetValue(tilePos, out var obj))
            {
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

            // 5. Terrain Features (Crops in HoeDirt, Fruit Trees, etc.)
            if (location.terrainFeatures.TryGetValue(tilePos, out var feature))
            {
                if (feature is HoeDirt hoeDirt && hoeDirt.crop != null)
                {
                    string harvestId = hoeDirt.crop.indexOfHarvest.Value;
                    var cropItem = ItemRegistry.Create(harvestId);
                    if (cropItem != null)
                    {
                        return LookupDataManager.BuildItemSubject(cropItem);
                    }
                }
                if (feature is FruitTree fruitTree && fruitTree.GetData()?.Fruit != null && fruitTree.GetData()!.Fruit.Count > 0)
                {
                    string fruitId = fruitTree.GetData()!.Fruit[0].ItemId;
                    var fruitItem = ItemRegistry.Create(fruitId);
                    if (fruitItem != null)
                    {
                        return LookupDataManager.BuildItemSubject(fruitItem);
                    }
                }
            }

            // 6. Fallback: Check facing tile / player standing tile
            Vector2 playerTile = Game1.player.Tile;
            if (location.Objects.TryGetValue(playerTile, out var playerObj))
            {
                return LookupDataManager.BuildItemSubject(playerObj);
            }

            return null;
        }

        private static NPC? GetHoveredSocialNPC(SocialPage socialPage)
        {
            try
            {
                int mouseX = Game1.getMouseX();
                int mouseY = Game1.getMouseY();

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
