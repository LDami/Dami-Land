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
			this.player.KeyStateChanged -= Player_KeyStateChanged;
			foreach (BaseVehicle v in vehicles)
			{
				if (!v.IsDisposed)
					v.Dispose();
			}
			vehicles.Clear();
			if(!moverObject.IsDisposed) moverObject.Dispose();

			this.player.CancelEdit();
			this.player.cameraController.SetBehindPlayer();
			this.player.cameraController.Enabled = false;
			Quit?.Invoke(this, e);
		}

		public List<BaseVehicle> vehicles = new List<BaseVehicle>();
		public int world;
		public VehicleModelType model;

		private PlayerObject moverObject;
		private const int moverObjectModelID = 19133;
		private Vector3 moverObjectOffset = new Vector3(0.0f, 0.0f, 1.0f);
		private float moverObjectTempZAngle;

		private Player player;
		private Stopwatch getKeysTimer;
		/// <summary>
		///		Number of milliseconds the script must not check for player's key in Player_Update
		/// </summary>
		private const int maxGetKeysTimer = 125;

		private int selectionIndex;

		public SpawnerCreator(Player player, int world, VehicleModelType model) : this(player, world, model, new List<Vector3R>()) { }
		public SpawnerCreator(Player player, int world, VehicleModelType model, List<Vector3R> existingSpawnPoints)
		{
			this.player = player;
			this.player.Update += Player_Update; // Used to detect left/right arrow
			this.player.KeyStateChanged += Player_KeyStateChanged;
			this.player.cameraController.Enabled = true;
			this.getKeysTimer = new Stopwatch();
			getKeysTimer.Start();

			this.world = world;
			this.model = model;

			vehicles = existingSpawnPoints.ConvertAll(
				new Converter<Vector3R, BaseVehicle>(Vector3RToBaseVehicle));
			if (vehicles.Count > 0)
			{
				selectionIndex = 0;
				vehicles[selectionIndex].ChangeColor(1, 1);
				moverObject = new PlayerObject(this.player, moverObjectModelID, vehicles[selectionIndex].Position + moverObjectOffset, vehicles[selectionIndex].Rotation);
				moverObject.Edited += MoverObject_Edited;
				player.SendClientMessage("Spawn Creator loaded, here is the controls:");
				player.SendClientMessage("    Fire button (default: left mouse or CTRL):   Edit current vehicle's position");
				player.SendClientMessage("    Handbrake button (default: Space):           Refresh editor");
				player.SendClientMessage("    Crouch button (default: C):                  Quit editor");
				player.SendClientMessage("    left button:                                 Go to previous vehicle");
				player.SendClientMessage("    right button:                                Go to next vehicle");
				player.cameraController.SetFree();
				UpdatePlayerCamera();
			}
			else
				OnQuit(new SpawnCreatorQuitEventArgs(new List<Vector3R>()));
		}

		private void Player_Update(object sender, SampSharp.GameMode.Events.PlayerUpdateEventArgs e)
		{
			if(this.getKeysTimer.ElapsedMilliseconds >= maxGetKeysTimer)
			{
				if (this.getKeysTimer.IsRunning) this.getKeysTimer.Stop();
				((Player)sender).GetKeys(out Keys keys, out int updown, out int leftright);
				if (leftright == -128) // Left
				{
					if (selectionIndex > 0)
					{
						vehicles[selectionIndex].ChangeColor(0, 0);
						selectionIndex--;
					}
					vehicles[selectionIndex].ChangeColor(1, 1);
					moverObject.Position = vehicles[selectionIndex].Position;
					UpdatePlayerCamera();
					player.Notificate(selectionIndex + "/" + (vehicles.Count - 1));
					this.getKeysTimer.Restart();
				}
				if (leftright == 128) // Right
				{
					if (selectionIndex < vehicles.Count)
					{
						vehicles[selectionIndex].ChangeColor(0, 0);
						selectionIndex++;
					}
					if (selectionIndex == vehicles.Count) // Create a new spawn position
					{
						vehicles.Add(BaseVehicle.Create(model, vehicles[selectionIndex - 1].Position + new Vector3(0.0, 0.0, 2.0), 0.0f, 1, 1));
					}
					vehicles[selectionIndex].ChangeColor(1, 1);
					moverObject.Position = vehicles[selectionIndex].Position;
					UpdatePlayerCamera();
					player.Notificate(selectionIndex + "/" + (vehicles.Count - 1));
					this.getKeysTimer.Restart();
				}
			}
		}

		private void Player_KeyStateChanged(object sender, SampSharp.GameMode.Events.KeyStateChangedEventArgs e)
		{
			switch(e.NewKeys)
			{
				case Keys.Fire:
					if(!moverObject.IsDisposed) moverObject.Edit();
					break;

				case Keys.Handbrake:
					moverObject.Position = vehicles[selectionIndex].Position;
					UpdatePlayerCamera();
					break;

				case Keys.Crouch:
					OnQuit(new SpawnCreatorQuitEventArgs(GetSpawnPoints()));
					break;
			}
		}

		private void MoverObject_Edited(object sender, SampSharp.GameMode.Events.EditPlayerObjectEventArgs e)
		{
			if (e.EditObjectResponse == EditObjectResponse.Cancel)
			{
			}
			else
			{
				Physics.ColAndreas.FindZ_For2DCoord(e.Position.X, e.Position.Y, out float zPos);
				float deltaZ = BaseVehicle.GetModelInfo(vehicles[selectionIndex].Model, VehicleModelInfoType.Size).Z / 2;
				Vector3 finalPos = new Vector3(e.Position.X, e.Position.Y, zPos + deltaZ);
				vehicles[selectionIndex].Position = finalPos;
				moverObjectTempZAngle = e.Rotation.Z;

				moverObject.Position = finalPos;
				// TODO: Z position of moverObject is not visually updated after new Z pos found by ColAndreas
				if (e.EditObjectResponse == EditObjectResponse.Update)
				{
					UpdatePlayerCamera();
				}
			}
		}

		private void UpdatePlayerCamera()
		{
			player.cameraController.MoveTo(new Vector3(vehicles[selectionIndex].Position.X + 10.0, vehicles[selectionIndex].Position.Y + 10.0, vehicles[selectionIndex].Position.Z + 10.0));
			player.cameraController.MoveToTarget(vehicles[selectionIndex].Position);
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
