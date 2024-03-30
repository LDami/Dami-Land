using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
    public class BinaryParser
    {
        public static int GetLittleEndianInt32FromByteArray(byte[] data, int startIndex)
        {
            return (data[startIndex + 3] << 24)
                 | (data[startIndex + 2] << 16)
                 | (data[startIndex + 1] << 8)
                 | data[startIndex];
        }

        public static Int16 GetLittleEndianInt16FromByteArray(byte[] data, int startIndex)
        {
            string hexValue = data[startIndex + 1].ToString("X2") + data[startIndex].ToString("X2");
            return Int16.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
        }

        public static Vector3 GetLittleEndianVector3FromByteArray(byte[] data, int startIndex)
        {
            float x, y, z;

            x = (data[startIndex + 3] << 24) | (data[startIndex + 2] << 16) | (data[startIndex + 1] << 8) | (data[startIndex]);
            startIndex += 4;
            y = (data[startIndex + 3] << 24) | (data[startIndex + 2] << 16) | (data[startIndex + 1] << 8) | (data[startIndex]);
            startIndex += 4;
            z = (data[startIndex + 3] << 24) | (data[startIndex + 2] << 16) | (data[startIndex + 1] << 8) | (data[startIndex]);

            return new Vector3(x / 8, y / 8, z / 8);
        }

        public static void stoLittleEndian(short s, ref byte[] array, int off = 0)
        {
            Console.WriteLine("short to convert: " + s.ToString());
            Console.WriteLine("offset: " + off);
            array[0 + off] = (byte)(s & 0x00FF);
            array[1 + off] = (byte)((s & 0xFF00) >> 8);
            Console.WriteLine("result: " + string.Join(", ", array));
        }

        public static void ConvertFloatToLittleEndian(float f, ref byte[] array, int off = 0)
        {
            array[0] = (byte)f;
            array[1] = (byte)(((uint)f >> 8) & 0xFF);
            array[2] = (byte)(((uint)f >> 16) & 0xFF);
            array[3] = (byte)(((uint)f >> 24) & 0xFF);
        }

        public static void ConvertIntToLittleEndian(int i, ref byte[] array, int off = 0)
        {
            array[0 + off] = (byte)(i & 0x000000FF);
            array[1 + off] = (byte)((i & 0x0000FF00) >> 8);
            array[2 + off] = (byte)((i & 0x00FF0000) >> 16);
            array[3 + off] = (byte)((i & 0xFF000000) >> 24);
        }
        public static void ConvertShortToLittleEndian(short s, ref byte[] array, int off = 0)
        {
            array[0 + off] = (byte)(s & 0x000000FF);
            array[1 + off] = (byte)((s & 0x0000FF00) >> 8);
        }
        public static void ConvertUnsignedShorttoLittleEndian(ushort s, ref byte[] array, int off = 0)
        {
            array[0 + off] = (byte)(s & 0x000000FF);
            array[1 + off] = (byte)((s & 0x0000FF00) >> 8);
        }
    }
}
