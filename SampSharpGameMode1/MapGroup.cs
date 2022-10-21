using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
    public class MapGroup
    {
        public int Id { get; set; }

        private string name;
        public string Name { get => name; set { name = value; this.Modified = true; } }

        private Color? foreColor;
        public Color? ForeColor { get => foreColor; set { foreColor = value; this.Modified = true; } }
        public bool Modified { get; private set; }

        public MapGroup(int id, Color foreColor, string name = "")
        {
            Id = id;
            Name = name;
            ForeColor = foreColor;
            Modified = false;
        }

        private static List<MapGroup> pool = new List<MapGroup>();
        public static MapGroup GetOrCreate(int id)
        {
            MapGroup result;
            if((result = pool.Find(group => group.Id == id)) == null)
            {
                result = new MapGroup(id, Color.AliceBlue, "Untitled");
                pool.Add(result);
            }
            return result;
        }
    }
}
