using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using SampSharp.Core.Natives;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Civilisation;
using SampSharpGameMode1.Events;
using static SampSharp.GameMode.SAMP.Server;
using static SampSharpGameMode1.Civilisation.PathExtractor;

namespace SampSharpGameMode1
{
    public class GameMode : BaseMode
    {
        public static MySQLConnector mySQLConnector = null;
        public static EventManager eventManager = null;
        public MySocketIO socket = null;
        #region Overrides of BaseMode

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Console.WriteLine("\n----------------------------------");
            Console.WriteLine(" LDami's gamemode");
#if DEBUG
            Console.WriteLine(" Mode: DEBUG");
#else
            Console.WriteLine(" Mode: Production");
#endif
            Console.WriteLine("----------------------------------\n");

            SetGameModeText("Free/Race/Derby");

            this.AddPlayerClass(1, 0, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);
            this.AddPlayerClass(1, 1, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);
            this.AddPlayerClass(1, 2, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);
            this.AddPlayerClass(1, 7, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);
            this.AddPlayerClass(1, 8, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);
            this.AddPlayerClass(1, 12, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);
            this.AddPlayerClass(1, 13, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);
            this.AddPlayerClass(1, 17, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);
            this.AddPlayerClass(1, 19, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);
            this.AddPlayerClass(1, 21, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);
            this.AddPlayerClass(1, 15, new Vector3(2486.5537, 1531.3606, 10.8191), 316.3417f);

            Logger.Init();

            Logger.WriteLineAndClose("GameMode.cs - GameMode.OnInitialized:I: Connecting to MySQL Server ... ");
            mySQLConnector = MySQLConnector.Instance();
            Boolean isConnected = false;
            while (!isConnected)
            {
                Thread.Sleep(1000);
                if (mySQLConnector.Connect())
                {
                    isConnected = true;
                }
            }
            eventManager = EventManager.Instance();

            
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Civilisation.PathExtractor.Load();
            sw.Stop();
            Logger.WriteLineAndClose($"GameMode.cs - GameMode.OnInitialized:I: PathExtractor.Load => {sw.ElapsedMilliseconds} ms");
            sw.Restart();
            //Civilisation.PathExtractor.Extract("E:\\Jeux\\GTA San Andreas\\data\\Paths", 54);
            for (int i = 0; i < 64; i++)
            {
                Civilisation.PathExtractor.Extract(ConfigurationManager.AppSettings["gta_basefolder"] + "/data/Paths", i);
            }
            sw.Stop();
            Logger.WriteLineAndClose($"GameMode.cs - GameMode.OnInitialized:I: PathExtractor.Extract => {sw.ElapsedMilliseconds} ms");
            sw.Restart();
            for (int i = 0; i < 64; i++)
            {
                Civilisation.PathExtractor.CheckLinks(i);
            }
            sw.Stop();
            Logger.WriteLineAndClose($"GameMode.cs - GameMode.OnInitialized:I: PathExtractor.CheckLinks => {sw.ElapsedMilliseconds} ms");
            sw.Restart();
            for (int i = 0; i < 64; i++)
            {
                Civilisation.PathExtractor.SeparateNodes(i);
            }
            sw.Stop();
            Logger.WriteLineAndClose($"GameMode.cs - GameMode.OnInitialized:I: PathExtractor.SeparateNodes => {sw.ElapsedMilliseconds} ms");
            sw.Restart();
            //Civilisation.PathExtractor.SeparateNodes(16);
            //Civilisation.PathExtractor.SeparateNodes(54);
            //Civilisation.PathExtractor.CheckLinks(54);
            Civilisation.PathExtractor.ValidateNaviLink();
            sw.Stop();
            Logger.WriteLineAndClose($"GameMode.cs - GameMode.OnInitialized:I: PathExtractor.ValidateNaviLink => {sw.ElapsedMilliseconds} ms");

            Logger logger = new Logger();
            logger.WriteLine($"GameMode.cs - GameMode.OnInitialized:I: Total path points: {PathExtractor.pathPoints.Count}");
            
            logger.Write("GameMode.cs - GameMode.OnInitialized:I: Initializing ColAndreas ... ");
            Physics.ColAndreas.Init();
            logger.WriteLine("Done !");

            logger.Write($"GameMode.cs - GameMode.OnInitialized:I: Loading Record file ...");
            sw.Restart();
            Record record = RecordConverter.Parse(@"C:\stayinvehicle.rec");
            sw.Stop();
            logger.WriteLine($"Done in {sw.ElapsedMilliseconds} ms");

            for (int i = 0; i < record.VehicleBlocks.Count; i++)
            {
                RecordInfo.VehicleBlock block = record.VehicleBlocks[i];
                block.velocity = Vector3.Forward;
                block.position = new Vector3(block.position.X, block.position.Y - (10 * i), block.position.Z);
                record.VehicleBlocks[i] = block;
            }

