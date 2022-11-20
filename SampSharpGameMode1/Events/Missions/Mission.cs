using SampSharp.GameMode;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using SampSharpGameMode1.Events.Missions.MissionPickups;

namespace SampSharpGameMode1.Events.Missions
{
    public class MissionEventArgs : EventArgs
    {
        public Mission Mission { get; set; }
    }
    public class MissionLoadedEventArgs : MissionEventArgs
    {
        public bool success { get; set; }
        public int availableSlots { get; set; }
    }
    public class MissionFinishedEventArgs : MissionEventArgs
    {
        //public BasePlayer winner { get; set; }
    }
    public class Mission : EventSource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MapId { get; set; }
        private List<MissionStep> Steps { get; set; }
        public bool IsLoaded { get; private set; }
        public bool IsCreatorMode { get; set; }
        public string Creator { get; set; }


        // Launcher only

        private bool isPreparing = false;
        public bool isStarted = false;
        public List<Player> players;
        private Dictionary<Player, MissionPlayerData> playersData = new Dictionary<Player, MissionPlayerData>();
        public List<Player> spectatingPlayers; // Contains spectating players who looses the mission, and others players who spectates without joining
        private Dictionary<Player, HUD> playersLiveInfoHUD = new Dictionary<Player, HUD>();
        private int virtualWorld;
        private SampSharp.GameMode.SAMP.Timer countdownTimer;
        private int countdown;
        public DateTime startedTime;
        private Map map;
        private int currentMissionIndex;

        #region MissionEvents
        public event EventHandler<MissionLoadedEventArgs> Loaded;
        protected virtual void OnLoaded(MissionLoadedEventArgs e)
        {
            Loaded?.Invoke(this, e);
            IsLoaded = e.success;
        }

        public event EventHandler<MissionFinishedEventArgs> Finished;
        protected virtual void OnFinished(MissionFinishedEventArgs e)
        {
            Finished?.Invoke(this, e);
            Player.SendClientMessageToAll(Color.Wheat, $"[Event]{Color.White} The Mission {ColorPalette.Secondary.Main}{e.Mission.Name}{Color.White} is finished !");
        }
        #endregion

        #region PlayerEvents
        private void OnPlayerUpdate(object sender, PlayerUpdateEventArgs e)
        {
        }
        public void OnPlayerDisconnect(object sender, DisconnectEventArgs e)
        {
            OnPlayerFinished((Player)sender, "Disconnected");
        }
        private void OnPlayerExitVehicle(object sender, PlayerVehicleEventArgs e)
        {
        }
        public void OnPlayerVehicleDied(object sender, PlayerEventArgs e)
        {
            OnPlayerFinished((Player)e.Player, "Vehicle destroyed");
        }

        private void OnPlayerKeyStateChanged(object sender, KeyStateChangedEventArgs e)
        {
        }
        #endregion

