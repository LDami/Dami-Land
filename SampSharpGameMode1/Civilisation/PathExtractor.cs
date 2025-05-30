﻿using Newtonsoft.Json;
using SampSharp.GameMode;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static SampSharpGameMode1.BinaryParser;

namespace SampSharpGameMode1.Civilisation
{
    public class PathExtractor
    {
        //https://gta.fandom.com/wiki/Paths_%28GTA_SA%29
        public struct Header
        {
            public int nodes; // nodes = vehiclesNodes + pedNodes
            public int vehiclesNodes;
            public int pedNodes;
            public int naviNodes;
            public int links;

            public override string ToString()
            {
                return "{ Nodes: " + nodes + ", Veh Nodes: " + vehiclesNodes + ", Ped Nodes: " + pedNodes + ", Navi Nodes: " + naviNodes + ", Links: " + links + " }";
            }
        }

        public struct LinkInfo
        {
            public PathNode targetNode;
            public NaviNode naviNodeLink;
        }

        public struct PathNodeFlag
        {
            public int linkCount;
            public int trafficLevel; // 0 = full 1 = high 2 = medium 3 = low
            public bool roadBlocks;
            public bool isWater;
            public bool emergencyOnly;
            public bool isHighway;
            public int spawnProbability;
            public override string ToString()
            {
                return "linkCount: " + linkCount + "\r\n" +
                    "trafficLevel: " + trafficLevel + "\r\n" +
                    "roadBlocks: " + roadBlocks + "\r\n" +
                    "isWater: " + isWater + "\r\n" +
                    "emergencyOnly: " + emergencyOnly + "\r\n" +
                    "isHighway: " + isHighway + "\r\n" +
                    "spawnProbability: " + spawnProbability + "\r\n";
            }
        }

        public struct PathNode
        {
            public string id;
            public int index;
            public Vector3 position;
            public UInt16 linkID;
            public UInt16 areaID;
            public UInt16 nodeID;
            public byte pathWidth;
            public byte nodeType; // 1: vehicle, 2: boats
            public PathNodeFlag flags;

            public List<LinkInfo> links;

            public override string ToString()
            {
                return "ID: " + id + "\r\n" +
                    "Position: " + position.ToString() + "\r\n" +
                    "linkID: " + linkID + "\r\n" +
                    "areaID: " + areaID + "\r\n" +
                    "nodeID: " + nodeID + "\r\n" +
                    "pathWidth: " + pathWidth + "\r\n" +
                    "nodeType: " + nodeType;
            }
        }

        public struct NaviNode
        {
            public int id;
            public Vector2 position;
            public UInt16 targetAreaID;
            public UInt16 targetNodeID;
            public Vector2 direction;
            public int flags;
            /*
             *  0- 7 - unknown
             *  8-10 - number of left lanes
             *  11-13 - number of right lanes
             *  14 - traffic light direction behavior
             *  15 - zero/unused
             *  16,17 - traffic light behavior
             *  18 - train crossing
             *  19-31 - zero/unused
            */

            public PathNode navigationTarget;

            public override string ToString()
            {
                return "ID: " + id + "\r\n" +
                    "Position: " + position.ToString() + "\r\n" +
                    "areaID: " + targetAreaID + "\r\n" +
                    "nodeID: " + targetNodeID + "\r\n" +
                    "direction: " + direction.ToString() + "\r\n" +
                    "nodeFlag: " + flags.ToString("X") + "\r\n";
            }
        }

        public struct Link
        {
            public UInt16 areaID;
            public UInt16 nodeID;

            public override string ToString()
            {
                return "areaID: " + areaID + "\r\n" +
                    "nodeID: " + nodeID + "\r\n";
            }
        }

        public struct NaviLink
        {
            public UInt16 naviNodeID;
            public UInt16 areaID;

            public override string ToString()
            {
                return "naviNodeID: " + naviNodeID + "\r\n" +
                    "areaID: " + areaID + "\r\n";
            }
        }

