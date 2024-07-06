using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using SampSharpGameMode1.Map;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Commands
{
    class MappingCommands
    {

        /* Display a list of all the player's maps */
        [Command("mymaps")]
        private static void MyMapsCommands(Player player)
        {
            List<string> maps = Map.Map.GetPlayerMapList(player);
            if (maps.Count == 0)
                player.SendClientMessage("You don't have any maps");
            else
            {
                ListDialog list = new(player.Name + "'s maps", "Options", "Close");
                list.AddItems(maps);
                list.Response += (object sender, DialogResponseEventArgs e) =>
                {
                    if (e.DialogButton == DialogButton.Left)
                    {
                        ListDialog actionList = new("Action", "Select", "Cancel");
                        actionList.AddItem("Infos ...");
                        actionList.AddItem("Edit");
                        actionList.AddItem("Delete");
                        actionList.Response += (object sender, DialogResponseEventArgs ev) =>
                        {
                            if (ev.DialogButton == DialogButton.Left)
                            {
                                try
                                {
                                    int mapid = Convert.ToInt32(maps[e.ListItem].Substring(0, maps[e.ListItem].IndexOf('_')));
                                    switch (ev.ListItem)
                                    {
                                        case 0: // Infos
                                            MappingCommandClass.GetInfo(player, mapid);
                                            break;
                                        case 1: // Edit
                                            MappingCommandClass.LoadCommand(player, mapid);
                                            break;
                                        case 2: // Delete
                                            player.SendClientMessage(Color.Red + "This function is not developped yet");
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MySQLConnector.Instance().CloseReader();
                                    Logger.WriteLineAndClose("MappingCommands.cs - MappingCommands.MyMapsCommands:E: Exception raised: " + ex.Message);
                                    player.SendClientMessage(Color.Red + "An error occured");
                                }
                            }
                        };
                        actionList.Show(player);
                    }
                };
                list.Show(player);
            }
        }
#if DEBUG
        [Command("deleteobj")]
        private static void DeleteObjectCommand(Player player, int modelid)
        {
            GlobalObject.Remove(player, modelid, player.Position, 500f);
            player.SendClientMessage($"Model ID remove: {ColorPalette.Secondary.Main}{modelid}");
        }
#endif
        [Command("mapping", Shortcut = "map")]
        private static void MappingCommand(Player player)
        {
            player.SendClientMessage($"Usage: {ColorPalette.Secondary.Main}/mapping [action]");
            player.SendClientMessage($"Global Actions: {ColorPalette.Secondary.Main}help, create, loadc, exit");
            player.SendClientMessage($"On map editing Actions: {ColorPalette.Secondary.Main}help, save, exit, info, object (add, delete, replace, ...), marker, dist, edit, magnet, settime");
        }
        [CommandGroup("mapping", "map")]
        class MappingCommandClass
        {
            [Command("help")]
            private static void HelpCommand(Player player)
            {
                CommandList commandList = new("Event command list");
                commandList.Add("/mapping create", "Create a new map");
                commandList.Add("/mapping loadc [id]", "Load a map");
                commandList.Add("/mapping save", "Save the map");
                commandList.Add("/mapping exit", "Close the editor (save your map first !)");
                commandList.Add("/mapping info [id]", "Display the info of a map");
                commandList.Add("/mapping group [action]", "Add, duplicate or delete a group");
                commandList.Add("/mapping object [action]", "Add, replace, delete or list objects");
                commandList.Add("/mapping marker [1-2]", "Edit the marker position to get distance");
                commandList.Add("/mapping dist", "Displays the distance between the markers");
                commandList.Add("/mapping edit [objectid]", "Edit position/rotation of object");
                commandList.Add("/mapping magnet", "Toggle magnet on objects during");
                commandList.Add("/mapping settime", "Set the time of this map (ex: \"12 00\", \"12:00\")");
                commandList.Show(player);
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
            public static void LoadCommand(Player player, int id)
            {
                if (player.pEvent != null)
                    return;
                player.mapCreator ??= new MapCreator(player);
                if(player.mapCreator.editingMap != null)
				{
                    MessageDialog msg = new("Confirm", "You are currently editing a map, do you want to close and lost all unsaved data to load the next map ?", "Yes, load", "No, I will save");
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
                if (player.mapCreator is not null)
                {
                    if(player.mapCreator.editingMap.Name == "[Untitled]")
					{
                        InputDialog nameDialog = new("Name of the map", "Please enter the name of the map", false, "Save", "Cancel");
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
                InputDialog dialog = new("Map list", "Enter keywords (separated with space) if you want to search a specific map, otherwise let the field empty", false, "Search", "Cancel");
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
                List<Map.Map> maps = Map.Map.GetAllLoadedMaps();
                ListDialog dialog = new("List of loaded maps", "Select", "Cancel");
                if (maps.Count > 0)
                {
                    foreach (Map.Map map in maps)
                    {
                        dialog.AddItem(map.Name + " " + map.VirtualWorld);
                    }
                    dialog.Response += (object sender, DialogResponseEventArgs e) =>
                    {
                        if (e.DialogButton == DialogButton.Left)
                        {
                            ListDialog mapMenuDialog = new("Map menu", "Select", "Cancel");
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

            [Command("group", "grp")]
            private static void GroupCommand(Player player)
            {
                if (player.mapCreator is not null)
                {
                    player.SendClientMessage("Usage: /mapping group [action]");
                    player.SendClientMessage("Actions: a(dd), del(ete), dupl(icate)");
                }
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [CommandGroup("group", "grp")]
            class MappingGroupCommandClass
            {
                [Command("a", "add")]
                private static void AddGroupCommand(Player player, string name)
                {
                    if (player.mapCreator is not null)
                    {
                        // TODO: Create a group
                    }
                    else
                        player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
                }
                [Command("d", "del")]
                private static void DelGroupCommand(Player player, int groupIndex)
                {
                    if (player.mapCreator is not null)
                    {
                        MessageDialog dialog = new("Delete all objects ?", "Do you want to delete all the objects from this group ? Press No to delete the group but not the objects", "Yes", "No");
                        dialog.Show(player);
                        dialog.Response += (sender, e) =>
                        {
                            if (e.DialogButton == DialogButton.Left)
                            {
                                // TODO: Delete all objects from group
                            }
                            else
                            {
                                // TODO: Delete group but not the objects
                            }
                        };
                    }
                    else
                        player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
                }
                [Command("dupl", "duplicate")]
                private static void DuplicateGroupCommand(Player player, int groupIndex)
                {
                    if (player.mapCreator is not null)
                    {
                        // TODO: Duplicate group and its objects
                    }
                    else
                        player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
                }
            }

            [Command("object", "obj")]
            private static void ObjectCommand(Player player)
            {
                if (player.mapCreator is not null)
                {
                    player.SendClientMessage("Usage: /mapping object [action]");
                    player.SendClientMessage("Actions: a(dd), del(ete), r(eplace), dupl(icate), l(ist)");
                }
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [CommandGroup("object", "obj")]
            class MappingObjectCommandClass
            {
                [Command("a", "add", UsageMessage = "Usage: /mapping obj(ect) a(dd) [modelid] ([group])")]
                private static void AddObjectCommand(Player player, int modelid)
                {
                    if (player.mapCreator is not null)
                        player.mapCreator.AddObject(modelid);
                    else
                        player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
                }
                [Command("a", "add", UsageMessage = "Usage: /mapping obj(ect) a(dd) [modelid] ([group])")]
                private static void AddObjectCommand(Player player, int modelid, int groupid)
                {
                    if (player.mapCreator is not null)
                        player.mapCreator.AddObject(modelid, groupid);
                    else
                        player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
                }
                [Command("del", "delete")]
                private static void DelObjectCommand(Player player, int objectid)
                {
                    if (player.mapCreator is not null)
                        player.mapCreator.DelObject(objectid);
                    else
                        player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
                }
                [Command("r", "replace")]
                private static void ReplaceCommand(Player player, int objectid, int modelid)
                {
                    if (player.mapCreator is not null)
                        player.mapCreator.ReplaceObject(objectid, modelid);
                    else
                        player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
                }
                [Command("dupl", "duplicate")]
                private static void DuplicateCommand(Player player, int objectid)
                {
                    if (player.mapCreator is not null)
                        player.mapCreator.DuplicateObject(objectid);
                    else
                        player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
                }
                [Command("setgroup")]
                private static void SetGroupCommand(Player player, int objectid, int groupid)
                {
                    if (player.mapCreator is not null)
                        player.mapCreator.SetObjectGroupId(objectid, groupid);
                    else
                        player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
                }
                [Command("l", "list")]
                private static void ListObjectcommand(Player player)
                {
                    if (player.mapCreator is not null)
                        player.mapCreator.ShowObjectList();
                    else
                        player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
                }
            }
            [Command("marker")]
            private static void MarkerCommand(Player player, int marker)
            {
                if (player.mapCreator is not null)
                    player.mapCreator.EditMarker(marker);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [Command("dist")]
            private static void DistCommand(Player player)
            {
                if (player.mapCreator is not null)
                    player.mapCreator.GetMarkersDistance();
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [Command("edit")]
            private static void EditCommand(Player player, int objectid)
            {
                if (player.mapCreator is not null)
                    player.mapCreator.EditObject(objectid);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [Command("magnet")]
            private static void MagnetCommand(Player player)
            {
                if (player.mapCreator is not null)
                    player.mapCreator.Magnet = !player.mapCreator.Magnet;
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }
            [Command("settime", UsageMessage = "Usage: /mapping settime [hour:minute] (ex: 12:00)")]
            private static void SetTimeCommand(Player player, string time)
            {
                if (player.mapCreator is not null)
                {
                    if (TimeOnly.TryParse(time, out TimeOnly _time))
                    {
                        player.mapCreator.SetTime(_time);
                    }
                    else
                        player.SendClientMessage(Color.Red, $"Unable to parse time from \"{time}\"");
                }
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, create or load a map first");
            }

            [Command("info")]
            public static void GetInfo(Player player, int id)
            {
                Dictionary<string, string> result = Map.Map.GetInfo(id);
                if (result.Count == 0)
                    player.SendClientMessage("No map found !");
                else
                {
                    ListDialog infoList = new("Map info", "Ok", "");
                    string str = "";
                    foreach (KeyValuePair<string, string> kvp in result)
                    {
                        str = ColorPalette.Primary.Main + kvp.Key + ": " + new Color(255, 255, 255) + kvp.Value;
                        if (str.Length >= 64)
                        {
                            infoList.AddItem(str[..63]);
                            infoList.AddItem(str[63..]);
                        }
                        else
                            infoList.AddItem(str);
                    }
                    infoList.Show(player);
                }
            }
        }
    }
}
