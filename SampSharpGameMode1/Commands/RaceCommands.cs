using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharpGameMode1.Events;
using SampSharpGameMode1.Events.Races;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Commands
{
    class RaceCommands
    {
        /* Respawn player during race */
        [Command("rr")]
        private static void RespawnCommand(Player player)
        {
            if(player.pEvent != null)
            {
                if (player.pEvent.Type == Events.EventType.Race)
                    ((Race)player.pEvent.Source).RespawnPlayerOnLastCheckpoint(player, true);
            }
        }

		/* Display a list of all the player's races */
		[Command("myraces")]
        private static void MyRacesCommand(Player player)
		{
            List<string> races = Race.GetPlayerRaceList(player);
            if (races.Count == 0)
                player.SendClientMessage("You don't have any races");
            else
            {
                ListDialog list = new ListDialog(player.Name + "'s races", "Options", "Close");
                list.AddItems(races);
				list.Response += (object sender, DialogResponseEventArgs e) =>
                {
                    if(e.DialogButton == DialogButton.Left)
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
                                            RaceCommandsClass.GetInfo(player, raceid);
                                            break;
                                        case 1: // Edit
                                            RaceCommandsClass.LoadRaceCreator(player, raceid);
                                            break;
                                        case 2: // Delete
                                            player.SendClientMessage(Color.Red + "This function is not developped yet");
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MySQLConnector.Instance().CloseReader();
                                    Logger.WriteLineAndClose("RaceCommands.cs - RaceCommands.MyRacesCommand:E: Exception raised: " + ex.Message);
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

		[Command("race")]
        private static void RaceCommandUsage(Player player)
		{
            player.SendClientMessage("Usage: /race [action]");
            player.SendClientMessage("Actions: create, loadc, save, exit, set current, teleport, addcp, find info");
		}

        [CommandGroup("race")]
        class RaceCommandsClass
        {
            [Command("help")]
            private static void HelpCommand(Player player)
			{
                Display.CommandList commandList = new Display.CommandList("Race command list");
                commandList.Add("/race create", "Create a new race");
                commandList.Add("/race loadc [id]", "Loading an existing race");
                commandList.Add("/race find [name]", "Find an existing race");
                commandList.Add("/race addcp", "Add a checkpoint");
                commandList.Add("/race set current", "Move the current checkpoint to your position");
                commandList.Add("/race teleport", "Teleport yourself to the current checkpoint");
                commandList.Add("/race prevcp", "Select the previous CP (shortcut: numpad 4)");
                commandList.Add("/race nextcp", "Select the next CP (shortcut: numpad 6)");
                commandList.Add("/race selectcp [index]", "Select the specified CP");
                commandList.Add("/race teleport", "Teleport yourself to the current checkpoint");
                commandList.Add("/race addsg", "Add a spectator group");
                commandList.Add("/race save", "Save the editing race");
                commandList.Add("/race exit", "Close the editor");
                commandList.Show(player);
			}

            [Command("create")]
            private static void CreateRace(Player player)
            {
                if (player.pEvent != null || player.mapCreator != null)
                    return;
                if (!(player.eventCreator is RaceCreator))
                {
                    player.eventCreator?.Unload();
                    player.eventCreator = new RaceCreator(player);
                }
                player.eventCreator.Create();
            }

            [Command("loadc")]
            public static void LoadRaceCreator(Player player, int id)
            {
                if (player.pEvent != null || player.mapCreator != null)
                    return;
                if (!(player.eventCreator is RaceCreator))
				{
                    player.eventCreator?.Unload();
                    player.eventCreator = new RaceCreator(player);
                }
                else
                    player.eventCreator.Unload();

                player.eventCreator.Load(id);
            }

            [Command("save")]
            private static void SaveRace(Player player)
            {
                if (player.eventCreator is RaceCreator)
                {
                    if ((player.eventCreator as RaceCreator).editingRace != null)
                    {
                        if (!(player.eventCreator as RaceCreator).isNew) // Si on édite une course déjà existante
                        {
                            if (player.eventCreator.Save())
                                player.SendClientMessage(Color.Green, $"Race {Color.Wheat}{(player.eventCreator as RaceCreator).editingRace.Name}{Color.Green} saved");
                            else
                                player.SendClientMessage(Color.Red, "Error saving race");
                        }
                        else
                        {
                            InputDialog raceName = new InputDialog("Name of the race", "Please enter the name of the race", false, "Create", "Cancel");
                            raceName.Show(player);
                            raceName.Response += RaceName_Response;
                        }
                    }
                    else
                        player.SendClientMessage("You must edit or create a race to use this command");
                }
                else
                    player.SendClientMessage("You must edit or create a race to use this command");
            }

            private static void RaceName_Response(object sender, DialogResponseEventArgs e)
            {
                Player player = (Player)e.Player;
                if (e.DialogButton != DialogButton.Right)
                {
                    if (e.InputText.Length > 0)
                    {
                        if (player.eventCreator.Save(e.InputText))
                            player.SendClientMessage(Color.Green, "Race saved");
                        else
                            player.SendClientMessage(Color.Red, "Error saving race");
                    }
                    else
                    {
                        InputDialog raceName = new InputDialog("Name of the race", "Please enter the name of the race", false, "Create", "Cancel");
                        raceName.Show(e.Player);
                        raceName.Response += RaceName_Response;
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

            [Command("set current")]
            private static void MoveCurrent(Player player)
            {
                if (player.eventCreator is RaceCreator && player.eventCreator is EventCreator)
                {
                    (player.eventCreator as RaceCreator).MoveCurrent(player.Position);
                }
            }
            [Command("teleport")]
            private static void TeleportCommand(Player player)
            {
                if (player.eventCreator is RaceCreator && player.eventCreator is EventCreator)
                {
                    (player.eventCreator as RaceCreator).TeleportToCurrent();
                }
            }
            [Command("prevcp")]
            private static void PrevCPCommand(Player player)
            {
                if (player.eventCreator is RaceCreator && player.eventCreator is EventCreator)
                {
                    (player.eventCreator as RaceCreator).SelectPreviousCP();
                }
            }
            [Command("nextcp")]
            private static void NextCPCommand(Player player)
            {
                if (player.eventCreator is RaceCreator && player.eventCreator is EventCreator)
                {
                    (player.eventCreator as RaceCreator).SelectNextCP();
                }
            }
            [Command("selectcp")]
            private static void SelectCPCommand(Player player, int index)
            {
                if (player.eventCreator is RaceCreator && player.eventCreator is EventCreator)
                {
                    (player.eventCreator as RaceCreator).SelectCP(index);
                }
            }
            [Command("addcp")]
            private static void AddCP(Player player)
            {
                if (player.eventCreator is RaceCreator && player.eventCreator is EventCreator)
                {
                    (player.eventCreator as RaceCreator).AddCheckpoint(player.Position);
                }
            }

            [Command("addsg")]
            private static void AddSG(Player player)
            {
                if (player.eventCreator is RaceCreator && player.eventCreator is EventCreator)
                {
                    player.SendClientMessage("A spectator group has been added to your virtual world, please not that is for guidance only, SGs will be created for each checkpoint during the race");
                    (player.eventCreator as RaceCreator).AddSpectatorGroup();
                }
            }

            [Command("find")]
            private static void FindRace(Player player, string name)
            {
                Dictionary<string, string> result = Race.Find(name);
                if (result.Count == 0)
                    player.SendClientMessage("No race found !");
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
                Dictionary<string, string> result = Race.GetInfo(id);
                if (result.Count == 0)
                    player.SendClientMessage("No race found !");
                else
                {
                    var infoList = new ListDialog("Race info", "Ok", "");
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
