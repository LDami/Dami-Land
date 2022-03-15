using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.Display;
using SampSharp.GameMode.Events;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharpGameMode1.Display;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Commands
{
	class MappingCommands
    {
        [Command("mapping", Shortcut = "map")]
        private static void MappingCommand(Player player)
        {
            player.SendClientMessage($"Usage: {ColorPalette.Secondary.Main}/mapping [action]");
            player.SendClientMessage($"Actions: {ColorPalette.Secondary.Main}help, init, list, addo, delo, replace, marker, dist, edit, save");
        }
        [CommandGroup("mapping")]
        class MappingCommandClass
        {
            [Command("help")]
            private static void HelpCommand(Player player)
            {
                string list =
                    $"{ColorPalette.Primary.Main}/mapping init {ColorPalette.Primary.Darken}Initialize the editor" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping list {ColorPalette.Primary.Darken}List all your races" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping addo [modelid] {ColorPalette.Primary.Darken}Add an object with specified modelid" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping delo [objectid] {ColorPalette.Primary.Darken}Delete the object" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping replace [objectid] [modelid] {ColorPalette.Primary.Darken}Replace the object by the s modelid" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping marker [1-2] {ColorPalette.Primary.Darken}Edit the marker position to get distance" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping dist {ColorPalette.Primary.Darken}Displays the distance between the markers" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping edit [objectid] {ColorPalette.Primary.Darken}Edit position/rotation of object" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping save {ColorPalette.Primary.Darken}Save the map" + "\n" +
                    $"{ColorPalette.Primary.Main}/mapping exit {ColorPalette.Primary.Darken}Close the editor"
                    ;
                MessageDialog dialog = new MessageDialog("Command list", list, "Close");
                dialog.Show(player);
            }
            [Command("init")]
            private static void InitCommand(Player player)
            {
                player.mapCreator ??= new MapCreator(player);
            }
            [Command("list")]
            private static void ListCommand(Player player)
            {
                InputDialog dialog = new InputDialog("Map list", "Enter keywords (separated with space) if you want to search a specific map, otherwise let the field empty", false, "Search", "Cancel");
                dialog.Response += (object sender, DialogResponseEventArgs e) =>
                {
                    if (e.DialogButton == DialogButton.Left)
                    {
                        string[] keywords = e.InputText.Split(" ");
                        MapCreator.ShowMapList(player, keywords);
                    }
                };
                dialog.Show(player);
            }
            [Command("addo")]
            private static void AddObjectCommand(Player player, int modelid)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.AddObject(modelid);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, type {ColorPalette.Secondary.Main}/mapping init {Color.Red}first");
            }
            [Command("delo")]
            private static void DelObjectCommand(Player player, int objectid)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.DelObject(objectid);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, type {ColorPalette.Secondary.Main}/mapping init {Color.Red}first");
            }
            [Command("replace")]
            private static void ReplaceCommand(Player player, int objectid, int modelid)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.ReplaceObject(objectid, modelid);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, type {ColorPalette.Secondary.Main}/mapping init {Color.Red}first");
            }
            [Command("marker")]
            private static void MarkerCommand(Player player, int marker)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.EditMarker(marker);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, type {ColorPalette.Secondary.Main}/mapping init {Color.Red}first");
            }
            [Command("dist")]
            private static void DistCommand(Player player)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.GetMarkersDistance();
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, type {ColorPalette.Secondary.Main}/mapping init {Color.Red}first");
            }
            [Command("edit")]
            private static void EditCommand(Player player, int objectid)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.EditObject(objectid);
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, type {ColorPalette.Secondary.Main}/mapping init {Color.Red}first");
            }
            [Command("save")]
            private static void SaveCommand(Player player)
            {
                if (!(player.mapCreator is null))
                    player.mapCreator.Save();
                else
                    player.SendClientMessage(Color.Red, $"Map creator is not initialized, type {ColorPalette.Secondary.Main}/mapping init {Color.Red}first");
            }
        }
    }
}