        public enum NodeType : byte
        {
            Cars = 1,
            Boats = 2,
            Peds = 100
        }
        
        public static Header[] headers = new Header[64];
        public static List<PathNode>[] pathNodes = new List<PathNode>[64];
        public static List<NaviNode>[] naviNodes = new List<NaviNode>[64];
        public static List<Link>[] links = new List<Link>[64];
        public static List<NaviLink>[] naviLinks = new List<NaviLink>[64];

        public static List<PathNode> carNodes = new List<PathNode>();
        public static List<PathNode> boatNodes = new List<PathNode>();
        public static List<PathNode> pedNodes = new List<PathNode>();

        public static float?[][] nodeBorders = new float?[64][];


        public static List<Vector3> pathPoints = new List<Vector3>();
        public static Vector3[] tmpPathPoints;
        private static ushort[] dataPoints;

        public static void Load()
        {
            string heightmapFile = Directory.GetCurrentDirectory() + "/scriptfiles/SAfull.hmap";
            using (FileStream fs = File.Open(heightmapFile, FileMode.Open, FileAccess.Read))
            {
                long fsLen = fs.Length;
                dataPoints = new ushort[fsLen/2];
                Logger.WriteAndClose("Loading dataPoints ...");
                
                byte[] buffer;
                for(int i = 0; i < fsLen/2; i++)
                {
                    buffer = new byte[2];
                    for (int j = 0; j < 2; j++)
                        fs.Read(buffer, j, 1);
                    dataPoints[i] = (ushort)GetLittleEndianInt16FromByteArray(buffer, 0);
                }

                Logger.WriteLineAndClose(" Done !");
                fs.Close();
            }
        }

        public static void Extract(string path, int index)
        {
            if(path.Length > 0 && index >= 0 && index < 64)
            {
                try
                {
                    string filename = path + "/NODES" + index + ".DAT";
                    Random rdm = new Random();
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
                        headers[index] = header;
                        //Console.WriteLine("== Header == \r\n" + header.ToString());

                        tmpPathPoints = new Vector3[header.naviNodes];

                        PathNode tmpPathNode;
                        int tmpFlag;
                        pathNodes[index] = new List<PathNode>();

                        //Logger.WriteLine(" == Area ID: " + index + " == ");
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
                            tmpFlag = GetLittleEndianInt32FromByteArray(buffer, 24);
                            tmpPathNode.id = (tmpPathNode.areaID.ToString() + "_" + tmpPathNode.nodeID.ToString() + "_" + rdm.Next(1000));
                            tmpPathNode.index = j;
                            tmpPathNode.links = new List<LinkInfo>();

                            tmpPathNode.flags.linkCount = tmpFlag & 0xF;
                            tmpPathNode.flags.trafficLevel = (tmpFlag & 0x30) >> 4;
                            tmpPathNode.flags.roadBlocks = Convert.ToBoolean(tmpFlag & 0x40);
                            tmpPathNode.flags.isWater = Convert.ToBoolean(tmpFlag & 0x80);
                            tmpPathNode.flags.emergencyOnly = Convert.ToBoolean(tmpFlag & 0x100);
                            tmpPathNode.flags.isHighway = !Convert.ToBoolean(tmpFlag & 0x1000);
                            tmpPathNode.flags.spawnProbability = (tmpFlag & 0xF0000) >> 16;

                            //if (index > 40) Logger.WriteLine("Area ID: " + tmpPathNode.areaID + "\tNode ID: " + tmpPathNode.nodeID);
                            //if ((NodeType)tmpPathNode.nodeType == NodeType.Cars)
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
                            tmpNaviNode.targetAreaID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 4);
                            tmpNaviNode.targetNodeID = (ushort)GetLittleEndianInt16FromByteArray(buffer, 6);
                            tmpNaviNode.direction = new Vector2(sbyte.Parse(buffer[8].ToString("X"), System.Globalization.NumberStyles.HexNumber), sbyte.Parse(buffer[9].ToString("X"), System.Globalization.NumberStyles.HexNumber));
                            tmpNaviNode.direction = new Vector2(tmpNaviNode.direction.X / 100, tmpNaviNode.direction.Y / 100);
                            tmpNaviNode.flags = GetLittleEndianInt32FromByteArray(buffer, 10);
                            tmpNaviNode.id = Convert.ToInt32(tmpNaviNode.targetAreaID.ToString() + tmpNaviNode.targetNodeID.ToString() + rdm.Next(1000));
                            naviNodes[index].Add(tmpNaviNode);
                            //Console.WriteLine("== NaviNode " + j + " == \r\n" + tmpNaviNode.ToString());
                            pathPoints.Add(new Vector3(tmpNaviNode.position.X, tmpNaviNode.position.Y, GetAverageZ(tmpNaviNode.position.X, tmpNaviNode.position.Y)));
                        }

                        Link tmpLink;
                        links[index] = new List<Link>();
                        //if (index == 54) Logger.WriteLineAndClose("== Links ==");
                        List<int> areasInLink = new List<int>();
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
                            links[index].Add(tmpLink);
                            if (!areasInLink.Contains(tmpLink.areaID))
                                areasInLink.Add(tmpLink.areaID);
                            //Console.WriteLine("== Link " + j + " == \r\n" + tmpLink.ToString() + "node exists: " + ((linkedNode.Count == 0) ? "false" : "true") + "\r\n");
                        }

