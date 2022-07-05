using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.Text;
using static SampSharpGameMode1.Civilisation.PathExtractor;

namespace SampSharpGameMode1.Civilisation
{
    public class PathTools
    {
        private static int GetArea(Vector3 position)
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
                    Console.WriteLine("PathTools.cs - PathTools.GetArea:E: PathExtractor.nodeBorders[" + i + "] is null");
                }
            }
            return -1;
        }

        public static PathNode GetNeirestPathNode(Vector3 position)
        {
            List<PathNode> allPathNodes = GetPathNodes();
            List<PathNode> allNearPathNodes = new List<PathNode>();

            PathNode nearestNode = new PathNode();
            PathNode lastNode = new PathNode();
            foreach (PathNode node in allPathNodes)
            {
                if (node.position.DistanceTo(position) < 1000.0f)
                {
                    allNearPathNodes.Add(node);
                }
            }
            foreach (PathNode node in allNearPathNodes)
            {
                if (lastNode.position != Vector3.Zero)
                {
                    if (nearestNode.position == Vector3.Zero || nearestNode.position.DistanceTo(position) > lastNode.position.DistanceTo(position))
                    {
                        nearestNode = lastNode;
                    }
                }
                lastNode = node;
            }
            return nearestNode;
        }

        public static List<PathNode> GetNeighbors(PathNode node)
        {
            List<LinkInfo> linkInfo = node.links;
            List<PathNode> result = new List<PathNode>();
            foreach (LinkInfo link in linkInfo)
            {
                result.Add(link.targetNode);
            }
            return result;
        }
    }
}
