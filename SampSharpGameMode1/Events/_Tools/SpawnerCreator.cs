using SampSharp.GameMode;
using SampSharp.GameMode.Controllers;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SampSharpGameMode1.Events._Tools
{
	public class SpawnCreatorQuitEventArgs : EventArgs
	{
		public List<Vector3R> spawnPoints { get; set; }
		public SpawnCreatorQuitEventArgs(List<Vector3R> points)
		{
			this.spawnPoints = points;
		}
	}

	public class SpawnerCreator
	{
		public event EventHandler<SpawnCreatorQuitEventArgs> Quit;

		public virtual void OnQuit(SpawnCreatorQuitEventArgs e)
		{
			Unload();
			Quit?.Invoke(this, e);
		}

		public List<BaseVehicle> vehicles = new List<BaseVehicle>();
		public float vehicleWidth;
		public int world;
		public VehicleModelType model;

		private Player player;

		private int selectionIndex;

		public SpawnerCreator(Player player, int world, VehicleModelType model) : this(player, world, model, new List<Vector3R>()) { }
		public SpawnerCreator(Player player, int world, VehicleModelType model, List<Vector3R> existingSpawnPoints)
		{
			this.player = player;
			this.player.KeyStateChanged += Player_KeyStateChanged;

			this.world = world;
			this.model = model;
			this.vehicleWidth = BaseVehicle.GetModelInfo(model, VehicleModelInfoType.Size).Y;
			vehicles = existingSpawnPoints.ConvertAll(
				new Converter<Vector3R, BaseVehicle>(Vector3RToBaseVehicle));
			if (vehicles.Count > 0)
			{
				selectionIndex = 0;
				vehicles[selectionIndex].ChangeColor(1, 1);
				player.SendClientMessage("Spawn Creator loaded, here are the controls:");
				player.SendClientMessage("    keypad 4:                                Go to previous vehicle");
				player.SendClientMessage("    keypad 6:                                Go to next vehicle / Add vehicle");
				player.PutInVehicle(vehicles[selectionIndex]);
			}
			else
				OnQuit(new SpawnCreatorQuitEventArgs(new List<Vector3R>()));
		}

		public void Unload()
		{
			this.player.KeyStateChanged -= Player_KeyStateChanged;
			foreach (BaseVehicle v in vehicles)
			{
				if (!v.IsDisposed)
					v.Dispose();
			}
			vehicles.Clear();
		}

		private void Player_KeyStateChanged(object sender, SampSharp.GameMode.Events.KeyStateChangedEventArgs e)
		{
			switch(e.NewKeys)
			{
				case Keys.AnalogLeft:
					if (selectionIndex > 0)
					{
						vehicles[selectionIndex].ChangeColor(0, 0);
						selectionIndex--;
					}
					vehicles[selectionIndex].ChangeColor(1, 1);
					player.PutInVehicle(vehicles[selectionIndex]);
					player.Notificate(selectionIndex + "/" + (vehicles.Count - 1));
					break;

				case Keys.AnalogRight:
					if (selectionIndex < vehicles.Count)
					{
						vehicles[selectionIndex].ChangeColor(0, 0);
						selectionIndex++;
					}
					if (selectionIndex == vehicles.Count) // Create a new spawn position
					{
						if(selectionIndex % 2 == 0)
						{
							vehicles.Add(BaseVehicle.Create(model, vehicles[selectionIndex - 2].Position + new Vector3(vehicleWidth + 5.0, 0.0, 2.0), vehicles[selectionIndex - 2].Angle, 0, 0));
						}
						else
						{
							vehicles.Add(BaseVehicle.Create(model, vehicles[selectionIndex - 1].Position + new Vector3(vehicleWidth / 2, 5.0, 2.0), vehicles[selectionIndex - 1].Angle, 0, 0));
						}
					}
					vehicles[selectionIndex].ChangeColor(1, 1);
					player.PutInVehicle(vehicles[selectionIndex]);
					player.Notificate(selectionIndex + "/" + (vehicles.Count - 1));
					break;
			}
		}

		public List<Vector3R> GetSpawnPoints()
		{
			return vehicles.ConvertAll(
				new Converter<BaseVehicle, Vector3R>(BaseVehicleToVector3R)
				);
		}

		private BaseVehicle Vector3RToBaseVehicle(Vector3R v)
		{
			BaseVehicle veh = BaseVehicle.Create(this.model, v.Position, v.Rotation, 0, 0);
			//veh.VirtualWorld = world;
			return veh;
		}

		private Vector3R BaseVehicleToVector3R(BaseVehicle v)
		{
			return new Vector3R(v.Position, v.Angle);
		}
	}
}
