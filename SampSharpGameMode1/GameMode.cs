using System;
using SampSharp.GameMode;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Civilisation;
using SampSharpGameMode1.Events;

namespace SampSharpGameMode1
{
    public class GameMode : BaseMode
    {
        #region Overrides of BaseMode

        public static MySQLConnector mySQLConnector = null;
        public static EventManager eventManager = null;
        public MySocketIO socket = null;
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Console.WriteLine("\n----------------------------------");
            Console.WriteLine(" LDami's gamemode");
            Console.WriteLine("----------------------------------\n");

            this.AddPlayerClass(1, 1, new Vector3(1431.6393, 1519.5398, 10.5988), 0.0f);

            Logger.Init();

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
            eventManager = EventManager.Instance();
            //NPC npc = new NPC();
            //npc.Create();
            //Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: NPC Created !");
            
            Civilisation.PathExtractor.Load();
            //Civilisation.PathExtractor.Extract("E:\\Jeux\\GTA San Andreas\\data\\Paths", 54);

            for (int i = 0; i < 64; i++)
            {
                Civilisation.PathExtractor.Extract("E:\\Jeux\\GTA San Andreas\\data\\Paths", i);
            }
            for (int i = 0; i < 64; i++)
            {
                Civilisation.PathExtractor.CheckLinks(i);
            }
            for (int i = 0; i < 64; i++)
            {
                Civilisation.PathExtractor.SeparateNodes(i);
            }
            //Civilisation.PathExtractor.SeparateNodes(16);
            //Civilisation.PathExtractor.SeparateNodes(54);
            //Civilisation.PathExtractor.CheckLinks(54);
            Civilisation.PathExtractor.ValidateNaviLink();
            
            Console.WriteLine("Total path points: " + PathExtractor.pathPoints.Count);
            
            Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Gamemode ready !");

            Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Connecting to socket ... ");
            socket = new MySocketIO("127.0.0.1", 5555);
            socket.Connect();
        }

        protected override void OnExited(EventArgs e)
        {
            base.OnExited(e);
        }

        #endregion
    }
}