using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Commands
{
	class MappingCommands
    {
        [Command("mapping", Shortcut = "map")]
        private static void MappingCommand(Player player)
        {
            player.SendClientMessage($"Usage: {ColorPalette.Secondary.Main}/mapping [action]");
            player.SendClientMessage($"Global Actions: {ColorPalette.Secondary.Main}help, create, loadc, exit");
            player.SendClientMessage($"On map editing Actions: {ColorPalette.Secondary.Main}help, save, exit, list, addo, delo, replace, marker, dist, edit");
        }
        [CommandGroup("mapping", "map")]
        class MappingCommandClass
        {
            [Command("help")]
            private static void HelpCommand(Player player)
            {
                string list =
                    $"{ColorPalette.Primary.Main}/mapping create {ColorPalette.Primary.Darken}Create a new map" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping loadc [id] {ColorPalette.Primary.Darken}Load a map" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping save {ColorPalette.Primary.Darken}Save the map" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping exit {ColorPalette.Primary.Darken}Close the editor without saving the map" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping list {ColorPalette.Primary.Darken}List all your races" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping addo [modelid] {ColorPalette.Primary.Darken}Add an object with specified modelid" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping delo [objectid] {ColorPalette.Primary.Darken}Delete the object" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping replace [objectid] [modelid] {ColorPalette.Primary.Darken}Replace the object by the s modelid" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping dupl [objectid] {ColorPalette.Primary.Darken}Duplicate the object" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping marker [1-2] {ColorPalette.Primary.Darken}Edit the marker position to get distance" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping dist {ColorPalette.Primary.Darken}Displays the distance between the markers" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping edit [objectid] {ColorPalette.Primary.Darken}Edit position/rotation of object" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping exit {ColorPalette.Primary.Darken}Close the editor"
                    ;
                MessageDialog dialog = new MessageDialog("Command list", list, "Close");
                dialog.Show(player);
            }
            [Command("create")]
            private static void CreateCommand(Player player)
            {
                if (player.pEvent != null)
                    return;
                player.mapCreator ??= new MapCreator(player);
                player.mapCreator.CreateMap();
            }
            [Command("loadc")]
            private static void LoadCommand(Player player, int id)
            {
                if (player.pEvent != null)
                    return;
                player.mapCreator ??= new MapCreator(player);
                if(player.mapCreator.editingMap != null)
				{
                    MessageDialog msg = new MessageDialog("Confirm", "You are currently editing a map, do you want to close and lost all unsaved data to load the next map ?", "Yes, load", "No, I will save");
                    msg.Response += (object sender, DialogResponseEventArgs e) =>
                    {
                        if (e.DialogButton == DialogButton.Left)
                        {
                            player.mapCreator.Unload();
                            player.mapCreator.Load(id);
                        }
                    };
                    msg.Show(player);
				}
                else
                    player.mapCreator.Load(id);
            }
            [Command("save")]
            private static void SaveCommand(Player player)
            {
                if (!(player.mapCreator is null))
                {
                    if(player.mapCreator.editingMap.Name == "[Untitled]")
					{
                        InputDialog nameDialog = new InputDialog("Name of the map", "Please enter the name of the map", false, "Save", "Cancel");
                        nameDialog.Response += (object sender, DialogResponseEventArgs e) => {
                            if (e.DialogButton == DialogButton.Left)
                            {
                                if (e.InputText.Length < 100 && e.InputText.Length > 3)
                                {
                                    if (player.mapCreator.Save(e.InputText))
                                        player.SendClientMessage(Color.Green, "Map saved");
                                    else
                                        player.SendClientMessage(Color.Red, "Error saving map");
                                }
                                else
                                {
                                    nameDialog.Message = "Map name must have at least 3 characters and cannot exceed 100 characters";
                                    nameDialog.Show(player);
                                }
                            }
                        };
                        nameDialog.Show(player);
                    }
                    else
                    {
                        if (player.mapCreator.Save())
                            player.SendClientMessage(Color.Green, "Map saved");
                        else
                            player.SendClientMessage(Color.Red, "Error saving map");
                    }
                }
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }

            [Command("exit")]
            private static void Exit(Player player)
            {
                if (player.mapCreator != null)
                {
                    player.mapCreator.Unload();
                    player.mapCreator = null;
                    player.SendClientMessage("Map creator closed");
                }
            }
            [Command("list")]
            private static void ListCommand(Player player)
            {
                InputDialog dialog = new InputDialog("Map list", "Enter keywords (separated with space) if you want to search a specific map, otherwise let the field empty", false, "Search", "Cancel");
                dialog.Response += (object sender, DialogResponseEventArgs e) =>
                {
                    if (e.DialogButton == DialogButton.Left)
                    {
                        string[] keywords = e.InputText.Split(" ");
                        MapCreator.ShowMapList(player, keywords);
                    }
                };
                dialog.Show(player);
            }
            [Command("loaded", PermissionChecker = typeof(AdminPermissionChecker))]
            private static void ListLoadedCommand(Player player)
            {
                List<Map> maps = Map.GetAllLoadedMaps();
                ListDialog dialog = new ListDialog("List of loaded maps", "Select", "Cancel");
                if (maps.Count > 0)
                {
                    foreach (Map map in maps)
                    {
                        dialog.AddItem(map.Name + " " + map.VirtualWorld);
                    }
                    dialog.Response += (object sender, DialogResponseEventArgs e) =>
                    {
                        if (e.DialogButton == DialogButton.Left)
                        {
                            ListDialog mapMenuDialog = new ListDialog("Map menu", "Select", "Cancel");
                            mapMenuDialog.AddItem(Color.Red + "Unload");
                            mapMenuDialog.Response += (object sender, DialogResponseEventArgs e) =>
                            {
                                maps[e.ListItem].Unload();
                                player.SendClientMessage("The map has been unloaded");
                            };
                            mapMenuDialog.Show(player);
                        }
                    };
                    dialog.Show(player);
                }
                else
                    player.SendClientMessage("There is no loaded map");
            }
            [Command("addo")]
            private static void AddObjectCommand(Player player, int modelid)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.AddObject(modelid);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [Command("delo")]
            private static void DelObjectCommand(Player player, int objectid)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.DelObject(objectid);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [Command("replace")]
            private static void ReplaceCommand(Player player, int objectid, int modelid)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.ReplaceObject(objectid, modelid);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [Command("dupl")]
            private static void DuplicateCommand(Player player, int objectid)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.DuplicateObject(objectid);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [Command("marker")]
            private static void MarkerCommand(Player player, int marker)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.EditMarker(marker);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [Command("dist")]
            private static void DistCommand(Player player)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.GetMarkersDistance();
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [Command("edit")]
            private static void EditCommand(Player player, int objectid)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.EditObject(objectid);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
        }
    }
}
