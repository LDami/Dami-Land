using SampSharp.GameMode;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Physics
{
	public class RayCastCollisionTarget
	{
		public RayCastCollisionTarget(Vector3 pos, float dist, int mid)
		{
			this.position = pos;
			this.distance = dist;
			this.modelid = mid;
			this.index = -1;
		}
		public Vector3 position { get; set; }
		public float distance { get; set; }
		public int modelid { get; set; }
		public int index { get; set; }
	}
}
