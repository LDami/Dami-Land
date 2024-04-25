using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.Pools;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Civilisation;
using SampSharpGameMode1.Display;
using SampSharpGameMode1.Events;
using SampSharpGameMode1.Map;
using static SampSharpGameMode1.Civilisation.PathExtractor;

namespace SampSharpGameMode1
{
    [PooledType]
    public class Player : BasePlayer
    {
        MySQLConnector mySQLConnector = null;

        private int dbid;
        public int DbId { get => dbid; set => dbid = value; }
        private string dbname; // Store the name to save in SaveAccount() if player is renaming in-game
        public string DbName { get => dbname; set => dbname = value; }

        private int adminlevel;
		public int Adminlevel { get => adminlevel; set => adminlevel = value; }

        //Statistics
        public DateTime LastLoginDate { get; set; }
        public TimeSpan PlayedTime { get; set; }
        public int PlayedRaces { get; set; }
        public int PlayedDerbies { get; set; }


        private Vector3R lastSavedPosition = Vector3R.Zero;
        public Vector3R LastSavedPosition { get => lastSavedPosition; set => lastSavedPosition = value; }



        //AntiCheat
        AntiCheat antiCheat;

        //Login
		Boolean isAuthenticated;
        int passwordEntryTries = 3;
        DateTime loginDateTime;

        //Vehicle
        public Speedometer Speedometer;
        public AirplaneHUD AirplaneHUD;
        private bool nitroEnabled;
        public bool NitroEnabled { get => nitroEnabled; set => nitroEnabled = value; }

        private List<BaseVehicle> spawnedVehicles = new List<BaseVehicle>();
        public List<BaseVehicle> SpawnedVehicles { get => spawnedVehicles; set => spawnedVehicles = value; }

        private bool disableForceEnterVehicle;
        public bool DisableForceEnterVehicle { get => disableForceEnterVehicle; set => disableForceEnterVehicle = value; }

        //Creators
        public TextdrawCreator textdrawCreator;
        public MapCreator mapCreator;
        public EventCreator eventCreator;
        public CameraController cameraController;

        //Event
        public Event pEvent;
        public bool IsInEvent { get => !(pEvent is null); }

        //Work
        public Works.WorkBase pWork;
        public bool IsInWork { get => !(pWork is null); }

        //NPC npc;
        VehicleAI vehicleAI;
        private SampSharp.GameMode.SAMP.Timer pathObjectsTimer;
        private List<DynamicObject> pathObjects = new List<DynamicObject>();
        private List<GlobalObject> naviObjects = new List<GlobalObject>();
        private TextLabel[] pathLabels = new TextLabel[1000];
        private TextLabel[] naviLabels = new TextLabel[1000];

        //Interior Preview
        public InteriorPreview InteriorPreview { get; private set; }

        public Player SpectatingTarget { get; set; } 
        public List<Player> Spectators { get; private set; }
        public Vector3R LastPositionBeforeSpectate { get; set; }
        public BaseVehicle? LastVehicleUsedBeforeSpectate { get; set; }
        public int LastVehicleSeatUsedBeforeSpectate { get; set; }

		#region Overrides of BasePlayer
        public override void OnConnected(EventArgs e)
        {
            base.OnConnected(e);

            Logger.WriteLineAndClose("Players.cs - Player.OnConnected:I: New player connected: [" + this.Id + "] " + this.Name);

            BasePlayer.SendClientMessageToAll($"{ColorPalette.Secondary.Main}{this.Name}{ColorPalette.Primary.Main} joined the server");
            this.SendClientMessage("Welcome to " + ColorPalette.Secondary.Main + "Dami's Land");
            this.SendClientMessage(ColorPalette.Primary.Main + "This server is still in beta, type /beta to see what is coming soon !");
            this.SendClientMessage(ColorPalette.Primary.Main + "Please read /event-infos to create events !");

            Console.WriteLine("New BasePlayer Connected");
            if (!this.IsNPC)
            {
#if DEBUG
                this.Notificate("DEBUG");
#endif
                isAuthenticated = false;
                Adminlevel = 0;

                antiCheat = new AntiCheat(this);

                mySQLConnector = MySQLConnector.Instance();

                textdrawCreator = new TextdrawCreator(this);
                //playerMapping = new PlayerMapping(this);
                cameraController = new CameraController(this);
                eventCreator = null;
                pEvent = null;

                Speedometer = new Speedometer(this);
                AirplaneHUD = new AirplaneHUD(this);

                pathObjectsTimer = new SampSharp.GameMode.SAMP.Timer(10000, true);
                //pathObjectsTimer.Tick += PathObjectsTimer_Tick;
                
                InteriorPreview = new InteriorPreview(this);

                SampSharp.Streamer.Streamer s = new SampSharp.Streamer.Streamer();
                s.ToggleIdleUpdate(this, true);

                Spectators = new List<Player>();

                // Preload shout animation for events spectators
                this.ApplyAnimation("ON_LOOKERS", "null", 4.1f, false, false, false, false, 0);

                if (this.IsRegistered())
                    ShowLoginForm();
                else
                    ShowSignupForm();
            }
        }

        private void PathObjectsTimer_Tick(object sender, EventArgs e)
        {
            if (this.State == PlayerState.OnFoot || this.State == PlayerState.Driving)
            {
                Console.WriteLine("Player.cs - Timer tick");
                Thread t = new Thread(new ThreadStart(UpdatePath));
                t.Start();
            }
            else Console.WriteLine("not spawned");
        }

        public override void OnDisconnected(DisconnectEventArgs e)
        {
            base.OnDisconnected(e);

            BasePlayer.SendClientMessageToAll($"{ColorPalette.Secondary.Main}{this.Name}{ColorPalette.Error.Main} left the server");

            if (!this.IsNPC)
            {
                SaveAccount();
                isAuthenticated = false;
                Adminlevel = 0;

                mySQLConnector = null;


                if(eventCreator != null)
                    eventCreator.Unload();
                eventCreator = null;

                Speedometer = null;
                AirplaneHUD = null;

                pathObjectsTimer.IsRunning = false;
                pathObjectsTimer.Dispose();
                pathObjectsTimer = null;

                pathObjects = new List<DynamicObject>();
                naviObjects = new List<GlobalObject>();
                pathLabels = new TextLabel[1000];
                naviLabels = new TextLabel[1000];
                //if (npc != null) npc.PlayerInstance.Dispose();

                if (this.vehicleAI != null)
                {
                    VehicleAI.Kick();
                }
            }
        }

