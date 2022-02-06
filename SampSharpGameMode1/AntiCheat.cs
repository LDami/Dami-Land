using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.SAMP;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
	public class AntiCheat
	{
		public enum SuspiciousBehavior
		{
			UnauthorizedWeaponInEvent = 0
		}

		private Player player;
		private Timer routineTimer;
		public AntiCheat(Player _player)
		{
			this.player = _player;
			routineTimer = new Timer(5000, true);
			routineTimer.Tick += Routine;
		}

		private void Routine(object sender, EventArgs e)
		{
			if (this.player.IsDisposed)
			{
				this.routineTimer.IsRepeating = false;
				this.routineTimer.IsRunning = false;
				this.routineTimer.Dispose();
			}
			else
			{
				if (this.player.IsInEvent)
				{
					if (this.player.Weapon != Weapon.None && this.player.pEvent.Status == Events.EventStatus.Running)
					{
						// BasePlayer.Weapon returns only the weapon held by the player before he enter in a vehicle
						// So the AntiCheat system will only detect the weapon if player leave the vehicle
						// See: https://open.mp/fr/docs/scripting/functions/GetPlayerWeapon
						OnSuspiciousBehavior(SuspiciousBehavior.UnauthorizedWeaponInEvent);
					}
				}
			}
		}

		private void OnSuspiciousBehavior(SuspiciousBehavior behavior)
		{
			switch(behavior)
			{
				case SuspiciousBehavior.UnauthorizedWeaponInEvent:
					this.player.Kick("[AntiCheat] You have been kicked for unauthorized used of weapon in event") ;
					break;
			}
		}
	}
}
