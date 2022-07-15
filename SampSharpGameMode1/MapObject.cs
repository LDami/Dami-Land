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

		/*
		private static readonly Dictionary<int, int> dbDict = new Dictionary<int, int>(); // pool[objectid] = dbid

		public static void AddDbPool(int objectid, int dbid)
		{
			dbDict[objectid] = dbid;
		}
		public static void RemoveFromDbPool(int objectid)
		{
			dbDict.Remove(objectid);
		}
		public static int GetObjectDbId(int objectid)
		{
			if (dbDict.ContainsKey(objectid))
				return dbDict[objectid];
			else
				return -1;
		}
		public static int GetPoolSize()
		{
			return dbDict.Count;
		}
		*/
	}
}
