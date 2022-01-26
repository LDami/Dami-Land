using Newtonsoft.Json;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
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
using System.Xml;

namespace SampSharpGameMode1.Events.Races
{
    public class RaceCreator : EventCreator
    {
        class HUD
        {
            TextdrawLayer layer;

            private string selectedIdx;
            public HUD(Player player)
            {
                layer = new TextdrawLayer();
                string filename = Directory.GetCurrentDirectory() + "\\scriptfiles\\racecreator.json";
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
                            layer.SetTextdrawText("racenamelabel", "Race Name:");
                            layer.SetTextdrawText("racename", "None");
                            layer.SetTextdrawText("selectedidx", "Selected CP: None");
                            layer.SetTextdrawText("totalcp", "Total CP: 0");
                            layer.SetTextdrawText("editingmode", "Mode: None");
                            layer.UnselectAllTextdraw();
                            layer.SetOnClickCallback("editingmode", OnEditingModeClick);
                            fs.Close();
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("RaceCreator.cs - RaceCreator.HUD._:E: Cannot load Race Creator HUD:");
                        Console.WriteLine(e.Message);
                    }
                }
            }
            public void Destroy()
            {
                layer.HideAll();
            }
            public void SetRaceName(string name)
            {
                layer.SetTextdrawText("racename", name ?? "[Untitled]");
            }
            public void SetSelectedIdx(string idx, EditingMode editingMode)
            {
                selectedIdx = idx;
                if(editingMode == EditingMode.Checkpoints)
                    layer.SetTextdrawText("selectedidx", "Selected CP: " + idx);
                else if(editingMode == EditingMode.SpawnPos)
                    layer.SetTextdrawText("selectedidx", "Selected Spawn: " + idx);
            }
            public void SetTotalCP(int cp)
            {
                layer.SetTextdrawText("totalcp", "Total CP: " + cp);
            }
            public void SetEditingMode(EditingMode editingMode)
            {
                layer.SetTextdrawText("editingmode", editingMode.ToString());
                this.SetSelectedIdx(selectedIdx, editingMode);
            }
            private void OnEditingModeClick()
			{
                Player.SendClientMessageToAll("EditingMode textdraw clicked !");
			}
        }
        enum EditingMode { Checkpoints, SpawnPos }

        protected MySQLConnector mySQLConnector = MySQLConnector.Instance();

        Player player;

        HUD hud;

        public Race editingRace = null;
        EditingMode editingMode;
        public bool isNew;
        DynamicRaceCheckpoint shownCheckpoint;
        int checkpointIndex;
        int spawnIndex;

        PlayerObject moverObject;
        const int moverObjectModelID = 19133;
        Vector3 moverObjectOffset = new Vector3(0.0f, 0.0f, 1.0f);


        BaseVehicle[] spawnVehicles;
        public RaceCreator(Player _player)
        {
            player = _player;
            editingRace = null;
            checkpointIndex = 0;
            spawnVehicles = new BaseVehicle[Race.MAX_PLAYERS_IN_RACE];
        }

        public void Create()
        {
            editingRace = new Race();
            editingRace.Name = "[Untitled]";
            editingRace.SpawnPoints = new List<Vector3R>();
            checkpointIndex = 0;
            editingRace.StartingVehicle = VehicleModelType.Infernus;
            spawnVehicles = new BaseVehicle[Race.MAX_PLAYERS_IN_RACE];
            isNew = true;
            this.SetPlayerInEditor();
        }

        public void Load(int id)
        {
            if (id > 0)
            {
                Race loadingRace = new Race();
                loadingRace.Loaded += LoadingRace_Loaded;
                loadingRace.Load(id);
            }
            else player.SendClientMessage(Color.Red, "Error loading race #" + id + " (invalid ID)");
        }

        private void LoadingRace_Loaded(object sender, RaceLoadedEventArgs e)
        {
            if(e.success)
            {
                isNew = false;
                checkpointIndex = 0;
                editingRace = e.race;
                spawnVehicles = new BaseVehicle[Race.MAX_PLAYERS_IN_RACE];
                UpdatePlayerCheckpoint();
                player.SendClientMessage(Color.Green, "Race #" + e.race.Id + " loaded successfully in creation mode");
                this.SetPlayerInEditor();
            }
            else
                player.SendClientMessage(Color.Red, "Error loading race (missing mandatory datas)");
        }
        private void SetPlayerInEditor()
        {
            player.EnablePlayerCameraTarget(true);
            player.KeyStateChanged += Player_KeyStateChanged;
            player.EnterCheckpoint += Player_EnterCheckpoint;
            player.EnterRaceCheckpoint += Player_EnterRaceCheckpoint;
            if (!player.InAnyVehicle)
            {
                Vector3 pos;
                if (editingRace.checkpoints.Count > 0)
                    pos = editingRace.checkpoints[0].Position;
                else
                    pos = player.Position;
                BaseVehicle veh = BaseVehicle.Create(VehicleModelType.Infernus, pos, 0.0f, 1, 1);
                player.DisableRemoteVehicleCollisions(true);
                player.PutInVehicle(veh);
            }

            hud = new HUD(player);
            hud.SetRaceName(editingRace.Name);
            editingMode = EditingMode.Checkpoints;
            hud.SetSelectedIdx("S", editingMode);
            hud.SetTotalCP(editingRace.checkpoints.Count - 1);
        }

        public void Unload()
        {
            editingRace = null;
            if(hud != null)
                hud.Destroy();
            hud = null;
            if(shownCheckpoint != null)
			{
                if(!shownCheckpoint.IsDisposed)
                    shownCheckpoint.Dispose();
			}
            if(moverObject != null)
            {
                moverObject.Edited -= moverObject_Edited;
                moverObject.Dispose();
            }
            moverObject = null;
            if(spawnVehicles != null)
			{
                foreach(BaseVehicle veh in spawnVehicles)
				{
                    if(veh != null)
                        veh.Dispose();
				}
			}
            spawnVehicles = null;
            if (player != null)
            {
                player.CancelEdit();
                player.DisableCheckpoint();
                player.DisableRaceCheckpoint();
                player.KeyStateChanged -= Player_KeyStateChanged;
                player.EnterCheckpoint -= Player_EnterCheckpoint;
                player.EnterRaceCheckpoint -= Player_EnterRaceCheckpoint;
            }
        }

        public Boolean Save()
        {
            if (editingRace != null)
            {
                Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@id", editingRace.Id }
                };
                mySQLConnector.Execute("DELETE FROM race_checkpoints WHERE race_id=@id", param);
                foreach (KeyValuePair<int, Checkpoint> kvp in editingRace.checkpoints)
                {
                    param = new Dictionary<string, object>
                    {
                        { "@id", editingRace.Id },
                        { "@checkpoint_number", kvp.Key },
                        { "@checkpoint_pos_x", kvp.Value.Position.X },
                        { "@checkpoint_pos_y", kvp.Value.Position.Y },
                        { "@checkpoint_pos_z", kvp.Value.Position.Z },
                        { "@checkpoint_size", kvp.Value.Size },
                        { "@checkpoint_type", kvp.Value.Type },
                        { "@checkpoint_vehiclechange", kvp.Value.NextVehicle },
                        { "@checkpoint_nitro", kvp.Value.NextNitro }
                    };
                    mySQLConnector.Execute("INSERT INTO race_checkpoints " +
                        "(race_id, checkpoint_number, checkpoint_pos_x, checkpoint_pos_y, checkpoint_pos_z, checkpoint_size, checkpoint_type, checkpoint_vehiclechange, checkpoint_nitro) VALUES" +
                        "(@id, @checkpoint_number, @checkpoint_pos_x, @checkpoint_pos_y, @checkpoint_pos_z, @checkpoint_size, @checkpoint_type, @checkpoint_vehiclechange, @checkpoint_nitro)", param);
                }
                param = new Dictionary<string, object>
                {
                    { "@id", editingRace.Id }
                };
                mySQLConnector.Execute("DELETE FROM race_spawn WHERE race_id=@id", param);
                if (editingRace.SpawnPoints.Count == 0)
                    player.SendClientMessage("You must place at least one spawn point (submission key) !");
                for (int i = 0; i < editingRace.SpawnPoints.Count; i++)
                {
                    param = new Dictionary<string, object>
                    {
                        { "@id", editingRace.Id },
                        { "@spawn_index",  i },
                        { "@spawn_pos_x",  editingRace.SpawnPoints[i].Position.X },
                        { "@spawn_pos_y",  editingRace.SpawnPoints[i].Position.Y },
                        { "@spawn_pos_z",  editingRace.SpawnPoints[i].Position.Z },
                        { "@spawn_rot",  editingRace.SpawnPoints[i].Rotation },
                    };
                    mySQLConnector.Execute("INSERT INTO race_spawn " +
                        "(race_id, spawn_index, spawn_pos_x, spawn_pos_y, spawn_pos_z, spawn_rot) VALUES " +
                        "(@id, @spawn_index, @spawn_pos_x, @spawn_pos_y, @spawn_pos_z, @spawn_rot)", param);

                }
                isNew = false;
                return (mySQLConnector.RowsAffected > 0);
            }
            return false;
        }

        public Boolean Save(string name) // Only if the race does not already exist
        {
            if (editingRace != null && name.Length > 0)
            {
                Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@race_name", name },
                    { "@race_creator", player.Name },
                    { "@race_startvehicle", editingRace.StartingVehicle }
                };
                editingRace.Id = (int)mySQLConnector.Execute("INSERT INTO races " +
                    "(race_name, race_creator, race_startvehicle) VALUES" +
                    "(@race_name, @race_creator, @race_startvehicle)", param);
                if (mySQLConnector.RowsAffected > 0)
                {
                    hud.SetRaceName(name);
                    return this.Save();
                }
                else return false;
            }
            return false;
        }

        public void AddCheckpoint(Vector3 position)
        {
            editingMode = EditingMode.Checkpoints;
            if(editingRace.checkpoints.Count == 0)
            {
                editingRace.checkpoints.Add(0, new Checkpoint(position, CheckpointType.Normal));
                checkpointIndex = 0;
                Console.WriteLine("RaceCreator.cs - RaceCreator.AddCheckpoint:I: First CP added");
            }
            else
            {
                if(checkpointIndex == editingRace.checkpoints.Count-1) // Add to the end
                {
                    editingRace.checkpoints.Add(editingRace.checkpoints.Count, new Checkpoint(position, CheckpointType.Normal));
                    checkpointIndex = editingRace.checkpoints.Count-1;
                    Console.WriteLine("RaceCreator.cs - RaceCreator.AddCheckpoint:I: checkpointIndex = count-1, new CP added at the end");
                }
                else
                {
                    Console.WriteLine("RaceCreator.cs - RaceCreator.AddCheckpoint:I: checkpointIndex != count-1, rearrangement  needed");
                    Dictionary<int, Checkpoint> tmp = editingRace.checkpoints;
                    editingRace.checkpoints[checkpointIndex + 1] = new Checkpoint(position, CheckpointType.Normal);
                    for(int i = editingRace.checkpoints.Count-1; i > checkpointIndex + 2; i--)
                    {
                        editingRace.checkpoints[i] = tmp[i-1];
                    }
                    editingRace.checkpoints.Add(editingRace.checkpoints.Count, tmp[tmp.Count -1]);
                    checkpointIndex++;
                }
            }
            
            UpdatePlayerCheckpoint();
            hud.SetSelectedIdx(checkpointIndex.ToString(), editingMode);
            hud.SetTotalCP(editingRace.checkpoints.Count);
        }
        public void MoveCurrent(Vector3 position)
        {
            try
            {
                editingMode = EditingMode.Checkpoints;
                editingRace.checkpoints[checkpointIndex].Position = position + new Vector3(0.0, 5.0, 0.0);
                UpdatePlayerCheckpoint();
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("RaceCreator.cs - MoveCurrent:E: Cannot move current: " + e.Message);
                player.SendClientMessage(Color.Red, "Error, place a checkpoint first ! (/race addcp)");
            }
        }
        public void TeleportToCurrent()
        {
            try
            {
                player.Teleport(editingRace.checkpoints[checkpointIndex].Position);
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("RaceCreator.cs - TeleportToCurrent:E: Cannot teleport player to current: " + e.Message);
                player.SendClientMessage(Color.Red, "Error, place a checkpoint first ! (/race addcp)");
            }
		}
        private void Player_KeyStateChanged(object sender, SampSharp.GameMode.Events.KeyStateChangedEventArgs e)
        {
            //if (e.NewKeys.ToString() != "0") player.GameText(e.NewKeys.ToString(), 100, 3);
            switch (e.NewKeys)
            {
                case Keys.AnalogLeft:
                    {
                        switch (editingMode)
                        {
                            case EditingMode.Checkpoints:
                                {
                                    if (checkpointIndex > 0) checkpointIndex--;
                                    if (checkpointIndex == 0)
                                    {
                                        hud.SetSelectedIdx("S", editingMode);
                                        player.GameText("CP: Start", 500, 3);
                                    }
                                    else
                                    {
                                        hud.SetSelectedIdx(checkpointIndex.ToString(), editingMode);
                                        player.GameText("CP: " + checkpointIndex, 500, 3);
                                    }
                                    UpdatePlayerCheckpoint();
                                    break;
                                }
                            case EditingMode.SpawnPos:
                                {
                                    if (spawnIndex > 0) spawnIndex--;
                                    hud.SetSelectedIdx(spawnIndex.ToString(), editingMode);
                                    break;
                                }
                        }
                        break;
                    }
                case Keys.AnalogRight:
                    {
                        switch (editingMode)
                        {
                            case EditingMode.Checkpoints:
                                {
                                    if (checkpointIndex < editingRace.checkpoints.Count - 1) checkpointIndex++;
                                    if (checkpointIndex == editingRace.checkpoints.Count - 1)
                                    {
                                        hud.SetSelectedIdx("F", editingMode);
                                        player.GameText("CP: Finish", 500, 3);
                                    }
                                    else
                                    {
                                        hud.SetSelectedIdx(checkpointIndex.ToString(), editingMode);
                                        player.GameText("CP: " + checkpointIndex, 500, 3);
                                    }
                                    UpdatePlayerCheckpoint();
                                    break;
                                }
                            case EditingMode.SpawnPos:
                                {
                                    if (spawnIndex < Race.MAX_PLAYERS_IN_RACE) spawnIndex++;
                                    hud.SetSelectedIdx(spawnIndex.ToString(), editingMode);
                                    break;
                                }
                        }
                        break;
                    }
                case Keys.Submission:
                    {
                        ShowRaceDialog();
                        break;
                    }
            }
        }

        private void ShowRaceDialog()
        {
            ListDialog raceDialog = new ListDialog("Race options", "Select", "Cancel");
            raceDialog.AddItem("Select starting vehicle [" + editingRace.StartingVehicle + "]");
            raceDialog.AddItem("Edit race name");

            if (editingMode == EditingMode.Checkpoints)
                raceDialog.AddItem("Edit spawn positions");
            else if (editingMode == EditingMode.SpawnPos)
                raceDialog.AddItem("Edit checkpoints");

            raceDialog.AddItem("Laps: " + editingRace.Laps);

            raceDialog.Show(player);
            raceDialog.Response += RaceDialog_Response;
        }

        private void RaceDialog_Response(object sender, SampSharp.GameMode.Events.DialogResponseEventArgs e)
        {
            if (e.Player.Equals(player))
            {
                if (e.DialogButton == DialogButton.Left)
                {
                    switch (e.ListItem)
                    {
                        case 0: // Select starting vehicle
                            {
                                InputDialog cpStartCarNameDialog = new InputDialog("Vehicle name", "Enter the vehicle name:", false, "Find", "Cancel");
                                cpStartCarNameDialog.Show(player);
                                cpStartCarNameDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        editingRace.StartingVehicle = Utils.GetVehicleModelType(eventArgs.InputText);
                                        player.Notificate("Updated");
                                        ShowRaceDialog();
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowRaceDialog();
                                    }
                                };
                                break;
                            }
                        case 1: // Edit race name
                            {
                                InputDialog cpRaceNameDialog = new InputDialog("Race name", "Enter the race name:", false, "Edit", "Cancel");
                                cpRaceNameDialog.Show(player);
                                cpRaceNameDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        editingRace.Name = eventArgs.InputText;
                                        hud.SetRaceName(editingRace.Name);
                                        player.Notificate("Updated");
                                        ShowRaceDialog();
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowRaceDialog();
                                    }
                                };
                                break;
                            }
                        case 2: // Set/Edit spawn position
                            {
                                if(editingRace.checkpoints.Count > 0)
                                {
                                    List<Vector3R> spawns = new List<Vector3R>();
                                    if (editingRace.SpawnPoints.Count == 0)
									{
                                        spawns.Add(new Vector3R(editingRace.checkpoints[0].Position + Vector3.UnitZ));
									}
                                    else
									{
                                        spawns = editingRace.SpawnPoints;
									}
                                    SpawnerCreator spawner = new SpawnerCreator(player, 0, editingRace.StartingVehicle.GetValueOrDefault(VehicleModelType.Infernus), spawns);
									spawner.Quit += (object sender, SpawnCreatorQuitEventArgs e) => {
                                        editingRace.SpawnPoints = e.spawnPoints;
                                        editingMode = EditingMode.Checkpoints;
                                        hud.SetEditingMode(editingMode);
                                    };
                                    editingMode = EditingMode.SpawnPos;
                                    hud.SetEditingMode(editingMode);
                                }
                                else
                                {
                                    player.SendClientMessage(Color.Red, "Error, place a checkpoint first ! (/race addcp)");
                                }
                                break;
                            }
                        case 3: // Laps
                            {
                                InputDialog cpRaceLapsDialog = new InputDialog("Race laps", "Enter the number of laps the race have:", false, "Set", "Cancel");
                                cpRaceLapsDialog.Show(player);
                                cpRaceLapsDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        try
                                        {
                                            editingRace.Laps = Convert.ToInt32(eventArgs.InputText);
                                            player.Notificate("Updated");
                                        }
                                        catch(Exception e)
                                        {
                                            player.SendClientMessage(Color.Red, "There was an error trying to set this value.");
                                            player.Notificate("Error");
                                        }
                                        ShowRaceDialog();
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowRaceDialog();
                                    }
                                };
                                break;
                            }
                    }
                }
            }
        }

		private void Player_EnterCheckpoint(object sender, EventArgs e)
        {
            ShowCheckpointDialog();
        }

        private void Player_EnterRaceCheckpoint(object sender, EventArgs e)
        {
            ShowCheckpointDialog();
        }

        private void ShowCheckpointDialog()
        {
            Checkpoint cp = editingRace.checkpoints[checkpointIndex];
            ListDialog cpDialog = new ListDialog("Checkpoint options", "Select", "Cancel");
            cpDialog.AddItem("Edit checkpoint position");
            cpDialog.AddItem("Edit checkpoint size [" + cp.Size.ToString() + "]");

            if (cp.Type == CheckpointType.Normal || cp.Type == CheckpointType.Finish)
                cpDialog.AddItem("Change checkpoint type: " + Color.Green + "Normal" + Color.White + "/Air");
            else if (cp.Type == CheckpointType.Air || cp.Type == CheckpointType.AirFinish)
                cpDialog.AddItem("Change checkpoint type: Normal/" + Color.Green + "Air");

            if(checkpointIndex > 0)
                cpDialog.AddItem("Edit events");

            cpDialog.Show(player);
            cpDialog.Response += CpDialog_Response;
        }

        private void CpDialog_Response(object sender, SampSharp.GameMode.Events.DialogResponseEventArgs e)
        {
            if(e.Player.Equals(player))
            {
                if (e.DialogButton == DialogButton.Left)
                {
                    Checkpoint cp = editingRace.checkpoints[checkpointIndex];
                    switch (e.ListItem)
                    {
                        case 0: // Edit checkpoint position
                            {
                                player.cameraController.Enabled = true;
                                if (moverObject == null)
                                {
                                    moverObject = new PlayerObject(
                                        player,
                                        moverObjectModelID,
                                        cp.Position + moverObjectOffset,
                                        new Vector3(0.0, 0.0, 0.0));

                                    moverObject.Edit();
                                    moverObject.Edited += moverObject_Edited;
                                }
                                else
                                {
                                    moverObject.Position = cp.Position;
                                    moverObject.Edit();
                                }
                                break;
                            }
                        case 1: // Edit checkpoint size
                            {
                                InputDialog cpSizeDialog = new InputDialog("Checkpoint size", "Current size: " + cp.Size.ToString(), false, "Set", "Cancel");
                                cpSizeDialog.Show(player);
                                cpSizeDialog.Response += CpSizeDialog_Response;
                                break;
                            }
                        case 2: // Change checkpoint type
                            {
                                if (cp.Type == CheckpointType.Normal)
                                    editingRace.checkpoints[checkpointIndex].Type = CheckpointType.Air;
                                else if (cp.Type == CheckpointType.Air)
                                    editingRace.checkpoints[checkpointIndex].Type = CheckpointType.Normal;
                                else if (cp.Type == CheckpointType.Finish)
                                    editingRace.checkpoints[checkpointIndex].Type = CheckpointType.AirFinish;
                                else if (cp.Type == CheckpointType.AirFinish)
                                    editingRace.checkpoints[checkpointIndex].Type = CheckpointType.Finish;
                                UpdatePlayerCheckpoint();
                                player.Notificate("Updated");
                                ShowCheckpointDialog();
                                break;
                            }
                        case 3: // Edit events
                            {
                                ShowCheckpointEventDialog();
                                break;
                            }
                    }
                }
            }
        }

        private void CpSizeDialog_Response(object sender, SampSharp.GameMode.Events.DialogResponseEventArgs e)
        {
            if (e.Player.Equals(player))
            {
                if (e.DialogButton == DialogButton.Left)
                {
                    if (e.InputText.Length > 0)
                    {
                        try
                        {
                            editingRace.checkpoints[checkpointIndex].Size = (float)Convert.ToDouble(e.InputText);
                            UpdatePlayerCheckpoint();
                            player.Notificate("Updated");
                        }
                        catch (Exception ex)
                        {
                            player.SendClientMessage(Color.Red, "There was an error trying to set this value.");
                            player.Notificate("Error");
                        }
                    }
                    ShowCheckpointDialog();
                }
                else
                {
                    ShowCheckpointDialog();
                }
            }
        }

        private void ShowCheckpointEventDialog()
        {
            ListDialog cpEventDialog = new ListDialog("Checkpoint events", "Select", "Cancel");
            if (editingRace.checkpoints[checkpointIndex].NextVehicle == null)
                cpEventDialog.AddItem("Vehicle change [None]");
            else
                cpEventDialog.AddItem("Vehicle change [" + Color.Green + editingRace.checkpoints[checkpointIndex].NextVehicle.ToString() + Color.White + "]");
            switch (editingRace.checkpoints[checkpointIndex].NextNitro)
            {
                case Checkpoint.NitroEvent.None:
                    cpEventDialog.AddItem("Set Nitro [Unchanged]");
                    break;
                case Checkpoint.NitroEvent.Give:
                    cpEventDialog.AddItem("Set Nitro [" + Color.Green + "Give" + Color.White + "]");
                    break;
                case Checkpoint.NitroEvent.Remove:
                    cpEventDialog.AddItem("Set Nitro [" + Color.Green + "Remove" + Color.White + "]");
                    break;
                default:
                    break;
            }
            cpEventDialog.Show(player);
            cpEventDialog.Response += CpEventDialog_Response;
        }

        private void CpEventDialog_Response(object sender, SampSharp.GameMode.Events.DialogResponseEventArgs e)
        {
            if (e.Player.Equals(player))
            {
                if (e.DialogButton == DialogButton.Left)
                {
                    Checkpoint cp = editingRace.checkpoints[checkpointIndex];
                    switch (e.ListItem)
                    {
                        case 0: // Vehicle event
                            {
                                InputDialog cpEventCarNameDialog = new InputDialog("Vehicle name", "Enter the vehicle name:", false, "Find", "Cancel");
                                cpEventCarNameDialog.Show(player);
                                cpEventCarNameDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        cp.NextVehicle = Utils.GetVehicleModelType(eventArgs.InputText);
                                        player.Notificate("Updated");
                                        ShowCheckpointEventDialog();
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowCheckpointEventDialog();
                                    }
                                };
                                break;
                            }
                        case 1: // Nitro event
                            {
                                ListDialog cpNitroEventDialog = new ListDialog("Nitro event", "Update", "Cancel");
                                cpNitroEventDialog.AddItem(((cp.NextNitro == Checkpoint.NitroEvent.None) ? "> " : "") + "[Unchanged]");
                                cpNitroEventDialog.AddItem(((cp.NextNitro == Checkpoint.NitroEvent.Give) ? "> " : "") + "[Give]");
                                cpNitroEventDialog.AddItem(((cp.NextNitro == Checkpoint.NitroEvent.Remove) ? "> " : "") + "[Remove]");
                                cpNitroEventDialog.Show(player);
                                cpNitroEventDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        cp.NextNitro = (Checkpoint.NitroEvent)eventArgs.ListItem;
                                        player.Notificate("Updated");
                                        ShowCheckpointEventDialog();
                                    }
                                    else
                                    {
                                        player.Notificate("Cancelled");
                                        ShowCheckpointEventDialog();
                                    }
                                };
                                break;
                            }
                    }
                }
                else
                {
                    ShowCheckpointDialog();
                }
            }
        }

        public void UpdatePlayerCheckpoint()
        {
            player.DisableCheckpoint();
            player.DisableRaceCheckpoint();
            Checkpoint cp = editingRace.checkpoints[checkpointIndex];

            Checkpoint nextCp = null;
            if (editingRace.checkpoints.ContainsKey(checkpointIndex + 1))
                nextCp = editingRace.checkpoints[checkpointIndex + 1];

            Vector3 nextPos = (nextCp != null) ? nextCp.Position : Vector3.Zero;

            if (shownCheckpoint == null || shownCheckpoint.IsDisposed)
                shownCheckpoint = new DynamicRaceCheckpoint(cp.Type, cp.Position, nextPos, cp.Size, 500.0f);
            else
            {
                shownCheckpoint.Position = cp.Position;
                shownCheckpoint.NextPosition = nextPos;
                shownCheckpoint.Size = cp.Size;
            }
            shownCheckpoint.ShowForPlayer(player);
            Streamer.Update(player);
            if (moverObject == null)
            {
                moverObject = new PlayerObject(
                    player,
                    moverObjectModelID,
                    cp.Position + moverObjectOffset,
                    new Vector3(0.0, 0.0, 0.0));
                moverObject.Edited += moverObject_Edited;
            }
            else
            {
                moverObject.Position = cp.Position;
            }
        }

        private void moverObject_Edited(object sender, SampSharp.GameMode.Events.EditPlayerObjectEventArgs e)
        {
            if (editingMode == EditingMode.Checkpoints)
            {
                moverObject.Position = e.Position;
                editingRace.checkpoints[checkpointIndex].Position = e.Position - moverObjectOffset;
                UpdatePlayerCheckpoint();
            }
            if (e.EditObjectResponse == EditObjectResponse.Cancel)
            {
                player.cameraController.SetBehindPlayer();
                player.cameraController.Enabled = true;
            }
        }

        public static Dictionary<string, string> Find(string str)
        {
            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@name", str }
                };
            mySQLConnector.OpenReader("SELECT race_id, race_name FROM races WHERE race_name LIKE @name", param);
            Dictionary<string, string> results = mySQLConnector.GetNextRow();
            mySQLConnector.CloseReader();
            return results;
        }

        public static Dictionary<string, string> GetInfo(int id)
        {
            // id, name, creator, type, number of checkpoints, zone
            Dictionary<string, string> results = new Dictionary<string, string>();
            Dictionary<string, string> row;

            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@id", id }
                };

            mySQLConnector.OpenReader("SELECT race_id, race_name, race_creator FROM races WHERE race_id = @id", param);

            row = mySQLConnector.GetNextRow();
            foreach (KeyValuePair<string, string> kvp in row)
                results.Add(MySQLConnector.Field.GetFieldName(kvp.Key), kvp.Value);

            mySQLConnector.CloseReader();

            mySQLConnector.OpenReader("SELECT checkpoint_id, checkpoint_number, checkpoint_pos_x, checkpoint_pos_y, checkpoint_pos_z " +
                "FROM race_checkpoints WHERE race_id = @id", param);
            int nbrOfCheckpoints = 0;
            row = mySQLConnector.GetNextRow();
            Vector3 firstCheckpointPos = new Vector3();
            while(row.Count > 0)
            {
                nbrOfCheckpoints++;
                if(row["checkpoint_number"] == "0")
                {
                    firstCheckpointPos = new Vector3(
                        (float)Convert.ToDouble(row["checkpoint_pos_x"]),
                        (float)Convert.ToDouble(row["checkpoint_pos_y"]),
                        (float)Convert.ToDouble(row["checkpoint_pos_z"])
                    );
                }
                row = mySQLConnector.GetNextRow();
            }
            results.Add("Nombre de checkpoints", nbrOfCheckpoints.ToString());
            mySQLConnector.CloseReader();

            // On récupère la zone du premier checkpoint
            Zone zone = new Zone();
            string zoneStr = zone.GetZoneName(firstCheckpointPos);
            results.Add("Zone", zoneStr);

            return results;
        }
    }
}
