﻿using System;
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
                Display.CommandList commandList = new Display.CommandList("Event command list");
                commandList.Add("/event manage", "Open the event manager");
                commandList.Add("/event purge", "Remove all upcoming events (admin only)");
                commandList.Add("/event join (shortcut: /join)", "Join the opened event");
                commandList.Add("/event leave (shortcut: /leave)", "Leave the event you are in");
                commandList.Show(player);
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
                if (!(player.eventCreator is null))
                {
                    player.SendClientMessage(Color.Wheat, "[Event]" + ColorPalette.Error.Main + " Close your editor first");
                    return;
                }
                if (GameMode.eventManager.openedEvent is null)
                {
                    player.SendClientMessage(Color.Wheat, "[Event]" + ColorPalette.Error.Main + " There is no opened event !");
                    return;
                }
                if (!(player.pEvent is null))
                {
                    player.SendClientMessage(Color.Wheat, "[Event]" + ColorPalette.Error.Main + " You are already in an event !");
                    return;
                }
                GameMode.eventManager.openedEvent.Join(player);
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
