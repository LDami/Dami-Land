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
					if (this.player.pEvent.Status == Events.EventStatus.Running)
                    {
						bool hasAnyWeapon = false;
						for(int i = 0; i <= 12; i++)
						{
							player.GetWeaponData(i, out Weapon weapon, out int _);
							if(weapon is not Weapon.None)
							{
								hasAnyWeapon = true;
								break;
							}
						}
						if(hasAnyWeapon)
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
