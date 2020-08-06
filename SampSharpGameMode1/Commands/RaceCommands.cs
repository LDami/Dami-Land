using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharpGameMode1.Events.Races;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Commands
{
    class RaceCommands
    {
        [CommandGroup("race")]
        class RaceCommandsClass
        {
            // Creator

            [Command("create")]
            private static void CreateRace(Player player)
            {
                player.playerRaceCreator = new RaceCreator(player);
            }

            [Command("loadc")]
            private static void LoadRaceCreator(Player player, int id)
            {
                if (player.playerRaceCreator == null)
                    player.playerRaceCreator = new RaceCreator(player);

                player.playerRaceCreator.Load(id);
            }

            [Command("save")]
            private static void SaveRace(Player player)
            {
                if (player.playerRaceCreator != null)
                {
                    if (player.playerRaceCreator.isEditing)
                    {
                        if (player.playerRaceCreator.editingRace.Name.Length > 0) // Si on édite une course déjà existante
                        {
                            if (player.playerRaceCreator.Save())
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
                        if (player.playerRaceCreator.Save(e.InputText))
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
                if (player.playerRaceCreator != null)
                {
                    player.playerRaceCreator.Unload();
                }
            }

            [Command("set start")]
            private static void SetStart(Player player)
            {
                if (player.playerRaceCreator != null)
                {
                    player.playerRaceCreator.PutStart(player.Position);
                }
            }
            [Command("set current")]
            private static void MoveCurrent(Player player)
            {
                if (player.playerRaceCreator != null)
                {
                    player.playerRaceCreator.MoveCurrent(player.Position);
                }
            }
            [Command("set finish")]
            private static void SetFinish(Player player)
            {
                if (player.playerRaceCreator != null)
                {
                    player.playerRaceCreator.PutFinish(player.Position);
                }
            }
            [Command("addcp")]
            private static void AddCP(Player player)
            {
                if (player.playerRaceCreator != null)
                {
                    player.playerRaceCreator.AddCheckpoint(player.Position);
                }
            }

            [Command("find")]
            private static void FindRace(Player player, string name)
            {
                Dictionary<string, string> result = RaceCreator.FindRace(name);
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
                Dictionary<string, string> result = RaceCreator.GetRaceInfo(id);
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

            // Launcher
            /*
            [Command("join")]
            private static void Join(Player player)
            {
                if (GameMode.raceLauncher.Join(player))
                {
                    player.SendClientMessage(Color.Green, "You joined the race !");
                }
                else
                    player.SendClientMessage(Color.Red, "You cannot join the race");
            }

            [Command("load")]
            private static void LoadRace(Player player, int id)
            {
                GameMode.raceLauncher.Load(player, id);
            }

            [Command("launchnext")]
            private static void LaunchNextRace(Player player)
            {
                if (GameMode.raceLauncher.LaunchNext())
                    player.SendClientMessage(Color.Green, "Race launched, waiting for players !");
            }

            [Command("abortnext")]
            private static void AbortNextRace(Player player)
            {
                GameMode.raceLauncher.AbortNext();
                player.SendClientMessage(Color.Green, "The next race will not be played");
            }
            */
        }
    }
}
