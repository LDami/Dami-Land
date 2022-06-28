using Newtonsoft.Json;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Display;
using SampSharpGameMode1.Events._Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SampSharpGameMode1.Events.Derbys
{
	public class DerbyCreator : EventCreator
    {
        class HUD
        {
            TextdrawLayer layer;
            public HUD(Player player)
            {
                layer = new TextdrawLayer();
                string filename = Directory.GetCurrentDirectory() + "\\scriptfiles\\derbycreator.json";
                string jsonData = "";
                if (File.Exists(filename))
                {
                    try
                    {
                        using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read))
                        {
                            byte[] output = new byte[fs.Length];
                            int idx = 0;
                            int blockLength = 1;
                            byte[] tmp = new byte[blockLength];
                            int readBytes;
                            while ((readBytes = fs.Read(tmp, 0, blockLength)) > 0)
                            {
                                for (int i = 0; i < readBytes; i++)
                                    output[idx + i] = tmp[i];
                                idx += readBytes;
                            }
                            jsonData = new UTF8Encoding(true).GetString(output);
                            List<textdraw> textdraws = JsonConvert.DeserializeObject<List<textdraw>>(jsonData);
                            foreach (textdraw textdraw in textdraws)
                            {
                                if (textdraw.Type.Equals("box"))
                                {
                                    layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Box);
                                    layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY));
                                    layer.SetTextdrawSize(textdraw.Name, textdraw.Width, textdraw.Height);
                                }
                                if (textdraw.Type.Equals("text"))
                                {
                                    layer.CreateTextdraw(player, textdraw.Name, TextdrawLayer.TextdrawType.Text);
                                    layer.SetTextdrawPosition(textdraw.Name, new Vector2(textdraw.PosX, textdraw.PosY));
                                }
                            }
                            layer.SetTextdrawText("derbynamelabel", "Derby Name:");
                            layer.SetTextdrawText("derbyname", "None");
                            layer.SetTextdrawText("selectedidx", "Spawn nbr:");
                            layer.SetTextdrawText("editingmode", "Mode: None");
                            layer.UnselectAllTextdraw();
                            fs.Close();
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("DerbyCreator.cs - DerbyCreator.HUD._:E: Cannot load Derby Creator HUD:");
                        Console.WriteLine(e.Message);
                    }
                    catch(TextdrawNameNotFoundException e)
                    {
                        Console.WriteLine("DerbyCreator.cs - DerbyCreator.HUD._:E: Cannot load Derby Creator HUD:");
                        Console.WriteLine(e.Message);
                    }
                }
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
                layer.SetTextdrawText("editingmode", editingMode.ToString());
            }
        }
        enum EditingMode { Mapping, SpawnPos }

        protected MySQLConnector mySQLConnector = MySQLConnector.Instance();

        Player player;
        HUD hud;

        public Derby editingDerby = null;
        EditingMode editingMode;
        public bool isNew;

        SpawnerCreator spawnerCreator;

        private int lastSelectedObjectId;
        private DerbyPickup? lastPickedUpPickup;
        private Dictionary<int, PlayerTextLabel> pickupLabels;

        PlayerObject moverObject;
        const int moverObjectModelID = 3082;
        Vector3 moverObjectOffset = new Vector3(0.0f, 0.0f, 1.0f);

        Map map = null;

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
            isNew = true;
            lastSelectedObjectId = -1;
            lastPickedUpPickup = null;
            pickupLabels = new Dictionary<int, PlayerTextLabel>();
            this.SetPlayerInEditor();
        }
        public void Load(int id)
        {
            if (id > 0)
            {
                Derby loadingDerby = new Derby();
                loadingDerby.IsCreatorMode = true;
                loadingDerby.Loaded += LoadingDerby_Loaded;
                loadingDerby.Load(id);
            }
            else player.SendClientMessage(Color.Red, "Error loading Derby #" + id + " (invalid ID)");
        }

        private void LoadingDerby_Loaded(object sender, DerbyLoadedEventArgs e)
        {
            if (e.success)
            {
                isNew = false;
                lastSelectedObjectId = -1;
                lastPickedUpPickup = null;
                pickupLabels = new Dictionary<int, PlayerTextLabel>();
                editingDerby = e.derby;

                if (map != null)
                    map.Unload();
                else
                    map = new Map();

                map.Loaded += (sender, eventArgs) =>
                {
                    editingDerby.MapId = eventArgs.map.Id;
                    player.SendClientMessage(ColorPalette.Primary.Main + "The map " + Color.White + eventArgs.map.Name + ColorPalette.Primary.Main + " has been loaded");
                };
                map.Load(editingDerby.MapId, (int)VirtualWord.EventCreators + player.Id);

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
                BaseVehicle veh = BaseVehicle.Create(VehicleModelType.Infernus, pos, rot, 1, 1);
                veh.VirtualWorld = player.VirtualWorld;
                player.DisableRemoteVehicleCollisions(true);
                player.PutInVehicle(veh);
            }
            else
            {
                player.Vehicle.Position = pos;
                player.Vehicle.Angle = rot;
            }

            hud = new HUD(player);
            hud.SetDerbyName(editingDerby.Name ?? "Untitled");
            editingMode = EditingMode.Mapping;
            hud.SetEditingMode(editingMode);
        }

		public void Unload()
        {
            if(editingDerby != null)
            {
                foreach (DerbyPickup pickup in editingDerby.Pickups)
                    pickup.pickup.Dispose();
                editingDerby.Pickups.Clear();
                editingDerby.Pickups = null;
            }
            editingDerby = null;
            if(pickupLabels != null)
			{
                foreach (PlayerTextLabel label in pickupLabels.Values)
                    label.Dispose();
			}
            pickupLabels = null;
            if (hud != null)
                hud.Destroy();
            hud = null;
            if (moverObject != null)
            {
                moverObject.Edited -= moverObject_Edited;
                moverObject.Dispose();
            }
            moverObject = null;

            if (spawnerCreator != null)
            {
                spawnerCreator.Unload();
                spawnerCreator = null;
            }

            foreach (BaseVehicle veh in BaseVehicle.All)
            {
                if (veh.VirtualWorld == (int)VirtualWord.EventCreators + player.Id)
                    veh.Dispose();
            }

            if (map != null)
                map.Unload();

            if (player != null)
            {
                player.VirtualWorld = 0;
                player.CancelEdit();
                player.KeyStateChanged -= OnPlayerKeyStateChanged;
            }
        }

		public Boolean Save()
        {
            if (editingDerby != null)
            {
                Dictionary<string, object> param = new Dictionary<string, object>
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
                    { "@id", editingDerby.Id }
                };
                mySQLConnector.Execute("UPDATE derbys SET derby_name=@name, derby_map=@mapid WHERE derby_id=@id", param);
                return (mySQLConnector.RowsAffected > 0);
            }
            return false;
        }

        public Boolean Save(string name) // Only if the derby does not already exist
        {
            if (editingDerby != null && name.Length > 0)
            {
                Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@derby_name", name },
                    { "@derby_creator", player.Name },
                    { "@derby_startvehicle", editingDerby.StartingVehicle }
                };
                editingDerby.Id = (int)mySQLConnector.Execute("INSERT INTO derbys " +
                    "(derby_name, derby_creator, derby_startvehicle) VALUES" +
                    "(@derby_name, @derby_creator, @derby_startvehicle)", param);
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
            DerbyPickup pickup = new DerbyPickup(modelid, Utils.GetPositionFrontOfPlayer(player), player.VirtualWorld, DerbyPickup.PickupEvent.None);
            editingDerby.Pickups.Add(pickup);
            PlayerTextLabel label = new PlayerTextLabel(player, $"Pickup #{pickup.pickup.Id}", Color.White, pickup.Position + Vector3.UnitZ, 50.0f);
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
                ListDialog dialog = new ListDialog($"Edit pickup #{pickup}", "Select", "Cancel");
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
                                ListDialog eventDialog = new ListDialog("Event dialog", "Select", "Cancel");
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
            ListDialog derbyDialog = new ListDialog("Derby options", "Select", "Cancel");
            derbyDialog.AddItem("Select starting vehicle [" + editingDerby.StartingVehicle + "]");
            derbyDialog.AddItem("Edit derby name");
            derbyDialog.AddItem("Start Spawn creator");
            derbyDialog.AddItem("Load a map ...");

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
                                InputDialog startCarNameDialog = new InputDialog("Vehicle name", "Enter the vehicle name:", false, "Find", "Cancel");
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
                                InputDialog raceNameDialog = new InputDialog("Derby's name", "Enter the derby's name:", false, "Edit", "Cancel");
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
                                }
                                else
                                {
                                    List<Vector3R> spawns = new List<Vector3R>();
                                    if (editingDerby.SpawnPoints.Count == 0)
                                    {
                                        spawns.Add(new Vector3R(player.Position));
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
                                        };
                                }
                                break;
							}
                        case 3:
                            {
                                InputDialog findMapDialog = new InputDialog("Find a map", "Type the name of the map you want to load, or empty for full list", false, "Search", "Cancel");
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
                    }
                }
            }
        }
        private void ShowLoadMapDialog(string text)
        {
            Dictionary<int, string> maps = Map.FindAll(text);
            if (maps.Count == 0)
            {
                player.Notificate("No results");
                GameMode.mySQLConnector.CloseReader();
                ShowDerbyDialog();
            }
            else
            {
                List<int> mapList = new List<int>();
                ListDialog mapListDialog = new ListDialog(maps.Count + " maps found", "Load", "Cancel");
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
                        if (map != null)
                            map.Unload();
                        else
                            map = new Map();

                        map.Loaded += (sender, eventArgs) =>
                        {
                            editingDerby.MapId = eventArgs.map.Id;
                            player.SendClientMessage(ColorPalette.Primary.Main + "The map " + Color.White + eventArgs.map.Name + ColorPalette.Primary.Main + " has been loaded");
                        };
                        map.Load(mapList[eventArgs.ListItem], player.VirtualWorld);
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
            ListDialog pickupDialog = new ListDialog("Pickup options", "Select", "Cancel");
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

        public static Dictionary<string, string> Find(string str)
        {
            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@name", str }
                };
            mySQLConnector.OpenReader("SELECT derby_id, derby_name FROM derbys WHERE derby_name LIKE @name", param);
            Dictionary<string, string> results = mySQLConnector.GetNextRow();
            mySQLConnector.CloseReader();
            return results;
        }
        public static Dictionary<string, string> GetInfo(int id)
        {
            // id, name, creator, type, number of spawn points, number of pickups, number of map objects
            Dictionary<string, string> results = new Dictionary<string, string>();
            Dictionary<string, string> row;
            bool exists = false;

            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@id", id }
                };

            mySQLConnector.OpenReader("SELECT derby_id, derby_name, derby_creator FROM derbys WHERE derby_id = @id", param);

            row = mySQLConnector.GetNextRow();
            if (row.Count > 0) exists = true;
            foreach (KeyValuePair<string, string> kvp in row)
                results.Add(MySQLConnector.Field.GetFieldName(kvp.Key), kvp.Value);

            mySQLConnector.CloseReader();

            if(exists)
            {
                mySQLConnector.OpenReader("SELECT COUNT(*) as nbr " +
                    "FROM derby_spawn WHERE derby_id = @id", param);
                row = mySQLConnector.GetNextRow();
                results.Add("Spawn points", row["nbr"]);
                mySQLConnector.CloseReader();

                mySQLConnector.OpenReader("SELECT COUNT(*) as nbr " +
                    "FROM derby_pickups WHERE derby_id = @id", param);
                row = mySQLConnector.GetNextRow();
                results.Add("Pickups", row["nbr"]);
                mySQLConnector.CloseReader();

                mySQLConnector.OpenReader("SELECT COUNT(*) as nbr " +
                    "FROM derby_mapobjects WHERE derby_id = @id", param);
                row = mySQLConnector.GetNextRow();
                results.Add("Map objects", row["nbr"]);
                mySQLConnector.CloseReader();
            }

            return results;
        }
    }
}