        public void Load(int id, int virtualworld = -1)
        {
            if (GameMode.mySQLConnector != null)
            {
                Thread t = new Thread(() =>
                {
                    IsLoaded = false;
                    bool errorFlag = false;

                    this.Name = "Mission #1";

                    Steps = new List<MissionStep>();
                    Stage s = new Stage();
                    s.Load(id, virtualworld);
                    s.Complete += Mission_Complete;
                    Steps.Add(s);

                    /*
                    Dictionary<string, string> row;
                    Dictionary<string, object> param = new Dictionary<string, object>
                    {
                        { "@id", id }
                    };
                    GameMode.mySQLConnector.OpenReader("SELECT * FROM derbys WHERE derby_id=@id", param);
                    row = GameMode.mySQLConnector.GetNextRow();
                    if (row.Count > 0)
                    {
                        this.Id = Convert.ToInt32(row["derby_id"]);
                        this.Name = row["derby_name"].ToString();
                        this.Creator = row["derby_creator"].ToString();
                        this.MapId = Convert.ToInt32(row["derby_map"] == "[null]" ? "-1" : row["derby_map"]);
                        this.virtualWorld = virtualworld;
                        if (Convert.ToInt32(row["derby_startvehicle"]) >= 400 && Convert.ToInt32(row["derby_startvehicle"]) <= 611)
                        {
                            this.StartingVehicle = (VehicleModelType)Convert.ToInt32(row["derby_startvehicle"]);
                        }
                        else
                        {
                            this.StartingVehicle = null;
                        }
                        this.MinimumHeight = (float)Convert.ToDouble(row["derby_minheight"]);
                    }
                    else
                    {
                        Logger.WriteLineAndClose($"Derby.cs - Derby.Load:E: Trying to load derby #{id} but it does not exists");
                        errorFlag = true;
                    }
                    GameMode.mySQLConnector.CloseReader();

                    int availableSlots = 0;
                    if (!errorFlag)
                    {
                        GameMode.mySQLConnector.OpenReader("SELECT spawn_pos_x, spawn_pos_y, spawn_pos_z, spawn_rot " +
                            "FROM derby_spawn " +
                            "WHERE derby_id=@id", param);
                        row = GameMode.mySQLConnector.GetNextRow();

                        this.SpawnPoints = new List<Vector3R>();

                        Vector3R pos;
                        while (row.Count > 0)
                        {
                            pos = new Vector3R(new Vector3(
                                        (float)Convert.ToDouble(row["spawn_pos_x"]),
                                        (float)Convert.ToDouble(row["spawn_pos_y"]),
                                        (float)Convert.ToDouble(row["spawn_pos_z"])
                                    ),
                                    (float)Convert.ToDouble(row["spawn_rot"])
                                );
                            if (pos.Position != Vector3.Zero)
                            {
                                this.SpawnPoints.Add(pos);
                                availableSlots++;
                            }
                            row = GameMode.mySQLConnector.GetNextRow();
                        }
                        GameMode.mySQLConnector.CloseReader();
                    }

                    if (!errorFlag)
                    {
                        GameMode.mySQLConnector.OpenReader(
                            "SELECT pickup_id, pickup_event, pickup_model, pickup_pos_x, pickup_pos_y, pickup_pos_z " +
                            "FROM derby_pickups " +
                            "WHERE derby_id=@id", param);
                        row = GameMode.mySQLConnector.GetNextRow();

                        this.Pickups = new List<DerbyPickup>();

                        int type = -1;
                        int modelid = -1;
                        Vector3 pos;
                        while (row.Count > 0)
                        {
                            type = Convert.ToInt32(row["pickup_event"]);
                            modelid = Convert.ToInt32(row["pickup_model"]);
                            pos = new Vector3(
                                        (float)Convert.ToDouble(row["pickup_pos_x"]),
                                        (float)Convert.ToDouble(row["pickup_pos_y"]),
                                        (float)Convert.ToDouble(row["pickup_pos_z"])
                                );
                            this.Pickups.Add(new DerbyPickup(modelid, pos, virtualworld, (DerbyPickup.PickupEvent)type));
                            row = GameMode.mySQLConnector.GetNextRow();
                        }
                        GameMode.mySQLConnector.CloseReader();
                    }
                    */

                    MissionLoadedEventArgs args = new MissionLoadedEventArgs();
                    args.Mission = this;
                    args.success = !errorFlag;
                    args.availableSlots = 10;

                    if (this.MapId > -1)
                    {
                        this.map = new Map();
                        this.map.Loaded += (sender, eventArgs) =>
                        {
                            OnLoaded(args);
                        };
                        this.map.Load(this.MapId, virtualWorld);
                    }
                    else
                        OnLoaded(args);
                });
                t.Start();
            }
        }

        public bool IsPlayerSpectating(Player player)
        {
            return this.spectatingPlayers.Contains(player);
        }

        public List<Player> GetPlayers()
        {
            return this.players;
        }

        public Boolean IsPlayable()
        {
            return true;
        }

