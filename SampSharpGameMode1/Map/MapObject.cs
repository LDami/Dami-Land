using SampSharp.GameMode;
using SampSharp.Streamer.World;

namespace SampSharpGameMode1.Map
{
    public class MapObject : DynamicObject
    {
        public int DbId { get; set; }
        public override Vector3 Position { get => base.Position; set { base.Position = value; Modified = true; } }
        public new Vector3 Rotation { get => base.Rotation; set { base.Rotation = value; Modified = true; } }
        private MapGroup group;
        public MapGroup Group { get => group; set { group = value; Modified = true; } }
        public bool Modified { get; private set; }

        public MapObject(int dbid, int modelid, Vector3 position, Vector3 rotation, MapGroup group, int virtualworld = 0) : base(modelid, position, rotation, worldid: virtualworld)
        {
            DbId = dbid;
            Group = group;
            Modified = false;
        }
    }
}
