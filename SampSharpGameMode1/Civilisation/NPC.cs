using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SampSharpGameMode1.Civilisation.PathExtractor;

namespace SampSharpGameMode1.Civilisation
{
    // Note: This class cannot be derived from BasePlayer because of S# limitation. Only one class can inherit BasePlayer.
    internal class NPC
    {
        public enum NPCType
        {
            Ped,
            Vehicle
        }
        private BasePlayer npcPlayer;
        private NPCType npcType;
        private string status;
        private Vector3 destination;
        private TextLabel labelStatus;
        private BaseVehicle vehicle;
        public NPC(BasePlayer npc)
        {
            npcPlayer = npc;
        }

        public void OnSpawned()
        {
            Logger.WriteLineAndClose("NPC.cs - NPC.OnSpawned:I: NPC " + npcPlayer.Name + " has spawned");

            labelStatus?.Dispose();
            labelStatus = new TextLabel("Status: N/A", Color.White, npcPlayer.Position + new Vector3(0.0, 0.0, 1.0), 200.0f);
            labelStatus.AttachTo(npcPlayer, new Vector3(0.0, 0.0, 0.5));
            status = "N/A";
        }

        public void OnUpdate()
        {
            /*
            if (npcPlayer.InAnyVehicle)
            {
                if (destination == Vector3.Zero || Vector3.Distance(npcPlayer.Position, destination) < 1)
                {
                    Vector3? nextDestination = NPCController.GetNextPoint(npcPlayer.Name);
                    if (nextDestination != null)
                    {
                        status = "Driving";
                        destination = nextDestination.Value;
                    }
                    else
                        status = "No destination";
                }
                else
                    status = "Destination not reached";
            }
            */
            if (labelStatus != null)
                labelStatus.Text = "Status: " + status + " " + DateTime.Now.ToString("hh:mm:ss") + " dest: " + ((destination == Vector3.Zero) ? "unknown" : "known");
        }

        public void Kick()
        {
            npcPlayer.Kick();
            Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " has been kicked");
        }

        public void Restart()
        {
            Player.SendClientMessageToAll("BEGIN restart");

            if (!npcPlayer.IsDisposed)
            {
                npcPlayer.SendClientMessage("NPC:STOP");
                Thread.Sleep(10);
                npcPlayer.SendClientMessage("NPC:START");
            }
            //BasePlayer.Find(0).PutInVehicle(vehicle, 1);
            Player.SendClientMessageToAll("END restart");
        }

