using SampSharp.GameMode;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SampSharpGameMode1.Physics
{
	public class CollisionManager
	{
		public static void ExplodeOnCollision(DynamicObject obj, Vector3 dest)
		{
			Thread t = new Thread(() =>
			{
				RayCastCollisionTarget collisionTarget;
				Vector3 objPos; // Contains the obj position to be set just after the IsDisposed check
				bool collisionWithVehicle;
				while (!obj.IsDisposed)
				{
					objPos = obj.Position;
					collisionTarget = ColAndreas.RayCastLine(objPos, dest);
					collisionWithVehicle = false;
					foreach (BaseVehicle veh in BaseVehicle.All)
					{
						if (veh.GetDistanceFromPoint(objPos) < 10.0) collisionWithVehicle = true;
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
