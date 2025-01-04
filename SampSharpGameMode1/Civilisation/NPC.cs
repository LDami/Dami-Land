using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Map;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
        private Vector3 startingPos;
        private Vector3 destination;
        private TextLabel labelStatus;
        private BaseVehicle vehicle;
        private float accumulatedAccel;


        DynamicCheckpoint destinationCP;
        List<DynamicObject> debugPosObj = new();
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
            if (npcPlayer.InAnyVehicle && status != "Destination reached")
            {
                if (Vector3.Distance(npcPlayer.Position, destination) < 1)
                {
                    ForceGetNewPos();
                }
            }
            if (labelStatus != null)
            {
                if(npcPlayer.InAnyVehicle)
                {
                    labelStatus.Text = "Status: " + status + " " + DateTime.Now.ToString("hh:mm:ss") + " dest: " + ((destination == Vector3.Zero) ? "unknown" : "known") +
                        Utils.GetKmhSpeedFromVelocity(npcPlayer.Vehicle.Velocity) + "km/h";
                }
                else
                {
                    labelStatus.Text = "Status: " + status + " " + DateTime.Now.ToString("hh:mm:ss") + " dest: " + ((destination == Vector3.Zero) ? "unknown" : "known");
                }
            }
        }

        public void Kick()
        {
            vehicle?.Dispose();
            destinationCP?.Dispose();
            destinationCP = null;
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
            startingPos = NPCController.GetNextPoint(npcPlayer.Name).GetValueOrDefault(Vector3.Zero) + Vector3.Up;
            /*
            if((destination = NPCController.PeekNextPoint(npcPlayer.Name).GetValueOrDefault(Vector3.Zero)) == Vector3.Zero)
            {
                Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " could not start: There is no destination to reach");
            }
            else
            {
            */
                Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " is starting, startingPos = " + startingPos);
                if(type == NPCType.Vehicle)
                {
                    if (vehicle == null)
                        vehicle = BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.Sentinel, startingPos, 0f, 0, 0);
                    else
                        vehicle.Position = startingPos + (Vector3.UnitZ*2);
                    Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: Vehicle OK");

                    // Create the stop .rec file
                    Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " is creating the Stop .rec file ...");
                    CreateStopScript(startingPos);
                    Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: Done");

                    Thread.Sleep(1000);
                    Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: Putting in vehicle");
                    npcPlayer.PutInVehicle(vehicle);
                    Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: OK");
                    Thread.Sleep(1000);
                    vehicle.Angle = Utils.GetAngleToPoint(startingPos.XY, destination.XY);
                    npcPlayer.SendClientMessage("NPC:START");
                    accumulatedAccel = 0f;
                    destinationCP?.Dispose();
                    if(debugPosObj is not null)
                    {
                        foreach(DynamicObject obj in debugPosObj)
                        {
                            obj?.Dispose();
                        }
                    }
                    debugPosObj = new List<DynamicObject>();
                }
                status = "Waiting for goto command";
                Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " is started");
            //}
        }

        public void ForceGetNewPos()
        {
            Vector3? nextDestination = NPCController.GetNextPoint(npcPlayer.Name);
            if (nextDestination is null)
            {
                status = "Destination reached";
                return;
            }
            startingPos = npcPlayer.Position + (Vector3.Up * 0.2f);
            status = "Moving";
            destination = nextDestination.Value;
            GoTo(destination);
            OnUpdate();
        }

        public void GoTo(Vector3 position)
        {
            Console.WriteLine(" ");
            Console.WriteLine("-- GOTO called --");
            destinationCP?.Dispose();
            destinationCP = new DynamicCheckpoint(destination, 5, 0, streamdistance: 200);
            Record record = new()
            {
                Header = new RecordInfo.Header(1, RecordInfo.RECORD_TYPE.VEHICLE)
            };
            if (npcType == NPCType.Ped)
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
                if (position != Vector3.Zero)
                {
                    int steps;
                    float vehicleHeight = BaseVehicle.GetModelInfo(vehicle.Model, SampSharp.GameMode.Definitions.VehicleModelInfoType.Size).Z - 0.5f;
                    destination = new Vector3(destination.X, destination.Y, FindZFromVector2(destination.X, destination.Y) + vehicleHeight / 2);
                    startingPos = new Vector3(startingPos.X, startingPos.Y, FindZFromVector2(startingPos.X, startingPos.Y) + vehicleHeight / 2);

                    float distance = (float)Math.Sqrt((destination.X - startingPos.X) * (destination.X - startingPos.X) + (destination.Y - startingPos.Y) * (destination.Y - startingPos.Y) + (destination.Z - startingPos.Z) * (destination.Z - startingPos.Z));
                    Console.WriteLine("distance = " + distance);

                    float delta = 2.74289999999996f; // Distance for 200ms for sentinel


                    float angleDEG = Utils.GetAngleToPoint(startingPos.XY, destination.XY);
                    float angleRAD = angleDEG * ((float)Math.PI / 180f);
                    Console.WriteLine("angleDEG = " + angleDEG);
                    Console.WriteLine("angleRAD = " + angleRAD);

                    float acceleration = 0.3f;
                    int updaterate = 200;

                    Vector3 vector = new(Math.Sin(angleRAD), Math.Cos(angleRAD), 0);
                    vector = new(vector.X > 0 ? -vector.X : Math.Abs(vector.X), vector.Y, vector.Z);
                    //float travelTime = (float)Math.Sqrt((2 * distance) / acceleration);
                    float travelTime = (updaterate * distance) / delta;
                    
                    Quaternion quat = QuaternionHelper.FromEuler(0, 0, angleRAD);
                    Console.WriteLine($"Quaternion: x = {quat.X} ; y = {quat.Y} ; z = {quat.Z} ; w = {quat.W}");
                    if (acceleration > 0)
                    {
                        Console.WriteLine($"travelTime: {travelTime}");
                        Console.WriteLine($"updaterate: {updaterate}");
                        steps = (int)Math.Truncate(travelTime / updaterate);
                        if (steps == 0) steps = 1;
                        Logger.WriteLineAndClose("steps: " + steps);

                        RecordInfo.VehicleBlock block = new()
                        {
                            rotQuaternion1 = quat.W,
                            rotQuaternion2 = quat.X,
                            rotQuaternion3 = quat.Y,
                            rotQuaternion4 = quat.Z,
                            velocity = vector * acceleration,
                            additionnalKeyCode = 8,
                            vehicleHealth = 1000
                        };
                        record.Blocks = new List<RecordInfo.Block>();
                        for (int i = 0; i < steps; i++)
                        {
                            Vector3 pos = new(startingPos.X + delta * i * vector.X, startingPos.Y + delta * i * vector.Y, startingPos.Z + delta * i * vector.Z);
                            Vector3 vel = vector * acceleration;

                            block.time = (uint)(updaterate * i);
                            if (acceleration > 0)
                            {
                                block.velocity = vel;
                            }
                            block.position = pos;
                            debugPosObj.Add(new DynamicObject(19203, block.position, vector));

                            //record.Blocks.Add(RecordInfo.VehicleBlock.Copy(block));
                        }
                    }

                    RecordInfo.VehicleBlock block2 = new()
                    {
                        time = (uint)travelTime,
                        rotQuaternion1 = quat.W,
                        rotQuaternion2 = quat.X,
                        rotQuaternion3 = quat.Y,
                        rotQuaternion4 = quat.Z,
                        velocity = Vector3.Zero,
                        additionnalKeyCode = 0,
                        vehicleHealth = 1000,
                        position = destination
                    };
                    //record.Blocks.Add(RecordInfo.VehicleBlock.Copy(block2));
                }
            }
            RecordCreator.Save(record, "recreated.rec");
            Restart();
        }

        public void CreateStopScript(Vector3 stopPoint)
        {
            Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " is stopping");
            RecordInfo.VehicleBlock block = new();

            Quaternion quat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, vehicle.Angle);
            quat = Quaternion.Normalize(quat);
            block.velocity = Vector3.Zero;

            block.time = 0;

            block.position = stopPoint;
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