        public void Start(NPCType type)
        {
            npcType = type;
            if((destination = NPCController.GetNextPoint(npcPlayer.Name).GetValueOrDefault(Vector3.Zero)) == Vector3.Zero)
            {
                Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " could not start: There is no destination to reach");
            }
            else
            {
                Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " is starting");
                if(type == NPCType.Vehicle)
                {
                    if (vehicle == null)
                        vehicle = BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.Mower, destination, 0f, 0, 0);
                    else
                        vehicle.Position = destination;
                    Thread.Sleep(1000);
                    npcPlayer.PutInVehicle(vehicle);
                    Thread.Sleep(1000);
                    vehicle.Angle = QuaternionHelper.CalculateRotationAngle(destination.XY, destination.XY, true);
                }
                status = "Moving";
                Thread.Sleep(1000);
                this.Restart();
                Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " is started");
            }
        }

        public void ForceGetNewPos()
        {
            Vector3? nextDestination = NPCController.GetNextPoint(npcPlayer.Name);
            if (nextDestination != null)
            {
                status = "Moving";
                destination = nextDestination.Value;
                GoTo(destination);
            }
            else
                status = "No destination";
        }

        public void GoTo(Vector3 position)
        {
            Record record = new();
            if(npcType == NPCType.Ped)
            {
                RecordInfo.PedBlock block = new RecordInfo.PedBlock();

                Quaternion quat = Quaternion.Identity;
                block.currentVelocity = Vector3.Zero;
                if (position != Vector3.Zero)
                {
                    Console.WriteLine("from: " + npcPlayer.Position);
                    Console.WriteLine("to: " + position);

                    float epsilon = 0.0001f; // Ajustez cela en fonction de la tolérance appropriée pour votre cas
                    if (Vector3.DistanceSquared(npcPlayer.Position, position) < epsilon * epsilon)
                    {
                        // Points très proches, renvoyer un quaternion d'identité (pas de rotation)
                        Console.WriteLine("!! Points trop proche, renvoi d'un Quaternion Identity");
                        quat = Quaternion.Identity;
                    }
                    else
                    {
                        // Création du quaternion
                        quat = QuaternionHelper.LookRotationToPoint(npcPlayer.Position, position);
                        Vector3 vec3 = QuaternionHelper.ToEuler(quat.W, quat.X, quat.Y, quat.Z).Normalized();
                        float pedSpeed = npcPlayer.Velocity.LengthSquared;
                        Console.WriteLine("pedSpeed = " + pedSpeed);
                        block.currentVelocity = new Vector3(vec3.X * pedSpeed, vec3.Y * pedSpeed, vec3.Z * pedSpeed);
                    }

                    Console.WriteLine($"Quaternion: x = {quat.X} ; y = {quat.Y} ; z = {quat.Z} ; w = {quat.W}");
                    quat = Quaternion.Normalize(quat);

                    Console.WriteLine($"Quaternion normalized = {quat.ToVector4()}");
                }
                block.time = 0;

                block.position = npcPlayer.Position;
                Console.WriteLine($"block.position = {block.position} ; time = {block.time}");
                block.rotQuaternion1 = quat.W;
                block.rotQuaternion2 = quat.X;
                block.rotQuaternion3 = quat.Y;
                block.rotQuaternion4 = quat.Z;
                block.additionnalKeyCode = 8;
                record.Header = new RecordInfo.Header(1, RecordInfo.RECORD_TYPE.ONFOOT);
                record.Blocks = new List<RecordInfo.Block> { block };
            }
            else if (npcType == NPCType.Vehicle)
            {
                RecordInfo.VehicleBlock block = new RecordInfo.VehicleBlock();

                Quaternion quat = Quaternion.Identity;
                block.velocity = Vector3.Zero;
                if (position != Vector3.Zero)
                {
                    Console.WriteLine("from: " + npcPlayer.Position);
                    Console.WriteLine("to: " + position);

                    float epsilon = 0.0001f; // Ajustez cela en fonction de la tolérance appropriée pour votre cas
                    if (Vector3.DistanceSquared(npcPlayer.Position, position) < epsilon * epsilon)
                    {
                        // Points très proches, renvoyer un quaternion d'identité (pas de rotation)
                        Console.WriteLine("!! Points trop proche, renvoi d'un Quaternion Identity");
                        quat = Quaternion.Identity;
                    }
                    else
                    {
                        // Création du quaternion
                        quat = QuaternionHelper.LookRotationToPoint(npcPlayer.Position, position);
                        Vector3 vec3 = QuaternionHelper.ToEuler(quat.W, quat.X, quat.Y, quat.Z).Normalized();
                        float vehicleSpeed = vehicle.Velocity.LengthSquared;
                        Console.WriteLine("vehicleSpeed = " + vehicleSpeed);
                        block.velocity = new Vector3(vec3.X * vehicleSpeed, vec3.Y * vehicleSpeed, vec3.Z * vehicleSpeed);
                    }

                    Console.WriteLine($"Quaternion: x = {quat.X} ; y = {quat.Y} ; z = {quat.Z} ; w = {quat.W}");
                    quat = Quaternion.Normalize(quat);

                    Console.WriteLine($"Quaternion normalized = {quat.ToVector4()}");
                }
                block.time = 0;

                block.position = npcPlayer.Position;
                Console.WriteLine($"block.position = {block.position} ; time = {block.time}");
                block.rotQuaternion1 = quat.W;
                block.rotQuaternion2 = quat.X;
                block.rotQuaternion3 = quat.Y;
                block.rotQuaternion4 = quat.Z;
                block.additionnalKeyCode = 8;
                block.vehicleHealth = 1000;
                record.Header = new RecordInfo.Header(1, RecordInfo.RECORD_TYPE.VEHICLE);
                record.Blocks = new List<RecordInfo.Block> { block };
            }
            RecordCreator.Save(record, "recreated.rec");
            Restart();
        }

        public void Stop()
        {
            Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " is stopping");
            RecordInfo.VehicleBlock block = new RecordInfo.VehicleBlock();

            Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, vehicle.Angle);
            quat = Quaternion.Normalize(quat);
            block.velocity = Vector3.Zero;

            block.time = 0;

            block.position = npcPlayer.Position;
            block.rotQuaternion1 = quat.W;
            block.rotQuaternion2 = quat.X;
            block.rotQuaternion3 = quat.Y;
            block.rotQuaternion4 = quat.Z;
            block.additionnalKeyCode = 0;
            block.vehicleHealth = vehicle.Health;

            Record record = new()
            {
                Header = new RecordInfo.Header(1, RecordInfo.RECORD_TYPE.VEHICLE),
                Blocks = new List<RecordInfo.Block> { block }
            };
            RecordCreator.Save(record, "recreated.rec");
            Restart();
        }
    }
}