        public override void OnRequestClass(RequestClassEventArgs e)
        {
            base.OnRequestClass(e);
            if (!this.IsNPC)
            {
                this.Position = new Vector3(471.8715, -1772.8622, 14.1192);
                this.Angle = 325.24f;
                this.CameraPosition = new Vector3(476.7202, -1766.9512, 15.2254);
                this.SetCameraLookAt(new Vector3(471.8715, -1772.8622, 14.1192));
            }
            else
            {
                Console.WriteLine("NPC Request class");
                e.PreventSpawning = false;
            }
        }
        public override void OnUpdate(PlayerUpdateEventArgs e)
        {
            base.OnUpdate(e);
            if(this.InAnyVehicle && Speedometer != null && !this.IsNPC)
            {
                Speedometer.Update();
                if (VehicleModelInfo.ForVehicle(this.Vehicle).Category == VehicleCategory.Airplane)
                    AirplaneHUD.Update();
            }
        }
        public override void OnEnterVehicle(EnterVehicleEventArgs e)
        {
            base.OnEnterVehicle(e);

        }
        public override void OnExitVehicle(PlayerVehicleEventArgs e)
        {
            base.OnExitVehicle(e);
        }

        public override void OnStateChanged(StateEventArgs e)
        {
            base.OnStateChanged(e);
            
            if(!this.IsNPC)
            {
                if (e.NewState == PlayerState.Driving || e.NewState == PlayerState.Passenger)
                {
                    Speedometer.Show();
                    if (VehicleModelInfo.ForVehicle(this.Vehicle).Category == VehicleCategory.Airplane)
                        AirplaneHUD.Show();
                    foreach (Player s in this.Spectators)
                    {
                        s.SpectateVehicle(this.Vehicle);
                    }
                }
                else
                {
                    Speedometer.Hide();
                    AirplaneHUD.Hide();
                    foreach (Player s in this.Spectators)
                    {
                        s.SpectatePlayer(this);
                    }
                }
            }
        }

        public override void OnEnterCheckpoint(EventArgs e)
        {
            base.OnEnterCheckpoint(e);
        }

        public override void OnEnterRaceCheckpoint(EventArgs e)
        {
            base.OnEnterRaceCheckpoint(e);
        }
        
        public override void OnSpawned(SpawnEventArgs e)
        {
            base.OnSpawned(e);
            if(!LastPositionBeforeSpectate.IsZero())
            {
                this.Position = LastPositionBeforeSpectate.Position;
                this.Angle = LastPositionBeforeSpectate.Rotation;
                if(LastVehicleUsedBeforeSpectate != null)
                {
                    this.PutInVehicle(LastVehicleUsedBeforeSpectate, LastVehicleSeatUsedBeforeSpectate);
                }
                LastPositionBeforeSpectate = Vector3R.Zero;
                LastVehicleUsedBeforeSpectate = null;
            }
        }

        public override void OnClickMap(PositionEventArgs e)
        {
            base.OnClickMap(e);
            //CalculateWay(this.Position, e.Position);
        }

		public override void OnPickUpPickup(PickUpPickupEventArgs e)
		{
			base.OnPickUpPickup(e);
            this.SendClientMessage("picked up !");
		}

		public override void OnKeyStateChanged(KeyStateChangedEventArgs e)
		{
            if (this.IsNPC)
                Logger.WriteLineAndClose("NPC new key pressed: " + e.NewKeys.ToString());
			base.OnKeyStateChanged(e);
            if (e.NewKeys.HasFlag(Keys.Fire) || e.NewKeys.HasFlag(Keys.Action))
            {
                if (this.InAnyVehicle && this.State == PlayerState.Driving && this.NitroEnabled && !this.IsInEvent)
                {
                    if (VehicleComponents.Get(1010).IsCompatibleWithVehicle(this.Vehicle))
                    {
                        this.Vehicle.AddComponent(1010);
                    }
                }
            }
		}
#endregion

		public void Notificate(string message, int style = 3)
        {
            if(!message.Equals(""))
                this.GameText(message, 1000, style);
        }

        public void Kick(string message)
        {
            this.SendClientMessage(message);
#if !DEBUG
            SampSharp.GameMode.SAMP.Timer kickTimer = new SampSharp.GameMode.SAMP.Timer(1000, false);
            kickTimer.Tick += (object sender, EventArgs e) =>
            {
                this.Kick();
            };
#endif
        }

        /// <summary>
        /// Teleports player, or the player's vehicle to the given position
        /// </summary>
        /// <param name="position">Position to teleport the player to</param>
        public void Teleport(Vector3 position)
		{
            position += Vector3.UnitZ;
            if (this.InAnyVehicle)
                this.Vehicle.Position = position;
            else
                this.Position = position;
		}

