using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Commands
{
    class AdminCommands
    {
        [Command("getmodel")]
        private static void GetModel(Player player, string model)
        {
            player.SendClientMessage("Found model: " + Utils.GetVehicleModelType(model).ToString());
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
            string cmdList = 
                $"{Display.ColorPalette.Primary.Main}/vmenu [vehicleid] {Display.ColorPalette.Primary.Darken} Open Vehicle menu\n" +
                $"{Display.ColorPalette.Primary.Main}/kick /ban [player] [reason] {Display.ColorPalette.Primary.Darken} Kick or Ban player\n" +
                $"{Display.ColorPalette.Primary.Main}/freeze /unfreeze [player] {Display.ColorPalette.Primary.Darken} Freeze/Unfreeze player\n" +
                $"{Display.ColorPalette.Primary.Main}/kill [player] {Display.ColorPalette.Primary.Darken} Kill player\n" +
                $"{Display.ColorPalette.Primary.Main}/get [player] {Display.ColorPalette.Primary.Darken} Teleport player to your position\n" +
                $"{Display.ColorPalette.Primary.Main}/goto [player] {Display.ColorPalette.Primary.Darken} Teleport yourself to player\n"
                ;
            new MessageDialog("Admin command list", cmdList, "Close").Show(player);
		}

        private static BaseVehicle vMenuDialogVehicle;
        [Command("vmenu", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void VehicleMenuCommand(Player player, int vehicleid)
        {
            BaseVehicle vehicle = BaseVehicle.Find(vehicleid);

            vMenuDialogVehicle = vehicle;
            if(!(vehicle is null))
            {
                var menu = new ListDialog($"ID: {vehicle.Id} Model: {vehicle.ModelInfo.Name}", "Ok", "Close");
                menu.AddItem("Eject driver");
                menu.AddItem("Respawn");
                menu.AddItem("Destroy");
                menu.AddItem(StoredVehicle.GetStoredVehicle(vehicle.Id) is null ? "Park" : "Unpark"); // Save vehicle spawn in database
                menu.AddItem(vehicle.Doors ? $"Doors: {Color.Red}locked" : $"Doors: {Color.Green}unlocked");
                menu.AddItem(vehicle.HasTrailer ? "Detach trailer" : "Attach trailer");
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
                        StoredVehicle veh = StoredVehicle.GetStoredVehicle(vMenuDialogVehicle.Id);
                        MySQLConnector mySQLConnector = MySQLConnector.Instance();
                        Dictionary<string, object> param = new Dictionary<string, object>();
                        if (veh is null) // Park
                        {
                            param.Add("@model_id", vMenuDialogVehicle.Model);
                            param.Add("@posx", vMenuDialogVehicle.Position.X);
                            param.Add("@posy", vMenuDialogVehicle.Position.Y);
                            param.Add("@posz", vMenuDialogVehicle.Position.Z);
                            param.Add("@rot", vMenuDialogVehicle.Angle);
                            param.Add("@color1", null);
                            param.Add("@color2", null);
                            int id = (int)mySQLConnector.Execute("INSERT INTO parked_vehicles (model_id, spawn_pos_x, spawn_pos_y, spawn_pos_z, spawn_rot, color1, color2) VALUES (@model_id, @posx, @posy, @posz, @rot, @color1, @color2)", param);
                            StoredVehicle.AddDbPool(vMenuDialogVehicle.Id, id);
                            player.Notificate("Vehicle parked");
                        }
                        else
                        {
                            param.Add("@vehicle_id", veh.DbId);
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
                            InputDialog trailerIdMenu = new InputDialog("Attach trailer", "Enter trailer ID", false, "Attach", "Cancel");
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
                                    catch (Exception ex)
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

        [Command("tp")]
        private static void TP(Player player)
        {
            player.Position = new Vector3(1431.6393, 1519.5398, 10.5988);
        }
        [Command("kick", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void KickCommand(Player player, Player targetPlayer, string reason = "No reason")
        {
            Logger.WriteLineAndClose(targetPlayer.Name + " has been kicked by " + player.Name + ". Reason: " + reason);
            player.SendClientMessage(Display.ColorPalette.Primary.Main + targetPlayer.Name + Display.ColorPalette.Secondary.Main + " has been kicked");
            targetPlayer.Kick("You have been kicked by " + player.Name + ". Reason: " + reason);
        }
        [Command("ban", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void BanCommand(Player player, Player targetPlayer, string reason)
        {
            targetPlayer.SendClientMessage("You have been banned by " + player.Name + ". Reason: " + reason);
            Logger.WriteLineAndClose(targetPlayer.Name + " has been banned by " + player.Name + ". Reason: " + reason);
            player.SendClientMessage(Display.ColorPalette.Primary.Main + targetPlayer.Name + Display.ColorPalette.Secondary.Main + " has been banned");
            targetPlayer.Ban(reason);
        }
        [Command("unfreeze", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void UnfreezeCommand(Player player, Player targetPlayer)
        {
            targetPlayer.ToggleControllable(true);
            player.SendClientMessage(Display.ColorPalette.Primary.Main + targetPlayer.Name + Display.ColorPalette.Secondary.Main + " has been unfreezed");
        }
        [Command("freeze", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void FreezeCommand(Player player, Player targetPlayer)
        {
            targetPlayer.ToggleControllable(false);
            player.SendClientMessage(Display.ColorPalette.Primary.Main + targetPlayer.Name + Display.ColorPalette.Secondary.Main + " has been freezed");
        }
        [Command("kill", PermissionChecker = typeof(AdminPermissionChecker))]
        private static void KillCommand(Player player, Player targetPlayer)
        {
            targetPlayer.Health = 0;
            player.SendClientMessage(Display.ColorPalette.Primary.Main + targetPlayer.Name + Display.ColorPalette.Secondary.Main + " has been killed");
        }

        [Command("get")]
        private static void GetPlayercommand(Player player, Player targetPlayer)
        {
            targetPlayer.Teleport(player.Position);
        }
        [Command("goto")]
        private static void GotoPlayercommand(Player player, Player targetPlayer)
        {
            if (targetPlayer.VirtualWorld != player.VirtualWorld)
			{
                AdminPermissionChecker isAdmin = new AdminPermissionChecker();
                if(isAdmin.Check(player))
                    player.VirtualWorld = targetPlayer.VirtualWorld;
                else
                    player.SendClientMessage("The target is not on the same VirtualWord. Only Administrators can travel through VirtualWords.");
            }
            player.Teleport(targetPlayer.Position);
        }
    }
}
