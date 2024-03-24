using SampSharp.GameMode;
using SampSharp.GameMode.Controllers;
using SampSharp.GameMode.SAMP;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;

namespace SampSharpGameMode1.Civilisation
{
    [Controller]
    public class NPCController : IEventListener
    {
        private static Dictionary<string, NPC> npcPool = new();
        private static Dictionary<string, Queue<Vector3>> paths = new();

        public void RegisterEvents(BaseMode gameMode)
        {
            gameMode.PlayerConnected += GameMode_PlayerConnected;
            gameMode.PlayerSpawned += GameMode_PlayerSpawned;
            gameMode.PlayerUpdate += GameMode_PlayerUpdate;
            Console.WriteLine("NPCController.cs - NPCController.Init:I: NPC Controller initialized.");
        }

        public static void Add(string npcScriptName, Queue<Vector3> path)
        {
            string npcName = $"bot_{npcScriptName}_{BasePlayer.PoolSize}";
            Server.ConnectNPC(npcName, npcScriptName);
            paths[npcName] = path;
            Console.WriteLine("NPCController.cs - NPCController.Add:I: Added npc " +  npcName);
        }

        private static void GameMode_PlayerConnected(object sender, EventArgs e)
        {
            if ((sender as BasePlayer).IsNPC)
            {
                NPC npc = new NPC(sender as BasePlayer);
                npcPool.Add((sender as BasePlayer).Name, npc);
            }
        }

        private static void GameMode_PlayerSpawned(object sender, SampSharp.GameMode.Events.SpawnEventArgs e)
        {
            if ((sender as BasePlayer).IsNPC)
            {
                npcPool[(sender as BasePlayer).Name].OnSpawned();
            }
        }

        private static void GameMode_PlayerUpdate(object sender, SampSharp.GameMode.Events.PlayerUpdateEventArgs e)
        {
            if ((sender as BasePlayer).IsNPC)
            {
                npcPool[(sender as BasePlayer).Name].OnUpdate();
            }
        }
        public static Vector3? GetNextPoint(string npcName)
        {
            if(paths.ContainsKey(npcName) && paths[npcName].Count > 0)
            {
                return paths[npcName].Dequeue();
            }
            else
            {
                return null;
            }
        }

        public static void Kick(string npcName)
        {
            if(npcPool.TryGetValue(npcName, out var npc))
            {
                npc.Kick();
            }
        }

        public static void StartAI(string npcName, string npcType)
        {
            if (npcPool.TryGetValue(npcName, out var npc))
            {
                npc.Start(npcType == "onfoot" ? NPC.NPCType.Ped : NPC.NPCType.Vehicle);
            }
        }

        public static IEnumerable<string> GetConnectedBotNames()
        {
            return npcPool.Keys;
        }

        public static void GoToNextPos(string npcName)
        {
            if (npcPool.TryGetValue(npcName, out var npc))
            {
                npc.ForceGetNewPos();
            }
        }

    }
}