        public Boolean IsRegistered()
        {
            if (mySQLConnector != null)
            {
                Dictionary<string, object> param = new Dictionary<string, object>();
                param.Add("@name", this.Name);
                mySQLConnector.OpenReader("SELECT id FROM users WHERE name=@name", param);
                Dictionary<string, string> results = mySQLConnector.GetNextRow();
                mySQLConnector.CloseReader();
                if (results.Count > 0)
                {
                    this.DbId = Convert.ToInt32(results["id"]);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                Logger.WriteLineAndClose("Player.cs - Player.IsRegisterd:E: MySQL not started");
                return false;
            }
        }
        private void ShowSignupForm()
        {
            InputDialog pwdDialog = new InputDialog("Sign up", "Enter a password between 6 and 20 characters", false, "Sign up", "Quit");
            pwdDialog.Show(this);
            pwdDialog.Response += PwdSignupDialog_Response;
        }

        private void PwdSignupDialog_Response(object sender, DialogResponseEventArgs e)
        {
            if (e.DialogButton != DialogButton.Right)
            {
                if (e.InputText.Length < 6 || e.InputText.Length > 20)
                {
                    isAuthenticated = false;
                    ShowSignupForm();
                }
                else
                {
                    string hashPassword = Password.Crypt(e.InputText);
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("@name", this.Name);
                    param.Add("@password", hashPassword);
                    this.DbId = Convert.ToInt32(mySQLConnector.Execute("INSERT INTO users (name, password, adminlvl, money) VALUES (@name, @password, 0, 0)", param));
                    if (mySQLConnector.RowsAffected == 0)
                    {
                        this.SendClientMessage(Color.Red, "An error occured, please try again later");
                        this.Kick("Unable to create account");
                        Logger.WriteLineAndClose("Player.cs - Player.PwdSignupDialog_Response:E: Unable to create player (state: " + mySQLConnector.GetState() + ")");
                        return;
                    }
                    param.Clear();
                    param.Add("@id", this.DbId);
                    param.Add("@lastlogin", DateTime.Now);
                    mySQLConnector.Execute("INSERT INTO user_stats (user_id, stat_lastlogin) VALUES (@id, @lastlogin)", param);
                    if (mySQLConnector.RowsAffected == 0)
                    {
                        this.SendClientMessage(Color.Red, "An error occured, please try again later");
                        this.Kick("Unable to create account");
                        Logger.WriteLineAndClose("Player.cs - Player.PwdSignupDialog_Response:E: Unable to create player (state: " + mySQLConnector.GetState() + ")");
                        return;
                    }
                    else
                    {
                        this.Notificate("Registered");
                        isAuthenticated = true;
                        Adminlevel = 0;
                        LastLoginDate = DateTime.Now;
                        PlayedTime = TimeSpan.Zero;
                        PlayedRaces = 0;
                        PlayedDerbies = 0;
                        loginDateTime = DateTime.Now;
                    }
                }
            }
            else
                this.Kick();
        }
        private void ShowLoginForm()
        {
            if (passwordEntryTries > 0)
            {
                InputDialog pwdDialog = new InputDialog("Login", "You are registered, please enter your password\nRemaining attempts: " + passwordEntryTries + "/3", true, "Login", "Quit");
                pwdDialog.Show(this);
                pwdDialog.Response += PwdLoginDialog_Response;
            }
            else
                this.Kick();
        }

        private void PwdLoginDialog_Response(object sender, DialogResponseEventArgs e)
        {
            if (e.DialogButton == DialogButton.Left)
            {
                if (e.InputText.Length == 0)
                {
                    isAuthenticated = false;
                    ShowLoginForm();
                }
                else
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("@name", this.Name);
                    mySQLConnector.OpenReader("SELECT password, adminlvl, money FROM users WHERE name=@name", param);
                    Dictionary<string, string> results = mySQLConnector.GetNextRow();
                    mySQLConnector.CloseReader();
                    if (results.Count > 0)
                    {
                        if (Password.Verify(e.InputText, results["password"]))
                        {
                            this.Notificate("Logged in" + ((Adminlevel > 0) ? " as admin" : ""));
                            isAuthenticated = true;
                            Adminlevel = Convert.ToInt32(results["adminlvl"]);
                            this.Money = Convert.ToInt32(results["money"] == "[null]" ? "0" : results["money"]);
                            loginDateTime = DateTime.Now;

                            // Check if user exists in user_stats (compatiblity for version < 1.0)
                            param.Clear();
                            param.Add("@id", this.DbId);
                            mySQLConnector.OpenReader("SELECT user_id, stat_playtime, stat_playedraces, stat_playedderbies FROM user_stats WHERE user_id=@id", param);
                            results = mySQLConnector.GetNextRow();
                            mySQLConnector.CloseReader();

                            param.Clear();
                            param.Add("@id", this.DbId);
                            LastLoginDate = DateTime.Now;
                            param.Add("@lastlogin", LastLoginDate);
                            if (results.Count == 0)
                            {
                                mySQLConnector.Execute("INSERT INTO user_stats (user_id, stat_lastlogin) VALUES (@id, @lastlogin)", param);
                                PlayedTime = TimeSpan.Zero;
                                PlayedRaces = 0;
                                PlayedDerbies = 0;
                            }
                            else
                            {
                                PlayedTime = TimeSpan.Parse(results["stat_playtime"]);
                                PlayedRaces = Convert.ToInt32(results["stat_playedraces"]);
                                PlayedDerbies = Convert.ToInt32(results["stat_playedderbies"]);
                                mySQLConnector.Execute("UPDATE user_stats SET stat_lastlogin=@lastlogin WHERE user_id=@id", param);
                            }
                        }
                        else
                        {
                            isAuthenticated = false;
                            passwordEntryTries--;
                            ShowLoginForm();
                        }
                    }
                    else
                    {
                        this.SendClientMessage("There was an error in the password verification, please try again");
                        ShowLoginForm();
                    }
                }
            }
            else
                this.Kick();
        }

