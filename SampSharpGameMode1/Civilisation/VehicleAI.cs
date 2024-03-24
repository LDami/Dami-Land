//using SampSharp.Core.Natives;
using SampSharp.Core.Natives;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml.Linq;
using static SampSharpGameMode1.Civilisation.PathExtractor;

namespace SampSharpGameMode1.Civilisation
{
    class VehicleAI
    {
        const float VEH_ACCEL = 1.1f;
        const float VEH_VELOCITY = 45.1f; // 45.1 meters per second
        enum Status
        {
            WaitingDestination,
            Driving,
            Blocked
        }
        public static BasePlayer npc;
        public static BaseVehicle vehicle;
        static Status status;
        static Vector3 destination;
        static Queue<PathNode> path;
        static List<DynamicObject> pathObjects = new List<DynamicObject>();
        static List<TextLabel> pathLabels = new List<TextLabel>();
        static List<DynamicObject> recordPathObjects = new List<DynamicObject>();
        static TextLabel textLabel;
        static bool stopProcess = false;
        static bool processEnded = false;

        static double maxSpeed;
        static int mode;

        static double vehicleSpeed = 0.05;
        static double vehicleAccel = 1.5; // + 1.5 velocity per second
        static int timeMultiplier = 20;
        static TextLabel lbl, lbl2, lblStatus;
        public static string Init(VehicleModelType vehicleModel, Vector3 position, float angle)
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI:Init called with position = " + position);
            processEnded = true;
            vehicle = BaseVehicle.Create(vehicleModel, position + Vector3.UnitZ, angle, 0, 0);
            Console.WriteLine("VehicleAI.cs - VehicleAI:Init vehicle position = " + vehicle.Position);

            string npcName = "stayinvehicle" + BasePlayer.PoolSize;
            Server.ConnectNPC(npcName, "npctest2");
            status = Status.WaitingDestination;
            return npcName;
        }

        public static void Restart()
        {
            Player.SendClientMessageToAll("BEGIN restart");

            if(!npc.IsDisposed)
            {
                npc.SendClientMessage("NPC:STOP");
                Thread.Sleep(10);
                npc.SendClientMessage("NPC:START");
            }
            //BasePlayer.Find(0).PutInVehicle(vehicle, 1);
            Player.SendClientMessageToAll("END restart");
        }

        public static void SetInVehicle()
        {
            npc.RemoveFromVehicle();
            Thread.Sleep(1000);
            npc.PutInVehicle(vehicle);
        }

        public void SetNPCHealth(int health)
        {
            npc.Health = health;
        }

        public static void Kick()
        {
            stopProcess = true;
            Console.Write("VehicleAI.cs - VehicleAI.Kick:I: Waiting process thread to end ...");
            while (!processEnded)
            {
                Thread.Sleep(100);
            }
            Console.WriteLine("Done !");
            npc.Kick();
            npc.Dispose();
            vehicle.Dispose();
            Console.WriteLine("VehicleAI.cs - VehicleAI.Kick:I: Vehicle AI kicked from the server !");
        }

        public static void SetDestination(Vector3 _destination, double _maxSpeed)
        {
            destination = _destination;
            maxSpeed = _maxSpeed;
            Console.WriteLine("VehicleAI.cs - VehicleAI.SetDestination:I: Destination set to " + _destination.ToString());
            Console.WriteLine("VehicleAI.cs - VehicleAI.SetDestination:I: Max speed set to " + maxSpeed.ToString());
            textLabel = new TextLabel("Destination", Color.White, _destination + Vector3.UnitZ, 200f);
            textLabel.VirtualWorld = vehicle.VirtualWorld;
        }

        public static void SetPath(PathNode[] nodes)
        {
            path = new Queue<PathNode>(nodes);
            vehicle.Position = nodes[0].position;
            vehicle.Angle = CalculateRotationAngle(nodes[0].position.XY, nodes[1].position.XY, true);
            Console.WriteLine("vehicle angle: " + vehicle.Angle);
            path.Dequeue();
            pathObjects = new List<DynamicObject>();
            pathLabels = new List<TextLabel>();
            for (int i = 0; i < nodes.Length; i++)
            {
                Console.WriteLine($"node {i}: {nodes[i].position}");
                if(i < nodes.Length - 1)
                {
                    DynamicObject dynamicObject = new DynamicObject(19130, nodes[i].position + Vector3.UnitZ, Vector3.Zero);

                    double angledegree = CalculateRotationAngle(nodes[i].position.XY, nodes[i + 1].position.XY, true);

                    dynamicObject.Rotation = new Vector3(90.0, 0.0, angledegree);
                    pathObjects.Add(dynamicObject);

                    string txt = nodes[i].position.ToString() + "\n" + angledegree;
                    TextLabel lbl = new TextLabel(txt, Color.White, nodes[i].position + new Vector3(0.0, 0.0, 2.0), 200.0f);
                    pathLabels.Add(lbl);
                }
            }
            
        }