        public void Prepare(List<EventSlot> slots)
        {
            if (IsPlayable())
            {
                bool isAborted = false;
                this.isPreparing = true;
                this.players = new List<Player>();
                this.spectatingPlayers = new List<Player>();

                foreach (EventSlot slot in slots)
                {
                    MissionPlayerData playerData = new MissionPlayerData();
                    playerData.spectatePlayerIndex = -1;
                    /*
                    playersLiveInfoHUD[slot.Player] = new HUD(slot.Player, "derbyhud.json");
                    playersLiveInfoHUD[slot.Player].Hide("iconrockets");
                    playersLiveInfoHUD[slot.Player].Hide("remainingrockets");
                    playersLiveInfoHUD[slot.Player].SetText("remainingplayers", slots.Count.ToString(@"000"));
                    */

                    slot.Player.VirtualWorld = virtualWorld;

                    slot.Player.Update += OnPlayerUpdate;
                    slot.Player.ExitVehicle += OnPlayerExitVehicle;
                    slot.Player.KeyStateChanged += OnPlayerKeyStateChanged;
                    slot.Player.Disconnected += OnPlayerDisconnect;

                    //playerData.startPosition = new Vector3R(this.SpawnPoints.Position, this.SpawnPoints.Rotation);
                    playersData.Add(slot.Player, playerData);

                    players.Add(slot.Player);
                }

                if (!isAborted)
                {
                    SampSharp.GameMode.SAMP.Timer preparationTimer = new SampSharp.GameMode.SAMP.Timer(3000, false);
                    preparationTimer.Tick += (object sender, EventArgs e) =>
                    {
                        countdown = 3;
                        countdownTimer = new SampSharp.GameMode.SAMP.Timer(1000, true);
                        countdownTimer.Tick += CountdownTimer_Tick;
                    };
                }
                else
                {
                    Player.SendClientMessageToAll($"Mission {this.Name} aborted");
                    //TODO: remettre les joueurs dans leurs vw et positions initiales
                }
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            foreach (Player p in players)
            {
                p.GameText(countdown.ToString(), 1000, 6);
                p.PlaySound((countdown > 0) ? 1056 : 1057);
            }
            if (countdown == 0)
            {
                countdownTimer.IsRepeating = false;
                countdownTimer.IsRunning = false;
                countdownTimer.Dispose();
                Start();
            }
            /*
            if (countdown == 1)
            {
                foreach (Player p in players)
                {
                    if (p.InAnyVehicle)
                    {
                        p.Vehicle.Position = playersData[p].startPosition.Position + Vector3.UnitZ;
                        p.Vehicle.Angle = playersData[p].startPosition.Rotation;
                    }
                    else
                    {
                        p.Position = playersData[p].startPosition.Position + Vector3.UnitZ;
                        p.Angle = playersData[p].startPosition.Rotation;
                    }
                }
            }
            */
            countdown--;
        }

        public void Start()
        {
            if (isPreparing && countdown == 0)
            {
                currentMissionIndex = 0;
                Steps[currentMissionIndex].Execute(players);
                startedTime = DateTime.Now;
                this.isPreparing = false;
                this.isStarted = true;
                foreach (Player p in players)
                {
                    p.Notificate($"Mission: {this.Name}");
                    if (p.Vehicle != null)
                        p.Vehicle.Engine = true;
                }
            }
        }

        private void Mission_Complete(object sender, MissionStepEventArgs e)
        {
            if(e.Success)
            {
                if(currentMissionIndex >= (Steps.Count - 1))
                {
                    OnPlayerFinished(e.Player, "Finished");
                }
            }
        }

        public void OnPlayerFinished(Player player, string reason)
        {
            if (!players.Contains(player))
                return;
            if (reason.Equals("Finished"))
            {
                player.GameText("Mission passed !", 5000, 4);
                Logger.WriteLineAndClose($"Mission.cs - OnPlayerFinished:I: {player.Name} finished the mission {this.Name}");
            }
            else if (reason.Equals("Leave") || reason.Equals("Disconnected"))
            {
                Event.SendEventMessageToPlayer(player, "You left the mission");
                Logger.WriteLineAndClose($"Mission.cs - OnPlayerFinished:I: {player.Name} left the mission {this.Name}");
                Event.SendEventMessageToAll(player.pEvent, $"{Color.Orange}{player.Name}{Color.White} has left the mission");
                player.GameText("GAME OVER", 5000, 4);
            }
            else
            {
                Event.SendEventMessageToPlayer(player, "You lost (reason: " + reason + ")");
                Logger.WriteLineAndClose($"Mission.cs - OnPlayerFinished:I: {player.Name} has been ejected from the mission {this.Name} (reason: {reason})");
                Event.SendEventMessageToAll(player.pEvent, $"{Color.Orange}{player.Name}{Color.White} has lost the mission (reason: {reason})");
                player.GameText("GAME OVER", 5000, 4);
            }

            players.Remove(player);
            if (players.Count == 0) // Si c'est le vainqueur
            {
                SampSharp.GameMode.SAMP.Timer ejectionTimer = new SampSharp.GameMode.SAMP.Timer(2000, false);
                ejectionTimer.Tick += (object sender, EventArgs e) =>
                {
                    MissionFinishedEventArgs args = new MissionFinishedEventArgs();
                    args.Mission = this;
                    OnFinished(args);
                };
            }
            else
            {
                if (!reason.Equals("Disconnected"))
                {
                    if (player.InAnyVehicle)
                    {
                        BaseVehicle vehicle = player.Vehicle;
                        player.RemoveFromVehicle();
                        vehicle.Dispose();
                    }
                    player.ToggleSpectating(true);
                    if (players[0].InAnyVehicle)
                        player.SpectateVehicle(players[0].Vehicle);
                    else
                        player.SpectatePlayer(players[0]);
                    spectatingPlayers.Add(player);
                    playersData[player].status = MissionPlayerStatus.Spectating;
                    playersData[player].spectatePlayerIndex = 0;
                }
            }
        }

        public void Eject(Player player)
        {
            //playersLiveInfoHUD[player].Hide();
            players.RemoveAll(x => x.Equals(player));
            spectatingPlayers.RemoveAll(x => x.Equals(player));
            playersData.Remove(player);

            if (!player.IsDisposed)
            {
                if (player.InAnyVehicle)
                {
                    BaseVehicle vehicle = player.Vehicle;
                    player.RemoveFromVehicle();
                    if (vehicle != null) vehicle.Dispose();
                }
                player.DisableCheckpoint();
                player.DisableRaceCheckpoint();
                player.ExitVehicle -= OnPlayerExitVehicle;
                player.KeyStateChanged -= OnPlayerKeyStateChanged;
                player.Disconnected -= OnPlayerDisconnect;
                player.ToggleSpectating(false);
                player.VirtualWorld = 0;
                player.pEvent = null;
                player.Spawn();
            }
        }

        public void Unload()
        {
            if (!IsCreatorMode)
            {
                foreach (Player p in BasePlayer.All)
                {
                    if (p.VirtualWorld == this.virtualWorld)
                        Eject(p);
                }
            }
            if (map != null)
                map.Unload();
            foreach (BaseVehicle v in BaseVehicle.All)
            {
                if (v.VirtualWorld == this.virtualWorld)
                    v.Dispose();
            }
            Logger.WriteLineAndClose($"Mission.cs - Mission.Unload:I: Mission {this.Name} unloaded");
        }
    }
}
