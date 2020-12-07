using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using static SampSharpGameMode1.Civilisation.PathExtractor;

namespace SampSharpGameMode1.Civilisation
{
    public class PathFindingDoneEventArgs : EventArgs
    {
        public PathNode[] path { get; set; }
    }
    class Node
    {
        public PathNode pathNode;
        public List<Node> neighbors = new List<Node>();
        public Node parent;
        public double f;
        public double g;
        public double h;
        public string ToString()
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

        List<Node> openList = new List<Node>();
        List<Node> closedList = new List<Node>();
        List<PathNode> finalPath = new List<PathNode>();

        Node start;
        Node end;

        List<Marker> markers = new List<Marker>();

        public PathFinder(List<PathNode> pathNodes, PathNode start, PathNode end)
        {
            Node node = new Node();
            node.pathNode = start;
            node.g = 0;
            node.parent = null;
            this.start = node;

            node = new Node();
            node.pathNode = end;
            this.end = node;

            openList.Add(this.start);
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
                GameMode gm = (GameMode)BaseMode.Instance;
                Node current;
                bool success = false;
                closedList = new List<Node>();
                finalPath = new List<PathNode>();
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
                    current = openList[0];
                    if (current.Equals(end))
                    {
                        ReconstructPath(current);
                        success = true;
                        break;
                    }
                    Console.WriteLine("Current = " + current.pathNode.id);
                    openList.Remove(current);
                    closedList.Add(current);
                    List<Node> neighbors = GetNeighbors(current);
                    Node tmpNeighbor;

                    gm.socket.Write("{\"id\": \"" + current.pathNode.id + "\", \"status\": \"current\"}");

                    Console.WriteLine("Neighbors = ");
                    for(int i = 0; i < neighbors.Count-1; i++)
                    {
                        Console.WriteLine(neighbors[i].ToString());
                        tmpNeighbor = neighbors[i];
                        double tempG = current.g + GetHeuristic(tmpNeighbor, current);
                        if (openList.Contains(tmpNeighbor))
                        {
                            if (tempG < tmpNeighbor.g)
                            {
                                Console.WriteLine("Already included in openList but G is better");
                                tmpNeighbor.parent = current;
                                tmpNeighbor.g = tempG;
                                tmpNeighbor.h = GetHeuristic(tmpNeighbor, end);
                                tmpNeighbor.f = tmpNeighbor.g + tmpNeighbor.h;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Not included in openList");
                            tmpNeighbor.parent = current;
                            tmpNeighbor.g = tempG;
                            tmpNeighbor.h = GetHeuristic(tmpNeighbor, end);
                            tmpNeighbor.f = tmpNeighbor.g + tmpNeighbor.h;
                            openList.Add(tmpNeighbor);
                        }
                        neighbors[i] = tmpNeighbor;
                        Console.WriteLine(neighbors[i].ToString());
                    }

                    markers.FindAll(e => e.current == true).ForEach(e => e.textLabel.Dispose());
                    markers.RemoveAll(e => e.current == true);

                    int id;
                    id = Convert.ToInt32(current.pathNode.areaID.ToString() + current.pathNode.nodeID.ToString());
                    List<Marker> existingMarkers = markers.FindAll(e => e.id == id);
                    if (existingMarkers.Count == 0)
                    {
                        string txt = "ID: " + current.pathNode.id + "\n" + " Current";
                        TextLabel lbl = new TextLabel(txt, Color.White, current.pathNode.position + new Vector3(0.0, 0.0, 2.0), 200.0f);
                        Marker marker = new Marker();
                        marker.id = id;
                        marker.textLabel = lbl;
                        marker.open = false;
                        marker.closed = false;
                        marker.current = true;
                        markers.Add(marker);
                    }
                    foreach (Node node in openList)
                    {
                        string txt = "ID: " + node.pathNode.id + "\n" + "\n" + "Open";
                        id = Convert.ToInt32(node.pathNode.areaID.ToString() + node.pathNode.nodeID.ToString());
                        List<Marker> existingMarkers2 = markers.FindAll(e => e.id == id);
                        if (existingMarkers2.Count == 0)
                        {
                            TextLabel lbl = new TextLabel(txt, Color.White, node.pathNode.position + new Vector3(0.0, 0.0, 2.0), 200.0f);
                            Marker marker = new Marker();
                            marker.id = id;
                            marker.textLabel = lbl;
                            marker.open = true;
                            marker.closed = false;
                            markers.Add(marker);
                        }
                        gm.socket.Write("{\"id\": \"" + node.pathNode.id + "\", \"status\": \"open\"}");
                    }
                    foreach (Node node in closedList)
                    {
                        string txt = "ID: " + node.pathNode.id + "\n" + "\n" + "\n" + "Closed";
                        id = Convert.ToInt32(node.pathNode.areaID.ToString() + node.pathNode.nodeID.ToString());
                        List<Marker> existingMarkers2 = markers.FindAll(e => e.id == id);
                        if (existingMarkers2.Count == 0)
                        {
                            TextLabel lbl = new TextLabel(txt, Color.White, node.pathNode.position + new Vector3(0.0, 0.0, 2.0), 200.0f);
                            Marker marker = new Marker();
                            marker.id = id;
                            marker.textLabel = lbl;
                            marker.open = false;
                            marker.closed = true;
                            markers.Add(marker);
                        }
                        else
                        {
                            if(existingMarkers2[0].open)
                            {
                                markers.Find(e => e.id == existingMarkers[0].id).textLabel.Dispose();
                                markers.Remove(existingMarkers2[0]);
                                TextLabel lbl = new TextLabel(txt, Color.White, node.pathNode.position + new Vector3(0.0, 0.0, 2.0), 200.0f);
                                Marker marker = new Marker();
                                marker.id = id;
                                marker.textLabel = lbl;
                                marker.open = false;
                                marker.closed = true;
                                markers.Add(marker);
                            }
                        }
                        gm.socket.Write("{\"id\": \"" + node.pathNode.id + "\", \"status\": \"closed\"}");
                        Thread.Sleep(10);
                    }
                }
                if (!success)
                    OnFailure(new EventArgs());
            }));
            t.Start();
        }

        public void ReconstructPath(Node current)
        {
            //Queue<Node> finalPath = new Queue<Node>();
            Stack<PathNode> finalPath = new Stack<PathNode>();
            while(current.parent != null)
            {
                current = current.parent;
                finalPath.Push(current.pathNode);
            }
            
            PathFindingDoneEventArgs args = new PathFindingDoneEventArgs();
            args.path = finalPath.ToArray();
            OnSuccess(args);
        }
    }
}