        public bool SaveAccount(string newPassword = null)
        {
            try
            {
                /* Check if the player username (if rename for example) is already used by another user */
                bool nameAlreadyExists = false;
                Dictionary<string, object> param = new Dictionary<string, object>();
                param.Add("@id", this.DbId);
                param.Add("@name", this.DbName);
                mySQLConnector.OpenReader("SELECT name FROM users WHERE id!=@id AND name=@name", param);
                Dictionary<string, string> results = mySQLConnector.GetNextRow();
                nameAlreadyExists = (results.Count > 0);
                mySQLConnector.CloseReader();

                if (nameAlreadyExists)
                    return false;

                param.Clear();
                string query;
                if (newPassword != null)
                {
                    string hashPassword = Password.Crypt(newPassword);
                    query = "UPDATE users SET name=@name, password=@password, adminlvl=@adminlvl, money=@money WHERE id=@id";
                    param.Add("@password", hashPassword);
                }
                else
                    query = "UPDATE users SET name=@name, adminlvl=@adminlvl, money=@money WHERE id=@id";
                param.Add("@id", this.DbId);
                param.Add("@name", this.Name);
                param.Add("@adminlvl", this.Adminlevel);
                param.Add("@money", this.Money);
                mySQLConnector.Execute(query, param);

                query = "UPDATE user_stats SET stat_playtime=@playtime, stat_playedraces=@playedraces, stat_playedderbies=@playedderbies WHERE user_id=@id";
                param.Clear();
                param.Add("@id", this.DbId);
                param.Add("@playtime", this.PlayedTime + DateTime.Now.Subtract(loginDateTime));
                param.Add("@playedraces", this.PlayedRaces);
                param.Add("@playedderbies", this.PlayedDerbies);
                mySQLConnector.Execute(query, param);
                if(mySQLConnector.RowsAffected > 0)
                    loginDateTime = DateTime.Now;
            }
            catch(Exception e)
            {
                Logger.WriteLineAndClose("Player.cs - Player.SaveAccount:E: Exception thrown during SaveAccount: " + e.Message);
            }
            return mySQLConnector.RowsAffected > 0;
        }

        private int GetArea(Vector3 position)
        {
            for (int i = 0; i < 64; i++)
            {
                try
                {
                    if (position.X > (PathExtractor.nodeBorders[i][0] ?? 0.0f) && position.X < (PathExtractor.nodeBorders[i][0] ?? 0.0f) + 750
                        && position.Y > (PathExtractor.nodeBorders[i][1] ?? 0.0f) && position.Y < (PathExtractor.nodeBorders[i][1] ?? 0.0f) + 750)
                    {
                        return i;
                    }
                }
                catch(Exception)
                {
                    Console.WriteLine("Player.cs - Player.GetArea:E: PathExtractor.nodeBorders[" + i + "] is null");
                }
            }
            return -1;
        }

        private void UpdatePath()
        {/*
            this.Notificate("Updating ...");
            int naviNodeCount = 0;

            Vector3 playerPos = this.Position;
            if(viewAreaID == -1) viewAreaID = GetArea(playerPos);
            Vector3 nearestPoint = Vector3.Zero;
            Console.WriteLine("playerpos: " + playerPos.ToString());

            Console.WriteLine("NodeIndex: " + viewAreaID + " ( " + PathExtractor.pathNodes[viewAreaID].Count + " nodes )");
            if (viewAreaID != -1)
            {
                int idx = 0;
                Vector3 lastNodePos = Vector3.Zero;

                PathExtractor.pathNodes[viewAreaID].Sort(delegate (PathExtractor.PathNode a, PathExtractor.PathNode b)
                {
                    return a.nodeID.CompareTo(b.nodeID);
                });
                foreach (PathExtractor.PathNode node in PathExtractor.pathNodes[viewAreaID])
                {
                    if (node.position.DistanceTo(playerPos) < 200)
                    {
                        if (idx > 0 && lastNodePos != Vector3.Zero)
                        {
                            if (nearestPoint == Vector3.Zero || nearestPoint.DistanceTo(playerPos) > lastNodePos.DistanceTo(playerPos))
                            {
                                nearestPoint = lastNodePos;
                            }
                            if (!pathObjects.Exists(x => x.Position.X == node.position.X && x.Position.Y == node.position.Y))
                            {
                                GlobalObject go = new GlobalObject(19130, lastNodePos + new Vector3(0.0, 0.0, 1.0), Vector3.Zero);

                                double angle = Math.Atan((node.position.X - lastNodePos.X) / (node.position.Y - lastNodePos.Y));
                                double angledegree = angle * 57.295779513;

                                go.Rotation = new Vector3(90.0, 0.0, -angledegree);
                                pathObjects.Add(go);

                                string txt = "ID: " + node.id + "\n" +
                                    "Path Node ID: " + node.nodeID;
                                TextLabel lbl = new TextLabel(txt, Color.White, lastNodePos + new Vector3(0.0, 0.0, 2.0), 200.0f);
                                pathLabels[pathObjects.Count - 1] = lbl;
                            }
                        }
                        lastNodePos = node.position;
                        idx ++;
                    }
                }
                
                foreach (PathExtractor.NaviNode node in PathExtractor.naviNodes[viewAreaID])
                {
                    Vector3 point = new Vector3(node.position.X, node.position.Y, PathExtractor.GetAverageZ(node.position.X, node.position.Y));
                    if (nearestPoint == Vector3.Zero || nearestPoint.DistanceTo(playerPos) > point.DistanceTo(playerPos))
                    {
                        nearestPoint = point;
                    }
                    if (point.DistanceTo(playerPos) < 200)
                    {
                        if (!naviObjects.Exists(x => x.Position.X == point.X && x.Position.Y == point.Y))
                        {
                            GlobalObject go = new GlobalObject(19130, point + new Vector3(0.0, 0.0, 1.0), Vector3.Zero);
                            double angle = Math.Atan(node.direction.X / node.direction.Y);
                            double angledegree = angle * 57.295779513;

                            go.Rotation = new Vector3(90.0, 0.0, -angledegree);
                            naviObjects.Add(go);
                            string txt = "Navi Node ID: " + node.id;
                            TextLabel lbl = new TextLabel(txt, Color.White, point + new Vector3(0.0, 0.0, 2.0), 200.0f);
                            naviLabels[naviObjects.Count] = lbl;
                        }
                        naviNodeCount++;
                    }
                }
                

                if (nearestPoint == Vector3.Zero)
                    this.SendClientMessage("No path node in this area !");
                else
                {
                    if (this.InAnyVehicle)
                        this.Vehicle.Position = nearestPoint + new Vector3(0.0, 0.0, 2.0);
                    else
                        this.Position = nearestPoint + new Vector3(0.0, 0.0, 2.0);
                }
            }
            Console.WriteLine("");
            Console.WriteLine("Player.cs - Number of Navinodes <= 200: " + naviNodeCount);

            this.Notificate("Updated !");
            */
        }

