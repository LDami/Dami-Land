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
                if (!(player.eventCreator is DerbyCreator))
                {
                    player.eventCreator = new DerbyCreator(player);
                }
                player.eventCreator.Create();
            }

            [Command("loadc")]
            private static void LoadDerbyCreator(Player player, int id)
            {
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
                Dictionary<string, string> result = DerbyCreator.Find(name);
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
            private static void GetInfo(Player player, int id)
            {
                Dictionary<string, string> result = DerbyCreator.GetInfo(id);
                if (result.Count == 0)
                    player.SendClientMessage("No derby found !");
                else
                {
                    var infoList = new ListDialog("Derby info", "Ok", "");
                    string str = "";
                    foreach (KeyValuePair<string, string> kvp in result)
                    {
                        str = new Color(50, 50, 255) + kvp.Key + ": " + new Color(255, 255, 255) + kvp.Value;
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
