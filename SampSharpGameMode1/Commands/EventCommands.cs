using System;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;

namespace SampSharpGameMode1.Commands
{
    class EventCommands
    {
        [CommandGroup("event")]
        class EventsCommandsClass
        {
            [Command("manage")]
            private static void CreateEvent(Player player)
            {
                GameMode.eventManager.ShowManagerDialog(player);
            }

            [Command("join")]
            private static void JoinEvent(Player player)
            {
                if (GameMode.eventManager.openedEvent == null)
                    player.SendClientMessage(Color.Red, "[Event]" + Color.White + " There is no opened event !");
                else
                    GameMode.eventManager.openedEvent.Join(player);
            }

            [Command("leave")]
            private static void LeaveEvent(Player player)
            {
                if(player.pEvent != null)
                    player.pEvent.Leave(player);
            }
        }
    }
}
