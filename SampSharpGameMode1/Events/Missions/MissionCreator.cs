using SampSharp.Core;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Text;
using static SampSharpGameMode1.Display.TextdrawLayer;

namespace SampSharpGameMode1.Events.Missions
{
    public class MissionCreator : EventCreator
    {
        class HUD : Display.HUD
        {
            private string selectedIdx;
            public HUD(Player player) : base(player, "missioncreator.json")
            {
            }
            public void Destroy()
            {
                layer.HideAll();
            }
            public void SetMissionName(string name)
            {
                string _name = "[Untitled]";
                if (name is not null)
                {
                    if (name.Length > 14)
                        _name = name[..14] + "...";
                    else
                        _name = name;
                }
                layer.SetTextdrawText("missionname", _name);
            }
            public void SetSelectedStage(string idx)
            {
                selectedIdx = idx;
                layer.SetTextdrawText("selectedidx", "Stage: " + idx);
            }
            public void SetTotalStages(int totalStages)
            {
                layer.SetTextdrawText("totalstage", "Total stages: " + totalStages);
            }
        }

        protected MySQLConnector mySQLConnector = MySQLConnector.Instance();

        Player player;

        HUD hud;

        public int EventId { get => editingMission.Id; }
        public Mission editingMission = null;
        public bool isNew;
        public int currentMissionStepIndex;

        public MissionCreator(Player _player)
        {
            player = _player;
            editingMission = null;
        }

        public void Create()
        {
            editingMission = new();
            editingMission.IsCreatorMode = true;
            editingMission.Name = "[Untitled]";
            editingMission.MapId = -1;
            editingMission.Steps = new List<MissionStep>();
            currentMissionStepIndex = -1;
            isNew = true;
            this.SetPlayerInEditor();
        }

        public void Load(int id)
        {
            if (id > 0)
            {
                Mission editingMission = new();
                editingMission.IsCreatorMode = true;
                editingMission.Loaded += EditingMission_Loaded;
                editingMission.Load(id);
            }
            else player.SendClientMessage(Color.Red, "Error loading mission #" + id + " (invalid ID)");
        }

        private async void EditingMission_Loaded(object sender, MissionLoadedEventArgs e)
        {
            await TaskHelper.SwitchToMainThread();
            if (e.success)
            {
                if (e.Mission.Creator == player.Name)
                {
                    isNew = false;
                    editingMission = e.Mission;
                    currentMissionStepIndex = 0;

                    player.SendClientMessage(Color.Green, "Mission #" + e.Mission.Id + " loaded successfully in creation mode");
                    this.SetPlayerInEditor();
                }
                else
                    player.SendClientMessage(Color.Red, "You cannot edit this mission because you are not it's creator");
            }
            else
                player.SendClientMessage(Color.Red, "Error loading mission (missing mandatory datas)");
        }
        private void SetPlayerInEditor()
        {
            player.VirtualWorld = (int)VirtualWord.EventCreators + player.Id;
            //player.EnablePlayerCameraTarget(true);

            if(editingMission.Steps.Count > 0)
            {
                player.Teleport(editingMission.Steps[0].SpawnPoint.Position);
            }
            
            hud = new HUD(player);
            hud.SetMissionName(editingMission.Name);
            hud.SetTotalStages(editingMission.Steps.Count - 1);
            
            player.SendClientMessage("Mission Creator loaded, here are the controls:");
            player.SendClientMessage($"    {ColorPalette.Secondary.Main}/mission help                               {Color.White}Show the controls list");
            player.SendClientMessage($"    {ColorPalette.Secondary.Main}/mission create [stage/actor/npc]           {Color.White}Show the controls list");
            player.SendClientMessage($"    {ColorPalette.Secondary.Main}/mission help                               {Color.White}Show the controls list");
            player.SendClientMessage($"{ColorPalette.Error.Main}Warning: the mission creator is not fully developped yet, and the save/load function has been disabled.");
            //player.KeyStateChanged += Player_KeyStateChanged;
        }

