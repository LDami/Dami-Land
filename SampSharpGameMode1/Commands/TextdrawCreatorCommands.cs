using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Commands
{
	class TextdrawCreatorCommands
    {
        [Command("td")]
        private static void TDCommand(Player player)
		{
            player.SendClientMessage("Usage: /td [action]");
            player.SendClientMessage("Actions: init, exit, load, save, hudswitch, add box, add text, select, unselect");
        }

        [CommandGroup("td")]
        class TextdrawCommandClass
        {
            [Command("init")]
            private static void InitTD(Player player)
            {
                if (player.pEvent != null)
                    return;
                if (player.IsAdmin)
                {
                    if (player.textdrawCreator == null)
                        player.textdrawCreator = new TextdrawCreator(player);

                    player.textdrawCreator.Init();
                    player.SendClientMessage("Textdraw Creator initialized");
                }
                else
                    player.SendClientMessage(Color.Red + "You are not Administrator");
            }
            [Command("exit")]
            private static void Exit(Player player)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.Close();
                }
            }
            [Command("load", UsageMessage = "Usage: /td load [name (without .json)]")]
            private static void Load(Player player, string name)
            {
                if (player.pEvent != null)
                    return;
                if (player.IsAdmin)
                {
                    if (player.textdrawCreator == null)
                        player.textdrawCreator = new TextdrawCreator(player);

                    if (!player.textdrawCreator.IsEditing)
                        player.textdrawCreator.Load(name);
                    else
                        player.SendClientMessage("You must close the opened editor before loading a new one");
                }
                else
                    player.SendClientMessage(Color.Red + "You are not Administrator");
            }
            [Command("save", UsageMessage = "Usage: /td save [name (without .json)]")]
            private static void Save(Player player, string name)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.Save(name);
                }
            }
            [Command("hudswitch")]
            private static void HUDSwitch(Player player)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.HUDSwitch();
                    player.SendClientMessage("Textdraw Creator interface has been switched");
                }
                else
                    player.SendClientMessage(Color.Red, "The Textdraw creator has not been initialized");
            }
            [Command("add")]
            private static void AddBox(Player player)
            {
                player.SendClientMessage("Usage: /td add [box/text] [name]");
            }
            [Command("add box")]
            private static void AddBox(Player player, string name)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.AddBox(name);
                    player.SendClientMessage("Textdraw created");
                }
                else
                    player.SendClientMessage(Color.Red, "The Textdraw creator has not been initialized");
            }
            [Command("add text")]
            private static void AddText(Player player, string name)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.AddText(name);
                    player.SendClientMessage("Textdraw created");
                }
                else
                    player.SendClientMessage(Color.Red, "The Textdraw creator has not been initialized");
            }
            [Command("select")]
            private static void Select(Player player, string name)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.Select(name);
                }
                else
                    player.SendClientMessage(Color.Red, "The Textdraw creator has not been initialized");
            }
            [Command("unselect")]
            private static void Unselect(Player player)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.Unselect();
                }
                else
                    player.SendClientMessage(Color.Red, "The Textdraw creator has not been initialized");
            }
        }
    }
}
