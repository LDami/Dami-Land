using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using System;

#pragma warning disable IDE0051 // Disable useless private members

namespace SampSharpGameMode1.Commands
{
	class VehicleCommands
	{
		[Command("vehicle", "veh", "v", DisplayName = "v")]
		private static void SpawnVehicleCommand(Player player, VehicleModelType model)
		{
			if(player.pEvent == null)
			{
				AdminPermissionChecker isAdmin = new();
				if (player.SpawnedVehicles.Count > 5 && !isAdmin.Check(player))
				{
					if (!player.SpawnedVehicles[0].IsDisposed)
						player.SpawnedVehicles[0].Dispose();
					player.SpawnedVehicles.RemoveAt(0);
				}
				Random rndColor = new();
				BaseVehicle v = BaseVehicle.Create(model, new Vector3(player.Position.X + 5.0, player.Position.Y, player.Position.Z), 0.0f, rndColor.Next(0, 255), rndColor.Next(0, 255));
				v.VirtualWorld = player.VirtualWorld;
				player.SpawnedVehicles.Add(v);
				if (!player.DisableForceEnterVehicle)
				{
					player.PutInVehicle(v, 0);
					SampSharp.GameMode.Events.EnterVehicleEventArgs e = new(player, v, false);
					player.OnEnterVehicle(e);
				}
			}
		}
		[Command("nitro")]
		private static void NitroCommand(Player player)
		{
			if (player.pEvent == null)
			{
				player.NitroEnabled = !player.NitroEnabled;
				if (player.NitroEnabled)
				{
					if (player.InAnyVehicle)
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
		[Command("enable-force-enter")]
		private static void EnableForceEnterCommand(Player player)
		{
			player.DisableForceEnterVehicle = false;
			player.SendClientMessage("The command /v will put you in the vehicle");
		}
		[Command("disable-force-enter")]
		private static void DisableForceEnterCommand(Player player)
		{
			player.DisableForceEnterVehicle = true;
			player.SendClientMessage("The command /v will not put you in the vehicle anymore");
		}

		[Command("re")]
		private static void ReCommand(Player player)
		{
			if (!player.IsInEvent && player.InAnyVehicle)
			{
				player.Vehicle.Angle = player.Vehicle.Angle;
				player.Vehicle.Repair();
			}
        }
        [Command("rep")]
        private static void RepCommand(Player player)
        {
            if (!player.IsInEvent && player.InAnyVehicle)
            {
                player.Vehicle.Repair();
                player.Notificate("Vehicle repaired");
            }
        }
        [Command("randomcolor")]
        private static void RandomColorCommand(Player player)
        {
            if (player.InAnyVehicle)
            {
                Random rdm = new();
                player.Vehicle.ChangeColor(rdm.Next(255), rdm.Next(255));
            }
        }
        [Command("getcolor")]
        private static void GetVehicleColorCommand(Player player)
        {
            if (player.InAnyVehicle)
            {
                player.Vehicle.GetColor(out int c1, out int c2);
                player.SendClientMessage("" + c1);
                player.SendClientMessage("" + c2);
            }
        }
        [Command("color")]
        private static void VehicleColorCommand(Player player, int color1, int color2)
        {
            if (player.InAnyVehicle)
            {
                Random rdm = new();
                player.Vehicle.ChangeColor(color1, color2);
            }
        }
    }
}
