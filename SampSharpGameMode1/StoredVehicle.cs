using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
	class StoredVehicle : BaseVehicle
	{
		public int DbId { get; private set; }


		private static Dictionary<int, StoredVehicle> pool = new Dictionary<int, StoredVehicle>(); // pool[vehicleid] = dbid
		public static void AddDbPool(int vehicleid, int dbid)
		{
			StoredVehicle veh = new StoredVehicle();
			veh.DbId = dbid;
			pool[vehicleid] = veh;
		}
		public static void RemoveFromDbPool(int vehicleid)
		{
			pool.Remove(vehicleid);
		}
		public static StoredVehicle GetStoredVehicle(int vehicleid)
		{
			try
			{
				return pool[vehicleid] ?? null;
			}
			catch(KeyNotFoundException e)
			{
				return null;
			}
		}
	}
}