        public void CalculateWay(Vector3 from, Vector3 to)
        {
            foreach(DynamicObject obj in pathObjects)
            {
                obj.Dispose();
            }
            pathObjects.Clear();

            PathNode startNode, endNode;
            List<PathNode> allPathNodes = GetPathNodes();
            List<PathNode> allNearPathNodes = new List<PathNode>();

            PathNode nearestNodeFrom = new PathNode();
            PathNode nearestNodeTo = new PathNode();
            PathNode lastNode = new PathNode();


            /*
            GameMode gm = (GameMode)BaseMode.Instance;
            bool isSocketAlive = false;
            MySocketIO socket = gm.socket;
            if (socket.GetStatus() == MySocketIO.SocketStatus.CONNECTED)
            {
                isSocketAlive = true;
                Console.WriteLine("Player.cs - Player.CalculateWay:I: Sending datas ... ");
            }
            string data;
            foreach (PathNode node in allPathNodes)
            {
                if(node.position.DistanceTo(from) < from.DistanceTo(to) || node.position.DistanceTo(to) < from.DistanceTo(to))
                {
                    allNearPathNodes.Add(node);
                    data = "{ \"id\": \"" + node.id + "\", \"posX\": " + node.position.X + ", \"posY\": " + node.position.Y + ", \"links\": [";
                    int idx = 1;
                    foreach(LinkInfo link in node.links)
                    {
                        data += "\"" + link.targetNode.id + "\"";
                        if (idx < node.links.Count)
                            data += ",";
                        idx++;
                    }
                    data += "] }";
                    if (socket.GetStatus() == MySocketIO.SocketStatus.CONNECTED)
                    {
                        isSocketAlive = true;
                    }
                    if (isSocketAlive)
                    {
                        if (socket.Write(data) == -1) isSocketAlive = false;
                    }
                }
            }
            if(isSocketAlive)
                Console.WriteLine("Done");
            else
                Console.WriteLine("KO");
            */
            foreach (PathNode node in allPathNodes)
            {
                if (node.position.DistanceTo(from) < from.DistanceTo(to) || node.position.DistanceTo(to) < from.DistanceTo(to))
                {
                    allNearPathNodes.Add(node);
                }
            }
            foreach (PathNode node in allNearPathNodes)
            {
                if (lastNode.position != Vector3.Zero)
                {
                    if (nearestNodeFrom.position == Vector3.Zero || nearestNodeFrom.position.DistanceTo(from) > lastNode.position.DistanceTo(from))
                    {
                        nearestNodeFrom = lastNode;
                    }
                    if (nearestNodeTo.position == Vector3.Zero || nearestNodeTo.position.DistanceTo(to) > lastNode.position.DistanceTo(to))
                    {
                        nearestNodeTo = lastNode;
                    }
                }
                lastNode = node;
            }
            
            
            startNode = nearestNodeFrom;
            endNode = nearestNodeTo;
            this.SendClientMessage("Starting node: " + startNode.id);
            this.SendClientMessage("Ending node: " + endNode.id);
            this.SendClientMessage("Initializing Pathfinding ...");
            PathFinder pf = new PathFinder(allNearPathNodes, startNode, endNode);
            this.SendClientMessage("Done.");
            this.SendClientMessage("Pathfinding in progress ...");
            pf.Find();
            pf.Success += Pf_Success;
            pf.Failure += Pf_Failure;
            
        }

        private void Pf_Failure(object sender, EventArgs e)
        {
            this.SendClientMessage(Color.Red, "The pathfinding failed !");
        }

        private void Pf_Success(object sender, PathFindingDoneEventArgs e)
        {
            this.SendClientMessage(Color.Green, "The pathfinding succeeded in " + e.duration.ToString(@"hh\:mm\:ss\.fff"));
            PathNode? lastNode = null;
            foreach(PathNode node in e.path)
            {
                if(lastNode != null)
                {
                    DynamicObject dynamicObject = new DynamicObject(19187, node.position + new Vector3(0.0, 0.0, 1.0), Vector3.Zero, player: this);

                    double angle = Math.Atan((node.position.X - lastNode.GetValueOrDefault().position.X) / (node.position.Y - lastNode.GetValueOrDefault().position.Y));
                    double angledegree = angle * 57.295779513;

                    dynamicObject.Rotation = new Vector3(90.0, 0.0, -angledegree);
                    pathObjects.Add(dynamicObject);

                    string txt = "ID: " + node.id + "\n" +
                        "Path Node ID: " + node.nodeID;
                    //TextLabel lbl = new TextLabel(txt, Color.White, node.position + new Vector3(0.0, 0.0, 2.0), 200.0f);
                    //pathLabels[pathObjects.Count - 1] = lbl;
                }
                lastNode = node;
            }
        }

