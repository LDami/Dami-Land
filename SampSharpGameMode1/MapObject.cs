using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SampSharpGameMode1
{
	public class MapObject : DynamicObject
	{
		public int DbId { get; set; }
		public override Vector3 Position { get => base.Position; set { base.Position = value; this.Modified = true; } }
		public new Vector3 Rotation { get => base.Rotation; set { base.Rotation = value; this.Modified = true; } }
		public bool Modified { get; private set; }

		public MapObject(int dbid, int modelid, Vector3 position, Vector3 rotation, int virtualworld = 0) : base(modelid, position, rotation, worldid: virtualworld)
		{
			this.DbId = dbid;
			this.Modified = false;
		}
	}
}
