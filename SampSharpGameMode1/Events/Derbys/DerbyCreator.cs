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

        private int lastSelectedObjectId;
        private DerbyPickup? lastPickedUpPickup;
        private Dictionary<int, DynamicTextLabel> labels;

        PlayerObject moverObject;
        const int moverObjectModelID = 3082;
        Vector3 moverObjectOffset = new Vector3(0.0f, 0.0f, 1.0f);

        BaseVehicle[] spawnVehicles;
        int spawnIndex;

        #region PlayerEvents
        private void OnPlayerKeyStateChanged(object sender, KeyStateChangedEventArgs e)
        {
            switch (e.NewKeys)
            {
                case Keys.AnalogLeft:
                    {
                        switch (editingMode)
                        {
                            case EditingMode.Mapping:
                                {
                                    break;
                                }
                            case EditingMode.SpawnPos:
                                {
                                    break;
                                }
                        }
                        break;
                    }
                case Keys.AnalogRight:
                    {
                        switch (editingMode)
                        {
                            case EditingMode.Mapping:
                                {
                                    break;
                                }
                            case EditingMode.SpawnPos:
                                {
                                    break;
                                }
                        }
                        break;
                    }
                case Keys.Submission:
                    {
                        ShowDerbyDialog();
                        break;
                    }
                case Keys.No:
                    {
                        Streamer streamer = new Streamer();
                        DynamicObject obj = streamer.GetPlayerCameraTargetObject(player);
                        if (obj != null)
                        {
                            if(lastSelectedObjectId != -1)
                                labels[lastSelectedObjectId].Color = Color.White;
                            labels[obj.Id].Color = Color.Red;
                            obj.Edited += OnMapObjectEdited;
                            obj.Edit(player);
                            lastSelectedObjectId = obj.Id;
                        }
                        break;
                    }
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
            spawnVehicles = new BaseVehicle[Derby.MAX_PLAYERS_IN_DERBY];
        }

        public void Create()
        {
            editingDerby = new Derby();
            editingDerby.IsCreatorMode = true;
            editingDerby.StartingVehicle = VehicleModelType.Infernus;
            editingDerby.SpawnPoints = new List<Vector3R>();
            editingDerby.MapObjects = new List<DynamicObject>();
            editingDerby.Pickups = new List<DerbyPickup>();
            isNew = true;
            lastSelectedObjectId = -1;
            lastPickedUpPickup = null;
            labels = new Dictionary<int, DynamicTextLabel>();
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
                labels = new Dictionary<int, DynamicTextLabel>();
                editingDerby = e.derby;
                foreach (DynamicObject obj in editingDerby.MapObjects)
                {
                    labels.Add(obj.Id, new DynamicTextLabel("ID: " + obj.Id, Color.White, obj.Position, 100.0f));
                }
                foreach (DerbyPickup pickup in editingDerby.Pickups)
                    pickup.pickup.PickedUp += OnPlayerPickUpPickup;
                player.SendClientMessage(Color.Green, "Derby #" + editingDerby.Id + " loaded successfully in creation mode");
                this.SetPlayerInEditor();
            }
            else
                player.SendClientMessage(Color.Red, "Error loading derby (missing mandatory datas)");
        }
        private void SetPlayerInEditor()
        {
            player.EnablePlayerCameraTarget(true);
            player.KeyStateChanged += OnPlayerKeyStateChanged;
            if (!player.InAnyVehicle)
            {
                BaseVehicle veh = BaseVehicle.Create(VehicleModelType.Infernus, player.Position + new Vector3(0.0, 5.0, 0.0), 0.0f, 1, 1);
                player.PutInVehicle(veh);
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
                foreach (DynamicObject obj in editingDerby.MapObjects)
                    obj.Dispose();
                editingDerby.MapObjects.Clear();
                editingDerby.MapObjects = null;
                foreach (DerbyPickup pickup in editingDerby.Pickups)
                    pickup.pickup.Dispose();
                editingDerby.Pickups.Clear();
                editingDerby.Pickups = null;
            }
            editingDerby = null;
            if(labels != null)
			{
                foreach (DynamicTextLabel label in labels.Values)
                    label.Dispose();
			}
            labels = null;
            if (hud != null)
                hud.Destroy();
            hud = null;
            if (moverObject != null)
            {
                moverObject.Edited -= moverObject_Edited;
                moverObject.Dispose();
            }
            moverObject = null;
            //TODO: cancel edit ?
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
                mySQLConnector.Execute("DELETE FROM derby_mapobjects WHERE derby_id=@id", param);
                for (int i = 0; i < editingDerby.MapObjects.Count; i++)
                {
                    param = new Dictionary<string, object>
                    {
                        { "@id", editingDerby.Id },
                        { "@mapobject_model",  editingDerby.MapObjects[i].ModelId },
                        { "@mapobject_pos_x",  editingDerby.MapObjects[i].Position.X },
                        { "@mapobject_pos_y",  editingDerby.MapObjects[i].Position.Y },
                        { "@mapobject_pos_z",  editingDerby.MapObjects[i].Position.Z },
                        { "@mapobject_rot_x",  editingDerby.MapObjects[i].Rotation.X },
                        { "@mapobject_rot_y",  editingDerby.MapObjects[i].Rotation.Y },
                        { "@mapobject_rot_z",  editingDerby.MapObjects[i].Rotation.Z },
                    };
                    mySQLConnector.Execute("INSERT INTO derby_mapobjects " +
                        "(derby_id, mapobject_model, mapobject_pos_x, mapobject_pos_y, mapobject_pos_z, mapobject_rot_x, mapobject_rot_y, mapobject_rot_z) VALUES " +
                        "(@id, @mapobject_model, @mapobject_pos_x, @mapobject_pos_y, @mapobject_pos_z, @mapobject_rot_x, @mapobject_rot_y, @mapobject_rot_z)", param);

                }
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
                    hud.SetDerbyName(name);
                    return this.Save();
                }
                else return false;
            }
            return false;
        }

        public void AddObject(int modelid)
        {
            DynamicObject obj = new DynamicObject(modelid, player.Position + new Vector3(10.0, 0.0, 0.0), Vector3.Zero);
            editingDerby.MapObjects.Add(obj);
            obj.Edited += OnMapObjectEdited;
            obj.ShowForPlayer(player);
            obj.Edit(player);
            lastSelectedObjectId = obj.Id;
        }

        public void DeleteObject(int objectid)
        {
            DynamicObject obj = editingDerby.MapObjects.Find(obj => obj.Id == objectid);
            if (obj == null)
                player.SendClientMessage($"The Object ID {objectid} does not exists");
            else
			{
                editingDerby.MapObjects.Remove(obj);
                labels[obj.Id].Dispose();
                labels.Remove(obj.Id);
                obj.Dispose();
                player.SendClientMessage($"Object ID {objectid} deleted");
            }
        }

        public void AddPickup(int modelid)
		{
            DerbyPickup pickup = new DerbyPickup(modelid, player.Position, player.VirtualWorld, DerbyPickup.PickupEvent.None);
            editingDerby.Pickups.Add(pickup);
		}

        public void DeletePickup(Vector3 position)
		{
            player.SendClientMessage("Function not implemented yet");
		}

        #region Dialogs
        private void ShowDerbyDialog()
        {
            ListDialog derbyDialog = new ListDialog("Derby options", "Select", "Cancel");
            derbyDialog.AddItem("Select starting vehicle [" + editingDerby.StartingVehicle + "]");
            derbyDialog.AddItem("Edit derby name");

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
                    }
                }
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
		private void OnMapObjectEdited(object sender, SampSharp.Streamer.Events.PlayerEditEventArgs e)
        {
            DynamicObject obj = (sender as DynamicObject);
            if (labels.ContainsKey(obj.Id))
			{
                labels[obj.Id].Position = e.Position;
                labels[obj.Id].Text = "ID: " + obj.Id;
                labels[obj.Id].Color = Color.White;
            }
            else
			{
                labels.Add(obj.Id, new DynamicTextLabel("ID: " + obj.Id, Color.White, e.Position, 100.0f));
            }
            lastSelectedObjectId = -1;
        }

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
            // id, name, creator, type, number of spawn points
            Dictionary<string, string> results = new Dictionary<string, string>();
            Dictionary<string, string> row;

            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@id", id }
                };

            mySQLConnector.OpenReader("SELECT derby_id, derby_name, derby_creator FROM derbys WHERE derby_id = @id", param);

            row = mySQLConnector.GetNextRow();
            foreach (KeyValuePair<string, string> kvp in row)
                results.Add(MySQLConnector.Field.GetFieldName(kvp.Key), kvp.Value);

            mySQLConnector.CloseReader();

            mySQLConnector.OpenReader("SELECT COUNT(*) as nbr " +
                "FROM derby_spawnpos WHERE derby_id = @id", param);
            int nbrOfSpawnPoints = 0;
            row = mySQLConnector.GetNextRow();
            while (row.Count > 0)
            {
                nbrOfSpawnPoints++;
                row = mySQLConnector.GetNextRow();
            }
            results.Add("Nombre de spawn points", nbrOfSpawnPoints.ToString());
            mySQLConnector.CloseReader();

            return results;
        }
    }
}
