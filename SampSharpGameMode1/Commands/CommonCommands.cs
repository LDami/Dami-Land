using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Commands
{
    class CommonCommands
    {
        [Command("beta")]
        private static void BetaCommand(Player player)
        {
            player.SendClientMessage(ColorPalette.Primary.Main + "These features are still in development and will be ready to test soon:");
            player.SendClientMessage(ColorPalette.Secondary.Main + " - Map creator (in progress)");
            player.SendClientMessage(ColorPalette.Secondary.Main + " - Derby creator and Derby events (need to implement Map creator first)");
            player.SendClientMessage(ColorPalette.Secondary.Main + " - AI / NPC to play with");
            player.SendClientMessage(ColorPalette.Secondary.Main + " - More event types");
        }
        [Command("event-infos")]
        private static void EventInfosCommand(Player player)
        {
            player.SendClientMessage(ColorPalette.Primary.Main + "On this server you can create your own races and derbies, so please read the following instructions:");
            player.SendClientMessage(ColorPalette.Secondary.Main + " - Use /race to see race creator commands, and /derby for derby creator commands");
            player.SendClientMessage(ColorPalette.Secondary.Main + " - Make sure you use a keyboard with numpad (controllers are not supported yet)");
            player.SendClientMessage(ColorPalette.Secondary.Main + " - You can only edit your own event");
            player.SendClientMessage(ColorPalette.Secondary.Main + " - If you want to add a map to your event, create the map first with /map commands, then load it into your event");
            player.SendClientMessage(ColorPalette.Secondary.Main + " - Once your event is playable, everybody can load it and join it");
        }

        [Command("s")]
        private static void SCommand(Player player)
        {
            player.LastSavedPosition = new Vector3R(player.Position, player.Angle);
            player.Notificate("Position saved");
        }
        [Command("r")]
        private static void RCommand(Player player)
        {
            if (player.LastSavedPosition.Position != Vector3.Zero)
            {
                player.Teleport(player.LastSavedPosition.Position + Vector3.UnitZ);
                player.Angle = player.LastSavedPosition.Rotation;
            }
            else
                player.SendClientMessage(ColorPalette.Error.Main + "Set the position with /s first");
        }

        [CommandGroup("time")]
        class TimeCommands
        {
            [Command(IsGroupHelp = true)]
            private static void TimeCommand(BasePlayer player)
            {
                player.SendClientMessage($"Usage: {ColorPalette.Secondary.Main}/time [option]");
                player.SendClientMessage($"Options: {ColorPalette.Secondary.Main}day, night, set [hour]");
            }
            [Command("day")]
            private static void DayCommand(BasePlayer player)
            {
                player.SetTime(12, 0);
            }

            [Command("night")]
            private static void NightCommand(BasePlayer player)
            {
                player.SetTime(0, 0);
            }

            [Command("set")]
            private static void SetCommand(BasePlayer player, int hour)
            {
                player.SetTime(hour % 24, 0);
            }
        }

        [Command("jet")]
        private static void JetpackCommand(Player player)
        {
            player.SpecialAction = SpecialAction.Usejetpack;
        }
    }
}