                        for (int i = 0; i < 768; i++)
                            fs.ReadByte();

                        NaviLink tmpNaviLink;
                        naviLinks[index] = new List<NaviLink>();

                        for (int j = 0; j < header.links; j++)
                        {
                            buffer = new byte[2];
                            for (int i = 0; i < 2; i++)
                            {
                                fs.Read(buffer, i, 1);
                                //Console.WriteLine("buffer[" + i + "] {0:X}", buffer[i]);
                            }

                            tmpNaviLink = new NaviLink();
                            UInt16 tmp = (ushort)GetLittleEndianInt16FromByteArray(buffer, 0);

                            tmpNaviLink.areaID = (ushort)Convert.ToInt16(tmp >> 10);
                            tmpNaviLink.naviNodeID = (ushort)Convert.ToInt16(tmp & 0b0000000000111111);
                            naviLinks[index].Add(tmpNaviLink);
                            //Console.WriteLine("== NaviLink " + j + " == \r\n" + tmpNaviLink.ToString());
                        }

                        int row, col;
                        row = (int)(index % 8);
                        col = (int)(index / 8);

                        nodeBorders[index] = new float?[] { -3000 + (750 * row), -3000 + (750 * col) };

                        //Logger.WriteLineAndClose("PathExtractor.cs - PathExtractor.Extract:I: Area " + index + " loaded");
                        //Logger.WriteLineAndClose(headers[index].ToString());

