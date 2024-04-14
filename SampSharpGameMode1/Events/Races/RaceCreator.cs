using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Civilisation;
using SampSharpGameMode1.Display;
using SampSharpGameMode1.Events._Tools;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Events.Races
{
    public class RaceCreator : EventCreator
    {
        class HUD : Display.HUD
        {
            private string selectedIdx;
            public HUD(Player player) : base(player, "racecreator.json")
            {
                layer.SetTextdrawText("racenamelabel", "Race Name:");
                layer.SetTextdrawText("racename", "None");
                layer.SetTextdrawText("selectedidx", "Selected CP: None");
                layer.SetTextdrawText("totalcp", "Total CP: 0");
                layer.SetTextdrawText("editingmode", "Mode: None");
                layer.UnselectAllTextdraw();
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
                    layer.SetTextdrawText("selectedidx", "Spawn: " + idx);
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
        }
        enum EditingMode { Checkpoints, SpawnPos }

        protected MySQLConnector mySQLConnector = MySQLConnector.Instance();

        Player player;

        HUD hud;

        public int EventId { get => editingRace.Id; }

        public Race editingRace = null;
        EditingMode editingMode;
        public bool isNew;
        DynamicRaceCheckpoint shownCheckpoint;
        int checkpointIndex;

        SpawnerCreator spawnerCreator;
        BaseVehicle? playerVehicle;
        List<Civilisation.SpectatorGroup> spectatorGroups;

        PlayerObject moverObject;
        const int moverObjectModelID = 19133;
        Vector3 moverObjectOffset = new Vector3(0.0f, 0.0f, 1.0f);

        public RaceCreator(Player _player)
        {
            player = _player;
            editingRace = null;
            checkpointIndex = 0;
            spectatorGroups = new List<Civilisation.SpectatorGroup>();
        }

        public void Create()
        {
            editingRace = new Race();
            editingRace.IsCreatorMode = true;
            editingRace.Name = "[Untitled]";
            editingRace.SpawnPoints = new List<Vector3R>();
            editingRace.MapId = -1;
            checkpointIndex = 0;
            editingRace.StartingVehicle = VehicleModelType.Infernus;
            spectatorGroups = new List<Civilisation.SpectatorGroup>();
            isNew = true;
            this.SetPlayerInEditor();
        }

        public void Load(int id)
        {
            if (id > 0)
            {
                Race loadingRace = new Race();
                loadingRace.IsCreatorMode = true;
                loadingRace.Loaded += LoadingRace_Loaded;
                loadingRace.Load(id);
            }
            else player.SendClientMessage(Color.Red, "Error loading race #" + id + " (invalid ID)");
        }

        private void LoadingRace_Loaded(object sender, RaceLoadedEventArgs e)
        {
            if(e.success)
            {
                if(e.race.Creator == player.Name)
                {
                    isNew = false;
                    checkpointIndex = 0;
                    editingRace = e.race;

                    player.SendClientMessage(Color.Green, "Race #" + e.race.Id + " loaded successfully in creation mode");
                    this.SetPlayerInEditor();
                }
                else
                    player.SendClientMessage(Color.Red, "You cannot edit this race because you are not it's creator");
            }
            else
                player.SendClientMessage(Color.Red, "Error loading race (missing mandatory datas)");
        }
        private void SetPlayerInEditor()
        {
            player.VirtualWorld = (int)VirtualWord.EventCreators + player.Id;
            player.EnablePlayerCameraTarget(true);

            Vector3 pos;
            float rot = 0;
            if (editingRace.SpawnPoints.Count > 0)
            {
                pos = editingRace.SpawnPoints[0].Position;
                rot = editingRace.SpawnPoints[0].Rotation;
            }
            else if (editingRace.checkpoints.Count > 0)
                pos = editingRace.checkpoints[0].Position;
            else
                pos = player.Position;

            if (!player.InAnyVehicle)
            {
                BaseVehicle veh = BaseVehicle.Create(VehicleModelType.Infernus, pos, rot, 1, 1);
                veh.VirtualWorld = (int)VirtualWord.EventCreators + player.Id;
                player.DisableRemoteVehicleCollisions(true);
                player.PutInVehicle(veh);
            }
            else
            {
                player.Vehicle.Position = pos;
                player.Vehicle.Angle = rot;
            }

            hud = new HUD(player);
            hud.SetRaceName(editingRace.Name);
            editingMode = EditingMode.Checkpoints;
            hud.SetSelectedIdx("S", editingMode);
            hud.SetTotalCP(editingRace.checkpoints.Count - 1);
            player.SendClientMessage("Race Creator loaded, here are the controls:");
            player.SendClientMessage("    keypad 4:                                Go to previous checkpoint");
            player.SendClientMessage("    keypad 6:                                Go to next checkpoint");
            player.SendClientMessage("    submission key (2/é):                    Open Race menu");
            player.SendClientMessage("    /race help                               Show the controls list");
            player.KeyStateChanged += Player_KeyStateChanged;
            player.EnterCheckpoint += Player_EnterCheckpoint;
            player.EnterRaceCheckpoint += Player_EnterRaceCheckpoint;
            UpdatePlayerCheckpoint();
        }

        public void Unload()
        {
            if (editingRace != null)
            {
                editingRace.Unload();
            }
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
            if (spawnerCreator != null)
            {
                spawnerCreator.Unload();
                spawnerCreator = null;
            }

            foreach(BaseVehicle veh in BaseVehicle.All)
            {
                if (veh.VirtualWorld == (int)VirtualWord.EventCreators + player.Id)
                    veh.Dispose();
            }

            if (spectatorGroups != null)
            {
                foreach (Civilisation.SpectatorGroup spectatorGroup in spectatorGroups)
                    spectatorGroup.Dispose();
            }
            spectatorGroups = null;

            if (player != null)
            {
                player.VirtualWorld = 0;
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
                        { "@checkpoint_nitro", kvp.Value.NextNitro },
                        { "@checkpoint_collision", kvp.Value.NextCollision },
                    };
                    mySQLConnector.Execute("INSERT INTO race_checkpoints " +
                        "(race_id, checkpoint_number, checkpoint_pos_x, checkpoint_pos_y, checkpoint_pos_z, checkpoint_size, checkpoint_type, checkpoint_vehiclechange, checkpoint_nitro, checkpoint_collision) VALUES" +
                        "(@id, @checkpoint_number, @checkpoint_pos_x, @checkpoint_pos_y, @checkpoint_pos_z, @checkpoint_size, @checkpoint_type, @checkpoint_vehiclechange, @checkpoint_nitro, @checkpoint_collision)", param);
                }
                param = new Dictionary<string, object>
                {
                    { "@id", editingRace.Id }
                };
                mySQLConnector.Execute("DELETE FROM race_spawn WHERE race_id=@id", param);
                if (spawnerCreator != null)
                    editingRace.SpawnPoints = spawnerCreator.GetSpawnPoints();
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
                param = new Dictionary<string, object>
                {
                    { "@name", editingRace.Name },
                    { "@mapid", editingRace.MapId == -1 ? null : editingRace.MapId.ToString() },
                    { "@vehicleid", editingRace.StartingVehicle },
                    { "@id", editingRace.Id }
                };
                mySQLConnector.Execute("UPDATE races SET race_name=@name, race_map=@mapid, race_startvehicle=@vehicleid WHERE race_id=@id", param);
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
                    this.editingRace.Name = name;
                    hud.SetRaceName(name);
                    return this.Save();
                }
                else return false;
            }
            return false;
        }

        public void AddCheckpoint(Vector3 position)
        {
            Physics.RayCastCollisionTarget collisionTarget = Physics.ColAndreas.RayCastLine(position + Vector3.UnitZ * 2, position - Vector3.UnitZ * 2);
            position = collisionTarget.Position + Vector3.UnitZ;
            editingMode = EditingMode.Checkpoints;
            if (editingRace.checkpoints.Count == 0)
            {
                editingRace.checkpoints.Add(0, new Checkpoint(position, CheckpointType.Normal));
                checkpointIndex = 0;
            }
            else
            {
                if (checkpointIndex == editingRace.checkpoints.Count - 1) // Add to the end
                {
                    editingRace.checkpoints.Add(editingRace.checkpoints.Count, new Checkpoint(position, CheckpointType.Normal));
                    checkpointIndex = editingRace.checkpoints.Count - 1;
                }
                else
                {
                    Dictionary<int, Checkpoint> tmp = new Dictionary<int, Checkpoint>(editingRace.checkpoints);
                    for (int i = checkpointIndex + 1; i < tmp.Count; i++)
                    {
                        editingRace.checkpoints[i + 1] = tmp[i];
                    }
                    checkpointIndex++;
                    editingRace.checkpoints[checkpointIndex] = new Checkpoint(position, CheckpointType.Normal);
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
                if (editingRace.checkpoints.Count > 0)
                {
                    editingMode = EditingMode.Checkpoints;
                    editingRace.checkpoints[checkpointIndex].Position = position;
                    UpdatePlayerCheckpoint();
                }
                else
                    player.SendClientMessage("Create a checkpoint first with command " + ColorPalette.Primary.Main + "/race addcp");
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
        public void SelectPreviousCP()
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
        }
        public void SelectNextCP()
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
        }
        public void SelectCP(int index)
        {
            if (index >= 0 && index <= editingRace.checkpoints.Count - 1) checkpointIndex = index;
            if (checkpointIndex == 0)
            {
                hud.SetSelectedIdx("S", editingMode);
                player.GameText("CP: Start", 500, 3);
            }
            else if (checkpointIndex == editingRace.checkpoints.Count - 1)
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
        }
        public void AddSpectatorGroup(Vector3 position)
        {
            // TODO remove parameter
            List<PathNode> allPathNodes = PathExtractor.pedNodes;
            List<PathNode> allNearPathNodes = new();

            Vector3 from = editingRace.checkpoints[checkpointIndex].Position;
            foreach (PathNode node in allPathNodes)
            {
                if (node.position.DistanceTo(from) < 40)
                {
                    allNearPathNodes.Add(node);
                }
            }
            foreach (PathNode node in allNearPathNodes)
            {
                spectatorGroups.Add(new SpectatorGroup(node.position + new Vector3(0, 0, 1.7), from, player.VirtualWorld));
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
                                    SelectPreviousCP();
                                    break;
                                }
                            case EditingMode.SpawnPos:
                                {
                                    // Managed by SpawnCreator
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
                                    SelectNextCP();
                                    break;
                                }
                            case EditingMode.SpawnPos:
                                {
                                    // Managed by SpawnCreator
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
            raceDialog.AddItem("Open current checkpoint menu");
            raceDialog.AddItem("Load a map ...");

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
                                if(editingMode == EditingMode.Checkpoints)
                                {
                                    // Going to SpawnPos mode
                                    if (editingRace.checkpoints.Count > 0)
                                    {
                                        checkpointIndex = 0;
                                        UpdatePlayerCheckpoint();
                                        List<Vector3R> spawns = new List<Vector3R>();
                                        if (editingRace.SpawnPoints.Count == 0)
                                        {
                                            spawns.Add(new Vector3R(editingRace.checkpoints[0].Position + Vector3.UnitZ));
                                        }
                                        else
                                        {
                                            spawns = editingRace.SpawnPoints;
                                        }
                                        playerVehicle = player.Vehicle;
                                        spawnerCreator = new SpawnerCreator(player, player.VirtualWorld, editingRace.StartingVehicle.GetValueOrDefault(VehicleModelType.Infernus), spawns);
                                        spawnerCreator.Update += (object sender, SpawnCreatorUpdateEventArgs e) =>
                                        {
                                            hud.SetSelectedIdx((e.selectedIndex + 1) + "/" + e.spawnCount, EditingMode.SpawnPos);
                                        };
                                        spawnerCreator.Quit += (object sender, SpawnCreatorQuitEventArgs e) => {
                                            editingRace.SpawnPoints = e.spawnPoints;
                                            editingMode = EditingMode.Checkpoints;
                                            hud.SetEditingMode(editingMode);
                                        };
                                        editingMode = EditingMode.SpawnPos;
                                        hud.SetEditingMode(editingMode);
                                        hud.SetSelectedIdx("1", EditingMode.SpawnPos);
                                    }
                                    else
                                    {
                                        player.SendClientMessage(Color.Red, "Error, place a checkpoint first ! (/race addcp)");
                                    }
                                }
                                else if(editingMode == EditingMode.SpawnPos)
                                {
                                    // Going to Checkpoint mode
                                    if (spawnerCreator != null)
                                    {
                                        editingRace.SpawnPoints = spawnerCreator.GetSpawnPoints();
                                        spawnerCreator.Unload();
                                        spawnerCreator = null;
                                    }
                                    editingMode = EditingMode.Checkpoints;
                                    hud.SetEditingMode(editingMode);
                                    SelectCP(checkpointIndex); // Update the textdraw
                                    if (playerVehicle != null)
                                        player.PutInVehicle(playerVehicle);
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
                        case 4: // Open checkpoint dialog
							{
                                ShowCheckpointDialog();
                                break;
                            }
                        case 5: // Load a map ...
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
                                        ShowRaceDialog();
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
            Dictionary<int, string> maps = Map.Map.FindAll(text, player);
            if (maps.Count == 0)
            {
                player.Notificate("No results");
                GameMode.mySQLConnector.CloseReader();
                ShowRaceDialog();
            }
            else
            {
                List<int> mapList = new List<int>();
                ListDialog mapListDialog = new ListDialog(maps.Count + " maps found", "Load", "Cancel");
                foreach(var map in maps)
                {
                    mapList.Add(Convert.ToInt32(map.Key));
                    mapListDialog.AddItem(map.Key + "_" + map.Value);
                }
                mapListDialog.Show(player);
                mapListDialog.Response += (sender, eventArgs) =>
                {
                    if (eventArgs.DialogButton == DialogButton.Left)
                    {
                        editingRace.MapId = mapList[eventArgs.ListItem];
                        editingRace.ReloadMap();
                    }
                    else
                    {
                        player.Notificate("Cancelled");
                        ShowRaceDialog();
                    }
                };
            }
        }


        private void Player_EnterCheckpoint(object sender, EventArgs e)
        {
            if(editingMode == EditingMode.Checkpoints)
                ShowCheckpointDialog();
        }

        private void Player_EnterRaceCheckpoint(object sender, EventArgs e)
        {
            if (editingMode == EditingMode.Checkpoints)
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

            cpDialog.AddItem(Color.Red + "Delete checkpoint");

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
                                if (checkpointIndex > 0)
                                    ShowCheckpointEventDialog();
                                else
								{
                                    DeleteCurrentCheckpoint();
								}
                                break;
                            }
                        case 4: // Delete checkpoint
                            {
                                DeleteCurrentCheckpoint();
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
                cpEventDialog.AddItem("Vehicle change [Unchanged]");
            else
                cpEventDialog.AddItem("Vehicle change [" + Color.Green + editingRace.checkpoints[checkpointIndex].NextVehicle.ToString() + Color.White + "]");
            cpEventDialog.AddItem("Set Nitro " + Checkpoint.GetEventStringStatus(editingRace.checkpoints[checkpointIndex].NextNitro));
            cpEventDialog.AddItem("Set Collision " + Checkpoint.GetEventStringStatus(editingRace.checkpoints[checkpointIndex].NextCollision));
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
                                InputDialog cpEventCarNameDialog = new InputDialog("Vehicle name", "Enter the vehicle name or leave blank to remove:", false, "Find", "Cancel");
                                cpEventCarNameDialog.Show(player);
                                cpEventCarNameDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        if (eventArgs.InputText == "")
                                            cp.NextVehicle = null;
                                        else
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
                                cpNitroEventDialog.AddItem(((cp.NextNitro == Checkpoint.EnableDisableEvent.None) ? "> " : "") + "[Unchanged]");
                                cpNitroEventDialog.AddItem(((cp.NextNitro == Checkpoint.EnableDisableEvent.Enable) ? "> " : "") + "[Enable]");
                                cpNitroEventDialog.AddItem(((cp.NextNitro == Checkpoint.EnableDisableEvent.Disable) ? "> " : "") + "[Disable]");
                                cpNitroEventDialog.Show(player);
                                cpNitroEventDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        cp.NextNitro = (Checkpoint.EnableDisableEvent)eventArgs.ListItem;
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
                        case 2: // Collision event
                            {
                                ListDialog cpNitroEventDialog = new ListDialog("Collision event", "Update", "Cancel");
                                cpNitroEventDialog.AddItem(((cp.NextCollision == Checkpoint.EnableDisableEvent.None) ? "> " : "") + "[Unchanged]");
                                cpNitroEventDialog.AddItem(((cp.NextCollision == Checkpoint.EnableDisableEvent.Enable) ? "> " : "") + "[Enable]");
                                cpNitroEventDialog.AddItem(((cp.NextCollision == Checkpoint.EnableDisableEvent.Disable) ? "> " : "") + "[Disable]");
                                cpNitroEventDialog.Show(player);
                                cpNitroEventDialog.Response += (sender, eventArgs) =>
                                {
                                    if (eventArgs.DialogButton == DialogButton.Left)
                                    {
                                        cp.NextCollision = (Checkpoint.EnableDisableEvent)eventArgs.ListItem;
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

        public void DeleteCurrentCheckpoint()
		{
            MessageDialog confirm = new MessageDialog("Confirmation", "You are about to delete the current checkpoint, are you sure ?", "Delete", "Cancel");
            confirm.Response += (object sender, SampSharp.GameMode.Events.DialogResponseEventArgs e) =>
            {
                if(e.DialogButton == DialogButton.Left)
				{
                    Dictionary<int, Checkpoint> tmp = new Dictionary<int, Checkpoint>();
                    int index = 0;
                    for (int i = 0; i <= editingRace.checkpoints.Count -1; i++)
                    {
                        if (i != checkpointIndex) // We don't add the current checkpoint to the new list
                        {
                            tmp.Add(index, editingRace.checkpoints[i]);
                            index++;
                        }
					}
                    editingRace.checkpoints = new Dictionary<int, Checkpoint>(tmp);
                    // By default, the new selected CP will be the next CP, but if it's the last CP we need to select the "new" last CP
                    if (checkpointIndex == editingRace.checkpoints.Count)
                        checkpointIndex--;
                    UpdatePlayerCheckpoint();
                }
            };
            confirm.Show(player);
        }

		public void UpdatePlayerCheckpoint()
        {
            player.DisableCheckpoint();
            player.DisableRaceCheckpoint();
            if(editingRace.checkpoints.Count > 0)
            {
                Checkpoint cp = editingRace.checkpoints[checkpointIndex];

                Checkpoint nextCp = null;
                if (editingRace.checkpoints.ContainsKey(checkpointIndex + 1))
                    nextCp = editingRace.checkpoints[checkpointIndex + 1];

                Vector3 nextPos = (nextCp != null) ? nextCp.Position : Vector3.Zero;

                if (shownCheckpoint == null || shownCheckpoint.IsDisposed)
                    shownCheckpoint = new DynamicRaceCheckpoint(cp.Type, cp.Position, nextPos, cp.Size, player.VirtualWorld, streamdistance: 500);
                else
                {
                    shownCheckpoint.Position = cp.Position;
                    shownCheckpoint.NextPosition = nextPos;
                    shownCheckpoint.Size = cp.Size;
                }
                shownCheckpoint.ShowForPlayer(player);
                Streamer st = new Streamer();
                st.Update(player);
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
        }

        private void moverObject_Edited(object sender, SampSharp.GameMode.Events.EditPlayerObjectEventArgs e)
        {
            if (editingMode == EditingMode.Checkpoints)
            {
                Physics.RayCastCollisionTarget collisionTarget = Physics.ColAndreas.RayCastLine(e.Position + Vector3.UnitZ*2, e.Position - Vector3.UnitZ*2);
                editingRace.checkpoints[checkpointIndex].Position = new Vector3(e.Position.X, e.Position.Y, collisionTarget.Position.Z) + Vector3.UnitZ;
                UpdatePlayerCheckpoint();
                if(e.EditObjectResponse == EditObjectResponse.Final)
                {
                    moverObject.Position = new Vector3(e.Position.X, e.Position.Y, collisionTarget.Position.Z) + Vector3.UnitZ;
                }
            }
            if (e.EditObjectResponse == EditObjectResponse.Cancel)
            {
                player.cameraController.SetBehindPlayer();
                player.cameraController.Enabled = true;
            }
        }
    }
}
