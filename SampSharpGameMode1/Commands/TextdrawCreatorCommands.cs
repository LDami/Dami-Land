﻿using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharpGameMode1.Display;

#pragma warning disable IDE0051 // Disable useless private members

namespace SampSharpGameMode1.Commands
{
	class TextdrawCreatorCommands
    {
        [Command("td")]
        private static void TDCommand(Player player)
		{
            player.SendClientMessage("Usage: /td [action]");
            player.SendClientMessage("Actions: init, exit, load, save, hudswitch, add, delete, set box, set text, select, unselect");
        }

        [CommandGroup("td")]
        class TextdrawCommandClass
        {
            [Command("init")]
            private static void InitTD(Player player)
            {
                if (player.pEvent != null)
                    return;
#if DEBUG
                player.textdrawCreator ??= new TextdrawCreator(player);

                player.textdrawCreator.Init();
                player.SendClientMessage("Textdraw Creator initialized");
#endif
            }
            [Command("exit")]
            private static void Exit(Player player)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.Close();
                    player.textdrawCreator = null;
                }
            }
            [Command("load", UsageMessage = "Usage: /td load [name (without .json)]")]
            private static void Load(Player player, string name)
            {
                if (player.pEvent != null)
                    return;
#if DEBUG
                player.textdrawCreator ??= new TextdrawCreator(player);

                if (!player.textdrawCreator.IsEditing)
                    player.textdrawCreator.Load(name);
                else
                    player.SendClientMessage("You must close the opened editor before loading a new one");
#endif
            }
            [Command("save", UsageMessage = "Usage: /td save [name (without .json)]")]
            private static void Save(Player player, string name)
            {
                player.textdrawCreator?.Save(name);
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
            private static void AddCommand(Player player)
            {
                player.SendClientMessage("Usage: /td add [background/box/text/previewmodel] [name]");
            }
            [Command("add background")]
            private static void AddBackground(Player player, string name)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.AddBackground(name);
                }
                else
                    player.SendClientMessage(Color.Red, "The Textdraw creator has not been initialized");
            }
            [Command("add box")]
            private static void AddBox(Player player, string name)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.AddBox(name);
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
                }
                else
                    player.SendClientMessage(Color.Red, "The Textdraw creator has not been initialized");
            }
            [Command("add previewmodel")]
            private static void AddPreviewModel(Player player, string name)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.AddPreviewModel(name);
                }
                else
                    player.SendClientMessage(Color.Red, "The Textdraw creator has not been initialized");
            }
            [Command("delete")]
            private static void Delete(Player player, string name)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.DeleteTextdraw(name);
                }
                else
                    player.SendClientMessage(Color.Red, "The Textdraw creator has not been initialized");
            }
            [Command("set box")]
            private static void SetAsBox(Player player, string name)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.SetAsBox(name);
                    player.SendClientMessage("Textdraw correctly set as box");
                }
                else
                    player.SendClientMessage(Color.Red, "The Textdraw creator has not been initialized");
            }
            [Command("set text")]
            private static void SetAsText(Player player, string name)
            {
                if (player.textdrawCreator != null)
                {
                    player.textdrawCreator.SetAsText(name);
                    player.SendClientMessage("Textdraw correctly set as text");
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
