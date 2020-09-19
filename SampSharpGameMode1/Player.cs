using System;
using System.Collections.Generic;
using System.Threading;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.Pools;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Civilisation;
using SampSharpGameMode1.Events.Races;

namespace SampSharpGameMode1
{
    [PooledType]
    public class Player : BasePlayer
    {
        MySQLConnector mySQLConnector = null;

        private int db_id;
        public int Db_Id { get => db_id; set => db_id = value; }

        Boolean isConnected;
        int passwordEntryTries = 3;

        TextdrawCreator textdrawCreator;
        PlayerMapping playerMapping;
        public RaceCreator playerRaceCreator;

        public Race playerRace;

        NPC npc;
        private SampSharp.GameMode.SAMP.Timer pathObjectsTimer;
        private List<GlobalObject> pathObjects = new List<GlobalObject>();
        private List<GlobalObject> naviObjects = new List<GlobalObject>();
        private TextLabel[] pathLabels = new TextLabel[1000];
        private TextLabel[] naviLabels = new TextLabel[1000];

        private int viewAreaID = -1;

        #region Overrides of BasePlayer
        public override void OnConnected(EventArgs e)
        {
            base.OnConnected(e);

            //this.SetSpawnInfo(0, 0, new Vector3(1431.6393, 1519.5398, 10.5988), 0.0f);
            

            isConnected = false;

            mySQLConnector = MySQLConnector.Instance();

            textdrawCreator = new TextdrawCreator(this);
            playerMapping = new PlayerMapping(this);
            playerRaceCreator = null;
            playerRace = null;

            pathObjectsTimer = new SampSharp.GameMode.SAMP.Timer(10000, true);
            //pathObjectsTimer.Tick += PathObjectsTimer_Tick;

            if (this.IsRegistered())
                ShowLoginForm();
            else
                ShowSignupForm();
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

            isConnected = false;

            mySQLConnector = null;
            playerMapping = null;
            playerRaceCreator = null;

            pathObjectsTimer.IsRunning = false;
            pathObjectsTimer.Dispose();
            pathObjectsTimer = null;

            pathObjects = new List<GlobalObject>();
            naviObjects = new List<GlobalObject>();
            pathLabels = new TextLabel[1000];
            naviLabels = new TextLabel[1000];

            if (npc != null) npc.Dispose();
        }

        public override void OnRequestClass(RequestClassEventArgs e)
        {
            base.OnRequestClass(e);
            this.Position = new Vector3(471.8715, -1772.8622, 14.1192);
            this.Angle = 325.24f;
            this.CameraPosition = new Vector3(476.7202, -1766.9512, 15.2254);
            this.SetCameraLookAt(new Vector3(471.8715, -1772.8622, 14.1192));
        }
        public override void OnUpdate(PlayerUpdateEventArgs e)
        {
            base.OnUpdate(e);
            if (playerMapping != null)
                playerMapping.Update();
            //if (playerRaceCreator != null)
            //    playerRaceCreator.Update();
        }
        public override void OnEnterVehicle(EnterVehicleEventArgs e)
        {
            base.OnEnterVehicle(e);
        }

        public override void OnEnterCheckpoint(EventArgs e)
        {
            if (playerRace != null)
            {
                playerRace.OnPlayerEnterCheckpoint(this);
            }
        }

        public override void OnEnterRaceCheckpoint(EventArgs e)
        {
            if (playerRace != null)
            {
                playerRace.OnPlayerEnterCheckpoint(this);
            }
        }

        public override void OnSpawned(SpawnEventArgs e)
        {
            base.OnSpawned(e);
        }
        #endregion

