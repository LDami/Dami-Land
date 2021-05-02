using SampSharp.GameMode;
using SampSharp.GameMode.World;
using SampSharp.Streamer;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SampSharpGameMode1.Physics
{
	public class CollisionManager
	{
		/// <summary>
		///		This method explodes a DynamicObject when he collides a wall or a vehicle
		/// </summary>
		/// <param name="obj">The <see cref="DynamicObject"/> to explode</param>
		/// <param name="dest">The destination where <paramref name="obj"/> goes</param>
		/// <param name="sender">The sender of obj. Used to prevent self-explosion at launch</param>
		public static void ExplodeOnCollision(DynamicObject obj, Vector3 dest, BasePlayer sender)
		{
			Thread t = new Thread(() =>
			{
				RayCastCollisionTarget collisionTarget;
				Vector3 objPos; // Contains the obj position to be set just after the IsDisposed check
				bool collisionWithVehicle;
				while(!obj.IsDisposed)
				{
					objPos = obj.Position;
					collisionTarget = ColAndreas.RayCastLine(objPos, dest);
					collisionWithVehicle = false;
					foreach(BaseVehicle veh in BaseVehicle.All)
					{
						if(veh.GetDistanceFromPoint(objPos) < 5.0 && !sender.Vehicle.Equals(veh)) collisionWithVehicle = true;
					}
					if(collisionTarget.distance < 10.0 || collisionWithVehicle)
					{
						BasePlayer.CreateExplosionForAll(objPos, SampSharp.GameMode.Definitions.ExplosionType.LargeVisibleDamageFire, 50.0f);
						obj.Dispose();
					}
					Thread.Sleep(500);
				}
			});
			t.Start();
		}
	}
}
