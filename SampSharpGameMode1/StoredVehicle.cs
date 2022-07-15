using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
	public class StoredVehicle : BaseVehicle
	{
		public int DbId { get; private set; }


		private static readonly Dictionary<int, int> dbDict = new Dictionary<int, int>(); // pool[vehicleid] = dbid

		public static void AddDbPool(int vehicleid, int dbid)
		{
			dbDict[vehicleid] = dbid;
		}
		public static void RemoveFromDbPool(int vehicleid)
		{
			dbDict.Remove(vehicleid);
		}
		public static int GetVehicleDbId(int vehicleid)
		{
			if (dbDict.ContainsKey(vehicleid))
				return dbDict[vehicleid];
			else
				return -1;
		}
		public static int GetPoolSize()
		{
			return dbDict.Count;
		}
	}
}
