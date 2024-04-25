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
        DynamicCheckpoint destinationCP;
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
            if (npcPlayer.InAnyVehicle)
            {
                if (Vector3.Distance(npcPlayer.Position, destination) < 1)
                {
                    status = "Destination reached";
                }
            }
            if (labelStatus != null)
                labelStatus.Text = "Status: " + status + " " + DateTime.Now.ToString("hh:mm:ss") + " dest: " + ((destination == Vector3.Zero) ? "unknown" : "known");
        }

        public void Kick()
        {
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
            startingPos = NPCController.GetNextPoint(npcPlayer.Name).GetValueOrDefault(Vector3.Zero);
            if((destination = NPCController.GetNextPoint(npcPlayer.Name).GetValueOrDefault(Vector3.Zero)) == Vector3.Zero)
            {
                Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " could not start: There is no destination to reach");
            }
            else
            {
                Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " is starting, startingPos = " + startingPos);
                if(type == NPCType.Vehicle)
                {
                    if (vehicle == null)
                        vehicle = BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.Mower, startingPos, 0f, 0, 0);
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
                    //destinationCP = new DynamicCheckpoint(destination, 5, 0, streamdistance: 200);
                }
                status = "Waiting for goto command";
                Logger.WriteLineAndClose("NPC.cs - NPC.Start:I: NPC " + npcPlayer.Name + " is started");
            }
        }

        public void ForceGetNewPos()
        {
            Vector3? nextDestination;
            if (destination == Vector3.Zero)
            {
                nextDestination = NPCController.GetNextPoint(npcPlayer.Name);
                if (nextDestination is null)
                {
                    status = "No destination";
                    return;
                }
                startingPos = destination + Vector3.Up;
            }
            else
            {
                nextDestination = destination;
            }
            status = "Moving";
            destination = nextDestination.Value;
            GoTo(destination);
            OnUpdate();
        }

        public void GoTo(Vector3 position)
        {
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
                    int time, steps;

                    float distance = (float)Math.Sqrt((destination.X - startingPos.X) * (destination.X - startingPos.X) + (destination.Y - startingPos.Y) * (destination.Y - startingPos.Y) + (destination.Z - startingPos.Z) * (destination.Z - startingPos.Z));

                    //float angle = (float)Math.Atan2(destination.Y - startingPos.Y, destination.X - startingPos.X);
                    float angleDEG = Utils.GetAngleToPoint(startingPos.XY, destination.XY);
                    float angleRAD = angleDEG * ((float)Math.PI / 180f);

                    float xvel;
                    float yvel;
                    float zvel = 0.0f;

                    int accsteps = 0;
                    float accdist;

                    float acceleration = 0.002f;
                    float speed = 0.3f;
                    int updaterate = 100;

                    if (acceleration > 0)
                    {
                        accsteps = (int)(speed / acceleration + 0.5);
                        accdist = 0;
                        for (int i = 0; i < accsteps; i++)
                        {
                            accdist += acceleration * i * updaterate;
                        }
                        time = (int)((distance - accdist) / speed) + accsteps * updaterate;
                        xvel = (float)Math.Cos(angleRAD);
                        yvel = (float)Math.Sin(angleRAD);
                    }
                    else
                    {
                        time = (int)(distance / speed);
                        xvel = (float)Math.Cos(angleRAD);
                        yvel = (float)Math.Sin(angleRAD);
                    }

                    //angle = (-angle) / (3.14159265359f / 180.0f) + 90.0F;

                    steps = time / updaterate;


                    float xrate = (destination.X - startingPos.X) / steps;
                    float yrate = (destination.Y - startingPos.Y) / steps;
                    float zrate = (destination.Z - startingPos.Z) / steps;

                    if (steps == 0) time = 0;
                    else time /= steps;

                    Console.WriteLine("from: " + startingPos);
                    Console.WriteLine("to: " + position);
                    Console.WriteLine("angleDEG: " + angleDEG);

                    //quat = Quaternion.Normalize(quat);
                    Quaternion quat = QuaternionHelper.FromAngle(angleRAD);
                    Console.WriteLine($"Quaternion: x = {quat.X} ; y = {quat.Y} ; z = {quat.Z} ; w = {quat.W}");


                    RecordInfo.VehicleBlock block = new();
                    block.rotQuaternion1 = quat.W;
                    block.rotQuaternion2 = quat.X;
                    block.rotQuaternion3 = quat.Y;
                    block.rotQuaternion4 = -quat.Z;
                    block.velocity = new Vector3(xvel, yvel, zvel);

                    int step = 0;
                    int curTime = 0;

                    record.Blocks = new List<RecordInfo.Block>();
                    Logger.WriteLineAndClose("steps: " + steps);
                    Logger.WriteLineAndClose("accsteps: " + accsteps);
                    while (step <= steps)
                    {
                        block.time = (uint)(time * step + curTime);
                        if (acceleration > 0)
                        {
                            if (step < accsteps)
                            {
                                block.velocity = new Vector3(xvel * acceleration * step, yvel * acceleration * step, 0);
                                float xpos = startingPos.X + xrate * step;
                                float ypos = startingPos.Y + yrate * step;
                                float zpos = startingPos.Z + zrate * step;
                                // We can use FindZFromVector2 if needed for zpos
                                block.position = new Vector3(xpos, ypos, zpos);
                            }
                            else
                            {
                                block.velocity = new Vector3(xvel * speed, yvel * speed, 0);
                                float xpos = startingPos.X + xrate * step;
                                float ypos = startingPos.Y + yrate * step;
                                float zpos = startingPos.Z + zrate * step;
                                // We can use FindZFromVector2 if needed for zpos
                                block.position = new Vector3(xpos, ypos, zpos);
                            }
                        }
                        else
                        {
                            float xpos = startingPos.X + xrate * step;
                            float ypos = startingPos.Y + yrate * step;
                            float zpos = startingPos.Z + zrate * step;
                            block.position = new Vector3(xpos, ypos, zpos);
                        }

                        if (step == steps)
                        {
                            block.udKeyCode = 0;
                            block.velocity = Vector3.Zero;
                        }
                        block.vehicleHealth = 1000;
                        record.Blocks.Add(RecordInfo.VehicleBlock.Copy(block));

                        step++;
                    }
                }
            }
            RecordCreator.Save(record, "recreated.rec");
            destination = Vector3.Zero;
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
