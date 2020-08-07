using SampSharp.GameMode;
using SampSharp.GameMode.World;
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

        public struct PathNode
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

        public struct NaviNode
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

            public override string ToString()
            {
                return "Position: " + position.ToString() + "\r\n" +
                    "areaID: " + areaID + "\r\n" +
                    "nodeID: " + nodeID + "\r\n" +
                    "direction: " + direction.ToString() + "\r\n" +
                    "nodeFlag: " + flags.ToString("X") + "\r\n";
            }
        }

        struct Link
        {
            public UInt16 areaID;
            public UInt16 nodeID;

            public override string ToString()
            {
                return "areaID: " + areaID + "\r\n" +
                    "nodeID: " + nodeID + "\r\n";
            }
        }

        struct NaviLink
        {
            public UInt16 naviNodeID;
            public UInt16 areaID;

            public override string ToString()
            {
                return "naviNodeID: " + naviNodeID + "\r\n" +
                    "areaID: " + areaID + "\r\n";
            }
        }

        public static List<Vector3>[] pathRoad = new List<Vector3>[10000];
        public static List<PathNode>[] pathNodes = new List<PathNode>[64];
        public static List<NaviNode>[] naviNodes = new List<NaviNode>[64];




        public static List<Vector3> pathPoints = new List<Vector3>();
        public static Vector3[] tmpPathPoints;
        private static ushort[] dataPoints;

        public static void Load()
        {
            string heighmapFile = BaseMode.Instance.Client.ServerPath + "\\scriptfiles\\SAfull.hmap";
            using (FileStream fs = File.Open(heighmapFile, FileMode.Open, FileAccess.Read))
            {
                long mapLength = 6000 * 6000;
                long fsLen = fs.Length;
                dataPoints = new ushort[fsLen/2];
                Console.Write("Loading dataPoints ...");

                byte[] buffer;
                for(int i = 0; i < fsLen/2; i++)
                {
                    buffer = new byte[2];
                    for (int j = 0; j < 2; j++)
                        fs.Read(buffer, j, 1);
                    dataPoints[i] = (ushort)GetLittleEndianInt16FromByteArray(buffer, 0);
                }
                Console.WriteLine(" Done");
                fs.Close();
            }
        }

        public static void Extract(string path, int index)
        {
            try
            {
                string filename = path + "\\NODES" + index + ".DAT";
                using (FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read))
                {
                    Header header = new Header();
                    byte[] buffer = new byte[20];
                    for (int i = 0; i < 20; i++)
                    {
                        fs.Read(buffer, i, 1);
                    }
                    header.nodes = GetLittleEndianInt32FromByteArray(buffer, 0);
                    header.vehiclesNodes = GetLittleEndianInt32FromByteArray(buffer, 4);
                    header.pedNodes = GetLittleEndianInt32FromByteArray(buffer, 8);
                    header.naviNodes = GetLittleEndianInt32FromByteArray(buffer, 12);
                    header.links = GetLittleEndianInt32FromByteArray(buffer, 16);
                    //Console.WriteLine("== Header == \r\n" + header.ToString());

                    tmpPathPoints = new Vector3[header.naviNodes];

                    PathNode tmpPathNode;
                    pathNodes[index] = new List<PathNode>();

                    for (int j = 0; j < header.nodes; j++)
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
                        pathNodes[index].Add(tmpPathNode);
                        //Console.WriteLine("== PathNode " + j + " == \r\n" + tmpPathNode.ToString());
                    }

                    NaviNode tmpNaviNode;
                    naviNodes[index] = new List<NaviNode>();

                    for (int j = 0; j < header.naviNodes; j++)
                    {
                        buffer = new byte[14];
                        for (int i = 0; i < 14; i++)
                        {
                            fs.Read(buffer, i, 1);
                            //Console.WriteLine("buffer[" + i + "] {0:X}", buffer[i]);
                        }

                        tmpNaviNode = new NaviNode();
                        tmpNaviNode.position = new Vector2(GetLittleEndianInt16FromByteArray(buffer, 0) / 8, GetLittleEndianInt16FromByteArray(buffer, 2) / 8);
                        tmpNaviNode.areaID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 4);
                        tmpNaviNode.nodeID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 6);
                        tmpNaviNode.direction = new Vector2(sbyte.Parse(buffer[8].ToString("X"), System.Globalization.NumberStyles.HexNumber), sbyte.Parse(buffer[9].ToString("X"), System.Globalization.NumberStyles.HexNumber));
                        tmpNaviNode.flags = GetLittleEndianInt32FromByteArray(buffer, 10);
                        naviNodes[index].Add(tmpNaviNode);
                        //Console.WriteLine("== NaviNode " + j + " == \r\n" + tmpNaviNode.ToString());
                        pathPoints.Add(new Vector3(tmpNaviNode.position.X, tmpNaviNode.position.Y, GetAverageZ(tmpNaviNode.position.X, tmpNaviNode.position.Y) + 20.0));
                    }

                    Link[] links = new Link[header.links];
                    Link tmpLink;

                    for (int j = 0; j < header.links; j++)
                    {
                        buffer = new byte[4];
                        for (int i = 0; i < 4; i++)
                        {
                            fs.Read(buffer, i, 1);
                            //Console.WriteLine("buffer[" + i + "] {0:X}", buffer[i]);
                        }

                        tmpLink = new Link();
                        tmpLink.areaID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 0);
                        tmpLink.nodeID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 2);
                        links[j] = tmpLink;
                        //Console.WriteLine("== Link " + j + " == \r\n" + tmpLink.ToString());
                    }

                    for (int i = 0; i < 768; i++)
                        fs.ReadByte();

                    NaviLink[] naviLinks = new NaviLink[512];
                    NaviLink tmpNaviLink;

                    for (int j = 0; j < 512; j++)
                    {
                        buffer = new byte[2];
                        for (int i = 0; i < 2; i++)
                        {
                            fs.Read(buffer, i, 1);
                            //Console.WriteLine("buffer[" + i + "] {0:X}", buffer[i]);
                        }

                        tmpNaviLink = new NaviLink();
                        UInt16 tmp = (ushort)GetLittleEndianInt16FromByteArray(buffer, 0);

                        tmpNaviLink.areaID = (ushort)Convert.ToInt16(tmp >> 6);
                        tmpNaviLink.naviNodeID = (ushort)Convert.ToInt16(tmp & 0b0000000000111111);
                        naviLinks[j] = tmpNaviLink;
                        //Console.WriteLine("== NaviLink " + j + " == \r\n" + tmpNaviLink.ToString());
                    }

                    //Console.WriteLine("PathExtractor.cs - PathExtractor.Extract:I: Path loaded from " + fs.Name);
                    //Console.WriteLine("PathExtractor.cs - PathExtractor.Extract:I: Path points: " + tmpPathPoints.Length);
                    fs.Close();
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("PathExtractor.cs - PathExtractor.Extract:E: " + e);
            }
        }

        public static void CalculateRoads()
        {

        }

        public static float FindZFromVector2(float x, float y)
        {
            if (x < -3000.0f || x > 3000.0f || y > 3000.0f || y < -3000.0f) return 0.0f;
            if (dataPoints != null)
            {
                int iGridX = ((int)x) + 3000;
                int iGridY = (((int)y) - 3000) * -1;
                int iDataPos;
                iDataPos = (iGridY * 6000) + iGridX; // for every Y, increment by the number of cols, add the col index.
                return (float)(dataPoints[iDataPos] / 100.0f); // the data is a float stored as ushort * 100
            }
            else return 0.0f;
        }

        public static float GetAverageZ(float x, float y)
        {
            float p2;
            float p3;
            float xx;
            float yy;
            float m_gridSize = 1.0f;

            // Get the Z value of 2 neighbor grids
            float p1 = FindZFromVector2(x, y);
            if (x < 0.0f) p2 = FindZFromVector2(x + m_gridSize, y);
            else p2 = FindZFromVector2(x - m_gridSize, y);
            if (y < 0.0f) p3 = FindZFromVector2(x, y + m_gridSize);
            else p3 = FindZFromVector2(x, y - m_gridSize);

            // Filter the decimal part only
            xx = (float)(x - Convert.ToInt32(x));
            yy = (float)(x - Convert.ToInt32(x));
            if (xx < 0) x = -xx;
            if (yy < 0) y = -yy;

            float result;
            // Calculate a linear approximation of the z coordinate
            result = p1 + xx * (p1 - p2) + yy * (p1 - p3);

            return (float)result;
        }
    }
}