                        //Console.WriteLine("PathExtractor.cs - PathExtractor.Extract:I: Path loaded from " + fs.Name);
                        //Console.WriteLine("PathExtractor.cs - PathExtractor.Extract:I: Path points: " + tmpPathPoints.Length);
                        fs.Close();
                    }
                }
                catch (IOException e)
                {
                    Logger.WriteLineAndClose("PathExtractor.cs - PathExtractor.Extract:E: " + e);
                }
            }
        }

        public static int GetArea(Vector3 position)
        {
            for (int i = 0; i < 64; i++)
            {
                try
                {
                    if (position.X > (PathExtractor.nodeBorders[i][0] ?? 0.0f) && position.X < (PathExtractor.nodeBorders[i][0] ?? 0.0f) + 750
                        && position.Y > (PathExtractor.nodeBorders[i][1] ?? 0.0f) && position.Y < (PathExtractor.nodeBorders[i][1] ?? 0.0f) + 750)
                    {
                        return i;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("PathExtractor.cs - PathExtractor.GetArea:E: PathExtractor.nodeBorders[" + i + "] is null");
                }
            }
            return -1;
        }

        // Renvoi tous les area autour de areaID
        private static List<int> GetAreaNeighborhood(int areaID)
        {
            List<int> result = new List<int>();
            int indexX = (areaID + 1) % 8;
            int indexY = Convert.ToInt32(Math.Truncate(Convert.ToDecimal((areaID + 1) / 8)));
            if (indexY == 8) indexY = 7;

            int aW, aNW, aN, aNE, aE, aSE, aS, aSW;
            aW = (indexX == 1) ? -1 : (areaID - 1);
            aNW = (indexX == 1) ? -1 : ((indexY == 7) ? -1 : (areaID + 7));
            aN = (indexY == 7) ? -1 : (areaID + 8);
            aNE = (indexX == 0) ? -1 : ((indexY == 7) ? -1 : (areaID + 9));
            aE = (indexX == 0) ? -1 : (areaID + 1);
            aSE = (indexX == 0) ? -1 : ((indexY == 0) ? -1 : (areaID - 7));
            aS = (indexY == 0) ? -1 : (areaID - 8);
            aSW = (indexX == 1) ? -1 : ((indexY == 0) ? -1 : (areaID - 9));

            result.Add(aW);
            result.Add(aNW);
            result.Add(aN);
            result.Add(aNE);
            result.Add(aE);
            result.Add(aSE);
            result.Add(aS);
            result.Add(aSW);

            return result;
        }
        public static void CheckLinks(int areaID)
        {
            int nbrOfExist = 0;
            int nbrOfUnknown = 0;
            List<PathNode> linkedNode;
            bool exists;

            for (int i = 0; i < headers[areaID].links; i++)
            {
                //Console.Write("Looking for node " + links[areaID][i].nodeID);
                exists = false;
                foreach(int area2 in GetAreaNeighborhood(areaID))
                {
                    if(area2 > -1)
                    {
                        if (pathNodes[area2] != null)
                        {
                            linkedNode = pathNodes[area2].FindAll(e => e.nodeID == links[areaID][i].nodeID);
                            if (linkedNode.Count > 0)
                            {
                                exists = true;
                                break;
                            }
                        }
                        else Logger.WriteLineAndClose("pathnodes in area " + area2 + " not exists");
                    }
                }
                //if (!exists) Console.WriteLine("Looking for node " + links[areaID][i].nodeID + " NOK");
                if (!exists)
                    nbrOfUnknown++;
                else
                    nbrOfExist++;
            }
            //if(nbrOfUnknown > 0)
            //    Logger.WriteLineAndClose("PathExtractor.cs - PathExtractor.CheckLinks:W: Area " + areaID + " : " + nbrOfUnknown + " of " + (nbrOfUnknown+nbrOfExist).ToString() + " links are not resolvable");
        }
        public static void SeparateNodes(int areaID)
        {
            int nbrOfLinks = 0;
            //Logger logger = new Logger();
            for (int i = 0; i < pathNodes[areaID].Count - 1; i++)
            {
                PathNode node = pathNodes[areaID][i];

                List<LinkInfo> nodeLinks;
                LinkInfo linkInfo;

                int linkIndex;

                //Console.WriteLine("PathExtractor.cs - PathExtractor.SeparateNodes:I: NodePath = " + node.id);

                for (int j = 0; j < node.flags.linkCount; j++)
                {
                    linkIndex = node.linkID + j;
                    linkInfo = new LinkInfo();

                    linkInfo.targetNode = pathNodes[links[areaID][linkIndex].areaID].Find(e => e.nodeID == links[areaID][linkIndex].nodeID);

                    if (i < headers[areaID].vehiclesNodes)
                    {
                        linkInfo.naviNodeLink = naviNodes[naviLinks[areaID][linkIndex].areaID].Find(e => e.targetNodeID == naviLinks[areaID][linkIndex].naviNodeID);
                    }
                    
                    pathNodes[areaID][i].links.Add(linkInfo);
                    //logger.WriteLine("PathExtractor.cs - PathExtractor.SeparateNodes:I: PathNode ID " + pathNodes[areaID][i].id + " (aera " + pathNodes[areaID][i].areaID + ")(node " + pathNodes[areaID][i].nodeID + ") linked !");
                    //Console.WriteLine("PathExtractor.cs - PathExtractor.SeparateNodes:I: Linked target node = " + linkInfo.targetNode.id);
                    List<PathNode> linkedNode;
                    linkedNode = pathNodes[links[areaID][linkIndex].areaID].FindAll(e => e.id == linkInfo.targetNode.id);
                    //if(linkedNode.Count == 0)
                        //logger.WriteLine("PathExtractor.cs - PathExtractor.SeparateNodes:E: " + linkInfo.targetNode.ToString() + " does not exists !");
                    nbrOfLinks++;
                }

                if (i < headers[areaID].vehiclesNodes)
                {
                    if (node.flags.isWater)
                        boatNodes.Add(node);
                    else
                        carNodes.Add(node);
                }
                else
                    pedNodes.Add(node);
            }
            //Logger.WriteLineAndClose("PathExtractor.cs - PathExtractor.SeparateNodes:I: Number of links = " + nbrOfLinks);

            for(int i = 0; i < headers[areaID].naviNodes; i++)
            {
                NaviNode tmp = naviNodes[areaID][i];
                tmp.navigationTarget = pathNodes[tmp.targetAreaID].Find(e => e.nodeID == tmp.targetNodeID);
            }
            //logger.Close();
        }

        public static void ValidateNaviLink()
        {
            Logger.WriteLineAndClose("PathExtractor.cs - PathExtractor.ValidateNaviLink:I: Starting navi link validation", true);
            int errors = 0;

            PathNode linkNode;
            NaviNode naviLinkNode;
            foreach (PathNode node in carNodes)
            {
                foreach(LinkInfo linkInfo in node.links)
                {
                    linkNode = linkInfo.targetNode;
                    naviLinkNode = linkInfo.naviNodeLink;
                    if(node.Equals(naviLinkNode.navigationTarget))
                    {
                        //Logger.WriteLineAndClose("Linked node: " + linkNode.position.ToString());
                        //Logger.WriteLineAndClose("Navi target: " + naviLinkNode.navigationTarget.position.ToString());
                        errors++;
                    }
                }
            }
            Logger.WriteLineAndClose("PathExtractor.cs - PathExtractor.ValidateNaviLink:" + ((errors > 0) ? "W" : "I") + ": Navi link validation ended, " + errors + " errors", true);
        }

        public static List<string> GetLinkedNode(int areaID, string id)
        {
            PathNode node = pathNodes[areaID].Find(e => e.id.Equals(id));
            if(node.links != null)
            {
                if (node.links.Count > 0)
                {
                    List<string> result = new List<string>();
                    foreach (LinkInfo li in node.links)
                    {
                        result.Add("Linked Node: " + li.targetNode.id + " and linked NaviNode: " + li.naviNodeLink.id);
                    }
                    return result;
                }
            }
            return null;
        }

        public struct LinkInfoWithID
        {
            public string targetNodeID;
            public NaviNode naviNodeLink;
        }

        public struct PathNodeWithLinkID
        {
            public string id;
            public int index;
            public Vector3 position;
            public UInt16 linkID;
            public UInt16 areaID;
            public UInt16 nodeID;
            public byte pathWidth;
            public byte nodeType; // 1: vehicle, 2: boats
            public PathNodeFlag flags;

            public List<LinkInfoWithID> links;

            public override string ToString()
            {
                return "ID: " + id + "\r\n" +
                    "Position: " + position.ToString() + "\r\n" +
                    "linkID: " + linkID + "\r\n" +
                    "areaID: " + areaID + "\r\n" +
                    "nodeID: " + nodeID + "\r\n" +
                    "pathWidth: " + pathWidth + "\r\n" +
                    "nodeType: " + nodeType;
            }
        }

        public static void ExportPathNodesToJSONFile(string filename)
        {
            using(FileStream fs = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                List<PathNodeWithLinkID> pn = new List<PathNodeWithLinkID>();
                for (int i = 0; i < 64; i++)
                {
                    foreach (PathNode pathNode in pathNodes[i])
                    {
                        PathNodeWithLinkID p = new PathNodeWithLinkID();
                        p.id = pathNode.id;
                        p.index = pathNode.index;
                        p.position = pathNode.position;
                        p.linkID = pathNode.linkID;
                        p.areaID = pathNode.areaID;
                        p.nodeID = pathNode.nodeID;
                        p.pathWidth = pathNode.pathWidth;
                        p.nodeType = pathNode.nodeType;
                        p.flags = pathNode.flags;
                        p.links = new List<LinkInfoWithID>();
                        foreach(LinkInfo link in pathNode.links)
                        {
                            LinkInfoWithID linkInfo = new LinkInfoWithID();
                            linkInfo.naviNodeLink = link.naviNodeLink;
                            linkInfo.targetNodeID = link.targetNode.id;
                            p.links.Add(linkInfo); 
                        }
                        pn.Add(p);
                    }
                    Logger.WriteLineAndClose("Export done for NODES" + i + ".dat");
                }
                string str = JsonConvert.SerializeObject(pn);
                byte[] buffer = ASCIIEncoding.UTF8.GetBytes(str);
                fs.Write(buffer);
            }
            Logger.WriteLineAndClose("Export done for all");
        }

        public static void ExportPathNodesToSQL()
        {
            /*
            Dictionary<string, object> param;
            
            for (int i = 0; i < 64; i++)
            {
                foreach (PathNode pathNode in pathNodes[i])
                {
                    int flagInt = 0;
                    flagInt |= (pathNode.flags.linkCount & 0xF);
                    flagInt |= ((pathNode.flags.trafficLevel & 0x3) << 4);
                    if (pathNode.flags.roadBlocks)
                        flagInt |= (1 << 6);
                    if (pathNode.flags.isWater)
                        flagInt |= (1 << 7);
                    if (pathNode.flags.emergencyOnly)
                        flagInt |= (1 << 8);
                    if (!pathNode.flags.isHighway)
                        flagInt |= (1 << 12);
                    if (pathNode.flags.isHighway)
                        flagInt |= (1 << 13);
                    flagInt |= ((pathNode.flags.spawnProbability & 0xF) << 16);

                    param = new Dictionary<string, object>
                    {
                        { "@pos_x", pathNode.position.X },
                        { "@pos_y", pathNode.position.Y },
                        { "@pos_z", pathNode.position.Z },
                        { "@link_id", pathNode.linkID },
                        { "@area_id", pathNode.areaID },
                        { "@index", pathNode.nodeID },
                        { "@path_width", pathNode.pathWidth },
                        { "@type", pathNode.nodeType },
                        { "@flags", flagInt },
                    };
                    GameMode.mySQLConnector.Execute("INSERT INTO map_pathnodes " +
                        "(node_pos_x, node_pos_y, node_pos_z, node_link_id, node_area_id, node_index, node_path_width, node_type, node_flags) VALUES " +
                        "(@pos_x, @pos_y, @pos_z, @link_id, @area_id, @index, @path_width, @type, @flags)", param);
                }
                Logger.WriteLineAndClose("SQL Export done for nodes in NODES" + i + ".dat");
            }

            Logger.WriteLineAndClose("Starting Link export ...");
            Dictionary<string, string> row;
            for (int i = 0; i < 64; i++)
            {
                foreach (PathNode pathNode in pathNodes[i])
                {
                    foreach (LinkInfo link in pathNode.links)
                    {
                        param = new Dictionary<string, object>
                        {
                            { "@areaId", pathNode.areaID },
                            { "@index", pathNode.nodeID }
                        };
                        GameMode.mySQLConnector.OpenReader("SELECT node_id FROM map_pathnodes WHERE node_area_id = @areaId AND node_index = @index", param);
                        row = GameMode.mySQLConnector.GetNextRow();
                        if(row.Count > 0)
                        {
                            int pId = Convert.ToInt32(row["node_id"]);
                            GameMode.mySQLConnector.CloseReader();
                            param = new Dictionary<string, object>
                            {
                                { "@areaId", link.targetNode.areaID },
                                { "@index", link.targetNode.nodeID }
                            };
                            GameMode.mySQLConnector.OpenReader("SELECT node_id FROM map_pathnodes WHERE node_area_id = @areaId AND node_index = @index", param);
                            row = GameMode.mySQLConnector.GetNextRow();
                            if (row.Count > 0)
                            {
                                int tId = Convert.ToInt32(row["node_id"]);
                                GameMode.mySQLConnector.CloseReader();
                                param = new Dictionary<string, object>
                                {
                                    { "@pathnode_id", pId },
                                    { "@target_id", tId },
                                    { "@navinode_id", link.naviNodeLink.id },
                                };
                                GameMode.mySQLConnector.Execute("INSERT INTO map_pathnodes_links " +
                                    "(pathnode_id, target_id, navinode_id) VALUES " +
                                    "(@pathnode_id, @target_id, @navinode_id)", param);
                            }
                        }
                    }
                }
                Logger.WriteLineAndClose("SQL Export done for links in NODES" + i + ".dat");
            }
            Logger.WriteLineAndClose("Link export done.");
            */
        }

        public static void UpdatePathNodeFlags()
        {
            Logger.WriteLineAndClose("Starting SQL Update ...");
            Dictionary<string, object> param;

            for (int i = 0; i < 64; i++)
            {
                foreach (PathNode pathNode in pathNodes[i])
                {
                    int flagInt = 0;
                    flagInt |= (pathNode.flags.linkCount & 0xF);
                    flagInt |= ((pathNode.flags.trafficLevel & 0x3) << 4);
                    if (pathNode.flags.roadBlocks)
                        flagInt |= (1 << 6);
                    if (pathNode.flags.isWater)
                        flagInt |= (1 << 7);
                    if (pathNode.flags.emergencyOnly)
                        flagInt |= (1 << 8);
                    if (!pathNode.flags.isHighway)
                        flagInt |= (1 << 12);
                    if (pathNode.flags.isHighway)
                        flagInt |= (1 << 13);
                    flagInt |= ((pathNode.flags.spawnProbability & 0xF) << 16);

                    param = new Dictionary<string, object>
                    {
                        { "@area_id", pathNode.areaID },
                        { "@index", pathNode.nodeID },
                        { "@flags", flagInt },
                    };
                    GameMode.MySQLConnector.Execute("UPDATE map_pathnodes " +
                        "SET node_flags = @flags " +
                        "WHERE node_area_id = @area_id AND node_index = @index", param);
                }
                Logger.WriteLineAndClose("SQL Update done for nodes in NODES" + i + ".dat");
            }
            Logger.WriteLineAndClose("SQL Update done");
        }

        public static List<PathNode> GetPathNodes()
        {
            List<PathNode> result = new List<PathNode>();
            for (int i = 0; i < 64; i++)
            {
                foreach (PathNode node in pathNodes[i])
                {
                    result.Add(node);
                }
            }
            return result;
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