        public static void SetSpeed(double speed)
        {
            vehicleSpeed = speed;
            Player.SendClientMessageToAll("Speed set to " + vehicleSpeed);
        }

        public static void SetTimeMultiplier(int _timeMultiplier)
        {
            timeMultiplier = _timeMultiplier;
            Player.SendClientMessageToAll("Time Multiplier set to " + timeMultiplier);
        }
        
        /**
         * 0 = auto, 1 = step by step
         */
        public static void SetMode(int _mode)
        {
            mode = _mode;
        }

        public static void Process()
        {
            lbl?.Dispose();
            lbl2?.Dispose();

            lbl = new TextLabel("AI", Color.White, vehicle.Position + new Vector3(0.0, 0.0, 1.0), 200.0f);
            lbl.AttachTo(vehicle, new Vector3(0.0, 0.0, 1.0));
            lbl2 = new TextLabel("AI", Color.White, vehicle.Position + new Vector3(-5.0, 0.0, 1.0), 200.0f);
            lbl2.AttachTo(npc, new Vector3(0.0, 0.0, 1.0));
            lblStatus = new TextLabel("Status: N/A", Color.White, vehicle.Position + new Vector3(-10.0, 0.0, 1.0), 200.0f);
            lblStatus.AttachTo(npc, new Vector3(0.0, 0.0, 0.5));

            stopProcess = false;
            processEnded = false;
            Thread t = new Thread(new ThreadStart(() =>
            {
                Player.SendClientMessageToAll("BEGIN Process thread");
                bool playerNearNPC;
                while(!stopProcess)
                {
                    //Console.WriteLine("VehicleAI.cs - VehicleAI.Process:I: npc is in vehicle: " + npc.InAnyVehicle);
                    //if (!npc.InAnyVehicle)
                    //    npc.PutInVehicle(vehicle);
                    playerNearNPC = false;
                    /*
                    foreach (BasePlayer p in BasePlayer.All)
                    {
                        if(!npc.IsDisposed && p.Name != npc.Name)
                        {
                            try
                            {
                                if (npc.IsInRangeOfPoint(10.0f, p.Position))
                                {
                                    playerNearNPC = true;
                                }
                            }
                            catch (ObjectDisposedException)
                            {
                                Console.WriteLine("VehicleAI.cs - VehicleAI.Process:E: npc is disposed");
                            }
                        }
                    }
                    */
                    if (playerNearNPC)
                    {
                        //Console.WriteLine("VehicleAI.cs - VehicleAI.Process:I: playerNearNPC = true");
                        if (status == Status.Driving)
                        {
                            Console.WriteLine("VehicleAI.cs - VehicleAI.Process:I: Obstacle: Stopping vehicle");
                            StopVehicle();
                        }
                    }
                    else
                    {
                        //Console.WriteLine("VehicleAI.cs - VehicleAI.Process:I: playerNearNPC = false");
                        if (status != Status.Driving)
                        {
                            //SetDestination(path.Dequeue().position, 0.2);
                            if(mode == 0)
                                StartVehicle();
                        }
                    }
                    
                    if (status == Status.WaitingDestination)
                    {
                        //SetDestination(path.Dequeue().position, 0.2);
                    }
                    else if(status == Status.Driving)
                    {
                        if(vehicle.Position.DistanceTo(destination) < 3.0)
                        {
                            Console.WriteLine("VehicleAI.cs - VehicleAI.Process:I: Destination reached: Stopping vehicle");
                            stopProcess = true;
                            //StopVehicle();
                            //status = Status.WaitingDestination;
                        }
                    }
                    
                    lbl.Text = "Speed: " + Math.Sqrt(vehicle.Velocity.LengthSquared).ToString(@"N3") + "\nSpeed kmh: " + (Math.Sqrt(vehicle.Velocity.LengthSquared)*181.5).ToString(@"N3");
                    lbl2.Text = "Health: " + npc.Health.ToString();
                    lblStatus.Text = "Status: " + status.ToString() + " stopProcess: " + stopProcess;
                    Thread.Sleep(1000);
                }
                processEnded = true;
                // Do not do action on server here (like update Players, Vehicle, TextLabels, ..)
            }));
            t.Start();
        }

