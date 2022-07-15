using SampSharp.GameMode;
using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Civilisation
{
    public class SpectatorGroup
    {
        public Vector3 Position;
        public int VirtualWord;
        public float Radius = 3.7f;
        public int BarrierModel = 3281;

        private BasePlayer player;
        private List<DynamicActor> actors;
        private List<DynamicObject> barriers;
        
        public SpectatorGroup(BasePlayer player, Vector3 position, int virtualWord)
        {
            this.Position = position;
            this.VirtualWord = virtualWord;

            this.player = player;
            actors = new List<DynamicActor>();
            Random rdm = new Random();
            Random rdmPositive = new Random();
            for(int i = 0; i < 5; i++)
            {
                Vector3 shiftPos = new Vector3(
                    (rdmPositive.NextDouble() > 0.5 ? 1 : -1) * rdm.NextDouble(),
                    (rdmPositive.NextDouble() > 0.5 ? 1 : -1) * rdm.NextDouble(),
                    0
                );
                DynamicActor actor = new DynamicActor(47, position + shiftPos, 0, false, worldid: virtualWord);
                actor.ApplyAnimation("ON_LOOKERS", "shout_02", 4.1f, true, false, false, true, 0);
                actors.Add(actor);
            }
            barriers = new List<DynamicObject>();
            DynamicObject barrier = new DynamicObject(BarrierModel, position + Vector3.UnitY * (Radius/2), default, virtualWord, player: player); // Up
            barriers.Add(barrier);
            barrier = new DynamicObject(BarrierModel, position - Vector3.UnitY * (Radius / 2), default, virtualWord, player: player); // Down
            barriers.Add(barrier);
            barrier = new DynamicObject(BarrierModel, position + Vector3.Left * (Radius / 2), new Vector3(0, 0, 90), virtualWord, player: player); // Left
            barriers.Add(barrier);
            barrier = new DynamicObject(BarrierModel, position + Vector3.Right * (Radius / 2), new Vector3(0, 0, 90), virtualWord, player: player); // Right
            barriers.Add(barrier);
        }

        public void Dispose()
        {
            foreach (DynamicActor actor in actors)
            {
                actor.Dispose();
            }
            actors.Clear();
            foreach (DynamicObject barrier in barriers)
            {
                barrier.Dispose();
            }
            barriers.Clear();
        }

    }
}
