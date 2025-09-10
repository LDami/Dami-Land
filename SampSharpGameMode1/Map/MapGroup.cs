using SampSharp.GameMode.SAMP;
using System.Collections.Generic;

namespace SampSharpGameMode1.Map
{
    public class MapGroup
    {
        public int DbId { get; set; }
        public int Index { get; set; }

        private string name;
        public string Name { get => name; set { name = value; Modified = true; } }

        private Color? foreColor;
        public Color? ForeColor { get => foreColor; set { foreColor = value; Modified = true; } }
        public bool Modified { get; private set; }

        public MapGroup(int id, int index, Color foreColor, string name = "")
        {
            DbId = id;
            Index = index;
            Name = name;
            ForeColor = foreColor;
            Modified = false;
        }

        public override string ToString()
        {
            return $"{this.Index} {this.Name}";
        }
    }
}
