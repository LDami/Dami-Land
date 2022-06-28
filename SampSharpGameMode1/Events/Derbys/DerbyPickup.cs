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

		public DynamicPickup pickup;
		public PickupEvent Event { get;set; }
		public int ModelId { get; set; }
		public Vector3 Position { get; set; }
		public int WorldId { get; set; }
		public bool IsEnabled { get; set; }

		public DerbyPickup(int modelid, Vector3 position, int worldid, PickupEvent evt)
		{
			pickup = new DynamicPickup(modelid, 14, position, worldid);
			ModelId = modelid;
			Event = evt;
			Position = position;
			WorldId = worldid;
			Enable();
		}

		public void Enable()
		{
			this.IsEnabled = true;
			pickup.PickedUp += Pickup_PickedUp;
		}
		public void Disable()
		{
			this.IsEnabled = false;
			pickup.PickedUp -= Pickup_PickedUp;
		}

		public void Respawn()
		{
			if (!pickup.IsDisposed)
				pickup.Dispose();
			pickup = new DynamicPickup(this.ModelId, 14, this.Position, this.WorldId);
		}

		private void Pickup_PickedUp(object sender, SampSharp.GameMode.Events.PlayerEventArgs e)
		{
			if(IsEnabled)
			{
				switch (Event)
				{
					case PickupEvent.Random:
						DerbyPickupRandomEvent randomEvent = new DerbyPickupRandomEvent(e.Player);
						break;
					case PickupEvent.ChangeVehicle:
						Vector3 pos = Vector3.Zero;
						float rot = 0.0f;
						if (e.Player.InAnyVehicle)
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
}