        public static void StartVehicle()
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI.StartVehicle:I: Starting vehicle");
            Player.SendClientMessageToAll("Starting vehicle !");
            status = Status.Driving;

            Record record = RecordConverter.Parse(@"C:\stayinvehicle.rec");

            recordPathObjects = new List<DynamicObject>();

            uint deltaTime = 0;
            Vector3 estimatedVehiclePos = vehicle.Position; // Le dernier Vector3 to devient le prochain Vector3 from
            double prevNodeDistance = 0;
            Console.WriteLine("vehicle.Position = " + vehicle.Position.ToString());
            if(mode == 0)
            {
                vehicleSpeed = 0.02;
                for (int i = 0; i < path.Count; i++)
                {
                    Console.WriteLine("");
                    PathNode node = path.Dequeue();

                    if (record.Blocks[i] is RecordInfo.VehicleBlock block)
                    {
                        Quaternion quat = Quaternion.Identity;
                        Vector3 from = new Vector3(estimatedVehiclePos.X, estimatedVehiclePos.Y, estimatedVehiclePos.Z);
                        Vector3 to = new Vector3(node.position.X, node.position.Y, FindZFromVector2(node.position.X, node.position.Y));
                        if (node.position != Vector3.Zero)
                        {
                            Console.WriteLine("from: " + from);
                            Console.WriteLine("to: " + to);

                            float epsilon = 0.0001f; // Ajustez cela en fonction de la tolérance appropriée pour votre cas
                            if (Vector3.DistanceSquared(from, to) < epsilon * epsilon)
                            {
                                // Points très proches, renvoyer un quaternion d'identité (pas de rotation)
                                Console.WriteLine("!! Points trop proche, renvoi d'un Quaternion Identity");
                                quat = Quaternion.Identity;
                            }
                            else
                            {
                                // Création du quaternion
                                quat = LookRotationToPoint(from, to);
                                Vector3 vec3 = ToEuler(quat.W, quat.X, quat.Y, quat.Z).Normalized();
                                block.velocity = new Vector3(vec3.X * vehicleSpeed, vec3.Y * vehicleSpeed, vec3.Z * vehicleSpeed);
                            }

                            Console.WriteLine($"Quaternion: x = {quat.X} ; y = {quat.Y} ; z = {quat.Z} ; w = {quat.W}");
                            quat = Quaternion.Normalize(quat);
                        }


                        if (i > 0)
                            block.time = record.Blocks[i - 1].time + (uint)(prevNodeDistance / vehicleSpeed) * 5;
                        else
                            block.time = 0;

                        // For 500ms steps:
                        /*
                        Player velocity: (0, 01735576, -0, 0019036975, -2, 0593627E-05); Distance ran in 500ms: 0,052331124
                        Player velocity: (0, 072712064, -0, 015453491, -3, 840732E-05); Distance ran in 500ms: 1,4043567
                        Player velocity: (0, 117630996, -0, 023287239, -6, 19408E-05); Distance ran in 500ms: 2,561746
                        Player velocity: (0, 15140219, -0, 015345437, -0, 0002563792); Distance ran in 500ms: 3,6772563
                        Player velocity: (0, 18521197, -0, 01883218, 1, 2021027E-05); Distance ran in 500ms: 4,6937623
                        Player velocity: (0, 21316706, -0, 021677177, 0, 00010614364); Distance ran in 500ms: 5,367864
                        Player velocity: (0, 22709104, -0, 023096712, -0, 0004229768); Distance ran in 500ms: 5,6239614
                        Player velocity: (0, 24148698, -0, 024681868, -9, 976953E-06); Distance ran in 500ms: 6,2685437
                        Player velocity: (0, 25550264, -0, 026032628, 4, 0788427E-07); Distance ran in 500ms: 6,645635
                        Player velocity: (0, 25935885, -0, 026399791, 1, 7690545E-06); Distance ran in 500ms: 7,313462
                        */

                        // Mower acceleration: 12.0 (0.1 to 10.0)
                        // En 5s: 0 à 7.3

                        //block.position = new Vector3(lastPos.X + (moveX * deltaTime * 0.5 * VEH_VELOCITY), lastPos.Y + (moveY * deltaTime * 0.5 * VEH_VELOCITY), destination.Z + 1);
                        block.position = from;
                        Console.WriteLine($"block.position[{i}] = {block.position} ; time = {block.time} ; vel = {block.velocity} ; d = {Vector3.Distance(from, to)}");
                        recordPathObjects.Add(new DynamicObject(19131, block.position + new Vector3(0, 0, 2), Vector3.Zero, 200));
                        //Console.WriteLine($"block[{i}].position = {block.position} ; block[{i}].velocity: = {block.velocity}");
                        block.rotQuaternion1 = quat.W;
                        block.rotQuaternion2 = quat.X;
                        block.rotQuaternion3 = quat.Y;
                        block.rotQuaternion4 = quat.Z;
                        block.additionnalKeyCode = 8;
                        if (i >= path.Count - 1)
                            block.additionnalKeyCode = 0;
                        block.vehicleHealth = 1000;
                        record.Blocks[i] = block;

                        vehicleSpeed *= 1.2;
                        Console.WriteLine($"VehicleSpeed for block[{i}]: {vehicleSpeed}");
                        estimatedVehiclePos = to; // The vehicle should be at 'to' position at the end of the block
                        prevNodeDistance = Vector3.Distance(from , to);

                    }
                }
                record.Blocks = record.Blocks.GetRange(0, path.Count);
                Player.SendClientMessageToAll("Record duration = " + (record.Blocks[^1].time - record.Blocks[0].time).ToString() + "ms");
            }
            else if(mode == 1)
            {
                RecordInfo.VehicleBlock block = new RecordInfo.VehicleBlock();
                Console.WriteLine("");
                PathNode node = path.Dequeue();

                Quaternion quat = Quaternion.Identity;
                Vector3 from = new Vector3(estimatedVehiclePos.X, estimatedVehiclePos.Y, estimatedVehiclePos.Z);
                Vector3 to = new Vector3(node.position.X, node.position.Y, FindZFromVector2(node.position.X, node.position.Y));
                block.velocity = Vector3.Zero;
                if (node.position != Vector3.Zero)
                {
                    Console.WriteLine("from: " + from);
                    Console.WriteLine("to: " + to);

                    float epsilon = 0.0001f; // Ajustez cela en fonction de la tolérance appropriée pour votre cas
                    if (Vector3.DistanceSquared(from, to) < epsilon * epsilon)
                    {
                        // Points très proches, renvoyer un quaternion d'identité (pas de rotation)
                        Console.WriteLine("!! Points trop proche, renvoi d'un Quaternion Identity");
                        quat = Quaternion.Identity;
                    }
                    else
                    {
                        // Création du quaternion
                        quat = LookRotationToPoint(from, to);
                        Vector3 vec3 = ToEuler(quat.W, quat.X, quat.Y, quat.Z).Normalized();
                        block.velocity = new Vector3(vec3.X * vehicleSpeed, vec3.Y * vehicleSpeed, vec3.Z * vehicleSpeed);

                        estimatedVehiclePos = to;
                    }

                    Console.WriteLine($"Quaternion: x = {quat.X} ; y = {quat.Y} ; z = {quat.Z} ; w = {quat.W}");
                    quat = Quaternion.Normalize(quat);

                    Console.WriteLine($"Quaternion normalized = {quat.ToVector4()}");
                }
                block.time = 0;

                //block.position = new Vector3(lastPos.X + (moveX * deltaTime * 0.5 * VEH_VELOCITY), lastPos.Y + (moveY * deltaTime * 0.5 * VEH_VELOCITY), destination.Z + 1);
                block.position = from;
                Console.WriteLine($"block.position = {block.position} ; time = {block.time} ; dt (from last) = {deltaTime} ; vel = {block.velocity} ; d = {Vector3.Distance(estimatedVehiclePos, node.position)}");
                recordPathObjects.Add(new DynamicObject(19131, block.position + new Vector3(0, 0, 2), Vector3.Zero, 200));
                //Console.WriteLine($"block[{i}].position = {block.position} ; block[{i}].velocity: = {block.velocity}");
                block.rotQuaternion1 = quat.W;
                block.rotQuaternion2 = quat.X;
                block.rotQuaternion3 = quat.Y;
                block.rotQuaternion4 = quat.Z;
                block.additionnalKeyCode = 0;
                block.vehicleHealth = 1000;
                record.Blocks = new List<RecordInfo.Block>
                {
                    block
                };
            }

