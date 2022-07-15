using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events
{
	public class EventSlot
	{
		public Player Player { get; set; }
		public Vector3R SpawnPoint { get; set; }

		public EventSlot(Player p, Vector3R sp)
		{
			this.Player = p;
			this.SpawnPoint = sp;
		}
	}
}
