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
    class AdminCommands : Player
    {
        [Command("getmodel")]
        private void GetModel(string model)
        {
            SendClientMessage("Found model: " + Utils.GetVehicleModelType(model).ToString());
        }

        [Command("vehicle", "veh", "v", DisplayName = "v")]
        private void SpawnVehicleCommand(VehicleModelType model)
        {
            Random rndColor = new Random();
            BaseVehicle v = BaseVehicle.Create(model, new Vector3(this.Position.X + 5.0, this.Position.Y, this.Position.Z), 0.0f, rndColor.Next(0, 255), rndColor.Next(0, 255));
            this.PutInVehicle(v, 0);
            SampSharp.GameMode.Events.EnterVehicleEventArgs e = new SampSharp.GameMode.Events.EnterVehicleEventArgs(this, v, false);
            this.OnEnterVehicle(e);
        }

        private BaseVehicle vMenuDialogVehicle;
        [Command("vmenu")]
        private void VehicleMenuCommand(int vehicleid)
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
                menu.Show(this);
            }
		}

        private void VehicleMenu_Response(object sender, SampSharp.GameMode.Events.DialogResponseEventArgs e)
        {
            if (e.Player.Equals(this))
            {
                if (e.DialogButton == DialogButton.Left)
                {
                    switch (e.ListItem)
                    {
                        case 0: // Eject driver
                            vMenuDialogVehicle.Driver?.RemoveFromVehicle();
                            this.Notificate("Driver ejected");
                            break;
                        case 1: // Respawn
                            vMenuDialogVehicle.Respawn();
                            this.Notificate("Vehicle respawned");
                            break;
                        case 2: // Destroy
                            vMenuDialogVehicle.Driver?.RemoveFromVehicle();
                            foreach (BasePlayer p in vMenuDialogVehicle.Passengers)
                                p?.RemoveFromVehicle();
                            vMenuDialogVehicle.Dispose();
                            this.Notificate("Vehicle destroyed");
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
                                this.Notificate("Vehicle parked");
                            }
                            else
                            {
                                param.Add("@vehicle_id", veh.DbId);
                                mySQLConnector.Execute("DELETE FROM parked_vehicles WHERE vehicle_id=@vehicle_id", param);
                                StoredVehicle.RemoveFromDbPool(vMenuDialogVehicle.Id);
                                this.Notificate("Vehicle unparked");
                            }
                            break;
                        case 4: // Doors
                            if(vMenuDialogVehicle.Doors)
                            {
                                vMenuDialogVehicle.Doors = false;
                                this.Notificate("Vehicle's doors opened");
                            }
                            else
                            {
                                vMenuDialogVehicle.Doors = true;
                                this.Notificate("Vehicle's doors locked");
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
                                                trailerIdMenu.Show(this);
                                            }
                                            else
											{
                                                if (foundV.Driver is null)
                                                {
                                                    vMenuDialogVehicle.Trailer = foundV;
                                                    this.Notificate("Vehicles attached");
                                                }
                                                else
                                                    this.SendClientMessage("You cannot attach an occupied vehicle !");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            trailerIdMenu.Show(this);
                                        }
                                    }
                                };
                                trailerIdMenu.Show(this);
                            }
                            break;
                    }
                }
            }
        }

		[Command("tp")]
        private void TP()
        {
            this.Position = new Vector3(1431.6393, 1519.5398, 10.5988);
        }
    }
}
