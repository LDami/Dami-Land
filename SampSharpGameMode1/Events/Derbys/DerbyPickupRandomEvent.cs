using SampSharp.GameMode;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using System;
using System.Linq;
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
		const float MissileSpeed = 25.0f;
		const float MissileRange = 250.0f;

		private BasePlayer Player {get;set; }
		private AvailableEvents Event { get; set; }
		private bool EventConsumed { get; set; }

		private DynamicObject marker;
		private DynamicObject protectiveSphere;

		public DerbyPickupRandomEvent(BasePlayer player, int eventid = -1)
		{
			this.Player = player;
			Array events = Enum.GetValues(typeof(AvailableEvents));
			Random rdm = new Random();
			if (eventid == -1 || eventid > events.Length)
				this.Event = (AvailableEvents)events.GetValue(rdm.Next(events.Length));
			else
				this.Event = (AvailableEvents)eventid;
			this.EventConsumed = false;
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
						BasePlayer.SendClientMessageToAll(this.Player.Name + " has some flat tires !");
						Random rdm = new Random();
						int panels, doors, lights, tires;
						this.Player.Vehicle.GetDamageStatus(out panels, out doors, out lights, out tires);
						this.Player.Vehicle.UpdateDamageStatus(panels, doors, lights, rdm.Next(15));
						SampSharp.GameMode.SAMP.Timer.RunOnce(rdm.Next(10000, 30000), () =>
						{
							if(this.Player.InAnyVehicle)
							{
								this.Player.Vehicle.GetDamageStatus(out panels, out doors, out lights, out tires);
								this.Player.Vehicle.UpdateDamageStatus(panels, doors, lights, 0);
								this.EventConsumed = true;
							}
						});
					}
					break;
				case AvailableEvents.GiveAirSupport:
					if (this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("You received an air support ! Place the target with the submission key and wait !");
						BasePlayer.SendClientMessageToAll(this.Player.Name + " received an air support !");
						marker = new DynamicObject(19133, this.Player.Position + new Vector3(0.0, 10.0, 0.0), new Vector3(0.0, 90.0, 0.0), 0, -1, this.Player);
						marker.ShowForPlayer(this.Player);
						marker.AttachTo(this.Player.Vehicle, new Vector3(0.0, 10.0, 0.0), Vector3.Zero);
						this.Player.KeyStateChanged += OnPlayerKeyStateChanged;
					}
					break;
				case AvailableEvents.GiveMissile:
					if (this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("You received a missile ! Launch it with the submission key !");
						BasePlayer.SendClientMessageToAll(this.Player.Name + " received a missile !");
						this.Player.KeyStateChanged += OnPlayerKeyStateChanged;
					}
					break;
				case AvailableEvents.CreateProtectiveSphere:
					if (this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("You received a protective sphere for 10 seconds !");
						BasePlayer.SendClientMessageToAll(this.Player.Name + " received a protective sphere !");
						protectiveSphere = new DynamicObject(11712, this.Player.Position, default, 0, -1);
						protectiveSphere.AttachTo(this.Player.Vehicle, new Vector3(0.0,0.0,1.0), Vector3.Zero);
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
			if (eventArgs.NewKeys == SampSharp.GameMode.Definitions.Keys.Submission && !this.EventConsumed && this.Player.InAnyVehicle)
			{
				if(this.Event == AvailableEvents.GiveAirSupport && !marker.IsDisposed)
				{
					Vector3 markerPos = Utils.GetPositionFrontOfPlayer(this.Player, 10);
					marker.Dispose();
					marker = new DynamicObject(18728, markerPos + new Vector3(0.0, 0.0, -5.0), Vector3.Zero, this.Player.VirtualWorld);
					Random rdm = new Random();
					Vector3 explosionOffset = Vector3.Zero;
					SampSharp.GameMode.SAMP.Timer.RunOnce(8000, () =>
					{
						explosionOffset = new Vector3(rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), 0.0);
						foreach (BasePlayer p in BasePlayer.GetAll<BasePlayer>().Where(p => p.VirtualWorld == this.Player.VirtualWorld))
						{
							p.CreateExplosion(markerPos + explosionOffset, SampSharp.GameMode.Definitions.ExplosionType.LargeVisibleDamageFire, 50.0f);
						}
					});
					SampSharp.GameMode.SAMP.Timer.RunOnce(8100, () =>
					{
						explosionOffset = new Vector3(rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), 0.0);
						foreach (BasePlayer p in BasePlayer.GetAll<BasePlayer>().Where(p => p.VirtualWorld == this.Player.VirtualWorld))
						{
							p.CreateExplosion(markerPos + explosionOffset, SampSharp.GameMode.Definitions.ExplosionType.LargeVisibleDamageFire, 50.0f);
						}
					});
					SampSharp.GameMode.SAMP.Timer.RunOnce(8200, () =>
					{
						explosionOffset = new Vector3(rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), rdm.Next(-MaxAirSupportRdmRange, MaxAirSupportRdmRange), 0.0);
						foreach (BasePlayer p in BasePlayer.GetAll<BasePlayer>().Where(p => p.VirtualWorld == this.Player.VirtualWorld))
						{
							p.CreateExplosion(markerPos + explosionOffset, SampSharp.GameMode.Definitions.ExplosionType.LargeVisibleDamageFire, 50.0f);
						}
					});
					SampSharp.GameMode.SAMP.Timer.RunOnce(10000, () =>
					{
						marker.Dispose();
					});
					this.EventConsumed = true;
					this.Player.KeyStateChanged -= OnPlayerKeyStateChanged;
				}
				else if(this.Event == AvailableEvents.GiveMissile)
				{
					// Creating missile
					float roofZ = BaseVehicle.GetModelInfo(this.Player.Vehicle.Model, SampSharp.GameMode.Definitions.VehicleModelInfoType.Size).Z / 2;
					DynamicObject box = new DynamicObject(3788, this.Player.Vehicle.Position, new Vector3(0.0, 0.0, 90.0));
					box.AttachTo(this.Player.Vehicle, new Vector3(0.0, 0.0, roofZ+0.2), new Vector3(0.0, 0.0, 90.0));
					DynamicObject missile = new DynamicObject(3790, this.Player.Vehicle.Position, new Vector3(0.0, 0.0, -90.0));
					missile.AttachTo(this.Player.Vehicle, new Vector3(0.0, 0.0, roofZ + 0.2), new Vector3(0.0, 0.0, -90.0));
					DynamicObject sparks = new DynamicObject(18718, missile.Position, new Vector3(0.0, 90.0, 90.0));
					sparks.AttachTo(this.Player.Vehicle, new Vector3(0.0, -0.4, roofZ + 0.2), new Vector3(0.0, 90.0, -90.0));

					missile.ToggleUpdate(this.Player, true);
					sparks.ToggleUpdate(this.Player, true);

					SampSharp.GameMode.SAMP.Timer.RunOnce(1000, () =>
					{
						// Bringing up missile
						if(this.Player.InAnyVehicle) // Need to be recheck because of timer
						{
							Quaternion vehQ = this.Player.Vehicle.GetRotationQuat();

							Vector3 vehE = Physics.ColAndreas.QuatToEuler(vehQ);

							Vector3 missilePos = this.Player.Vehicle.Position + new Vector3(0.0, 0.0, roofZ + 0.2);
							missile.Dispose();
							missile = new DynamicObject(3790, missilePos, new Vector3(vehE.X, vehE.Y, this.Player.Vehicle.Angle -90));
							missile.SetNoCameraCollision();
							missile.Move(missile.Position + new Vector3(0.0, 0.0, 1.0), 1f);
							sparks.Dispose();
							sparks = new DynamicObject(18718, missilePos, new Vector3(0, 90, this.Player.Vehicle.Angle - 90));
							sparks.SetNoCameraCollision();
							sparks.Move(sparks.Position + new Vector3(0.0, 0.0, 1.0), 1.0f);
							DynamicObject smoke = null;
							SampSharp.GameMode.SAMP.Timer.RunOnce(500, () =>
							{
								smoke = new DynamicObject(18694, missilePos + new Vector3(0, -0.15, 2.65), vehE + new Vector3(180, 0, 0));
								smoke.AttachTo(missile, new Vector3(1.2, -0.015, -1.6), new Vector3(0, 0, -90));
								smoke.SetNoCameraCollision();
								sparks.Dispose();
							});
							SampSharp.GameMode.SAMP.Timer.RunOnce(1000, () =>
							{
								// Launching missile on target
								if (this.Player.InAnyVehicle) // Need to be recheck because of timer
								{
									missilePos += new Vector3(0.0, 0.0, 1.0); // Last move
									Vector3 dest = missilePos + new Vector3(
										-MissileRange * Math.Sin(Math.PI * vehE.Z / 180.0),
										MissileRange * Math.Cos(Math.PI * vehE.Z / 180.0),
										MissileRange * Math.Sin(Math.PI * vehE.X / 180.0)
										);
									Physics.ColAndreas.FindZ_For2DCoord(dest.X, dest.Y, out float z);
									dest = new Vector3(dest.X, dest.Y, z);

									missile.Move(dest, MissileSpeed);
									//if(!(smoke is null)) smoke.Move(dest, MissileSpeed);
									missile.Disposed += (sender, args) =>
									{
										if (!(smoke is null)) smoke.Dispose();
									};
									Physics.CollisionManager.ExplodeOnCollision(missile, dest, this.Player);
								}
							});
						}
					});
					SampSharp.GameMode.SAMP.Timer.RunOnce(10000, () =>
					{
						box.Dispose();
						missile.Dispose();
						sparks.Dispose();
					});
					this.EventConsumed = true;
					this.Player.KeyStateChanged -= OnPlayerKeyStateChanged;
				}
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
