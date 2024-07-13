using SampSharp.GameMode.SAMP.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Commands
{
    class WorkCommands
    {
        [Command("leavework")]
        private static void LeaveWorkCommand(Player player)
        {
            player.pWork?.StopWork(player);
        }
    }
}
