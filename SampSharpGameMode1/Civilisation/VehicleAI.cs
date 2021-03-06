//using SampSharp.Core.Natives;
using SampSharp.Core.Natives;
using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Threading;

namespace SampSharpGameMode1.Civilisation
{
    class VehicleAI
    {
        const float VEH_ACCEL = 1.1f;
        enum Status
        {
            WaitingDestination,
            Driving,
            Blocked
        }
        BasePlayer npc;
        BaseVehicle vehicle;
        Status status;
        Vector3 destination;
        bool stopProcess;

        double maxSpeed;

        double vehicleSpeed;
        TextLabel lbl, lbl2;
        public VehicleAI(VehicleModelType vehicleModel, Vector3 position, float angle)
        {
            NPC npcInstance = Server.ConnectNPC("stayinvehicle", "npcidle");
            npcInstance.Connected += (sender, args) =>
            {
                npc = (sender as NPC).PlayerInstance;
                Console.WriteLine("VehicleAI.cs - VehicleAI.__:E: NPC id : " + npc.Id);
                Console.WriteLine("VehicleAI.cs - VehicleAI.__:E: NPC id2 : " + args.PlayerId);

                vehicle = BaseVehicle.Create(vehicleModel, position, angle, 0, 0);
                npc.PutInVehicle(vehicle);
                status = Status.WaitingDestination;
                destination = Vector3.Zero;
                stopProcess = false;

                vehicleSpeed = 0.0f;
                lbl = new TextLabel("AI", Color.White, vehicle.Position + new Vector3(0.0, 0.0, 1.0), 200.0f);
                lbl.AttachTo(vehicle, new Vector3(0.0, 0.0, 1.0));
                lbl2 = new TextLabel("AI", Color.White, vehicle.Position + new Vector3(-5.0, 0.0, 1.0), 200.0f);
                lbl2.AttachTo(npc, new Vector3(0.0, 0.0, 1.0));
                this.Process();

                Console.WriteLine("VehicleAI.cs - VehicleAI.__:E: NPC is connected !");
            };
        }

        private void Npc_Spawned(object sender, SampSharp.GameMode.Events.SpawnEventArgs e)
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI.Npc_Spawned:I: NPC has spawn !");
            if (vehicle != null)
            {
                ((BasePlayer)sender).PutInVehicle(vehicle);
            }
        }

        public void SetNPCHealth(int health)
        {
            npc.Health = health;
        }

        private void Npc_Died(object sender, SampSharp.GameMode.Events.DeathEventArgs e)
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI.Npc_Died:I: NPC has been killed !");
        }

        public void Kick()
        {
            npc.Kick();
            npc.Dispose();
            vehicle.Dispose();
            Console.WriteLine("VehicleAI.cs - VehicleAI.Kick:I: Vehicle AI kicked from the server !");
        }

        public void SetDestination(Vector3 _destination, double _maxSpeed)
        {
            destination = _destination;
            maxSpeed = _maxSpeed;
            Console.WriteLine("VehicleAI.cs - VehicleAI.SetDestination:I: Destination set to " + _destination.ToString());
            Console.WriteLine("VehicleAI.cs - VehicleAI.SetDestination:I: Max speed set to " + maxSpeed.ToString());
        }

        private void Process()
        {
            Thread t = new Thread(new ThreadStart(() =>
            {
                while(!stopProcess)
                {
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
                    lbl.Text = "Speed: " + Math.Sqrt(vehicle.Velocity.LengthSquared).ToString(@"N3") + "\nSpeed kmh: " + (Math.Sqrt(vehicle.Velocity.LengthSquared)*181.5).ToString(@"N3");
                    lbl2.Text = "Health: " + npc.Health.ToString();
                }
            }));
            t.Start();
        }

        public void StartVehicle()
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI.StartVehicle:I: Starting vehicle");
            Player.SendClientMessageToAll("Starting vehicle !");
            this.status = Status.Driving;
            vehicleSpeed = 0.15;
            Quaternion rot = vehicle.GetRotationQuat();
            Matrix matrix = Matrix.CreateFromQuaternion(rot);
            //vehicle.SetAngularVelocity(matrix.Forward.Normalized() + new Vector3(vehicleSpeed, 0, 0));
            vehicle.Velocity = matrix.Forward.Normalized() * 0.15f;
            /*

            Thread.Sleep(500);
            while (vehicleSpeed < maxSpeed)
            {
                Console.WriteLine("VehicleAI.cs - VehicleAI.Process:I: vehicleSpeed: " + vehicleSpeed.ToString());
                //vehicleSpeed = Math.Sqrt(vehicle.Velocity.LengthSquared) * VEH_ACCEL;
                Console.WriteLine("VehicleAI.cs - VehicleAI.Process:I: vehicleSpeed: " + vehicleSpeed.ToString());
                rot = vehicle.GetRotationQuat();
                matrix = Matrix.CreateFromQuaternion(rot);
                //vehicle.SetAngularVelocity(matrix.Forward.Normalized() + new Vector3(vehicleSpeed, 0, 0));
                //vehicleSpeed = Math.Sqrt(vehicle.Velocity.LengthSquared);
                Console.WriteLine("VehicleAI.cs - VehicleAI.Process:I: vehicleSpeed: " + vehicleSpeed.ToString());
                Thread.Sleep(500);
            }
            */
            Player.SendClientMessageToAll("Vehicle at max speed !");
            Console.WriteLine("VehicleAI.cs - VehicleAI.StartVehicle:I: Max speed reached");
        }

        public void StopVehicle()
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI.StopVehicle:I: Stopping vehicle");
            Player.SendClientMessageToAll("Stopping vehicle");
            Quaternion rot = vehicle.GetRotationQuat();
            Matrix matrix = Matrix.CreateFromQuaternion(rot);
            vehicle.Velocity = matrix.Forward.Normalized() * 0;
            /*
            while (vehicleSpeed > 0.0)
            {
                vehicleSpeed = Math.Sqrt(vehicle.Velocity.LengthSquared) * (VEH_ACCEL - 0.3);
                rot = vehicle.GetRotationQuat();
                matrix = Matrix.CreateFromQuaternion(rot);
                vehicle.Velocity = matrix.Forward.Normalized() * (float)vehicleSpeed;
            }
            */
            Player.SendClientMessageToAll("Vehicle stopped !");
            this.status = Status.WaitingDestination;
            this.destination = Vector3.Zero;
            Console.WriteLine("VehicleAI.cs - VehicleAI.StopVehicle:I: Vehicle stopped");
        }

        public void GoForward()
        {
            Console.WriteLine("VehicleAI.cs - VehicleAI.GoForward:I: Continue");
            Quaternion rot = vehicle.GetRotationQuat();
            Matrix matrix = Matrix.CreateFromQuaternion(rot);
            vehicle.Velocity = matrix.Forward.Normalized() * (float)vehicleSpeed;
        }
    }
}
