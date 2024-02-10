using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
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
		public float vehicleHeight;
		public int world;
		public VehicleModelType model;

		private Player player;

		private int selectionIndex;
		private List<DynamicTextLabel> labels = new List<DynamicTextLabel>();

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
            UpdateTextLabels();
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
            {
				Logger.WriteLineAndClose("SpawnerCreator - SpawnerCreator._:W: SpawnerCreator initialized but no vehicles found. OnQuit called");
				OnQuit(new SpawnCreatorQuitEventArgs(new List<Vector3R>()));
			}
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
					UpdateTextLabels();
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
						BaseVehicle veh;
						if(selectionIndex % 2 == 0)
						{
							veh = BaseVehicle.Create(model, vehicles[selectionIndex - 2].Position + new Vector3(vehicleWidth + 5.0, 0.0, 2.0), vehicles[selectionIndex - 2].Angle, 0, 0);
							veh.VirtualWorld = player.VirtualWorld;
						}
						else
						{
							veh = BaseVehicle.Create(model, vehicles[selectionIndex - 1].Position + new Vector3(vehicleWidth / 2, 5.0, 2.0), vehicles[selectionIndex - 1].Angle, 0, 0);
							veh.VirtualWorld = player.VirtualWorld;
						}
						vehicles.Add(veh);
					}
					vehicles[selectionIndex].ChangeColor(1, 1);
                    UpdateTextLabels();
                    player.PutInVehicle(vehicles[selectionIndex]);
					player.Notificate(selectionIndex + "/" + (vehicles.Count - 1));
					break;
                case Keys.No:
                    {
                        vehicles[selectionIndex].Dispose();
                        List<BaseVehicle> tmp = new List<BaseVehicle>(vehicles);
                        for (int i = selectionIndex + 1; i < tmp.Count; i++)
                        {
                            vehicles[i - 1] = tmp[i];
                        }
                        labels[selectionIndex].Dispose();
                        List<DynamicTextLabel> tmpLabels = new List<DynamicTextLabel>(labels);
                        for (int i = selectionIndex + 1; i < tmpLabels.Count; i++)
                        {
                            labels[i - 1] = tmpLabels[i];
                        }
						selectionIndex = Math.Clamp(selectionIndex, 0, vehicles.Count);
                        vehicles[selectionIndex].ChangeColor(1, 1);
                        UpdateTextLabels();
                        player.PutInVehicle(vehicles[selectionIndex]);
                        break;
                    }
            }
		}

		public void UpdateTextLabels()
		{
			for(int i = 0; i < labels.Count; i ++)
			{
				switch(i + 1)
                {
                    case 1:
                        labels[i].Text = "1st";
                        break;
                    case 2:
                        labels[i].Text = "2nd";
                        break;
                    case 3:
                        labels[i].Text = "3rd";
                        break;
                    default:
                        labels[i].Text = (i + 1).ToString() + "th";
                        break;
                }
				labels[i].ShowForPlayer(player);
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
			BaseVehicle veh = BaseVehicle.Create(this.model, v.Position + Vector3.UnitZ, v.Rotation, 0, 0);
			veh.VirtualWorld = world;
			labels.Add(new DynamicTextLabel("N/A", Color.White, v.Position, 200f, null, veh, false, player.VirtualWorld));
			return veh;
		}

		private Vector3R BaseVehicleToVector3R(BaseVehicle v)
		{
			return new Vector3R(v.Position, v.Angle);
		}
	}
}
