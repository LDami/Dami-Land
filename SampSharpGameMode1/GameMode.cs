using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using SampSharp.Core.Natives;
using SampSharp.GameMode;
using SampSharp.GameMode.Controllers;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Civilisation;
using SampSharpGameMode1.CustomDatas;
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
        protected override void LoadControllers(ControllerCollection controllers)
        {
            // Load the default controllers first
            base.LoadControllers(controllers);

            controllers.Add(new NPCController());
        }

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

            /* Loading parked vehicles */
            logger.Write("GameMode.cs - GameMode.OnInitialized:I: Loading parked vehicles ... ");
            try
            {
                Random rdm = new();
                mySQLConnector.OpenReader("SELECT * FROM parked_vehicles", new Dictionary<string, object>());
                Dictionary<string, string> row = mySQLConnector.GetNextRow();
                while (row.Count > 0)
                {
                    BaseVehicle v = StoredVehicle.CreateStatic((VehicleModelType)Convert.ToInt32(row["model_id"]), new Vector3(
                        (float)Convert.ToDouble(row["spawn_pos_x"]),
                        (float)Convert.ToDouble(row["spawn_pos_y"]),
                        (float)Convert.ToDouble(row["spawn_pos_z"])), (float)Convert.ToDouble(row["spawn_rot"]),
                        row["color1"] != "[null]" ? Convert.ToInt16(row["color1"]) : rdm.Next(255),
                        row["color2"] != "[null]" ? Convert.ToInt16(row["color2"]) : rdm.Next(255)
                    );
                    v.GetColor(out int c1, out int c2);
                    StoredVehicle.AddDbPool(v.Id, Convert.ToInt32(row["vehicle_id"]));
                    row = mySQLConnector.GetNextRow();
                }
                mySQLConnector.CloseReader();
                logger.WriteLine("Done !");
                logger.WriteLine($"GameMode.cs - GameMode.OnInitialized:I: {StoredVehicle.GetPoolSize()} vehicles loaded.");
            }
            catch(Exception ex)
            {
                logger.WriteLine("Error !");
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
            SampSharp.GameMode.SAMP.Timer t = new(60000 * 3, true);
            string[] randomMessageList =
            {
                $"Try {ColorPalette.Secondary.Main}/event-infos {ColorPalette.Primary.Main}to create events !",
                $"Type {ColorPalette.Secondary.Main}/help {ColorPalette.Primary.Main}for help !",
                $"Use {ColorPalette.Secondary.Main}/v {ColorPalette.Primary.Main}to spawn a vehicle !",
                $"Please report any issue on the Github project, or in the discord channel !",
                $"Join the discord server now ! {ColorPalette.Secondary.Main}https://discord.gg/82fdEvJ96U",
                $"You can do some truck or tram missions to earn some cash !",
                $"You can save your current position with {ColorPalette.Secondary.Main}/s {ColorPalette.Primary.Main}and go back to it later with {ColorPalette.Secondary.Main}/r",
            };
            t.Tick += (sender, e) =>
            {
                Random rdm = new();
                BasePlayer.SendClientMessageToAll(ColorPalette.Primary.Main + "Tip: " + randomMessageList[rdm.Next(randomMessageList.Length)]);
            };
            ExtractMapObjects();
            logger.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Gamemode ready !");

            logger.Close();

            /*
            Console.WriteLine("GameMode.cs - GameMode.OnInitialized:I: Connecting to socket ... ");
            socket = new MySocketIO("192.168.1.38", 5555);
            socket.Connect();
            */
        }

        protected override void OnExited(EventArgs e)
        {
            base.OnExited(e);
        }

#endregion

        public static void ExtractMapObjects()
        {
            // Chemin vers votre fichier contenant les données
            string filePath = "C:\\Serveur OpenMP\\scriptfiles\\mapobjects.txt";

            // Liste pour stocker les objets de la carte
            List<MapObjectData> mapObjects = new();

            // Lire les lignes du fichier
            string[] lines = System.IO.File.ReadAllLines(filePath);

            // Variable temporaire pour stocker la catégorie en cours de traitement
            MapObjectGroupData currentCategory = new("A51 Replacement Land Bit", "");
            string comment = "";

            foreach (string line in lines)
            {
                // Vérifier si la ligne est vide ou si elle commence par "###" pour identifier une nouvelle catégorie
                if (!string.IsNullOrWhiteSpace(line))
                {
                    if (line.StartsWith("###"))
                    {
                        // Si c'est une nouvelle catégorie, mettez à jour la variable de catégorie
                        currentCategory = new(line.Replace("###", "").Replace("*", "").Trim(), comment);
                        comment = "";
                    }
                    else
                    {
                        string[] parts = line.Split('\t');
                        if (int.TryParse(parts[0], out int id))
                        {
                            string name = parts[1];
                            // Ajouter l'objet de la carte à la liste
                            mapObjects.Add(new MapObjectData(id, name, currentCategory));
                        }
                        else
                            comment += " " + line;
                    }
                }
            }
            Console.WriteLine("GameMode.ExtractMapObjects:I: Got " + mapObjects.Count + " map objects");
            MapObjectData.UpdateMapObject(mapObjects);
        }
    }
}