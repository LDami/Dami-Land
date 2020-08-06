using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SampSharpGameMode1.Events.Races
{
    public class RaceLoadedEventArgs : EventArgs
    {
        public Race race { get; set; }
    }
    public class RaceEventArgs : EventArgs
    {
        public Race race { get; set; }
    }

    public class Race
    {
        public const int MIN_PLAYERS_IN_RACE = 1;
        public const int MAX_PLAYERS_IN_RACE = 100;

        private int id;
        private string name;
        private int laps;
        public Dictionary<int, Checkpoint> checkpoints = new Dictionary<int, Checkpoint>();
        public Vector3R[] startingSpawn;
        private VehicleModelType? startingVehicle;


        public int Id { get => id; private set => id = value; }
        public string Name { get => name; set => name = value; }
        public int Laps { get => laps; set => laps = value; }
        public VehicleModelType? StartingVehicle { get => startingVehicle; set => startingVehicle = value; }

        // Common Events

        public event EventHandler<RaceLoadedEventArgs> Loaded;

        protected virtual void OnLoaded(RaceLoadedEventArgs e)
        {
            EventHandler<RaceLoadedEventArgs> handler = Loaded;
            if (handler != null)
                handler(this, e);
        }

        // Launcher only

        private bool isStarted = false;
        private bool isPreparing = false;
        public List<Player> players;
        public List<Player> spectatingPlayers;
        public Dictionary<Player, int> spectatingPlayersIndex = new Dictionary<Player, int>();
        private Dictionary<Player, int> playersStartingPos = new Dictionary<Player, int>();
        private Dictionary<Player, int> playerCheckpoint = new Dictionary<Player, int>(); // Showed checkpoint (destination for player)
        private Dictionary<Player, TimeSpan> playerTimeSpan = new Dictionary<Player, TimeSpan>();
        private Dictionary<Player, TimeSpan> personalRecords = new Dictionary<Player, TimeSpan>();
        private Dictionary<string, TimeSpan> records = new Dictionary<string, TimeSpan>();
        public Player winner;
        private int virtualWorld;
        private SampSharp.GameMode.SAMP.Timer countdownTimer;
        private int countdown;
        public DateTime startedTime;
        public bool IsStarted { get => isStarted; set => isStarted = value; }

        // Launcher Events

        public event EventHandler<RaceEventArgs> Finished;

        protected virtual void OnFinished(RaceEventArgs e)
        {
            Finished?.Invoke(this, e);
            Player.SendClientMessageToAll("Race \"" + e.race.Name + "\" is finished, the winner is " + e.race.winner.Name + " !");
        }

        public void Load(int id)
        {
            if (GameMode.mySQLConnector != null)
            {
                Thread t = new Thread(() =>
                {
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
                    GameMode.mySQLConnector.CloseReader();

                    GameMode.mySQLConnector.OpenReader("SELECT checkpoint_number, checkpoint_pos_x, checkpoint_pos_y, checkpoint_pos_z, checkpoint_size, checkpoint_type " +
                        "FROM race_checkpoints " +
                        "WHERE race_id=@id ORDER BY checkpoint_number", param);
                    row = GameMode.mySQLConnector.GetNextRow();

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
                        this.checkpoints.Add(idx++, checkpoint);
                        row = GameMode.mySQLConnector.GetNextRow();
                    }
                    GameMode.mySQLConnector.CloseReader();

                    GameMode.mySQLConnector.OpenReader("SELECT spawn_index, spawnpos_x, spawnpos_y, spawnpos_z, spawnpos_rot " +
                        "FROM race_spawnpos " +
                        "WHERE race_id=@id ORDER BY spawn_index", param);
                    row = GameMode.mySQLConnector.GetNextRow();

                    this.startingSpawn = new Vector3R[MAX_PLAYERS_IN_RACE];
                    Vector3R pos;
                    idx = 0;
                    while (row.Count > 0)
                    {
                        pos = new Vector3R(new Vector3(
                                    (float)Convert.ToDouble(row["spawnpos_x"]),
                                    (float)Convert.ToDouble(row["spawnpos_y"]),
                                    (float)Convert.ToDouble(row["spawnpos_z"])
                                ),
                                (float)Convert.ToDouble(row["spawnpos_rot"])
                            );
                        this.startingSpawn[idx++] = pos;
                        row = GameMode.mySQLConnector.GetNextRow();
                    }
                    for (int i = 0; i < this.startingSpawn.Length; i++)
                    {
                        if (this.startingSpawn[i].Position == Vector3.Zero)
                        {
                            this.startingSpawn[i].Position = this.checkpoints[0].Position;
                        }
                    }
                    GameMode.mySQLConnector.CloseReader();

                    GameMode.mySQLConnector.OpenReader("SELECT player_id, record_duration, name " +
                        "FROM race_records INNER JOIN users ON race_records.player_id = users.id " +
                        "WHERE race_id=@id ORDER BY record_duration", param);
                    row = GameMode.mySQLConnector.GetNextRow();

                    while (row.Count > 0)
                    {
                        Player p = Player.GetPlayerByDatabaseId(Convert.ToInt32(row["player_id"]));
                        if (p != null)
                            personalRecords[p] = TimeSpan.Parse(row["record_duration"]);
                        records[row["name"]] = TimeSpan.Parse(row["record_duration"]);
                        row = GameMode.mySQLConnector.GetNextRow();
                    }
                    GameMode.mySQLConnector.CloseReader();

                    RaceLoadedEventArgs args = new RaceLoadedEventArgs();
                    args.race = this;
                    OnLoaded(args);
                });
                t.Start();
            }
        }

        public Boolean IsPlayable()
        {
            return (checkpoints.Count > 0 && startingVehicle != null) ? true : false;
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

                foreach (Player p in players)
                {
                    spectatingPlayersIndex[p] = -1;
                    p.VirtualWorld = virtualWorld;

                    pos = rdm.Next(1, players.Count);
                    while (generatedPos.Contains(pos) && tries++ < MAX_PLAYERS_IN_RACE)
                        pos = rdm.Next(1, players.Count);

                    if (tries >= MAX_PLAYERS_IN_RACE)
                    {
                        Player.SendClientMessageToAll("Error during position randomization for the race. Race aborted");
                        isAborted = true;
                        break;
                    }
                    tries = 0;

                    playersStartingPos[p] = pos;

                    BaseVehicle veh = BaseVehicle.Create(startingVehicle.GetValueOrDefault(VehicleModelType.Bike), this.startingSpawn[pos].Position, this.startingSpawn[pos].Rotation, 1, 1);
                    veh.VirtualWorld = virtualWorld;
                    veh.Engine = false;
                    veh.Doors = true;
                    veh.Died += OnPlayerVehicle_Died;
                    p.PutInVehicle(veh);

                    playerCheckpoint[p] = 1;
                    UpdatePlayerCheckpoint(p);
                    p.playerRace = this;
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

        private void OnPlayerVehicle_Died(object sender, SampSharp.GameMode.Events.PlayerEventArgs e)
        {
            OnPlayerFinished((Player)e.Player, "Vehicle destroyed");
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdown--;
            foreach (Player p in players)
                p.GameText(countdown.ToString(), 1000, 6);
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
                foreach (Player p in players)
                {
                    p.Vehicle.Engine = true;
                }
            }
        }

        public void OnPlayerEnterCheckpoint(Player player)
        {
            if(playerCheckpoint[player] != -1)
            {
                if(playerCheckpoint[player] == this.checkpoints.Count)
                {
                    OnPlayerFinished(player, "Finished");
                }
                else
                {
                    player.Notificate("CP: " + (playerCheckpoint[player] - 1).ToString() + "/" + (this.checkpoints.Count - 1).ToString());
                    playerCheckpoint[player]++;
                    UpdatePlayerCheckpoint(player);
                }
            }
        }

        private void UpdatePlayerCheckpoint(Player player)
        {
            player.DisableRaceCheckpoint();
            if (playerCheckpoint[player] < this.checkpoints.Count)
            {
                Checkpoint cp = this.checkpoints[playerCheckpoint[player] - 1];
                try
                {
                    Checkpoint nextcp = this.checkpoints[playerCheckpoint[player]];
                    player.SetRaceCheckpoint(cp.Type, cp.Position, nextcp.Position, cp.Size);
                    if(nextcp.NextVehicle != null)
                    {
                        BaseVehicle newVeh = BaseVehicle.Create((VehicleModelType)nextcp.NextVehicle, player.Vehicle.Position, 0.0f, 1, 1);
                        newVeh.VirtualWorld = virtualWorld;
                        newVeh.Rotation = player.Vehicle.Rotation;
                        newVeh.Velocity = player.Vehicle.Velocity;
                        newVeh.Engine = false;
                        newVeh.Doors = true;
                        newVeh.Died += OnPlayerVehicle_Died;
                        BaseVehicle lastVeh = player.Vehicle;
                        player.PutInVehicle(newVeh);
                        lastVeh.Dispose();
                    }
                }
                catch (KeyNotFoundException e)
                {
                    player.SetRaceCheckpoint(cp.Type, cp.Position, Vector3.Zero, cp.Size);
                }
            }
        }

        public void OnPlayerFinished(Player player, string reason)
        {
            if(reason.Equals("Finished"))
            {
                TimeSpan duration = DateTime.Now - startedTime;
                playerTimeSpan[player] = duration;
                int place = playerTimeSpan.Count;
                string placeStr = "";
                switch (place)
                {
                    case 1:
                        placeStr = "1st";
                        break;
                    case 2:
                        placeStr = "2nd";
                        break;
                    case 3:
                        placeStr = "3rd";
                        break;
                    default:
                        placeStr = place + "th";
                        break;
                }
                bool isNewRecord = false;
                if (personalRecords.ContainsKey(player)) // Is player has already a personal record on this race ?
                {
                    if (personalRecords[player].CompareTo(duration) > 0)
                    {
                        isNewRecord = true;
                    }
                }
                else
                {
                    isNewRecord = true;
                }
                player.GameText(placeStr + " place !~n~" + duration.ToString(@"hh\:mm\:ss\.fff") + ((isNewRecord) ? "~n~~r~New record  ! -" + @personalRecords[player].Subtract(duration).ToString(@"hh\:mm\:ss\.fff") : ""), 5000, 4);

                if (isNewRecord)
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("@race_id", this.Id);
                    param.Add("@player_id", player.Db_Id);
                    GameMode.mySQLConnector.Execute("DELETE FROM race_records WHERE race_id = @race_id AND player_id = @player_id", param);
                    param = new Dictionary<string, object>();
                    param.Add("@race_id", this.Id);
                    param.Add("@player_id", player.Db_Id);
                    param.Add("@record_duration", duration.ToString(@"hh\:mm\:ss\.fff"));
                    GameMode.mySQLConnector.Execute("INSERT INTO race_records (race_id, player_id, record_duration) VALUES (@race_id, @player_id, TIME_FORMAT(@record_duration, \"%H:\")", param);
                }
            }

            BaseVehicle vehicle = player.Vehicle;
            player.RemoveFromVehicle();
            vehicle.Dispose();

            players.Remove(player);
            if(players.Count == 0)
            {
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
                player.SpectatePlayer(players[0]);
                player.KeyStateChanged += Player_KeyStateChanged;
                spectatingPlayers.Add(player);
                spectatingPlayersIndex[player] = 0;
            }
        }

        private void Player_KeyStateChanged(object sender, SampSharp.GameMode.Events.KeyStateChangedEventArgs e)
        {
            Player spectator = (Player)sender;
            if(spectatingPlayers.Contains(spectator))
            {
                switch (e.NewKeys)
                {
                    case Keys.Fire:
                        spectatingPlayersIndex[spectator]++;
                        if (spectatingPlayersIndex[spectator] >= players.Count)
                        {
                            spectatingPlayersIndex[spectator] = 0;
                        }
                        spectator.SpectatePlayer(players[spectatingPlayersIndex[spectator]]);
                        spectator.Notificate(players[spectatingPlayersIndex[spectator]].Name);
                        break;
                    case Keys.Aim:
                        spectatingPlayersIndex[spectator]--;
                        if (spectatingPlayersIndex[spectator] < 0)
                        {
                            spectatingPlayersIndex[spectator] = players.Count;
                        }
                        spectator.SpectatePlayer(players[spectatingPlayersIndex[spectator]]);
                        spectator.Notificate(players[spectatingPlayersIndex[spectator]].Name);
                        break;
                }
            }
        }

        public void Eject(Player player)
        {
            player.ToggleSpectating(false);
            player.VirtualWorld = 0;
            player.Spawn();
        }
    }
}
