using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Civilisation
{
    class FileEncoding
    {
        public static void ConvertIntToLittleEndian(int i, ref byte[] array, int off = 0)
        {
            array[0 + off] = (byte)(i & 0x000000FF);
            array[1 + off] = (byte)((i & 0x0000FF00) >> 8);
            array[2 + off] = (byte)((i & 0x00FF0000) >> 16);
            array[3 + off] = (byte)((i & 0xFF000000) >> 24);
        }
        public static void ConvertShorttoLittleEndian(short s, ref byte[] array, int off = 0)
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
