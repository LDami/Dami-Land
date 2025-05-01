using SampSharp.GameMode.SAMP.Commands.PermissionCheckers;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1
{
	public class AdminPermissionChecker : IPermissionChecker
	{
        #region Implementation of IPermissionChecker

        /// <summary>
        ///     Gets the message displayed when the player is denied permission.
        /// </summary>
        public string Message
        {
            get { return "You need to be admin to run this command."; }
        }

        /// <summary>
        ///     Checks the permission for the specified player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>true if allowed; false if denied.</returns>
        public bool Check(BasePlayer player)
        {
            Player p = (Player)player;
            return p.Adminlevel >= 1 || p.IsAdmin;
        }
        #endregion
    }
}
