using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SampSharpGameMode1.Events.Races
{
    public class RaceLoadedEventArgs : EventArgs
    {
        public Race race { get; set; }
        public bool success { get; set; }
    }
    public class RaceEventArgs : EventArgs
    {
        public Race race { get; set; }
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
        public bool IsLoaded { get; private set; }

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
        private Dictionary<Player, HUD> playersRecordsHUD = new Dictionary<Player, HUD>();
        private Dictionary<Player, HUD> playersLiveInfoHUD = new Dictionary<Player, HUD>();
        private Dictionary<Player, TimeSpan> playersTimeSpan = new Dictionary<Player, TimeSpan>();
        private Dictionary<string, TimeSpan> records = new Dictionary<string, TimeSpan>();
        public Player winner;
        private int virtualWorld;
        private SampSharp.GameMode.SAMP.Timer countdownTimer;
        private int countdown;
        public DateTime startedTime;

        public struct PlayerCheckpointData
        {
            public PlayerCheckpointData(Checkpoint cp, VehicleModelType model, Vector3 velocity, float angle)
            {
                this.Checkpoint = cp;
                this.VehicleModel = model;
                this.VehicleVelocity = velocity;
                this.VehicleAngle = angle;
            }
            public Checkpoint Checkpoint { get; set; }
            public VehicleModelType VehicleModel { get; set; }
            public Vector3 VehicleVelocity { get; set; }
            public float VehicleAngle { get; set; }
        }
        public Dictionary<Player, PlayerCheckpointData> playerLastCheckpointData = new Dictionary<Player, PlayerCheckpointData>();

        // Launcher Events

        public event EventHandler<RaceEventArgs> Finished;

        protected virtual void OnFinished(RaceEventArgs e)
        {
            Finished?.Invoke(this, e);
            Player.SendClientMessageToAll("Race \"" + e.race.Name + "\" is finished, the winner is " + e.race.winner.Name + " !");
        }

        public void OnPlayerVehicleDied(object sender, SampSharp.GameMode.Events.PlayerEventArgs e)
        {
            OnPlayerFinished((Player)e.Player, "Vehicle destroyed");
        }

        public void Load(int id)
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
                        this.Laps = Convert.ToInt32(row["race_laps"]);
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
                            checkpoint = new Checkpoint(new Vector3(
                                    (float)Convert.ToDouble(row["checkpoint_pos_x"]),
                                    (float)Convert.ToDouble(row["checkpoint_pos_y"]),
                                    (float)Convert.ToDouble(row["checkpoint_pos_z"])
                                ), (CheckpointType)Convert.ToInt32(row["checkpoint_type"]), (float)Convert.ToDouble(row["checkpoint_size"]));

                            if (row["checkpoint_vehiclechange"].Equals("[null]"))
                                checkpoint.NextVehicle = null;
                            else
                                checkpoint.NextVehicle = (VehicleModelType)Convert.ToInt32(row["checkpoint_vehiclechange"]);

                            if (row["checkpoint_nitro"].Equals("[null]"))
                                checkpoint.NextNitro = Checkpoint.NitroEvent.None;
                            else
                                checkpoint.NextNitro = (Checkpoint.NitroEvent)Convert.ToInt32(row["checkpoint_nitro"]);

                            checkpoint.Idx = idx;
                            this.checkpoints.Add(idx++, checkpoint);
                            row = GameMode.mySQLConnector.GetNextRow();
                        }
                        GameMode.mySQLConnector.CloseReader();
                    }

                    if(!errorFlag)
                    {
                        GameMode.mySQLConnector.OpenReader("SELECT spawn_index, spawn_pos_x, spawn_pos_y, spawn_pos_z, spawn_rot " +
                            "FROM race_spawn " +
                            "WHERE race_id=@id ORDER BY spawn_index", param);
                        row = GameMode.mySQLConnector.GetNextRow();
                        if (row.Count == 0) errorFlag = true;

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
                                this.SpawnPoints.Add(pos);
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
                    args.race = this;
                    args.success = !errorFlag;
                    OnLoaded(args);
                });
                t.Start();
            }
        }

        public Boolean IsPlayable()
        {
            return (checkpoints.Count > 0 && StartingVehicle != null) ? true : false;
        }

        public void Prepare(List<Player> players, int virtualWorld)
        {
            if(IsPlayable())
            {
                bool isAborted = false;
                this.players = players;
                this.spectatingPlayers = new List<Player>();
                this.virtualWorld = virtualWorld;

                Random rdm = new Random();
                List<int> generatedPos = new List<int>();
                int pos;
                int tries = 0;

                Dictionary<string, string> row;
                foreach (Player p in players)
                {
                    RacePlayer playerData = new RacePlayer();
                    playerData.spectatePlayerIndex = -1;
                    playerData.status = RacePlayerStatus.Running;
                    playerData.nextCheckpoint = checkpoints[1];

                    // Get player record
                    Dictionary<string, object> param = new Dictionary<string, object>
                    {
                        { "@race_id", this.Id },
                        { "@player_id", p.Db_Id}
                    };
                    GameMode.mySQLConnector.OpenReader("SELECT record_duration " +
                        "FROM race_records WHERE race_id=@race_id AND player_id=@player_id", param);
                    row = GameMode.mySQLConnector.GetNextRow();

                    if (row.Count > 0)
                        playerData.record = TimeSpan.Parse(row["record_duration"]);
                    else
                        playerData.record = TimeSpan.Zero;

                    GameMode.mySQLConnector.CloseReader();

                    playersData.Add(p, playerData);

                    playersRecordsHUD[p] = new HUD(p, "racerecords.json");
                    int recordIdx = 1;
                    foreach(KeyValuePair<string, TimeSpan> record in records)
                        playersRecordsHUD[p].SetText("localRecord" + (recordIdx++) + "Label", "~W~" + record.Key + "~G~" + record.Value.ToString(@"hh\:mm\:ss\.fff"));

                    p.VirtualWorld = virtualWorld;

                    p.EnterCheckpoint += (sender, eventArgs) => { OnPlayerEnterCheckpoint((Player)sender); };
                    p.EnterRaceCheckpoint += (sender, eventArgs) => { OnPlayerEnterCheckpoint((Player)sender); };
                    p.KeyStateChanged += OnPlayerKeyStateChanged;

                    pos = rdm.Next(1, players.Count);
                    while (generatedPos.Contains(pos) && tries++ < MAX_PLAYERS_IN_RACE && pos >= this.SpawnPoints.Count)
                        pos = rdm.Next(1, players.Count);

                    if (tries >= MAX_PLAYERS_IN_RACE)
                    {
                        Player.SendClientMessageToAll("Error during position randomization for the race. Race aborted");
                        isAborted = true;
                        break;
                    }
                    tries = 0;

                    BaseVehicle veh = BaseVehicle.Create(StartingVehicle.GetValueOrDefault(VehicleModelType.Bike), this.SpawnPoints[pos].Position, this.SpawnPoints[pos].Rotation, 1, 1);
                    veh.VirtualWorld = virtualWorld;
                    veh.Engine = false;
                    veh.Doors = true;
                    veh.Died += OnPlayerVehicleDied;
                    p.PutInVehicle(veh);

                    UpdatePlayerCheckpoint(p);
                }

                if (!isAborted)
                {
                    countdown = 3;
                    countdownTimer = new SampSharp.GameMode.SAMP.Timer(1000, true);
                    countdownTimer.Tick += CountdownTimer_Tick;
                    isPreparing = true;
                }
                else
                {
                    //TODO: remettre les joueurs dans leurs vw et positions initiales
                }
            }
        }

        private void OnPlayerKeyStateChanged(object sender, SampSharp.GameMode.Events.KeyStateChangedEventArgs e)
        {
            Player spectator = (Player)sender;
            if (spectatingPlayers.Contains(spectator))
            {
                switch (e.NewKeys)
                {
                    case Keys.Fire:
                        playersData[(Player)sender].spectatePlayerIndex++;
                        if (playersData[(Player)sender].spectatePlayerIndex >= players.Count)
                        {
                            playersData[(Player)sender].spectatePlayerIndex = 0;
                        }
                        spectator.SpectatePlayer(players[playersData[(Player)sender].spectatePlayerIndex]);
                        spectator.Notificate(players[playersData[(Player)sender].spectatePlayerIndex].Name);
                        break;
                    case Keys.Aim:
                        playersData[(Player)sender].spectatePlayerIndex--;
                        if (playersData[(Player)sender].spectatePlayerIndex < 0)
                        {
                            playersData[(Player)sender].spectatePlayerIndex = players.Count;
                        }
                        spectator.SpectatePlayer(players[playersData[(Player)sender].spectatePlayerIndex]);
                        spectator.Notificate(players[playersData[(Player)sender].spectatePlayerIndex].Name);
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

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdown--;
            foreach (Player p in players)
            {
                p.GameText(countdown.ToString(), 1000, 6);
                p.PlaySound((countdown > 0) ? 1056 : 1057);
            }
            if(countdown == 0)
            {
                countdownTimer.IsRepeating = false;
                countdownTimer.IsRunning = false;
                countdownTimer.Dispose();
                Start();
            }
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
                    p.Vehicle.Engine = true;
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
                    int cpidx = playersData[player].nextCheckpoint.Idx;
                    Console.WriteLine("Race.cs - OnPlayerEnterCheckpoint:I: playerCheckpoint[" + player.Name + "].Idx = " + playersData[player].nextCheckpoint.Idx);
                    player.Notificate("CP: " + cpidx + "/" + (this.checkpoints.Count - 1).ToString());
                    playersData[player].nextCheckpoint = this.checkpoints[cpidx+1];
                    UpdatePlayerCheckpoint(player);

                    playerLastCheckpointData[player] = new PlayerCheckpointData(this.checkpoints[cpidx], player.Vehicle.Model, player.Vehicle.Velocity, player.Vehicle.Angle);

                    this.checkpoints[cpidx].ExecuteEvents(player);
                }
            }
        }

        private void UpdatePlayerCheckpoint(Player player)
        {
            player.DisableRaceCheckpoint();
            Checkpoint cp = playersData[player].nextCheckpoint;
            if (cp == this.checkpoints[this.checkpoints.Count - 1]) // If it's the last checkpoint
            {
                player.SetCheckpoint(cp.Position, cp.Size);
            }
            else
            {
                try
                {
                    Checkpoint nextcp = this.checkpoints[cp.Idx + 1];
                    player.SetRaceCheckpoint(cp.Type, cp.Position, nextcp.Position, cp.Size);
                }
                catch (KeyNotFoundException e)
                {
                    player.SetRaceCheckpoint(cp.Type, cp.Position, Vector3.Zero, cp.Size);
                    Console.WriteLine("Race.cs - UpdatePlayerCheckpoint:E: Unable to display next checkpoint: " + e.Message);
                }
            }
        }

        public void RespawnPlayerOnLastCheckpoint(Player player, bool safeRespawn)
        {
            if (player != null)
            {
                if (players.Contains(player) && this.isStarted && playersData[player].nextCheckpoint.Idx < this.checkpoints.Count && playersData[player].nextCheckpoint.Idx > 0)
                {
                    if (!player.InAnyVehicle)
                    {
                        BaseVehicle veh = BaseVehicle.Create(
                            playerLastCheckpointData[player].VehicleModel,
                            playerLastCheckpointData[player].Checkpoint.Position,
                            playerLastCheckpointData[player].VehicleAngle,
                            1, 1
                        );
                        veh.VirtualWorld = this.virtualWorld;
                        veh.Engine = true;
                        if (!safeRespawn) veh.Velocity = playerLastCheckpointData[player].VehicleVelocity;
                        veh.Doors = true;
                        veh.Died += OnPlayerVehicleDied;
                        player.PutInVehicle(veh);
                    }
                    else
                    {
                        BaseVehicle veh = player.Vehicle;
                        veh.Position = playerLastCheckpointData[player].Checkpoint.Position;
                        veh.Angle = playerLastCheckpointData[player].VehicleAngle;
                        if (!safeRespawn) veh.Velocity = playerLastCheckpointData[player].VehicleVelocity;
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
                if(playersTimeSpan.Count == 0)
                {
                    winner = player;
                }

                TimeSpan duration = DateTime.Now - startedTime;
                playersTimeSpan[player] = duration;
                int place = playersTimeSpan.Count;
                string placeStr = "";
                switch (place)
                {
                    case 1:
                        placeStr = "1st";
                        player.GiveMoney(1000);
                        player.PlaySound(5448);
                        break;
                    case 2:
                        placeStr = "2nd";
                        player.GiveMoney(750);
                        break;
                    case 3:
                        placeStr = "3rd";
                        player.GiveMoney(500);
                        break;
                    default:
                        placeStr = place + "th";
                        break;
                }

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
                    param.Add("@player_id", player.Db_Id);
                    GameMode.mySQLConnector.Execute("DELETE FROM race_records WHERE race_id = @race_id AND player_id = @player_id", param);
                    param = new Dictionary<string, object>();
                    param.Add("@race_id", this.Id);
                    param.Add("@player_id", player.Db_Id);
                    param.Add("@record_duration", duration.ToString(@"hh\:mm\:ss\.ffffff"));
                    GameMode.mySQLConnector.Execute("INSERT INTO race_records (race_id, player_id, record_duration) VALUES (@race_id, @player_id, @record_duration)", param);
                }
            }

            if(player.InAnyVehicle)
            {
                BaseVehicle vehicle = player.Vehicle;
                player.RemoveFromVehicle();
                vehicle.Dispose();
            }

            player.DisableCheckpoint();
            player.DisableRaceCheckpoint();

            players.Remove(player);
            if(players.Count == 0) // Si on arrive dernier / si le dernier arrive
            {
                Eject(player);
                foreach(Player p in spectatingPlayers)
                {
                    Eject(p);
                }
                RaceEventArgs args = new RaceEventArgs();
                args.race = this;
                OnFinished(args);
            }
            else
            {
                player.ToggleSpectating(true);
                if(players[0].InAnyVehicle)
                    player.SpectateVehicle(players[0].Vehicle);
                else
                    player.SpectatePlayer(players[0]);
                spectatingPlayers.Add(player);
                playersData[player].status = RacePlayerStatus.Spectating;
                playersData[player].spectatePlayerIndex = 0;
            }
        }

        public void Eject(Player player)
        {
            playersRecordsHUD[player].Hide();
            player.ToggleSpectating(false);
            player.VirtualWorld = 0;
            player.Spawn();
        }
    }
}
