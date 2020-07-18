﻿using System;
using SampSharp.GameMode;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Events.Races;

namespace SampSharpGameMode1
{
    public class GameMode : BaseMode
    {
        #region Overrides of BaseMode

        public static MySQLConnector mySQLConnector = null;
        public static RaceLauncher raceLauncher = null;
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Console.WriteLine("\n----------------------------------");
            Console.WriteLine(" LDami's gamemode");
            Console.WriteLine("----------------------------------\n");

            this.AddPlayerClass(1, 1, new Vector3(1431.6393, 1519.5398, 10.5988), 0.0f);

            Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Connecting to MySQL Server ...");
            mySQLConnector = MySQLConnector.Instance();
            Boolean isConnected = false;
            while (!isConnected)
            {
                System.Threading.Thread.Sleep(1000);
                if (mySQLConnector.Connect())
                {
                    Console.WriteLine("Done");
                    Console.WriteLine("MySql State: " + mySQLConnector.GetState());
                    isConnected = true;
                }
            }
            raceLauncher = RaceLauncher.Instance();
            Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Gamemode ready !");
            //NPC npc = new NPC();
            //npc.Create();
            //Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: NPC Created !");

            //Civilisation.PathExtractor.Extract("E:\\Jeux\\GTA San Andreas\\data\\Paths\\NODES0.DAT");
            // TODO: Put logic to initialize your game mode here
        }

        protected override void OnExited(EventArgs e)
        {
            base.OnExited(e);
        }

        #endregion
    }
}