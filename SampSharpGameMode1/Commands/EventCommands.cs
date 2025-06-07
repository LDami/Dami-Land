using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharpGameMode1.Display;
using SampSharpGameMode1.Events;
using System.Linq;

#pragma warning disable IDE0051 // Disable useless private members

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
                Display.CommandList commandList = new("Event command list");
                commandList.Add("/event manage", "Open the event manager");
                commandList.Add("/event purge", "Remove all upcoming events (admin only)");
                commandList.Add("/event join (shortcut: /join)", "Join the opened event");
                commandList.Add("/event leave (shortcut: /leave)", "Leave the event you are in");
                commandList.Add("/event spectate (shortcute: /specevent)", "Chose an event to spectate");
                commandList.Show(player);
            }
            [Command("manage", PermissionChecker = typeof(AdminPermissionChecker))]
            private static void CreateEvent(Player player)
            {
                GameMode.EventManager.ShowManagerDialog(player);
            }
            [Command("purge", PermissionChecker = typeof(AdminPermissionChecker))]
            private static void PurgeEvent(Player player)
            {
                GameMode.EventManager.PurgeEvents(player);
            }

			[Command("join", Shortcut = "join")]
			private static void JoinEvent(Player player)
			{
                if (player.eventCreator is not null)
                {
                    player.SendClientMessage(Color.Wheat, "[Event]" + ColorPalette.Error.Main + " Close your editor first");
                    return;
                }
                if (GameMode.EventManager.openedEvent is null)
                {
                    player.SendClientMessage(Color.Wheat, "[Event]" + ColorPalette.Error.Main + " There is no opened event !");
                    return;
                }
                if (player.pEvent is not null)
                {
                    player.SendClientMessage(Color.Wheat, "[Event]" + ColorPalette.Error.Main + " You are already in an event !");
                    return;
                }
                GameMode.EventManager.openedEvent.Join(player, false);
			}

            [Command("leave", Shortcut = "leave")]
            private static void LeaveEvent(Player player)
            {
                player.pEvent?.Leave(player);
            }
            [Command("spectate", Shortcut = "specevent")]
            private static void SpectateEvent(Player player)
            {
                if(GameMode.EventManager.openedEvent != null)
                    GameMode.EventManager.openedEvent.Join(player, true);
                else
                {
                    ListDialog managerDialog = new("Chose the event to spectate", "Select", "Cancel");
                    foreach (Event evt in GameMode.EventManager.RunningEvents)
                    {
                        managerDialog.AddItem(Color.White + "[" + evt.Status.ToString() + "]" + evt.Name);
                    }

                    managerDialog.Show(player);
                    managerDialog.Response += (sender, eventArgs) =>
                    {
                        if (eventArgs.DialogButton == DialogButton.Left)
                        {
                            GameMode.EventManager.RunningEvents.ElementAt(eventArgs.ListItem).SetPlayerInSpectator(player);
                        }
                        else
                        {
                            player.Notificate("Cancelled");
                        }
                    };
                }
            }
        }
    }
}
