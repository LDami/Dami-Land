﻿using SampSharp.GameMode;
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
						Random rdm = new Random();
						int panels, doors, lights, tires;
						this.Player.Vehicle.GetDamageStatus(out panels, out doors, out lights, out tires);
						this.Player.Vehicle.UpdateDamageStatus(panels, doors, lights, rdm.Next(15));
						SampSharp.GameMode.SAMP.Timer.RunOnce(rdm.Next(10000, 30000), () =>
						{
							this.Player.Vehicle.GetDamageStatus(out panels, out doors, out lights, out tires);
							this.Player.Vehicle.UpdateDamageStatus(panels, doors, lights, 0);
							this.EventConsumed = true;
						});
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
				case AvailableEvents.GiveMissile:
					if (this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("You received a missile ! Launch it with the submission key !");
						this.Player.KeyStateChanged += OnPlayerKeyStateChanged;
					}
					break;
				case AvailableEvents.CreateProtectiveSphere:
					if (this.Player.InAnyVehicle)
					{
						this.Player.SendClientMessage("You received a protective sphere for 10 seconds !");
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
					this.EventConsumed = true;
					this.Player.KeyStateChanged -= OnPlayerKeyStateChanged;
				}
				else if(this.Event == AvailableEvents.GiveMissile)
				{
					System.Numerics.Vector3 playerPos = new System.Numerics.Vector3(this.Player.Position.X, this.Player.Position.Y, this.Player.Position.Z);
					/*
					System.Numerics.Vector3 direction = new System.Numerics.Vector3(
						2 * vehQ.X * vehQ.Y - 2 * vehQ.Z * vehQ.W,
						(float)(1 - 2 * Math.Pow(vehQ.X, 2) - 2 * Math.Pow(vehQ.Z, 2)),
						2 * vehQ.Y * vehQ.W + 2 * vehQ.X * vehQ.W
					);
					System.Numerics.Vector3 direction = new System.Numerics.Vector3(
						vehE.X, vehE.Y, vehE.Z
					);
					Single dist = 100.0f;
					Player.SendClientMessage("direction.X=" + direction.X + " ; direction.Y=" + direction.Y + " ; direction.Z=" + direction.Z);
					System.Numerics.Vector3 endPos = System.Numerics.Vector3.Add(playerPos, new System.Numerics.Vector3(direction.X * dist, direction.Y * dist, direction.Z));
					Vector3 objPos = new Vector3(endPos.X, endPos.Y, endPos.Z);
					Player.SendClientMessage("objPos.X=" + objPos.X + " ; objPos.Y=" + objPos.Y + " ; objPos.Z=" + objPos.Z);
					marker = new DynamicObject(18728, objPos, Vector3.Zero, 0);
					*/
					/*
					endPos.X = playerPos.Position.X + (dist * Math.Sin(Math.PI*playerPos.Rotation/180.0));
					endPos.Y = playerPos.Position.Y + (dist * Math.Cos(Math.PI * playerPos.Rotation / 180.0));
					Physics.RayCastCollisionTarget target = Physics.ColAndreas.RayCastLine(playerPos.Position, this.Player.Get)
						CA_RayCastLine(
							playerPos.Position.X + (maxDistance * floatsin(-r, degrees)), playerPos.Position.Y + (maxDistance * floatcos(-r, degrees)), playerPos.Position.Z, playerPos.Position.X + (maxDistance * floatsin(-r, degrees)), playerPos.Position.Y + (maxDistance * floatcos(-r, degrees)), playerPos.Position.Z, tmp, tmp, tmp)
					this.EventConsumed = true;
					this.Player.KeyStateChanged -= OnPlayerKeyStateChanged;
					*/

					float roofZ = BaseVehicle.GetModelInfo(this.Player.Vehicle.Model, SampSharp.GameMode.Definitions.VehicleModelInfoType.Size).Z / 2;
					DynamicObject box = new DynamicObject(3788, this.Player.Vehicle.Position, new Vector3(0.0, 0.0, 90.0));
					box.AttachTo(this.Player.Vehicle, new Vector3(0.0, 0.0, roofZ+0.2), new Vector3(0.0, 0.0, 90.0));
					DynamicObject missile = new DynamicObject(3790, this.Player.Vehicle.Position, new Vector3(0.0, 0.0, 90.0));
					missile.AttachTo(this.Player.Vehicle, new Vector3(0.0, 0.0, roofZ + 0.2), new Vector3(0.0, 0.0, 90.0));
					DynamicObject sparks = new DynamicObject(18718, missile.Position, new Vector3(0.0, 90.0, 90.0));
					sparks.AttachTo(this.Player.Vehicle, new Vector3(0.0, -1.0, roofZ + 0.2), new Vector3(0.0, 90.0, 90.0));

					SampSharp.GameMode.SAMP.Timer.RunOnce(1000, () =>
					{
						if(this.Player.InAnyVehicle) // Need to be recheck because of timer
						{
							Vector3 missilePos = this.Player.Vehicle.Position + new Vector3(0.0, 0.0, roofZ + 0.75);
							missile.Dispose();
							missile = new DynamicObject(3790, missilePos, new Vector3(0.0, 0.0, 90.0));
							missile.Move(missile.Position + new Vector3(0.0, 0.0, 1.0), 3f);
							sparks.Dispose();
							sparks = new DynamicObject(18718, this.Player.Vehicle.Position + new Vector3(0.0, 0.0, roofZ + 0.5), new Vector3(0.0, 90.0, 90.0));
							sparks.Move(sparks.Position + new Vector3(0.0, 0.0, 0.5), 2.0f);
							SampSharp.GameMode.SAMP.Timer.RunOnce(1000, () =>
							{
								if (this.Player.InAnyVehicle) // Need to be recheck because of timer
								{
									Quaternion vehQ = this.Player.Vehicle.GetRotationQuat();
									Vector3 vehE = Physics.ColAndreas.QuatToEuler(vehQ);
									missilePos += new Vector3(0.0, 0.0, 1.0); // Last move
									Vector3 dest = missilePos + new Vector3(
										-MissileRange * Math.Sin(Math.PI * vehE.Z / 180.0),
										MissileRange * Math.Cos(Math.PI * vehE.Z / 180.0),
										MissileRange * Math.Sin(Math.PI * vehE.X / 180.0)
										);
									Physics.ColAndreas.FindZ_For2DCoord(dest.X, dest.Y, out float z);
									dest = new Vector3(dest.X, dest.Y, z);

									missile.Dispose();
									missile = new DynamicObject(3790, missilePos, vehE);
									missile.Move(dest, MissileSpeed);
									DynamicObject smoke = new DynamicObject(18694, missilePos, vehE);
									smoke.Move(dest, MissileSpeed);
									missile.Disposed += (sender, args) =>
									{
										smoke.Dispose();
									};
									Physics.CollisionManager.ExplodeOnCollision(missile, dest, this.Player);
									sparks.Dispose();
								}
							});
						}
					});
					SampSharp.GameMode.SAMP.Timer.RunOnce(15000, () =>
					{
						box.Dispose();
						missile.Dispose();
						sparks.Dispose();
					});

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