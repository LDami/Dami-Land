using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace SampSharpGameMode1
{
    class NPC
    {
        static void itoLittleEndian(int i, ref byte[] array, int off = 0)
        {
            array[0 + off] = (byte)(i & 0x000000FF);
            array[1 + off] = (byte)((i & 0x0000FF00) >> 8);
            array[2 + off] = (byte)((i & 0x00FF0000) >> 16);
            array[3 + off] = (byte)((i & 0xFF000000) >> 24);
        }

        static void stoLittleEndian(short s, ref byte[] array, int off = 0)
        {
            Console.WriteLine("short to convert: " + s.ToString());
            Console.WriteLine("offset: " + off);
            array[0 + off] = (byte)(s & 0x00FF);
            array[1 + off] = (byte)((s & 0xFF00) >> 8);
            Console.WriteLine("result: " + string.Join(", ", array));
        }

        static void ftoLittleEndian(float f, ref byte[] array, int off = 0)
        {
            array[0] = (byte)f;
            array[1] = (byte)(((uint)f >> 8) & 0xFF);
            array[2] = (byte)(((uint)f >> 16) & 0xFF);
            array[3] = (byte)(((uint)f >> 24) & 0xFF);
        }
        struct OnFootDataBlock
        {
            public int time;
            public short LRanalog;
            public short UDanalog;
            public short usKey;
            public Vector3 position;
            public float fQuaternion;
            public float fQuaternion2;
            public float fQuaternion3;
            public float fQuaternion4;
            public byte health;
            public byte armour;
            public byte weaponID;
            public byte specialAction;
            public Vector3 velocity;
            public short animationID;
            public short animationParam;

            public byte[] ToBinary()
            {
                byte[] result = new byte[72];

                itoLittleEndian(time, ref result, 0);
                stoLittleEndian(LRanalog, ref result, 4);
                stoLittleEndian(UDanalog, ref result, 6);
                stoLittleEndian(usKey, ref result, 8);
                ftoLittleEndian(position.X, ref result, 10);
                ftoLittleEndian(position.Y, ref result, 14);
                ftoLittleEndian(position.Z, ref result, 18);
                ftoLittleEndian(fQuaternion, ref result, 22);
                ftoLittleEndian(fQuaternion2, ref result, 26);
                ftoLittleEndian(fQuaternion3, ref result, 30);
                ftoLittleEndian(fQuaternion4, ref result, 34);
                result[38] = health;
                result[39] = armour;
                result[40] = weaponID;
                result[41] = specialAction;
                ftoLittleEndian(velocity.X, ref result, 42);
                ftoLittleEndian(velocity.Y, ref result, 46);
                ftoLittleEndian(velocity.Z, ref result, 50);
                for (int i = 54; i < 68; i++)
                    result[i] = 0;
                stoLittleEndian(animationID, ref result, 68);
                stoLittleEndian(animationParam, ref result, 70);

                return result;
            }
        }

        struct VehicleDataBlock
        {
            public int time;
            public short vehicle;
            public short LRanalog;
            public short UDanalog;
            public short usKey;
            public float fQuaternion;
            public float fQuaternion2;
            public float fQuaternion3;
            public float fQuaternion4;
            public Vector3 position;
            public Vector3 velocity;
            public float health;
            public byte driverHealth;
            public byte driverArmour;
            public byte driverWeapon;
            public byte sirenState;
            public byte gearState;
            public byte[] ToBinary()
            {
                byte[] result = new byte[67];

                itoLittleEndian(time, ref result, 0);
                stoLittleEndian(vehicle, ref result, 4);
                stoLittleEndian(LRanalog, ref result, 6);
                stoLittleEndian(UDanalog, ref result, 8);
                stoLittleEndian(usKey, ref result, 10);
                ftoLittleEndian(fQuaternion, ref result, 12);
                ftoLittleEndian(fQuaternion2, ref result, 16);
                ftoLittleEndian(fQuaternion3, ref result, 20);
                ftoLittleEndian(fQuaternion4, ref result, 24);
                ftoLittleEndian(position.X, ref result, 28);
                ftoLittleEndian(position.Y, ref result, 32);
                ftoLittleEndian(position.Z, ref result, 36);
                ftoLittleEndian(velocity.X, ref result, 40);
                ftoLittleEndian(velocity.Y, ref result, 44);
                ftoLittleEndian(velocity.Z, ref result, 48);
                ftoLittleEndian(health, ref result, 52);
                result[56] = driverHealth;
                result[57] = driverArmour;
                result[58] = driverWeapon;
                result[59] = sirenState;
                result[60] = gearState;
                for (int i = 61; i < 67; i++) result[i] = 0;

                return result;
            }
        }

        BasePlayer bot;

        public void Create()
        {
            List<VehicleDataBlock> blocks = new List<VehicleDataBlock>();
            VehicleDataBlock vehicleBlock = new VehicleDataBlock();
            vehicleBlock.time = 0x6e;
            vehicleBlock.vehicle = 423;
            vehicleBlock.position = new Vector3(1431.6393f, 1519.5398f, 10.5988f);
            vehicleBlock.health = 100.0f;
            blocks.Add(vehicleBlock);

            vehicleBlock.time = 0xeb;
            vehicleBlock.velocity = new Vector3(1.0f, 0.0f, 0.0f);
            blocks.Add(vehicleBlock);

            byte[] header = new[] { (byte)0xE8, (byte)0x03, (byte)0x00, (byte)0x00, (byte)0x01, (byte)0x00, (byte)0x00, (byte)0x00 };

            try
            {
                string filename = SampSharp.GameMode.BaseMode.Instance.Client.ServerPath + "/npcmodes/recordings/npctest.rec";
                using (FileStream fs = File.Open(filename, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    foreach (byte headerbyte in header)
                        fs.WriteByte(headerbyte);
                    foreach(VehicleDataBlock block in blocks)
                    {
                        fs.Write(block.ToBinary());
                    }
                }
            }
            catch(IOException e)
            {
                Console.WriteLine("NPC.cs - NPC.Create:E: " + e);
            }
        }

        public void Connect(Player player)
        {
            /*
            bot = SampSharp.GameMode.SAMP.Server.ConnectNPC("testman", "npctest2");
            bot.SetSpawnInfo(0, 0, new SampSharp.GameMode.Vector3(1431.6393f, 1519.5398f, 10.5988f), 0.0f);
            bot.Spawn();

            player.SendClientMessage("bot id = " + bot.Id);
            player.SendClientMessage("bot is npc = " + bot.IsNPC.ToString()); // False

            BaseVehicle vehicle = BaseVehicle.Create(SampSharp.GameMode.Definitions.VehicleModelType.Buccaneer, new SampSharp.GameMode.Vector3(1431.6393f, 1519.5398f, 10.5988f), 0.0f, 1, 1);
            bot.PutInVehicle(vehicle, 0);
            */
        }

        public void Dispose()
        {
            if(!bot.IsDisposed)
            {
                bot.Kick();
                bot.Dispose();
            }
        }
    }
}
