using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SampSharpGameMode1.Civilisation
{
    class RecordCreator
    {
        public enum RECORD_TYPE
        {
            VEHICLE = 1,
            ONFOOT = 2
        }
        public struct Header
        {
            public int id;
            public int recordType; // 1 for vehicle recordings, 2 for on-foot recordings
        }

        public struct VehicleBlock
        {
            public int time;
            public ushort vehicleID;
            public ushort lrKeyCode; // Left Right key code
            public ushort udKeyCode; // Up Down key code
            public short additionnalKeyCode;
            public float rotQuaternion1;
            public float rotQuaternion2;
            public float rotQuaternion3;
            public float rotQuaternion4;
            public Vector3 position;
            public Vector3 velocity;
            public float vehicleHealth;
            public byte driverHealth;
            public byte driverArmor;
            public byte driverWeaponID;
            public byte vehicleSirenState;
            public byte vehicleGearState;
            public ushort vehicleTrailerID;
        }

        Header header;
        List<VehicleBlock> vehicleBlocks;

        /// <summary>
        /// Returns a RecordCreator element
        /// </summary>
        /// <param name="recordType">1 for vehicle recordings, 2 for on-foot recordings</param>
        public RecordCreator(RECORD_TYPE recordType)
        {
            if(recordType == RECORD_TYPE.VEHICLE || recordType == RECORD_TYPE.ONFOOT)
            {
                header = new Header();
                header.id = 1000;
                header.recordType = (int)recordType;
                vehicleBlocks = new List<VehicleBlock>();
            }
        }

        public void AddVehicleBlock(VehicleBlock vehicleBlock)
        {
            if(vehicleBlocks != null)
            {
                vehicleBlocks.Add(vehicleBlock);
            }
        }

        /// <summary>
        /// Save the record file into the npcmodes\recordings folder
        /// </summary>
        /// <param name="filename">File name with .rec extension</param>
        public void Save(string filename)
        {
            using (FileStream fs = File.Open(BaseMode.Instance.Client.ServerPath + "\\npcmodes\\recordings\\" + filename, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer;

                buffer = new byte[4];
                FileEncoding.ConvertIntToLittleEndian(header.id, ref buffer, 0);
                fs.Write(buffer, 0, 4);

                buffer = new byte[4];
                FileEncoding.ConvertIntToLittleEndian(header.recordType, ref buffer, 0);
                fs.Write(buffer, 0, 4);

                vehicleBlocks.ForEach(block =>
                {
                    buffer = new byte[4];
                    FileEncoding.ConvertIntToLittleEndian(block.time, ref buffer, 0);
                    fs.Write(buffer, 0, 4);
                    buffer = new byte[2];
                    FileEncoding.ConvertUnsignedShorttoLittleEndian(block.vehicleID, ref buffer, 0);
                    fs.Write(buffer, 0, 2);

                });

                fs.Close();
            }
        }
    }
}
