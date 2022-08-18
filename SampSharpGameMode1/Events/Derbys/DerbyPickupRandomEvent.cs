using SampSharp.GameMode;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Physics;
using System;
using System.Linq;
using System.Threading;

namespace SampSharpGameMode1.Events.Derbys
{
	public class DerbyPickupRandomEvent
	{
		public enum AvailableEvents
		{
			FlatSomeTires = 0,
			GiveAirSupport,
			GiveMissile,
			CreateProtectiveSphere,
			Blackout,
			SuperPoweredLaser
		}

		const int MaxAirSupportRdmRange = 10;
		const int ProtectiveSphereRange = 15;
		const float MissileSpeed = 25.0f;
		const float MissileRange = 250.0f;
		const float LaserRange = 20.0f;

		private Player Player {get;set; }
		private AvailableEvents Event { get; set; }
		private bool EventConsumed { get; set; }

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
			this.EventConsumed = false;
			this.Execute();
		}

		protected void Execute()
		{
			Random rdm = new Random();
			switch (this.Event)
			{
				case AvailableEvents.FlatSomeTires:
					if(this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("Be carefull, your vehicle has some flat tires !");
						if (this.Player.pEvent != null)
							this.Player.pEvent.SendEventMessageToAll($"{this.Player.Name} has some flat tires !");
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
						if (this.Player.pEvent != null)
							this.Player.pEvent.SendEventMessageToAll($"{this.Player.Name} received an air support !");
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
						if (this.Player.pEvent != null)
							this.Player.pEvent.SendEventMessageToAll($"{this.Player.Name} received a missile !");
						this.Player.KeyStateChanged += OnPlayerKeyStateChanged;
					}
					break;
				case AvailableEvents.CreateProtectiveSphere:
					if (this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("You received a protective sphere for 10 seconds !");
						if (this.Player.pEvent != null)
							this.Player.pEvent.SendEventMessageToAll($"{this.Player.Name} received a protective sphere !");
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
				case AvailableEvents.Blackout:
					if (this.Player.InAnyVehicle)
					{
						void ToggleAllPlayersVehicle(bool state)
						{
							if (this.Player.pEvent != null)
							{
								foreach (Player p in this.Player.pEvent.Source.GetPlayers())
								{
									if (p != this.Player && p.InAnyVehicle)
									{
										if (!state)
											p.Notificate("Shutdown");
										p.Vehicle.Engine = state;
										p.Vehicle.Lights = state;
									}
								}
							}
						}
						long delay = rdm.Next(3000, 10000);
						this.Player.SendClientMessage($"You have shutdown all the concurrent vehicles for {delay / 1000} seconds !");
						if (this.Player.pEvent != null)
							this.Player.pEvent.SendEventMessageToAll($"{this.Player.Name} has shutdown all the concurrent vehicles for {delay / 1000} seconds !");
						ToggleAllPlayersVehicle(false);
						SampSharp.GameMode.SAMP.Timer.RunOnce(delay, () =>
						{
							ToggleAllPlayersVehicle(true);
						});
					}
					break;
				case AvailableEvents.SuperPoweredLaser:
					if (this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("You received a super powered laser ! You can explode the other competitor with it !");
						if(this.Player.pEvent != null)
							this.Player.pEvent.SendEventMessageToAll($"{this.Player.Name} has a super powered laser !");
						float frontOfVehicle = BaseVehicle.GetModelInfo(this.Player.Vehicle.Model, SampSharp.GameMode.Definitions.VehicleModelInfoType.Size).Z / 2;
						DynamicObject laser = new DynamicObject(18643, this.Player.Vehicle.Position, new Vector3(0.0, 0.0, 90.0), this.Player.VirtualWorld);
						laser.AttachTo(this.Player.Vehicle, new Vector3(0.0, frontOfVehicle, 0.0), new Vector3(0.0, 0.0, 90.0));
						Player.Update += CheckLaserRaycast;
						SampSharp.GameMode.SAMP.Timer.RunOnce(25000, () =>
						{
							laser.Dispose();
							Player.Update -= CheckLaserRaycast;
						});

						void CheckLaserRaycast(object sender, PlayerUpdateEventArgs e)
						{
							BasePlayer player = ((BasePlayer)sender);
							if (player.InAnyVehicle)
							{
								Vector3 vehE = ColAndreas.QuatToEuler(player.Vehicle.GetRotationQuat());
								Vector3 dest = laser.Position + new Vector3(
									-LaserRange * Math.Sin(Math.PI * vehE.Z / 180.0),
									LaserRange * Math.Cos(Math.PI * vehE.Z / 180.0),
									LaserRange * Math.Sin(Math.PI * vehE.X / 180.0)
									);
								RayCastCollisionTarget collisionTarget = ColAndreas.RayCastLine(laser.Position, dest);
								int collisionWithVehicle = -1;
								foreach (BaseVehicle veh in BaseVehicle.All)
								{
									if (!veh.IsDisposed)
									{
										if (Utils.IsInTwoVectors(player.Vehicle.Position, dest, veh.Position) && !player.Vehicle.Equals(veh) && veh.VirtualWorld == player.VirtualWorld)
											collisionWithVehicle = veh.Id;
									}
								}
								if (collisionTarget.Distance < LaserRange)
								{
									float rotZ = (player.Vehicle.Position.Y > collisionTarget.Position.Y) ? 0 : 180;
									DynamicObject sparks = new DynamicObject(18718, collisionTarget.Position, new Vector3(0.0, 90.0, rotZ), worldid: player.VirtualWorld);
									SampSharp.GameMode.SAMP.Timer.RunOnce(500, () =>
									{
										sparks.Dispose();
									});
								}
								BaseVehicle v;
								if (collisionWithVehicle != -1 && (v = BaseVehicle.Find(collisionWithVehicle)) != null)
								{
									foreach (Player p in BasePlayer.GetAll<BasePlayer>().Where(p => p.VirtualWorld == ((BasePlayer)sender).VirtualWorld))
									{
										p.CreateExplosion(v.Position, SampSharp.GameMode.Definitions.ExplosionType.LargeVisibleDamageFire, 50.0f);
										if (p.pEvent != null)
											p.pEvent.SendEventMessage(p, $"You've been blasted by the laser of {player.Name}");
									}
								}
							}
							else
							{
								laser.Dispose();
								Player.Update -= CheckLaserRaycast;
							}
						}
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
					box.SetNoCameraCollision();
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

							Vector3 vehE = ColAndreas.QuatToEuler(vehQ);

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
									vehE = ColAndreas.QuatToEuler(vehQ);
									missilePos += new Vector3(0.0, 0.0, 1.0); // Last move
									Vector3 dest = missilePos + new Vector3(
										-MissileRange * Math.Sin(Math.PI * vehE.Z / 180.0),
										MissileRange * Math.Cos(Math.PI * vehE.Z / 180.0),
										MissileRange * Math.Sin(Math.PI * vehE.X / 180.0)
										);
									ColAndreas.FindZ_For2DCoord(dest.X, dest.Y, out float z);
									dest = new Vector3(dest.X, dest.Y, z);

									missile.Move(dest, MissileSpeed);
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
