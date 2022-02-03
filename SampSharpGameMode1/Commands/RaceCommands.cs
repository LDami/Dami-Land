﻿using SampSharp.GameMode.Definitions;
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
        [Command("rr")]
        private static void RespawnCommand(Player player)
        {
            if(player.pEvent != null)
            {
                if (player.pEvent.Type == Events.EventType.Race)
                    ((Race)player.pEvent.Source).RespawnPlayerOnLastCheckpoint(player, false);
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
            private static void HelpRace(Player player)
			{
                string list =
                    "/race create               Create a new race" + "\n" +
                    "/race loadc [id]           Loading an existing race" + "\n" +
                    "/race find [name]          Find an existing race" + "\n" +
                    "/race addcp                Add a checkpoint" + "\n" +
                    "/race set current          Move the current checkpoint to your position" + "\n" +
                    "/race teleport             Teleport yourself to the current checkpoint" + "\n" +
                    "/race save                 Save the editing race" + "\n" +
                    "/race exit                 Close the editor"
                    ;
                MessageDialog dialog = new MessageDialog("Command list", list, "Close");
                dialog.Show(player);
			}

            [Command("create")]
            private static void CreateRace(Player player)
            {
                if(!(player.eventCreator is RaceCreator))
                {
                    player.eventCreator?.Unload();
                    player.eventCreator = new RaceCreator(player);
                }
                player.eventCreator.Create();
            }

            [Command("loadc")]
            private static void LoadRaceCreator(Player player, int id)
            {
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
                                player.SendClientMessage(Color.Green, "Race saved");
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
            [Command("addcp")]
            private static void AddCP(Player player)
            {
                if (player.eventCreator is RaceCreator && player.eventCreator is EventCreator)
                {
                    (player.eventCreator as RaceCreator).AddCheckpoint(player.Position);
                }
            }

            [Command("find")]
            private static void FindRace(Player player, string name)
            {
                Dictionary<string, string> result = RaceCreator.Find(name);
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
            private static void GetInfo(Player player, int id)
            {
                Dictionary<string, string> result = RaceCreator.GetInfo(id);
                if (result.Count == 0)
                    player.SendClientMessage("No race found !");
                else
                {
                    var infoList = new ListDialog("Race info", "Ok", "");
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
