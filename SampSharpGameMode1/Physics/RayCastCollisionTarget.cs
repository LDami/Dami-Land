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
			this.Position = pos;
			this.Distance = dist;
			this.ModelId = mid;
			this.Index = -1;
		}
		public Vector3 Position { get; set; }
		public float Distance { get; set; }
		public int ModelId { get; set; }
		public int Index { get; set; }
	}
}
