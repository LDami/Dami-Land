using SampSharp.GameMode;
using SampSharp.GameMode.SAMP;
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

        private List<DynamicTextLabel> textLabels;


        public SpectatorGroup(Vector3 position, Vector3 lookAtPos, int virtualWord)
        {
            //Console.WriteLine("Spectator group pos: " + position);
            this.Position = position;
            this.VirtualWord = virtualWord;

            barriers = new List<DynamicObject>();
            DynamicObject barrier = new(BarrierModel, position + Vector3.UnitY * (Radius / 2), default, virtualWord); // Up
            barriers.Add(barrier);
            barrier = new(BarrierModel, position - Vector3.UnitY * (Radius / 2), default, virtualWord); // Down
            barriers.Add(barrier);
            barrier = new(BarrierModel, position + Vector3.Left * (Radius / 2), new Vector3(0, 0, 90), virtualWord); // Left
            barriers.Add(barrier);
            barrier = new(BarrierModel, position + Vector3.Right * (Radius / 2), new Vector3(0, 0, 90), virtualWord); // Right
            barriers.Add(barrier);

            actors = new List<DynamicActor>();
            textLabels = new();
            Random rdm = new();
            Random rdmPositive = new();
            int number = rdm.Next(5, 15);
            for(int i = 0; i < number; i++)
            {
                Vector3 shiftPos = new(
                    (rdmPositive.NextDouble() > 0.5 ? Radius : -Radius) * rdm.NextDouble() / 2.2,
                    (rdmPositive.NextDouble() > 0.5 ? Radius : -Radius) * rdm.NextDouble() / 2.2,
                    0
                );
                int modelId = rdm.Next(311);
                while(modelId == 92 || modelId == 99) // Skaters removed due to default rolling animation
                {
                    modelId = rdm.Next(311);
                }
                DynamicActor actor = new(modelId, position + shiftPos, 0, false, worldid: virtualWord);
                if(rdm.NextDouble() > 0.8)
                {
                    actor.ApplyAnimation("ON_LOOKERS", "shout_02", 4.1f * i, true, false, false, false, 0);
                }
                actor.FacingAngle = Utils.GetAngleToPoint(actor.Position.XY, lookAtPos.XY);
                if(actor.Position.Z > (position.Z + 0.2))
                {
                    // Actor is probably glitching with barriers, remove it
                    actor.Dispose();
                }
                else
                    actors.Add(actor);
                textLabels.Add(new((position + shiftPos).ToString(), Color.White, position + shiftPos, 300));
            }
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