        public void CreateStage()
        {
            editingMission.Steps.Add(new Stage());
            currentMissionStepIndex = editingMission.Steps.Count - 1;
            player.SendClientMessage("Stage created");
        }

        public void CreateActor(int modelid, Vector3R pos)
        {
            if(editingMission.Steps.Count > 0)
            {
                if(currentMissionStepIndex >= 0 && currentMissionStepIndex < editingMission.Steps.Count)
                {
                    DynamicActor actor = new DynamicActor(modelid, pos.Position, pos.Rotation, invulnerable: false, streamdistance: 50, worldid: (int)VirtualWord.EventCreators + player.Id);
                    Logger.WriteLineAndClose($"MissionCreator.cs - MissionCreator.CreateActor:I: actor is invulnerable: {actor.IsInvulnerable}"); // ": False"
                    IStreamer st = GameMode.Instance.Services.GetService<IStreamer>();
                    st.PlayerGiveDamageDynamicActor += (sender, args) =>
                    {
                        Logger.WriteLineAndClose($"MissionCreator.cs - MissionCreator.CreateActor:I: {args.Player} has made {args.Amount} damage to {(sender as DynamicActor).Id}");
                    };
                    editingMission.Steps[currentMissionStepIndex].Actors.Add(actor);
                    player.SendClientMessage("Actor created: " + actor.Id);
                }
            }
            else
            {
                player.SendClientMessage($"You must create a mission step first ! Type {ColorPalette.Primary.Main}/mission create [stage/cutscene]");
            }
        }

        public void CreateNPC(string name)
        {
            player.SendClientMessage(Color.Red, "The function to create NPC is not implemented in Open.MP yet");
            
            MessageDialog confirm = new MessageDialog("Create NPC", "You are about to start the recording of NPC file, do you want to continue recording ?", "Yes", "No/Cancel");
            confirm.Response += (sender, args) =>
            {
                if(args.DialogButton == DialogButton.Left)
                {
                    player.StartRecordingPlayerData(player.InAnyVehicle ? PlayerRecordingType.Driver : PlayerRecordingType.OnFoot, name);
                    player.SendClientMessage($"NPC Recording ... Write {ColorPalette.Secondary.Main}/stoprecord{Color.White} to stop the record");
                }
            };
            confirm.Show(player);
            
        }
        public void AddVehicle(BaseVehicle vehicle)
        {
            editingMission.Steps[currentMissionStepIndex].Vehicles.Add(vehicle);
            player.SendClientMessage("Vehicle added to the stage/cutscene");
        }

        public Boolean Save()
        {
            /*
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
            */
            return false;
        }

        public Boolean Save(string name) // Only if the race does not already exist
        {
            /*
            if (editingMission != null && name.Length > 0)
            {
                Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@race_name", name },
                    { "@race_creator", player.Name },
                };
                editingMission.Id = (int)mySQLConnector.Execute("INSERT INTO races " +
                    "(race_name, race_creator, race_startvehicle) VALUES" +
                    "(@race_name, @race_creator, @race_startvehicle)", param);
                if (mySQLConnector.RowsAffected > 0)
                {
                    this.editingMission.Name = name;
                    //hud.SetRaceName(name);
                    return this.Save();
                }
                else return false;
            }
            */
            return false;
        }

        public void Unload()
        {
            editingMission?.Unload();
            editingMission = null;
        
            hud?.Destroy();
            hud = null;
        
            foreach (BaseVehicle veh in BaseVehicle.All)
            {
                if (veh.VirtualWorld == (int)VirtualWord.EventCreators + player.Id)
                    veh.Dispose();
            }

            foreach(DynamicActor actor in DynamicActor.All)
            {
                if(actor.IsVisibleInWorld((int)VirtualWord.EventCreators + player.Id))
                    actor.Dispose();
            }

            if (player != null)
            {
                player.VirtualWorld = 0;
                player.CancelEdit();
                //player.KeyStateChanged -= Player_KeyStateChanged;
            }
        }
    }
}
