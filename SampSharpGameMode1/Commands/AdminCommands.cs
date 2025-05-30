﻿using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Display;
using SampSharpGameMode1.Events;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable IDE0051 // Disable useless private members

namespace SampSharpGameMode1.Commands
{
    class AdminCommands
    {
        [Command("promote", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void PromoteCommand(Player player, Player target)
        {
            AdminPermissionChecker isAdmin = new();
            if (!isAdmin.Check(target))
            {
                MessageDialog confirm = new("Do you confirm ?", $"You will promote {target.Name} as administrator, do you confirm ?", "Yes", "Cancel");
                confirm.Response += (sender, e) =>
                {
                    if(e.DialogButton == DialogButton.Left)
                    {
                        target.Adminlevel = 1;
                        target.SaveAccount();
                        player.SendClientMessage($"{target.Name} is now admin");
                        target.SendClientMessage($"You have been promoted as administrator by {player.Name}");
                        Logger.WriteLineAndClose($"[Admin] {player.Name} promoted {target.Name}");
                    }
                };
                confirm.Show(player);
            }
            else
                player.SendClientMessage($"{target.Name} is already admin");
        }
        [Command("demote", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void DemoteCommand(Player player, Player target)
        {
            AdminPermissionChecker isAdmin = new();
            if (isAdmin.Check(target))
            {
                MessageDialog confirm = new("Do you confirm ?", $"You will demote {target.Name} as administrator, do you confirm ?", "Yes", "Cancel");
                confirm.Response += (sender, e) =>
                {
                    if (e.DialogButton == DialogButton.Left)
                    {
                        target.Adminlevel = 0;
                        target.SaveAccount();
                        player.SendClientMessage($"{target.Name} is not admin anymore");
                        target.SendClientMessage($"You have been demoted as administrator by {player.Name}");
                        Logger.WriteLineAndClose($"[Admin] {player.Name} demoted {target.Name}");
                    }
                };
                confirm.Show(player);
            }
            else
                player.SendClientMessage($"{target.Name} is not admin");
        }
        [Command("reload-td", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void ReloadTextdraws(Player player)
        {
            player.SendClientMessage("Reloading all player's textdraws ...");
            foreach(Player p in Player.All.Cast<Player>())
            {
                p.Speedometer.Hide();
                p.Speedometer = null;
                p.Speedometer = new Speedometer(p);
                p.AirplaneHUD = null;
                p.AirplaneHUD = new AirplaneHUD(p);
                if (p.InAnyVehicle)
                {
                    p.Speedometer.Show();
                    if (VehicleModelInfo.ForVehicle(p.Vehicle).Category == VehicleCategory.Airplane)
                        p.AirplaneHUD.Show();
                }
                p.AnnounceHUD.Unload();
                p.AnnounceHUD = null;
                p.AnnounceHUD = new AnnounceHUD(p);
                if(EventManager.Instance().openedEvent != null)
                {
                    p.AnnounceHUD.Open(EventManager.Instance().openedEvent);
                }
            }
            player.SendClientMessage("Done.");
        }
        [Command("reload-zones", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void ReloadZones(Player player)
        {
            player.SendClientMessage("Reloading all zones ...");
            Zone.InitZones();
            player.SendClientMessage("Done.");
        }
        [Command("reload-works", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void ReloadWorks(Player player)
        {
            player.SendClientMessage("Reloading all works ...");
            Works.TruckWork.Dispose();
            Works.TramWork.Dispose();
            Works.TruckWork.Init();
            Works.TramWork.Init();
            player.SendClientMessage("Done.");
        }
        [Command("reload-objectlist", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void ReloadObjects(Player player)
        {
            player.SendClientMessage("Reloading object list ...");
            GameMode.ExtractMapObjectList();
            player.SendClientMessage("Done.");
        }
        [Command("getmodel")]
        private static void GetModel(Player player, string model)
        {
            player.SendClientMessage("Found model: " + Utils.GetVehicleModelType(model).ToString());
        }


        [Command("interior-preview")]
        private static void InteriorPreviewCommand(Player player, int id = 0)
        {
            player.InteriorPreview.Display(id);
        }
        [Command("interior-log")]
        private static void InteriorLogCommand(Player player)
        {
            Logger.WriteLineAndClose(player.InteriorPreview.GetInterior().ToString());
        }

        [Command("recordnpc")]
        private static void RecordNPC(Player player, string name)
        {
            player.SendClientMessage(Color.Red, "The function to create NPC is not implemented in Open.MP yet");

            MessageDialog confirm = new("Create NPC", "You are about to start the recording of NPC file, do you want to continue recording ?", "Yes", "No/Cancel");
            confirm.Response += (sender, args) =>
            {
                if (args.DialogButton == DialogButton.Left)
                {
                    player.StartRecordingPlayerData(player.InAnyVehicle ? PlayerRecordingType.Driver : PlayerRecordingType.OnFoot, name);
                    player.SendClientMessage($"NPC Recording ... Write {ColorPalette.Secondary.Main}/stoprecord{Color.White} to stop the record");
                }
            };
            confirm.Show(player);
        }

#if DEBUG
        [Command("admin")]
        private static void AdminTmp(Player player)
        {
            player.Adminlevel = 1;
            player.SendClientMessage("You are admin now");
        }
#endif
        [Command("acmds")]
        private static void AdminCommandsListCommand(Player player)
        {
            CommandList commandList = new("Admin command list");
            commandList.Add("/vmenu [vehicleid]", "Open Vehicle menu");
            commandList.Add("/kick [player] [reason]", "Kick a player");
            commandList.Add("/ban [player] [reason]", "Ban a player");
            commandList.Add("/(un)freeze [player]", "Freeze/Unfreeze player");
            commandList.Add("/kill [player]", "Kill player");
            commandList.Add("/get [player]", "Teleport player to your position");
            commandList.Add("/goto [player]", "Teleport yourself to player");
            commandList.Add("/clearveh", "Respawn all vehicles");
            commandList.Add("/whereis [player]", "Get the position and virtualworld of player");
            commandList.Add("/spec [player]", "Spectate player");
            commandList.Add("/pickup", "List available pickups");
            commandList.Add("/map loaded", "List all loaded maps");
            commandList.Show(player);
        }

        [Command("clearveh", PermissionChecker = typeof(AdminPermissionChecker))]
#pragma warning disable IDE0060 // Unused parameter
        private static void ClearVehCommand(Player player)
#pragma warning restore IDE0060 // Unused parameter
        {
            foreach (BaseVehicle veh in BaseVehicle.All)
            {
                if(veh.Driver == null)
                {
                    if (StoredVehicle.GetVehicleDbId(veh.Id) == -1)
                        veh.Dispose();
                    else
                        veh.Respawn();
                }
            }
        }

        private static BaseVehicle vMenuDialogVehicle;
        [Command("vmenu", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void VehicleMenuCommand(Player player, int vehicleid)
        {
            BaseVehicle vehicle = BaseVehicle.Find(vehicleid);

            vMenuDialogVehicle = vehicle;
            if(vehicle is not null)
            {
                var menu = new ListDialog($"ID: {vehicle.Id} Model: {vehicle.ModelInfo.Name}", "Ok", "Close");
                menu.AddItem("Eject driver");
                menu.AddItem("Respawn");
                menu.AddItem("Destroy");
                menu.AddItem(StoredVehicle.GetVehicleDbId(vehicle.Id) == -1 ? "Park" : "Unpark"); // Save vehicle spawn in database
                menu.AddItem(vehicle.Doors ? $"Doors: {Color.Red}locked" : $"Doors: {Color.Green}unlocked");
                menu.AddItem(vehicle.HasTrailer ? "Detach trailer" : "Attach trailer");
                menu.AddItem("Set been occupied");
				menu.Response += VehicleMenu_Response;
                menu.Show(player);
            }
		}

        private static void VehicleMenu_Response(object sender, SampSharp.GameMode.Events.DialogResponseEventArgs e)
        {
            if (e.DialogButton == DialogButton.Left)
            {
                Player player = ((Player)e.Player);
                switch (e.ListItem)
                {
                    case 0: // Eject driver
                        vMenuDialogVehicle.Driver?.RemoveFromVehicle();
                        player.Notificate("Driver ejected");
                        break;
                    case 1: // Respawn
                        vMenuDialogVehicle.Respawn();
                        player.Notificate("Vehicle respawned");
                        break;
                    case 2: // Destroy
                        vMenuDialogVehicle.Driver?.RemoveFromVehicle();
                        foreach (BasePlayer p in vMenuDialogVehicle.Passengers)
                            p?.RemoveFromVehicle();
                        vMenuDialogVehicle.Dispose();
                        player.Notificate("Vehicle destroyed");
                        break;
                    case 3: // Park/Unpark
                        int vDbId = StoredVehicle.GetVehicleDbId(vMenuDialogVehicle.Id);
                        MySQLConnector mySQLConnector = MySQLConnector.Instance();
                        Dictionary<string, object> param = new();
                        if (vDbId == -1) // Park
                        {
                            param.Add("@model_id", vMenuDialogVehicle.Model);
                            param.Add("@posx", vMenuDialogVehicle.Position.X);
                            param.Add("@posy", vMenuDialogVehicle.Position.Y);
                            param.Add("@posz", vMenuDialogVehicle.Position.Z);
                            param.Add("@rot", vMenuDialogVehicle.Angle);
                            vMenuDialogVehicle.GetColor(out int color1, out int color2);
                            param.Add("@color1", color1);
                            param.Add("@color2", color2);
                            int id = (int)mySQLConnector.Execute("INSERT INTO parked_vehicles (model_id, spawn_pos_x, spawn_pos_y, spawn_pos_z, spawn_rot, color1, color2) VALUES (@model_id, @posx, @posy, @posz, @rot, @color1, @color2)", param);
                            StoredVehicle.AddDbPool(vMenuDialogVehicle.Id, id);
                            player.Notificate("Vehicle parked");
                        }
                        else
                        {
                            param.Add("@vehicle_id", vDbId);
                            mySQLConnector.Execute("DELETE FROM parked_vehicles WHERE vehicle_id=@vehicle_id", param);
                            StoredVehicle.RemoveFromDbPool(vMenuDialogVehicle.Id);
                            player.Notificate("Vehicle unparked");
                        }
                        break;
                    case 4: // Doors
                        if(vMenuDialogVehicle.Doors)
                        {
                            vMenuDialogVehicle.Doors = false;
                            player.Notificate("Vehicle's doors opened");
                        }
                        else
                        {
                            vMenuDialogVehicle.Doors = true;
                            player.Notificate("Vehicle's doors locked");
                        }
                        break;
                    case 5: // Trailer
                        if(vMenuDialogVehicle.HasTrailer)
						{
                            vMenuDialogVehicle.Trailer = null;
						}
                        else
						{
                            InputDialog trailerIdMenu = new("Attach trailer", "Enter trailer ID", false, "Attach", "Cancel");
                            trailerIdMenu.Response += (object sender, SampSharp.GameMode.Events.DialogResponseEventArgs e) =>
                            {
                                if(e.DialogButton == DialogButton.Left)
                                {
                                    try
                                    {
                                        int id = int.Parse(e.InputText);
                                        BaseVehicle foundV = BaseVehicle.Find(id);
                                        if (foundV is null)
                                        {
                                            trailerIdMenu.Message = "Invalid Id !";
                                            trailerIdMenu.Show(player);
                                        }
                                        else
										{
                                            if (foundV.Driver is null)
                                            {
                                                vMenuDialogVehicle.Trailer = foundV;
                                                player.Notificate("Vehicles attached");
                                            }
                                            else
                                                player.SendClientMessage("You cannot attach an occupied vehicle !");
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        trailerIdMenu.Show(player);
                                    }
                                }
                            };
                            trailerIdMenu.Show(player);
                        }
                        break;
                }
            }
        }
        [Command("kick", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void KickCommand(Player player, Player targetPlayer, string reason = "No reason")
        {
            Logger.WriteLineAndClose(targetPlayer.Name + " has been kicked by " + player.Name + ". Reason: " + reason);
            player.SendClientMessage(ColorPalette.Primary.Main + targetPlayer.Name + ColorPalette.Secondary.Main + " has been kicked");
            targetPlayer.Kick("You have been kicked by " + player.Name + ". Reason: " + reason);
        }
        [Command("ban", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void BanCommand(Player player, Player targetPlayer, string reason)
        {
            targetPlayer.SendClientMessage("You have been banned by " + player.Name + ". Reason: " + reason);
            Logger.WriteLineAndClose(targetPlayer.Name + " has been banned by " + player.Name + ". Reason: " + reason);
            player.SendClientMessage(ColorPalette.Primary.Main + targetPlayer.Name + ColorPalette.Secondary.Main + " has been banned");
            targetPlayer.Ban(reason);
        }
        [Command("unfreeze", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void UnfreezeCommand(Player player, Player targetPlayer)
        {
            targetPlayer.ToggleControllable(true);
            player.SendClientMessage(ColorPalette.Primary.Main + targetPlayer.Name + ColorPalette.Secondary.Main + " has been unfreezed");
        }
        [Command("freeze", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void FreezeCommand(Player player, Player targetPlayer)
        {
            targetPlayer.ToggleControllable(false);
            player.SendClientMessage(ColorPalette.Primary.Main + targetPlayer.Name + ColorPalette.Secondary.Main + " has been freezed");
        }
        [Command("kill", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void KillCommand(Player player, Player targetPlayer)
        {
            targetPlayer.Health = 0;
            player.SendClientMessage(ColorPalette.Primary.Main + targetPlayer.Name + ColorPalette.Secondary.Main + " has been killed");
        }

        [Command("vw", "virtualworld", DisplayName = "vw")]
        private static void VWCommand(Player player, int virtualworld)
        {
            AdminPermissionChecker isAdmin = new();
            if (player.IsInEvent && !isAdmin.Check(player))
            {
                player.SendClientMessage(Color.Red + "You cannot use this command during events");
            }
            else if (player.eventCreator != null)
            {
                player.SendClientMessage(Color.Red + "You cannot use this command now, close your creator first");
            }
            else
            {
                if (virtualworld == (int)VirtualWord.Main || virtualworld == (int)VirtualWord.Players + player.Id || isAdmin.Check(player))
                {
                    player.VirtualWorld = virtualworld;
                    player.Notificate("World ~g~" + virtualworld, 1);
                    if (virtualworld != (int)VirtualWord.Main)
                        player.SendClientMessage($"You have been teleported to a new VirtualWorld ! Type {ColorPalette.Primary.Main}/vw 0{Color.White} to go back to the main world");
                }
                else
                    player.SendClientMessage("Invalid VirtualWorld id");
            }
        }
        [Command("vw", "virtualworld", DisplayName = "vw")]
        private static void VWCommand(Player player, VirtualWord virtualworld)
        {
            AdminPermissionChecker isAdmin = new();
            if (player.IsInEvent && !isAdmin.Check(player))
            {
                player.SendClientMessage(Color.Red + "You cannot use this command during events");
            }
            else if (player.eventCreator != null)
            {
                player.SendClientMessage(Color.Red + "You cannot use this command now, close your creator first");
            }
            else
            {
                if (Enum.IsDefined(typeof(VirtualWord), virtualworld) && virtualworld < VirtualWord.Players || isAdmin.Check(player)) // Cannot teleport to a list of virualworlds like Players or Events
                {
                    player.VirtualWorld = (int)virtualworld;
                    player.Notificate("World ~g~" + virtualworld, 1);
                    if (virtualworld != VirtualWord.Main)
                        player.SendClientMessage($"You have been teleported to a new VirtualWorld ! Type {ColorPalette.Primary.Main}/vw 0{Color.White} to go back to the main world");
                }
                else
                    player.SendClientMessage("Invalid VirtualWorld id");
            }
        }

        [Command("get")]
        private static void GetPlayerCommand(Player player, Player targetPlayer)
        {
            if (player.IsInEvent)
            {
                player.SendClientMessage(Color.Red + "You cannot use this command during events");
            }
            else if (player.eventCreator != null)
            {
                player.SendClientMessage(Color.Red + "You cannot use this command now, close your creator first");
            }
            else
            {
                if (targetPlayer.VirtualWorld != player.VirtualWorld)
                {
                    targetPlayer.VirtualWorld = player.VirtualWorld;
                    targetPlayer.SendClientMessage($"You have been teleported to a new VirtualWorld ! Type {ColorPalette.Primary.Main}/vw 0{Color.White} to go back to the main world");
                }
                targetPlayer.Teleport(player.Position + Vector3.UnitZ);
            }
        }
        [Command("goto")]
        private static void GotoPlayerCommand(Player player, Player targetPlayer)
        {
            if (player.IsInEvent)
            {
                player.SendClientMessage(Color.Red + "You cannot use this command during events");
            }
            else if (player.eventCreator != null)
            {
                player.SendClientMessage(Color.Red + "You cannot use this command now, close your creator first");
            }
            else
            {
                if (targetPlayer.VirtualWorld != player.VirtualWorld)
                {
                    AdminPermissionChecker isAdmin = new();
                    if (isAdmin.Check(player))
                    {
                        player.VirtualWorld = targetPlayer.VirtualWorld;
                        player.SendClientMessage($"You have been teleported to a new VirtualWorld ! Type {ColorPalette.Primary.Main}/vw 0{Color.White} to go back to the main world");
                        player.Teleport(targetPlayer.Position + Vector3.UnitZ);
                    }
                    else
                        player.SendClientMessage("The target is not on the same VirtualWorld. Only Administrators can travel through VirtualWorlds.");
                }
                else
                    player.Teleport(targetPlayer.Position + Vector3.UnitZ);
            }
        }
        [Command("whereami")]
        private static void WhereAmICommand(Player player)
        {
            player.SendClientMessage($"Position: {player.Position} ; VirtualWorld: {player.VirtualWorld} ; Zone: {Zone.GetDetailedZoneName(player.Position)}");
        }
        [Command("whereis")]
        private static void WhereIsCommand(Player player, Player targetPlayer)
        {
            player.SendClientMessage($"Position of {targetPlayer.Name}: {targetPlayer.Position} ; VirtualWorld: {targetPlayer.VirtualWorld} ; Zone: {Zone.GetDetailedZoneName(targetPlayer.Position)}");
        }
        [Command("whereis")]
        private static void WhereIsCommand(Player player, float x, float y, float z)
        {
            DynamicCheckpoint checkpoint = new(new Vector3(x, y, z), 5f, 0, streamdistance: 3000);
            checkpoint.ShowForPlayer(player);
            checkpoint.Enter += (sender, evt) =>
            {
                checkpoint.Dispose();
            };
            player.SendClientMessage($"A checkpoint has been created on position {new Vector3(x, y, z)}");
        }
        [Command("pickup", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void PickupCommand(Player player)
        {
            player.SendClientMessage("Available pickups:");
            int length = Enum.GetValues(typeof(Events.Derbys.DerbyPickupRandomEvent.AvailableEvents)).Length;
            for(int i=0; i < length; i++)
            {
                player.SendClientMessage(i + ": " + Enum.GetValues(typeof(Events.Derbys.DerbyPickupRandomEvent.AvailableEvents)).GetValue(i));
            }
        }
        [Command("pickup", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void PickupCommand(Player player, int id)
        {
            _ = new Events.Derbys.DerbyPickupRandomEvent(player, id);
        }
        [Command("ak47", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void AK47Command(Player player)
        {
            player.GiveWeapon(Weapon.AK47, 200);
        }
        [Command("spec", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void SpecCommand(Player player, Player targetPlayer)
        {
            player.LastPositionBeforeSpectate = new Vector3R(player.Position, player.Angle);
            if (player.InAnyVehicle)
            {
                player.LastVehicleUsedBeforeSpectate = player.Vehicle;
                player.LastVehicleSeatUsedBeforeSpectate = player.VehicleSeat;
            }
            else
                player.LastVehicleUsedBeforeSpectate = null;

            player.ToggleSpectating(true);
            player.SpectatingTarget = targetPlayer;
            player.SpectatePlayer(targetPlayer);
            targetPlayer.Spectators.Add(player);
            player.Notificate("Spectating " + targetPlayer.Name);
            player.SendClientMessage($"{ColorPalette.Primary.Main}Type {ColorPalette.Secondary.Main}/specoff{ColorPalette.Primary.Main} to switch off the spectate mode.");
        }
        [Command("specoff", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void SpecOffCommand(Player player)
        {
            player.SpectatingTarget.Spectators.Remove(player);
            player.ToggleSpectating(false);
            player.Position = player.LastPositionBeforeSpectate.Position;
            player.Angle = player.LastPositionBeforeSpectate.Rotation;
            if(player.LastVehicleUsedBeforeSpectate != null)
            {
                player.PutInVehicle(player.LastVehicleUsedBeforeSpectate, player.LastVehicleSeatUsedBeforeSpectate);
            }
        }
    }
}
