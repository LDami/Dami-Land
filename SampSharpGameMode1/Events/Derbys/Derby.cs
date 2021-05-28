using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SampSharpGameMode1.Events.Derbys
{
    public class DerbyEventArgs : EventArgs
    {
        public Derby derby { get; set; }
    }
    public class DerbyLoadedEventArgs : DerbyEventArgs
    {
        public bool success { get; set; }
    }
    public class Derby : EventSource
    {
        public const int MIN_PLAYERS_IN_DERBY = 0;
        public const int MAX_PLAYERS_IN_DERBY = 100;
        public int Id { get; set; }
        public string Name { get; set; }
        public VehicleModelType? StartingVehicle { get; set; }
        public List<Vector3R> SpawnPoints { get; set; }
        public List<DynamicObject> MapObjects { get; set; }
        public List<DerbyPickup> Pickups { get; set; }
        public bool IsLoaded { get; private set; }
        public bool IsCreatorMode { get; set; }


        private bool isPreparing = false;
        public bool isStarted = false;
        public List<Player> players;
        public Dictionary<Player, DerbyPlayer> playersData = new Dictionary<Player, DerbyPlayer>();
        public List<Player> spectatingPlayers; // Contains spectating players who looses the derby, and others players who spectate without joining
        private Dictionary<Player, HUD> playersLiveInfoHUD = new Dictionary<Player, HUD>();
        public Player winner;
        private int virtualWorld;
        private SampSharp.GameMode.SAMP.Timer countdownTimer;
        private int countdown;
        public DateTime startedTime;

        #region DerbyEvents
        public event EventHandler<DerbyLoadedEventArgs> Loaded;
        protected virtual void OnLoaded(DerbyLoadedEventArgs e)
        {
            Loaded?.Invoke(this, e);
            IsLoaded = e.success;
        }

        public event EventHandler<DerbyEventArgs> Finished;
        protected virtual void OnFinished(DerbyEventArgs e)
        {
            Finished?.Invoke(this, e);
            Player.SendClientMessageToAll("Derby \"" + e.derby.Name + "\" is finished, the winner is " + e.derby.winner.Name + " !");
        }
        #endregion

        #region PlayerEvents
        public void OnPlayerVehicleDied(object sender, PlayerEventArgs e)
        {
            OnPlayerFinished((Player)e.Player, "Vehicle destroyed");
        }

        private void OnPlayerKeyStateChanged(object sender, KeyStateChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion
        public void Load(int id)
        {
            if (GameMode.mySQLConnector != null)
            {
                Thread t = new Thread(() =>
                {
                    IsLoaded = false;
                    bool errorFlag = false;
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
                        if (Convert.ToInt32(row["derby_startvehicle"]) >= 400 && Convert.ToInt32(row["derby_startvehicle"]) <= 611)
                        {
                            this.StartingVehicle = (VehicleModelType)Convert.ToInt32(row["derby_startvehicle"]);
                        }
                        else
                        {
                            this.StartingVehicle = null;
                        }
                    }
                    else
					{
                        Logger.WriteLineAndClose($"Derby.cs - Derby.Load:E: Trying to load derby #{id} but it does not exists");
                        errorFlag = true;
                    }
                    GameMode.mySQLConnector.CloseReader();
                    if (!errorFlag)
                    {
                        GameMode.mySQLConnector.OpenReader("SELECT spawn_pos_x, spawn_pos_y, spawn_pos_z, spawn_rot " +
                            "FROM derby_spawn " +
                            "WHERE derby_id=@id", param);
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
                            this.SpawnPoints.Add(pos);
                            row = GameMode.mySQLConnector.GetNextRow();
                        }
                        for (int i = 0; i < this.SpawnPoints.Count; i++)
                        {
                            if (this.SpawnPoints[i].Position == Vector3.Zero)
                            {
                                this.SpawnPoints.RemoveAt(i);
                            }
                        }
                        if (this.SpawnPoints.Count == 0)
						{
                            Logger.WriteLineAndClose($"Derby.cs - Derby.Load:E: Trying to load derby #{id} but it does not have spawn points");
                            errorFlag = true;
                        }
                        GameMode.mySQLConnector.CloseReader();
                    }

                    if (!errorFlag)
                    {
                        GameMode.mySQLConnector.OpenReader(
                            "SELECT mapobject_model, mapobject_pos_x, mapobject_pos_y, mapobject_pos_z, mapobject_rot_x, mapobject_rot_y, mapobject_rot_z " +
                            "FROM derby_mapobjects " +
                            "WHERE derby_id=@id", param);
                        row = GameMode.mySQLConnector.GetNextRow();

                        this.MapObjects = new List<DynamicObject>();

                        int modelid = -1;
                        Vector3 pos;
                        Vector3 rot;
                        while (row.Count > 0)
                        {
                            modelid = Convert.ToInt32(row["mapobject_model"]);
                            pos = new Vector3(
                                        (float)Convert.ToDouble(row["mapobject_pos_x"]),
                                        (float)Convert.ToDouble(row["mapobject_pos_y"]),
                                        (float)Convert.ToDouble(row["mapobject_pos_z"])
                                );
                            rot = new Vector3(
                                        (float)Convert.ToDouble(row["mapobject_rot_x"]),
                                        (float)Convert.ToDouble(row["mapobject_rot_y"]),
                                        (float)Convert.ToDouble(row["mapobject_rot_z"])
                                );
                            this.MapObjects.Add(new DynamicObject(modelid, pos, rot));
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
                            this.Pickups.Add(new DerbyPickup(modelid, pos, virtualWorld, (DerbyPickup.PickupEvent)type));
                            row = GameMode.mySQLConnector.GetNextRow();
                        }
                        GameMode.mySQLConnector.CloseReader();
                    }
                    DerbyLoadedEventArgs args = new DerbyLoadedEventArgs();
                    args.derby = this;
                    args.success = !errorFlag;
                    OnLoaded(args);
                });
                t.Start();
            }
        }

        public Boolean IsPlayable()
        {
            return (IsLoaded && StartingVehicle != null) ? true : false;
        }

        public void Prepare(List<EventSlot> slots, int virtualWorld)
        {
            if (IsPlayable())
            { // TODO: implementer slots
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
                    DerbyPlayer playerData = new DerbyPlayer();
                    playerData.spectatePlayerIndex = -1;
                    playerData.status = DerbyPlayerStatus.Running;

                    playersData.Add(p, playerData);

                    playersLiveInfoHUD[p] = new HUD(p, "derbyhud.json");
                    playersLiveInfoHUD[p].Hide("iconrockets");
                    playersLiveInfoHUD[p].Hide("remainingrockets");
                    playersLiveInfoHUD[p].SetText("remainingplayers", "Remaining players: " + players.Count + "/" + players.Count);

                    p.VirtualWorld = virtualWorld;

                    p.KeyStateChanged += OnPlayerKeyStateChanged;

                    pos = rdm.Next(1, players.Count);
                    while (generatedPos.Contains(pos) && tries++ < this.SpawnPoints.Count)
                        pos = rdm.Next(1, players.Count);

                    if (tries >= this.SpawnPoints.Count)
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

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdown--;
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
        }

        public void Start()
        {
            if (isPreparing && countdown == 0)
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

        public void OnPlayerFinished(Player player, string reason)
        {
            if (reason.Equals("Finished"))
            {
                int place = players.Count;
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

                player.GameText(placeStr + " place !", 5000, 4);

            }

            if (player.InAnyVehicle)
            {
                BaseVehicle vehicle = player.Vehicle;
                player.RemoveFromVehicle();
                vehicle.Dispose();
            }

            players.Remove(player);
            if(players.Count == 0) // Si c'est le vainqueur
            {
                Eject(player);
                foreach (Player p in spectatingPlayers)
                {
                    Eject(p);
                }
                DerbyEventArgs args = new DerbyEventArgs();
                args.derby = this;
                OnFinished(args);
            }
            else
            {
                if (players.Count == 1) // Si il ne reste plus qu'un seul joueur, on l'exclu pour terminer le derby
                    OnPlayerFinished(players.FindLast(player => player.Id > 0), "Finished");

                player.ToggleSpectating(true);
                if (players[0].InAnyVehicle)
                    player.SpectateVehicle(players[0].Vehicle);
                else
                    player.SpectatePlayer(players[0]);
                spectatingPlayers.Add(player);
                playersData[player].status = DerbyPlayerStatus.Spectating;
                playersData[player].spectatePlayerIndex = 0;
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
