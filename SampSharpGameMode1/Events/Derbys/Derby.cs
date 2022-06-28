using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
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
        public bool success { get; set; }
        public Derby derby { get; set; }
        public int availableSlots { get; set; }
    }
    public class DerbyLoadedEventArgs : DerbyEventArgs
    {
        public bool success { get; set; }
    }
    public class Derby : EventSource
    {
        public const int MIN_PLAYERS_IN_DERBY = 0;
        public const int MAX_PLAYERS_IN_DERBY = 20;
        public int Id { get; set; }
        public string Name { get; set; }
        public VehicleModelType? StartingVehicle { get; set; }
        public List<Vector3R> SpawnPoints { get; set; }
        public int MapId { get; set; }
        public List<DerbyPickup> Pickups { get; set; }
        public bool IsLoaded { get; private set; }
        public bool IsCreatorMode { get; set; }
        public string Creator { get; set; }


        // Launcher only

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
        private Map map;

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
            Player.SendClientMessageToAll("Derby \"" + e.derby.Name + "\" is finished, the winner is " + Color.Orange + (e.derby.winner?.Name ?? "nobody") + Color.White + " !");
        }
        #endregion

        #region PlayerEvents
        public void OnPlayerDisconnect(object sender, DisconnectEventArgs e)
        {
            Player p = (Player)sender;
            players.Remove(p);
            if (players.Count == 0)
            {
                if (map != null)
                    map.Unload();
                spectatingPlayers.Clear();
                foreach (BaseVehicle veh in BaseVehicle.All)
                {
                    if (veh.VirtualWorld == this.virtualWorld)
                        veh.Dispose();
                }
                DerbyEventArgs args = new DerbyEventArgs();
                args.derby = this;
                OnFinished(args);
            }
        }
        public void OnPlayerVehicleDied(object sender, PlayerEventArgs e)
        {
            OnPlayerFinished((Player)e.Player, "Vehicle destroyed");
        }

        private void OnPlayerKeyStateChanged(object sender, KeyStateChangedEventArgs e)
        {
            //throw new NotImplementedException();
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
                    DerbyLoadedEventArgs args = new DerbyLoadedEventArgs();
                    args.derby = this;
                    args.success = !errorFlag;
                    args.availableSlots = availableSlots;
                    OnLoaded(args);
                });
                t.Start();
            }
        }

        public Boolean IsPlayable()
        {
            return (StartingVehicle != null && SpawnPoints.Count > Derby.MIN_PLAYERS_IN_DERBY) ? true : false;
        }

        public void Prepare(List<EventSlot> slots, int virtualWorld)
        {
            if (IsPlayable())
            {
                bool isAborted = false;
                this.isPreparing = true;
                this.players = new List<Player>();
                this.spectatingPlayers = new List<Player>();
                this.virtualWorld = virtualWorld;

                //List<DerbyPickup> tmpPickups = new List<DerbyPickup>();

                Random rdm = new Random();
                List<int> generatedPos = new List<int>();
                List<int> remainingPos = new List<int>();
                for (int i = 0; i < slots.Count; i++)
                    remainingPos.Add(i);
                int pos;
                int tries = 0;
                foreach (EventSlot slot in slots)
                {/*
                    foreach(DerbyPickup pickup in Pickups)
                    {
                        Console.WriteLine("Creating pickup for " + slot.Player.Name);
                        DerbyPickup p = new DerbyPickup(slot.Player, pickup.ModelId, pickup.Position, pickup.WorldId, pickup.Event);
                        p.Enable();
                        tmpPickups.Add(p);
                    }
                    */
                    DerbyPlayer playerData = new DerbyPlayer();
                    playerData.spectatePlayerIndex = -1;
                    playerData.status = DerbyPlayerStatus.Running;

                    playersData.Add(slot.Player, playerData);

                    playersLiveInfoHUD[slot.Player] = new HUD(slot.Player, "derbyhud.json");
                    playersLiveInfoHUD[slot.Player].Hide("iconrockets");
                    playersLiveInfoHUD[slot.Player].Hide("remainingrockets");
                    playersLiveInfoHUD[slot.Player].SetText("remainingplayers", "Remaining players: " + players.Count + "/" + players.Count);

                    slot.Player.VirtualWorld = virtualWorld;

                    slot.Player.KeyStateChanged += OnPlayerKeyStateChanged;
                    slot.Player.Disconnected += OnPlayerDisconnect;

                    pos = remainingPos[rdm.Next(0, remainingPos.Count)];

                    remainingPos.Remove(pos);
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
                    slot.Player.PutInVehicle(veh);
                    players.Add(slot.Player);
                }
                /*
                foreach(DerbyPickup pickup in Pickups)
                {
                    pickup.Dispose();
                }
                Pickups.Clear();
                Pickups = tmpPickups;
                foreach(DerbyPickup pickup in Pickups)
                {
                    Console.WriteLine("There is a pickup for " + pickup.Player.Name);
                }*/

                if (!isAborted)
                {
                    if (this.MapId > -1)
                    {
                        this.map = new Map();
                        this.map.Loaded += (sender, eventArgs) =>
                        {
                            SampSharp.GameMode.SAMP.Timer preparationTimer = new SampSharp.GameMode.SAMP.Timer(3000, false);
                            preparationTimer.Tick += (object sender, EventArgs e) =>
                            {
                                countdown = 3;
                                countdownTimer = new SampSharp.GameMode.SAMP.Timer(1000, true);
                                countdownTimer.Tick += CountdownTimer_Tick;
                            };
                        };
                        this.map.Load(this.MapId, virtualWorld);
                    }
                    else
                    {
                        SampSharp.GameMode.SAMP.Timer preparationTimer = new SampSharp.GameMode.SAMP.Timer(3000, false);
                        preparationTimer.Tick += (object sender, EventArgs e) =>
                        {
                            countdown = 3;
                            countdownTimer = new SampSharp.GameMode.SAMP.Timer(1000, true);
                            countdownTimer.Tick += CountdownTimer_Tick;
                        };
                    }
                }
                else
                {
                    Player.SendClientMessageToAll($"Derby {this.Name} aborted");
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
                    if (p.Vehicle != null)
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
                        winner = player;
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
            else if (reason.Equals("Leave"))
            {
                Logger.WriteLineAndClose($"Derby.cs - OnPlayerFinished:I: {player.Name} leaved the derby {this.Name}");
                player.SendClientMessage(Color.Wheat, "[Event]" + Color.White + " You leaved the derby");
                player.GameText("GAME OVER", 5000, 4);
            }
            else
            {
                Logger.WriteLineAndClose($"Derby.cs - OnPlayerFinished:I: {player.Name} has been ejected from the derby {this.Name} (reason: {reason})");
                player.SendClientMessage(Color.Wheat, "[Event]" + Color.White + " You lost (reason: " + reason + ")");
                player.GameText("GAME OVER", 5000, 4);
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
                SampSharp.GameMode.SAMP.Timer ejectionTimer = new SampSharp.GameMode.SAMP.Timer(2000, false);
                ejectionTimer.Tick += (object sender, EventArgs e) =>
                {
                    Eject(player);
                    List<Player> tmpPlayerList = new List<Player>(spectatingPlayers);
                    foreach (Player p in tmpPlayerList)
                    {
                        Eject(p);
                    }
                    if (map != null)
                        map.Unload();
                    foreach (BaseVehicle veh in BaseVehicle.All)
                    {
                        if (veh.VirtualWorld == this.virtualWorld)
                            veh.Dispose();
                    }
                    foreach (DynamicPickup pickup in DynamicPickup.All)
                    {
                        pickup.Dispose();
                    }
                    spectatingPlayers.Clear();
                    foreach (BaseVehicle veh in BaseVehicle.All)
                    {
                        if (veh.VirtualWorld == this.virtualWorld)
                            veh.Dispose();
                    }
                    DerbyEventArgs args = new DerbyEventArgs();
                    args.derby = this;
                    OnFinished(args);
                };
            }
            else
            {
                if (players.Count == 1) // Si il ne reste plus qu'un seul joueur, on l'exclu pour terminer le derby
                    OnPlayerFinished(players.FindLast(player => player.Id >= 0), "Finished");
                else
                {
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
        }
        public void Eject(Player player)
        {
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
                player.KeyStateChanged -= OnPlayerKeyStateChanged;
                player.ToggleSpectating(false);
                player.VirtualWorld = 0;
                player.pEvent = null;
                player.Spawn();
            }
        }
    }
}
