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

using SampSharpGameMode1.Events.Races;

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
        public RaceCreator playerRaceCreator;

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