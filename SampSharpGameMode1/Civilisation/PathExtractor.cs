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
            string hexValue = data[startIndex + 1].ToString("X") + data[startIndex].ToString("X");
            return Int16.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
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
            public Vector2 position;
            public UInt16 areaID;
            public UInt16 nodeID;
            public Vector2 direction;
            public int flags;
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
            public override string ToString()
            {
                return "Position: " + position.ToString() + "\r\n" +
                    "areaID: " + areaID + "\r\n" +
                    "nodeID: " + nodeID + "\r\n" +
                    "direction: " + direction.ToString() + "\r\n" +
                    "nodeFlag: " + flags.ToString("X") + "\r\n";
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
            public Vector3 position;
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
                    "nodeFlag: " + nodeFlag.ToString("X") + "\r\n";
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
                        fs.Read(buffer, i, 1);
                    }
                    header.nodes = GetLittleEndianInt32FromByteArray(buffer, 0);
                    header.vehiclesNodes = GetLittleEndianInt32FromByteArray(buffer, 4);
                    header.pedNodes = GetLittleEndianInt32FromByteArray(buffer, 8);
                    header.naviNodes = GetLittleEndianInt32FromByteArray(buffer, 12);
                    header.links = GetLittleEndianInt32FromByteArray(buffer, 16);
                    Console.WriteLine("== Header == \r\n" + header.ToString());

                    PathNode[] pathNodes = new PathNode[header.nodes];
                    PathNode tmpPathNode;

                    for (int j=0; j < header.nodes; j++)
                    {
                        buffer = new byte[28];
                        for (int i = 0; i < 28; i++)
                        {
                            fs.Read(buffer, i, 1);
                        }
                        tmpPathNode = new PathNode();
                        tmpPathNode.position = new Vector3(GetLittleEndianInt16FromByteArray(buffer, 8) / 8, GetLittleEndianInt16FromByteArray(buffer, 10) / 8, GetLittleEndianInt16FromByteArray(buffer, 12) / 8);
                        tmpPathNode.linkID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 16);
                        tmpPathNode.areaID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 18);
                        tmpPathNode.nodeID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 20);
                        tmpPathNode.pathWidth = buffer[22];
                        tmpPathNode.nodeType = buffer[23];
                        tmpPathNode.flags = GetLittleEndianInt32FromByteArray(buffer, 24);
                        pathNodes[j] = tmpPathNode;
                        Console.WriteLine("== PathNode " + j + " == \r\n" + tmpPathNode.ToString());
                    }

                    NaviNode[] naviNodes = new NaviNode[header.naviNodes];
                    NaviNode tmpNaviNode;

                    for (int j = 0; j < header.naviNodes; j++)
                    {
                        buffer = new byte[14];
                        for (int i = 0; i < 14; i++)
                        {
                            fs.Read(buffer, i, 1);
                            Console.WriteLine("buffer[" + i + "] {0:X}", buffer[i]);
                        }

                        tmpNaviNode = new NaviNode();
                        tmpNaviNode.position = new Vector2(GetLittleEndianInt16FromByteArray(buffer, 0) / 8, GetLittleEndianInt16FromByteArray(buffer, 2) / 8);
                        tmpNaviNode.areaID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 4);
                        tmpNaviNode.nodeID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 6);
                        tmpNaviNode.direction = new Vector2(sbyte.Parse(buffer[8].ToString("X"), System.Globalization.NumberStyles.HexNumber), sbyte.Parse(buffer[9].ToString("X"), System.Globalization.NumberStyles.HexNumber));
                        tmpNaviNode.flags = GetLittleEndianInt32FromByteArray(buffer, 10);
                        Console.WriteLine("== NaviNode " + j + " == \r\n" + tmpNaviNode.ToString());
                    }

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