        public void Notificate(string message)
        {
            if(!message.Equals(""))
                this.GameText(message, 1000, 3);
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
                    this.Db_Id = Convert.ToInt32(results["id"]);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                Console.WriteLine("Player.cs - Player.IsRegisterd:E: MySQL not started");
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
                    isConnected = false;
                    ShowSignupForm();
                }
                else
                {
                    string hashPassword = Password.Crypt(e.InputText);
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("@name", this.Name);
                    param.Add("@password", hashPassword);
                    mySQLConnector.Execute("INSERT INTO users (name, password, adminlvl) VALUES (@name, @password, 0)", param);
                    if (mySQLConnector.RowsAffected > 0)
                    {
                        this.Notificate("Registered");
                        isConnected = true;
                        param = new Dictionary<string, object>();
                        param.Add("@name", this.Name);
                        mySQLConnector.OpenReader("SELECT id FROM users WHERE name=@name", param);
                        Dictionary<string, string> results = mySQLConnector.GetNextRow();
                        this.Db_Id = Convert.ToInt32(results["id"]);
                        mySQLConnector.CloseReader();
                        this.Spawn();
                    }
                    else
                        Console.WriteLine("Player.cs - Player.PwdSignupDialog_Response:E: Unable to create player (state: " + mySQLConnector.GetState() + ")");
                }
            }
            else
                this.Kick();
        }
        private void ShowLoginForm()
        {
            if (passwordEntryTries > 0)
            {
                InputDialog pwdDialog = new InputDialog("Login", "You are registered, please enter your password\nRemaining attempts:" + passwordEntryTries + "/3", true, "Login", "Quit");
                pwdDialog.Show(this);
                pwdDialog.Response += PwdLoginDialog_Response;
            }
        }

        private void PwdLoginDialog_Response(object sender, DialogResponseEventArgs e)
        {
            if (e.DialogButton != DialogButton.Right)
            {
                if (e.InputText.Length == 0)
                {
                    isConnected = true;
                    //this.Spawn();
                    /*
                    isConnected = false;
                    ShowLoginForm();
                    */
                }
                else
                {
                    Dictionary<string, object> param = new Dictionary<string, object>();
                    param.Add("@name", this.Name);
                    mySQLConnector.OpenReader("SELECT password FROM users WHERE name=@name", param);
                    Dictionary<string, string> results = mySQLConnector.GetNextRow();
                    mySQLConnector.CloseReader();
                    if (results.Count > 0)
                    {
                        if (Password.Verify(e.InputText, results["password"]))
                        {
                            isConnected = true;
                            this.Notificate("Logged in");
                            this.Spawn();
                        }
                        else
                        {
                            isConnected = false;
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

        /**
         * Boolean IsInEvent
         * Parameters: None
         * Returns: true (if the player is in any event) ; else false
         * */
        public Boolean IsInEvent()
        {
            return false;
        }

        public static Player GetPlayerByDatabaseId(int id)
        {
            Player result = null;
            foreach(Player player in Player.All)
            {
                if(player.Db_Id == id)
                {
                    result = player;
                    break;
                }
            }
            return result;
        }

        private int GetArea(Vector3 position)
        {
            for (int i = 0; i < 64; i++)
            {
                /*
                Console.WriteLine("position: " + position.ToString());
                Console.WriteLine("index: " + i);
                Console.WriteLine(((position.X > PathExtractor.nodeBorders[i][0]) ? "true" : "false") + "-" + ((position.X < PathExtractor.nodeBorders[i][0] + 750) ? "true" : "false"));
                Console.WriteLine(((position.Y > PathExtractor.nodeBorders[i][1]) ? "true" : "false") + "-" + ((position.Y < PathExtractor.nodeBorders[i][1] + 750) ? "true" : "false"));
                */

                if (position.X > PathExtractor.nodeBorders[i][0] && position.X < PathExtractor.nodeBorders[i][0] + 750
                    && position.Y > PathExtractor.nodeBorders[i][1] && position.Y < PathExtractor.nodeBorders[i][1] + 750)
                {
                    return i;
                }
            }
            return -1;
        }

        private void UpdatePath()
        {
            this.Notificate("Updating ...");
            int naviNodeCount = 0;

            Vector3 playerPos = this.Position;
            if(viewAreaID == -1) viewAreaID = GetArea(playerPos);
            Vector3 nearestPoint = Vector3.Zero;
            Console.WriteLine("playerpos: " + playerPos.ToString());

            Console.WriteLine("NodeIndex: " + viewAreaID + " ( " + PathExtractor.pathNodes[viewAreaID].Count + " nodes )");
            if (viewAreaID != -1)
            {
                /*
                foreach (PathExtractor.PathNode node in PathExtractor.pathNodes[nodeIndex])
                {
                    Vector3 point = node.position;
                    if (nearestPoint == Vector3.Zero || nearestPoint.DistanceTo(playerPos) > point.DistanceTo(playerPos))
                    {
                        nearestPoint = point;
                    }
                    if (point.DistanceTo(playerPos) < 200)
                    {
                        if (!pathObjects.Exists(x => x.Position.X == point.X && x.Position.Y == point.Y))
                        {
                            GlobalObject go = new GlobalObject(19130, point + new Vector3(0.0, 0.0, 1.0), Vector3.Zero);
                            pathObjects.Add(go);
                            string txt = "Node ID: " + node.nodeID + "\n" +
                                "Area ID: " + node.areaID + "\n" +
                                "Flags: " + node.flags;
                            TextLabel lbl = new TextLabel(txt, Color.White, point + new Vector3(0.0, 0.0, 2.0), 200.0f);
                            pathLabels[pathObjects.Count] = lbl;
                            Console.WriteLine("Path node n° " + pathObjects.LastIndexOf(go) + " created: " + point.ToString());
                        }
                        naviNodeCount++;
                    }
                }
                */
                int idx = 0;
                Vector3 lastNodePos = Vector3.Zero;

                PathExtractor.pathNodes[viewAreaID].Sort(delegate (PathExtractor.PathNode a, PathExtractor.PathNode b)
                {
                    return a.nodeID.CompareTo(b.nodeID);
                });
                foreach (PathExtractor.PathNode node in PathExtractor.pathNodes[viewAreaID])
                {
                    if (idx > 0 && lastNodePos != Vector3.Zero)
                    {
                        if (nearestPoint == Vector3.Zero || nearestPoint.DistanceTo(playerPos) > lastNodePos.DistanceTo(playerPos))
                        {
                            nearestPoint = lastNodePos;
                        }
                        GlobalObject go = new GlobalObject(19130, lastNodePos + new Vector3(0.0, 0.0, 1.0), Vector3.Zero);

                        double angle = Math.Atan((node.position.X - lastNodePos.X) / (node.position.Y - lastNodePos.Y));
                        double angledegree = angle * 57.295779513;

                        go.Rotation = new Vector3(90.0, 0.0, -angledegree);
                        pathObjects.Add(go);

                        string txt = "Path Node ID: " + (node.nodeID - 1) + "\n" +
                            "Area ID: " + node.areaID + "\n" +
                            "Flags: " + node.flags;
                        TextLabel lbl = new TextLabel(txt, Color.White, lastNodePos + new Vector3(0.0, 0.0, 2.0), 200.0f);
                        pathLabels[pathObjects.Count] = lbl;
                    }

                    lastNodePos = node.position;
                    idx++;
                }
                /*
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
                            string txt = "Navi Node ID: " + node.nodeID + "\n" +
                                "Area ID: " + node.areaID + "\n" +
                                "Direction: " + node.direction.ToString() + "\n" +
                                "Angle: " + angledegree + "\n" +
                                "Flags: " + node.flags;
                            TextLabel lbl = new TextLabel(txt, Color.White, point + new Vector3(0.0, 0.0, 2.0), 200.0f);
                            naviLabels[naviObjects.Count] = lbl;
                            Console.WriteLine("Navi node n° " + naviObjects.LastIndexOf(go) + " created: " + point.ToString());
                        }
                        naviNodeCount++;
                    }
                }
                */

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
            Console.WriteLine("Player.cs - Number of Navinodes <= 500: " + naviNodeCount);

            /*
            foreach (PathExtractor.NaviNode node in PathExtractor.naviNodes[0].FindAll(x => x.position.DistanceTo(new Vector2(this.Position.X, this.Position.Y)) <= 100.0))
            {
                Vector2 point = node.position;
                if (!pathLabels2.Exists(x => x.Position.X == point.X && x.Position.Y == point.Y))
                {
                    TextLabel lbl = new TextLabel("NaviNode: " + node.nodeID + "\n" + point.ToString(), Color.White, new Vector3(point.X, point.Y, 20.0f), 100.0f);
                    pathLabels2.Add(lbl);
                }
            }
            foreach (TextLabel lbl in pathLabels2.FindAll(x => x.Position.DistanceTo(this.Position) > 100.0))
            {
                pathLabels2.Remove(lbl);
                lbl.Dispose();
            }
            */
            this.Notificate("Updated !");
        }

        [Command("test")]
        private void TestCommand()
        {
            if (PathExtractor.pathPoints != null)
            {
                this.Position = PathExtractor.pathPoints[0] + new Vector3(0.0f, 0.0f, 30.0f);
            }
        }

        [Command("test2")]
        private void Test2Command(int area = -1)
        {
            if (area == -1) area = this.GetArea(this.Position);
            if (PathExtractor.pathPoints != null)
            {
                viewAreaID = area;
                UpdatePath();
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

        [Command("getz")]
        private void GetZCommand()
        {
            if (PathExtractor.pathPoints != null)
            {
                this.SendClientMessage("Position.Z = " + PathExtractor.FindZFromVector2(this.Position.X, this.Position.Y));
            }
        }

        [Command("join")]
        private void JoinCommand()
        {
            if (GameMode.eventManager.openedEvent != null)
            {
                GameMode.eventManager.openedEvent.Join(this);
            }
            else this.SendClientMessage(Color.Red, "There is no event to join");
        }

        [Command("event")]
        private void EventCommand()
        {
            GameMode.eventManager.ShowManagerDialog(this);
        }

        [Command("mapping")]
        private void MappingCommand()
        {
            if (!playerMapping.IsInMappingMode)
                playerMapping.Enter();
            else
                playerMapping.Exit();
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
                        if (node.nodeID == index)
                        {
                            Vector3 pos = new Vector3(node.position.X, node.position.Y, PathExtractor.GetAverageZ(node.position.X, node.position.Y) + 2.0);
                            Console.WriteLine("Teleporting player to navi node " + node.nodeID + " in area " + node.areaID + " at position " + pos.ToString());
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
                    List<PathExtractor.NaviNode> nodes = PathExtractor.naviNodes[area].FindAll(x => x.areaID == area && x.nodeID == index);
                    watch.Stop();
                    Console.WriteLine("Player.cs - Execution time with .Find method: " + watch.ElapsedMilliseconds + " ms");

                    nodes.Clear();
                    watch = System.Diagnostics.Stopwatch.StartNew();
                    foreach (PathExtractor.NaviNode node in PathExtractor.naviNodes[area])
                    {
                        if (node.areaID == area && node.nodeID == index)
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
                            string txt = "Navi Node ID: " + node.nodeID + "\n" +
                                "area ID: " + node.areaID + "\n" +
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
            [CommandGroup("td")]
            class TextdrawCommandClass
            {
                [Command("init")]
                private static void InitTD(Player player)
                {
                    if (player.textdrawCreator == null)
                        player.textdrawCreator = new TextdrawCreator(player);

                    player.textdrawCreator.Init();
                    player.SendClientMessage("Textdraw Creator initialized");
                }
                [Command("exit")]
                private static void Exit(Player player)
                {
                    if (player.textdrawCreator != null)
                    {
                        player.textdrawCreator.Close();
                    }
                }
                [Command("add box")]
                private static void AddBox(Player player, string name)
                {
                    if (player.textdrawCreator != null)
                    {
                        player.textdrawCreator.AddBox(name);
                        player.SendClientMessage("Textdraw created");
                    }
                }
                [Command("add text")]
                private static void AddText(Player player, string name)
                {
                    if (player.textdrawCreator != null)
                    {
                        player.textdrawCreator.AddText(name);
                        player.SendClientMessage("Textdraw created");
                    }
                }
                [Command("select")]
                private static void Select(Player player, string name)
                {
                    if (player.textdrawCreator != null)
                    {
                        player.textdrawCreator.Select(name);
                    }
                }
                [Command("load")]
                private static void Load(Player player, string name)
                {
                    if (player.textdrawCreator != null)
                    {
                        player.textdrawCreator.Load(name);
                    }
                }
                [Command("save")]
                private static void Save(Player player, string name)
                {
                    if (player.textdrawCreator != null)
                    {
                        player.textdrawCreator.Save(name);
                    }
                }

            }

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
        }

    }
}