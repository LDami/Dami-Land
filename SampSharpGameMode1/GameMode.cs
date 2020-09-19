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
            eventManager = EventManager.Instance();
            //NPC npc = new NPC();
            //npc.Create();
            //Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: NPC Created !");

            Civilisation.PathExtractor.Load();
            //Civilisation.PathExtractor.Extract("E:\\Jeux\\GTA San Andreas\\data\\Paths", 54);
            
            for (int i=0; i < 64; i++)
            {
                Civilisation.PathExtractor.Extract("E:\\Jeux\\GTA San Andreas\\data\\Paths", i);
            }
            
            Console.WriteLine("Total path points: " + PathExtractor.pathPoints.Count);
            /*
            if(PathExtractor.pathPoints != null)
            {
                GlobalObject[] globalObject = new GlobalObject[PathExtractor.pathPoints.Length];
                TextLabel[] textLabels = new TextLabel[PathExtractor.pathPoints.Length];
                int idx = 0;
                foreach(Vector3 point in PathExtractor.pathPoints)
                {
                    Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Creating object " + idx + "/" + PathExtractor.pathPoints.Length);
                    Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Position: " + point.ToString());
                    globalObject[idx] = new GlobalObject(18808, point + new Vector3(0.0f, 0.0f, 5.0f), Vector3.Zero);
                    textLabels[idx] = new TextLabel("N°" + idx + "\n" + point.ToString(), SampSharp.GameMode.SAMP.Color.White, point + new Vector3(0, 0, 20.0), 200.0f);
                    idx++;
                    //globalObject[idx++] = new GlobalObject(18876, point + new Vector3(0.0f, 0.0f, 5.0f), Vector3.Zero);
                }
            }
            */
            Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Gamemode ready !");
        }

        protected override void OnExited(EventArgs e)
        {
            base.OnExited(e);
        }

        #endregion
    }
}