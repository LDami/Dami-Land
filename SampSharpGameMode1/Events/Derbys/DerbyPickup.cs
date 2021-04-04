using SampSharp.GameMode;
using SampSharp.Streamer.World;

namespace SampSharpGameMode1.Events.Derbys
{
	public class DerbyPickup
	{
		public enum PickupEvent
		{
			None,
			Random,
			ChangeVehicle,
			Repair
		}
		private DynamicPickup pickup;
		public PickupEvent Event { get;set; }

		public DerbyPickup(int modelid, Vector3 position, int worldid, PickupEvent evt)
		{
			pickup = new DynamicPickup(modelid, 2, position, worldid);
			pickup.PickedUp += Pickup_PickedUp;
			Event = evt;
		}

		private void Pickup_PickedUp(object sender, SampSharp.GameMode.Events.PlayerEventArgs e)
		{
			switch(Event)
			{
				case PickupEvent.Random:
					break;
			}
		}
	}
}
