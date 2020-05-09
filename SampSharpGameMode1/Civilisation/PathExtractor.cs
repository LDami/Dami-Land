using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SampSharpGameMode1.Civilisation
{
    class PathExtractor
    {
        //https://gta.fandom.com/wiki/Paths_%28GTA_SA%29
        static void itoLittleEndian(int i, ref byte[] array, int off = 0)
        {
            array[0 + off] = (byte)(i & 0x000000FF);
            array[1 + off] = (byte)((i & 0x0000FF00) >> 8);
            array[2 + off] = (byte)((i & 0x00FF0000) >> 16);
            array[3 + off] = (byte)((i & 0xFF000000) >> 24);
        }

        static int GetLittleEndianInt32FromByteArray(byte[] data, int startIndex)
        {
            return (data[startIndex + 3] << 24)
                 | (data[startIndex + 2] << 16)
                 | (data[startIndex + 1] << 8)
                 | data[startIndex];
        }

        static Int16 GetLittleEndianInt16FromByteArray(byte[] data, int startIndex)
        {
            return Convert.ToInt16((data[startIndex + 1] << 8) | data[startIndex]);
        }

        static Vector3 GetLittleEndianVector3FromByteArray(byte[] data, int startIndex)
        {
            float x, y, z;

            x = (data[startIndex + 3] << 24) | (data[startIndex + 2] << 16) | (data[startIndex + 1] << 8) | (data[startIndex]);
            startIndex += 4;
            y = (data[startIndex + 3] << 24) | (data[startIndex + 2] << 16) | (data[startIndex + 1] << 8) | (data[startIndex]);
            startIndex += 4;
            z = (data[startIndex + 3] << 24) | (data[startIndex + 2] << 16) | (data[startIndex + 1] << 8) | (data[startIndex]);

            return new Vector3(x/8, y/8, z/8);
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
        struct NaviNode
        {
            Vector2 position; // diviser par 8
            UInt16 areaID;
            UInt16 nodeID;
            Vector2 direction;
            int flags;
            /*
             *  0- 7 - unknown
             *  8-10 - number of left lanes
             *  11-13 - number of right lanes
             *  14 - traffic light direction behavior
             *  15 - zero/unused
             *  16,17 - traffic light behavior
             *  18-31 - zero/unused
            */

            public byte[] ToBinary()
            {
                byte[] result = new byte[14];

                return result;
            }
        }
        struct Links
        {
            UInt16 areaID;
            UInt16 nodeID;
            public byte[] ToBinary()
            {
                byte[] result = new byte[14];

                itoLittleEndian(areaID, ref result, 0);
                itoLittleEndian(nodeID, ref result, 2);

                return result;
            }
        }
        struct Header
        {
            public int nodes; // nodes = vehiclesNodes + pedNodes
            public int vehiclesNodes;
            public int pedNodes;
            public int naviNodes;
            public int links;

            public override string ToString()
            {
                return "Nodes: " + nodes + "\r\n" +
                    "Vehicles Nodes: " + vehiclesNodes + "\r\n" +
                    "Ped Nodes: " + pedNodes + "\r\n" +
                    "Navi Nodes: " + naviNodes + "\r\n" +
                    "Links: " + links + "\r\n";
            }
        }

        struct PathNode
        {
            public Vector3 position; // diviser par 8
            public UInt16 linkID;
            public UInt16 areaID;
            public UInt16 nodeID;
            public byte pathWidth;
            public byte nodeType; // 1: vehicle, 2: boats
            public int flags;

            public int linkCount { get { return flags & 0x000F; } }
            public int nodeFlag { get { return flags >> 4; } }

            public override string ToString()
            {
                return "Position: " + position.ToString() + "\r\n" +
                    "linkID: " + linkID + "\r\n" +
                    "areaID: " + areaID + "\r\n" +
                    "nodeID: " + nodeID + "\r\n" +
                    "pathWidth: " + pathWidth + "\r\n" +
                    "nodeType: " + nodeType + "\r\n" +
                    "linkCount: " + linkCount + "\r\n" +
                    "nodeFlag: " + nodeFlag + "\r\n";
            }
        }

        public static void Extract(string filename)
        {
            try
            {
                using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    Header header = new Header();
                    byte[] buffer = new byte[20];
                    for(int i=0; i < 20; i++)
                    {
                        fs.Read(buffer, 0, 20);
                    }
                    header.nodes = GetLittleEndianInt32FromByteArray(buffer, 0);
                    header.vehiclesNodes = GetLittleEndianInt32FromByteArray(buffer, 4);
                    header.pedNodes = GetLittleEndianInt32FromByteArray(buffer, 8);
                    header.naviNodes = GetLittleEndianInt32FromByteArray(buffer, 12);
                    header.links = GetLittleEndianInt32FromByteArray(buffer, 16);
                    Console.WriteLine("Header: \r\n" + header.ToString());

                    buffer = new byte[28];
                    for (int i = 0; i < 28; i++)
                    {
                        fs.Read(buffer, 0, 28);
                    }

                    PathNode pathNode = new PathNode();
                    pathNode.position = GetLittleEndianVector3FromByteArray(buffer, 8);
                    pathNode.linkID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 14);
                    pathNode.areaID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 18);
                    pathNode.nodeID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 20);
                    pathNode.pathWidth = buffer[21];
                    pathNode.nodeType = buffer[22];
                    pathNode.flags = GetLittleEndianInt32FromByteArray(buffer, 23);
                    Console.WriteLine("PathNode: \r\n" + pathNode.ToString());

                    fs.Close();
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("NPC.cs - NPC.Create:E: " + e);
            }
        }
    }
}
