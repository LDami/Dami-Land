using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharpGameMode1.Events.Derbys;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Commands
{
    class DerbyCommands
    {

        /* Display a list of all the player's races */
        [Command("myderbies")]
        private static void MyDerbiesCommand(Player player)
        {
            List<string> races = Derby.GetPlayerDerbyList(player);
            if (races.Count == 0)
                player.SendClientMessage("You don't have any derbies");
            else
            {
                ListDialog list = new ListDialog(player.Name + "'s derbies", "Options", "Close");
                list.AddItems(races);
                list.Response += (object sender, DialogResponseEventArgs e) =>
                {
                    if (e.DialogButton == DialogButton.Left)
                    {
                        ListDialog actionList = new ListDialog("Action", "Select", "Cancel");
                        actionList.AddItem("Infos ...");
                        actionList.AddItem("Edit");
                        actionList.AddItem("Delete");
                        actionList.Response += (object sender, DialogResponseEventArgs ev) =>
                        {
                            if (ev.DialogButton == DialogButton.Left)
                            {
                                try
                                {
                                    int raceid = Convert.ToInt32(races[e.ListItem].Substring(0, races[e.ListItem].IndexOf('_')));
                                    switch (ev.ListItem)
                                    {
                                        case 0: // Infos
                                            DerbyCommandsClass.GetInfo(player, raceid);
                                            break;
                                        case 1: // Edit
                                            DerbyCommandsClass.LoadDerbyCreator(player, raceid);
                                            break;
                                        case 2: // Delete
                                            player.SendClientMessage(Color.Red + "This function is not developped yet");
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MySQLConnector.Instance().CloseReader();
                                    Logger.WriteLineAndClose("DerbyCommands.cs - DerbyCommands.MyDerbiesCommand:E: Exception raised: " + ex.Message);
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

        [Command("derby")]
        private static void DerbyCommandUsage(Player player)
        {
            player.SendClientMessage("Usage: /derby [action]");
            player.SendClientMessage("Actions: create, loadc, save, exit, addp, delp, find, info");
        }
        [CommandGroup("derby")]
        class DerbyCommandsClass
        {
            // Creator

            [Command("create")]
            private static void CreateDerby(Player player)
            {
                if (player.pEvent != null || player.mapCreator != null)
                    return;
                if (!(player.eventCreator is DerbyCreator))
                {
                    player.eventCreator = new DerbyCreator(player);
                }
                player.eventCreator.Create();
            }

            [Command("loadc")]
            public static void LoadDerbyCreator(Player player, int id)
            {
                if (player.pEvent != null || player.mapCreator != null)
                    return;
                if (!(player.eventCreator is DerbyCreator))
                {
                    player.eventCreator?.Unload();
                    player.eventCreator = new DerbyCreator(player);
                }
                else
                    player.eventCreator.Unload();

                player.eventCreator.Load(id);
            }

            [Command("save")]
            private static void SaveRace(Player player)
            {
                if (player.eventCreator is DerbyCreator)
                {
                    if ((player.eventCreator as DerbyCreator).editingDerby != null)
                    {
                        if (!(player.eventCreator as DerbyCreator).isNew) // Si on édite une course déjà existante
                        {
                            if (player.eventCreator.Save())
                                player.SendClientMessage(Color.Green, "Derby saved");
                            else
                                player.SendClientMessage(Color.Red, "Error saving derby");
                        }
                        else
                        {
                            InputDialog derbyName = new InputDialog("Name of the derby", "Please enter the name of the derby", false, "Create", "Cancel");
                            derbyName.Show(player);
                            derbyName.Response += DerbyName_Response;
                        }
                    }
                    else
                        player.SendClientMessage("You must edit or create a race to use this command");
                }
                else
                    player.SendClientMessage("You must edit or create a race to use this command");
            }

            private static void DerbyName_Response(object sender, DialogResponseEventArgs e)
            {
                Player player = (Player)e.Player;
                if (e.DialogButton != DialogButton.Right)
                {
                    if (e.InputText.Length > 0)
                    {
                        if (player.eventCreator.Save(e.InputText))
                            player.SendClientMessage(Color.Green, "Derby saved");
                        else
                            player.SendClientMessage(Color.Red, "Error saving derby");
                    }
                    else
                    {
                        InputDialog raceName = new InputDialog("Name of the derby", "Please enter the name of the derby", false, "Create", "Cancel");
                        raceName.Show(e.Player);
                        raceName.Response += DerbyName_Response;
                    }
                }
            }

            [Command("exit")]
            private static void Exit(Player player)
            {
                if (player.eventCreator != null)
                {
                    player.eventCreator.Unload();
                    player.eventCreator = null;
                }
            }

            // Pickups
            [Command("addp")]
            private static void AddPickup(Player player, int modelid)
            {
                if (player.eventCreator is DerbyCreator)
                {
                    (player.eventCreator as DerbyCreator).AddPickup(modelid);
                }
            }

            [Command("delp")]
            private static void DeletePickup(Player player, int objectid)
            {
                if (player.eventCreator is DerbyCreator)
                {
                    (player.eventCreator as DerbyCreator).DeletePickup(objectid);
                }
            }

            [Command("editp")]
            private static void EditPickup(Player player, int objectid)
            {
                if (player.eventCreator is DerbyCreator)
                {
                    (player.eventCreator as DerbyCreator).EditPickup(objectid);
                }
            }


            [Command("find")]
            private static void FindDerby(Player player, string name)
            {
                Dictionary<string, string> result = Derby.Find(name);
                if (result.Count == 0)
                    player.SendClientMessage("No derby found !");
                else
                {
                    foreach (KeyValuePair<string, string> kvp in result)
                    {
                        player.SendClientMessage(string.Format("{0}: {1}", kvp.Key, kvp.Value));
                    }
                }
            }

            [Command("info")]
            public static void GetInfo(Player player, int id)
            {
                Dictionary<string, string> result = Derby.GetInfo(id);
                if (result.Count == 0)
                    player.SendClientMessage("No derby found !");
                else
                {
                    var infoList = new ListDialog("Derby info", "Ok", "");
                    string str = "";
                    foreach (KeyValuePair<string, string> kvp in result)
                    {
                        str = Display.ColorPalette.Primary.Main + kvp.Key + ": " + new Color(255, 255, 255) + kvp.Value;
                        if (str.Length >= 64)
                        {
                            infoList.AddItem(str.Substring(0, 63));
                            infoList.AddItem(str.Substring(63));
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
