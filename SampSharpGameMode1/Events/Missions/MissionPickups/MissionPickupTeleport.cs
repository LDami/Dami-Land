using SampSharp.GameMode.Events;
using SampSharp.Streamer.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Missions.MissionPickups
{
    public class MissionPickupTeleport : MissionPickupBase
    {
        public Vector3R TargetPosition { get; set; }
        public int TargetInterior { get; set; }
        public MissionPickupTeleport()
        {
        }

        internal override void OnPickupPickedUp(object sender, PlayerEventArgs args)
        {
            Console.WriteLine($"{args.Player.Name} pick up a pickup !");
            if ((sender as DynamicPickup).Id == this.Pickup.Id)
            {
                Console.WriteLine($"{args.Player.Name} has pick up the correct Pickup, teleporting ...");
                (args.Player as Player).Teleport(TargetPosition.Position);
            }
        }
    }
}
