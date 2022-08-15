using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;

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

		private Timer respawnTimer;

		public DerbyPickup(int modelid, Vector3 position, int worldid, PickupEvent evt)
		{
			pickup = new DynamicPickup(modelid, 14, position, worldid);
			ModelId = modelid;
			Event = evt;
			Position = position;
			WorldId = worldid;
			Enable();
			respawnTimer = new Timer(1000 * 60, true);
			respawnTimer.Tick += (sender, e) => Respawn();
		}

		public void Dispose()
        {
			Disable();
			if(respawnTimer != null)
            {
				respawnTimer.IsRepeating = false;
				respawnTimer.IsRunning = false;
			}
			if (!pickup.IsDisposed)
				pickup.Dispose();
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
			Enable();
		}

		private void Pickup_PickedUp(object sender, SampSharp.GameMode.Events.PlayerEventArgs e)
		{
			if(IsEnabled)
			{
				switch (Event)
				{
					case PickupEvent.Random:
						DerbyPickupRandomEvent randomEvent = new DerbyPickupRandomEvent((Player)e.Player);
						break;
					case PickupEvent.ChangeVehicle:
						Vector3 pos = Vector3.Zero;
						float rot = 0.0f;
						if (e.Player.InAnyVehicle)
						{
							BaseVehicle veh = e.Player.Vehicle;
							pos = e.Player.Vehicle.Position;
							rot = e.Player.Vehicle.Angle;
							e.Player.RemoveFromVehicle();
							veh.Dispose();
						}
						Random rdm = new Random();
						// Prevent system to load an unusable / cheated vehicle in Derby
						List<VehicleCategory> notAuthorizedVehicle = new List<VehicleCategory>()
						{
							VehicleCategory.Airplane,
							VehicleCategory.Boat,
							VehicleCategory.Helicopter,
							VehicleCategory.RemoteControl,
							VehicleCategory.Trailer,
							VehicleCategory.TrainTrailer
						};
						VehicleModelType modelType = VehicleModelType.Ambulance;
						bool isValidVehicle = false;
						while (!isValidVehicle)
                        {
							modelType = (VehicleModelType)rdm.Next(400, 611);
							if(!notAuthorizedVehicle.Contains(VehicleModelInfo.ForVehicle(modelType).Category))
                            {
								if (VehicleModelInfo.ForVehicle(modelType).Category == VehicleCategory.Unique &&
									(VehicleModelInfo.ForVehicle(modelType).Name == "Tram" || VehicleModelInfo.ForVehicle(modelType).Name.Contains("Train")))
								{

								}
								else
									isValidVehicle = true;

							}

						}
						BaseVehicle newVeh = BaseVehicle.Create(modelType, pos, rot, rdm.Next(0, 255), rdm.Next(0, 255));
						newVeh.VirtualWorld = e.Player.VirtualWorld;
						newVeh.Engine = true;
						newVeh.Doors = true;
						newVeh.Died += ((Player)e.Player).pEvent.Source.OnPlayerVehicleDied;
						e.Player.PutInVehicle(newVeh);
						break;
					case PickupEvent.Repair:
						if (e.Player.InAnyVehicle)
						{
							e.Player.Vehicle.Repair();
							e.Player.PlaySound(1134);
						}
						break;
				}
			}
		}
	}
}
