using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static SampSharpGameMode1.Civilisation.PathExtractor;

namespace SampSharpGameMode1.Civilisation
{
    public class PathFindingDoneEventArgs : EventArgs
    {
        public PathNode[] path { get; set; }
        public TimeSpan duration { get; set; }
    }
    class Node
    {
        public PathNode pathNode;
        public List<Node> neighbors = new List<Node>();
        public Node parent;
        public double f;
        public double g;
        public double h;
        public override string ToString()
        {
            return "{id: " + this.pathNode.id + ", neighbordsCount: " + this.neighbors.Count + ", f: " + this.f + ", g: " + this.g + ", h: " + this.h + "}";
        }
    }
    struct Marker
    {
        public int id;
        public TextLabel textLabel;
        public bool open;
        public bool closed;
        public bool current;
    }
    class PathFinder
    {
        public event EventHandler<PathFindingDoneEventArgs> Success;
        public event EventHandler<EventArgs> Failure;
        protected virtual void OnSuccess(PathFindingDoneEventArgs e)
        {
            EventHandler<PathFindingDoneEventArgs> handler = Success;
            if (handler != null)
                handler(this, e);
        }
        protected virtual void OnFailure(EventArgs e)
        {
            EventHandler<EventArgs> handler = Failure;
            if (handler != null)
                handler(this, e);
        }

        DateTime startDT;

        List<Node> openList = new List<Node>();
        List<Node> closedList = new List<Node>();

        Node start;
        Node end;

        public PathFinder(List<PathNode> pathNodes, PathNode start, PathNode end)
        {
            Node node = new Node();
            node.pathNode = start;
            node.g = 0;
            node.f = GetHeuristic(start, end);
            node.parent = null;
            this.start = node;

            node = new Node();
            node.pathNode = end;
            this.end = node;

            openList.Add(this.start);

            // Websocket
            SendPathNodeToWebSocket(start, "start");
            SendPathNodeToWebSocket(end, "end");
        }

        public List<Node> GetNeighbors(Node node)
        {
            List<LinkInfo> linkInfo = node.pathNode.links;
            List<Node> result = new List<Node>();
            Node neighbor;
            foreach (LinkInfo link in linkInfo)
            {
                neighbor = new Node();
                neighbor.pathNode = link.targetNode;
                neighbor.h = GetHeuristic(link.targetNode, this.end.pathNode);
                result.Add(neighbor);
            }
            return result;
        }

        public double GetHeuristic(Node from, Node to)
        {
            return from.pathNode.position.DistanceTo(to.pathNode.position);
        }
        public double GetHeuristic(PathNode from, PathNode to)
        {
            return from.position.DistanceTo(to.position);
        }

