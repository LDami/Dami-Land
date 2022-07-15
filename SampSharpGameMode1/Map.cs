using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SampSharpGameMode1
{
    public class MapLoadedEventArgs : EventArgs
    {
        public bool success { get; set; }
        public Map map { get; set; }
        public int loadedObjects { get; set; }
    }
    public class Map
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Creator { get; set; }
        public bool IsLoaded { get; private set; }
        public List<MapObject> Objects { get; private set; } = new List<MapObject>();
        public Vector3 Spawn { get; set; }
        public int VirtualWorld { get; private set; }

        private static List<Map> pool = new List<Map>();

        // Common Events

        public event EventHandler<MapLoadedEventArgs> Loaded;

        protected virtual void OnLoaded(MapLoadedEventArgs e)
        {
            Loaded?.Invoke(this, e);
            IsLoaded = e.success;
        }

        public Map()
		{
            this.Id = -1;
        }
        public void Load(int id, int virtualworld)
        {
            if (GameMode.mySQLConnector != null)
            {
                Thread t = new Thread(() =>
                {
                    this.VirtualWorld = virtualworld;

                    bool errorFlag = false;
                    Dictionary<string, string> row;
                    Dictionary<string, object> param = new Dictionary<string, object>
                    {
                        { "@id", id }
                    };
                    GameMode.mySQLConnector.OpenReader("SELECT * FROM maps WHERE map_id=@id", param);
                    row = GameMode.mySQLConnector.GetNextRow();
                    if (row.Count > 0)
                    {
                        this.Id = Convert.ToInt32(row["map_id"]);
                        this.Name = row["map_name"].ToString();
                        this.Creator = Convert.ToInt32(row["map_creator"]);
                    }
                    else
                        errorFlag = true;
                    GameMode.mySQLConnector.CloseReader();

                    if(!errorFlag)
					{
                        GameMode.mySQLConnector.OpenReader("SELECT * FROM mapobjects WHERE map_id=@id", param);
                        row = GameMode.mySQLConnector.GetNextRow();
                        this.Objects.Clear();
                        while (row.Count > 0)
                        {
                            this.Objects.Add(new MapObject(
                                Convert.ToInt32(row["obj_id"]),
                                Convert.ToInt32(row["obj_model"]),
                                new Vector3(
                                    (float)Convert.ToDouble(row["obj_pos_x"]),
                                    (float)Convert.ToDouble(row["obj_pos_y"]),
                                    (float)Convert.ToDouble(row["obj_pos_z"])
                                ),
                                new Vector3(
                                    (float)Convert.ToDouble(row["obj_rot_x"]),
                                    (float)Convert.ToDouble(row["obj_rot_y"]),
                                    (float)Convert.ToDouble(row["obj_rot_z"])
                                ),
                                virtualworld));
                            row = GameMode.mySQLConnector.GetNextRow();
                        }
                        GameMode.mySQLConnector.CloseReader();
                    }

                    MapLoadedEventArgs args = new MapLoadedEventArgs();
                    args.success = !errorFlag;
                    args.map = this;
                    args.loadedObjects = this.Objects.Count;
                    OnLoaded(args);
                    Map.AddPool(this);
                });
                t.Start();
            }
        }

        public void Unload()
        {
            if(this.Objects.Count > 0)
            {
                this.Objects.ForEach(map => map.Dispose());
                this.Objects.Clear();
                this.Id = -1;
                this.Name = "";
                this.IsLoaded = false;
                Map.RemovePool(this);
            }
        }

        protected static void AddPool(Map map)
        {
            Map.pool.Add(map);
        }
        protected static void RemovePool(Map map)
        {
            Map.pool.Remove(map);
        }
        public static List<Map> GetAllLoadedMaps()
        {
            return pool;
        }

        public static Dictionary<int, string> FindAll(string str)
        {
            Dictionary<int, string> results = new Dictionary<int, string>();

            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@name", str }
                };
            mySQLConnector.OpenReader("SELECT map_id, map_name FROM maps WHERE map_name LIKE @name", param);
            Dictionary<string, string> row = mySQLConnector.GetNextRow();

            while(row.Count > 0)
            {
                results.Add(Convert.ToInt32(row["map_id"]), row["map_name"]);
                row = mySQLConnector.GetNextRow();
            }
            mySQLConnector.CloseReader();
            return results;
        }
    }
}
