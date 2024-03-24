using Newtonsoft.Json;
using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static SampSharpGameMode1.BinaryParser;

namespace SampSharpGameMode1.Civilisation
{
    internal class RecordConverter
    {
        public static Record Parse(string filename, string output = "record.json")
        {
            Record record = new Record();

            using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read))
            {
                RecordInfo.Header header = new RecordInfo.Header();
                byte[] buffer = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    fs.Read(buffer, i, 1);
                }
                header.id = GetLittleEndianInt32FromByteArray(buffer, 0);
                header.recordType = (RecordInfo.RECORD_TYPE)GetLittleEndianInt32FromByteArray(buffer, 4);
                record.Header = header;

                if(header.recordType == RecordInfo.RECORD_TYPE.VEHICLE)
                {
                    RecordInfo.VehicleBlock vehicleBlock = new RecordInfo.VehicleBlock();
                    const int BLOCK_SIZE = 67;
                    long nbrOfBlocks = (fs.Length - 8) / BLOCK_SIZE;
                    //nbrOfBlocks = 3;
                    for(int blockIndex = 0; blockIndex < nbrOfBlocks; blockIndex++)
                    {
                        buffer = new byte[67];
                        for (int i = 0; i < 67; i++)
                        {
                            fs.Read(buffer, i, 1);
                        }
                        vehicleBlock.time = (uint)GetLittleEndianInt32FromByteArray(buffer, 0);
                        vehicleBlock.vehicleID = GetLittleEndianInt16FromByteArray(buffer, 4);
                        vehicleBlock.lrKeyCode = (ushort)GetLittleEndianInt16FromByteArray(buffer, 6);
                        vehicleBlock.udKeyCode = (ushort)GetLittleEndianInt16FromByteArray(buffer, 8);
                        vehicleBlock.additionnalKeyCode = GetLittleEndianInt16FromByteArray(buffer, 10);
                        vehicleBlock.rotQuaternion1 = GetFloatFrom4ByteArray(buffer, 12);
                        vehicleBlock.rotQuaternion2 = GetFloatFrom4ByteArray(buffer, 16);
                        vehicleBlock.rotQuaternion3 = GetFloatFrom4ByteArray(buffer, 20);
                        vehicleBlock.rotQuaternion4 = GetFloatFrom4ByteArray(buffer, 24);

                        vehicleBlock.position = new Vector3(
                            GetFloatFrom4ByteArray(buffer, 28),
                            GetFloatFrom4ByteArray(buffer, 32),
                            GetFloatFrom4ByteArray(buffer, 36)
                            );
                        vehicleBlock.velocity = new Vector3(
                            GetFloatFrom4ByteArray(buffer, 40),
                            GetFloatFrom4ByteArray(buffer, 44),
                            GetFloatFrom4ByteArray(buffer, 48)
                            );
                        vehicleBlock.vehicleHealth = GetFloatFrom4ByteArray(buffer, 52);
                        vehicleBlock.driverHealth = buffer[56];
                        vehicleBlock.driverArmor = buffer[57];
                        vehicleBlock.driverWeaponID = buffer[58];
                        vehicleBlock.vehicleSirenState = buffer[59];
                        vehicleBlock.vehicleGearState = buffer[60];
                        vehicleBlock.vehicleTrailerID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 61);
                        record.Blocks.Add(vehicleBlock);
                    }
                }
                fs.Close();
            }

            //Logger.WriteLineAndClose("RecordConverter.cs - RecordConvert.Parse:I: " + record.VehicleBlocks.Count + " blocks read");

            string json = JsonConvert.SerializeObject(record);

            try
            {
                using (FileStream fs = File.Open(output, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = new UTF8Encoding(true).GetBytes(json);
                    foreach (byte databyte in data)
                        fs.WriteByte(databyte);
                    fs.FlushAsync();
                    fs.Close();
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("RecordConverter.cs - RecordConvert.Parse:E: Cannot write in json file: ");
                Console.WriteLine(e.Message);
            }

            return record;
        }

        private static float GetFloatFrom4ByteArray(byte[] source, int startIndex)
        {
            byte[] x = new byte[4];
            Array.Copy(source, startIndex, x, 0, 4);
            return BitConverter.ToSingle(x, 0);
        }
    }
}
