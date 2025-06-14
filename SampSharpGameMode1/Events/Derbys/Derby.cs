﻿using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public int availableSlots { get; set; }
    }
    public class DerbyFinishedEventArgs : DerbyEventArgs
    {
        public BasePlayer winner { get; set; }
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
        public float MinimumHeight { get; set; }
        public TimeOnly Time { get; set; }


        // Launcher only

        private bool isPreparing = false;
        public bool isStarted = false;
        public List<Player> players;
        public Dictionary<Player, DerbyPlayer> playersData = new();
        public List<Player> spectatingPlayers; // Contains spectating players who looses the derby, and others players who spectate without joining
        private Dictionary<Player, HUD> playersLiveInfoHUD = new();
        private int virtualWorld;
        private SampSharp.GameMode.SAMP.Timer countdownTimer;
        private int countdown;
        public DateTime startedTime;
        private Map.Map map;

        #region DerbyEvents
        public event EventHandler<DerbyLoadedEventArgs> Loaded;
        protected virtual void OnLoaded(DerbyLoadedEventArgs e)
        {
            Loaded?.Invoke(this, e);
            IsLoaded = e.success;
        }

        public event EventHandler<DerbyFinishedEventArgs> Finished;
        protected virtual void OnFinished(DerbyFinishedEventArgs e)
        {
            Finished?.Invoke(this, e);
            Player.SendClientMessageToAll(Color.Wheat, $"[Event]{Color.White} The Derby {ColorPalette.Secondary.Main}{e.derby.Name}{Color.White} is finished, the winner is {Color.Orange}{(e.winner?.Name ?? "nobody")}{Color.White} !");
            foreach (Player sp in spectatingPlayers)
            {
                sp.CancelSelectTextDraw();
                sp.pEvent.RemoveFromSpectating(sp);
            }
        }
        #endregion

        #region PlayerEvents
        private void OnPlayerUpdate(object sender, PlayerUpdateEventArgs e)
        {
            if(((BasePlayer)sender).Position.Z < this.MinimumHeight)
            {
                OnPlayerFinished((Player)sender, "Fall from the map");
                ((Player)sender).Update -= OnPlayerUpdate;
            }
        }
        public void OnPlayerDisconnect(object sender, DisconnectEventArgs e)
        {
            OnPlayerFinished((Player)sender, "Disconnected");
        }
        private void OnPlayerExitVehicle(object sender, PlayerVehicleEventArgs e)
        {
            e.Player.Position = e.Vehicle.Position;
            SampSharp.GameMode.SAMP.Timer timer = new(100, false);
            timer.Tick += (sender, e2) =>
            {
                if (!e.Vehicle.IsDisposed)
                    e.Player.PutInVehicle(e.Vehicle);
            };
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
            if (GameMode.MySQLConnector != null)
            {
                Thread t = new(() =>
                {
                    IsLoaded = false;
                    bool errorFlag = false;
                    Dictionary<string, string> row;
                    Dictionary<string, object> param = new()
                    {
                        { "@id", id }
                    };
                    GameMode.MySQLConnector.OpenReader("SELECT * FROM derbys WHERE derby_id=@id", param);
                    row = GameMode.MySQLConnector.GetNextRow();
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
                    GameMode.MySQLConnector.CloseReader();

                    int availableSlots = 0;
                    if (!errorFlag)
                    {
                        GameMode.MySQLConnector.OpenReader("SELECT spawn_pos_x, spawn_pos_y, spawn_pos_z, spawn_rot " +
                            "FROM derby_spawn " +
                            "WHERE derby_id=@id", param);
                        row = GameMode.MySQLConnector.GetNextRow();

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
                            row = GameMode.MySQLConnector.GetNextRow();
                        }
                        GameMode.MySQLConnector.CloseReader();
                    }

                    if (!errorFlag)
                    {
                        GameMode.MySQLConnector.OpenReader(
                            "SELECT pickup_id, pickup_event, pickup_model, pickup_pos_x, pickup_pos_y, pickup_pos_z " +
                            "FROM derby_pickups " +
                            "WHERE derby_id=@id", param);
                        row = GameMode.MySQLConnector.GetNextRow();

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
                            row = GameMode.MySQLConnector.GetNextRow();
                        }
                        GameMode.MySQLConnector.CloseReader();
                    }

                    DerbyLoadedEventArgs args = new()
                    {
                        derby = this,
                        success = !errorFlag,
                        availableSlots = availableSlots
                    };

                    if (this.MapId > -1)
                    {
                        this.map = new Map.Map();
                        this.map.Loaded += (sender, eventArgs) =>
                        {
                            this.Time = eventArgs.map.Time;
                            OnLoaded(args);
                        };
                        this.map.Load(this.MapId, virtualWorld);
                    }
                    else
                    {
                        this.Time = TimeOnly.Parse("12:00:00");
                        OnLoaded(args);
                    }
                });
                t.Start();
            }
        }

        public Boolean IsPlayable()
        {
            return (StartingVehicle != null && SpawnPoints.Count > Derby.MIN_PLAYERS_IN_DERBY);
        }

        public void Prepare(List<EventSlot> slots)
        {
            if (IsPlayable())
            {
                bool isAborted = false;
                this.isPreparing = true;
                this.players = new List<Player>();
                this.spectatingPlayers = new List<Player>();

                Random rdm = new();
                List<int> generatedPos = new();
                List<int> remainingPos = new();
                for (int i = 0; i < slots.Count; i++)
                    remainingPos.Add(i);
                int pos;
                foreach (EventSlot slot in slots.Where(s => s.SpectateOnly == false))
                {
                    DerbyPlayer playerData = new()
                    {
                        spectatePlayerIndex = 0,
                        status = DerbyPlayerStatus.Running
                    };

                    playersLiveInfoHUD[slot.Player] = new HUD(slot.Player, "derbyhud.json");
                    playersLiveInfoHUD[slot.Player].Hide("iconrockets");
                    playersLiveInfoHUD[slot.Player].Hide("remainingrockets");
                    playersLiveInfoHUD[slot.Player].SetText("remainingplayers", slots.Count.ToString(@"000"));

                    slot.Player.VirtualWorld = virtualWorld;
                    slot.Player.SetTime(this.Time.Hour, this.Time.Minute);

                    slot.Player.Update += OnPlayerUpdate;
                    slot.Player.ExitVehicle += OnPlayerExitVehicle;
                    slot.Player.KeyStateChanged += OnPlayerKeyStateChanged;
                    slot.Player.Disconnected += OnPlayerDisconnect;

                    pos = remainingPos[rdm.Next(0, remainingPos.Count)];

                    remainingPos.Remove(pos);

                    playerData.startPosition = new Vector3R(this.SpawnPoints[pos].Position, this.SpawnPoints[pos].Rotation);
                    playersData.Add(slot.Player, playerData);

                    BaseVehicle veh = BaseVehicle.Create(StartingVehicle.GetValueOrDefault(VehicleModelType.Bike), this.SpawnPoints[pos].Position, this.SpawnPoints[pos].Rotation, 1, 1);
                    veh.VirtualWorld = virtualWorld;
                    veh.Engine = false;
                    veh.Doors = true;
                    veh.Died += OnPlayerVehicleDied;
                    slot.Player.PutInVehicle(veh);
                    players.Add(slot.Player);
                }

                foreach (EventSlot spectatorSlot in slots.Where(s => s.SpectateOnly == true))
                {
                    DerbyPlayer playerData = new()
                    {
                        spectatePlayerIndex = 0,
                        status = DerbyPlayerStatus.Spectating
                    };

                    playersLiveInfoHUD[spectatorSlot.Player] = new HUD(spectatorSlot.Player, "derbyhud.json");
                    playersLiveInfoHUD[spectatorSlot.Player].Hide("iconrockets");
                    playersLiveInfoHUD[spectatorSlot.Player].Hide("remainingrockets");
                    playersLiveInfoHUD[spectatorSlot.Player].SetText("remainingplayers", slots.Count.ToString(@"000"));

                    spectatorSlot.Player.VirtualWorld = virtualWorld;
                    spectatorSlot.Player.SetTime(this.Time.Hour, this.Time.Minute);
                    spectatorSlot.Player.ToggleControllable(true);
                    spectatorSlot.Player.ResetWeapons();
                    Thread.Sleep(10); // Used to prevent AntiCheat to detect weapon before player enters in vehicle

                    spectatorSlot.Player.Disconnected += OnPlayerDisconnect;
                    spectatorSlot.Player.KeyStateChanged += OnPlayerKeyStateChanged;

                    spectatorSlot.Player.pEvent.SetPlayerInSpectator(spectatorSlot.Player);
                    spectatingPlayers.Add(spectatorSlot.Player);
                    playersData.Add(spectatorSlot.Player, playerData);
                }

                if (!isAborted)
                {
                    SampSharp.GameMode.SAMP.Timer preparationTimer = new(3000, false);
                    preparationTimer.Tick += (object sender, EventArgs e) =>
                    {
                        countdown = 3;
                        countdownTimer = new SampSharp.GameMode.SAMP.Timer(1000, true);
                        countdownTimer.Tick += CountdownTimer_Tick;
                    };
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
            if(countdown == 1)
            {
                foreach(Player p in players)
                {
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
            if (isPreparing && countdown == 0)
            {
                startedTime = DateTime.Now;
                this.isPreparing = false;
                this.isStarted = true;
                foreach (Player p in players)
                {
                    if (p.Vehicle != null)
                        p.Vehicle.Engine = true;
                    p.PlayedDerbies++;
                }
            }
        }

        public void OnPlayerFinished(Player player, string reason)
        {
            if (!players.Contains(player))
                return;
            if (reason.Equals("Finished"))
            {
                int place = players.Count;
                string coloredPlaceStr = "";
                string placeStr = "";
                int money = 0;
                switch (place)
                {
                    case 1:
                        coloredPlaceStr = $"{Color.Gold}1st{Color.White}";
                        placeStr = "1st";
                        money = 1000;
                        player.PlaySound(5448);
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
                        coloredPlaceStr = place + "th";
                        money = 0;
                        break;
                }

                player.GiveMoney(money);
                Event.SendEventMessageToAll(player.pEvent, $"{Color.Orange}{player.Name}{Color.White} finished the derby at {coloredPlaceStr} place");

                player.GameText(placeStr + " place !", 5000, 4);
                Logger.WriteLineAndClose($"Derby.cs - OnPlayerFinished:I: {player.Name} finished the derby {this.Name} at {placeStr} place");
            }
            else if (reason.Equals("Leave") || reason.Equals("Disconnected"))
            {
                Event.SendEventMessageToPlayer(player, "You left the derby");
                Logger.WriteLineAndClose($"Derby.cs - OnPlayerFinished:I: {player.Name} left the derby {this.Name}");
                Event.SendEventMessageToAll(player.pEvent, $"{Color.Orange}{player.Name}{Color.White} has left the derby");
                player.GameText("GAME OVER", 5000, 4);
            }
            else
            {
                Event.SendEventMessageToPlayer(player, "You lost (reason: " + reason + ")");
                Logger.WriteLineAndClose($"Derby.cs - OnPlayerFinished:I: {player.Name} has been ejected from the derby {this.Name} (reason: {reason})");
                Event.SendEventMessageToAll(player.pEvent, $"{Color.Orange}{player.Name}{Color.White} has lost the derby (reason: {reason})");
                player.GameText("GAME OVER", 5000, 4);
            }

            players.Remove(player);
            foreach (Player p in players)
                playersLiveInfoHUD[p].SetText("remainingplayers", players.Count.ToString(@"000"));
            foreach (Player p in spectatingPlayers)
                playersLiveInfoHUD[p].SetText("remainingplayers", players.Count.ToString(@"000"));
            if (players.Count == 0) // Si c'est le vainqueur
            {
                SampSharp.GameMode.SAMP.Timer ejectionTimer = new(2000, false);
                ejectionTimer.Tick += (object sender, EventArgs e) =>
                {
                    DerbyFinishedEventArgs args = new()
                    {
                        derby = this,
                        winner = player
                    };
                    OnFinished(args);
                };
            }
            else
            {
                if (players.Count == 1) // Si il ne reste plus qu'un seul joueur, on l'exclu pour terminer le derby
                    OnPlayerFinished(players.FindLast(player => player.Id >= 0), "Finished");
                else
                {
                    foreach (Player spectators in spectatingPlayers)
                    {
                        spectators.pEvent.UpdateSpectatingPlayersHUD(spectators);
                    }
                    if (!reason.Equals("Disconnected"))
                    {
                        if (player.InAnyVehicle)
                        {
                            BaseVehicle vehicle = player.Vehicle;
                            player.RemoveFromVehicle();
                            if (vehicle is not null && !vehicle.IsDisposed) vehicle.Dispose();
                        }
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
        }
        public void Eject(Player player)
        {
            playersLiveInfoHUD[player].Hide();
            players.RemoveAll(x => x.Equals(player));
            spectatingPlayers.RemoveAll(x => x.Equals(player));
            playersData.Remove(player);

            if (!player.IsDisposed)
            {
                if (player.InAnyVehicle)
                {
                    BaseVehicle vehicle = player.Vehicle;
                    player.RemoveFromVehicle();
                    vehicle?.Dispose();
                }
                player.DisableCheckpoint();
                player.DisableRaceCheckpoint();
                player.Update -= OnPlayerUpdate;
                player.ExitVehicle -= OnPlayerExitVehicle;
                player.KeyStateChanged -= OnPlayerKeyStateChanged;
                player.Disconnected -= OnPlayerDisconnect;
                player.ToggleSpectating(false);
                player.VirtualWorld = 0;
                player.SetTime(12, 0);
                player.pEvent = null;
                player.Spawn();
            }
        }

        public void Unload()
        {
            if(!IsCreatorMode)
            {
                if (players != null) // If event didn't start
                {
                    foreach (Player sp in spectatingPlayers)
                    {
                        sp.CancelSelectTextDraw();
                        sp.pEvent.RemoveFromSpectating(sp);
                    }
                    foreach (Player p in BasePlayer.All.Cast<Player>())
                    {
                        if (p.VirtualWorld == this.virtualWorld)
                            Eject(p);
                    }
                }
            }
            map?.Unload();
            foreach (BaseVehicle v in BaseVehicle.All)
            {
                if (v.VirtualWorld == this.virtualWorld)
                    v.Dispose();
            }
            foreach (DerbyPickup pickup in this.Pickups)
            {
                pickup.Dispose();
            }
            Logger.WriteLineAndClose($"Derby.cs - Derby.Unload:I: Derby {this.Name} unloaded");
        }
        public void ReloadMap(Action loadedAction)
        {
            this.map?.Unload();

            if (this.MapId > -1)
            {
                this.map = new Map.Map();
                this.map.Loaded += (sender, eventArgs) =>
                {
                    this.Time = eventArgs.map.Time;
                    loadedAction();
                };
                this.map.Load(this.MapId, virtualWorld);
            }
        }

        public bool IsPlayerSpectating(Player player)
        {
            if (this.spectatingPlayers == null) return false; // can happen if /leave is entered before Race start
            return this.spectatingPlayers.Contains(player);
        }

        public void AddSpectator(Player player)
        {
            DerbyPlayer playerData = new()
            {
                spectatePlayerIndex = 0,
                status = DerbyPlayerStatus.Spectating,
            };

            playersLiveInfoHUD[player] = new HUD(player, "derbyhud.json");
            playersLiveInfoHUD[player].Hide("iconrockets");
            playersLiveInfoHUD[player].Hide("remainingrockets");
            playersLiveInfoHUD[player].SetText("remainingplayers", this.players.Count.ToString(@"000"));


            player.VirtualWorld = virtualWorld;
            player.SetTime(this.Time.Hour, this.Time.Minute);
            player.ToggleControllable(true);
            player.ResetWeapons();
            Thread.Sleep(10); // Used to prevent AntiCheat to detect weapon before player enters in vehicle

            player.Disconnected += OnPlayerDisconnect;
            player.KeyStateChanged += OnPlayerKeyStateChanged;

            spectatingPlayers.Add(player);
            playersData.Add(player, playerData);
        }

        public List<Player> GetPlayers()
        {
            return this.players;
        }

        public static List<string> GetPlayerDerbyList(Player player)
        {
            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new()
                {
                    { "@name", player.Name }
                };
            mySQLConnector.OpenReader("SELECT derby_id, derby_name FROM derbys WHERE derby_creator = @name", param);
            List<string> result = new();
            Dictionary<string, string> row = mySQLConnector.GetNextRow();
            while (row.Count > 0)
            {
                result.Add(row["derby_id"] + "_" + Display.ColorPalette.Primary.Main + row["derby_name"]);
                row = mySQLConnector.GetNextRow();
            }
            mySQLConnector.CloseReader();

            return result;
        }

        public static Dictionary<string, string> Find(string str)
        {
            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new()
                {
                    { "@name", str }
                };
            mySQLConnector.OpenReader("SELECT derby_id, derby_name FROM derbys WHERE derby_name LIKE @name", param);
            Dictionary<string, string> results = mySQLConnector.GetNextRow();
            mySQLConnector.CloseReader();
            return results;
        }
        public static Dictionary<string, string> GetInfo(int id)
        {
            // id, name, creator, number of spawn points, number of pickups, number of map objects
            Dictionary<string, string> results = new();
            Dictionary<string, string> row;
            bool exists = false;

            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new()
                {
                    { "@id", id }
                };

            mySQLConnector.OpenReader("SELECT derby_id, derby_name, derby_creator, map_name FROM derbys LEFT JOIN maps ON (derby_map = map_id) WHERE derby_id = @id", param);

            row = mySQLConnector.GetNextRow();
            if (row.Count > 0) exists = true;
            foreach (KeyValuePair<string, string> kvp in row)
                results.Add(MySQLConnector.Field.GetFieldName(kvp.Key), kvp.Value);

            mySQLConnector.CloseReader();

            if (exists)
            {
                mySQLConnector.OpenReader("SELECT COUNT(*) as nbr " +
                    "FROM derby_spawn WHERE derby_id = @id", param);
                row = mySQLConnector.GetNextRow();
                results.Add("Spawn points", row["nbr"]);
                mySQLConnector.CloseReader();

                mySQLConnector.OpenReader("SELECT COUNT(*) as nbr " +
                    "FROM derby_pickups WHERE derby_id = @id", param);
                row = mySQLConnector.GetNextRow();
                results.Add("Pickups", row["nbr"]);
                mySQLConnector.CloseReader();
            }

            return results;
        }
    }
}
