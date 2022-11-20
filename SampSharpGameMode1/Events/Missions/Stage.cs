using SampSharp.GameMode;
using SampSharp.GameMode.Definitions;
using SampSharp.GameMode.World;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Missions
{
    internal class Stage : MissionStep
    {
        internal Vector3R SpawnPoint { get; set; }
        internal VehicleModelType? StartingVehicle { get; set; }
        public Goal Goal { get; set; }

        internal override void Load(int id, int virtualWorld = -1)
        {
            this.SpawnPoint = new Vector3R(new Vector3(-1527.83, 814.52, 6.75), -10);
            this.VirtualWorld = virtualWorld;
            this.Pickups = new List<MissionPickups.MissionPickupBase>();
            this.Pickups.Add(new MissionPickups.MissionPickupTeleport
            {
                //Pickup = new SampSharp.Streamer.World.DynamicPickup(19197, 5, new Vector3(-2706.73, 866.0, 71.7), worldid: virtualWorld),
                Pickup = new SampSharp.Streamer.World.DynamicPickup(19197, 5, new Vector3(-1518, 874, 7.1), worldid: virtualWorld),
                TargetPosition = new Vector3R(new Vector3(-2739.66, 855.76, 63), 180)
            }); ;
        }

        internal override void Execute(List<Player> players)
        {
            foreach(MissionPickups.MissionPickupBase pickup in this.Pickups)
            {
                pickup.Enable(players);
            }
            //this.Goal = new Goal(players, GoalType.GoToSneaky, null, new Vector3(-2706.73, 866.0, 70.7));
            this.Goal = new Goal(players, GoalType.GoToSneaky, null, new Vector3(-1518, 874, 7.0));
            this.Goal.Complete += (sender, args) =>
            {
                BasePlayer.SendClientMessageToAll("Stage passed");
                foreach (MissionPickups.MissionPickupBase pickup in this.Pickups)
                {
                    pickup.Dispose();
                }
                this.OnComplete(new MissionStepEventArgs { Player = args.Player, Success = true });
            };

            BaseVehicle veh = BaseVehicle.Create(StartingVehicle.GetValueOrDefault(VehicleModelType.Admiral), SpawnPoint.Position, SpawnPoint.Rotation, 0, 0);
            veh.VirtualWorld = this.VirtualWorld;
            foreach(Player p in players)
            {
                p.VirtualWorld = this.VirtualWorld;
                p.PutInVehicle(veh);
            }
        }
    }
}
