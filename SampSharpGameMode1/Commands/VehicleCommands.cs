using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Commands
{
	class VehicleCommands : Player
	{
		[Command("vehicle", "veh", "v", DisplayName = "v")]
		private void SpawnVehicleCommand(VehicleModelType model)
		{
			Random rndColor = new Random();
			BaseVehicle v = BaseVehicle.Create(model, new Vector3(this.Position.X + 5.0, this.Position.Y, this.Position.Z), 0.0f, rndColor.Next(0, 255), rndColor.Next(0, 255));
			this.PutInVehicle(v, 0);
			SampSharp.GameMode.Events.EnterVehicleEventArgs e = new SampSharp.GameMode.Events.EnterVehicleEventArgs(this, v, false);
			this.OnEnterVehicle(e);
		}
		[Command("nitro")]
		private void NitroCommand()
		{
			this.NitroEnabled = !this.NitroEnabled;
			if (this.NitroEnabled)
			{
				if(this.InAnyVehicle)
				{
					if (VehicleComponents.Get(1010).IsCompatibleWithVehicle(this.Vehicle))
					{
						this.Vehicle.AddComponent(1010);
					}
				}
				this.Notificate("Nitro added");
			}
			else
			{
				if (this.InAnyVehicle)
				{
					this.Vehicle.RemoveComponent(1010);
				}
				this.Notificate("Nitro removed");
			}
		}
	}
}
