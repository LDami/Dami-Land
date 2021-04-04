using SampSharp.GameMode;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SampSharpGameMode1.Events.Derbys
{
	public class DerbyPickupRandomEvent
	{
		enum AvailableEvents
		{
			FlatSomeTires = 0,
			GiveAirSupport,
			GiveMissile,
			CreateProtectiveSphere,
		}

		const int MaxAirSupportRdmRange = 10;
		const int ProtectiveSphereRange = 15;

		private Player Player {get;set; }
		private AvailableEvents Event { get; set; }

		private DynamicObject marker;
		private DynamicObject protectiveSphere;

		public DerbyPickupRandomEvent(Player player, int eventid = -1)
		{
			this.Player = player;
			Array events = Enum.GetValues(typeof(AvailableEvents));
			Random rdm = new Random();
			if (eventid == -1 || eventid > events.Length)
				this.Event = (AvailableEvents)events.GetValue(rdm.Next(events.Length));
			else
				this.Event = (AvailableEvents)eventid;
			this.Execute();
		}

		protected void Execute()
		{
			switch(this.Event)
			{
				case AvailableEvents.FlatSomeTires:
					if(this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("Be carefull, your vehicle has some flat tires !");
						Random rdm = new Random();
						int panels, doors, lights, tires;
						this.Player.Vehicle.GetDamageStatus(out panels, out doors, out lights, out tires);
						this.Player.Vehicle.UpdateDamageStatus(panels, doors, lights, rdm.Next(15));
					}
					break;
				case AvailableEvents.GiveAirSupport:
					if (this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("You received an air support ! Place the target with the submission key and wait !");
						marker = new DynamicObject(19133, this.Player.Position + new Vector3(0.0, 10.0, 0.0), new Vector3(0.0, 90.0, 0.0), 0, -1, this.Player);
						marker.ShowForPlayer(this.Player);
						marker.AttachTo(this.Player.Vehicle, new Vector3(0.0, 10.0, 0.0), Vector3.Zero);
						this.Player.KeyStateChanged += OnPlayerKeyStateChanged;
					}
					break;
				case AvailableEvents.CreateProtectiveSphere:
					if (this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("You received a protective sphere for 10 seconds !");
						protectiveSphere = new DynamicObject(11712, this.Player.Position, default, 0, -1);
						protectiveSphere.AttachTo(this.Player.Vehicle, new Vector3(0.0,0.0,3.0), Vector3.Zero);
						//UpdateProtectiveSpherePosition();
						DynamicArea area = DynamicArea.CreateCircle(this.Player.Position.X, this.Player.Position.X, ProtectiveSphereRange, 0);
						area.AttachTo(this.Player.Vehicle);
						area.Enter += (sender, eventArgs) =>
						{
							if(!eventArgs.Player.Equals(this.Player) && eventArgs.Player.InAnyVehicle)
							{
								BaseVehicle vehicle = eventArgs.Player.Vehicle;
								Quaternion rot = vehicle.GetRotationQuat();
								Matrix matrix = Matrix.CreateFromQuaternion(rot);
								vehicle.Velocity = matrix.Backward.Normalized() * 0.5f;
							}
						};
						SampSharp.GameMode.SAMP.Timer.RunOnce(10000, () =>
						{
							protectiveSphere.Dispose();
							area.Dispose();
						});
					}
					break;
			}
		}

		private void OnPlayerKeyStateChanged(Object sender, KeyStateChangedEventArgs eventArgs)
		{
			if (eventArgs.NewKeys == SampSharp.GameMode.Definitions.Keys.Submission && this.Event == AvailableEvents.GiveAirSupport && !marker.IsDisposed)
			{
				Vector3 markerPos = marker.Position;
				marker.Dispose();
				marker = new DynamicObject(18728, markerPos + new Vector3(0.0, 0.0, -5.0), Vector3.Zero, 0);
				Random rdm = new Random();
				Vector3 explosionOffset = Vector3.Zero;
				SampSharp.GameMode.SAMP.Timer.RunOnce(8000, () =>
				{
					explosionOffset = new Vector3(rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), 0.0);
					BasePlayer.CreateExplosionForAll(markerPos + explosionOffset, SampSharp.GameMode.Definitions.ExplosionType.LargeVisibleDamageFire, 50.0f);
				});
				SampSharp.GameMode.SAMP.Timer.RunOnce(8100, () =>
				{
					explosionOffset = new Vector3(rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), 0.0);
					BasePlayer.CreateExplosionForAll(markerPos + explosionOffset, SampSharp.GameMode.Definitions.ExplosionType.LargeVisibleDamageFire, 50.0f);
				});
				SampSharp.GameMode.SAMP.Timer.RunOnce(8200, () =>
				{
					explosionOffset = new Vector3(rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), 0.0);
					BasePlayer.CreateExplosionForAll(markerPos + explosionOffset, SampSharp.GameMode.Definitions.ExplosionType.LargeVisibleDamageFire, 50.0f);
				});
				SampSharp.GameMode.SAMP.Timer.RunOnce(10000, () =>
				{
					marker.Dispose();
				});
				this.Player.KeyStateChanged -= OnPlayerKeyStateChanged;
			}
		}

		private void UpdateProtectiveSpherePosition()
		{
			Thread t = new Thread(() =>
			{
				double sinV = 0.0, cosV = 1.0;
				bool sinIncrement = true, cosIncrement = false;
				Vector3 newOffset;
				while (!protectiveSphere.IsDisposed && this.Player.InAnyVehicle)
				{
					newOffset = new Vector3(Math.Cos(cosV) * ProtectiveSphereRange, Math.Sin(sinV) * ProtectiveSphereRange, 0.0);
					//Logger.WriteLineAndClose("newPos: " + newOffset.ToString());
					//Logger.WriteLineAndClose("trigo: " + new Vector3(Math.Cos(cosV), Math.Sin(sinV), 0.0).ToString());
					Logger.WriteLineAndClose("cos: " + cosV + ", sin: " + sinV);
					//protectiveSphere.AttachTo(this.Player.Vehicle, newOffset, Vector3.Zero);
					protectiveSphere.Position = this.Player.Vehicle.Position + newOffset;
					if (sinV <= -1) sinIncrement = true;
					if (sinV >= 1) sinIncrement = false;
					if (cosV <= -1) cosIncrement = true;
					if (cosV >= 1) cosIncrement = false;
					if (sinIncrement) sinV += 0.2;
					else sinV -= 0.2;
					if (cosIncrement) cosV += 0.2;
					else cosV -= 0.2;
					Thread.Sleep(250);
				}
			});
			t.Start();
		}
	}
}
