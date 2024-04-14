using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SampSharpGameMode1.Events.Races
{
    public class RaceEventArgs : EventArgs
    {
        public Race race { get; set; }
    }
    public class RaceLoadedEventArgs : EventArgs
    {
        public bool success { get; set; }
        public Race race { get; set; }
        public int availableSlots { get; set; }
    }
    public class RaceFinishedEventArgs : RaceEventArgs
    {
        public BasePlayer winner { get; set; }
    }

    public class Race : EventSource
    {
        public const int MIN_PLAYERS_IN_RACE = 0;
        public const int MAX_PLAYERS_IN_RACE = 20;

        public Dictionary<int, Checkpoint> checkpoints = new Dictionary<int, Checkpoint>();

        public int Id { get; set; }
        public string Name { get; set; }
        public int Laps { get; set; }
        public VehicleModelType? StartingVehicle { get; set; }
        public List<Vector3R> SpawnPoints { get; set; }
        public int MapId { get; set; }
        public bool IsLoaded { get; private set; }
        public bool IsCreatorMode { get; set; }
        public string Creator { get; set; }

        // Common Events

        public event EventHandler<RaceLoadedEventArgs> Loaded;

        protected virtual void OnLoaded(RaceLoadedEventArgs e)
        {
            Loaded?.Invoke(this, e);
            IsLoaded = e.success;
        }

        // Launcher only

        private bool isPreparing = false;
        public bool isStarted = false;
        public List<Player> players;
        public Dictionary<Player, RacePlayer> playersData = new Dictionary<Player, RacePlayer>();
        public List<Player> spectatingPlayers; // Contains spectating players who finished the race, and others players who spectate without racing
        private EventHandler<EventArgs> checkpointEventHandler;
        private Dictionary<Player, HUD> playersRecordsHUD = new Dictionary<Player, HUD>();
        private Dictionary<Player, HUD> playersLiveInfoHUD = new Dictionary<Player, HUD>();
        private Dictionary<Player, HUD> playerCPLiveHUD = new Dictionary<Player, HUD>();
        private List<CheckpointLiveInfo> checkpointLiveInfos = new List<CheckpointLiveInfo>();
        private Dictionary<Player, TimeSpan> playersTimeSpan = new Dictionary<Player, TimeSpan>();
        private Dictionary<string, TimeSpan> records = new Dictionary<string, TimeSpan>();
        Dictionary<BasePlayer, BaseVehicle> playersVehicles = new(); // Used in Prepare
        private int virtualWorld;
        private SampSharp.GameMode.SAMP.Timer countdownTimer;
        private int countdown;
        public DateTime startedTime;
        private SampSharp.GameMode.SAMP.Timer stopWatchTimer;
        private Map.Map map;
        //private List<Civilisation.SpectatorGroup> spectatorGroups = new List<Civilisation.SpectatorGroup>();
        private List<Actor> spectators = new List<Actor>();
        private Player winner;

        public struct PlayerCheckpointData
        {
            public PlayerCheckpointData(Checkpoint cp, TimeSpan time, VehicleModelType model, Vector3 velocity, float angle)
            {
                this.Checkpoint = cp;
                this.Time = time;
                this.VehicleModel = model;
                this.VehicleVelocity = velocity;
                this.VehicleAngle = angle;
            }
            public Checkpoint Checkpoint { get; set; }
            public TimeSpan Time { get; set; }
            public VehicleModelType VehicleModel { get; set; }
            public Vector3 VehicleVelocity { get; set; }
            public float VehicleAngle { get; set; }
        }
        public Dictionary<Player, PlayerCheckpointData> playerLastCheckpointData = new Dictionary<Player, PlayerCheckpointData>();

        // Launcher Events

        public event EventHandler<RaceFinishedEventArgs> Finished;

        protected virtual void OnFinished(RaceFinishedEventArgs e)
        {
            Finished?.Invoke(this, e);
            Player.SendClientMessageToAll(Color.Wheat, $"[Event]{Color.White} The Race {ColorPalette.Secondary.Main}{e.race.Name}{Color.White} is finished, the winner is {Color.Orange}{(e.winner?.Name ?? "nobody")}{Color.White} !");
            foreach(Actor spectator in spectators)
            {
                spectator.Dispose();
            }
            spectators.Clear();
        }

        #region Player's event
        public void OnPlayerDisconnect(object sender, DisconnectEventArgs e)
        {
            OnPlayerFinished((Player)sender, "Disconnected");
        }
        private void OnPlayerKeyStateChanged(object sender, SampSharp.GameMode.Events.KeyStateChangedEventArgs e)
        {
            Player spectator = (Player)sender;
            if (spectatingPlayers.Contains(spectator))
            {
                Player target;
                switch (e.NewKeys)
                {
                    case Keys.Fire:
                        playersData[(Player)sender].spectatePlayerIndex++;
                        if (playersData[(Player)sender].spectatePlayerIndex >= players.Count)
                        {
                            playersData[(Player)sender].spectatePlayerIndex = 0;
                        }
                        target = players[playersData[(Player)sender].spectatePlayerIndex];
                        if (target.InAnyVehicle)
                            spectator.SpectateVehicle(target.Vehicle);
                        else
                            spectator.SpectatePlayer(target);
                        spectator.Notificate(target.Name);
                        break;
                    case Keys.Aim:
                        playersData[(Player)sender].spectatePlayerIndex--;
                        if (playersData[(Player)sender].spectatePlayerIndex < 0)
                        {
                            playersData[(Player)sender].spectatePlayerIndex = players.Count - 1;
                        }
                        target = players[playersData[(Player)sender].spectatePlayerIndex];
                        if (target.InAnyVehicle)
                            spectator.SpectateVehicle(target.Vehicle);
                        else
                            spectator.SpectatePlayer(target);
                        spectator.Notificate(target.Name);
                        break;
                }
            }
            else
            {
                if (e.NewKeys == Keys.Yes)
                {
                    this.RespawnPlayerOnLastCheckpoint((Player)sender, false);
                }
            }
        }

        private void OnPlayerExitVehicle(object sender, PlayerVehicleEventArgs e)
        {
            e.Player.Position = e.Vehicle.Position;
            SampSharp.GameMode.SAMP.Timer timer = new SampSharp.GameMode.SAMP.Timer(100, false);
            timer.Tick += (sender, e2) =>
            {
                if (!e.Vehicle.IsDisposed)
                    e.Player.PutInVehicle(e.Vehicle);
            };
        }
        public void OnPlayerVehicleDied(object sender, SampSharp.GameMode.Events.PlayerEventArgs e)
        {
            OnPlayerFinished((Player)e.Player, "Vehicle destroyed");
        }
        #endregion

        public Race()
		{
            checkpointEventHandler = (sender, eventArgs) => { OnPlayerEnterCheckpoint((Player)sender); };
        }

        public void Load(int id, int virtualworld = -1)
        {
            if (GameMode.mySQLConnector != null)
            {
                Thread t = new Thread(() =>
                {
                    bool errorFlag = false;
                    Dictionary<string, string> row;
                    Dictionary<string, object> param = new Dictionary<string, object>
                    {
                        { "@id", id }
                    };
                    GameMode.mySQLConnector.OpenReader("SELECT * FROM races WHERE race_id=@id", param);
                    row = GameMode.mySQLConnector.GetNextRow();
                    if (row.Count > 0)
                    {
                        this.Id = Convert.ToInt32(row["race_id"]);
                        this.Name = row["race_name"].ToString();
                        this.Creator = row["race_creator"].ToString();
                        this.Laps = Convert.ToInt32(row["race_laps"]);
                        this.MapId = Convert.ToInt32(row["race_map"] == "[null]" ? "-1" : row["race_map"]);
                        this.virtualWorld = virtualworld;
                        if (Convert.ToInt32(row["race_startvehicle"]) >= 400 && Convert.ToInt32(row["race_startvehicle"]) <= 611)
                        {
                            this.StartingVehicle = (VehicleModelType)Convert.ToInt32(row["race_startvehicle"]);
                        }
                        else
                        {
                            this.StartingVehicle = null;
                        }
                    }
                    else
                        errorFlag = true;
                    GameMode.mySQLConnector.CloseReader();

                    if(!errorFlag)
                    {
                        GameMode.mySQLConnector.OpenReader("SELECT * " +
                            "FROM race_checkpoints " +
                            "WHERE race_id=@id ORDER BY checkpoint_number", param);
                        row = GameMode.mySQLConnector.GetNextRow();
                        if (row.Count == 0) errorFlag = true;

                        this.checkpoints.Clear();
                        Checkpoint checkpoint;
                        int idx = 0;
                        while (row.Count > 0)
                        {
                            checkpoint = new Checkpoint(idx, new Vector3(
                                    (float)Convert.ToDouble(row["checkpoint_pos_x"]),
                                    (float)Convert.ToDouble(row["checkpoint_pos_y"]),
                                    (float)Convert.ToDouble(row["checkpoint_pos_z"])
                                ), (CheckpointType)Convert.ToInt32(row["checkpoint_type"]), (float)Convert.ToDouble(row["checkpoint_size"]));

                            if (row["checkpoint_vehiclechange"].Equals("[null]"))
                                checkpoint.NextVehicle = null;
                            else
                                checkpoint.NextVehicle = (VehicleModelType)Convert.ToInt32(row["checkpoint_vehiclechange"]);

                            if (row["checkpoint_nitro"].Equals("[null]"))
                                checkpoint.NextNitro = Checkpoint.EnableDisableEvent.None;
                            else
                                checkpoint.NextNitro = (Checkpoint.EnableDisableEvent)Convert.ToInt32(row["checkpoint_nitro"]);

                            if (row["checkpoint_collision"].Equals("[null]"))
                                checkpoint.NextCollision = Checkpoint.EnableDisableEvent.None;
                            else
                                checkpoint.NextCollision = (Checkpoint.EnableDisableEvent)Convert.ToInt32(row["checkpoint_collision"]);

                            checkpoint.PlayerVehicleChanged += Checkpoint_PlayerVehicleChanged;
                            this.checkpoints.Add(idx++, checkpoint);
                            row = GameMode.mySQLConnector.GetNextRow();
                        }
                        GameMode.mySQLConnector.CloseReader();
                    }

                    int availableSlots = 0;
                    if (!errorFlag)
                    {
                        GameMode.mySQLConnector.OpenReader("SELECT spawn_index, spawn_pos_x, spawn_pos_y, spawn_pos_z, spawn_rot " +
                            "FROM race_spawn " +
                            "WHERE race_id=@id ORDER BY spawn_index", param);
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
                            if(pos.Position != Vector3.Zero)
                            {
                                this.SpawnPoints.Add(pos);
                                availableSlots++;
                            }
                            row = GameMode.mySQLConnector.GetNextRow();
                        }
                        GameMode.mySQLConnector.CloseReader();
                    }

                    if(!errorFlag)
                    {
                        GameMode.mySQLConnector.OpenReader("SELECT player_id, record_duration, name " +
                            "FROM race_records INNER JOIN users ON race_records.player_id = users.id " +
                            "WHERE race_id=@id ORDER BY record_duration LIMIT 5", param);
                        row = GameMode.mySQLConnector.GetNextRow();

                        while (row.Count > 0)
                        {
                            records[row["name"]] = TimeSpan.Parse(row["record_duration"]);
                            row = GameMode.mySQLConnector.GetNextRow();
                        }
                        GameMode.mySQLConnector.CloseReader();
                    }

                    RaceLoadedEventArgs args = new RaceLoadedEventArgs();
                    args.success = !errorFlag;
                    args.race = this;
                    args.availableSlots = availableSlots;

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

		public Boolean IsPlayable()
        {
            return (checkpoints.Count > 0 && StartingVehicle != null && SpawnPoints.Count > Race.MIN_PLAYERS_IN_RACE) ? true : false;
        }

        public void Prepare(List<EventSlot> slots)
        {
            if(IsPlayable())
            {
                bool isAborted = false;
                this.isPreparing = true;
                this.players = new List<Player>();
                this.spectatingPlayers = new List<Player>();
                this.checkpointLiveInfos = new List<CheckpointLiveInfo>();
                for(int i = 0; i < this.checkpoints.Count; i ++)
                    this.checkpointLiveInfos.Add(new CheckpointLiveInfo());

                Random rdm = new Random();
                List<int> generatedPos = new List<int>();
                List<int> remainingPos = new List<int>();
                for (int i = 0; i < slots.Count; i++)
                    remainingPos.Add(i);
                int pos;

                string displayedRecord;
                /*
                PathNode nearestPathNode = Civilisation.PathTools.GetNeirestPathNode(this.checkpoints[0].Position);
                List<PathNode> neighbors = Civilisation.PathTools.GetNeighbors(nearestPathNode);
                PathNode neighbor1 = neighbors[0];
                for(int i = 0; i < 5; i++)
                {
                    Actor actor = Actor.Create(47, nearestPathNode.position + new Vector3(rdm.NextDouble() + i, rdm.NextDouble() + i, 1), 0);
                    actor.VirtualWorld = this.virtualWorld;
                    actor.ApplyAnimation("ON_LOOKERS", "shout_02", 4.1f, true, false, false, true, 0);
                    actor.IsInvulnerable = false;
                    Logger.WriteLineAndClose($"Race.cs - Race.Prepare:I: Actor Id {actor.Id} created at pos {actor.Position} and vw {actor.VirtualWorld}");
                    spectators.Add(actor);
                }
                */

                Dictionary<string, string> row;
                playersVehicles = new Dictionary<BasePlayer, BaseVehicle>();
                foreach (EventSlot slot in slots)
                {
                    slot.Player.Position = slot.Player.Position;
                    RacePlayer playerData = new RacePlayer();
                    playerData.spectatePlayerIndex = -1;
                    playerData.status = RacePlayerStatus.Running;
                    playerData.nextCheckpoint = checkpoints[1];

                    // Get player record
                    Dictionary<string, object> param = new Dictionary<string, object>
                    {
                        { "@race_id", this.Id },
                        { "@player_id", slot.Player.DbId}
                    };
                    GameMode.mySQLConnector.OpenReader("SELECT record_duration " +
                        "FROM race_records WHERE race_id=@race_id AND player_id=@player_id", param);
                    row = GameMode.mySQLConnector.GetNextRow();

                    if (row.Count > 0)
                        playerData.record = TimeSpan.Parse(row["record_duration"]);
                    else
                        playerData.record = TimeSpan.Zero;

                    GameMode.mySQLConnector.CloseReader();

                    playersLiveInfoHUD[slot.Player] = new HUD(slot.Player, "racelive.json");
                    playersLiveInfoHUD[slot.Player].SetText("racename", this.Name);
                    playersLiveInfoHUD[slot.Player].SetText("stopwatch", "00:00:00.000");
                    playersLiveInfoHUD[slot.Player].Hide("checkpointtime");
                    playersLiveInfoHUD[slot.Player].Hide("checkpointdelta");
                    playersLiveInfoHUD[slot.Player].Hide("nitro");

                    playersRecordsHUD[slot.Player] = new HUD(slot.Player, "racerecords.json");
                    int recordIdx = 1;
                    foreach(KeyValuePair<string, TimeSpan> record in records)
                    {
                        displayedRecord = (record.Value.Hours > 0) ? record.Value.ToString(@"hh\:mm\:ss\.fff") : record.Value.ToString(@"mm\:ss\.fff");
                        playersRecordsHUD[slot.Player].SetText("localRecord" + (recordIdx++) + "Label", "~W~" + record.Key + "~R~ - ~G~" + displayedRecord);
                    }

                    playerCPLiveHUD[slot.Player] = new HUD(slot.Player, "racecplive.json");
                    playerCPLiveHUD[slot.Player].Hide();


                    slot.Player.VirtualWorld = virtualWorld;
                    slot.Player.ToggleControllable(true);
                    slot.Player.ResetWeapons();
                    Thread.Sleep(10); // Used to prevent AntiCheat to detect weapon before player enters in vehicle

                    slot.Player.Disconnected += OnPlayerDisconnect;
                    slot.Player.EnterCheckpoint += checkpointEventHandler;
                    slot.Player.EnterRaceCheckpoint += checkpointEventHandler;
                    slot.Player.KeyStateChanged += OnPlayerKeyStateChanged;
                    slot.Player.ExitVehicle += OnPlayerExitVehicle;

                    pos = remainingPos[rdm.Next(0, remainingPos.Count)];
                    remainingPos.Remove(pos);

                    playerData.startPosition = new Vector3R(this.SpawnPoints[pos].Position, this.SpawnPoints[pos].Rotation);
                    playersData.Add(slot.Player, playerData);

                    BaseVehicle veh = BaseVehicle.Create(StartingVehicle.GetValueOrDefault(VehicleModelType.Bike), this.SpawnPoints[pos].Position, this.SpawnPoints[pos].Rotation, 1, 1);
                    veh.VirtualWorld = virtualWorld;
                    veh.Engine = false;
                    veh.Died += OnPlayerVehicleDied;
                    playersVehicles.Add(slot.Player, veh);
                    slot.Player.PutInVehicle(veh);

                    slot.Player.GameText("Press ~k~~CONVERSATION_YES~ ~n~or type /rr~n~ to respawn", 2000, 6);

                    UpdatePlayerCheckpoint(slot.Player);
                    players.Add(slot.Player);
                }

                stopWatchTimer = new SampSharp.GameMode.SAMP.Timer(10, true);
                stopWatchTimer.Tick += RaceStopWatch;

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
                    //TODO: remettre les joueurs dans leurs vw et positions initiales
                }
            }
        }

        private void RaceStopWatch(object sender, EventArgs e)
        {
            // TODO: upgrade with making global HUD instead of per-player HUD for stopwatch
            if(this.isStarted)
            {
                foreach (Player p in players)
                {
                    playersLiveInfoHUD[p].SetText("stopwatch", (DateTime.Now - startedTime).ToString(@"hh\:mm\:ss\.fff"));
                }
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            foreach (Player p in players)
            {
                if(!p.IsDisposed)
                {
                    p.GameText(countdown.ToString(), 1000, 6);
                    p.PlaySound((countdown > 0) ? 1056 : 1057);
                }
                else
				{
                    countdownTimer.IsRepeating = false;
                    countdownTimer.IsRunning = false;
                    countdownTimer.Dispose();
                    Eject(p);
                }
            }
            if (countdown == 0)
            {
                countdownTimer.IsRepeating = false;
                countdownTimer.IsRunning = false;
                countdownTimer.Dispose();
                Start();
            }
            if (countdown == 1)
            {
                foreach (Player p in players)
                {
                    if (!p.InAnyVehicle)
                    {
                        p.PutInVehicle(playersVehicles[p]);
                    }
                    if (p.InAnyVehicle)
                    {
                        p.Vehicle.Position = playersData[p].startPosition.Position + Vector3.UnitZ;
                        p.Vehicle.Angle = playersData[p].startPosition.Rotation;
                    }
                }
            }
            countdown--;
        }

        public void Start()
        {
            if(isPreparing && countdown == 0)
            {
                startedTime = DateTime.Now;
                this.isPreparing = false;
                this.isStarted = true;
                foreach (Player p in players)
                {
                    if(p.Vehicle != null)
                        p.Vehicle.Engine = true;
                    p.PlayedRaces++;
                }
            }
        }

        public void OnPlayerEnterCheckpoint(Player player)
        {
            if(player.InAnyVehicle)
            {
                if(playersData[player].nextCheckpoint == this.checkpoints[this.checkpoints.Count - 1])
                {
                    OnPlayerFinished(player, "Finished");
                }
                else
                {
                    string displayedCPTime;
                    TimeSpan cpTime = (DateTime.Now - startedTime);
                    if (cpTime.Hours > 0)
                        displayedCPTime = cpTime.ToString(@"hh\:mm\:ss\.fff");
                    else
                        displayedCPTime = cpTime.ToString(@"mm\:ss\.fff");

                    checkpointLiveInfos[playersData[player].nextCheckpoint.Idx].Add(player, cpTime);

                    playersLiveInfoHUD[player].SetText("checkpointtime", displayedCPTime);
                    playersLiveInfoHUD[player].Show("checkpointtime");
                    Player playerInFront;
                    TimeSpan gap = TimeSpan.MaxValue;
                    foreach (KeyValuePair<Player, PlayerCheckpointData> kvp in playerLastCheckpointData)
					{
                        if(kvp.Value.Checkpoint == playersData[player].nextCheckpoint)
                        {
                            if (gap.CompareTo((cpTime.Subtract(kvp.Value.Time))) > 0)
                            {
                                gap = cpTime - kvp.Value.Time;
                                playerInFront = kvp.Key;
                            }
                        }
					}
                    if(gap < TimeSpan.MaxValue)
                    {
                        playersLiveInfoHUD[player].SetText("checkpointdelta", "~R~+ " + gap.ToString(@"mm\:ss\.fff"));
                        playersLiveInfoHUD[player].Show("checkpointdelta");
                    }
                    SampSharp.GameMode.SAMP.Timer.RunOnce(5000, () =>
                    {
                        if(!player.IsDisposed) // Can be disconnected during race
                        {
                            playersLiveInfoHUD[player].Hide("checkpointtime");
                            playersLiveInfoHUD[player].Hide("checkpointdelta");
                        }
                    });

                    int cpidx = playersData[player].nextCheckpoint.Idx;
                    player.Notificate("CP: " + cpidx + "/" + (this.checkpoints.Count - 1).ToString());
                    playersData[player].nextCheckpoint = this.checkpoints[cpidx+1];
                    UpdatePlayerCheckpoint(player);

                    foreach (Player p in this.players)
                        UpdatePlayerCPLiveDisplay(p, playersData[player].nextCheckpoint.Idx -1);

                    playerLastCheckpointData[player] = new PlayerCheckpointData(this.checkpoints[cpidx], cpTime, player.Vehicle.Model, player.Vehicle.Velocity, player.Vehicle.Angle);

                    this.checkpoints[cpidx].ExecuteEvents(player);
                    if (this.checkpoints[cpidx].NextNitro == Checkpoint.EnableDisableEvent.Enable)
                        playersLiveInfoHUD[player].Show("nitro");
                    if (this.checkpoints[cpidx].NextNitro == Checkpoint.EnableDisableEvent.Disable)
                        playersLiveInfoHUD[player].Hide("nitro");
                }
            }
        }

        private void UpdatePlayerCPLiveDisplay(Player player, int cpidx)
        {
            if (cpidx != playersData[player].nextCheckpoint.Idx - 1)
                return;

            playerCPLiveHUD[player].Show("localCheckpointsBox");
            playerCPLiveHUD[player].Show("localCheckpointsBoxLabel");
            for(int i = 1; i <= 5; i ++)
            {
                playerCPLiveHUD[player].Hide($"localCheckpoint{i}");
                playerCPLiveHUD[player].Hide($"localCheckpoint{i}Label");
            }
            int idx = 1;
            string displayedCPPos;
            string displayedCPTime;
            bool isPlayerDisplayed = false; // Used to force display of player even if he's not in the 5 first players
            foreach (KeyValuePair<Player, CheckpointLiveInfo.Rank> kvp in checkpointLiveInfos[cpidx].Ranking) // Ranking is already ordered
            {
                if (kvp.Key == player)
                    isPlayerDisplayed = true;

                switch(kvp.Value.Pos)
                {
                    case 1:
                        displayedCPPos = "1st";
                        break;
                    case 2:
                        displayedCPPos = "2nd";
                        break;
                    case 3:
                        displayedCPPos = "3rd";
                        break;
                    default:
                        displayedCPPos = $"{kvp.Value.Pos}th";
                        break;
                }

                if (kvp.Value.Time.Hours > 0)
                    displayedCPTime = kvp.Value.Time.ToString(@"hh\:mm\:ss\.fff");
                else
                    displayedCPTime = kvp.Value.Time.ToString(@"mm\:ss\.fff");

                if (idx == 5 && !isPlayerDisplayed)
                {
                    CheckpointLiveInfo.Rank rank = checkpointLiveInfos[cpidx].GetRankForPlayer(player);
                    displayedCPPos = "5th";
                    if (rank.Time.Hours > 0)
                        displayedCPTime = rank.Time.ToString(@"hh\:mm\:ss\.fff");
                    else
                        displayedCPTime = rank.Time.ToString(@"mm\:ss\.fff");
                }
                if (idx == 1 || idx < 5) // Always display first player
                {
                    playerCPLiveHUD[player].SetText($"localCheckpoint{idx}Label", $"{displayedCPPos} {kvp.Key.Name} - {displayedCPTime}");
                    playerCPLiveHUD[player].Show($"localCheckpoint{idx}");
                    playerCPLiveHUD[player].Show($"localCheckpoint{idx}Label");
                }

                idx++;
            }
        }

        private void UpdatePlayerCheckpoint(Player player)
        {
            player.DisableRaceCheckpoint();
            Checkpoint cp = playersData[player].nextCheckpoint;
            if (cp == this.checkpoints[this.checkpoints.Count - 1]) // If it's the last checkpoint
            {
                player.SetRaceCheckpoint(cp.Type + 1, cp.Position, Vector3.Zero, cp.Size);
                foreach (Player p in spectatingPlayers)
                {
                    if (players[playersData[p].spectatePlayerIndex] == player)
                        p.SetRaceCheckpoint(cp.Type + 1, cp.Position, Vector3.Zero, cp.Size);
                }
            }
            else
            {
                try
                {
                    Checkpoint nextcp = this.checkpoints[cp.Idx + 1];
                    player.SetRaceCheckpoint(cp.Type, cp.Position, nextcp.Position, cp.Size);
                    foreach(Player p in spectatingPlayers)
                    {
                        if(players[playersData[p].spectatePlayerIndex] == player)
                            p.SetRaceCheckpoint(cp.Type, cp.Position, nextcp.Position, cp.Size);
                    }
                }
                catch (KeyNotFoundException e)
                {
                    player.SetRaceCheckpoint(cp.Type, cp.Position, Vector3.Zero, cp.Size);
                    Console.WriteLine("Race.cs - UpdatePlayerCheckpoint:E: Unable to display next checkpoint: " + e.Message);
                }
            }
        }

        private void Checkpoint_PlayerVehicleChanged(object sender, SampSharp.GameMode.Events.PlayerEventArgs e)
        {
            foreach(Player p in spectatingPlayers)
			{
                if(players[playersData[p].spectatePlayerIndex].Equals(e.Player))
                {
                    if (e.Player.InAnyVehicle)
                        p.SpectateVehicle(e.Player.Vehicle);
                    else
                        p.SpectatePlayer(e.Player);
                }
			}
        }

        public void RespawnPlayerOnLastCheckpoint(Player player, bool safeRespawn)
        {
            if (player != null)
            {
                if (players.Contains(player) && this.isStarted && playersData[player].nextCheckpoint.Idx < this.checkpoints.Count && playersData[player].nextCheckpoint.Idx > 0)
                {
                    Vector3 pos = Vector3.Zero;
                    float angle = 0;
                    VehicleModelType model = VehicleModelType.Admiral;
                    Vector3 velocity = Vector3.Zero;
                    try
                    {
                        model = playerLastCheckpointData[player].VehicleModel;
                        pos = playerLastCheckpointData[player].Checkpoint.Position + new Vector3(0, 0, BaseVehicle.GetModelInfo(model, VehicleModelInfoType.Size).Z / 2);
                        angle = playerLastCheckpointData[player].VehicleAngle;
                        velocity = playerLastCheckpointData[player].VehicleVelocity;
                    }
                    catch(KeyNotFoundException)
                    {
                        pos = playersData[player].startPosition.Position;
                        angle = playersData[player].startPosition.Rotation;
                        model = this.StartingVehicle.Value;
                    }
                    finally
                    {
                        if (!player.InAnyVehicle)
                        {
                            BaseVehicle veh = BaseVehicle.Create(
                                model,
                                pos,
                                angle,
                                1, 1
                            );
                            veh.VirtualWorld = this.virtualWorld;
                            veh.Engine = true;
                            if (!safeRespawn) veh.Velocity = velocity;
                            veh.Died += OnPlayerVehicleDied;
                            player.PutInVehicle(veh);
                        }
                        else
                        {
                            BaseVehicle veh = player.Vehicle;
                            veh.Position = pos;
                            veh.Angle = angle;
                            if (!safeRespawn) veh.Velocity = velocity;
                        }
                    }
                }
                else
                    Console.WriteLine("Race.cs - RespawnPlayerOnLastCheckpoint:E: player is not in race");
            }
            else
                Console.WriteLine("Race.cs - RespawnPlayerOnLastCheckpoint:E: player is null");
        }

        public void OnPlayerFinished(Player player, string reason)
        {
            if(reason.Equals("Finished"))
            {
                TimeSpan duration = DateTime.Now - startedTime;
                playersTimeSpan[player] = duration;
                int place = playersTimeSpan.Count;
                string coloredPlaceStr = "";
                string placeStr = "";
                int money = 0;
                switch (place)
                {
                    case 1:
                        coloredPlaceStr = $"{Color.Gold}1st{Color.White}";
                        placeStr = "1st";
                        money = 1000;
                        winner = player;
                        break;
                    case 2:
                        coloredPlaceStr = $"{Color.Silver}2nd{Color.White}";
                        placeStr = "2nd";
                        money = 750;
                        break;
                    case 3:
                        coloredPlaceStr = $"{Color.OrangeRed}3rd{Color.White}";
                        placeStr = "3rd";
                        money = 500;
                        break;
                    default:
                        money = 0;
                        coloredPlaceStr = place + "th";
                        break;
                }
                player.GiveMoney(money);
                Event.SendEventMessageToAll(player.pEvent, $"{Color.Orange}{player.Name}{Color.White} finished the race at {coloredPlaceStr} place");
                Logger.WriteLineAndClose($"Race.cs - OnPlayerFinished:I: {player.Name} finished the race {this.Name} at {placeStr} place");

                string finishText = placeStr + " place !~n~" + duration.ToString(@"hh\:mm\:ss\.fff");
                bool isNewRecord = false;
                if (playersData[player].record == TimeSpan.Zero) // No record for this race
                {
                    finishText += "~n~First record ";
                    isNewRecord = true;
                }
                if (playersData[player].record.CompareTo(duration) > 0) // Better than previous record
                {
                    finishText += "~n~~g~New record  ! -" + playersData[player].record.Subtract(duration).ToString(@"hh\:mm\:ss\.fff");
                    player.GiveMoney(200);
                    isNewRecord = true;
                }

                player.GameText(finishText, 5000, 4);

                if (isNewRecord)
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("@race_id", this.Id);
                    param.Add("@player_id", player.DbId);
                    GameMode.mySQLConnector.Execute("DELETE FROM race_records WHERE race_id = @race_id AND player_id = @player_id", param);
                    param = new Dictionary<string, object>();
                    param.Add("@race_id", this.Id);
                    param.Add("@player_id", player.DbId);
                    param.Add("@record_duration", duration.ToString(@"hh\:mm\:ss\.ffffff"));
                    GameMode.mySQLConnector.Execute("INSERT INTO race_records (race_id, player_id, record_duration) VALUES (@race_id, @player_id, @record_duration)", param);
                }
            }
            else if(reason.Equals("Leave"))
            {
                Event.SendEventMessageToPlayer(player, "You left the race");
                Logger.WriteLineAndClose($"Race.cs - OnPlayerFinished:I: {player.Name} left the race {this.Name}");
                Event.SendEventMessageToAll(player.pEvent, $"{Color.Orange}{player.Name}{Color.White} has left the race");
                player.GameText("GAME OVER", 5000, 4);
            }
            else if(reason.Equals("Disconnected"))
            {
                Logger.WriteLineAndClose($"Race.cs - OnPlayerFinished:I: {player.Name} left the race {this.Name}");
                Event.SendEventMessageToAll(player.pEvent, $"{Color.Orange}{player.Name}{Color.White} has left the race");
                Eject(player);
            }
            else
            {
                Event.SendEventMessageToPlayer(player, "You lost (reason: " + reason + ")");
                Logger.WriteLineAndClose($"Race.cs - OnPlayerFinished:I: {player.Name} has been ejected from the race {this.Name} (reason: {reason})");
                Event.SendEventMessageToAll(player.pEvent, $"{Color.Orange}{player.Name}{Color.White} has lost the race (reason: {reason})");
                player.GameText("GAME OVER", 5000, 4);
            }

            players.Remove(player);
            if(players.Count == 0) // Si on arrive dernier / si le dernier arrive
            {
                SampSharp.GameMode.SAMP.Timer ejectionTimer = new SampSharp.GameMode.SAMP.Timer(2000, false);
                ejectionTimer.Tick += (object sender, EventArgs e) =>
                {
                    RaceFinishedEventArgs args = new RaceFinishedEventArgs();
                    args.race = this;
                    args.winner = winner;
                    OnFinished(args);
                };
            }
            else
            {
                if(!reason.Equals("Disconnected"))
                {
                    if (player.InAnyVehicle)
                    {
                        BaseVehicle vehicle = player.Vehicle;
                        player.RemoveFromVehicle();
                        if (!(vehicle is null) && !vehicle.IsDisposed) vehicle.Dispose();
                    }
                    player.ToggleSpectating(true);
                    if (players[0].InAnyVehicle)
                        player.SpectateVehicle(players[0].Vehicle);
                    else
                        player.SpectatePlayer(players[0]);
                    spectatingPlayers.Add(player);
                    playersData[player].status = RacePlayerStatus.Spectating;
                    playersData[player].spectatePlayerIndex = 0;
                }
            }
        }

        public void Eject(Player player)
        {
            playersLiveInfoHUD[player].Hide();
            playersRecordsHUD[player].Hide();
            playerCPLiveHUD[player].Hide();
            players.RemoveAll(x => x.Equals(player));
            spectatingPlayers.RemoveAll(x => x.Equals(player));

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
                player.EnterCheckpoint -= checkpointEventHandler;
                player.EnterRaceCheckpoint -= checkpointEventHandler;
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
            Logger.WriteLineAndClose($"Race.cs - Race.Unload:I: Race {this.Name} unloaded");
        }
        public void ReloadMap()
        {
            if (this.map != null)
                this.map.Unload();

            if (this.MapId > -1)
            {
                this.map = new Map.Map();
                this.map.Loaded += (sender, eventArgs) =>
                {

                };
                this.map.Load(this.MapId, virtualWorld);
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


        public static List<string> GetPlayerRaceList(Player player)
        {
            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@name", player.Name }
                };
            mySQLConnector.OpenReader("SELECT race_id, race_name FROM races WHERE race_creator = @name", param);
            List<string> result = new List<string>();
            Dictionary<string, string> row = mySQLConnector.GetNextRow();
            while (row.Count > 0)
            {
                result.Add(row["race_id"] + "_" + Display.ColorPalette.Primary.Main + row["race_name"]);
                row = mySQLConnector.GetNextRow();
            }
            mySQLConnector.CloseReader();

            return result;
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
            // id, name, creator, number of checkpoints, zone, number of spawnpoints
            Dictionary<string, string> results = new Dictionary<string, string>();
            Dictionary<string, string> row;

            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@id", id }
                };

            mySQLConnector.OpenReader("SELECT race_id, race_name, race_creator, map_name FROM races LEFT JOIN maps ON (race_map = map_id) WHERE race_id = @id", param);

            row = mySQLConnector.GetNextRow();
            foreach (KeyValuePair<string, string> kvp in row)
                results.Add(MySQLConnector.Field.GetFieldName(kvp.Key), kvp.Value);

            mySQLConnector.CloseReader();

            mySQLConnector.OpenReader("SELECT checkpoint_id, checkpoint_number, checkpoint_pos_x, checkpoint_pos_y, checkpoint_pos_z " +
                "FROM race_checkpoints WHERE race_id = @id", param);
            int nbrOfCheckpoints = 0;
            row = mySQLConnector.GetNextRow();
            Vector3 firstCheckpointPos = new Vector3();
            while (row.Count > 0)
            {
                nbrOfCheckpoints++;
                if (row["checkpoint_number"] == "0")
                {
                    firstCheckpointPos = new Vector3(
                        (float)Convert.ToDouble(row["checkpoint_pos_x"]),
                        (float)Convert.ToDouble(row["checkpoint_pos_y"]),
                        (float)Convert.ToDouble(row["checkpoint_pos_z"])
                    );
                }
                row = mySQLConnector.GetNextRow();
            }
            results.Add("Number of checkpoints", nbrOfCheckpoints.ToString());
            mySQLConnector.CloseReader();

            string zoneStr = Zone.GetMainZoneName(firstCheckpointPos);
            results.Add("Zone", zoneStr);

            mySQLConnector.OpenReader("SELECT COUNT(spawn_index) as nbr " +
                "FROM race_spawn WHERE race_id = @id", param);
            row = mySQLConnector.GetNextRow();
            if (row.Count == 0)
                results.Add("Number of spawn points", Color.Red + "No spawn point");
            else
                results.Add("Number of spawn points", row["nbr"]);
            mySQLConnector.CloseReader();
            return results;
        }
    }
}
