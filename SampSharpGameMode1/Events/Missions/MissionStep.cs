﻿using SampSharp.GameMode.World;
using SampSharp.Streamer.World;
using SampSharpGameMode1.Events.Missions.MissionPickups;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Missions
{
    public class MissionStepEventArgs : EventArgs
    {
        public Player Player;
        public bool Success;
    }
    internal abstract class MissionStep
    {
        internal int Id { get; set; }
        internal string Name { get; set; }
        internal int VirtualWorld { get; set; }
        internal Vector3R SpawnPoint { get; set; }
        internal List<DynamicActor> Actors { get; set; } = new List<DynamicActor>();
        internal List<MissionPickupBase> Pickups { get; set; } = new List<MissionPickupBase>();
        internal List<BaseVehicle> Vehicles { get; set; } = new List<BaseVehicle>();

        public event EventHandler<MissionStepEventArgs> Complete;
        protected virtual void OnComplete(MissionStepEventArgs e)
        {
            Complete?.Invoke(this, e);
        }

        internal abstract void Load(int id, int virtualWorld = -1);
        internal abstract void Execute(List<Player> players);
    }
}
