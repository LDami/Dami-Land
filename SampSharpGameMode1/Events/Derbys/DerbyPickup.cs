using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using System;

namespace SampSharpGameMode1.Events.Derbys
{
	public class DerbyPickup
	{
		public enum PickupEvent
		{
			None = 0,
			Random,
			ChangeVehicle,
			Repair
		}
		private DynamicPickup pickup;

		public PickupEvent Event { get;set; }
		public int ModelId { get; set; }
		public Vector3 Position { get; set; }

		public DerbyPickup(int modelid, Vector3 position, int worldid, PickupEvent evt, bool enabled)
		{
			pickup = new DynamicPickup(modelid, 2, position, worldid);
			if(enabled) pickup.PickedUp += Pickup_PickedUp;
			ModelId = modelid;
			Event = evt;
			Position = position;
		}

		private void Pickup_PickedUp(object sender, SampSharp.GameMode.Events.PlayerEventArgs e)
		{
			switch(Event)
			{
				case PickupEvent.Random:
					DerbyPickupRandomEvent randomEvent = new DerbyPickupRandomEvent(e.Player);
					break;
				case PickupEvent.ChangeVehicle:
					Vector3 pos = Vector3.Zero;
					float rot = 0.0f;
					if(e.Player.InAnyVehicle)
					{
						pos = e.Player.Vehicle.Position;
						rot = e.Player.Vehicle.Angle;
						e.Player.RemoveFromVehicle();
					}
					Random rdm = new Random();
					e.Player.PutInVehicle(BaseVehicle.Create((VehicleModelType)rdm.Next(400, 611), pos, rot, rdm.Next(0, 255), rdm.Next(0, 255)));
					break;
				case PickupEvent.Repair:
					if (e.Player.InAnyVehicle)
					{
						e.Player.Vehicle.Health = 100.0f;
						e.Player.PlaySound(1134);
					}
					break;
			}
		}
	}
}
