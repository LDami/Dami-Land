using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static SampSharpGameMode1.Civilisation.Record;

namespace SampSharpGameMode1.Civilisation
{
    class RecordCreator
    {

        RecordInfo.Header header;
        List<RecordInfo.VehicleBlock> vehicleBlocks;

        /// <summary>
        /// Returns a RecordCreator element
        /// </summary>
        /// <param name="recordType">1 for vehicle recordings, 2 for on-foot recordings</param>
        public RecordCreator(RecordInfo.RECORD_TYPE recordType)
        {
            if(recordType == RecordInfo.RECORD_TYPE.VEHICLE || recordType == RecordInfo.RECORD_TYPE.ONFOOT)
            {
                header = new RecordInfo.Header();
                header.id = 1000;
                header.recordType = recordType;
                vehicleBlocks = new List<RecordInfo.VehicleBlock>();
            }
        }

        public void AddVehicleBlock(RecordInfo.VehicleBlock vehicleBlock)
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
        public static void Save(Record record, string filename)
        {
            using (FileStream fs = File.Open(Directory.GetCurrentDirectory() + "/npcmodes/recordings/" + filename, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer;

                buffer = new byte[4];
                BinaryParser.ConvertIntToLittleEndian(record.Header.id, ref buffer, 0);
                fs.Write(buffer, 0, 4);

                buffer = new byte[4];
                BinaryParser.ConvertIntToLittleEndian((int)record.Header.recordType, ref buffer, 0);
                fs.Write(buffer, 0, 4);

                record.VehicleBlocks.ForEach(block =>
                {
                    // Time
                    buffer = new byte[4];
                    BinaryParser.ConvertIntToLittleEndian((int)block.time, ref buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Vehicle ID
                    buffer = new byte[2];
                    BinaryParser.ConvertShorttoLittleEndian(block.vehicleID, ref buffer, 0);
                    fs.Write(buffer, 0, 2);
                    // Left/Right key code
                    buffer = new byte[2];
                    BinaryParser.ConvertUnsignedShorttoLittleEndian((ushort)block.lrKeyCode, ref buffer, 0);
                    fs.Write(buffer, 0, 2);
                    // Up/Down key code
                    buffer = new byte[2];
                    BinaryParser.ConvertUnsignedShorttoLittleEndian((ushort)block.udKeyCode, ref buffer, 0);
                    fs.Write(buffer, 0, 2);
                    // Additional key code
                    buffer = new byte[2];
                    BinaryParser.ConvertShorttoLittleEndian(block.additionnalKeyCode, ref buffer, 0);
                    fs.Write(buffer, 0, 2);
                    // Quaternion 1
                    buffer = new byte[4];
                    BinaryParser.ConvertFloatToLittleEndian(block.rotQuaternion1, ref buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Quaternion 2
                    buffer = new byte[4];
                    BinaryParser.ConvertFloatToLittleEndian(block.rotQuaternion2, ref buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Quaternion 3
                    buffer = new byte[4];
                    BinaryParser.ConvertFloatToLittleEndian(block.rotQuaternion3, ref buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Quaternion 4
                    buffer = new byte[4];
                    BinaryParser.ConvertFloatToLittleEndian(block.rotQuaternion4, ref buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Position X
                    buffer = new byte[4];
                    BitConverter.GetBytes(block.position.X).CopyTo(buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Position Y
                    buffer = new byte[4];
                    BitConverter.GetBytes(block.position.Y).CopyTo(buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Position Z
                    buffer = new byte[4];
                    BitConverter.GetBytes(block.position.Z).CopyTo(buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Velocity X
                    buffer = new byte[4];
                    BitConverter.GetBytes(block.velocity.X).CopyTo(buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Velocity Y
                    buffer = new byte[4];
                    BitConverter.GetBytes(block.velocity.Y).CopyTo(buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Velocity Z
                    buffer = new byte[4];
                    BitConverter.GetBytes(block.velocity.Z).CopyTo(buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Vehicle Health
                    buffer = new byte[4];
                    BitConverter.GetBytes(block.vehicleHealth).CopyTo(buffer, 0);
                    fs.Write(buffer, 0, 4);
                    // Driver Health
                    buffer = new byte[1];
                    buffer[0] = block.driverHealth;
                    fs.Write(buffer, 0, 1);
                    // Driver Armor
                    buffer = new byte[1];
                    buffer[0] = block.driverArmor;
                    fs.Write(buffer, 0, 1);
                    // Driver Weapon
                    buffer = new byte[1];
                    buffer[0] = block.driverWeaponID;
                    fs.Write(buffer, 0, 1);
                    // Siren state
                    buffer = new byte[1];
                    buffer[0] = block.vehicleSirenState;
                    fs.Write(buffer, 0, 1);
                    // Gear state
                    buffer = new byte[1];
                    buffer[0] = block.vehicleGearState;
                    fs.Write(buffer, 0, 1);
                    // Trailer ID
                    buffer = new byte[2];
                    BinaryParser.ConvertUnsignedShorttoLittleEndian((ushort)block.vehicleTrailerID, ref buffer, 0);
                    fs.Write(buffer, 0, 2);
                    // Unknown
                    buffer = new byte[4];// Unknown
                    fs.Write(buffer, 0, 4);

                });

                fs.Close();
            }
        }
    }
}