        [Command("textlabel")]
        private void TextLabelCommand(BasePlayer player)
        {
            this.SendClientMessage("List of 3D Player Text Labels for: " + player.Name);
            foreach (PlayerTextLabel label in PlayerTextLabel.Of(player))
            {
                this.SendClientMessage($"Id: {label.Id} ; Text: {label.Text}");
            }
        }
        /*
        [Command("create-script")]
        private void CreateScriptCommand()
        {
            Record record = RecordConverter.Parse(@"C:\stayinvehicle.rec");
            this.SendClientMessage("Record read");

            Vector3 destination = new Vector3(1600, 1610, 10.54);
            //Quaternion quat = Quaternion.CreateFromYawPitchRoll(destination.Z - this.Position.Z, destination.Y - this.Position.Y, destination.X - this.Position.X);
            float xAxis = (float)Math.Acos((destination.X - this.Position.X)/3000);
            float yAxis = (float)Math.Asin((destination.Y - this.Position.Y)/3000);
            double angle = Math.Acos(
                (this.Position.X * destination.X + this.Position.Y * destination.Y + this.Position.Z * destination.Z) / 
                (this.Position.LengthSquared * destination.LengthSquared)
            );
            angle *= 180 / Math.PI;
            this.SendClientMessage("xAxis: " + xAxis.ToString());
            this.SendClientMessage("yAxis: " + yAxis.ToString());
            this.SendClientMessage("angle: " + angle.ToString());
            Quaternion quat = Quaternion.CreateFromAxisAngle(new Vector3(xAxis, yAxis, 0), (float)angle);
            this.SendClientMessage("Quat: " + quat.ToVector4().ToString());
            this.SendClientMessage("QuatX: " + quat.X.ToString());

            float dot = Vector3.Dot(this.Position, destination);
            Console.WriteLine(Vector3.Normalize(destination).ToString());

            int recordCount = 20;
            uint deltaTime = 0;
            double speed = 0.5;
            Vector3 lastPos = record.VehicleBlocks[0].position;
            for (int i = 0; i < record.VehicleBlocks.Count; i++)
            {
                if (i > 0)
                {
                    deltaTime = record.VehicleBlocks[i].time - record.VehicleBlocks[i - 1].time;
                    lastPos = record.VehicleBlocks[i - 1].position;
                }
                RecordInfo.VehicleBlock block = record.VehicleBlocks[i];
                //block.velocity = new Vector3(0.0, speed, 0.0);
                //block.position = new Vector3(block.position.X, lastPos.Y + (0.001 * deltaTime * 0.5 * 45.1), block.position.Z);
                block.rotQuaternion1 = quat.W;
                block.rotQuaternion2 = quat.X;
                block.rotQuaternion3 = quat.Y;
                block.rotQuaternion4 = quat.Z;
                block.additionnalKeyCode = 8;
                block.vehicleHealth = 1000;
                record.VehicleBlocks[i] = block;
            }
            record.VehicleBlocks = record.VehicleBlocks.GetRange(0, recordCount);

            this.SendClientMessage("Record random velocity value: " + record.VehicleBlocks[3].velocity);
            RecordCreator.Save(record, @"recreated.rec");
            this.SendClientMessage("Record wrote");
        }

        [Command("read-script")]
        private void ReadScriptCommand(string filename)
        {
            Record record = RecordConverter.Parse(@$"C:\Serveur OpenMP\npcmodes\recordings\{filename}.rec", filename+ ".json");
            this.SendClientMessage("Record read");
        }

        [Command("simulate-script")]
        private void SimulateScriptCommand()
        {
            Record record = RecordConverter.Parse(@"C:\Serveur OpenMP\npcmodes\recordings\recreated.rec");
            Thread t = new Thread(new ThreadStart(() =>
            {
                for(int i=1; i < record.VehicleBlocks.Count; i++)
                {
                    RecordInfo.VehicleBlock block = record.VehicleBlocks[i];
                    if(this.InAnyVehicle)
                    {
                        if(i > 0)
                        {
                            double dist;
                            if ((dist = record.VehicleBlocks[i-1].position.DistanceTo(this.Vehicle.Position)) < 1.0)
                                this.SendClientMessage($"{i}: OK Distance: {dist}");
                            else
                                this.SendClientMessage($"{i}: NOK Distance: {dist}");
                        }
                        this.Vehicle.Position = block.position;
                        this.Vehicle.Velocity = block.velocity;

                        Thread.Sleep((int)(record.VehicleBlocks[i].time - record.VehicleBlocks[i - 1].time));
                    }
                }
            }));
            t.Start();
        }
        */

