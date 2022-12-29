using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
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

                    if (!errorFlag)
					{
                        GameMode.mySQLConnector.OpenReader("SELECT mapobjects.*, groups.group_Color, groups.group_Name FROM mapobjects LEFT JOIN mapobjects_groups AS groups ON (mapobjects.group_id = groups.group_id) WHERE map_id=@id", param);
                        //GameMode.mySQLConnector.OpenReader("SELECT * FROM mapobjects WHERE map_id=@id", param);
                        row = GameMode.mySQLConnector.GetNextRow();
                        this.Objects.Clear();
                        MapGroup? objGroup = null;
                        while (row.Count > 0)
                        {
                            if (row["mapobjects.group_id"] != null)
                            {
                                objGroup = MapGroup.GetOrCreate(Convert.ToInt32(row["mapobjects.group_id"]));
                                objGroup.ForeColor ??= Utils.GetColorFromString("0x" + row["group_color"]) ?? Color.AliceBlue;
                            }
                            else
                                objGroup = null;

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
                                objGroup,
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

        public static Dictionary<int, string> FindAll(string str, Player owner)
        {
            Dictionary<int, string> results = new Dictionary<int, string>();

            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@name", str },
                    { "@playerid", owner.DbId }
                };
            mySQLConnector.OpenReader("SELECT map_id, map_name FROM maps WHERE map_name LIKE @name AND map_creator = @playerid", param);
            Dictionary<string, string> row = mySQLConnector.GetNextRow();

            while(row.Count > 0)
            {
                results.Add(Convert.ToInt32(row["map_id"]), row["map_name"]);
                row = mySQLConnector.GetNextRow();
            }
            mySQLConnector.CloseReader();
            return results;
        }
        public static List<string> GetPlayerMapList(Player player)
        {
            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@playerid", player.DbId }
                };
            mySQLConnector.OpenReader("SELECT map_id, map_name FROM maps WHERE map_creator = @playerid", param);
            List<string> result = new List<string>();
            Dictionary<string, string> row = mySQLConnector.GetNextRow();
            while (row.Count > 0)
            {
                result.Add(row["map_id"] + "_" + Display.ColorPalette.Primary.Main + row["map_name"]);
                row = mySQLConnector.GetNextRow();
            }
            mySQLConnector.CloseReader();

            return result;
        }
        public static Dictionary<string, string> GetInfo(int id)
        {
            // id, name, creator, zone, number of objects, creation date, last update date
            Dictionary<string, string> results = new Dictionary<string, string>();
            Dictionary<string, string> row;

            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>
                {
                    { "@id", id }
                };

            mySQLConnector.OpenReader("SELECT map_id, map_name, map_creator, map_creationdate, map_lasteditdate FROM maps WHERE map_id = @id", param);

            row = mySQLConnector.GetNextRow();
            foreach (KeyValuePair<string, string> kvp in row)
                results.Add(MySQLConnector.Field.GetFieldName(kvp.Key), kvp.Value);

            mySQLConnector.CloseReader();

            mySQLConnector.OpenReader("SELECT obj_id, obj_pos_x, obj_pos_y, obj_pos_z " +
                "FROM mapobjects WHERE map_id = @id", param);
            int nbrOfObjects = 0;
            row = mySQLConnector.GetNextRow();
            Vector3 firstObjectPos = Vector3.Zero;
            while (row.Count > 0)
            {
                nbrOfObjects++;
                if (firstObjectPos == Vector3.Zero)
                {
                    firstObjectPos = new Vector3(
                        (float)Convert.ToDouble(row["obj_pos_x"]),
                        (float)Convert.ToDouble(row["obj_pos_y"]),
                        (float)Convert.ToDouble(row["obj_pos_z"])
                    );
                }
                row = mySQLConnector.GetNextRow();
            }
            results.Add("Number of objects", nbrOfObjects.ToString());
            mySQLConnector.CloseReader();

            string zoneStr = Zone.GetMainZoneName(firstObjectPos);
            results.Add("Zone", zoneStr);

            List<string> usedBy = new List<string>();

            mySQLConnector.OpenReader("SELECT race_id, race_name FROM races WHERE race_map = @id", param);
            row = mySQLConnector.GetNextRow();
            while (row.Count > 0)
            {
                usedBy.Add($"[Race] {row["race_id"]}_{row["race_name"]}");
                row = mySQLConnector.GetNextRow();
            }
            mySQLConnector.CloseReader();
            mySQLConnector.OpenReader("SELECT derby_id, derby_name FROM derbys WHERE derby_map = @id", param);
            row = mySQLConnector.GetNextRow();
            while (row.Count > 0)
            {
                usedBy.Add($"[Derby] {row["derby_id"]}_{row["derby_name"]}");
                row = mySQLConnector.GetNextRow();
            }
            mySQLConnector.CloseReader();

            results.Add("Used in", $"{usedBy.Count} events");
            usedBy.ForEach(evt => results.Add("", evt));
            return results;
        }
    }
}
