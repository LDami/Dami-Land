//using SampSharp.Core.Natives;
using SampSharp.Core.Natives;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Linq;
using System.Threading;

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
        static bool stopProcess = false;
        static bool processEnded = false;

        static double maxSpeed;

        static double vehicleSpeed;
        static TextLabel lbl, lbl2, lblStatus;
        public static void Init(VehicleModelType vehicleModel, Vector3 position, float angle)
        {
            processEnded = true;
            vehicle = BaseVehicle.Create(vehicleModel, position, angle, 0, 0);
            Server.ConnectNPC("stayinvehicle" + BasePlayer.PoolSize, "npctest2");
            status = Status.WaitingDestination;
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
                Thread.Sleep(10);
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
        }

        public static void Process()
        {
            if (lbl != null)
                lbl.Dispose();
            if (lbl2 != null)
                lbl2.Dispose();

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
                            StartVehicle();
                        }
                    }
                    /*
                    if (status == Status.WaitingDestination)
                    {
                        if(destination != Vector3.Zero)
                        {
                            Console.WriteLine("VehicleAI.cs - VehicleAI.Process:I: Destination received: Starting vehicle");
                            StartVehicle();
                        }
                    }
                    else if(status == Status.Driving)
                    {
                        if(vehicle.Position.DistanceTo(destination) < 3.0)
                        {
                            Console.WriteLine("VehicleAI.cs - VehicleAI.Process:I: Destination reached: Stopping vehicle");
                            StopVehicle();
                        }
                        else
                        {
                            GoForward();
                        }
                    }
                    */
                    lbl.Text = "Speed: " + Math.Sqrt(vehicle.Velocity.LengthSquared).ToString(@"N3") + "\nSpeed kmh: " + (Math.Sqrt(vehicle.Velocity.LengthSquared)*181.5).ToString(@"N3");
                    lbl2.Text = "Health: " + npc.Health.ToString();
                    lblStatus.Text = "Status: " + status.ToString();
                    Thread.Sleep(10);
                }
                Console.WriteLine("VehicleAI.cs - VehicleAI.Process:E: received END signal, ending thread ...");
                processEnded = true;
                Player.SendClientMessageToAll("END Process");
            }));
            t.Start();
        }

        public static void StartVehicle()
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI.StartVehicle:I: Starting vehicle");
            Player.SendClientMessageToAll("Starting vehicle !");
            status = Status.Driving;
            vehicleSpeed = 0.5;


            Record record = RecordConverter.Parse(@"C:\stayinvehicle.rec");


            if(destination != Vector3.Zero)
            {


            }

            int recordCount = 20;

            uint deltaTime = 0;
            Vector3 lastPos = vehicle.Position;
            RecordInfo.VehicleBlock block = new RecordInfo.VehicleBlock();
            for (int i = 0; i < recordCount; i++)
            {
                if (i > 0)
                {
                    deltaTime = record.VehicleBlocks[i].time - record.VehicleBlocks[i - 1].time;
                    lastPos = record.VehicleBlocks[i - 1].position;
                }
                block = record.VehicleBlocks[i];
                block.velocity = new Vector3(0.0, vehicleSpeed, 0.0);
                block.position = new Vector3(block.position.X, lastPos.Y + (0.001 * deltaTime * 0.5 * VEH_VELOCITY), block.position.Z);
                //Console.WriteLine($"block[{i}].position = {block.position} ; block[{i}].velocity: = {block.velocity}");
                block.rotQuaternion1 = 1;
                block.rotQuaternion2 = 0;
                block.rotQuaternion3 = 0;
                block.rotQuaternion4 = 0;
                block.additionnalKeyCode = 8;
                if(i >= recordCount - 1)
                    block.additionnalKeyCode = 0;
                block.vehicleHealth = 1000;
                record.VehicleBlocks[i] = block;
            }
            record.VehicleBlocks = record.VehicleBlocks.GetRange(0, recordCount);
            Player.SendClientMessageToAll("Record duration = " + (record.VehicleBlocks[record.VehicleBlocks.Count - 1].time - record.VehicleBlocks[0].time).ToString() + "ms");

            RecordCreator.Save(record, @"recreated.rec");
            Player.SendClientMessageToAll("Record wrote, restarting NPC ...");

            // Parsing again to see new .json file

            record = RecordConverter.Parse(@"C:\Serveur OpenMP\npcmodes\recordings\recreated.rec");

            Restart();
        }

        public static void StopVehicle()
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI.StopVehicle:I: Stopping vehicle");
            Player.SendClientMessageToAll("Stopping vehicle");

            status = Status.Blocked;


            Record record = RecordConverter.Parse(@"C:\stayinvehicle.rec");

            uint deltaTime = 0;
            Vector3 lastPos = vehicle.Position;
            for (int i = 0; i < record.VehicleBlocks.Count; i++)
            {
                if(vehicleSpeed > 0)
                    vehicleSpeed -= 0.1;
                if (i > 0)
                {
                    deltaTime = record.VehicleBlocks[i].time - record.VehicleBlocks[i - 1].time;
                    lastPos = record.VehicleBlocks[i - 1].position;
                }
                RecordInfo.VehicleBlock block = record.VehicleBlocks[i];
                block.velocity = new Vector3(0.0, vehicleSpeed, 0.0);
                block.position = new Vector3(block.position.X, lastPos.Y + (0.001 * deltaTime * 0.5 * VEH_VELOCITY), block.position.Z);
                block.additionnalKeyCode = 0;
                block.vehicleHealth = 1000;
                record.VehicleBlocks[i] = block;
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