        [Command("ai-cmds")]
        private void AICmdsCommand()
        {
            ListDialog dialog = new ListDialog("AI Actions", "Execute", "Cancel");
            dialog.AddItems(new string[] {
                "create",
                "start",
                "kick",
                "go to next pos",
                "restart AI"
            });
            dialog.Response += (sender, e) =>
            {
                if(e.DialogButton == DialogButton.Left)
                {
                    switch (e.ListItem)
                    {
                        case 0: // create

                            //VehicleAI.Init(VehicleModelType.Mower, PathTools.GetNeirestPathNode(this.Position).position, 0.0f);
                            //string npcName = VehicleAI.Init(VehicleModelType.Mower, new Vector3(1501.22, 1712.39, 10.54), 0.0f);
                            //VehicleAI.SetMode(1);

                            List<PathNode> allPathNodes = PathExtractor.carNodes;
                            List<PathNode> allNearPathNodes = new List<PathNode>();

                            PathNode nearestNodeFrom = new PathNode();
                            PathNode nearestNodeTo = new PathNode();
                            PathNode lastNode = new PathNode();

                            Vector3 from = this.Position;
                            Vector3 to = new Vector3(2595.62, 1472.35, 10.40);

                            foreach (PathNode node in allPathNodes)
                            {
                                if (node.position.DistanceTo(from) < from.DistanceTo(to) || node.position.DistanceTo(to) < from.DistanceTo(to))
                                {
                                    allNearPathNodes.Add(node);
                                }
                            }
                            foreach (PathNode node in allNearPathNodes)
                            {
                                if (lastNode.position != Vector3.Zero)
                                {
                                    if (nearestNodeFrom.position == Vector3.Zero || nearestNodeFrom.position.DistanceTo(from) > lastNode.position.DistanceTo(from))
                                    {
                                        nearestNodeFrom = lastNode;
                                    }
                                    if (nearestNodeTo.position == Vector3.Zero || nearestNodeTo.position.DistanceTo(to) > lastNode.position.DistanceTo(to))
                                    {
                                        nearestNodeTo = lastNode;
                                    }
                                }
                                lastNode = node;
                            }

                            this.SendClientMessage("Initializing Pathfinding ...");
                            PathFinder pf = new PathFinder(allNearPathNodes, nearestNodeFrom, nearestNodeTo);
                            this.SendClientMessage("Done.");
                            this.SendClientMessage("Pathfinding in progress ...");
                            pf.Find();
                            pf.Success += (obj, e) =>
                            {
                                /*
                                PathNode[] pathNodes = new PathNode[2];
                                pathNodes[0] = new PathNode();
                                pathNodes[0].position = new Vector3(1501.22, 1712.39, 10.54);
                                pathNodes[1] = new PathNode();
                                pathNodes[1].position = new Vector3(1402.59, 1858.7728, 10.54);
                                VehicleAI.SetPath(pathNodes);*/

                                //VehicleAI.SetPath(e.path);
                                NPCController.Add("npctest2", new Queue<Vector3>(e.path.Select(x => x.position)));
                            };
                            pf.Failure += Pf_Failure;
                            this.SendClientMessage("AI created");
                            break;
                        case 1: // start
                            ListDialog listDialog = new ListDialog("Chose the NPC to start", "Start", "Cancel");
                            List<string> botNames = NPCController.GetConnectedBotNames().ToList();
                            listDialog.AddItems(botNames);
                            listDialog.Response += (obj, evt) =>
                            {
                                ListDialog listDialog2 = new ListDialog("Chose the way to start", "Start", "Cancel");
                                listDialog2.AddItems(new List<string>() { "On foot", "Vehicle" });
                                listDialog2.Response += (obj, evt2) =>
                                {
                                    NPCController.StartAI(botNames[evt.ListItem], evt2.ListItem == 0 ? "onfoot" : "vehicle");
                                };
                                listDialog2.Show(this);
                            };
                            listDialog.Show(this);
                            break;
                        case 2: // kick
                            ListDialog listDialogKick = new ListDialog("Chose the NPC to kick", "Start", "Cancel");
                            List<string> botNamesKick = NPCController.GetConnectedBotNames().ToList();
                            listDialogKick.AddItems(botNamesKick);
                            listDialogKick.Response += (obj, evt) =>
                            {
                                NPCController.Kick(botNamesKick[evt.ListItem]);
                            };
                            listDialogKick.Show(this);
                            break;
                        case 3: // go to next
                            ListDialog listDialog2 = new ListDialog("Chose the NPC you want to update", "Start", "Cancel");
                            List<string> botNames2 = NPCController.GetConnectedBotNames().ToList();
                            listDialog2.AddItems(botNames2);
                            listDialog2.Response += (obj, evt) =>
                            {
                                NPCController.GoToNextPos(botNames2[evt.ListItem]);
                            };
                            listDialog2.Show(this);
                            break;
                        case 4: // restart AI
                            //VehicleAI.StartVehicle();
                            this.SendClientMessage("Not implemented");
                            break;
                        default:
                            break;
                    }
                }
            };
            dialog.Show(this);
        }
        [Command("ai-speed")]
        private void AISpeedCommand(float speed)
        {
            VehicleAI.SetSpeed(speed);
        }
        [Command("ai-timemul")]
        private void AITimeMulCommand(int multiplier)
        {
            VehicleAI.SetTimeMultiplier(multiplier);
        }


        [Command("record-start")]
        private void RecordStartCommand(int start)
        {
            bool stopRequested = false;
            Thread t = new Thread(new ThreadStart(() =>
            {
                Vector3 lastPos = this.Position;
                while(!stopRequested)
                {
                    Console.WriteLine("Player velocity: " + this.Vehicle.Velocity + "; Distance ran in 500ms: " + Vector3.Distance(lastPos, this.Vehicle.Position));
                    lastPos = this.Vehicle.Position;
                    Thread.Sleep(500);
                }
            }));
            if(start == 0)
            {
                stopRequested = true;
            }
            else
            {
                stopRequested = false;
                t.Start();
            }
        }

        [Command("followme")]
        private void FollowMeCommand()
        {
            VehicleAI.SetDestination(this.Position + new Vector3(0.0, 5.0, 0.0), 0.5);
        }
        [Command("start")]
        private void StartAICommand()
        {
            VehicleAI.StartVehicle();
        }
        [Command("stop")]
        private void StopAICommand()
        {
            VehicleAI.StopVehicle();
        }
        [Command("sethp")]
        private void KillAICommand(int health)
        {
            if (vehicleAI != null)
                vehicleAI.SetNPCHealth(health);
            else
                Console.WriteLine("vehicleAI is null !");
        }
        [Command("accel")]
        private void AccelCommand()
        {
            if(this.InAnyVehicle)
            {
                Thread t = new Thread(() =>
                {
                    this.Vehicle.Velocity = new Vector3(0, 1, 0);
                    Vector3 prev = this.Vehicle.Position;
                    Thread.Sleep(1000);
                    this.SendClientMessage("Difference in 1s: " + (prev - this.Vehicle.Position));
                });
                t.Start();
            }
        }

        [Command("select-td")]
        private void SelectTDCommand()
        {
            this.SelectTextDraw(Color.OrangeRed);
        }

        [Command("getlink")]
        private void GetLinkCommand(int areaID, string id)
        {
            if (PathExtractor.pathPoints != null)
            {
                List<string> links = PathExtractor.GetLinkedNode(areaID, id);
                if (links != null)
                {
                    this.SendClientMessage(Color.AliceBlue, "Links for path node " + id);
                    foreach (string msg in links)
                        this.SendClientMessage(msg);
                }
                else
                    this.SendClientMessage("No links for this node");
            }
        }

        [Command("getarea")]
        private void GetareaCommand()
        {
            int area = this.GetArea(this.Position);
            this.SendClientMessage("Your area is: " + area);
            this.SendClientMessage(PathExtractor.headers[area].ToString());
        }

        [Command("getpos")]
        private void GetPosCommand()
        {
            this.SendClientMessage("Your position is: " + this.Position.ToString());
        }

        [Command("getangle")]
        private void GetAngleCommand()
        {
            // Angle: North = 0, West = 90, South = 180, East = 270 (anti-clockwise)
            if(this.InAnyVehicle)
            {
                this.SendClientMessage("Your vehicle's angle is: " + this.Vehicle.Angle.ToString());
                this.Vehicle.GetRotationQuat(out float w, out float x, out float y, out float z);
                this.SendClientMessage($"Your vehicle's rotation quat is: (w = {w}, x = {x}, y = {y}, z = {z})");
            }
            else
                this.SendClientMessage("Your angle is: " + this.Angle.ToString());
        }

