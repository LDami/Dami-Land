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

                record.Blocks.ForEach(block =>
                {
                    // Time
                    buffer = new byte[4];
                    BinaryParser.ConvertIntToLittleEndian((int)block.time, ref buffer, 0);
                    fs.Write(buffer, 0, 4);
                    if (block is RecordInfo.PedBlock pedBlock)
                    {
                        // Left/Right key code
                        buffer = new byte[2];
                        BinaryParser.ConvertShortToLittleEndian(pedBlock.lrKeyCode, ref buffer, 0);
                        fs.Write(buffer, 0, 2);
                        // Up/Down key code
                        buffer = new byte[2];
                        BinaryParser.ConvertShortToLittleEndian(pedBlock.udKeyCode, ref buffer, 0);
                        fs.Write(buffer, 0, 2);
                        // Additional key code
                        buffer = new byte[2];
                        BinaryParser.ConvertShortToLittleEndian(pedBlock.additionnalKeyCode, ref buffer, 0);
                        fs.Write(buffer, 0, 2);
                        // Position X
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.position.X).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Position Y
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.position.Y).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Position Z
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.position.Z).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Quaternion 1
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.rotQuaternion1).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Quaternion 2
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.rotQuaternion2).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Quaternion 3
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.rotQuaternion3).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Quaternion 4
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.rotQuaternion4).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Health
                        buffer = new byte[1];
                        buffer[0] = pedBlock.health;
                        fs.Write(buffer, 0, 1);
                        // Armor
                        buffer = new byte[1];
                        buffer[0] = pedBlock.armor;
                        fs.Write(buffer, 0, 1);
                        // Weapon ID
                        buffer = new byte[1];
                        buffer[0] = pedBlock.weapon;
                        fs.Write(buffer, 0, 1);
                        // Currently applied special action
                        buffer = new byte[1];
                        buffer[0] = pedBlock.currentlyAppliedSpecialAction;
                        fs.Write(buffer, 0, 1);
                        // Current Velocity X
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.currentVelocity.X).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Current Velocity Y
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.currentVelocity.Y).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Current Velocity Z
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.currentVelocity.Z).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Current surfing X
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.currentSurfing.X).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Current surfing Y
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.currentSurfing.Y).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Current surfing Z
                        buffer = new byte[4];
                        BitConverter.GetBytes(pedBlock.currentSurfing.Z).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Current surfing Vehicle ID
                        buffer = new byte[2];
                        BinaryParser.ConvertUnsignedShorttoLittleEndian((ushort)pedBlock.currentlySurfingVehicleID, ref buffer, 0);
                        fs.Write(buffer, 0, 2);
                        // Currently applied Animation index
                        buffer = new byte[2];
                        BinaryParser.ConvertUnsignedShorttoLittleEndian((ushort)pedBlock.currentlyAppliedAnimationIndex, ref buffer, 0);
                        fs.Write(buffer, 0, 2);
                        // Some animation parameters
                        buffer = new byte[2];// Unknown
                        fs.Write(buffer, 0, 2);
                    }
                    if(block is RecordInfo.VehicleBlock vehicleBlock)
                    {
                        // Vehicle ID
                        buffer = new byte[2];
                        BinaryParser.ConvertShortToLittleEndian(vehicleBlock.vehicleID, ref buffer, 0);
                        fs.Write(buffer, 0, 2);
                        // Left/Right key code
                        buffer = new byte[2];
                        BinaryParser.ConvertUnsignedShorttoLittleEndian((ushort)vehicleBlock.lrKeyCode, ref buffer, 0);
                        fs.Write(buffer, 0, 2);
                        // Up/Down key code
                        buffer = new byte[2];
                        BinaryParser.ConvertUnsignedShorttoLittleEndian((ushort)vehicleBlock.udKeyCode, ref buffer, 0);
                        fs.Write(buffer, 0, 2);
                        // Additional key code
                        buffer = new byte[2];
                        BinaryParser.ConvertShortToLittleEndian(vehicleBlock.additionnalKeyCode, ref buffer, 0);
                        fs.Write(buffer, 0, 2);
                        // Quaternion 1
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.rotQuaternion1).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Quaternion 2
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.rotQuaternion2).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Quaternion 3
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.rotQuaternion3).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Quaternion 4
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.rotQuaternion4).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Position X
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.position.X).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Position Y
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.position.Y).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Position Z
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.position.Z).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Velocity X
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.velocity.X).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Velocity Y
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.velocity.Y).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Velocity Z
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.velocity.Z).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Vehicle Health
                        buffer = new byte[4];
                        BitConverter.GetBytes(vehicleBlock.vehicleHealth).CopyTo(buffer, 0);
                        fs.Write(buffer, 0, 4);
                        // Driver Health
                        buffer = new byte[1];
                        buffer[0] = vehicleBlock.driverHealth;
                        fs.Write(buffer, 0, 1);
                        // Driver Armor
                        buffer = new byte[1];
                        buffer[0] = vehicleBlock.driverArmor;
                        fs.Write(buffer, 0, 1);
                        // Driver Weapon
                        buffer = new byte[1];
                        buffer[0] = vehicleBlock.driverWeaponID;
                        fs.Write(buffer, 0, 1);
                        // Siren state
                        buffer = new byte[1];
                        buffer[0] = vehicleBlock.vehicleSirenState;
                        fs.Write(buffer, 0, 1);
                        // Gear state
                        buffer = new byte[1];
                        buffer[0] = vehicleBlock.vehicleGearState;
                        fs.Write(buffer, 0, 1);
                        // Trailer ID
                        buffer = new byte[2];
                        BinaryParser.ConvertUnsignedShorttoLittleEndian((ushort)vehicleBlock.vehicleTrailerID, ref buffer, 0);
                        fs.Write(buffer, 0, 2);
                        // Unknown
                        buffer = new byte[4];// Unknown
                        fs.Write(buffer, 0, 4);
                    }

                });

                fs.Close();
            }
        }
    }
}
