using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Commands
{
	class VehicleCommands
	{
		[Command("vehicle", "veh", "v", DisplayName = "v")]
		private static void SpawnVehicleCommand(Player player, VehicleModelType model)
		{
			Random rndColor = new Random();
			BaseVehicle v = BaseVehicle.Create(model, new Vector3(player.Position.X + 5.0, player.Position.Y, player.Position.Z), 0.0f, rndColor.Next(0, 255), rndColor.Next(0, 255));
			v.VirtualWorld = player.VirtualWorld;
			player.PutInVehicle(v, 0);
			SampSharp.GameMode.Events.EnterVehicleEventArgs e = new SampSharp.GameMode.Events.EnterVehicleEventArgs(player, v, false);
			player.OnEnterVehicle(e);
		}
		[Command("nitro")]
		private static void NitroCommand(Player player)
		{
			player.NitroEnabled = !player.NitroEnabled;
			if (player.NitroEnabled)
			{
				if(player.InAnyVehicle)
				{
					if (VehicleComponents.Get(1010).IsCompatibleWithVehicle(player.Vehicle))
					{
						player.Vehicle.AddComponent(1010);
					}
				}
				player.Notificate("Nitro added");
			}
			else
			{
				if (player.InAnyVehicle)
				{
					player.Vehicle.RemoveComponent(1010);
				}
				player.Notificate("Nitro removed");
			}
		}
	}
}
