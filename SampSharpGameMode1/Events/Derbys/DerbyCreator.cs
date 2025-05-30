﻿using SampSharp.Core;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Events._Tools;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Events.Derbys
{
    public class DerbyCreator : EventCreator
    {
        class HUD : Display.HUD
        {
            public HUD(Player player) : base(player, "derbycreator.json")
            {
                layer.SetTextdrawText("derbynamelabel", "Derby Name:");
                layer.SetTextdrawText("derbyname", "None");
                layer.SetTextdrawText("selectedidx", "Spawn nbr:");
                layer.SetTextdrawText("editingmode", "Mode: None");
                layer.UnselectAllTextdraw();
            }
            public void Destroy()
            {
                layer.HideAll();
            }
            public void SetDerbyName(string name)
            {
                layer.SetTextdrawText("derbyname", name);
            }
            public void SetSelectedIdx(int idx)
            {
                layer.SetTextdrawText("selectedidx", idx.ToString());
            }
            public void SetEditingMode(EditingMode editingMode)
            {
                layer.SetTextdrawText("editingmode", "Mode: " + editingMode.ToString());
            }
        }
        enum EditingMode { None, SpawnPos }

        protected MySQLConnector mySQLConnector = MySQLConnector.Instance();

        Player player;
        HUD hud;

        public int EventId { get => editingDerby.Id; }
        public Derby editingDerby = null;
        EditingMode editingMode;
        public bool isNew;

        SpawnerCreator spawnerCreator;

        private DerbyPickup? lastPickedUpPickup;
        private Dictionary<int, PlayerTextLabel> pickupLabels;

        PlayerObject moverObject;
        const int moverObjectModelID = 3082;
        Vector3 moverObjectOffset = new Vector3(0.0f, 0.0f, 1.0f);

        #region PlayerEvents
        private void OnPlayerKeyStateChanged(object sender, KeyStateChangedEventArgs e)
        {
            switch (e.NewKeys)
            {
                case Keys.Submission:
                    ShowDerbyDialog();
                    break;
            }
        }
        private void OnPlayerPickUpPickup(object pickup, PlayerEventArgs e)
		{
            Logger.WriteLineAndClose("player pickup");
            DerbyPickup pickedUp = editingDerby.Pickups.Find(x => x.pickup.Id == ((DynamicPickup)pickup).Id);
            lastPickedUpPickup = pickedUp;
            ShowPickupDialog(pickedUp);
		}
        #endregion

        public DerbyCreator(Player _player)
        {
            player = _player;
            editingDerby = null;
        }

        public void Create()
        {
            editingDerby = new Derby();
            editingDerby.IsCreatorMode = true;
            editingDerby.StartingVehicle = VehicleModelType.Infernus;
            editingDerby.SpawnPoints = new List<Vector3R>();
            editingDerby.Pickups = new List<DerbyPickup>();
            editingDerby.MapId = -1;
            isNew = true;
            lastPickedUpPickup = null;
            pickupLabels = new Dictionary<int, PlayerTextLabel>();
            this.SetPlayerInEditor();
        }
        public void Load(int id)
        {
            if (id > 0)
            {
                Derby loadingDerby = new();
                loadingDerby.IsCreatorMode = true;
                loadingDerby.Loaded += LoadingDerby_Loaded;
                loadingDerby.Load(id, (int)VirtualWord.EventCreators + player.Id);
            }
            else player.SendClientMessage(Color.Red, "Error loading Derby #" + id + " (invalid ID)");
        }

        private async void LoadingDerby_Loaded(object sender, DerbyLoadedEventArgs e)
        {
            await TaskHelper.SwitchToMainThread();
            if (e.success)
            {
                isNew = false;
                lastPickedUpPickup = null;
                pickupLabels = new Dictionary<int, PlayerTextLabel>();
                editingDerby = e.derby;

                foreach (DerbyPickup pickup in editingDerby.Pickups)
                {
                    pickup.pickup.PickedUp += OnPlayerPickUpPickup;
                    pickup.IsEnabled = false;
                }
                player.SendClientMessage(Color.Green, "Derby #" + editingDerby.Id + " loaded successfully in creation mode");
                this.SetPlayerInEditor();
                pickupLabels.Clear();
                foreach (DerbyPickup pickup in e.derby.Pickups)
                {
                    pickupLabels.Add(pickup.pickup.Id, new PlayerTextLabel(player, $"Pickup #{pickup.pickup.Id}", Color.White, pickup.pickup.Position, 50.0f, false));
                }
            }
            else
                player.SendClientMessage(Color.Red, "Error loading derby (missing mandatory datas)");
        }
        private void SetPlayerInEditor()
        {
            player.VirtualWorld = (int)VirtualWord.EventCreators + player.Id;
            player.EnablePlayerCameraTarget(true);
            player.KeyStateChanged += OnPlayerKeyStateChanged;

            Vector3 pos;
            float rot = 0;
            if (editingDerby.SpawnPoints.Count > 0)
            {
                pos = editingDerby.SpawnPoints[0].Position;
                rot = editingDerby.SpawnPoints[0].Rotation;
            }
            else
                pos = player.Position;

            if (!player.InAnyVehicle)
            {
                BaseVehicle veh = BaseVehicle.Create(VehicleModelType.Infernus, pos, rot, 243, 243);
                veh.VirtualWorld = player.VirtualWorld;
                player.DisableRemoteVehicleCollisions(true);
                player.PutInVehicle(veh);
            }
            else
            {
                player.Vehicle.Position = pos;
                player.Vehicle.Angle = rot;
            }

            player.SetTime(editingDerby.Time.Hour, editingDerby.Time.Minute);

            hud = new HUD(player);
            hud.SetDerbyName(editingDerby.Name ?? "Untitled");
            editingMode = EditingMode.None;
            hud.SetEditingMode(editingMode);
        }

		public void Unload()
        {
            editingDerby?.Unload();
            editingDerby = null;
            if(pickupLabels != null)
			{
                foreach (PlayerTextLabel label in pickupLabels.Values)
                    label.Dispose();
			}
            pickupLabels = null;
            hud?.Destroy();
            hud = null;
            if (moverObject != null)
            {
                moverObject.Edited -= moverObject_Edited;
                moverObject.Dispose();
            }
            moverObject = null;

            spawnerCreator?.Unload();
            spawnerCreator = null;

            foreach (BaseVehicle veh in BaseVehicle.All)
            {
                if (veh.VirtualWorld == (int)VirtualWord.EventCreators + player.Id)
                    veh.Dispose();
            }

            if (player != null)
            {
                player.VirtualWorld = 0;
                player.SetTime(12, 0);
                player.CancelEdit();
                player.KeyStateChanged -= OnPlayerKeyStateChanged;
            }
        }

		public Boolean Save()
        {
            if (editingDerby != null)
            {
                Dictionary<string, object> param = new()
                {
                    { "@id", editingDerby.Id }
                };
                mySQLConnector.Execute("DELETE FROM derby_spawn WHERE derby_id=@id", param);
                if (spawnerCreator != null)
                    editingDerby.SpawnPoints = spawnerCreator.GetSpawnPoints();
                if (editingDerby.SpawnPoints.Count == 0)
                    player.SendClientMessage("You must place at least one spawn point (submission key) !");
                for (int i = 0; i < editingDerby.SpawnPoints.Count; i++)
                {
                    param = new Dictionary<string, object>
                    {
                        { "@id", editingDerby.Id },
                        { "@spawn_pos_x",  editingDerby.SpawnPoints[i].Position.X },
                        { "@spawn_pos_y",  editingDerby.SpawnPoints[i].Position.Y },
                        { "@spawn_pos_z",  editingDerby.SpawnPoints[i].Position.Z },
                        { "@spawn_rot",  editingDerby.SpawnPoints[i].Rotation },
                    };
                    mySQLConnector.Execute("INSERT INTO derby_spawn " +
                        "(derby_id, spawn_pos_x, spawn_pos_y, spawn_pos_z, spawn_rot) VALUES " +
                        "(@id, @spawn_pos_x, @spawn_pos_y, @spawn_pos_z, @spawn_rot)", param);
                };

                param = new Dictionary<string, object>
                {
                    { "@id", editingDerby.Id }
                };
                mySQLConnector.Execute("DELETE FROM derby_pickups WHERE derby_id=@id", param);
                for (int i = 0; i < editingDerby.Pickups.Count; i++)
                {
                    param = new Dictionary<string, object>
                    {
                        { "@id", editingDerby.Id },
                        { "@pickup_event",  editingDerby.Pickups[i].Event },
                        { "@pickup_model",  editingDerby.Pickups[i].ModelId },
                        { "@pickup_pos_x",  editingDerby.Pickups[i].Position.X },
                        { "@pickup_pos_y",  editingDerby.Pickups[i].Position.Y },
                        { "@pickup_pos_z",  editingDerby.Pickups[i].Position.Z },
                    };
                    mySQLConnector.Execute("INSERT INTO derby_pickups " +
                        "(derby_id, pickup_event, pickup_model, pickup_pos_x, pickup_pos_y, pickup_pos_z) VALUES " +
                        "(@id, @pickup_event, @pickup_model, @pickup_pos_x, @pickup_pos_y, @pickup_pos_z)", param);
                }
                param = new Dictionary<string, object>
                {
                    { "@name", editingDerby.Name },
                    { "@mapid", editingDerby.MapId == -1 ? null : editingDerby.MapId.ToString() },
                    { "@vehicleid", editingDerby.StartingVehicle },
                    { "@minheight", editingDerby.MinimumHeight },
                    { "@id", editingDerby.Id }
                };
                mySQLConnector.Execute("UPDATE derbys SET derby_name=@name, derby_map=@mapid, derby_startvehicle=@vehicleid, derby_minheight=@minheight WHERE derby_id=@id", param);
                isNew = false;
                return (mySQLConnector.RowsAffected > 0);
            }
            return false;
        }

        public Boolean Save(string name) // Only if the derby does not already exist
        {
            if (editingDerby != null && name.Length > 0)
            {
                Dictionary<string, object> param = new()
                {
                    { "@derby_name", name },
                    { "@derby_creator", player.Name },
                    { "@derby_startvehicle", editingDerby.StartingVehicle },
                    { "@derby_minheight", editingDerby.MinimumHeight },
                };
                editingDerby.Id = (int)mySQLConnector.Execute("INSERT INTO derbys " +
                    "(derby_name, derby_creator, derby_startvehicle, derby_minheight) VALUES" +
                    "(@derby_name, @derby_creator, @derby_startvehicle, @derby_minheight)", param);
                if (mySQLConnector.RowsAffected > 0)
                {
                    editingDerby.Name = name;
                    hud.SetDerbyName(name);
                    return this.Save();
                }
                else return false;
            }
            return false;
        }

        public void AddPickup(int modelid)
		{
            DerbyPickup pickup = new(modelid, Utils.GetPositionFrontOfPlayer(player), player.VirtualWorld, DerbyPickup.PickupEvent.None);
            editingDerby.Pickups.Add(pickup);
            PlayerTextLabel label = new(player, $"Pickup #{pickup.pickup.Id}", Color.White, pickup.Position + Vector3.UnitZ, 50.0f);
            pickupLabels.Add(pickup.pickup.Id, label);
		}

        public void DeletePickup(int pickupid)
        {
            DerbyPickup pickup = editingDerby.Pickups.Find(x => x.pickup.Id == pickupid);
            if (pickup != null)
            {
                pickup.pickup.Dispose();
                editingDerby.Pickups.Remove(pickup);
                pickupLabels[pickupid].Dispose();
                pickupLabels.Remove(pickupid);
                player.Notificate("Pickup Deleted");
            }
            else
                player.SendClientMessage("Unknown pickup id");
        }
        public void EditPickup(int pickupid)
        {
            DerbyPickup pickup = editingDerby.Pickups.Find(x => x.pickup.Id == pickupid);
            if (pickup != null)
            {
                ListDialog dialog = new($"Edit pickup #{pickup}", "Select", "Cancel");
                dialog.AddItem("Change model");
                dialog.AddItem("Edit event [" + Enum.GetNames(typeof(DerbyPickup.PickupEvent))[(int)pickup.Event] + "]");
                dialog.AddItem(Color.Red + "Delete");
                dialog.Response += (sender, e) =>
                {
                    if(e.DialogButton == DialogButton.Left)
                    {
                        switch(e.ListItem)
                        {
                            case 0:
                                player.SendClientMessage("This function is not implemented yet");
                                EditPickup(pickupid);
                                break;
                            case 1:
                                ListDialog eventDialog = new("Event dialog", "Select", "Cancel");
                                foreach (string evt in Enum.GetNames(typeof(DerbyPickup.PickupEvent)))
                                {
                                    if(evt == Enum.GetNames(typeof(DerbyPickup.PickupEvent))[(int)pickup.Event])
                                        eventDialog.AddItem("> " + evt);
                                    else
                                        eventDialog.AddItem(evt);
                                }
                                eventDialog.Response += (sender, e) =>
                                {
                                    if(e.DialogButton == DialogButton.Left)
                                    {
                                        pickup.Event = (DerbyPickup.PickupEvent)e.ListItem;
                                        player.Notificate("Updated");
                                        EditPickup(pickupid);
                                    }
                                };
                                eventDialog.Show(player);
                                break;
                            case 2:
                                this.DeletePickup(pickupid);
                                break;
                        }
                    }
                };
                dialog.Show(player);
            }
            else
                player.SendClientMessage("Unknown pickup id");
        }

        #region Dialogs
        private void ShowDerbyDialog()
        {
            ListDialog derbyDialog = new("Derby options", "Select", "Cancel");
            derbyDialog.AddItem("Select starting vehicle [" + editingDerby.StartingVehicle + "]");
            derbyDialog.AddItem("Edit derby name");
            derbyDialog.AddItem("Start Spawn creator");
            derbyDialog.AddItem("Load a map ...");
            derbyDialog.AddItem("Set minimum height");

            derbyDialog.Show(player);
            derbyDialog.Response += DerbyDialog_Response;
        }

        private void DerbyDialog_Response(object sender, DialogResponseEventArgs e)
        {
            if (e.Player.Equals(player))
            {
                if (e.DialogButton == DialogButton.Left)
                {
                    switch (e.ListItem)
                    {
                        case 0: // Select starting vehicle
                            {
                                InputDialog startCarNameDialog = new("Vehicle name", "Enter the vehicle name:", false, "Find", "Cancel");
                                startCarNameDialog.Show(player);
                                startCarNameDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        editingDerby.StartingVehicle = Utils.GetVehicleModelType(eventArgs.InputText);
                                        player.Notificate("Updated");
                                        ShowDerbyDialog();
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowDerbyDialog();
                                    }
                                };
                                break;
                            }
                        case 1: // Edit derby name
                            {
                                InputDialog raceNameDialog = new("Derby's name", "Enter the derby's name:", false, "Edit", "Cancel");
                                raceNameDialog.Show(player);
                                raceNameDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        editingDerby.Name = eventArgs.InputText;
                                        player.Notificate("Updated");
                                        ShowDerbyDialog();
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowDerbyDialog();
                                    }
                                };
                                break;
                            }
                        case 2: // Start Spawn creator
                            {
                                if (spawnerCreator != null)
                                {
                                    editingDerby.SpawnPoints = spawnerCreator.GetSpawnPoints();
                                    spawnerCreator.Unload();
                                    spawnerCreator = null;
                                    editingMode = EditingMode.None;
                                    hud.SetEditingMode(editingMode);
                                }
                                else
                                {
                                    List<Vector3R> spawns = new();
                                    if (editingDerby.SpawnPoints.Count == 0)
                                    {
                                        spawns.Add(new Vector3R(player.Position + Vector3.UnitX));
                                    }
                                    else
                                    {
                                        spawns = editingDerby.SpawnPoints;
                                    }
                                    spawnerCreator = new SpawnerCreator(player, player.VirtualWorld, editingDerby.StartingVehicle.GetValueOrDefault(VehicleModelType.Ambulance), spawns);
                                    spawnerCreator.Quit += (sender, e) =>
                                        {
                                            editingDerby.SpawnPoints = e.spawnPoints;
                                            player.Notificate("Spawn points updated");
                                            editingMode = EditingMode.None;
                                            hud.SetEditingMode(editingMode);
                                        };
                                    editingMode = EditingMode.SpawnPos;
                                    hud.SetEditingMode(editingMode);
                                }
                                break;
                            }
                        case 3: // Load a map
                            {
                                InputDialog findMapDialog = new("Find a map", "Type the name of the map you want to load, or empty for full list", false, "Search", "Cancel");
                                findMapDialog.Show(player);
                                findMapDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        ShowLoadMapDialog(eventArgs.InputText);
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowDerbyDialog();
                                    }
                                };
                                break;
                            }
                        case 4: // Set minimum height
                            {
                                MessageDialog minHeightDialog = new("Setting minimum height", "This setting is used to determinate when player fall from the map. Place yourself on the lowest part of your derby, when you are ready press \"SET\"", "SET", "Cancel");
                                minHeightDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        editingDerby.MinimumHeight = player.Position.Z - 10.0f;
                                        player.Notificate("Minimum height set");
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowDerbyDialog();
                                    }
                                };
                                minHeightDialog.Show(player);
                                break;
                            }
                    }
                }
            }
        }
        private void ShowLoadMapDialog(string text)
        {
            Dictionary<int, string> maps = Map.Map.FindAll(text, player);
            if (maps.Count == 0)
            {
                player.Notificate("No results");
                GameMode.MySQLConnector.CloseReader();
                ShowDerbyDialog();
            }
            else
            {
                List<int> mapList = new();
                ListDialog mapListDialog = new(maps.Count + " maps found", "Load", "Cancel");
                foreach (var map in maps)
                {
                    mapList.Add(Convert.ToInt32(map.Key));
                    mapListDialog.AddItem(map.Key + "_" + map.Value);
                }
                mapListDialog.Show(player);
                mapListDialog.Response += (sender, eventArgs) =>
                {
                    if (eventArgs.DialogButton == DialogButton.Left)
                    {
                        editingDerby.MapId = mapList[eventArgs.ListItem];
                        editingDerby.ReloadMap(() =>
                        {
                            player.SetTime(editingDerby.Time.Hour, editingDerby.Time.Minute);
                            player.Notificate("Map loaded");
                        });
                    }
                    else
                    {
                        player.Notificate("Cancelled");
                        ShowDerbyDialog();
                    }
                };
            }
        }

        private void ShowPickupDialog(DerbyPickup pickup)
		{
            ListDialog pickupDialog = new("Pickup options", "Select", "Cancel");
            pickupDialog.AddItem("Edit pickup position");
            pickupDialog.AddItem("Edit pickup model [" + pickup.ModelId + "]");
            pickupDialog.AddItem("Edit event");
			pickupDialog.Response += PickupDialog_Response;
            pickupDialog.Show(player);
        }

        private void PickupDialog_Response(object sender, DialogResponseEventArgs e)
        {
            if (e.Player.Equals(player) && lastPickedUpPickup.ModelId != 0)
            {
                if (e.DialogButton == DialogButton.Left)
                {
                    switch (e.ListItem)
                    {
                        case 0: // Edit position
                            {
                                if (moverObject == null)
                                {
                                    moverObject = new PlayerObject(
                                        player,
                                        moverObjectModelID,
                                        lastPickedUpPickup.Position + moverObjectOffset,
                                        new Vector3(0.0, 0.0, 0.0));

                                    moverObject.Edit();
                                    moverObject.Edited += moverObject_Edited;
                                }
                                else
                                {
                                    moverObject.Position = lastPickedUpPickup.Position;
                                    moverObject.Edit();
                                }
                                player.ToggleControllable(false);
                                player.cameraController.SetFree();
                                player.cameraController.SetPosition(new Vector3(moverObject.Position.X + 10.0, moverObject.Position.Y + 10.0, moverObject.Position.Z + 10.0));
                                player.cameraController.SetTarget(moverObject.Position, true);
                                player.Vehicle.Position = new Vector3(lastPickedUpPickup.Position.X + 10.0, lastPickedUpPickup.Position.Y + 10.0, lastPickedUpPickup.Position.Z + 12.0);
                                break;
                            }
                    }
                }
            }
        }
		#endregion

        private void moverObject_Edited(object sender, EditPlayerObjectEventArgs e)
        {
            if (e.EditObjectResponse == EditObjectResponse.Final)
            {
                //player.SendClientMessage("debug: object final");
                player.cameraController.SetBehindPlayer();
                player.ToggleControllable(true);
                lastPickedUpPickup.Respawn();
                moverObject.Dispose();
            }
            else if (e.EditObjectResponse == EditObjectResponse.Update)
            {
                //player.SendClientMessage("debug: object update");
                player.cameraController.MoveTo(new Vector3(e.Position.X + 10.0, e.Position.Y + 10.0, e.Position.Z + 10.0));
                player.cameraController.MoveToTarget(e.Position);
            }
            if(lastPickedUpPickup is DerbyPickup pickup)
			{
                pickup.Position = e.Object.Position;
			}
            if(e.EditObjectResponse == EditObjectResponse.Final || e.EditObjectResponse == EditObjectResponse.Cancel)
                lastPickedUpPickup = null;
        }
    }
}
