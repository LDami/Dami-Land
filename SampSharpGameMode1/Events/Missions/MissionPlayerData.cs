using System;
using System.Collections.Generic;
using System.Text;

namespace SampSharpGameMode1.Events.Missions
{
    public enum MissionPlayerStatus
    {
        Running,
        Spectating
    }
    internal class MissionPlayerData
    {
        public MissionPlayerStatus status;
        public int spectatePlayerIndex;
        public Vector3R startPosition;
    }
}