            logger.Write($"GameMode.cs - GameMode.OnInitialized:I: Recreating Record file ...");
            sw.Restart();
            RecordCreator.Save(record, @"recreated.rec");
            sw.Stop();
            logger.WriteLine($"Done in {sw.ElapsedMilliseconds} ms");
            RecordConverter.Parse(@"C:\Serveur OpenMP\npcmodes\recordings\recreated.rec");

            /* Loading parked vehicles */
            logger.Write("GameMode.cs - GameMode.OnInitialized:I: Loading parked vehicles ... ");
            try
            {
                mySQLConnector.OpenReader("SELECT * FROM parked_vehicles", new Dictionary<string, object>());
                Dictionary<string, string> row = mySQLConnector.GetNextRow();
                while (row.Count > 0)
                {
                    BaseVehicle v = StoredVehicle.CreateStatic((VehicleModelType)Convert.ToInt32(row["model_id"]), new Vector3(
                        (float)Convert.ToDouble(row["spawn_pos_x"]),
                        (float)Convert.ToDouble(row["spawn_pos_y"]),
                        (float)Convert.ToDouble(row["spawn_pos_z"])), (float)Convert.ToDouble(row["spawn_rot"]), 0, 0);
                    StoredVehicle.AddDbPool(v.Id, Convert.ToInt32(row["vehicle_id"]));
                    row = mySQLConnector.GetNextRow();
                }
                mySQLConnector.CloseReader();
                logger.WriteLine("Done !");
                logger.WriteLine($"GameMode.cs - GameMode.OnInitialized:I: {StoredVehicle.GetPoolSize()} vehicles loaded.");
            }
            catch(Exception ex)
            {
                logger.Write("Error !");
                logger.WriteLine("GameMode.cs - GameMode.OnInitialized:E: Error trying to load vehicles: " + ex.Message);
            }

            Zone.InitZones();

            logger.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Initializing works ...");
            Works.TruckWork.Init();
            logger.WriteLine("GameMode.cs - GameMode.OnInitialized:I: - Truck work initialized");
            Works.TramWork.Init();
            logger.WriteLine("GameMode.cs - GameMode.OnInitialized:I: - Tram work initialized");
            logger.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Works initialized.");

            DisableInteriorEnterExits();
            logger.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Gamemode ready !");

            logger.Close();

            /*
            Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Connecting to socket ... ");
            socket = new MySocketIO("127.0.0.1", 5555);
            socket.Connect();
            */
            Vector3 start = new Vector3(-2615.5942, 2307.6628, 7.7573);
            //Vector3 start = new Vector3(-1574.7374, 2671.0313, 55.6593); // pos1
            //Vector3 end = new Vector3(-2672.7515, 2461.6265, 41.8708); // Long
            //Vector3 end = new Vector3(-2520.9419, 2409.3198, 17.1795); // Short
            //Vector3 end = new Vector3(-1574.7374, 2671.0313, 55.6593); // pos1
            Vector3 end = new Vector3(-1646.4111, 2490.5620, 86.0364); // chemin
            //Vector3 end = new Vector3(-1714.8938, 2524.8657, 102.2524); // offroad
            //this.CalculateWay(start, end);
        }

        protected override void OnExited(EventArgs e)
        {
            base.OnExited(e);
        }

#endregion
        DateTime startedTime = DateTime.Now;
        private void CalculateWay(Vector3 from, Vector3 to)
        {
            PathNode startNode, endNode;
            List<PathNode> allPathNodes = GetPathNodes();
            List<PathNode> allNearPathNodes = new List<PathNode>();

            PathNode nearestNodeFrom = new PathNode();
            PathNode nearestNodeTo = new PathNode();
            PathNode lastNode = new PathNode();


            GameMode gm = this;
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
                if (node.position.DistanceTo(from) < from.DistanceTo(to) || node.position.DistanceTo(to) < from.DistanceTo(to))
                {
                    allNearPathNodes.Add(node);
                    data = "{ \"id\": \"" + node.id + "\", \"posX\": " + node.position.X + ", \"posY\": " + node.position.Y + ", \"links\": [";
                    int idx = 1;
                    foreach (LinkInfo link in node.links)
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
            if (isSocketAlive)
                Console.WriteLine("Done");
            else
                Console.WriteLine("KO");

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
            PathFinder pf = new PathFinder(allNearPathNodes, startNode, endNode);

            startedTime = DateTime.Now;
            pf.Find();
            pf.Success += Pf_Success;
            pf.Failure += Pf_Failure;

        }

        private void Pf_Failure(object sender, EventArgs e)
        {
            Console.WriteLine("Failure");
        }

        private void Pf_Success(object sender, PathFindingDoneEventArgs e)
        {
            Console.WriteLine("Success");
            TimeSpan duration = DateTime.Now - startedTime;
            Console.WriteLine("Path found in " + duration.ToString());
        }
    }
}