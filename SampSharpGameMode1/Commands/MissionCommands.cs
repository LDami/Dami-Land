using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.SAMP.Commands;
using SampSharp.GameMode.World;
using SampSharpGameMode1.Display;
using SampSharpGameMode1.Events.Missions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Commands
{
    class MissionCommands
    {
        [CommandGroup("mission")]
        class MissionCommandClass
        {
            static Mission m = new Mission();
            [Command("create")]
            private static void CreateCommand(Player player)
            {
                if (player.pEvent != null || player.mapCreator != null)
                    return;
                if (!(player.eventCreator is MissionCreator))
                {
                    player.eventCreator = new MissionCreator(player);
                }
                player.eventCreator.Create();
            }
            [Command("exit")]
            private static void ExitCommand(Player player)
            {
                if (player.eventCreator != null)
                {
                    player.eventCreator.Unload();
                    player.eventCreator = null;
                }
            }
            [CommandGroup("create")]
            class MissionCreateClass
            {
                [Command("stage")]
                private static void CreateStage(Player player)
                {
                    if (player.eventCreator is MissionCreator)
                    {
                        (player.eventCreator as MissionCreator).CreateStage();
                    }
                }
                [Command("actor")]
                private static void CreateActor(Player player, int modelid)
                {
                    if (player.eventCreator is MissionCreator)
                    {
                        (player.eventCreator as MissionCreator).CreateActor(modelid, new Vector3R(player.Position + Vector3.Forward, 0));
                    }
                }
                [Command("npc")]
                private static void CreateNPC(Player player, string name)
                {
                    if (player.eventCreator is MissionCreator)
                    {
                        (player.eventCreator as MissionCreator).CreateNPC(name);
                    }
                }
            }
            [CommandGroup("add")]
            class MissionAddClass
            {
                [Command("vehicle")]
                private static void AddVehicle(Player player)
                {
                    if (player.eventCreator is MissionCreator)
                    {
                        if (player.InAnyVehicle)
                            (player.eventCreator as MissionCreator).AddVehicle(player.Vehicle);
                        else
                            player.SendClientMessage(Color.Red, "You must be in the vehicle you want to add to the scene");
                    }
                }
            }
        }
    }
}