            RecordCreator.Save(record, @"recreated.rec");
            Player.SendClientMessageToAll("Record wrote, restarting NPC ...");

            // Parsing again to see new .json file

            //record = RecordConverter.Parse(@"C:\Serveur OpenMP\npcmodes\recordings\recreated.rec");

            Restart();
        }
        static float CalculateRotationAngle(Vector2 pointA, Vector2 pointB, bool isZAngle = false)
        {
            // Calculer la différence entre les coordonnées de B et A
            Vector2 direction = pointB - pointA;

            // Utiliser la fonction Atan2 pour calculer l'angle en radians
            double angleRadians = Math.Atan2(direction.Y, direction.X);

            // Convertir l'angle en degrés
            double angleDegrees = Math.Abs(angleRadians) * (180.0f / Math.PI);

            if (isZAngle)
                angleDegrees = 270f - angleDegrees;
            // Ajuster l'angle pour qu'il soit dans la plage [0, 360]
            angleDegrees = (angleDegrees + 360) % 360;

            return (float)angleDegrees;
        }

        public static Quaternion QuatFromEuler(float x, float y, float z)
        {
            // Convertir les angles d'Euler en radians
            float angleX = x * (float)(Math.PI / 180.0);
            float angleY = y * (float)(Math.PI / 180.0);
            float angleZ = z * (float)(Math.PI / 180.0);

            // Calculer les moitiés des angles
            float halfX = 0.5f * angleX;
            float halfY = 0.5f * angleY;
            float halfZ = 0.5f * angleZ;

            // Calculer les fonctions trigonométriques
            float cosHalfX = (float)Math.Cos(halfX);
            float cosHalfY = (float)Math.Cos(halfY);
            float cosHalfZ = (float)Math.Cos(halfZ);
            float sinHalfX = (float)Math.Sin(halfX);
            float sinHalfY = (float)Math.Sin(halfY);
            float sinHalfZ = (float)Math.Sin(halfZ);

            // Créer le quaternion
            Quaternion result = new Quaternion(
                sinHalfX * cosHalfY * cosHalfZ - cosHalfX * sinHalfY * sinHalfZ,
                cosHalfX * sinHalfY * cosHalfZ + sinHalfX * cosHalfY * sinHalfZ,
                cosHalfX * cosHalfY * sinHalfZ - sinHalfX * sinHalfY * cosHalfZ,
                cosHalfX * cosHalfY * cosHalfZ + sinHalfX * sinHalfY * sinHalfZ
            );

            return result;
        }

        static Quaternion LookRotationToPoint(Vector3 pointA, Vector3 pointB)
        {
            // Calculer la rotation autour de l'axe X
            float angleX = CalculateRotationAngle(new Vector2(pointA.Z, pointA.Y), new Vector2(pointB.Z, pointB.Y));
            angleX = 0;
            Quaternion rotationX = QuatFromEuler(angleX, 0, 0);

            // Calculer la rotation autour de l'axe Y
            float angleY = CalculateRotationAngle(new Vector2(pointA.X, pointA.Z), new Vector2(pointB.X, pointB.Z));
            Quaternion rotationY = QuatFromEuler(0, angleY, 0);

            // Calculer la rotation autour de l'axe Z (en supposant que Z est l'axe de hauteur)
            float angleZ = CalculateRotationAngle(new Vector2(pointA.X, pointA.Y), new Vector2(pointB.X, pointB.Y), true);
            Quaternion rotationZ = QuatFromEuler(0, 0, angleZ);

            Console.WriteLine($"LookRotationToPoint: x = {angleX}, y = {angleY}, z = {angleZ}, quatZ: {rotationZ.ToVector4()}");

            // Combiner les rotations pour obtenir le quaternion final
            Quaternion finalRotation = rotationX * rotationY * rotationZ;
            finalRotation = FromEuler(angleX, angleY, angleZ);
            return finalRotation;
        }

        public static Vector3 NormalizeInScale(Vector3 vector, float scale)
        {
            float normalizedScale = 1.0f / scale;
            return Vector3.Normalize(vector * normalizedScale);
        }
        public static Vector3 ToEuler(float w, float x, float y, float z)
        {
            // Convertir le quaternion en angles d'Euler
            float sinr_cosp = 2 * (w * x + y * z);
            float cosr_cosp = 1 - 2 * (x * x + y * y);
            float roll = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            float sinp = 2 * (w * y - z * x);
            float pitch;
            if (Math.Abs(sinp) >= 1)
                pitch = (float)Math.CopySign(Math.PI / 2, sinp); // Utiliser 90 degrés si l'inclinaison est proche de 90 degrés
            else
                pitch = (float)Math.Asin(sinp);

            float siny_cosp = 2 * (w * z + x * y);
            float cosy_cosp = 1 - 2 * (y * y + z * z);
            float yaw = (float)Math.Atan2(siny_cosp, cosy_cosp);

            // Convertir les angles d'Euler en degrés
            float rollDeg = roll * (180.0f / (float)Math.PI);
            float pitchDeg = pitch * (180.0f / (float)Math.PI);
            float yawDeg = yaw * (180.0f / (float)Math.PI);

            return new Vector3(rollDeg, pitchDeg, yawDeg);
        }


        public static Quaternion FromEuler(float x, float y, float z)
        {
            // Convertir les angles d'Euler en radians
            float angleX = x * (float)(Math.PI / 180.0);
            float angleY = -y * (float)(Math.PI / 180.0);
            float angleZ = z * (float)(Math.PI / 180.0);

            // Calculer les moitiés des angles
            float halfX = 0.5f * angleX;
            float halfY = 0.5f * angleY;
            float halfZ = 0.5f * angleZ;

            // Calculer les fonctions trigonométriques
            float cosHalfX = (float)Math.Cos(halfX);
            float cosHalfY = (float)Math.Cos(halfY);
            float cosHalfZ = (float)Math.Cos(halfZ);
            float sinHalfX = (float)Math.Sin(halfX);
            float sinHalfY = (float)Math.Sin(halfY);
            float sinHalfZ = (float)Math.Sin(halfZ);

            // Créer le quaternion
            Quaternion result = new Quaternion(
                sinHalfX * cosHalfY * cosHalfZ - cosHalfX * sinHalfY * sinHalfZ,
                cosHalfX * sinHalfY * cosHalfZ + sinHalfX * cosHalfY * sinHalfZ,
                cosHalfX * cosHalfY * sinHalfZ - sinHalfX * sinHalfY * cosHalfZ,
                1 - Math.Abs(cosHalfX * cosHalfY * cosHalfZ + sinHalfX * sinHalfY * sinHalfZ)
            );

            return result;
        }

        public static void StopVehicle()
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI.StopVehicle:I: Stopping vehicle");
            Player.SendClientMessageToAll("Stopping vehicle");

            status = Status.Blocked;


            Record record = RecordConverter.Parse(@"C:\stayinvehicle.rec");

            uint deltaTime = 0;
            Vector3 lastPos = vehicle.Position;
            for (int i = 0; i < record.Blocks.Count; i++)
            {
                if(vehicleSpeed > 0)
                    vehicleSpeed -= 0.1;
                if (i > 0)
                {
                    deltaTime = record.Blocks[i].time - record.Blocks[i - 1].time;
                    lastPos = record.Blocks[i - 1].position;
                }
                if(record.Blocks[i] is RecordInfo.VehicleBlock block)
                {
                    block.velocity = new Vector3(0.0, vehicleSpeed, 0.0);
                    block.position = new Vector3(block.position.X, lastPos.Y + (0.001 * deltaTime * 0.5 * VEH_VELOCITY), block.position.Z);
                    block.additionnalKeyCode = 0;
                    block.vehicleHealth = 1000;
                    record.Blocks[i] = block;
                }
            }

            RecordCreator.Save(record, "recreated.rec");
            Player.SendClientMessageToAll("Record wrote, restarting NPC ...");

            Restart();
        }

        public static void GoForward()
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI.GoForward:I: Continue");
            Quaternion rot = vehicle.GetRotationQuat();
            Matrix matrix = Matrix.CreateFromQuaternion(rot);
            vehicle.Velocity = matrix.Forward.Normalized() * (float)vehicleSpeed;
        }
    }
}
