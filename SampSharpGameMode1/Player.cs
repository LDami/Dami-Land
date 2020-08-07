using System;
using System.Collections.Generic;
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
        private Timer pathObjectsTimer;
        private List<GlobalObject> pathObjects = new List<GlobalObject>();
        private List<TextLabel> pathLabels = new List<TextLabel>();
        private List<TextLabel> pathLabels2 = new List<TextLabel>();

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

            pathObjectsTimer = new Timer(10000, true);
            pathObjectsTimer.Tick += PathObjectsTimer_Tick;

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
                this.Notificate("Updating ...");
                int count = 0;
                foreach (PathExtractor.PathNode node in PathExtractor.pathNodes.FindAll(x => x.position.DistanceTo(this.Position) <= 200.0))
                {
                    Vector3 point = node.position;
                    if (!pathObjects.Exists(x => x.Position.X == point.X && x.Position.Y == point.Y))
                    {
                        GlobalObject go = new GlobalObject(18728, point, Vector3.Zero);
                        pathObjects.Add(go);
                        Console.WriteLine("Object n° " + pathObjects.LastIndexOf(go) + " created: " + point);
                    }
                    count++;
                }
                Console.WriteLine("Player.cs - Number of PathNode <= 200: " + count);
                count = 0;
                foreach (GlobalObject go in pathObjects.FindAll(x => x.Position.DistanceTo(this.Position) > 200.0))
                {
                    pathObjects.Remove(go);
                    go.Dispose();
                    count++;
                }
                Console.WriteLine("Player.cs - Number of GlobalObjects > 200: " + count);
                count = 0;
                foreach (PathExtractor.PathNode node in PathExtractor.pathNodes.FindAll(x => x.position.DistanceTo(this.Position) <= 100.0))
                {
                    Vector3 point = node.position;
                    if (!pathLabels.Exists(x => x.Position.X == point.X && x.Position.Y == point.Y))
                    {
                        TextLabel lbl = new TextLabel("PathNode: " + node.nodeID + "\n" + point.ToString(), Color.White, point, 200.0f);
                        pathLabels.Add(lbl);
                    }
                    count++;
                }
                Console.WriteLine("Player.cs - Number of PathNode <= 100: " + count);
                count = 0;
                foreach (TextLabel lbl in pathLabels.FindAll(x => x.Position.DistanceTo(this.Position) > 100.0))
                {
                    pathLabels.Remove(lbl);
                    lbl.Dispose();
                    count++;
                }
                Console.WriteLine("Player.cs - Number of TextLabel > 100: " + count);

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

        public override void OnEnterRaceCheckpoint(EventArgs e)
        {
            if(playerRace != null)
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

        [Command("test")]
        private void TestCommand()
        {
            if (PathExtractor.pathPoints != null)
            {
                this.Position = PathExtractor.pathPoints[0] + new Vector3(0.0f, 0.0f, 30.0f);
            }
        }

        [Command("test2")]
        private void Test2Command()
        {
            if (PathExtractor.pathPoints != null)
            {
                this.Notificate("Processing ...");
                int idx = 0;
                foreach (PathExtractor.PathNode node in PathExtractor.pathNodes[0].FindAll(x => x.position.DistanceTo(this.Position) < 200.0))
                {
                    Vector3 point = node.position;
                    Console.WriteLine("PathNode " + idx + " position: " + point + ", distance to player: " + point.DistanceTo(this.Position));
                    if (!pathLabels.Exists(x => x.Position.X == point.X && x.Position.Y == point.Y))
                    {
                        TextLabel lbl = new TextLabel("PathNode: " + node.nodeID + "\n" + point.ToString(), Color.White, point, 200.0f);
                        pathLabels.Add(lbl);
                        Console.WriteLine("Label n° " + lbl.Id + " created: " + point);
                    }
                    idx++;
                }
                idx = 0;
                foreach (PathExtractor.NaviNode node in PathExtractor.naviNodes[0].FindAll(x => x.position.DistanceTo(new Vector2(this.Position.X, this.Position.Y)) < 200.0))
                {
                    Vector2 point = node.position;
                    Console.WriteLine("Point " + idx + " position: " + point + ", distance to player: " + point.DistanceTo(new Vector2(this.Position.X, this.Position.Y)));
                    if (!pathLabels2.Exists(x => x.Position.X == point.X && x.Position.Y == point.Y))
                    {
                        TextLabel lbl = new TextLabel("NaviNode: " + node.nodeID + "\n" + point.ToString(), Color.White, new Vector3(point.X, point.Y, 20.0f), 200.0f);
                        pathLabels2.Add(lbl);
                        Console.WriteLine("Label n° " + lbl.Id + " created: " + point);
                    }
                    idx++;
                }
                foreach (TextLabel lbl in pathLabels.FindAll(x => x.Position.DistanceTo(this.Position) > 200.0))
                {
                    Console.WriteLine("Label n° " + lbl.Id + " removed");
                    pathLabels.Remove(lbl);
                    lbl.Dispose();
                }
                foreach (TextLabel lbl in pathLabels2.FindAll(x => x.Position.DistanceTo(this.Position) > 200.0))
                {
                    Console.WriteLine("Label n° " + lbl.Id + " removed");
                    pathLabels2.Remove(lbl);
                    lbl.Dispose();
                }

                this.Notificate("Done !");
            }
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