        [Command("getz")]
        private void GetZCommand()
        {
            if (PathExtractor.pathPoints != null)
            {
                this.SendClientMessage("Position.Z = " + PathExtractor.FindZFromVector2(this.Position.X, this.Position.Y));
            }
        }

        class GroupedCommandClass
        {

            [CommandGroup("pathnode")]
            class PathnodeCommandClass
            {
                [Command("hide")]
                private static void Hide(Player player)
                {
                    foreach (TextLabel lbl in player.pathLabels)
                    {
                        if (lbl != null)
                            lbl.Color = new Color(255, 255, 255, 0);
                    }
                }
                [Command("show")]
                private static void Show(Player player)
                {
                    foreach (TextLabel lbl in player.pathLabels)
                    {
                        if (lbl != null)
                            lbl.Color = new Color(255, 255, 255, 255);
                    }
                }
                [Command("tp")]
                private static void tp(Player player, int index, int area = -1)
                {
                    if (area == -1) area = player.GetArea(player.Position);
                    foreach (PathExtractor.PathNode node in PathExtractor.pathNodes[area])
                    {
                        if (node.nodeID == index)
                        {
                            if (player.InAnyVehicle)
                                player.Vehicle.Position = node.position + new Vector3(0.0, 0.0, 2.0);
                            else
                                player.Position = node.position + new Vector3(0.0, 0.0, 2.0);
                            break;
                        }
                    }
                }
            }

            [CommandGroup("navinode")]
            class NavinodeCommandClass
            {
                [Command("hide")]
                private static void Hide(Player player)
                {
                    foreach (TextLabel lbl in player.naviLabels)
                    {
                        if(lbl != null)
                            lbl.Color = new Color(255, 255, 255, 0);
                    }
                }
                [Command("show")]
                private static void Show(Player player)
                {
                    foreach (TextLabel lbl in player.naviLabels)
                    {
                        if (lbl != null)
                            lbl.Color = new Color(255, 255, 255, 255);
                    }
                }
                [Command("tp")]
                private static void tp(Player player, int index, int area = -1)
                {
                    if (area == -1) area = player.GetArea(player.Position);
                    foreach (PathExtractor.NaviNode node in PathExtractor.naviNodes[area])
                    {
                        if (node.targetNodeID == index)
                        {
                            Vector3 pos = new Vector3(node.position.X, node.position.Y, PathExtractor.GetAverageZ(node.position.X, node.position.Y) + 2.0);
                            Console.WriteLine("Teleporting player to navi node " + node.targetNodeID + " in area " + node.targetAreaID + " at position " + pos.ToString());
                            if (player.InAnyVehicle)
                                player.Vehicle.Position = pos;
                            else
                                player.Position = pos;
                            break;
                        }
                    }
                }
                [Command("create")]
                private static void Create(Player player, int index, int area = -1)
                {
                    if (area == -1) area = player.GetArea(player.Position);
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    List<PathExtractor.NaviNode> nodes = PathExtractor.naviNodes[area].FindAll(x => x.targetAreaID == area && x.targetNodeID == index);
                    watch.Stop();
                    Console.WriteLine("Player.cs - Execution time with .Find method: " + watch.ElapsedMilliseconds + " ms");

                    nodes.Clear();
                    watch = System.Diagnostics.Stopwatch.StartNew();
                    foreach (PathExtractor.NaviNode node in PathExtractor.naviNodes[area])
                    {
                        if (node.targetAreaID == area && node.targetNodeID == index)
                        {
                            nodes.Add(node);
                        }
                    }
                    watch.Stop();
                    Console.WriteLine("Player.cs - Execution time with for loop: " + watch.ElapsedMilliseconds + " ms");

                    foreach(PathExtractor.NaviNode node in nodes)
                    {
                        Vector3 point = new Vector3(node.position.X, node.position.Y, PathExtractor.GetAverageZ(node.position.X, node.position.Y));
                        if (!player.naviObjects.Exists(x => x.Position.X == point.X && x.Position.Y == point.Y))
                        {
                            GlobalObject go = new GlobalObject(19130, point + new Vector3(0.0, 0.0, 1.0), Vector3.Zero);
                            double angle = Math.Atan(node.direction.X / node.direction.Y);
                            double angledegree = angle * 57.295779513;

                            go.Rotation = new Vector3(90.0, 0.0, -angledegree);
                            player.naviObjects.Add(go);
                            string txt = "Navi Node ID: " + node.targetNodeID + "\n" +
                                "area ID: " + node.targetAreaID + "\n" +
                                "Direction: " + node.direction.ToString() + "\n" +
                                "Angle: " + angledegree + "\n" +
                                "Flags: " + node.flags;
                            TextLabel lbl = new TextLabel(txt, Color.White, point + new Vector3(0.0, 0.0, 2.0), 200.0f);
                            player.naviLabels[player.naviObjects.Count] = lbl;
                            Console.WriteLine("Navi node n° " + player.naviObjects.LastIndexOf(go) + " created: " + point.ToString());
                        }
                    }
                }
            }

            /*
            [CommandGroup("npc")]
            class NPCCommandClass
            {
                [Command("create")]
                private static void Create(Player player)
                {
                    player.npc = new NPC();
                    player.npc.Create();
                    player.SendClientMessage("NPC created");
                }
                [Command("connect")]
                private static void Connect(Player player)
                {
                    player.npc = new NPC();
                    player.npc.Connect(player);
                    player.SendClientMessage("NPC connected");
                }
            }
            */

            public static Player GetPlayerByDatabaseId(int id)
            {
                Player result = null;
                foreach (Player player in Player.All)
                {
                    if (player.DbId == id)
                    {
                        result = player;
                        break;
                    }
                }
                return result;
            }
        }

    }
}