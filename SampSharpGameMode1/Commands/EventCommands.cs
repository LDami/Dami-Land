using System;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharpGameMode1.Display;

namespace SampSharpGameMode1.Commands
{
    class EventCommands
    {
        [Command("event")]
        private static void MappingCommand(Player player)
        {
            player.SendClientMessage($"Usage: {ColorPalette.Secondary.Main}/event [action]");
            player.SendClientMessage($"Actions: {ColorPalette.Secondary.Main}help, manage, purge, join, leave");
        }
        [CommandGroup("event")]
        class EventsCommandsClass
        {
            [Command("help")]
            private static void HelpCommand(Player player)
            {
                string list =
                    $"{ColorPalette.Primary.Main}/event manage {ColorPalette.Primary.Darken}Open the event manager" + "\n" +
                    $"{ColorPalette.Primary.Main}/event purge {ColorPalette.Primary.Darken}Remove all upcoming events (admin only)" + "\n" +
                    $"{ColorPalette.Primary.Main}/event join (shortcut: /join) {ColorPalette.Primary.Darken}Join the opened event" + "\n" +
                    $"{ColorPalette.Primary.Main}/event leave (shortcut: /leave) {ColorPalette.Primary.Darken}Leave the event you are in" + "\n"
                    ;
                MessageDialog dialog = new MessageDialog("Command list", list, "Close");
                dialog.Show(player);
            }
            [Command("manage", PermissionChecker = typeof(AdminPermissionChecker))]
            private static void CreateEvent(Player player)
            {
                GameMode.eventManager.ShowManagerDialog(player);
            }
            [Command("purge", PermissionChecker = typeof(AdminPermissionChecker))]
            private static void PurgeEvent(Player player)
            {
                GameMode.eventManager.PurgeEvents(player);
            }

			[Command("join", Shortcut = "join")]
			private static void JoinEvent(Player player)
			{
				if (player.eventCreator is null)
				{
					if (GameMode.eventManager.openedEvent == null)
						player.SendClientMessage(Color.Wheat, "[Event]" + ColorPalette.Error.Main + " There is no opened event !");
                    else
                    {
                        if (player.pEvent == null)
                            GameMode.eventManager.openedEvent.Join(player);
                        else
                            player.SendClientMessage(Color.Wheat, "[Event]" + ColorPalette.Error.Main + " You are already in an event !");
                    }
				}
				else
                {
					player.SendClientMessage(Color.Wheat, "[Event]" + ColorPalette.Error.Main + " Close your editor first");
                }
			}

            [Command("leave", Shortcut = "leave")]
            private static void LeaveEvent(Player player)
            {
                if (player.pEvent != null)
                    player.pEvent.Leave(player);
            }
        }
    }
}
