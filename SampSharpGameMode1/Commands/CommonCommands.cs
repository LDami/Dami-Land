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
        [Command("help")]
        private static void HelpCommand(Player player)
        {
            player.SendClientMessage(ColorPalette.Primary.Main + "This is a SA-MP where you can create your own maps, races and derbies !");
            player.SendClientMessage($"Type {ColorPalette.Secondary.Main}/event-infos{Color.White} to get more information about how to create a map, a race, or a derby.");
            player.SendClientMessage($"Teleport yourself where you want with {ColorPalette.Secondary.Main}/tlps");
            player.SendClientMessage($"This server is still in beta, type {ColorPalette.Secondary.Main}/beta{Color.White} to see what is coming soon !");
        }
        [Command("beta")]
        private static void BetaCommand(Player player)
        {
            player.SendClientMessage(ColorPalette.Primary.Main + "These features are still in development and will be ready to test soon:");
            player.SendClientMessage(" - AI / NPC to play with");
            player.SendClientMessage(" - More event types");
        }
        [Command("event-infos")]
        private static void EventInfosCommand(Player player)
        {
            player.SendClientMessage(ColorPalette.Primary.Main + "On this server you can create your own races and derbies, so please read the following instructions:");
            player.SendClientMessage($" - Use {ColorPalette.Secondary.Main}/race{Color.White} to see race creator commands, and {ColorPalette.Secondary.Main}/derby{Color.White} for derby creator commands");
            player.SendClientMessage($" - Make sure you use a keyboard with numpad (controllers are not supported yet)");
            player.SendClientMessage($" - You can only edit your own event");
            player.SendClientMessage($" - If you want to add a map to your event, create the map first with {ColorPalette.Secondary.Main}/map{Color.White} commands, then load it into your event");
            player.SendClientMessage($" - Don't forget to save your creations with {ColorPalette.Secondary.Main}/race save{Color.White}, {ColorPalette.Secondary.Main}/derby save{Color.White} or {ColorPalette.Secondary.Main}/map save{Color.White}");
            player.SendClientMessage($" - Once your event is playable, everybody can load it and join it");
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
                if (!player.IsInEvent)
                {
                    player.Teleport(player.LastSavedPosition.Position + Vector3.UnitZ);
                    player.Angle = player.LastSavedPosition.Rotation;
                }
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

        // /stats
        [Command("stats")]
        private static void StatsCommand(Player player)
        {
            ShowPlayerStats(player, player);
        }
        // /stats <id/name> of connected player
        [Command("stats")]
        private static void StatsCommand(Player player, Player target)
        {
            player.SendClientMessage(target.Name + " is not connected");
            if (target.IsConnected)
                ShowPlayerStats(player, target);
            else
            {
                player.SendClientMessage(target.Name + " is not connected");
            }
        }
        // /stats <name> of player, connected or not
        [Command("stats")]
        private static void StatsCommand(Player player, string targetName)
        {
            MySQLConnector mySQLConnector = MySQLConnector.Instance();
            Dictionary<string, object> param = new Dictionary<string, object>();
            param.Add("@name", targetName);
            mySQLConnector.OpenReader("SELECT id FROM users WHERE name=@name", param);
            Dictionary<string, string> results = mySQLConnector.GetNextRow();
            mySQLConnector.CloseReader();

            if(results.Count > 0)
            {
                Player p = new Player();
                p.DbId = Convert.ToInt32(results["id"]);

                param.Clear();
                param.Add("@id", p.DbId);
                mySQLConnector.OpenReader("SELECT stat_playtime, stat_playedraces, stat_derbies FROM users WHERE user_id=@id", param);
                results = mySQLConnector.GetNextRow();
                mySQLConnector.CloseReader();

                if(results.Count > 0)
                {
                    p.PlayedTime = TimeSpan.Parse(results["stat_playtime"]);
                    p.PlayedRaces = Convert.ToInt32(results["stat_playedraces"]);
                    p.PlayedDerbies = Convert.ToInt32(results["stat_derbies"]);
                }
                ShowPlayerStats(player, p);
            }
            else
            {
                player.SendClientMessage(targetName + " is not a valid user");
            }
        }

        private static void ShowPlayerStats(Player player, Player target)
        {
            ListDialog dialog = new ListDialog($"{target.Name} stats", "Close");
            dialog.AddItem($"{ColorPalette.Primary.Main}Database ID: {new Color(255, 255, 255)}{ target.DbId}");
            dialog.AddItem($"{ColorPalette.Primary.Main}Last Login: {new Color(255, 255, 255)}{target.LastLoginDate}");
            dialog.AddItem($"{ColorPalette.Primary.Main}Played time: {new Color(255, 255, 255)}{target.PlayedTime}");
            dialog.AddItem($"{ColorPalette.Primary.Main}Finished Races: {new Color(255, 255, 255)}{target.PlayedRaces}");
            dialog.AddItem($"{ColorPalette.Primary.Main}Finished Derbies: {new Color(255, 255, 255)} {target.PlayedDerbies}");
            dialog.Show(player);
        }

        [Command("jet")]
        private static void JetpackCommand(Player player)
        {
            player.SpecialAction = SpecialAction.Usejetpack;
        }
    }
}
