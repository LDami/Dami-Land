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

using SampSharpGameMode1.Race;

namespace SampSharpGameMode1
{
    [PooledType]
    public class Player : BasePlayer
    {
        MySQLConnector mySQLConnector = null;

        Boolean isConnected;
        int passwordEntryTries = 3;

        TextdrawCreator textdrawCreator;
        PlayerMapping playerMapping;
        RaceCreator playerRaceCreator;

        NPC npc;

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

            if (this.IsRegistered())
                ShowLoginForm();
            else
                ShowSignupForm();
        }
        public override void OnDisconnected(DisconnectEventArgs e)
        {
            base.OnDisconnected(e);

            isConnected = false;

            mySQLConnector = null;
            playerMapping = null;
            playerRaceCreator = null;

            if(npc != null) npc.Dispose();
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
            this.SendClientMessage("You entered a vehicle");
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
                mySQLConnector.OpenReader("SELECT id FROM userlogin WHERE name=@name", param);
                Dictionary<string, string> results = mySQLConnector.GetNextRow();
                mySQLConnector.CloseReader();
                if (results.Count > 0)
                    return true;
                else
                    return false;
            }
            else
            {
                Console.WriteLine("Player.cs - Player.IsRegisterd: MySQL not started");
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
                    mySQLConnector.Execute("INSERT INTO userlogin (name, password, adminlvl) VALUES (@name, @password, 0)", param);
                    if (mySQLConnector.RowsAffected > 0)
                    {
                        this.Notificate("Registered");
                        isConnected = true;
                        this.Spawn();
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
                    mySQLConnector.OpenReader("SELECT password FROM userlogin WHERE name=@name", param);
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

        [Command("getmodel")]
        private void GetModel(string model)
        {
            SendClientMessage("Found model: " + Utils.GetVehicleModelType(model).ToString());
        }

        [Command("vehicle", "veh", "v", DisplayName = "v")]
        private void SpawnVehicleCommand(VehicleModelType model)
        {
            Random rndColor = new Random();
            BaseVehicle v = BaseVehicle.Create(model, new Vector3(this.Position.X + 5.0, this.Position.Y, this.Position.Z), 0.0f, rndColor.Next(0, 255), rndColor.Next(0, 255));
            this.PutInVehicle(v, 0);
        }


        [Command("tp")]
        private void TP()
        {
            this.Position = new Vector3(1431.6393, 1519.5398, 10.5988);
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
            [CommandGroup("race")]
            class RaceCommandsClass
            {
                // Creator

                [Command("create")]
                private static void CreateRace(Player player)
                {
                    player.playerRaceCreator = new RaceCreator(player);
                }

                [Command("loadc")]
                private static void LoadRaceCreator(Player player, int id)
                {
                    if (player.playerRaceCreator == null)
                        player.playerRaceCreator = new RaceCreator(player);

                    if (player.playerRaceCreator.Load(id))
                        player.SendClientMessage(Color.Green, "Race #" + id + " loaded successfully in creation mode");
                    else
                        player.SendClientMessage(Color.Red, "Error loading race #" + id);
                }

                [Command("save")]
                private static void SaveRace(Player player)
                {
                    if (player.playerRaceCreator != null)
                    {
                        if (player.playerRaceCreator.isEditing)
                        {
                            if (player.playerRaceCreator.editingRace.Name.Length > 0) // Si on édite une course déjà existante
                            {
                                if (player.playerRaceCreator.Save())
                                    player.SendClientMessage(Color.Green, "Race saved");
                                else
                                    player.SendClientMessage(Color.Red, "Error saving race");
                            }
                            else
                            {
                                InputDialog raceName = new InputDialog("Name of the race", "Please enter the name of the race", false, "Create", "Cancel");
                                raceName.Show(player);
                                raceName.Response += RaceName_Response;
                            }
                        }
                        else
                            player.SendClientMessage("You must edit or create a race to use this command");
                    }
                    else
                        player.SendClientMessage("You must edit or create a race to use this command");
                }

                private static void RaceName_Response(object sender, DialogResponseEventArgs e)
                {
                    Player player = (Player)e.Player;
                    if (e.DialogButton != DialogButton.Right)
                    {
                        if (e.InputText.Length > 0)
                        {
                            if (player.playerRaceCreator.Save(e.InputText))
                                player.SendClientMessage(Color.Green, "Race saved");
                            else
                                player.SendClientMessage(Color.Red, "Error saving race");
                        }
                        else
                        {
                            InputDialog raceName = new InputDialog("Name of the race", "Please enter the name of the race", false, "Create", "Cancel");
                            raceName.Show(e.Player);
                            raceName.Response += RaceName_Response;
                        }
                    }
                }

                [Command("set start")]
                private static void SetStart(Player player)
                {
                    if (player.playerRaceCreator != null)
                    {
                        player.playerRaceCreator.PutStart(player.Position);
                    }
                }
                [Command("set current")]
                private static void MoveCurrent(Player player)
                {
                    if (player.playerRaceCreator != null)
                    {
                        player.playerRaceCreator.MoveCurrent(player.Position);
                    }
                }
                [Command("set finish")]
                private static void SetFinish(Player player)
                {
                    if (player.playerRaceCreator != null)
                    {
                        player.playerRaceCreator.PutFinish(player.Position);
                    }
                }
                [Command("addcp")]
                private static void AddCP(Player player)
                {
                    if (player.playerRaceCreator != null)
                    {
                        player.playerRaceCreator.AddCheckpoint(player.Position);
                    }
                }

                [Command("find")]
                private static void FindRace(Player player, string name)
                {
                    Dictionary<string, string> result = RaceCreator.FindRace(name);
                    if (result.Count == 0)
                        player.SendClientMessage("No race found !");
                    else
                    {
                        foreach (KeyValuePair<string, string> kvp in result)
                        {
                            player.SendClientMessage(string.Format("{0}: {1}", kvp.Key, kvp.Value));
                        }
                    }
                }

                [Command("info")]
                private static void GetInfo(Player player, int id)
                {
                    Dictionary<string, string> result = RaceCreator.GetRaceInfo(id);
                    if (result.Count == 0)
                        player.SendClientMessage("No race found !");
                    else
                    {
                        var infoList = new ListDialog("Race info", "Ok", "");
                        string str = "";
                        foreach (KeyValuePair<string, string> kvp in result)
                        {
                            str = new Color(50, 50, 255) + kvp.Key + ": " + new Color(255, 255, 255) + kvp.Value;
                            if (str.Length >= 64)
                            {
                                infoList.AddItem(str.Substring(0, 63));
                                infoList.AddItem(str.Substring(63));
                            }
                            else
                                infoList.AddItem(str);
                        }
                        infoList.Show(player);
                    }
                }

                // Launcher

                [Command("join")]
                private static void Join(Player player)
                {
                    if (GameMode.raceLauncher.Join(player))
                    {
                        player.SendClientMessage(Color.Green, "Vous avez rejoint la course");
                    }
                    else
                        player.SendClientMessage(Color.Red, "Vous n'avez pas pu rejoindre la course");
                }

                [Command("load")]
                private static void LoadRace(Player player, int id)
                {
                    if (GameMode.raceLauncher.Load(id))
                        player.SendClientMessage(Color.Green, "Race #" + id + " loaded successfully");
                    else
                        player.SendClientMessage(Color.Red, "Error loading race #" + id);
                }

                [Command("launchnext")]
                private static void LaunchNextRace(Player player)
                {
                    GameMode.raceLauncher.LaunchNext();
                    player.SendClientMessage(Color.Green, "Race launched, waiting for players !");
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