using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.World;
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
        public int Id { get; private set; }
        public string Name { get; set; }
        public VehicleModelType? StartingVehicle { get; set; }
        public List<Vector3R> StartingSpawn { get; set; }
        public bool IsLoaded { get; private set; }


        private bool isPreparing = false;
        public bool isStarted = false;
        public List<Player> players;
        public Dictionary<Player, DerbyPlayer> playersData = new Dictionary<Player, DerbyPlayer>();
        public List<Player> spectatingPlayers; // Contains spectating players who looses the derby, and others players who spectate without joining
        public Player winner;
        private int virtualWorld;
        private SampSharp.GameMode.SAMP.Timer countdownTimer;
        private int countdown;
        public DateTime startedTime;

        #region DerbyEvents
        public event EventHandler<DerbyLoadedEventArgs> Loaded;
        public event EventHandler<DerbyLoadedEventArgs> LoadFailed;
        protected virtual void OnLoaded(DerbyLoadedEventArgs e)
        {
            if (e.success)
                Loaded?.Invoke(this, e);
            else
            {
                IsLoaded = false;
                LoadFailed?.Invoke(this, e);
            }
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
                        GameMode.mySQLConnector.CloseReader();
                    }
                    else
                        errorFlag = true;


                    if (!errorFlag)
                    {
                        GameMode.mySQLConnector.OpenReader("SELECT spawn_index, spawnpos_x, spawnpos_y, spawnpos_z, spawnpos_rot " +
                            "FROM derby_spawnpos " +
                            "WHERE derby_id=@id", param);
                        row = GameMode.mySQLConnector.GetNextRow();
                        if (row.Count == 0) errorFlag = true;

                        this.StartingSpawn = new List<Vector3R>();

                        Vector3R pos;
                        while (row.Count > 0)
                        {
                            pos = new Vector3R(new Vector3(
                                        (float)Convert.ToDouble(row["spawnpos_x"]),
                                        (float)Convert.ToDouble(row["spawnpos_y"]),
                                        (float)Convert.ToDouble(row["spawnpos_z"])
                                    ),
                                    (float)Convert.ToDouble(row["spawnpos_rot"])
                                );
                            this.StartingSpawn.Add(pos);
                            row = GameMode.mySQLConnector.GetNextRow();
                        }
                        for (int i = 0; i < this.StartingSpawn.Count; i++)
                        {
                            if (this.StartingSpawn[i].Position == Vector3.Zero)
                            {
                                this.StartingSpawn.RemoveAt(i);
                            }
                        }
                        if (this.StartingSpawn.Count == 0)
                            errorFlag = true;
                        GameMode.mySQLConnector.CloseReader();
                    }
                });
                t.Start();
            }
        }

        public Boolean IsPlayable()
        {
            return (IsLoaded && StartingVehicle != null) ? true : false;
        }

        public void Prepare(List<Player> players, int virtualWorld)
        {
            if (IsPlayable())
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
                    DerbyPlayer playerData = new DerbyPlayer();
                    playerData.spectatePlayerIndex = -1;
                    playerData.status = DerbyPlayerStatus.Running;

                    playersData.Add(p, playerData);

                    p.VirtualWorld = virtualWorld;

                    p.KeyStateChanged += OnPlayerKeyStateChanged;

                    pos = rdm.Next(1, players.Count);
                    while (generatedPos.Contains(pos) && tries++ < this.StartingSpawn.Count)
                        pos = rdm.Next(1, players.Count);

                    if (tries >= this.StartingSpawn.Count)
                    {
                        Player.SendClientMessageToAll("Error during position randomization for the race. Race aborted");
                        isAborted = true;
                        break;
                    }
                    tries = 0;

                    BaseVehicle veh = BaseVehicle.Create(StartingVehicle.GetValueOrDefault(VehicleModelType.Bike), this.StartingSpawn[pos].Position, this.StartingSpawn[pos].Rotation, 1, 1);
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
