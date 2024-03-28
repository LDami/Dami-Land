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

        private List<DynamicActor> actors;
        private List<DynamicObject> barriers;
        
        public SpectatorGroup(Vector3 position, Vector3 lookAtPos, int virtualWord)
        {
            //Console.WriteLine("Spectator group pos: " + position);
            this.Position = position;
            this.VirtualWord = virtualWord;

            actors = new List<DynamicActor>();
            Random rdm = new();
            Random rdmPositive = new();
            for(int i = 0; i < 10; i++)
            {
                Vector3 shiftPos = new(
                    (rdmPositive.NextDouble() > 0.5 ? 2 : -2) * rdm.NextDouble(),
                    (rdmPositive.NextDouble() > 0.5 ? 2 : -2) * rdm.NextDouble(),
                    0
                );
                DynamicActor actor = new(47, position + shiftPos, 0, false, worldid: virtualWord);
                actor.ApplyAnimation("ON_LOOKERS", "shout_02", 4.1f, true, false, false, false, 0);
                actor.FacingAngle = Utils.GetAngleToPoint(actor.Position.XY, lookAtPos.XY);
                actors.Add(actor);
            }
            barriers = new List<DynamicObject>();
            DynamicObject barrier = new(BarrierModel, position + Vector3.UnitY * (Radius/2), default, virtualWord); // Up
            barriers.Add(barrier);
            barrier = new(BarrierModel, position - Vector3.UnitY * (Radius / 2), default, virtualWord); // Down
            barriers.Add(barrier);
            barrier = new(BarrierModel, position + Vector3.Left * (Radius / 2), new Vector3(0, 0, 90), virtualWord); // Left
            barriers.Add(barrier);
            barrier = new(BarrierModel, position + Vector3.Right * (Radius / 2), new Vector3(0, 0, 90), virtualWord); // Right
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