        public void Find()
        {
            List<TextLabel> tls = new List<TextLabel>();
            Thread t = new Thread(new ThreadStart(() => {
                startDT = DateTime.Now;
                GameMode gm = (GameMode)BaseMode.Instance;
                Node current;
                bool success = false;
                closedList = new List<Node>();
                while (openList.Count > 0)
                {
                    if(openList.Count > 1)
                    {
                        openList.Sort((x, y) => {
                            if (x.f > y.f) return 1;
                            else if (x.f < y.f) return -1;
                            else if (x.f == y.f) return 0;
                            else return 0;
                        });
                    }

                    // Websocket
                    /*
                    openList.ForEach(n =>
                    {
                        SendPathNodeToWebSocket(n.pathNode, "open");
                    });
                    closedList.ForEach(n =>
                    {
                        SendPathNodeToWebSocket(n.pathNode, "closed");
                    });
                    */

                    current = openList[0];
                    openList.Remove(current);
                    if (current.pathNode.id == end.pathNode.id)
                    {
                        Console.WriteLine("Pathfinding: Process ended !");
                        ReconstructPath(current);
                        success = true;
                        break;
                    }
                    //Console.WriteLine("Current = " + current.pathNode.id);
                    //Console.WriteLine("Current F = " + current.f);
                    openList.Remove(current);
                    closedList.Add(current);
                    List<Node> neighbors = GetNeighbors(current);
                    Node tmpNeighbor;

                    //gm.socket.Write("{\"id\": \"" + current.pathNode.id + "\", \"status\": \"current\"}");

                    //Console.WriteLine("Neighbors = ");
                    for(int i = 0; i <= neighbors.Count-1; i++)
                    {
                        Console.WriteLine(neighbors[i].ToString());
                        tmpNeighbor = neighbors[i];
                        int foundNode = closedList.FindIndex(element => element.pathNode.id == tmpNeighbor.pathNode.id);
                        if (foundNode != -1)
                            continue;
                        foundNode = openList.FindIndex(element => element.pathNode.id == tmpNeighbor.pathNode.id);
                        if (foundNode == -1)
                        {
                            tmpNeighbor.parent = current;
                            tmpNeighbor.g = current.g + GetHeuristic(tmpNeighbor, current);
                            tmpNeighbor.h = GetHeuristic(tmpNeighbor, end);
                            tmpNeighbor.f = tmpNeighbor.g + tmpNeighbor.h;
                            openList.Add(tmpNeighbor);
                            //gm.socket.Write("{\"id\": \"" + tmpNeighbor.pathNode.id + "\", \"status\": \"open\"}");
                        }
                        else
                        {
                            if(tmpNeighbor.g < (current.g + GetHeuristic(tmpNeighbor, current)))
                            {
                                Node tmp = openList.Find(node => node.pathNode.id == tmpNeighbor.pathNode.id); // tmp contient la référence, pas la valeur !
                                tmp.g = current.g + GetHeuristic(tmpNeighbor, current);
                                tmp.f = tmp.g + tmp.h;
                                tmp.parent = current;
                            }
                        }
                    }
                    closedList.Add(current);
                }
                if (!success)
                    OnFailure(new EventArgs());
            }));
            t.Start();
        }

        private static void SendPathNodeToWebSocket(PathNode node, string status)
        {
            GameMode gm = (GameMode)BaseMode.Instance;
            bool isSocketAlive = false;
            MySocketIO socket = gm.socket;
            if (socket.GetStatus() == MySocketIO.SocketStatus.CONNECTED)
            {
                isSocketAlive = true;
            }

            string data = "{ \"id\": \"" + node.id + "\", \"posX\": " + node.position.X + ", \"posY\": " + node.position.Y + ", \"links\": [";
            int idx = 1;
            foreach (LinkInfo link in node.links)
            {
                data += "\"" + link.targetNode.id + "\"";
                if (idx < node.links.Count)
                    data += ",";
                idx++;
            }
            data += "], \"status\": \"" + status + "\" }";
            if (socket.GetStatus() == MySocketIO.SocketStatus.CONNECTED)
            {
                isSocketAlive = true;
            }
            if (isSocketAlive)
            {
                if (socket.Write(data) == -1) isSocketAlive = false;
            }
        }

        public void ReconstructPath(Node current)
        {
            Stack<PathNode> finalPath = new Stack<PathNode>();
            while(current.parent != null)
            {
                current = current.parent;
                finalPath.Push(current.pathNode);
            }


            PathFindingDoneEventArgs args = new PathFindingDoneEventArgs();
            args.path = finalPath.ToArray();
            args.duration = DateTime.Now - startDT;
            OnSuccess(args);
            /*
            GameMode gm = (GameMode)BaseMode.Instance;
            Console.WriteLine("Path nodes count: " + finalPath.Count);
            int count = finalPath.Count;
            for (int i=0; i < count; i++)
            {
                gm.socket.Write("{\"id\": \"" + finalPath.Pop().id + "\", \"status\": \"finalpath\"}");
            }
            */
        }
    }
}
