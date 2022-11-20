using SampSharp.GameMode;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Missions.MissionPickups
{
    public abstract class MissionPickupBase
    {
		public List<Player> Players;
		public DynamicPickup Pickup;
        public Vector3 Position { get; set; }

		public virtual void Dispose()
		{
			/*
			Disable();
			if(respawnTimer != null)
            {
				respawnTimer.IsRepeating = false;
				respawnTimer.IsRunning = false;
			}
			*/
			if (!Pickup.IsDisposed)
			{
				this.Pickup.PickedUp -= OnPickupPickedUp;
				Pickup.Dispose();
			}
		}
		public virtual void Enable(List<Player> players)
        {
			Players = players;
			this.Pickup.PickedUp += OnPickupPickedUp;
		}
        internal virtual void OnPickupPickedUp(object sender, SampSharp.GameMode.Events.PlayerEventArgs args)
		{
			throw new NotImplementedException();
		}
    }
}
